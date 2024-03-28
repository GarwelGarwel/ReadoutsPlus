using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ReadoutsPlus
{
    [StaticConstructorOnStartup]
    public static class ReadoutsPlus
    {
        internal const int OpenMask = 32;

        static ReadoutsPlus()
        {
            Harmony harmony = new Harmony("Garwel.ReadoutsPlus");
            if (harmony.Patch(AccessTools.Method("Listing_ResourceReadout:DoThingDef"), postfix: new HarmonyMethod(typeof(ReadoutsPlus).GetMethod("Listing_ResourceReadout_DoThingDef"))) == null
                || harmony.Patch(AccessTools.Method("ResourceReadout:DrawResourceSimple"), postfix: new HarmonyMethod(typeof(ReadoutsPlus).GetMethod("ResourceReadout_DrawResourceSimple"))) == null
                || harmony.Patch(AccessTools.Method("Listing_ResourceReadout:DoCategory"), prefix: new HarmonyMethod(typeof(ReadoutsPlus).GetMethod("Listing_ResourceReadout_DoCategory"))) == null)
                Log($"Failed to initialize. Harmony patches applied: {harmony.GetPatchedMethods().Select(method => method.Name).ToCommaList()}.", true);
        }

        static void CheckClick(ThingDef thingDef)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (Event.current.clickCount == 1)
                    SelectNextThing(thingDef);
                else if (Event.current.clickCount == 2)
                    SelectAll(thingDef);
                Event.current.Use();
            }
        }

        #region HARMONY PATCHES

        public static void Listing_ResourceReadout_DoThingDef(Listing_ResourceReadout __instance, ThingDef thingDef, int nestLevel)
        {
            if (Mouse.IsOver(new Rect(nestLevel * __instance.nestIndentWidth + 18, __instance.CurHeight - __instance.lineHeight - __instance.verticalSpacing, __instance.ColumnWidth, __instance.lineHeight))
                && Find.CurrentMap.resourceCounter.GetCount(thingDef) > 0)
                CheckClick(thingDef);
        }

        public static void ResourceReadout_DrawResourceSimple(Rect rect, ThingDef thingDef)
        {
            if (Mouse.IsOver(rect) && Find.CurrentMap.resourceCounter.GetCount(thingDef) > 0)
            {
                GUI.DrawTexture(rect, TexUI.HighlightTex);
                CheckClick(thingDef);
            }
        }

        public static void Listing_ResourceReadout_DoCategory(Listing_ResourceReadout __instance, TreeNode_ThingCategory node, int nestLevel, int openMask)
        {
            Rect rect = new Rect(nestLevel * __instance.nestIndentWidth + 18, __instance.CurHeight, __instance.ColumnWidth, __instance.lineHeight);
            if (Mouse.IsOver(rect)
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && AnyOfCategoryOnCurrentMap(node.catDef))
            {
                bool isOpen = __instance.IsOpen(node, openMask);
                if (isOpen)
                    SoundDefOf.TabClose.PlayOneShotOnCamera();
                else SoundDefOf.TabOpen.PlayOneShotOnCamera();
                node.SetOpen(openMask, !isOpen);
                Event.current.Use();
            }
        }

        #endregion

        #region UTILITIES

        internal static void Log(string message, bool important = false)
        {
            if (important)
                Verse.Log.Warning($"[Readouts+] {message}");
#if DEBUG
            else Verse.Log.Message($"[Readouts+] {message}");
#endif
        }

        public static void SelectNextThing(ThingDef thingDef)
        {
            HaulDestinationManager haulDestinationManager = Find.CurrentMap?.haulDestinationManager;
            if (haulDestinationManager == null || thingDef == null)
                return;
            List<Thing> things = haulDestinationManager.AllGroupsListForReading.SelectMany(group => group.HeldThings).Where(thing => thing.def == thingDef).ToList();
            if (things.NullOrEmpty())
                return;
            int i = 0;
            Thing selected = Find.Selector.SingleSelectedThing;
            if (selected?.def == thingDef)
            {
                while (i < things.Count)
                    if (things[i++] == selected)
                        break;
                if (i >= things.Count)
                    i = 0;
            }
            CameraJumper.TryJumpAndSelect(things[i]);
        }

        public static void SelectAll(ThingDef thingDef)
        {
            Find.Selector.ClearSelection();
            HaulDestinationManager haulDestinationManager = Find.CurrentMap?.haulDestinationManager;
            if (haulDestinationManager == null || thingDef == null)
                return;
            foreach (Thing thing in haulDestinationManager.AllGroupsListForReading.SelectMany(group => group.HeldThings).Where(thing => thing.def == thingDef))
                Find.Selector.Select(thing);
        }

        public static bool AnyOfCategoryOnCurrentMap(ThingCategoryDef categoryDef)
        {
            Map map = Find.CurrentMap;
            if (map == null || categoryDef == null)
                return false;
            for (int i = 0; i < categoryDef.childThingDefs.Count; i++)
                if (map.resourceCounter.GetCount(categoryDef.childThingDefs[i]) > 0)
                    return true;
            for (int i = 0; i < categoryDef.childCategories.Count; i++)
                if (!categoryDef.childCategories[i].resourceReadoutRoot && AnyOfCategoryOnCurrentMap(categoryDef.childCategories[i]))
                    return true;
            return false;
        }

        public static bool IsOpen(this ThingCategoryDef category) => category.treeNode.IsOpen(OpenMask);

        public static List<ThingCategoryDef> GetOpenCategories(this IEnumerable<ThingCategoryDef> categories)
        {
            List<ThingCategoryDef> list = new List<ThingCategoryDef>();
            foreach (ThingCategoryDef cat1 in categories)
                foreach (ThingCategoryDef cat2 in cat1.ThisAndChildCategoryDefs)
                    if (cat2.IsOpen() && !list.Contains(cat2))
                        list.Add(cat2);
            return list;
        }

        public static string ThingCategoryDescription(this ThingCategoryDef def) => $"{def.defName} ({(IsOpen(def) ? "open" : "closed")})";

        public static string CategoriesDescription(List<ThingCategoryDef> categories) => categories?.Select(cat => ThingCategoryDescription(cat)).ToLineList("- ");

#endregion
    }
}
