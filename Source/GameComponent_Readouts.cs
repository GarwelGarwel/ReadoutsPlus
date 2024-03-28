using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ReadoutsPlus
{
    public class GameComponent_Readouts : GameComponent
    {
        static readonly List<ThingCategoryDef> emptyList = new List<ThingCategoryDef>();

        List<ThingCategoryDef> readoutCategoriesCached;

        public List<ThingCategoryDef> ReadoutCategories
        {
            get
            {
                if (readoutCategoriesCached != null)
                    return readoutCategoriesCached;
                if (Find.MapUI == null)
                {
                    ReadoutsPlus.Log("MapUI is null.", true);
                    return emptyList;
                }
                ResourceReadout resourceReadout = AccessTools.Field(typeof(MapInterface), "resourceReadout").GetValue(Find.MapUI) as ResourceReadout;
                if (resourceReadout != null)
                    if ((readoutCategoriesCached = AccessTools.Field(typeof(ResourceReadout), "RootThingCategories").GetValue(resourceReadout) as List<ThingCategoryDef>) != null)
                        return readoutCategoriesCached;
                    else ReadoutsPlus.Log("Could not access RootThingCategories.", true);
                else ReadoutsPlus.Log("Could not access resourceReadout.", true);
                return emptyList;
            }
        }

        public GameComponent_Readouts(Game game)
            : base()
        { }

        public void SetOpenCategories(List<ThingCategoryDef> openCategories)
        {
            foreach (ThingCategoryDef cat1 in ReadoutCategories)
                foreach (ThingCategoryDef cat2 in cat1.ThisAndChildCategoryDefs)
                    cat2.treeNode.SetOpen(ReadoutsPlus.OpenMask, openCategories.Contains(cat2));
        }

        public void CloseAllCategories()
        {
            foreach (ThingCategoryDef cat1 in ReadoutCategories)
                foreach (ThingCategoryDef cat2 in cat1.ThisAndChildCategoryDefs)
                    cat2.treeNode.SetOpen(ReadoutsPlus.OpenMask, false);
        }

        public override void ExposeData()
        {
            List<ThingCategoryDef> openCategories = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                openCategories = ReadoutCategories.GetOpenCategories();
                ReadoutsPlus.Log($"Current open categories:\n{ReadoutsPlus.CategoriesDescription(openCategories)}");
                if (openCategories.NullOrEmpty())
                    return;
            }
            Scribe_Collections.Look(ref openCategories, "OpenCategories", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (openCategories == null)
                {
                    ReadoutsPlus.Log("OpenCategories not found.", true);
                    CloseAllCategories();
                    return;
                }
                ReadoutsPlus.Log($"Loaded open categories:\n{ReadoutsPlus.CategoriesDescription(openCategories)}");
                SetOpenCategories(openCategories);
            }
        }
    }
}
