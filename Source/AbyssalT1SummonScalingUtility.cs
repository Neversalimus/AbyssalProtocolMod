using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalT1SummonScalingUtility
    {
        private const string UnstableBreachRitualId = "unstable_breach";
        private const string EmberHuntRitualId = "ember_hunt";
        private const string ChoirEngineRitualId = "choir_engine";

        private const int ImpThreatValue = 85;
        private const int HoundThreatValue = 190;
        private const int ThrallThreatValue = 160;
        private const int ZealotThreatValue = 235;

        public sealed class ThreatPlan
        {
            public string RitualId;
            public int Tier;
            public int ColonistTier;
            public int WealthTier;
            public int ThreatBudget;
            public int PortalImpCount;
            public int PackImpCount;
            public int HoundCount;
            public int ThrallCount;
            public int ZealotCount;

            public int TotalImpCount => Math.Max(0, PortalImpCount) + Math.Max(0, PackImpCount);
            public int TotalEscortCount => Math.Max(0, HoundCount) + TotalImpCount + Math.Max(0, ThrallCount) + Math.Max(0, ZealotCount);
        }

        public static bool IsSupportedRitual(string ritualId)
        {
            return string.Equals(ritualId, UnstableBreachRitualId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ritualId, EmberHuntRitualId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ritualId, ChoirEngineRitualId, StringComparison.OrdinalIgnoreCase);
        }

        public static int GetActiveColonistCount(Map map)
        {
            return Math.Max(1, map?.mapPawns?.FreeColonistsSpawnedCount ?? 0);
        }

        public static ThreatPlan GetThreatPlan(Map map, string ritualId)
        {
            if (!IsSupportedRitual(ritualId))
            {
                return null;
            }

            int colonistTier = GetColonistTier(map);
            int wealthTier = GetWealthTier(map);

            ThreatPlan plan = new ThreatPlan
            {
                RitualId = ritualId,
                ColonistTier = colonistTier,
                WealthTier = wealthTier,
                Tier = Math.Max(colonistTier, wealthTier)
            };

            if (string.Equals(ritualId, UnstableBreachRitualId, StringComparison.OrdinalIgnoreCase))
            {
                ApplyUnstableBreachPlan(map, plan);
            }
            else if (string.Equals(ritualId, EmberHuntRitualId, StringComparison.OrdinalIgnoreCase))
            {
                ApplyEmberHuntPreviewPlan(map, plan);
            }
            else
            {
                ApplyChoirEnginePlan(map, plan);
            }

            return plan;
        }

        public static int GetScaledThreatBudget(Map map, string ritualId, int fallback)
        {
            ThreatPlan plan = GetThreatPlan(map, ritualId);
            return plan?.ThreatBudget ?? fallback;
        }

        public static string GetThreatTierLabel(int tier)
        {
            int safeTier = Math.Max(0, tier);
            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleThreatTier", "T{0}", safeTier);
        }

        public static string GetPreviewText(ThreatPlan plan)
        {
            if (plan == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            if (plan.HoundCount > 0)
            {
                parts.Add(GetCountLabel(plan.HoundCount, "ABY_CirclePreview_Hound_Singular", "hound", "ABY_CirclePreview_Hound_Plural", "hounds"));
            }

            int totalImps = plan.TotalImpCount;
            if (totalImps > 0)
            {
                parts.Add(GetCountLabel(totalImps, "ABY_CirclePreview_Imp_Singular", "imp", "ABY_CirclePreview_Imp_Plural", "imps"));
            }

            if (plan.ThrallCount > 0)
            {
                parts.Add(GetCountLabel(plan.ThrallCount, "ABY_CirclePreview_Thrall_Singular", "thrall", "ABY_CirclePreview_Thrall_Plural", "thralls"));
            }

            if (plan.ZealotCount > 0)
            {
                parts.Add(GetCountLabel(plan.ZealotCount, "ABY_CirclePreview_Zealot_Singular", "zealot", "ABY_CirclePreview_Zealot_Plural", "zealots"));
            }

            return parts.Count == 0
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CirclePreview_None", "no hostiles")
                : string.Join(" + ", parts);
        }

        private static int GetColonistTier(Map map)
        {
            int colonists = GetActiveColonistCount(map);
            if (colonists <= 4)
            {
                return 0;
            }

            if (colonists <= 8)
            {
                return 1;
            }

            if (colonists <= 13)
            {
                return 2;
            }

            if (colonists <= 18)
            {
                return 3;
            }

            return 4;
        }

        private static int GetWealthTier(Map map)
        {
            float wealth = map?.wealthWatcher?.WealthTotal ?? 0f;
            if (wealth <= 90000f)
            {
                return 0;
            }

            if (wealth <= 180000f)
            {
                return 1;
            }

            if (wealth <= 320000f)
            {
                return 2;
            }

            if (wealth <= 550000f)
            {
                return 3;
            }

            return 4;
        }

        private static void ApplyUnstableBreachPlan(Map map, ThreatPlan plan)
        {
            int colonists = GetActiveColonistCount(map);
            plan.PortalImpCount = Math.Min(60, Math.Max(3, colonists * 3));
            plan.PackImpCount = 0;
            plan.HoundCount = 0;
            plan.ThrallCount = 0;
            plan.ZealotCount = 0;
            plan.ThreatBudget = plan.PortalImpCount * ImpThreatValue;
        }

        private static void ApplyEmberHuntPreviewPlan(Map map, ThreatPlan plan)
        {
            int colonists = GetActiveColonistCount(map);
            int minCount = Math.Max(1, colonists);
            int maxCount = Math.Min(25, Math.Max(minCount, colonists * 3));
            plan.HoundCount = Math.Max(1, (minCount + maxCount) / 2);
            plan.PortalImpCount = 0;
            plan.PackImpCount = 0;
            plan.ThrallCount = 0;
            plan.ZealotCount = 0;
            plan.ThreatBudget = plan.HoundCount * HoundThreatValue;
        }

        private static void ApplyChoirEnginePlan(Map map, ThreatPlan plan)
        {
            int colonists = GetActiveColonistCount(map);
            int totalEscort = Math.Min(30, Math.Max(6, colonists * 6));

            plan.PortalImpCount = 0;
            plan.PackImpCount = totalEscort / 2;
            plan.ThrallCount = totalEscort / 3;
            plan.ZealotCount = Math.Max(0, totalEscort - plan.PackImpCount - plan.ThrallCount);
            plan.HoundCount = 0;
            plan.ThreatBudget = plan.TotalImpCount * ImpThreatValue
                + plan.ThrallCount * ThrallThreatValue
                + plan.ZealotCount * ZealotThreatValue;
        }

        private static string GetCountLabel(int count, string singularKey, string singularFallback, string pluralKey, string pluralFallback)
        {
            if (count == 1)
            {
                return count + " " + AbyssalSummoningConsoleUtility.TranslateOrFallback(singularKey, singularFallback);
            }

            return count + " " + AbyssalSummoningConsoleUtility.TranslateOrFallback(pluralKey, pluralFallback);
        }
    }
}
