using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionBalanceUtility
    {
        private const string ImpPortalDefName = "ABY_ImpPortal";
        private const string RupturePortalDefName = "ABY_RupturePortal";

        public sealed class RuntimeProfile
        {
            public int Colonists;
            public float Wealth;
            public int StageTier;
            public int ReplayTier;
            public int ActiveHostiles;
            public int ActivePortals;
            public int MaxActiveHostiles;
            public int MaxActivePortals;
            public int CleanupPortalBudget;
            public int AmbientPulseIntervalTicks;
            public int AmbientSoundIntervalTicks;
            public int MaintenanceIntervalTicks;
            public float AmbientContaminationMultiplier;
            public float ScreenFxMultiplier;
            public bool LowFxMode;
        }

        public static RuntimeProfile BuildProfile(Map map, MapComponent_DominionCrisis crisis)
        {
            RuntimeProfile profile = new RuntimeProfile();
            profile.Colonists = Mathf.Max(1, map != null ? ABY_Phase2PortalUtility.CountActivePlayerColonists(map) : 1);
            profile.Wealth = map?.wealthWatcher?.WealthTotal ?? 0f;

            int colonistTier = GetColonistTier(profile.Colonists);
            int wealthTier = GetWealthTier(profile.Wealth);
            profile.ReplayTier = crisis != null ? Mathf.Clamp(crisis.CompletionCount - crisis.FailureCount, 0, 2) : 0;
            profile.StageTier = AbyssalDifficultyUtility.ScaleDominionStageTier(Mathf.Clamp(Mathf.Max(colonistTier, wealthTier) + profile.ReplayTier, 0, 6));

            profile.ActiveHostiles = map != null ? AbyssalDominionWaveUtility.CountActiveAbyssalHostiles(map) : 0;
            profile.ActivePortals = map != null ? AbyssalDominionWaveUtility.CountActivePortals(map) : 0;
            profile.MaxActiveHostiles = Mathf.Clamp(AbyssalDifficultyUtility.ScaleEncounterBudget(14 + profile.Colonists + profile.StageTier * 2 + (crisis?.CompletionCount ?? 0)), 16, 42);
            profile.MaxActivePortals = profile.StageTier >= 4 ? 3 : 2;
            if (AbyssalDifficultyUtility.CurrentPreset >= ABY_DifficultyPreset.FinalGate && profile.MaxActivePortals < 4)
            {
                profile.MaxActivePortals = 4;
            }
            profile.CleanupPortalBudget = Mathf.Max(1, profile.MaxActivePortals - (profile.ActiveHostiles >= profile.MaxActiveHostiles ? 1 : 0));
            profile.LowFxMode = profile.ActiveHostiles >= Mathf.RoundToInt(profile.MaxActiveHostiles * 0.82f) || profile.ActivePortals >= profile.MaxActivePortals;
            profile.AmbientPulseIntervalTicks = Mathf.Clamp((profile.LowFxMode ? 270 : 210) - profile.StageTier * 6, 150, 300);
            profile.AmbientSoundIntervalTicks = profile.LowFxMode ? 1320 : 900;
            profile.MaintenanceIntervalTicks = profile.LowFxMode ? 240 : 300;
            profile.AmbientContaminationMultiplier = Mathf.Clamp((0.94f + profile.StageTier * 0.035f) * (profile.LowFxMode ? 0.92f : 1f) * AbyssalDifficultyUtility.CurrentProfile.InstabilityMultiplier, 0.85f, 1.35f);
            profile.ScreenFxMultiplier = profile.LowFxMode ? 0.72f : 1f;
            return profile;
        }

        public static string GetCalibrationValue(Map map, MapComponent_DominionCrisis crisis)
        {
            RuntimeProfile profile = BuildProfile(map, crisis);
            if (profile.StageTier <= 1)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionCalibration_Contained", "contained");
            }

            if (profile.StageTier <= 3)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionCalibration_Escalated", "escalated");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionCalibration_Apex", "apex");
        }

        public static string GetRuntimeBudgetValue(Map map, MapComponent_DominionCrisis crisis)
        {
            RuntimeProfile profile = BuildProfile(map, crisis);
            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionBudget_Value",
                "{0}/{1} hostiles • {2}/{3} portals",
                profile.ActiveHostiles,
                profile.MaxActiveHostiles,
                profile.ActivePortals,
                profile.MaxActivePortals);
        }

        public static string GetFxModeValue(Map map, MapComponent_DominionCrisis crisis)
        {
            return ShouldUseLowFxMode(map, crisis)
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionFxMode_Restrained", "restrained")
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionFxMode_Standard", "standard");
        }

        public static List<string> GetConsoleLines(Map map, MapComponent_DominionCrisis crisis)
        {
            List<string> lines = new List<string>();
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionBalanceConsoleCalibration",
                "Calibration: {0}",
                GetCalibrationValue(map, crisis)));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionBalanceConsoleBudget",
                "Runtime budget: {0}",
                GetRuntimeBudgetValue(map, crisis)));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionBalanceConsoleFx",
                "FX mode: {0}",
                GetFxModeValue(map, crisis)));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_Difficulty_ConsoleLine",
                "Difficulty protocol: {0}",
                AbyssalDifficultyUtility.GetCurrentPresetLabel()));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_Difficulty_ConsoleImpact",
                "Profile effect: encounter x{0}, instability x{1}, rewards x{2}",
                AbyssalDifficultyUtility.CurrentProfile.EncounterBudgetMultiplier.ToString("F2"),
                AbyssalDifficultyUtility.CurrentProfile.InstabilityMultiplier.ToString("F2"),
                AbyssalDifficultyUtility.CurrentProfile.RewardMultiplier.ToString("F2")));

            if (crisis != null && !crisis.LastMaintenanceSummary.NullOrEmpty())
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_DominionBalanceConsoleMaintenance",
                    "Governor: {0}",
                    crisis.LastMaintenanceSummary));
            }

            return lines;
        }

        public static int GetAmbientPulseIntervalTicks(Map map, MapComponent_DominionCrisis crisis)
        {
            return BuildProfile(map, crisis).AmbientPulseIntervalTicks;
        }

        public static int GetAmbientSoundIntervalTicks(Map map, MapComponent_DominionCrisis crisis)
        {
            return BuildProfile(map, crisis).AmbientSoundIntervalTicks;
        }

        public static int GetMaintenanceIntervalTicks(Map map, MapComponent_DominionCrisis crisis)
        {
            return BuildProfile(map, crisis).MaintenanceIntervalTicks;
        }

        public static float GetAmbientContaminationMultiplier(Map map, MapComponent_DominionCrisis crisis)
        {
            return BuildProfile(map, crisis).AmbientContaminationMultiplier;
        }

        public static float GetScreenFxMultiplier(Map map, MapComponent_DominionCrisis crisis)
        {
            return BuildProfile(map, crisis).ScreenFxMultiplier;
        }

        public static bool ShouldUseLowFxMode(Map map, MapComponent_DominionCrisis crisis)
        {
            return BuildProfile(map, crisis).LowFxMode;
        }

        public static int CollapseExcessPortals(Map map, MapComponent_DominionCrisis crisis, IntVec3 focusCell)
        {
            if (map == null)
            {
                return 0;
            }

            RuntimeProfile profile = BuildProfile(map, crisis);
            List<Thing> portals = GetActivePortals(map);
            if (portals.Count <= profile.CleanupPortalBudget)
            {
                return 0;
            }

            portals.Sort(delegate (Thing a, Thing b)
            {
                float aScore = GetPortalCleanupScore(a, focusCell);
                float bScore = GetPortalCleanupScore(b, focusCell);
                return bScore.CompareTo(aScore);
            });

            int removed = 0;
            for (int i = profile.CleanupPortalBudget; i < portals.Count; i++)
            {
                Thing portal = portals[i];
                if (portal == null || portal.Destroyed)
                {
                    continue;
                }

                if (!profile.LowFxMode)
                {
                    FleckMaker.ThrowLightningGlow(portal.DrawPos, map, 1.25f);
                }

                portal.Destroy(DestroyMode.Vanish);
                removed++;
            }

            return removed;
        }

        private static float GetPortalCleanupScore(Thing portal, IntVec3 focusCell)
        {
            if (portal == null)
            {
                return 0f;
            }

            float score = portal.thingIDNumber;
            if (focusCell.IsValid)
            {
                score += portal.PositionHeld.DistanceToSquared(focusCell) * 100f;
            }

            return score;
        }

        private static List<Thing> GetActivePortals(Map map)
        {
            List<Thing> portals = new List<Thing>();
            AddPortalsOfDef(map, ImpPortalDefName, portals);
            AddPortalsOfDef(map, RupturePortalDefName, portals);
            return portals;
        }

        private static void AddPortalsOfDef(Map map, string defName, List<Thing> portals)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null && thing.Spawned && !thing.Destroyed)
                {
                    portals.Add(thing);
                }
            }
        }

        private static int GetColonistTier(int colonists)
        {
            if (colonists <= 5)
            {
                return 0;
            }

            if (colonists <= 8)
            {
                return 1;
            }

            if (colonists <= 11)
            {
                return 2;
            }

            if (colonists <= 15)
            {
                return 3;
            }

            if (colonists <= 20)
            {
                return 4;
            }

            return 5;
        }

        private static int GetWealthTier(float wealth)
        {
            if (wealth <= 120000f)
            {
                return 0;
            }

            if (wealth <= 240000f)
            {
                return 1;
            }

            if (wealth <= 400000f)
            {
                return 2;
            }

            if (wealth <= 650000f)
            {
                return 3;
            }

            if (wealth <= 950000f)
            {
                return 4;
            }

            return 5;
        }
    }
}
