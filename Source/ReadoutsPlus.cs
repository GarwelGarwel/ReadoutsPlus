using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ReadoutsPlus
{
    [StaticConstructorOnStartup]
    public static class ReadoutsPlus
    {
        static ReadoutsPlus()
        {
            Harmony harmony = new Harmony("ReadoutsPlus");
            if (harmony.Patch(AccessTools.Method("Listing_ResourceReadout:DoThingDef"), postfix: new HarmonyMethod(typeof(ReadoutsPlus).GetMethod("Listing_ResourceReadout_DoThingDef"))) == null
                || harmony.Patch(AccessTools.Method("ResourceReadout:DrawResourceSimple"), postfix: new HarmonyMethod(typeof(ReadoutsPlus).GetMethod("ResourceReadout_DrawResourceSimple"))) == null)
                Log("Failed to apply Harmony patch!", true);
        }

        static void Log(string message, bool important = false)
        {
            if (important || Prefs.DevMode)
                Verse.Log.Message($"[Readouts+] {message}");
        }

        static Thing NextUnselectedThing(ThingDef thingDef)
        {
            List<Thing> things = Find.CurrentMap.haulDestinationManager.AllGroupsListForReading.SelectMany(group => group.HeldThings).Where(thing => thing.def == thingDef).ToList();
            if (things.NullOrEmpty())
            {
                Log($"List of {thingDef} on the map is null or empty.", true);
                return null;
            }
            Thing selected = Find.Selector.SingleSelectedThing;
            int i = 0;
            if (selected?.def == thingDef)
            {
                while(i < things.Count)
                    if (things[i++] == selected)
                        break;
                if (i >= things.Count)
                    i = 0;
            }
            return things[i];
        }

        static void SelectAll(ThingDef thingDef)
        {
            Find.Selector.ClearSelection();
            foreach (Thing thing in Find.CurrentMap.haulDestinationManager.AllGroupsListForReading.SelectMany(group => group.HeldThings).Where(thing => thing.def == thingDef))
                Find.Selector.Select(thing);
        }

        static void ProcessClick(Rect rect, ThingDef thingDef)
        {
            if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (Event.current.clickCount == 1)
                    CameraJumper.TryJumpAndSelect(NextUnselectedThing(thingDef));
                else if (Event.current.clickCount == 2)
                    SelectAll(thingDef);
                Event.current.Use();
            }
        }

        public static void Listing_ResourceReadout_DoThingDef(Listing_ResourceReadout __instance, ThingDef thingDef, int nestLevel)
        {
            if (Find.CurrentMap.resourceCounter.GetCount(thingDef) > 0)
                ProcessClick(
                    new Rect(nestLevel * __instance.nestIndentWidth + 18, __instance.CurHeight - __instance.lineHeight - __instance.verticalSpacing, __instance.ColumnWidth, __instance.lineHeight),
                    thingDef);
        }

        public static void ResourceReadout_DrawResourceSimple(Rect rect, ThingDef thingDef)
        {
            if (Find.CurrentMap.resourceCounter.GetCount(thingDef) == 0)
                return;
            if (Mouse.IsOver(rect))
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            ProcessClick(rect, thingDef);
        }
    }
}
