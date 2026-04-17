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
        public const int MaxAttunementTier = 50;
        public const int MaxAttunementResidue = 150000;
        public const int ResiduePerAttunementTier = 3000;

        public const string AllCategory = "All";
        public const string CoreCategory = "Core";
        public const string WeaponsCategory = "Weapons";
        public const string ArmorCategory = "Armor";
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
            ArmorCategory,
            ImplantsCategory,
            RitualCategory,
            HeraldCategory
        };

        public static IEnumerable<string> Categories => CategoryOrder;

        public static ThingDef ForgeDef => DefDatabase<ThingDef>.GetNamedSilentFail(ForgeDefName);
        public static ThingDef ResidueDef => DefDatabase<ThingDef>.GetNamedSilentFail(ResidueDefName);
        public static HediffDef AttunementHediffDef => DefDatabase<HediffDef>.GetNamedSilentFail(AttunementHediffDefName);

        private static string TranslateOrFallback(string key, string fallback)
        {
            string value = key.Translate();
            return value == key ? fallback : value;
        }

        private static string TranslateOrFallback(string key, string fallbackFormat, params object[] args)
        {
            string template = key.Translate();
            if (template == key)
            {
                return string.Format(fallbackFormat, args);
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        public static int GetAttunementTierForResidue(int totalResidueOffered)
        {
            if (totalResidueOffered <= 0)
            {
                return 1;
            }

            float ratio = Mathf.Clamp01(totalResidueOffered / (float)MaxAttunementResidue);
            int tier = 1 + Mathf.FloorToInt(ratio * (MaxAttunementTier - 1));
            return Mathf.Clamp(tier, 1, MaxAttunementTier);
        }

        public static int GetResidueRequiredForAttunementTier(int tier)
        {
            if (tier <= 1)
            {
                return 0;
            }

            float ratio = (tier - 1) / (float)(MaxAttunementTier - 1);
            return Mathf.Clamp(Mathf.CeilToInt(ratio * MaxAttunementResidue), 0, MaxAttunementResidue);
        }

        public static int GetNextAttunementTierResidue(int currentTier)
        {
            if (currentTier >= MaxAttunementTier)
            {
                return -1;
            }

            return GetResidueRequiredForAttunementTier(currentTier + 1);
        }

        public static float GetAttunementLevelFill(int tier)
        {
            if (tier <= 0)
            {
                return 0f;
            }

            float normalized = Mathf.Clamp01((tier - 1f) / (MaxAttunementTier - 1f));
            return Mathf.Lerp(0.04f, 1f, normalized);
        }

        public static float GetSummoningInstabilityReductionForTier(int tier)
        {
            if (tier <= 0)
            {
                return 0f;
            }

            return 0.09f * Mathf.Clamp01(tier / (float)MaxAttunementTier);
        }

        public static float GetSummoningInstabilityReduction(Map map)
        {
            MapComponent_AbyssalForgeProgress progress = map?.GetComponent<MapComponent_AbyssalForgeProgress>();
            return progress != null ? GetSummoningInstabilityReductionForTier(progress.GetCurrentAttunementTier(false)) : 0f;
        }

        public static float GetForgeBillSpeedCapstoneBonusForTier(int tier)
        {
            return tier >= MaxAttunementTier ? 0.50f : 0f;
        }

        public static string GetAttunementBandLabel(int tier)
        {
            if (tier <= 0)
            {
                return TranslateOrFallback("ABY_ForgeAttunementBand_Dormant", "dormant lattice");
            }

            if (tier >= MaxAttunementTier)
            {
                return TranslateOrFallback("ABY_ForgeAttunementBand_Dominion", "dominion attunement");
            }

            if (tier >= 40)
            {
                return TranslateOrFallback("ABY_ForgeAttunementBand_Ascendant", "ascendant attunement");
            }

            if (tier >= 30)
            {
                return TranslateOrFallback("ABY_ForgeAttunementBand_Crowned", "crowned attunement");
            }

            if (tier >= 20)
            {
                return TranslateOrFallback("ABY_ForgeAttunementBand_Deep", "deep attunement");
            }

            if (tier >= 10)
            {
                return TranslateOrFallback("ABY_ForgeAttunementBand_Stable", "stable attunement");
            }

            return TranslateOrFallback("ABY_ForgeAttunementBand_Faint", "faint attunement");
        }

        public static string GetAttunementMetricLabel(int tier)
        {
            return TranslateOrFallback("ABY_ForgeAttunementMetricLabel", "Tier {0}/50", tier, MaxAttunementTier);
        }

        public static string GetAttunementBarLabel(int tier)
        {
            return TranslateOrFallback("ABY_ForgeAttunementLevelBar", "Attunement level • tier {0}/50", tier, MaxAttunementTier);
        }

        public static string GetAttunementDisplayLabel(int tier)
        {
            return TranslateOrFallback("ABY_ForgeAttunementDisplayLabel", "{0} · tier {1}/50", GetAttunementBandLabel(tier), tier, MaxAttunementTier);
        }

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

        public static string GetAttunementTooltip(int tier, int totalResidueOffered, bool active)
        {
            List<string> lines = new List<string>
            {
                GetAttunementDisplayLabel(tier),
                TranslateOrFallback("ABY_ForgeAttunementResidueProgress", "Residue attuned: {0}/{1}", totalResidueOffered, MaxAttunementResidue)
            };

            int nextTierResidue = GetNextAttunementTierResidue(tier);
            if (nextTierResidue > 0)
            {
                lines.Add(TranslateOrFallback("ABY_ForgeAttunementNextTier", "Next tier at {0} residue", nextTierResidue));
            }
            else
            {
                lines.Add(TranslateOrFallback("ABY_ForgeAttunementMaxTier", "Maximum attunement reached"));
            }

            HediffDef hediffDef = AttunementHediffDef;
            if (hediffDef != null && !hediffDef.description.NullOrEmpty())
            {
                lines.Add(string.Empty);
                lines.Add(hediffDef.description);
            }

            HediffStage stage = GetAttunementStageForTier(tier);
            if (stage != null)
            {
                lines.Add(string.Empty);
                lines.Add(TranslateOrFallback("ABY_ForgeAttunementEffectsHeader", "Current effects"));

                if (stage.statOffsets != null && stage.statOffsets.Count > 0)
                {
                    for (int i = 0; i < stage.statOffsets.Count; i++)
                    {
                        StatModifier modifier = stage.statOffsets[i];
                        if (modifier?.stat == null)
                        {
                            continue;
                        }

                        lines.Add("• " + modifier.stat.LabelCap + ": " + FormatStatOffset(modifier.stat, modifier.value));
                    }
                }

                if (stage.capMods != null && stage.capMods.Count > 0)
                {
                    for (int i = 0; i < stage.capMods.Count; i++)
                    {
                        PawnCapacityModifier modifier = stage.capMods[i];
                        if (modifier?.capacity == null)
                        {
                            continue;
                        }

                        lines.Add("• " + modifier.capacity.label.CapitalizeFirst() + ": " + FormatCapacityOffset(modifier.capacity, modifier.offset));
                    }
                }
            }

            float instabilityReduction = GetSummoningInstabilityReductionForTier(tier);
            if (instabilityReduction > 0f)
            {
                lines.Add("• " + TranslateOrFallback("ABY_ForgeAttunementInstability", "Summoning instability") + ": -" + instabilityReduction.ToStringPercent());
            }

            if (!active)
            {
                lines.Add(string.Empty);
                lines.Add(TranslateOrFallback("ABY_ForgeAttunementTooltipSuspended", "Suspended until at least one Abyssal Forge on this map is powered."));
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

            float severity = tier <= 0 ? 0f : tier;
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

        private static string FormatStatOffset(StatDef stat, float value)
        {
            string prefix = value > 0f ? "+" : string.Empty;
            if (stat == null)
            {
                return prefix + value.ToString("0.##");
            }

            switch (stat.defName)
            {
                case "WorkSpeedGlobal":
                case "SmithingSpeed":
                case "CraftingSpeed":
                case "ConstructionSpeed":
                case "ShootingAccuracyPawn":
                case "MeleeHitChance":
                case "ResearchSpeed":
                case "MedicalTendQuality":
                case "ImmunityGainSpeed":
                case "GeneralLaborSpeed":
                    return prefix + value.ToStringPercent();
                case "ComfyTemperatureMax":
                case "ComfyTemperatureMin":
                    return prefix + value.ToString("0.#") + " C";
                case "CarryingCapacity":
                case "MoveSpeed":
                    return prefix + value.ToString("0.##");
                default:
                    return prefix + value.ToString("0.##");
            }
        }

        private static string FormatCapacityOffset(PawnCapacityDef capacity, float value)
        {
            string prefix = value > 0f ? "+" : string.Empty;
            if (capacity == null)
            {
                return prefix + value.ToString("0.##");
            }

            switch (capacity.defName)
            {
                case "Manipulation":
                    return prefix + value.ToStringPercent();
                default:
                    return prefix + value.ToStringPercent();
            }
        }
    }
}
