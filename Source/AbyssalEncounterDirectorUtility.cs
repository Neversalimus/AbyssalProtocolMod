using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalEncounterDirectorUtility
    {
        public sealed class DirectedEntry
        {
            public PawnKindDef KindDef;
            public int Count;
            public float BudgetCost;
            public string Role;
        }

        public sealed class EncounterPlan
        {
            public string PoolId;
            public string TemplateDefName;
            public int AllowedContentTier;
            public float Budget;
            public List<DirectedEntry> Entries = new List<DirectedEntry>();

            public int TotalUnits
            {
                get
                {
                    int total = 0;
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        DirectedEntry entry = Entries[i];
                        if (entry != null && entry.Count > 0)
                        {
                            total += entry.Count;
                        }
                    }

                    return total;
                }
            }

            public int GetCount(string pawnKindDefName)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    DirectedEntry entry = Entries[i];
                    if (entry != null && entry.KindDef != null && string.Equals(entry.KindDef.defName, pawnKindDefName, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.Count;
                    }
                }

                return 0;
            }

            public int GetRoleCount(string role)
            {
                int count = 0;
                for (int i = 0; i < Entries.Count; i++)
                {
                    DirectedEntry entry = Entries[i];
                    if (entry == null || entry.Count <= 0)
                    {
                        continue;
                    }

                    if (string.Equals(entry.Role ?? string.Empty, role ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                    {
                        count += entry.Count;
                    }
                }

                return count;
            }

            public string GetSummary()
            {
                List<string> parts = new List<string>();
                for (int i = 0; i < Entries.Count; i++)
                {
                    DirectedEntry entry = Entries[i];
                    if (entry == null || entry.KindDef == null || entry.Count <= 0)
                    {
                        continue;
                    }

                    string label = entry.KindDef.label ?? entry.KindDef.defName;
                    parts.Add(entry.Count + " " + label);
                }

                return parts.Count == 0
                    ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CirclePreview_None", "no hostiles")
                    : string.Join(" + ", parts);
            }

            public List<AbyssalHostileSummonUtility.HostilePackEntry> ToHostilePackEntries()
            {
                List<AbyssalHostileSummonUtility.HostilePackEntry> entries = new List<AbyssalHostileSummonUtility.HostilePackEntry>();
                for (int i = 0; i < Entries.Count; i++)
                {
                    DirectedEntry entry = Entries[i];
                    if (entry == null || entry.KindDef == null || entry.Count <= 0)
                    {
                        continue;
                    }

                    entries.Add(new AbyssalHostileSummonUtility.HostilePackEntry
                    {
                        KindDef = entry.KindDef,
                        Count = entry.Count
                    });
                }

                return entries;
            }
        }

        private sealed class Candidate
        {
            public PawnKindDef KindDef;
            public DefModExtension_AbyssalDifficultyScaling Extension;
            public float EffectiveWeight;
        }

        public static EncounterPlan BuildPlan(string poolId, float baseBudget, int baseContentTier)
        {
            return BuildPlan(poolId, baseBudget, baseContentTier, null, null, null, null);
        }

        public static EncounterPlan BuildPlan(
            string poolId,
            float baseBudget,
            int baseContentTier,
            int? seed,
            Dictionary<string, int> minimumRoleCounts,
            Dictionary<string, int> maximumRoleCounts)
        {
            return BuildPlan(poolId, baseBudget, baseContentTier, null, seed, minimumRoleCounts, maximumRoleCounts);
        }

        public static EncounterPlan BuildPlan(
            string poolId,
            float baseBudget,
            int baseContentTier,
            Map map,
            int? seed,
            Dictionary<string, int> minimumRoleCounts,
            Dictionary<string, int> maximumRoleCounts)
        {
            if (seed.HasValue)
            {
                Rand.PushState(seed.Value);
            }

            try
            {
                ABY_EncounterTemplateDef template = ChooseTemplate(poolId, baseContentTier, map);
                EncounterPlan plan = new EncounterPlan
                {
                    PoolId = poolId ?? string.Empty,
                    TemplateDefName = template?.defName ?? string.Empty,
                    AllowedContentTier = GetAllowedTemplateTier(baseContentTier, template),
                    Budget = Math.Max(1f, baseBudget * AbyssalDifficultyUtility.GetEncounterBudgetMultiplier() * Math.Max(0.25f, template?.budgetMultiplier ?? 1f))
                };

                Dictionary<string, int> mergedMinimums = MergeRoleCounts(minimumRoleCounts, template?.minimumRoleCounts, true);
                Dictionary<string, int> mergedMaximums = MergeRoleCounts(maximumRoleCounts, template?.maximumRoleCounts, false);

                List<Candidate> candidates = GetCandidates(plan.PoolId, baseContentTier, plan.AllowedContentTier);
                if (candidates.Count == 0)
                {
                    return plan;
                }

                float remainingBudget = plan.Budget;
                ApplyRoleMinimums(plan, candidates, mergedMinimums, mergedMaximums, template, ref remainingBudget);

                int safety = 0;
                while (remainingBudget > 0.01f && safety < 128)
                {
                    safety++;
                    Candidate pick = TryPickCandidate(plan, candidates, remainingBudget, mergedMaximums, template);
                    if (pick == null)
                    {
                        break;
                    }

                    AddOrIncrement(plan, pick);
                    remainingBudget -= Math.Max(1f, pick.Extension != null ? pick.Extension.budgetCost : 100f);
                }

                return plan;
            }
            finally
            {
                if (seed.HasValue)
                {
                    Rand.PopState();
                }
            }
        }

        private static ABY_EncounterTemplateDef ChooseTemplate(string poolId, int baseContentTier, Map map)
        {
            List<ABY_EncounterTemplateDef> allDefs = DefDatabase<ABY_EncounterTemplateDef>.AllDefsListForReading;
            if (allDefs == null || allDefs.Count == 0)
            {
                return null;
            }

            List<ABY_EncounterTemplateDef> candidates = new List<ABY_EncounterTemplateDef>();
            ABY_DifficultyProfileDef profile = AbyssalDifficultyUtility.GetCurrentProfile();
            for (int i = 0; i < allDefs.Count; i++)
            {
                ABY_EncounterTemplateDef template = allDefs[i];
                if (template == null || !string.Equals(template.poolId ?? string.Empty, poolId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (baseContentTier < template.minBaseContentTier || baseContentTier > template.maxBaseContentTier)
                {
                    continue;
                }

                if (AbyssalDifficultyUtility.GetProfileOrder(profile) < AbyssalDifficultyUtility.GetProfileOrder(template.difficultyFloorDefName))
                {
                    continue;
                }

                candidates.Add(template);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            float[] weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                ABY_EncounterTemplateDef template = candidates[i];
                float weight = Math.Max(0.01f, template.selectionWeight);
                int templateHits = ABY_EncounterTelemetryUtility.GetRecentTemplateHits(poolId, template.defName, Math.Max(0, template.recentTemplateLookback));
                for (int hit = 0; hit < templateHits; hit++)
                {
                    weight *= Mathf.Clamp(template.recentTemplatePenalty, 0.15f, 1f);
                }

                if (template.reduceStackedSniperPressure && ABY_EncounterTelemetryUtility.HadRecentSniperPressure(poolId, Math.Max(1, template.recentTemplateLookback)))
                {
                    weight *= template.GetRoleWeightMultiplier("elite") > 1.15f ? 0.78f : 0.90f;
                }

                if (template.reduceStackedSupportPressure && ABY_EncounterTelemetryUtility.HadRecentSupportPressure(poolId, Math.Max(1, template.recentTemplateLookback)))
                {
                    weight *= template.GetRoleWeightMultiplier("support") > 1.10f ? 0.82f : 0.92f;
                }

                if (template.reduceStackedLargeWavePressure && ABY_EncounterTelemetryUtility.HadRecentLargeWavePressure(poolId, Math.Max(1, template.recentTemplateLookback)))
                {
                    weight *= template.budgetMultiplier > 1.02f ? 0.85f : 0.94f;
                }

                weights[i] = Math.Max(0.01f, weight);
                totalWeight += weights[i];
            }

            float roll = Rand.Value * Math.Max(0.01f, totalWeight);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                {
                    return candidates[i];
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static int GetAllowedTemplateTier(int baseContentTier, ABY_EncounterTemplateDef template)
        {
            int allowed = AbyssalDifficultyUtility.GetAllowedContentTier(baseContentTier);
            if (template != null)
            {
                allowed += Math.Max(0, template.extraContentTier);
            }

            return Math.Max(1, allowed);
        }

        private static Dictionary<string, int> MergeRoleCounts(Dictionary<string, int> baseCounts, List<ABY_EncounterTemplateRoleCount> templateCounts, bool useMaximum)
        {
            Dictionary<string, int> merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (baseCounts != null)
            {
                foreach (KeyValuePair<string, int> pair in baseCounts)
                {
                    merged[pair.Key ?? string.Empty] = Math.Max(0, pair.Value);
                }
            }

            if (templateCounts != null)
            {
                for (int i = 0; i < templateCounts.Count; i++)
                {
                    ABY_EncounterTemplateRoleCount entry = templateCounts[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    string role = entry.role ?? string.Empty;
                    int count = Math.Max(0, entry.count);
                    int existing;
                    if (!merged.TryGetValue(role, out existing))
                    {
                        merged[role] = count;
                    }
                    else
                    {
                        merged[role] = useMaximum ? Math.Max(existing, count) : Math.Min(existing, count);
                    }
                }
            }

            return merged;
        }

        private static List<Candidate> GetCandidates(string poolId, int baseContentTier, int allowedContentTier)
        {
            List<Candidate> result = new List<Candidate>();
            ABY_DifficultyProfileDef profile = AbyssalDifficultyUtility.GetCurrentProfile();
            List<PawnKindDef> defs = DefDatabase<PawnKindDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                PawnKindDef kindDef = defs[i];
                DefModExtension_AbyssalDifficultyScaling extension = kindDef != null ? kindDef.GetModExtension<DefModExtension_AbyssalDifficultyScaling>() : null;
                if (kindDef == null || extension == null)
                {
                    continue;
                }

                if (extension.encounterPools == null || !extension.encounterPools.Contains(poolId))
                {
                    continue;
                }

                if (!AbyssalDifficultyUtility.CanUseByDifficulty(extension, profile))
                {
                    continue;
                }

                if (extension.contentTier > allowedContentTier)
                {
                    continue;
                }

                if (!extension.allowFutureAutoEscalation && extension.contentTier > baseContentTier)
                {
                    continue;
                }

                float effectiveWeight = Math.Max(0.01f, extension.selectionWeight) * Math.Max(0.10f, AbyssalDifficultyUtility.GetRoleWeightMultiplier(extension.role));
                result.Add(new Candidate
                {
                    KindDef = kindDef,
                    Extension = extension,
                    EffectiveWeight = effectiveWeight
                });
            }

            return result;
        }

        private static void ApplyRoleMinimums(
            EncounterPlan plan,
            List<Candidate> candidates,
            Dictionary<string, int> minimumRoleCounts,
            Dictionary<string, int> maximumRoleCounts,
            ABY_EncounterTemplateDef template,
            ref float remainingBudget)
        {
            if (minimumRoleCounts == null || minimumRoleCounts.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, int> pair in minimumRoleCounts)
            {
                string role = pair.Key ?? string.Empty;
                int required = Math.Max(0, pair.Value);
                while (plan.GetRoleCount(role) < required && remainingBudget > 0.01f)
                {
                    Candidate pick = TryPickRoleCandidate(plan, candidates, role, remainingBudget, maximumRoleCounts, template);
                    if (pick == null)
                    {
                        break;
                    }

                    AddOrIncrement(plan, pick);
                    remainingBudget -= Math.Max(1f, pick.Extension != null ? pick.Extension.budgetCost : 100f);
                }
            }
        }

        private static Candidate TryPickRoleCandidate(
            EncounterPlan plan,
            List<Candidate> candidates,
            string role,
            float remainingBudget,
            Dictionary<string, int> maximumRoleCounts,
            ABY_EncounterTemplateDef template)
        {
            Candidate best = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                if (candidate == null || candidate.Extension == null)
                {
                    continue;
                }

                if (!string.Equals(candidate.Extension.role ?? string.Empty, role ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!CanAddCandidateToPlan(plan, candidate, maximumRoleCounts, template))
                {
                    continue;
                }

                float budgetCost = Math.Max(1f, candidate.Extension.budgetCost);
                if (budgetCost > remainingBudget && remainingBudget >= 1f)
                {
                    continue;
                }

                float score = GetDynamicCandidateWeight(plan, candidate, template) / budgetCost;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static Candidate TryPickCandidate(
            EncounterPlan plan,
            List<Candidate> candidates,
            float remainingBudget,
            Dictionary<string, int> maximumRoleCounts,
            ABY_EncounterTemplateDef template)
        {
            float cheapestBudget = GetCheapestBudget(candidates, maximumRoleCounts, plan, template);
            List<Candidate> affordable = new List<Candidate>();
            List<float> weights = new List<float>();
            float totalWeight = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                if (candidate == null || candidate.Extension == null)
                {
                    continue;
                }

                if (!CanAddCandidateToPlan(plan, candidate, maximumRoleCounts, template))
                {
                    continue;
                }

                float budgetCost = Math.Max(1f, candidate.Extension.budgetCost);
                if (!(budgetCost <= remainingBudget || remainingBudget < cheapestBudget * 1.05f))
                {
                    continue;
                }

                float weight = GetDynamicCandidateWeight(plan, candidate, template);
                if (weight <= 0.001f)
                {
                    continue;
                }

                affordable.Add(candidate);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (affordable.Count == 0)
            {
                return null;
            }

            float roll = Rand.Value * Math.Max(0.01f, totalWeight);
            for (int i = 0; i < affordable.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                {
                    return affordable[i];
                }
            }

            return affordable[affordable.Count - 1];
        }

        private static float GetDynamicCandidateWeight(EncounterPlan plan, Candidate candidate, ABY_EncounterTemplateDef template)
        {
            if (candidate == null || candidate.Extension == null)
            {
                return 0f;
            }

            float weight = Math.Max(0.01f, candidate.EffectiveWeight);
            weight *= Math.Max(0.10f, template?.GetRoleWeightMultiplier(candidate.Extension.role) ?? 1f);

            int recentKindHits = ABY_EncounterTelemetryUtility.GetRecentKindHits(plan.PoolId, candidate.KindDef?.defName, Math.Max(0, template?.recentKindLookback ?? 0));
            float kindPenalty = Mathf.Clamp(template?.recentKindPenalty ?? 0.75f, 0.15f, 1f);
            for (int i = 0; i < recentKindHits; i++)
            {
                weight *= kindPenalty;
            }

            string role = (candidate.Extension.role ?? string.Empty).ToLowerInvariant();
            if (role == "support")
            {
                if (plan.GetRoleCount("assault") <= 0 && plan.TotalUnits > 0)
                {
                    weight *= 0.72f;
                }

                if (template != null && template.reduceStackedSupportPressure && ABY_EncounterTelemetryUtility.HadRecentSupportPressure(plan.PoolId, Math.Max(1, template.recentKindLookback)))
                {
                    weight *= 0.78f;
                }
            }
            else if (role == "elite")
            {
                if (plan.GetRoleCount("elite") > 0 && plan.GetRoleCount("assault") <= 0)
                {
                    weight *= 0.76f;
                }

                if (candidate.KindDef != null && candidate.KindDef.defName == "ABY_RiftSniper" && template != null && template.reduceStackedSniperPressure && ABY_EncounterTelemetryUtility.HadRecentSniperPressure(plan.PoolId, Math.Max(1, template.recentKindLookback)))
                {
                    weight *= 0.55f;
                }
            }

            if (plan.TotalUnits >= 8 && role != "assault")
            {
                weight *= 0.84f;
            }

            return Math.Max(0.001f, weight);
        }

        private static bool CanAddCandidateToPlan(EncounterPlan plan, Candidate candidate, Dictionary<string, int> maximumRoleCounts, ABY_EncounterTemplateDef template)
        {
            if (candidate == null || candidate.Extension == null)
            {
                return false;
            }

            string role = candidate.Extension.role ?? string.Empty;
            if (maximumRoleCounts != null && maximumRoleCounts.Count > 0)
            {
                foreach (KeyValuePair<string, int> pair in maximumRoleCounts)
                {
                    if (!string.Equals(pair.Key ?? string.Empty, role, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (plan.GetRoleCount(role) >= Math.Max(0, pair.Value))
                    {
                        return false;
                    }

                    break;
                }
            }

            int currentKindCount = plan.GetCount(candidate.KindDef != null ? candidate.KindDef.defName : string.Empty);
            int extensionMax = candidate.Extension.maxPlanCount;
            if (extensionMax > 0 && currentKindCount >= extensionMax)
            {
                return false;
            }

            int templateMax = template != null ? Math.Max(1, template.maxSameKindCount) : 999;
            if (currentKindCount >= templateMax)
            {
                return false;
            }

            return true;
        }

        private static float GetCheapestBudget(List<Candidate> candidates, Dictionary<string, int> maximumRoleCounts, EncounterPlan plan, ABY_EncounterTemplateDef template)
        {
            float cheapest = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                if (!CanAddCandidateToPlan(plan, candidate, maximumRoleCounts, template))
                {
                    continue;
                }

                float cost = Math.Max(1f, candidate != null && candidate.Extension != null ? candidate.Extension.budgetCost : 100f);
                if (cost < cheapest)
                {
                    cheapest = cost;
                }
            }

            return cheapest == float.MaxValue ? 1f : cheapest;
        }

        private static void AddOrIncrement(EncounterPlan plan, Candidate candidate)
        {
            for (int i = 0; i < plan.Entries.Count; i++)
            {
                DirectedEntry existing = plan.Entries[i];
                if (existing != null && existing.KindDef == candidate.KindDef)
                {
                    existing.Count++;
                    return;
                }
            }

            plan.Entries.Add(new DirectedEntry
            {
                KindDef = candidate.KindDef,
                Count = 1,
                BudgetCost = Math.Max(1f, candidate.Extension != null ? candidate.Extension.budgetCost : 100f),
                Role = candidate.Extension != null ? candidate.Extension.role : "assault"
            });
        }
    }
}
