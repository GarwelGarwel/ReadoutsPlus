using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ReadoutsPlus
{
    [StaticConstructorOnStartup]
    public static class ReadoutsPlus
    {
        static ReadoutsPlus()
        {
            Log("Starting ReadoutsPlus...");
            Harmony harmony = new Harmony("ReadoutsPlus");
            if (harmony.Patch(AccessTools.Method("Listing_ResourceReadout:DoThingDef"), prefix: new HarmonyMethod(typeof(ReadoutsPlus).GetMethod("Listing_ResourceReadout_DoThingDef"))) == null)
                Log("Failed to apply Harmony patch!");
        }

        static void Log(string message)
        {
            Verse.Log.Message($"[ReadoutsPlus] {message}");
        }

        static ThingDef mousePosition;

        static Thing NextUnselectedThing(ThingDef thingDef)
        {
            Map map = Find.CurrentMap;
            List<Thing> things = map.listerThings.ThingsOfDef(thingDef);
            Log($"{things.Count} {thingDef} things found on the map.");
            Thing selected = Find.Selector.SingleSelectedThing;
            int i = 0;
            if (selected?.def == thingDef)
            {
                Log($"Thing {Find.Selector.SingleSelectedThing} is already selected.");
                while(i < things.Count)
                    if (things[i++] == selected)
                        break;
                if (i >= things.Count)
                    i = 0;
            }
            return things[i];
        }

        public static void Listing_ResourceReadout_DoThingDef(Listing_ResourceReadout __instance, ThingDef thingDef, int nestLevel)
        {
            Map map = Find.CurrentMap;
            if (map.resourceCounter.GetCount(thingDef) == 0)
                return;
            Rect rect = new Rect(nestLevel * __instance.nestIndentWidth + 18, __instance.CurHeight, __instance.ColumnWidth, __instance.lineHeight);
            if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseDown)
            {
                mousePosition = thingDef;
                Event.current.Use();
            }
            if (Mouse.IsOver(rect) && Event.current.type == EventType.MouseUp)
            {
                if (mousePosition == thingDef)
                {
                    CameraJumper.TryJumpAndSelect(NextUnselectedThing(thingDef));
                    Event.current.Use();
                }
                mousePosition = null;
            }
        }
    }
}
