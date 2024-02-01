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
        static ReadoutsPlus()
        {
            Harmony harmony = new Harmony("Garwel.ReadoutsPlus");
            if (harmony.Patch(AccessTools.Method("Listing_ResourceReadout:DoThingDef"), postfix: new HarmonyMethod(typeof(ReadoutsPlus).GetMethod("Listing_ResourceReadout_DoThingDef"))) == null
                || harmony.Patch(AccessTools.Method("ResourceReadout:DrawResourceSimple"), postfix: new HarmonyMethod(typeof(ReadoutsPlus).GetMethod("ResourceReadout_DrawResourceSimple"))) == null
                || harmony.Patch(AccessTools.Method("Listing_ResourceReadout:DoCategory"), prefix: new HarmonyMethod(typeof(ReadoutsPlus).GetMethod("Listing_ResourceReadout_DoCategory"))) == null)
                Log.Error($"[Readouts+] Failed to initialize. Harmony patches applied: {harmony.GetPatchedMethods().Select(method => method.Name).ToCommaList()}.");
        }

        static void SelectNextThing(ThingDef thingDef)
        {
            List<Thing> things = Find.CurrentMap.haulDestinationManager.AllGroupsListForReading.SelectMany(group => group.HeldThings).Where(thing => thing.def == thingDef).ToList();
            if (things.NullOrEmpty())
                return;
            int i = 0;
            Thing selected = Find.Selector.SingleSelectedThing;
            if (selected?.def == thingDef)
            {
                while(i < things.Count)
                    if (things[i++] == selected)
                        break;
                if (i >= things.Count)
                    i = 0;
            }
            CameraJumper.TryJumpAndSelect(things[i]);
        }

        static void SelectAll(ThingDef thingDef)
        {
            Find.Selector.ClearSelection();
            foreach (Thing thing in Find.CurrentMap.haulDestinationManager.AllGroupsListForReading.SelectMany(group => group.HeldThings).Where(thing => thing.def == thingDef))
                Find.Selector.Select(thing);
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

        static bool AnyOfCategory(ThingCategoryDef categoryDef)
        {
            Map map = Find.CurrentMap;
            for (int i = 0; i < categoryDef.childThingDefs.Count; i++)
                if (map.resourceCounter.GetCount(categoryDef.childThingDefs[i]) > 0)
                    return true;
            for (int i = 0; i < categoryDef.childCategories.Count; i++)
                if (AnyOfCategory(categoryDef.childCategories[i]))
                    return true;
            return false;
        }

        public static void Listing_ResourceReadout_DoCategory(Listing_ResourceReadout __instance, TreeNode_ThingCategory node, int nestLevel, int openMask)
        {
            Rect rect = new Rect(nestLevel * __instance.nestIndentWidth + 18, __instance.CurHeight, __instance.ColumnWidth, __instance.lineHeight);
            if (Mouse.IsOver(rect)
                && Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && AnyOfCategory(node.catDef))
            {
                if (__instance.IsOpen(node, openMask))
                {
                    SoundDefOf.TabClose.PlayOneShotOnCamera();
                    node.SetOpen(openMask, false);
                }
                else
                {
                    SoundDefOf.TabOpen.PlayOneShotOnCamera();
                    node.SetOpen(openMask, true);
                }
                Event.current.Use();
            }
        }
    }
}
