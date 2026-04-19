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
            foreach (StatDrawEntry entry in base.SpecialDisplayStats())
            {
                yield return entry;
            }

            ThingDef implantThingDef = parent != null ? parent.def : null;
            if (implantThingDef == null)
            {
                yield break;
            }

            HediffDef implantHediff = ResolveImplantHediff(implantThingDef);

            StatCategoryDef category = ResolveStatCategory();
            int displayOrder = 6200;

            string implantDescription = ResolveImplantDescription(implantThingDef, implantHediff);
            if (!implantDescription.NullOrEmpty())
            {
                yield return MakeEntry(
                    category,
                    displayOrder++,
                    TranslateOrFallback("ABY_ImplantInfo_Profile", "Implant profile"),
                    implantDescription,
                    TranslateOrFallback("ABY_ImplantInfo_ProfileDesc", "Summary of what this implant is and how it alters the host."));
            }

            if (implantHediff == null)
            {
                yield break;
            }

            if (implantHediff.addedPartProps != null && implantHediff.addedPartProps.partEfficiency > 0f)
            {
                yield return MakeEntry(
                    category,
                    displayOrder++,
                    TranslateOrFallback("ABY_ImplantInfo_BodyPartEfficiency", "Body part efficiency"),
                    implantHediff.addedPartProps.partEfficiency.ToStringPercent(),
                    TranslateOrFallback("ABY_ImplantInfo_BodyPartEfficiencyDesc", "Efficiency of the replaced or added body part after installation."));
            }

            HediffStage stage = implantHediff.stages != null && implantHediff.stages.Count > 0
                ? implantHediff.stages[0]
                : null;

            if (stage == null)
            {
                yield break;
            }

            if (stage.capMods != null)
            {
                foreach (PawnCapacityModifier capacityModifier in stage.capMods.Where(mod => mod != null && mod.capacity != null).OrderBy(mod => mod.capacity.label))
                {
                    if (Math.Abs(capacityModifier.offset) < 0.0001f)
                    {
                        continue;
                    }

                    string capacityLabel = capacityModifier.capacity.label.CapitalizeFirst();
                    string offsetValue = FormatPercentOffset(capacityModifier.offset);
                    yield return MakeEntry(
                        category,
                        displayOrder++,
                        capacityLabel,
                        offsetValue,
                        TranslateOrFallback("ABY_ImplantInfo_CapacityDesc", "Changes {0} by {1} when the implant is installed.", capacityLabel, offsetValue));
                }
            }

            if (stage.statOffsets != null)
            {
                foreach (StatModifier statModifier in stage.statOffsets.Where(mod => mod != null && mod.stat != null).OrderBy(mod => mod.stat.label))
                {
                    if (Math.Abs(statModifier.value) < 0.0001f)
                    {
                        continue;
                    }

                    string statLabel = statModifier.stat.LabelCap;
                    string offsetValue = FormatStatOffset(statModifier.stat, statModifier.value);
                    yield return MakeEntry(
                        category,
                        displayOrder++,
                        statLabel,
                        offsetValue,
                        TranslateOrFallback("ABY_ImplantInfo_StatOffsetDesc", "Changes {0} by {1} when the implant is installed.", statLabel, offsetValue));
                }
            }

            if (stage.statFactors != null)
            {
                foreach (StatModifier statModifier in stage.statFactors.Where(mod => mod != null && mod.stat != null).OrderBy(mod => mod.stat.label))
                {
                    float delta = statModifier.value - 1f;
                    if (Math.Abs(delta) < 0.0001f)
                    {
                        continue;
                    }

                    string statLabel = statModifier.stat.LabelCap;
                    string factorValue = FormatStatFactor(statModifier.stat, statModifier.value);
                    string factorLabel = TranslateOrFallback("ABY_ImplantInfo_StatFactorLabel", "{0} factor", statLabel);
                    yield return MakeEntry(
                        category,
                        displayOrder++,
                        factorLabel,
                        factorValue,
                        TranslateOrFallback("ABY_ImplantInfo_StatFactorDesc", "Multiplies {0} by {1} when the implant is installed.", statLabel, statModifier.value.ToStringPercent()));
                }
            }

            if (Math.Abs(stage.totalBleedFactor - 1f) > 0.0001f)
            {
                string bleedValue = FormatFactorDelta(stage.totalBleedFactor);
                yield return MakeEntry(
                    category,
                    displayOrder++,
                    TranslateOrFallback("ABY_ImplantInfo_BleedingRate", "Bleeding rate"),
                    bleedValue,
                    TranslateOrFallback("ABY_ImplantInfo_BleedingRateDesc", "Changes total bleeding rate by {0} while the implant is installed.", bleedValue));
            }

            if (Math.Abs(stage.painFactor - 1f) > 0.0001f)
            {
                string painValue = FormatFactorDelta(stage.painFactor);
                yield return MakeEntry(
                    category,
                    displayOrder++,
                    TranslateOrFallback("ABY_ImplantInfo_Pain", "Pain"),
                    painValue,
                    TranslateOrFallback("ABY_ImplantInfo_PainDesc", "Changes felt pain by {0} while the implant is installed.", painValue));
            }

            if (Math.Abs(stage.hungerRateFactor - 1f) > 0.0001f)
            {
                string hungerValue = FormatFactorDelta(stage.hungerRateFactor);
                yield return MakeEntry(
                    category,
                    displayOrder++,
                    TranslateOrFallback("ABY_ImplantInfo_HungerRate", "Hunger rate"),
                    hungerValue,
                    TranslateOrFallback("ABY_ImplantInfo_HungerRateDesc", "Changes hunger rate by {0} while the implant is installed.", hungerValue));
            }
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

        private static StatDrawEntry MakeEntry(StatCategoryDef category, int displayOrder, string label, string value, string description)
        {
            return new StatDrawEntry(category, label, value, description, displayOrder);
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
                   ?? DefDatabase<StatCategoryDef>.AllDefsListForReading.FirstOrDefault();
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
