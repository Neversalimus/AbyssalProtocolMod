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
        }

        public static bool IsSupportedRitual(string ritualId)
        {
            return string.Equals(ritualId, UnstableBreachRitualId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ritualId, EmberHuntRitualId, StringComparison.OrdinalIgnoreCase);
        }

        public static ThreatPlan GetThreatPlan(Map map, string ritualId)
        {
            if (!IsSupportedRitual(ritualId))
            {
                return null;
            }

            int colonistTier = GetColonistTier(map);
            int wealthTier = GetWealthTier(map);
            int finalTier = Math.Max(colonistTier, wealthTier);

            ThreatPlan plan = new ThreatPlan
            {
                RitualId = ritualId,
                ColonistTier = colonistTier,
                WealthTier = wealthTier,
                Tier = finalTier
            };

            if (string.Equals(ritualId, UnstableBreachRitualId, StringComparison.OrdinalIgnoreCase))
            {
                ApplyUnstableBreachPlan(plan);
            }
            else
            {
                ApplyEmberHuntPlan(plan);
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
            int colonists = map?.mapPawns?.FreeColonistsSpawnedCount ?? 0;
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

        private static void ApplyUnstableBreachPlan(ThreatPlan plan)
        {
            int[] portalImpCounts = { 2, 3, 4, 5, 6 };
            int tier = Math.Min(plan.Tier, portalImpCounts.Length - 1);
            plan.PortalImpCount = portalImpCounts[tier];
            plan.ThrallCount = plan.Tier >= 4 ? 1 : 0;
            plan.ZealotCount = plan.Tier >= 4 ? 1 : 0;
            plan.ThreatBudget = plan.PortalImpCount * ImpThreatValue + plan.ThrallCount * ThrallThreatValue + plan.ZealotCount * ZealotThreatValue;
        }

        private static void ApplyEmberHuntPlan(ThreatPlan plan)
        {
            switch (plan.Tier)
            {
                case 0:
                    plan.HoundCount = 1;
                    plan.PackImpCount = 0;
                    plan.ThrallCount = 0;
                    plan.ZealotCount = 0;
                    break;
                case 1:
                    plan.HoundCount = 1;
                    plan.PackImpCount = 1;
                    plan.ThrallCount = 0;
                    plan.ZealotCount = 0;
                    break;
                case 2:
                    plan.HoundCount = 1;
                    plan.PackImpCount = 2;
                    plan.ThrallCount = 0;
                    plan.ZealotCount = 0;
                    break;
                case 3:
                    plan.HoundCount = 2;
                    plan.PackImpCount = 0;
                    plan.ThrallCount = 0;
                    plan.ZealotCount = 0;
                    break;
                default:
                    plan.HoundCount = 2;
                    plan.PackImpCount = 2;
                    plan.ThrallCount = 1;
                    plan.ZealotCount = 1;
                    break;
            }

            plan.ThreatBudget = plan.HoundCount * HoundThreatValue
                + plan.PackImpCount * ImpThreatValue
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
