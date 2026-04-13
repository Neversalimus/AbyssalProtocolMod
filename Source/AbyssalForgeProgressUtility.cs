using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalForgeProgressUtility
    {
        public const string ForgeDefName = "ABY_AbyssalForge";
        public const string ResidueDefName = "ABY_AbyssalResidue";
        public const string AttunementHediffDefName = "ABY_AbyssalAttunement";

        public const string AllCategory = "All";
        public const string CoreCategory = "Core";
        public const string WeaponsCategory = "Weapons";
        public const string ImplantsCategory = "Implants";
        public const string RitualCategory = "Ritual";
        public const string HeraldCategory = "Herald";

        private static readonly List<string> CategoryOrder = new List<string>
        {
            AllCategory,
            CoreCategory,
            WeaponsCategory,
            ImplantsCategory,
            RitualCategory,
            HeraldCategory
        };

        public static IEnumerable<string> Categories => CategoryOrder;

        public static ThingDef ForgeDef => DefDatabase<ThingDef>.GetNamedSilentFail(ForgeDefName);
        public static ThingDef ResidueDef => DefDatabase<ThingDef>.GetNamedSilentFail(ResidueDefName);
        public static HediffDef AttunementHediffDef => DefDatabase<HediffDef>.GetNamedSilentFail(AttunementHediffDefName);

        public static List<RecipeDef> GetForgeRecipes(ThingDef forgeDef = null)
        {
            forgeDef ??= ForgeDef;
            if (forgeDef?.AllRecipes == null)
            {
                return new List<RecipeDef>();
            }

            return forgeDef.AllRecipes
                .Where(recipe => recipe != null)
                .OrderBy(GetRequiredResidue)
                .ThenBy(recipe => GetCategoryOrderIndex(GetCategory(recipe)))
                .ThenBy(recipe => GetRecipeDisplayLabel(recipe))
                .ToList();
        }

        public static DefModExtension_AbyssalForgeUnlock GetUnlockExtension(RecipeDef recipe)
        {
            if (recipe == null)
            {
                return null;
            }

            DefModExtension_AbyssalForgeUnlock extension = recipe.GetModExtension<DefModExtension_AbyssalForgeUnlock>();
            if (extension != null)
            {
                return extension;
            }

            ThingDef product = GetPrimaryProduct(recipe);
            return product?.GetModExtension<DefModExtension_AbyssalForgeUnlock>();
        }

        public static ThingDef GetPrimaryProduct(RecipeDef recipe)
        {
            if (recipe?.products == null || recipe.products.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < recipe.products.Count; i++)
            {
                ThingDef thingDef = recipe.products[i]?.thingDef;
                if (thingDef != null)
                {
                    return thingDef;
                }
            }

            return null;
        }

        public static int GetRequiredResidue(RecipeDef recipe)
        {
            return GetUnlockExtension(recipe)?.requiredResidue ?? 0;
        }

        public static string GetCategory(RecipeDef recipe)
        {
            string category = GetUnlockExtension(recipe)?.category;
            if (string.IsNullOrWhiteSpace(category))
            {
                return CoreCategory;
            }

            return CategoryOrder.Contains(category) ? category : CoreCategory;
        }

        public static string GetCategoryLabel(string category)
        {
            return ("ABY_ForgeCategory_" + category).Translate();
        }

        public static string GetRecipeDisplayLabel(RecipeDef recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            ThingDef product = GetPrimaryProduct(recipe);
            if (product?.label != null)
            {
                return product.LabelCap;
            }

            return recipe.LabelCap;
        }

        public static bool RecipeMatchesCategory(RecipeDef recipe, string category)
        {
            return category == AllCategory || GetCategory(recipe) == category;
        }

        public static bool IsRecipeUnlocked(RecipeDef recipe, int totalResidueOffered)
        {
            return totalResidueOffered >= GetRequiredResidue(recipe);
        }

        public static int CountAvailableResidue(Map map)
        {
            ThingDef residueDef = ResidueDef;
            if (map?.listerThings == null || residueDef == null)
            {
                return 0;
            }

            List<Thing> stacks = map.listerThings.ThingsOfDef(residueDef);
            if (stacks == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                Thing thing = stacks[i];
                if (thing == null || thing.Destroyed || !thing.Spawned)
                {
                    continue;
                }

                total += thing.stackCount;
            }

            return total;
        }

        public static int ConsumeResidue(Map map, int requestedAmount, IntVec3 priorityCell)
        {
            if (requestedAmount <= 0)
            {
                return 0;
            }

            ThingDef residueDef = ResidueDef;
            if (map?.listerThings == null || residueDef == null)
            {
                return 0;
            }

            List<Thing> stacks = map.listerThings.ThingsOfDef(residueDef)
                .Where(thing => thing != null && !thing.Destroyed && thing.Spawned)
                .OrderBy(thing => thing.Position.DistanceToSquared(priorityCell))
                .ToList();

            int remaining = requestedAmount;
            int consumed = 0;
            for (int i = 0; i < stacks.Count && remaining > 0; i++)
            {
                Thing stack = stacks[i];
                int take = stack.stackCount <= remaining ? stack.stackCount : remaining;
                if (take <= 0)
                {
                    continue;
                }

                if (take >= stack.stackCount)
                {
                    consumed += stack.stackCount;
                    remaining -= stack.stackCount;
                    stack.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    Thing split = stack.SplitOff(take);
                    consumed += take;
                    remaining -= take;
                    split.Destroy(DestroyMode.Vanish);
                }
            }

            return consumed;
        }

        public static int GetCategoryOrderIndex(string category)
        {
            int index = CategoryOrder.IndexOf(category);
            return index >= 0 ? index : CategoryOrder.Count;
        }
    }
}
