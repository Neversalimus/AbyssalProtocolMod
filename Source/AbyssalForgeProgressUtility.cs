using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
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

        public class IngredientAvailabilityEntry
        {
            public string label;
            public int requiredCount;
            public int availableCount;

            public bool IsSatisfied => availableCount >= requiredCount;

            public string ToDisplayString()
            {
                return label + " " + availableCount + "/" + requiredCount;
            }
        }

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
            if (forgeDef == null)
            {
                forgeDef = ForgeDef;
            }

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

        public static string GetRecipeIngredientSummary(RecipeDef recipe, int maxEntries)
        {
            if (recipe?.ingredients == null || recipe.ingredients.Count == 0)
            {
                return "ABY_ForgePatternNoMaterialData".Translate();
            }

            List<string> parts = new List<string>();
            int limit = Math.Max(1, maxEntries);
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                IngredientCount ingredient = recipe.ingredients[i];
                if (ingredient == null)
                {
                    continue;
                }

                string label = GetIngredientLabel(ingredient);
                int count = Mathf.CeilToInt(ingredient.GetBaseCount());
                parts.Add(count + " " + label);

                if (parts.Count >= limit)
                {
                    break;
                }
            }

            if (parts.Count == 0)
            {
                return "ABY_ForgePatternNoMaterialData".Translate();
            }

            if (recipe.ingredients.Count > limit)
            {
                parts.Add("…");
            }

            return string.Join(" • ", parts.ToArray());
        }

        public static string GetRecipeIngredientTooltip(RecipeDef recipe)
        {
            if (recipe?.ingredients == null || recipe.ingredients.Count == 0)
            {
                return "ABY_ForgePatternNoMaterialData".Translate();
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                IngredientCount ingredient = recipe.ingredients[i];
                if (ingredient == null)
                {
                    continue;
                }

                string label = GetIngredientLabel(ingredient);
                int count = Mathf.CeilToInt(ingredient.GetBaseCount());
                parts.Add("• " + count + " " + label);
            }

            return string.Join("\n", parts.ToArray());
        }

        public static List<IngredientAvailabilityEntry> GetIngredientAvailabilityEntries(Map map, RecipeDef recipe)
        {
            List<IngredientAvailabilityEntry> result = new List<IngredientAvailabilityEntry>();
            if (recipe?.ingredients == null || recipe.ingredients.Count == 0)
            {
                return result;
            }

            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                IngredientCount ingredient = recipe.ingredients[i];
                if (ingredient == null)
                {
                    continue;
                }

                result.Add(new IngredientAvailabilityEntry
                {
                    label = GetIngredientLabel(ingredient),
                    requiredCount = Mathf.CeilToInt(ingredient.GetBaseCount()),
                    availableCount = CountAvailableForIngredient(map, ingredient)
                });
            }

            return result;
        }

        public static string GetRecipeAvailabilityTooltip(Map map, RecipeDef recipe)
        {
            List<IngredientAvailabilityEntry> entries = GetIngredientAvailabilityEntries(map, recipe);
            if (entries.Count == 0)
            {
                return "ABY_ForgePatternNoMaterialData".Translate();
            }

            List<string> lines = new List<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                IngredientAvailabilityEntry entry = entries[i];
                lines.Add("• " + entry.label + ": " + entry.availableCount + "/" + entry.requiredCount);
            }

            return string.Join("\n", lines.ToArray());
        }

        public static string GetAttunementTooltip(int tier, bool active)
        {
            List<string> lines = new List<string>();
            HediffDef hediffDef = AttunementHediffDef;
            if (hediffDef != null && !hediffDef.description.NullOrEmpty())
            {
                lines.Add(hediffDef.description);
            }

            HediffStage stage = GetAttunementStageForTier(tier);
            if (stage != null)
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("ABY_ForgeAttunementEffectsHeader".Translate());
                if (stage.statOffsets != null && stage.statOffsets.Count > 0)
                {
                    for (int i = 0; i < stage.statOffsets.Count; i++)
                    {
                        StatModifier modifier = stage.statOffsets[i];
                        if (modifier?.stat == null)
                        {
                            continue;
                        }

                        lines.Add("• " + modifier.stat.LabelCap + ": " + FormatStatOffset(modifier.value));
                    }
                }
            }

            if (!active)
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("ABY_ForgeAttunementTooltipSuspended".Translate());
            }

            return string.Join("\n", lines.Where(line => line != null).ToArray());
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

        private static int CountAvailableForIngredient(Map map, IngredientCount ingredient)
        {
            if (map?.listerThings == null || ingredient?.filter == null)
            {
                return 0;
            }

            IEnumerable<ThingDef> allowedDefs = ingredient.filter.AllowedThingDefs;
            if (allowedDefs == null)
            {
                return 0;
            }

            int total = 0;
            foreach (ThingDef thingDef in allowedDefs)
            {
                if (thingDef == null)
                {
                    continue;
                }

                List<Thing> stacks = map.listerThings.ThingsOfDef(thingDef);
                if (stacks == null)
                {
                    continue;
                }

                for (int i = 0; i < stacks.Count; i++)
                {
                    Thing thing = stacks[i];
                    if (thing == null || thing.Destroyed || !thing.Spawned)
                    {
                        continue;
                    }

                    total += thing.stackCount;
                }
            }

            return total;
        }

        private static string GetIngredientLabel(IngredientCount ingredient)
        {
            ThingFilter filter = ingredient.filter;
            if (filter == null)
            {
                return "ingredient";
            }

            IEnumerable<ThingDef> allowedDefs = filter.AllowedThingDefs;
            List<ThingDef> allowed = allowedDefs != null ? allowedDefs.ToList() : new List<ThingDef>();
            if (allowed.Count == 1 && allowed[0] != null)
            {
                return allowed[0].label;
            }

            string summary = filter.Summary;
            if (!summary.NullOrEmpty())
            {
                return summary;
            }

            return "ingredient";
        }

        private static HediffStage GetAttunementStageForTier(int tier)
        {
            HediffDef hediffDef = AttunementHediffDef;
            if (hediffDef?.stages == null || hediffDef.stages.Count == 0)
            {
                return null;
            }

            float severity = tier <= 1 ? 1f : tier;
            HediffStage stage = hediffDef.stages[0];
            for (int i = 0; i < hediffDef.stages.Count; i++)
            {
                HediffStage candidate = hediffDef.stages[i];
                if (candidate != null && candidate.minSeverity <= severity)
                {
                    stage = candidate;
                }
            }

            return stage;
        }

        private static string FormatStatOffset(float value)
        {
            string prefix = value > 0f ? "+" : string.Empty;
            return prefix + value.ToStringPercent();
        }
    }
}
