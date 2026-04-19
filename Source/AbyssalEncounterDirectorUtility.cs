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
            int safety = 0;
            while (remainingBudget > 0.01f && safety < 128)
            {
                safety++;
                Candidate pick = TryPickCandidate(candidates, remainingBudget);
                if (pick == null)
                {
                    break;
                }

                AddOrIncrement(plan, pick);
                remainingBudget -= Math.Max(1f, pick.Extension != null ? pick.Extension.budgetCost : 100f);
            }

            return plan;
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

        private static Candidate TryPickCandidate(List<Candidate> candidates, float remainingBudget)
        {
            float cheapestBudget = GetCheapestBudget(candidates);
            List<Candidate> affordable = candidates
                .Where(c => c != null && c.Extension != null && (c.Extension.budgetCost <= remainingBudget || remainingBudget < cheapestBudget * 1.05f))
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

        private static float GetCheapestBudget(List<Candidate> candidates)
        {
            float cheapest = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                float cost = Math.Max(1f, candidates[i] != null && candidates[i].Extension != null ? candidates[i].Extension.budgetCost : 100f);
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
