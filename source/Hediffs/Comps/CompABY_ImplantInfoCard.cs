using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_ImplantInfoCard : CompProperties
    {
        public CompProperties_ABY_ImplantInfoCard()
        {
            compClass = typeof(CompABY_ImplantInfoCard);
        }
    }

    public class CompABY_ImplantInfoCard : ThingComp
    {
        private static readonly Dictionary<string, HediffDef> CachedImplantHediffsByThingDefName = new Dictionary<string, HediffDef>();

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            List<StatDrawEntry> baseEntries = SafeCollectBaseEntries();
            for (int i = 0; i < baseEntries.Count; i++)
            {
                StatDrawEntry entry = baseEntries[i];
                if (entry != null)
                {
                    yield return entry;
                }
            }

            List<StatDrawEntry> customEntries = BuildCustomDisplayEntries();
            for (int i = 0; i < customEntries.Count; i++)
            {
                StatDrawEntry entry = customEntries[i];
                if (entry != null)
                {
                    yield return entry;
                }
            }
        }

        private List<StatDrawEntry> SafeCollectBaseEntries()
        {
            List<StatDrawEntry> result = new List<StatDrawEntry>();

            try
            {
                IEnumerable<StatDrawEntry> enumerable = base.SpecialDisplayStats();
                if (enumerable == null)
                {
                    return result;
                }

                foreach (StatDrawEntry entry in enumerable)
                {
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        private List<StatDrawEntry> BuildCustomDisplayEntries()
        {
            List<StatDrawEntry> result = new List<StatDrawEntry>();

            try
            {
                ThingDef implantThingDef = parent?.def;
                if (implantThingDef == null)
                {
                    return result;
                }

                HediffDef implantHediff = ResolveImplantHediff(implantThingDef);
                StatCategoryDef category = ResolveStatCategory();
                if (category == null)
                {
                    return result;
                }

                int displayOrder = 6200;

                string implantDescription = ResolveImplantDescription(implantThingDef, implantHediff);
                AddEntry(
                    result,
                    category,
                    ref displayOrder,
                    TranslateOrFallback("ABY_ImplantInfo_Profile", "Implant profile"),
                    implantDescription,
                    TranslateOrFallback("ABY_ImplantInfo_ProfileDesc", "Summary of what this implant is and how it alters the host."));

                if (implantHediff == null)
                {
                    return result;
                }

                if (implantHediff.addedPartProps != null && implantHediff.addedPartProps.partEfficiency > 0f)
                {
                    AddEntry(
                        result,
                        category,
                        ref displayOrder,
                        TranslateOrFallback("ABY_ImplantInfo_BodyPartEfficiency", "Body part efficiency"),
                        implantHediff.addedPartProps.partEfficiency.ToStringPercent(),
                        TranslateOrFallback("ABY_ImplantInfo_BodyPartEfficiencyDesc", "Efficiency of the replaced or added body part after installation."));
                }

                HediffStage stage = implantHediff.stages != null && implantHediff.stages.Count > 0
                    ? implantHediff.stages[0]
                    : null;

                if (stage == null)
                {
                    return result;
                }

                AppendCapacityEntries(result, category, stage, ref displayOrder);
                AppendStatOffsetEntries(result, category, stage, ref displayOrder);
                AppendStatFactorEntries(result, category, stage, ref displayOrder);

                if (Math.Abs(stage.totalBleedFactor - 1f) > 0.0001f)
                {
                    string bleedValue = FormatFactorDelta(stage.totalBleedFactor);
                    AddEntry(
                        result,
                        category,
                        ref displayOrder,
                        TranslateOrFallback("ABY_ImplantInfo_BleedingRate", "Bleeding rate"),
                        bleedValue,
                        TranslateOrFallback("ABY_ImplantInfo_BleedingRateDesc", "Changes total bleeding rate by {0} while the implant is installed.", bleedValue));
                }

                if (Math.Abs(stage.painFactor - 1f) > 0.0001f)
                {
                    string painValue = FormatFactorDelta(stage.painFactor);
                    AddEntry(
                        result,
                        category,
                        ref displayOrder,
                        TranslateOrFallback("ABY_ImplantInfo_Pain", "Pain"),
                        painValue,
                        TranslateOrFallback("ABY_ImplantInfo_PainDesc", "Changes felt pain by {0} while the implant is installed.", painValue));
                }

                if (Math.Abs(stage.hungerRateFactor - 1f) > 0.0001f)
                {
                    string hungerValue = FormatFactorDelta(stage.hungerRateFactor);
                    AddEntry(
                        result,
                        category,
                        ref displayOrder,
                        TranslateOrFallback("ABY_ImplantInfo_HungerRate", "Hunger rate"),
                        hungerValue,
                        TranslateOrFallback("ABY_ImplantInfo_HungerRateDesc", "Changes hunger rate by {0} while the implant is installed.", hungerValue));
                }
            }
            catch
            {
            }

            return result;
        }

        private void AppendCapacityEntries(List<StatDrawEntry> result, StatCategoryDef category, HediffStage stage, ref int displayOrder)
        {
            if (stage?.capMods == null)
            {
                return;
            }

            List<PawnCapacityModifier> modifiers = stage.capMods
                .Where(mod => mod != null && mod.capacity != null)
                .OrderBy(mod => mod.capacity.label ?? mod.capacity.defName)
                .ToList();

            for (int i = 0; i < modifiers.Count; i++)
            {
                PawnCapacityModifier capacityModifier = modifiers[i];
                if (Math.Abs(capacityModifier.offset) < 0.0001f)
                {
                    continue;
                }

                string capacityLabel = (capacityModifier.capacity.label ?? capacityModifier.capacity.defName ?? "Capacity").CapitalizeFirst();
                string offsetValue = FormatPercentOffset(capacityModifier.offset);
                AddEntry(
                    result,
                    category,
                    ref displayOrder,
                    capacityLabel,
                    offsetValue,
                    TranslateOrFallback("ABY_ImplantInfo_CapacityDesc", "Changes {0} by {1} when the implant is installed.", capacityLabel, offsetValue));
            }
        }

        private void AppendStatOffsetEntries(List<StatDrawEntry> result, StatCategoryDef category, HediffStage stage, ref int displayOrder)
        {
            if (stage?.statOffsets == null)
            {
                return;
            }

            List<StatModifier> modifiers = stage.statOffsets
                .Where(mod => mod != null && mod.stat != null)
                .OrderBy(mod => mod.stat.label ?? mod.stat.defName)
                .ToList();

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier statModifier = modifiers[i];
                if (Math.Abs(statModifier.value) < 0.0001f)
                {
                    continue;
                }

                string statLabel = statModifier.stat.LabelCap.NullOrEmpty()
                    ? (statModifier.stat.label ?? statModifier.stat.defName ?? "Stat").CapitalizeFirst()
                    : statModifier.stat.LabelCap;
                string offsetValue = FormatStatOffset(statModifier.stat, statModifier.value);
                AddEntry(
                    result,
                    category,
                    ref displayOrder,
                    statLabel,
                    offsetValue,
                    TranslateOrFallback("ABY_ImplantInfo_StatOffsetDesc", "Changes {0} by {1} when the implant is installed.", statLabel, offsetValue));
            }
        }

        private void AppendStatFactorEntries(List<StatDrawEntry> result, StatCategoryDef category, HediffStage stage, ref int displayOrder)
        {
            if (stage?.statFactors == null)
            {
                return;
            }

            List<StatModifier> modifiers = stage.statFactors
                .Where(mod => mod != null && mod.stat != null)
                .OrderBy(mod => mod.stat.label ?? mod.stat.defName)
                .ToList();

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier statModifier = modifiers[i];
                float delta = statModifier.value - 1f;
                if (Math.Abs(delta) < 0.0001f)
                {
                    continue;
                }

                string statLabel = statModifier.stat.LabelCap.NullOrEmpty()
                    ? (statModifier.stat.label ?? statModifier.stat.defName ?? "Stat").CapitalizeFirst()
                    : statModifier.stat.LabelCap;
                string factorValue = FormatStatFactor(statModifier.stat, statModifier.value);
                string factorLabel = TranslateOrFallback("ABY_ImplantInfo_StatFactorLabel", "{0} factor", statLabel);
                AddEntry(
                    result,
                    category,
                    ref displayOrder,
                    factorLabel,
                    factorValue,
                    TranslateOrFallback("ABY_ImplantInfo_StatFactorDesc", "Multiplies {0} by {1} when the implant is installed.", statLabel, statModifier.value.ToStringPercent()));
            }
        }

        private static void AddEntry(List<StatDrawEntry> result, StatCategoryDef category, ref int displayOrder, string label, string value, string description)
        {
            if (result == null || category == null || label.NullOrEmpty() || value.NullOrEmpty())
            {
                return;
            }

            result.Add(new StatDrawEntry(category, label, value, description ?? string.Empty, displayOrder++));
        }

        private static HediffDef ResolveImplantHediff(ThingDef implantThingDef)
        {
            if (implantThingDef == null || implantThingDef.defName.NullOrEmpty())
            {
                return null;
            }

            if (CachedImplantHediffsByThingDefName.TryGetValue(implantThingDef.defName, out HediffDef cached))
            {
                return cached;
            }

            HediffDef resolved = ResolveImplantHediffByRemovedThing(implantThingDef)
                                ?? ResolveImplantHediffByDefNameConvention(implantThingDef)
                                ?? ResolveImplantHediffBySurgeryRecipe(implantThingDef);

            CachedImplantHediffsByThingDefName[implantThingDef.defName] = resolved;
            return resolved;
        }

        private static HediffDef ResolveImplantHediffByRemovedThing(ThingDef implantThingDef)
        {
            List<HediffDef> allHediffs = DefDatabase<HediffDef>.AllDefsListForReading;
            for (int i = 0; i < allHediffs.Count; i++)
            {
                HediffDef candidate = allHediffs[i];
                if (candidate != null && candidate.spawnThingOnRemoved == implantThingDef)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static HediffDef ResolveImplantHediffByDefNameConvention(ThingDef implantThingDef)
        {
            string hediffDefName = implantThingDef.defName + "_Implant";
            return DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
        }

        private static HediffDef ResolveImplantHediffBySurgeryRecipe(ThingDef implantThingDef)
        {
            List<RecipeDef> allRecipes = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int i = 0; i < allRecipes.Count; i++)
            {
                RecipeDef recipe = allRecipes[i];
                if (recipe == null || recipe.addsHediff == null)
                {
                    continue;
                }

                ThingFilter filter = recipe.fixedIngredientFilter;
                if (filter != null && filter.Allows(implantThingDef))
                {
                    return recipe.addsHediff;
                }

                List<IngredientCount> costList = recipe.ingredients;
                if (costList == null)
                {
                    continue;
                }

                for (int j = 0; j < costList.Count; j++)
                {
                    IngredientCount cost = costList[j];
                    ThingFilter ingredientFilter = cost?.filter;
                    if (ingredientFilter != null && ingredientFilter.Allows(implantThingDef))
                    {
                        return recipe.addsHediff;
                    }
                }
            }

            return null;
        }

        private static string ResolveImplantDescription(ThingDef implantThingDef, HediffDef implantHediff)
        {
            string thingDescription = implantThingDef?.description;
            string hediffDescription = implantHediff?.description;

            if (!thingDescription.NullOrEmpty())
            {
                return thingDescription;
            }

            if (!hediffDescription.NullOrEmpty())
            {
                return hediffDescription;
            }

            string recipeDescription = ResolveImplantRecipeDescription(implantThingDef, implantHediff);
            if (!recipeDescription.NullOrEmpty())
            {
                return recipeDescription;
            }

            return BuildFallbackImplantSummary(implantThingDef, implantHediff);
        }

        private static string ResolveImplantRecipeDescription(ThingDef implantThingDef, HediffDef implantHediff)
        {
            if (implantThingDef == null)
            {
                return null;
            }

            List<RecipeDef> allRecipes = DefDatabase<RecipeDef>.AllDefsListForReading;
            for (int i = 0; i < allRecipes.Count; i++)
            {
                RecipeDef recipe = allRecipes[i];
                if (recipe == null)
                {
                    continue;
                }

                if (RecipeUsesImplantThing(recipe, implantThingDef) || (implantHediff != null && recipe.addsHediff == implantHediff))
                {
                    if (!recipe.description.NullOrEmpty())
                    {
                        return recipe.description;
                    }
                }
            }

            return null;
        }

        private static bool RecipeUsesImplantThing(RecipeDef recipe, ThingDef implantThingDef)
        {
            if (recipe == null || implantThingDef == null)
            {
                return false;
            }

            ThingFilter fixedFilter = recipe.fixedIngredientFilter;
            if (fixedFilter != null && fixedFilter.Allows(implantThingDef))
            {
                return true;
            }

            List<IngredientCount> ingredients = recipe.ingredients;
            if (ingredients == null)
            {
                return false;
            }

            for (int i = 0; i < ingredients.Count; i++)
            {
                ThingFilter ingredientFilter = ingredients[i]?.filter;
                if (ingredientFilter != null && ingredientFilter.Allows(implantThingDef))
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildFallbackImplantSummary(ThingDef implantThingDef, HediffDef implantHediff)
        {
            string label = implantThingDef?.label ?? implantHediff?.label;
            if (label.NullOrEmpty())
            {
                return null;
            }

            if (implantHediff?.addedPartProps != null)
            {
                return TranslateOrFallback("ABY_ImplantInfo_FallbackSummary", "A specialized abyssal implant that permanently alters the host through {0} integration.", label);
            }

            return TranslateOrFallback("ABY_ImplantInfo_FallbackSummaryGeneric", "A specialized abyssal implant built for disciplined host integration.");
        }

        private static StatCategoryDef ResolveStatCategory()
        {
            return DefDatabase<StatCategoryDef>.GetNamedSilentFail("Basics")
                   ?? DefDatabase<StatCategoryDef>.AllDefsListForReading.FirstOrDefault(def => def != null);
        }

        private static string FormatStatOffset(StatDef statDef, float value)
        {
            if (statDef != null)
            {
                return value.ToStringByStyle(statDef.toStringStyle, ToStringNumberSense.Offset);
            }

            return FormatSignedFloat(value);
        }

        private static string FormatStatFactor(StatDef statDef, float factor)
        {
            float delta = factor - 1f;
            if (statDef != null)
            {
                return delta.ToStringByStyle(statDef.toStringStyle, ToStringNumberSense.Offset);
            }

            return FormatSignedPercent(delta);
        }

        private static string FormatFactorDelta(float factor)
        {
            return FormatSignedPercent(factor - 1f);
        }

        private static string FormatPercentOffset(float value)
        {
            return FormatSignedPercent(value);
        }

        private static string FormatSignedFloat(float value)
        {
            if (value > 0f)
            {
                return "+" + value.ToString("0.##");
            }

            return value.ToString("0.##");
        }

        private static string FormatSignedPercent(float value)
        {
            string text = value.ToStringPercent();
            if (value > 0f && !text.StartsWith("+"))
            {
                text = "+" + text;
            }

            return text;
        }

        private static string TranslateOrFallback(string key, string fallback)
        {
            TaggedString translated = key.Translate();
            return translated.RawText == key ? fallback : translated.Resolve();
        }

        private static string TranslateOrFallback(string key, string fallbackFormat, params object[] args)
        {
            TaggedString translated = key.Translate();
            string template = translated.RawText == key ? fallbackFormat : translated.Resolve();

            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }
    }
}
