using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
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
            public int AllowedContentTier;
            public float Budget;
            public List<DirectedEntry> Entries = new List<DirectedEntry>();

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
            return BuildPlan(poolId, baseBudget, baseContentTier, null, null, null);
        }

        public static EncounterPlan BuildPlan(
            string poolId,
            float baseBudget,
            int baseContentTier,
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
                EncounterPlan plan = new EncounterPlan
                {
                    PoolId = poolId ?? string.Empty,
                    AllowedContentTier = AbyssalDifficultyUtility.GetAllowedContentTier(baseContentTier),
                    Budget = Math.Max(1f, baseBudget * AbyssalDifficultyUtility.GetEncounterBudgetMultiplier())
                };

                List<Candidate> candidates = GetCandidates(plan.PoolId, baseContentTier, plan.AllowedContentTier);
                if (candidates.Count == 0)
                {
                    return plan;
                }

                float remainingBudget = plan.Budget;
                ApplyRoleMinimums(plan, candidates, minimumRoleCounts, maximumRoleCounts, ref remainingBudget);

                int safety = 0;
                while (remainingBudget > 0.01f && safety < 128)
                {
                    safety++;
                    Candidate pick = TryPickCandidate(candidates, remainingBudget, maximumRoleCounts, plan);
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
                    Candidate pick = TryPickRoleCandidate(candidates, role, remainingBudget, maximumRoleCounts, plan);
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
            List<Candidate> candidates,
            string role,
            float remainingBudget,
            Dictionary<string, int> maximumRoleCounts,
            EncounterPlan plan)
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

                if (!CanAddCandidateToPlan(plan, candidate, maximumRoleCounts))
                {
                    continue;
                }

                float budgetCost = Math.Max(1f, candidate.Extension.budgetCost);
                if (budgetCost > remainingBudget && remainingBudget >= 1f)
                {
                    continue;
                }

                float score = candidate.EffectiveWeight / budgetCost;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static Candidate TryPickCandidate(
            List<Candidate> candidates,
            float remainingBudget,
            Dictionary<string, int> maximumRoleCounts,
            EncounterPlan plan)
        {
            float cheapestBudget = GetCheapestBudget(candidates, maximumRoleCounts, plan);
            List<Candidate> affordable = candidates
                .Where(c => c != null
                    && c.Extension != null
                    && CanAddCandidateToPlan(plan, c, maximumRoleCounts)
                    && (c.Extension.budgetCost <= remainingBudget || remainingBudget < cheapestBudget * 1.05f))
                .ToList();

            if (affordable.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < affordable.Count; i++)
            {
                totalWeight += affordable[i].EffectiveWeight;
            }

            float roll = Rand.Value * Math.Max(0.01f, totalWeight);
            for (int i = 0; i < affordable.Count; i++)
            {
                roll -= affordable[i].EffectiveWeight;
                if (roll <= 0f)
                {
                    return affordable[i];
                }
            }

            return affordable[affordable.Count - 1];
        }

        private static bool CanAddCandidateToPlan(EncounterPlan plan, Candidate candidate, Dictionary<string, int> maximumRoleCounts)
        {
            if (candidate == null || candidate.Extension == null)
            {
                return false;
            }

            if (maximumRoleCounts == null || maximumRoleCounts.Count == 0)
            {
                return true;
            }

            string role = candidate.Extension.role ?? string.Empty;
            foreach (KeyValuePair<string, int> pair in maximumRoleCounts)
            {
                if (!string.Equals(pair.Key ?? string.Empty, role, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return plan.GetRoleCount(role) < Math.Max(0, pair.Value);
            }

            return true;
        }

        private static float GetCheapestBudget(List<Candidate> candidates, Dictionary<string, int> maximumRoleCounts, EncounterPlan plan)
        {
            float cheapest = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                if (!CanAddCandidateToPlan(plan, candidate, maximumRoleCounts))
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
