using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
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
        private const int PriestThreatValue = 340;
        private const int SniperThreatValue = 420;

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
            public int PriestCount;
            public int SniperCount;
            public string ForecastText;
            public AbyssalEncounterDirectorUtility.EncounterPlan DirectedPlan;

            public int TotalImpCount => Math.Max(0, PortalImpCount) + Math.Max(0, PackImpCount);
            public int TotalEscortCount => Math.Max(0, HoundCount) + TotalImpCount + Math.Max(0, ThrallCount) + Math.Max(0, ZealotCount) + Math.Max(0, PriestCount) + Math.Max(0, SniperCount);
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
                ApplyEmberHuntPlan(map, plan);
            }
            else
            {
                ApplyChoirEnginePlan(map, plan);
            }

            plan.ForecastText = BuildForecastText(plan);
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

            return !plan.ForecastText.NullOrEmpty() ? plan.ForecastText : BuildForecastText(plan);
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
            float baseBudget = Math.Min(60, Math.Max(3, colonists * 3)) * ImpThreatValue;
            AbyssalEncounterDirectorUtility.EncounterPlan directed = AbyssalEncounterDirectorUtility.BuildPlan("unstable_breach_portal", baseBudget, 1, map, null, null, null);

            plan.DirectedPlan = directed;
            plan.PortalImpCount = directed.GetCount("ABY_RiftImp");
            plan.PackImpCount = 0;
            plan.DirectedPlan = directed;
            plan.HoundCount = directed.GetCount("ABY_EmberHound");
            plan.ThrallCount = directed.GetCount("ABY_HexgunThrall");
            plan.ZealotCount = directed.GetCount("ABY_ChainZealot");
            plan.PriestCount = directed.GetCount("ABY_NullPriest");
            plan.SniperCount = directed.GetCount("ABY_RiftSniper");

            if (plan.TotalEscortCount <= 0)
            {
                plan.PortalImpCount = Math.Max(3, colonists * 3);
            }

            plan.ThreatBudget = plan.TotalImpCount * ImpThreatValue
                + plan.HoundCount * HoundThreatValue
                + plan.ThrallCount * ThrallThreatValue
                + plan.ZealotCount * ZealotThreatValue
                + plan.PriestCount * PriestThreatValue
                + plan.SniperCount * SniperThreatValue;
        }

        private static void ApplyEmberHuntPlan(Map map, ThreatPlan plan)
        {
            int colonists = GetActiveColonistCount(map);
            int minCount = Math.Max(1, colonists);
            int maxCount = Math.Min(25, Math.Max(minCount, colonists * 3));
            int mid = Math.Max(1, (minCount + maxCount) / 2);
            float baseBudget = mid * HoundThreatValue;
            AbyssalEncounterDirectorUtility.EncounterPlan directed = AbyssalEncounterDirectorUtility.BuildPlan("ember_hunt_pack", baseBudget, 1, map, null, null, null);

            plan.HoundCount = directed.GetCount("ABY_EmberHound");
            plan.DirectedPlan = directed;
            plan.PortalImpCount = 0;
            plan.PackImpCount = directed.GetCount("ABY_RiftImp");
            plan.ThrallCount = directed.GetCount("ABY_HexgunThrall");
            plan.ZealotCount = directed.GetCount("ABY_ChainZealot");
            plan.PriestCount = directed.GetCount("ABY_NullPriest");
            plan.SniperCount = directed.GetCount("ABY_RiftSniper");

            if (plan.TotalEscortCount <= 0)
            {
                plan.HoundCount = Mathf.Clamp(mid, 1, 25);
            }

            plan.ThreatBudget = plan.TotalImpCount * ImpThreatValue
                + plan.HoundCount * HoundThreatValue
                + plan.ThrallCount * ThrallThreatValue
                + plan.ZealotCount * ZealotThreatValue
                + plan.PriestCount * PriestThreatValue
                + plan.SniperCount * SniperThreatValue;
        }

        private static void ApplyChoirEnginePlan(Map map, ThreatPlan plan)
        {
            int colonists = GetActiveColonistCount(map);
            float baseBudget = Math.Min(30, Math.Max(6, colonists * 6)) * ImpThreatValue;
            AbyssalEncounterDirectorUtility.EncounterPlan directed = AbyssalEncounterDirectorUtility.BuildPlan("choir_escort", baseBudget, 2, map, null, null, null);

            plan.PortalImpCount = 0;
            plan.PackImpCount = directed.GetCount("ABY_RiftImp");
            plan.HoundCount = directed.GetCount("ABY_EmberHound");
            plan.ThrallCount = directed.GetCount("ABY_HexgunThrall");
            plan.ZealotCount = directed.GetCount("ABY_ChainZealot");
            plan.PriestCount = directed.GetCount("ABY_NullPriest");
            plan.SniperCount = directed.GetCount("ABY_RiftSniper");

            if (plan.TotalEscortCount <= 0)
            {
                int fallbackEscort = Math.Min(30, Math.Max(6, colonists * 6));
                plan.PackImpCount = fallbackEscort / 2;
                plan.ThrallCount = fallbackEscort / 3;
                plan.ZealotCount = Math.Max(0, fallbackEscort - plan.PackImpCount - plan.ThrallCount);
            }

            plan.ThreatBudget = plan.TotalImpCount * ImpThreatValue
                + plan.HoundCount * HoundThreatValue
                + plan.ThrallCount * ThrallThreatValue
                + plan.ZealotCount * ZealotThreatValue
                + plan.PriestCount * PriestThreatValue
                + plan.SniperCount * SniperThreatValue;
        }

        private static string BuildForecastText(ThreatPlan plan)
        {
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

            if (plan.PriestCount > 0)
            {
                parts.Add(GetCountLabel(plan.PriestCount, "ABY_CirclePreview_Priest_Singular", "null priest", "ABY_CirclePreview_Priest_Plural", "null priests"));
            }

            if (plan.SniperCount > 0)
            {
                parts.Add(GetCountLabel(plan.SniperCount, "ABY_CirclePreview_Sniper_Singular", "rift sniper", "ABY_CirclePreview_Sniper_Plural", "rift snipers"));
            }

            return parts.Count == 0
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CirclePreview_None", "no hostiles")
                : string.Join(" + ", parts);
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
