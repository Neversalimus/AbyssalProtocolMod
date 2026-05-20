using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_EncounterShadowComparison
    {
        public string Context;
        public string PoolId;
        public float BaseBudget;
        public int BaseContentTier;
        public string LegacySummary;
        public string DirectedSummary;
        public int LegacyUnits;
        public int DirectedUnits;
        public float LegacyEstimatedBudget;
        public float DirectedBudget;
        public string TemplateDefName;
        public string DoctrineDefName;

        public string BuildLine()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("Encounter shadow ");
            builder.Append(Context.NullOrEmpty() ? "unknown" : Context);
            builder.Append(" pool=");
            builder.Append(PoolId ?? string.Empty);
            builder.Append(" tier=");
            builder.Append(BaseContentTier);
            builder.Append(" baseBudget=");
            builder.Append(BaseBudget.ToString("0.#"));
            builder.Append(" legacyUnits=");
            builder.Append(LegacyUnits);
            builder.Append(" directedUnits=");
            builder.Append(DirectedUnits);
            builder.Append(" legacyBudget~");
            builder.Append(LegacyEstimatedBudget.ToString("0.#"));
            builder.Append(" directedBudget=");
            builder.Append(DirectedBudget.ToString("0.#"));
            builder.Append(" template=");
            builder.Append(TemplateDefName ?? string.Empty);
            builder.Append(" doctrine=");
            builder.Append(DoctrineDefName ?? string.Empty);
            builder.Append(" | legacy: ");
            builder.Append(LegacySummary ?? "none");
            builder.Append(" | directed: ");
            builder.Append(DirectedSummary ?? "none");
            return builder.ToString();
        }
    }

    public static class ABY_EncounterShadowPlannerUtility
    {
        private const int ShadowThrottleTicks = 1800;

        public static bool ShadowPlanningEnabled => AbyssalProtocolMod.Settings?.enableEncounterShadowPlanning ?? false;

        public static void TryLogShadowPlanForLegacyPack(
            string context,
            string poolId,
            float baseBudget,
            int baseContentTier,
            Map map,
            List<AbyssalHostileSummonUtility.HostilePackEntry> legacyEntries,
            int? seed = null)
        {
            if (!ShadowPlanningEnabled)
            {
                return;
            }

            try
            {
                ABY_EncounterShadowComparison comparison = BuildComparison(context, poolId, baseBudget, baseContentTier, map, legacyEntries, seed);
                if (comparison == null)
                {
                    return;
                }

                ABY_LogThrottleUtility.Message("encounter-shadow-" + (context ?? string.Empty) + "-" + (poolId ?? string.Empty), "[Abyssal Protocol] " + comparison.BuildLine(), ShadowThrottleTicks);
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("encounter-shadow-failed", "[Abyssal Protocol] Encounter shadow planning failed: " + ex.GetType().Name + ": " + ex.Message, 6000);
            }
        }

        public static ABY_EncounterShadowComparison BuildComparison(
            string context,
            string poolId,
            float baseBudget,
            int baseContentTier,
            Map map,
            List<AbyssalHostileSummonUtility.HostilePackEntry> legacyEntries,
            int? seed = null)
        {
            if (poolId.NullOrEmpty())
            {
                return null;
            }

            int resolvedSeed = seed ?? BuildStableShadowSeed(context, poolId, baseBudget, baseContentTier, map);
            AbyssalEncounterDirectorUtility.EncounterPlan directed = AbyssalEncounterDirectorUtility.BuildPlan(poolId, Math.Max(1f, baseBudget), Math.Max(1, baseContentTier), map, resolvedSeed, null, null);
            return new ABY_EncounterShadowComparison
            {
                Context = context ?? string.Empty,
                PoolId = poolId ?? string.Empty,
                BaseBudget = Math.Max(1f, baseBudget),
                BaseContentTier = Math.Max(1, baseContentTier),
                LegacySummary = BuildLegacySummary(legacyEntries),
                DirectedSummary = directed != null ? directed.GetSummary() : "no directed plan",
                LegacyUnits = CountLegacyUnits(legacyEntries),
                DirectedUnits = directed?.TotalUnits ?? 0,
                LegacyEstimatedBudget = EstimateLegacyBudget(legacyEntries),
                DirectedBudget = directed?.Budget ?? 0f,
                TemplateDefName = directed?.TemplateDefName ?? string.Empty,
                DoctrineDefName = directed?.DoctrineDefName ?? string.Empty
            };
        }

        public static float EstimateLegacyBudget(List<AbyssalHostileSummonUtility.HostilePackEntry> entries)
        {
            float total = 0f;
            if (entries == null)
            {
                return total;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                AbyssalHostileSummonUtility.HostilePackEntry entry = entries[i];
                if (entry == null || entry.KindDef == null || entry.Count <= 0)
                {
                    continue;
                }

                DefModExtension_AbyssalDifficultyScaling scaling = entry.KindDef.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
                float cost = Math.Max(1f, scaling != null ? scaling.budgetCost : 100f);
                total += cost * Math.Max(0, entry.Count);
            }

            return total;
        }

        private static int BuildStableShadowSeed(string context, string poolId, float baseBudget, int baseContentTier, Map map)
        {
            int seed = 931777;
            seed = Gen.HashCombineInt(seed, map != null ? map.uniqueID : 0);
            seed = Gen.HashCombineInt(seed, Mathf.RoundToInt(Math.Max(1f, baseBudget)));
            seed = Gen.HashCombineInt(seed, Math.Max(1, baseContentTier));
            seed = Gen.HashCombineInt(seed, AbyssalDifficultyUtility.GetCurrentProfileOrder());
            seed = Gen.HashCombineInt(seed, AbyssalDifficultyUtility.GetProgressionStage(map));
            if (!context.NullOrEmpty())
            {
                seed = Gen.HashCombineInt(seed, context.GetHashCode());
            }

            if (!poolId.NullOrEmpty())
            {
                seed = Gen.HashCombineInt(seed, poolId.GetHashCode());
            }

            return seed;
        }

        private static int CountLegacyUnits(List<AbyssalHostileSummonUtility.HostilePackEntry> entries)
        {
            int total = 0;
            if (entries == null)
            {
                return total;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                AbyssalHostileSummonUtility.HostilePackEntry entry = entries[i];
                if (entry != null && entry.Count > 0)
                {
                    total += entry.Count;
                }
            }

            return total;
        }

        private static string BuildLegacySummary(List<AbyssalHostileSummonUtility.HostilePackEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "no legacy hostiles";
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                AbyssalHostileSummonUtility.HostilePackEntry entry = entries[i];
                if (entry == null || entry.KindDef == null || entry.Count <= 0)
                {
                    continue;
                }

                parts.Add(entry.Count + " " + (entry.KindDef.label ?? entry.KindDef.defName));
            }

            return parts.Count == 0 ? "no legacy hostiles" : string.Join(" + ", parts.ToArray());
        }
    }
}
