using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionWaveUtility
    {
        private const string RiftImpPawnKindDefName = "ABY_RiftImp";
        private const string EmberHoundPawnKindDefName = "ABY_EmberHound";
        private const string HexgunThrallPawnKindDefName = "ABY_HexgunThrall";
        private const string NullPriestPawnKindDefName = "ABY_NullPriest";
        private const string ImpPortalDefName = "ABY_ImpPortal";
        private const string RupturePortalDefName = "ABY_RupturePortal";

        private const float PortalMinRadius = 5.9f;
        private const float PortalMaxRadius = 11.9f;
        private const int PortalWarmupBaseTicks = 42;
        private const int PortalLingerBaseTicks = 170;
        private const int PortalSpawnIntervalFastTicks = 12;
        private const int PortalSpawnIntervalSlowTicks = 17;
        private const int NullPriestManifestationWarmupTicks = 105;
        private const int NullPriestManifestationWarmupTicksGate = 120;

        public sealed class ThreatPlan
        {
            public int Tier;
            public int ColonistTier;
            public int WealthTier;
            public int PortalCount;
            public int TotalImpCount;
            public int HoundCount;
            public int ThrallCount;
            public int PriestCount;
            public int ZealotCount;
            public int SniperCount;
            public int HaloHuskCount;
            public int MaxActiveHostiles;
            public int MaxActivePortals;
            public int ActiveAnchorCount;
            public int SuppressionAnchors;
            public int DrainAnchors;
            public int WardAnchors;
            public int BreachAnchors;
            public AbyssalEncounterDirectorUtility.EncounterPlan DirectedPlan;

            public int TotalUnits => Math.Max(0, TotalImpCount)
                + Math.Max(0, HoundCount)
                + Math.Max(0, ThrallCount)
                + Math.Max(0, PriestCount)
                + Math.Max(0, ZealotCount)
                + Math.Max(0, SniperCount)
                + Math.Max(0, HaloHuskCount);
        }

        public static ThreatPlan BuildThreatPlan(Map map, MapComponent_DominionCrisis crisis)
        {
            ThreatPlan plan = new ThreatPlan();
            if (map == null || crisis == null)
            {
                return plan;
            }

            AbyssalDominionBalanceUtility.RuntimeProfile profile = AbyssalDominionBalanceUtility.BuildProfile(map, crisis);
            plan.ColonistTier = GetColonistTier(profile.Colonists);
            plan.WealthTier = GetWealthTier(profile.Wealth);
            plan.Tier = Mathf.Clamp(Math.Max(plan.ColonistTier, plan.WealthTier) + Mathf.Clamp(crisis.WavesTriggered / 2, 0, 2) + profile.ReplayTier, 0, 5);

            plan.ActiveAnchorCount = crisis.ActiveAnchorCount;
            plan.SuppressionAnchors = crisis.GetActiveAnchorCount(DominionAnchorRole.Suppression);
            plan.DrainAnchors = crisis.GetActiveAnchorCount(DominionAnchorRole.Drain);
            plan.WardAnchors = crisis.GetActiveAnchorCount(DominionAnchorRole.Ward);
            plan.BreachAnchors = crisis.GetActiveAnchorCount(DominionAnchorRole.Breach);

            int legacyPortalCount = plan.BreachAnchors > 0
                ? (plan.Tier >= 3 ? 2 : 1)
                : (plan.ActiveAnchorCount >= 3 && plan.Tier >= 4 ? 1 : 0);

            int legacyImpCount = 0;
            if (legacyPortalCount > 0)
            {
                legacyImpCount = 2 + plan.Tier + Mathf.Min(2, plan.BreachAnchors) + Mathf.Min(1, plan.DrainAnchors) + Mathf.Min(crisis.WavesTriggered, 2);
                if (profile.StageTier >= 4)
                {
                    legacyImpCount += 1;
                }

                if (plan.ActiveAnchorCount <= 2)
                {
                    legacyImpCount = Mathf.Max(2, legacyImpCount - 1);
                }
            }

            int legacyHounds = plan.Tier >= 2 ? 1 : 0;
            if (plan.WardAnchors > 0 && plan.Tier >= 3)
            {
                legacyHounds += 1;
            }
            if (profile.StageTier >= 5)
            {
                legacyHounds += 1;
            }
            if (plan.ActiveAnchorCount <= 2)
            {
                legacyHounds = Mathf.Max(0, legacyHounds - 1);
            }

            int legacyThralls = 0;
            if (plan.SuppressionAnchors > 0 && plan.Tier >= 2)
            {
                legacyThralls = 1;
            }
            if (plan.DrainAnchors > 0 && plan.Tier >= 4)
            {
                legacyThralls += 1;
            }
            if (crisis.WavesTriggered >= 3 && plan.Tier >= 5)
            {
                legacyThralls = Mathf.Min(2, legacyThralls + 1);
            }
            if (plan.ActiveAnchorCount <= 1)
            {
                legacyThralls = Mathf.Min(legacyThralls, 1);
            }

            int legacyPriests = 0;
            if (plan.Tier >= 4 && (plan.SuppressionAnchors > 0 || plan.WardAnchors > 0 || plan.DrainAnchors > 0))
            {
                legacyPriests = 1;
            }
            if (plan.ActiveAnchorCount <= 2)
            {
                legacyPriests = 0;
            }

            float baseBudget = legacyImpCount * 85f + legacyHounds * 160f + legacyThralls * 210f + legacyPriests * 360f;
            baseBudget = Mathf.Max(170f, baseBudget);
            baseBudget += Mathf.Max(0, plan.ActiveAnchorCount - 1) * 30f;

            Dictionary<string, int> minimumRoleCounts = new Dictionary<string, int>();
            Dictionary<string, int> maximumRoleCounts = new Dictionary<string, int>
            {
                { "boss", 0 },
                { "support", plan.Tier >= 4 ? 2 : 1 },
                { "elite", plan.Tier >= 5 ? 3 : (plan.Tier >= 3 ? 2 : 1) }
            };

            if (plan.Tier >= 2 && (plan.WardAnchors > 0 || plan.BreachAnchors > 0))
            {
                minimumRoleCounts["elite"] = 1;
            }

            if (plan.Tier >= 4 && (plan.SuppressionAnchors > 0 || plan.DrainAnchors > 0 || plan.WardAnchors > 0))
            {
                minimumRoleCounts["support"] = 1;
            }

            int seed = GetDirectorSeed(map, crisis, 3719 + plan.ActiveAnchorCount * 11);
            plan.DirectedPlan = AbyssalEncounterDirectorUtility.BuildPlan(
                "dominion_wave",
                baseBudget,
                plan.Tier,
                seed,
                minimumRoleCounts,
                maximumRoleCounts);

            ApplyDirectedEntriesToThreatPlan(plan, plan.DirectedPlan);

            if (plan.TotalUnits <= 0)
            {
                plan.TotalImpCount = legacyImpCount;
                plan.HoundCount = legacyHounds;
                plan.ThrallCount = legacyThralls;
                plan.PriestCount = legacyPriests;
            }

            plan.MaxActiveHostiles = profile.MaxActiveHostiles;
            plan.PortalCount = 0;
            if (plan.TotalImpCount > 0)
            {
                if (plan.BreachAnchors > 0)
                {
                    plan.PortalCount = plan.TotalImpCount >= 5 || plan.Tier >= 3 ? 2 : 1;
                }
                else if (plan.ActiveAnchorCount >= 3 && plan.Tier >= 4)
                {
                    plan.PortalCount = 1;
                }
            }

            plan.MaxActivePortals = Mathf.Min(profile.MaxActivePortals, plan.PortalCount >= 2 ? 3 : 2);
            return plan;
        }

        private static void ApplyDirectedEntriesToThreatPlan(ThreatPlan plan, AbyssalEncounterDirectorUtility.EncounterPlan directed)
        {
            if (plan == null || directed == null)
            {
                return;
            }

            plan.TotalImpCount = directed.GetCount(RiftImpPawnKindDefName);
            plan.HoundCount = directed.GetCount(EmberHoundPawnKindDefName);
            plan.ThrallCount = directed.GetCount(HexgunThrallPawnKindDefName);
            plan.PriestCount = directed.GetCount(NullPriestPawnKindDefName);
            plan.ZealotCount = directed.GetCount("ABY_ChainZealot");
            plan.SniperCount = directed.GetCount("ABY_RiftSniper");
            plan.HaloHuskCount = directed.GetCount("ABY_HaloHusk");
        }

        private static int GetDirectorSeed(Map map, MapComponent_DominionCrisis crisis, int salt)
        {
            int seed = 17;
            seed = Gen.HashCombineInt(seed, map != null ? map.uniqueID : 0);
            seed = Gen.HashCombineInt(seed, crisis != null ? crisis.WavesTriggered : 0);
            seed = Gen.HashCombineInt(seed, crisis != null ? crisis.ActiveAnchorCount : 0);
            seed = Gen.HashCombineInt(seed, crisis != null ? crisis.CompletionCount : 0);
            seed = Gen.HashCombineInt(seed, salt);
            return seed;
        }

        public static int GetInitialWaveDelayTicks(MapComponent_DominionCrisis crisis)
        {
            return Mathf.Clamp(GetNextWaveDelayTicks(crisis) - 420, 1500, 4200);
        }

        public static int GetNextWaveDelayTicks(MapComponent_DominionCrisis crisis)
        {
            if (crisis == null)
            {
                return 3600;
            }

            int anchors = Mathf.Max(0, crisis.ActiveAnchorCount);
            int breachAnchors = crisis.GetActiveAnchorCount(DominionAnchorRole.Breach);
            AbyssalDominionBalanceUtility.RuntimeProfile profile = AbyssalDominionBalanceUtility.BuildProfile(crisis.CrisisMap, crisis);
            int delay = 4800;
            delay -= anchors * 220;
            delay -= breachAnchors * 600;
            delay -= Mathf.Min(5, crisis.WavesTriggered) * 130;
            if (profile.LowFxMode)
            {
                delay += 240;
            }

            delay -= profile.StageTier * 40;
            return Mathf.Clamp(delay, 2100, 5400);
        }

        public static string GetWavePressureLabel(Map map, MapComponent_DominionCrisis crisis)
        {
            if (crisis == null || !crisis.IsAnchorPhaseActive)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePressure_Dormant", "dormant");
            }

            ThreatPlan plan = BuildThreatPlan(map, crisis);
            if (plan.TotalUnits <= 0)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePressure_Low", "low");
            }

            if (plan.TotalUnits <= 3)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePressure_Low", "low");
            }

            if (plan.TotalUnits <= 5)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePressure_High", "high");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePressure_Severe", "severe");
        }

        public static string GetWavePreviewText(Map map, MapComponent_DominionCrisis crisis)
        {
            if (crisis == null || !crisis.IsAnchorPhaseActive)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePreviewStatus_Dormant", "not armed");
            }

            string throttleReason = GetWaveThrottleReason(map, crisis);
            if (!throttleReason.NullOrEmpty())
            {
                return throttleReason;
            }

            ThreatPlan plan = BuildThreatPlan(map, crisis);
            string composition = GetCompositionText(plan);
            return composition.NullOrEmpty()
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePreviewStatus_Quiet", "quiet")
                : composition;
        }

        public static string GetWaveThrottleReason(Map map, MapComponent_DominionCrisis crisis)
        {
            if (map == null || crisis == null || !crisis.IsAnchorPhaseActive)
            {
                return null;
            }

            ThreatPlan plan = BuildThreatPlan(map, crisis);
            int activeHostiles = CountActiveAbyssalHostiles(map);
            if (activeHostiles >= plan.MaxActiveHostiles)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePreviewStatus_ThrottledUnits", "throttled while earlier abyssal hostiles are still active");
            }

            int activePortals = CountActivePortals(map);
            if (activePortals >= plan.MaxActivePortals)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePreviewStatus_ThrottledPortals", "waiting for earlier breach portals to collapse");
            }

            return null;
        }

        public static List<string> GetConsoleLines(Map map, MapComponent_DominionCrisis crisis)
        {
            List<string> lines = new List<string>();
            if (crisis == null)
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveConsoleIdle", "Wave routing remains dormant until anchorfall begins."));
                return lines;
            }

            if (crisis.IsGatePhaseActive)
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveConsoleGatecore", "Wave routing is now slaved directly to the Crowned Gate core."));
                if (!crisis.LastWaveSummary.NullOrEmpty())
                {
                    lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveConsoleLastPulse", "Last pulse: {0}", crisis.LastWaveSummary));
                }
                return lines;
            }

            if (!crisis.IsAnchorPhaseActive)
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveConsoleIdle", "Wave routing remains dormant until anchorfall begins."));
                return lines;
            }

            ThreatPlan plan = BuildThreatPlan(map, crisis);
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionWaveConsoleComposition",
                "Projected pulse: {0}",
                GetCompositionText(plan)));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionWaveConsoleCadence",
                "Pulse cadence: roughly every {0}",
                GetNextWaveDelayTicks(crisis).ToStringTicksToPeriod()));

            string throttleReason = GetWaveThrottleReason(map, crisis);
            if (!throttleReason.NullOrEmpty())
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_DominionWaveConsoleThrottle",
                    "Current throttle: {0}",
                    throttleReason));
            }
            else
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_DominionWaveConsolePressure",
                    "Wave pressure: {0}",
                    GetWavePressureLabel(map, crisis)));
            }

            if (!crisis.LastWaveSummary.NullOrEmpty())
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_DominionWaveConsoleLastPulse",
                    "Last pulse: {0}",
                    crisis.LastWaveSummary));
            }

            lines.AddRange(AbyssalDominionBalanceUtility.GetConsoleLines(map, crisis));
            return lines;
        }

        public static bool TryExecuteWave(Map map, MapComponent_DominionCrisis crisis, out string summary, out IntVec3 focusCell)
        {
            summary = null;
            focusCell = IntVec3.Invalid;

            if (map == null || crisis == null || !crisis.IsAnchorPhaseActive)
            {
                return false;
            }

            string throttleReason = GetWaveThrottleReason(map, crisis);
            if (!throttleReason.NullOrEmpty())
            {
                summary = throttleReason;
                return false;
            }

            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            if (hostileFaction == null)
            {
                return false;
            }

            ThreatPlan plan = BuildThreatPlan(map, crisis);
            if (plan.TotalUnits <= 0)
            {
                return false;
            }

            bool anySpawned = false;
            List<string> summaryParts = new List<string>();
            List<Building_AbyssalDominionAnchor> anchors = crisis.GetLiveAnchors();

            if (plan.TotalImpCount > 0)
            {
                int requestedPortals = Mathf.Clamp(plan.PortalCount, 0, Math.Max(1, plan.TotalImpCount));
                int portalsOpened = 0;
                int impsSpawned = 0;
                for (int i = 0; i < requestedPortals; i++)
                {
                    int remainingPortals = Math.Max(1, requestedPortals - i);
                    int impsForPulse = Mathf.Clamp(Mathf.CeilToInt((float)(plan.TotalImpCount - impsSpawned) / remainingPortals), 1, Math.Max(1, plan.TotalImpCount - impsSpawned));
                    IntVec3 origin = GetPreferredAnchorFromRoles(anchors, DominionAnchorRole.Breach, DominionAnchorRole.Drain, DominionAnchorRole.Ward)?.PositionHeld ?? crisis.SourceCell;
                    if (!origin.IsValid)
                    {
                        continue;
                    }

                    bool spawnedPortal = ABY_Phase2PortalUtility.TrySpawnImpPortalNear(
                        map,
                        hostileFaction,
                        origin,
                        PortalMinRadius,
                        PortalMaxRadius,
                        impsForPulse,
                        PortalWarmupBaseTicks + (plan.Tier * 5),
                        plan.Tier >= 4 ? PortalSpawnIntervalFastTicks : PortalSpawnIntervalSlowTicks,
                        PortalLingerBaseTicks + (plan.Tier * 10),
                        out Building_AbyssalImpPortal portal);

                    if (!spawnedPortal)
                    {
                        PawnKindDef impKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(RiftImpPawnKindDefName);
                        if (impKind != null && TrySpawnSingleKindPack(
                            map,
                            hostileFaction,
                            impKind,
                            impsForPulse,
                            origin,
                            AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Imps", "dominion imp pulse"),
                            out IntVec3 arrivalCell))
                        {
                            spawnedPortal = true;
                            focusCell = arrivalCell;
                        }
                    }
                    else if (portal != null)
                    {
                        focusCell = portal.PositionHeld;
                    }

                    if (spawnedPortal)
                    {
                        portalsOpened++;
                        impsSpawned += impsForPulse;
                        anySpawned = true;
                    }
                }

                int directImps = Math.Max(0, plan.TotalImpCount - impsSpawned);
                if (directImps > 0)
                {
                    PawnKindDef impKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(RiftImpPawnKindDefName);
                    IntVec3 fallbackOrigin = GetPreferredAnchorFromRoles(anchors, DominionAnchorRole.Breach, DominionAnchorRole.Drain, DominionAnchorRole.Ward)?.PositionHeld ?? crisis.SourceCell;
                    if (impKind != null && fallbackOrigin.IsValid && TrySpawnSingleKindPack(
                        map,
                        hostileFaction,
                        impKind,
                        directImps,
                        fallbackOrigin,
                        AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Imps", "dominion imp pulse"),
                        out IntVec3 arrivalCell))
                    {
                        focusCell = focusCell.IsValid ? focusCell : arrivalCell;
                        anySpawned = true;
                        impsSpawned += directImps;
                    }
                }

                if (impsSpawned > 0)
                {
                    summaryParts.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_DominionWaveSummary_Imps",
                        "{0} via {1}",
                        GetCountLabel(impsSpawned, "ABY_CirclePreview_Imp_Singular", "imp", "ABY_CirclePreview_Imp_Plural", "imps"),
                        portalsOpened <= 1
                            ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveSummary_PortalSingle", "1 portal")
                            : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveSummary_PortalPlural", "{0} portals", portalsOpened)));
                }
            }

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail(EmberHoundPawnKindDefName),
                plan.HoundCount,
                GetPreferredAnchorFromRoles(anchors, DominionAnchorRole.Ward, DominionAnchorRole.Breach)?.PositionHeld ?? crisis.SourceCell,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Hounds", "dominion hunter pack"),
                "ABY_CirclePreview_Hound_Singular",
                "hound",
                "ABY_CirclePreview_Hound_Plural",
                "hounds",
                summaryParts,
                ref focusCell);

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail("ABY_ChainZealot"),
                plan.ZealotCount,
                GetPreferredAnchorFromRoles(anchors, DominionAnchorRole.Breach, DominionAnchorRole.Suppression, DominionAnchorRole.Ward)?.PositionHeld ?? crisis.SourceCell,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Zealots", "dominion breach cell"),
                "ABY_CirclePreview_Zealot_Singular",
                "zealot",
                "ABY_CirclePreview_Zealot_Plural",
                "zealots",
                summaryParts,
                ref focusCell);

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail(HexgunThrallPawnKindDefName),
                plan.ThrallCount,
                GetPreferredAnchorFromRoles(anchors, DominionAnchorRole.Suppression, DominionAnchorRole.Drain, DominionAnchorRole.Ward)?.PositionHeld ?? crisis.SourceCell,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Thralls", "dominion relay pack"),
                "ABY_CirclePreview_Thrall_Singular",
                "thrall",
                "ABY_CirclePreview_Thrall_Plural",
                "thralls",
                summaryParts,
                ref focusCell);

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail("ABY_RiftSniper"),
                plan.SniperCount,
                GetPreferredAnchorFromRoles(anchors, DominionAnchorRole.Suppression, DominionAnchorRole.Ward, DominionAnchorRole.Drain)?.PositionHeld ?? crisis.SourceCell,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Snipers", "dominion mark pack"),
                "ABY_CirclePreview_Sniper_Singular",
                "rift sniper",
                "ABY_CirclePreview_Sniper_Plural",
                "rift snipers",
                summaryParts,
                ref focusCell);

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail("ABY_HaloHusk"),
                plan.HaloHuskCount,
                GetPreferredAnchorFromRoles(anchors, DominionAnchorRole.Ward, DominionAnchorRole.Suppression, DominionAnchorRole.Breach)?.PositionHeld ?? crisis.SourceCell,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_HaloHusks", "dominion halo cell"),
                "ABY_CirclePreview_HaloHusk_Singular",
                "halo husk",
                "ABY_CirclePreview_HaloHusk_Plural",
                "halo husks",
                summaryParts,
                ref focusCell);

            PawnKindDef priestKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(NullPriestPawnKindDefName);
            IntVec3 priestOrigin = GetPreferredAnchorFromRoles(anchors, DominionAnchorRole.Suppression, DominionAnchorRole.Ward, DominionAnchorRole.Drain)?.PositionHeld ?? crisis.SourceCell;
            if (plan.PriestCount > 0 && priestKind != null && priestOrigin.IsValid && TrySpawnNullPriestManifestationPack(
                map,
                hostileFaction,
                priestKind,
                plan.PriestCount,
                priestOrigin,
                NullPriestManifestationWarmupTicks,
                out IntVec3 priestArrivalCell))
            {
                focusCell = focusCell.IsValid ? focusCell : priestArrivalCell;
                anySpawned = true;
                summaryParts.Add(GetCountLabel(plan.PriestCount, "ABY_CirclePreview_Priest_Singular", "null priest", "ABY_CirclePreview_Priest_Plural", "null priests"));
            }

            if (!anySpawned)
            {
                return false;
            }

            if (focusCell.IsValid)
            {
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", focusCell, map);
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.13f);
            }

            summary = summaryParts.Count > 0
                ? string.Join(" + ", summaryParts)
                : GetCompositionText(plan);
            return true;
        }

        public static bool TryExecuteGateSupportWave(Map map, MapComponent_DominionCrisis crisis, IntVec3 origin, out string summary, out IntVec3 focusCell)
        {
            summary = null;
            focusCell = IntVec3.Invalid;

            if (map == null || crisis == null || !crisis.IsGatePhaseActive || !origin.IsValid)
            {
                return false;
            }

            AbyssalDominionBalanceUtility.RuntimeProfile profile = AbyssalDominionBalanceUtility.BuildProfile(map, crisis);
            int activeHostiles = CountActiveAbyssalHostiles(map);
            if (activeHostiles >= profile.MaxActiveHostiles)
            {
                summary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePreviewStatus_ThrottledUnits", "throttled while earlier abyssal hostiles are still active");
                return false;
            }

            int activePortals = CountActivePortals(map);
            if (activePortals >= profile.MaxActivePortals)
            {
                summary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePreviewStatus_ThrottledPortals", "waiting for earlier breach portals to collapse");
                return false;
            }

            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            if (hostileFaction == null)
            {
                return false;
            }

            int tier = Mathf.Clamp(profile.StageTier + Mathf.Clamp(crisis.WavesTriggered / 2, 0, 2), 0, 5);
            int legacyImpCount = Mathf.Clamp(3 + tier + Mathf.Min(2, crisis.WavesTriggered / 2), 3, 9);
            int legacyHoundCount = tier >= 1 ? 1 + (tier >= 4 ? 1 : 0) : 0;
            int legacyThrallCount = tier >= 2 ? 1 + (tier >= 5 ? 1 : 0) : 0;
            int legacyPriestCount = tier >= 4 ? 1 : 0;
            float baseBudget = Mathf.Max(220f, legacyImpCount * 85f + legacyHoundCount * 160f + legacyThrallCount * 210f + legacyPriestCount * 360f);

            Dictionary<string, int> minimumRoleCounts = new Dictionary<string, int>();
            Dictionary<string, int> maximumRoleCounts = new Dictionary<string, int>
            {
                { "boss", 0 },
                { "support", tier >= 4 ? 2 : 1 },
                { "elite", tier >= 5 ? 3 : 2 }
            };

            if (tier >= 2)
            {
                minimumRoleCounts["elite"] = 1;
            }
            if (tier >= 4)
            {
                minimumRoleCounts["support"] = 1;
            }

            AbyssalEncounterDirectorUtility.EncounterPlan directed = AbyssalEncounterDirectorUtility.BuildPlan(
                "dominion_gate_support",
                baseBudget,
                tier,
                GetDirectorSeed(map, crisis, 6127 + tier),
                minimumRoleCounts,
                maximumRoleCounts);

            ThreatPlan plan = new ThreatPlan
            {
                Tier = tier,
                DirectedPlan = directed
            };
            ApplyDirectedEntriesToThreatPlan(plan, directed);

            if (plan.TotalUnits <= 0)
            {
                plan.TotalImpCount = legacyImpCount;
                plan.HoundCount = legacyHoundCount;
                plan.ThrallCount = legacyThrallCount;
                plan.PriestCount = legacyPriestCount;
            }

            bool anySpawned = false;
            List<string> summaryParts = new List<string>();

            if (plan.TotalImpCount > 0)
            {
                bool spawnedPortal = ABY_Phase2PortalUtility.TrySpawnImpPortalNear(
                    map,
                    hostileFaction,
                    origin,
                    PortalMinRadius,
                    PortalMaxRadius + 1.8f,
                    plan.TotalImpCount,
                    PortalWarmupBaseTicks + 12,
                    PortalSpawnIntervalFastTicks,
                    PortalLingerBaseTicks + 50,
                    out Building_AbyssalImpPortal portal);

                if (!spawnedPortal)
                {
                    PawnKindDef impKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(RiftImpPawnKindDefName);
                    if (impKind != null && TrySpawnSingleKindPack(
                        map,
                        hostileFaction,
                        impKind,
                        plan.TotalImpCount,
                        origin,
                        AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Imps", "dominion imp pulse"),
                        out IntVec3 impArrivalCell))
                    {
                        spawnedPortal = true;
                        focusCell = impArrivalCell;
                    }
                }
                else if (portal != null)
                {
                    focusCell = portal.PositionHeld;
                }

                if (spawnedPortal)
                {
                    anySpawned = true;
                    summaryParts.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_DominionWaveSummary_Imps",
                        "{0} via {1}",
                        GetCountLabel(plan.TotalImpCount, "ABY_CirclePreview_Imp_Singular", "imp", "ABY_CirclePreview_Imp_Plural", "imps"),
                        AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveSummary_PortalSingle", "1 portal")));
                }
            }

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail(EmberHoundPawnKindDefName),
                plan.HoundCount,
                origin,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Hounds", "dominion hunter pack"),
                "ABY_CirclePreview_Hound_Singular",
                "hound",
                "ABY_CirclePreview_Hound_Plural",
                "hounds",
                summaryParts,
                ref focusCell);

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail("ABY_ChainZealot"),
                plan.ZealotCount,
                origin,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Zealots", "dominion breach cell"),
                "ABY_CirclePreview_Zealot_Singular",
                "zealot",
                "ABY_CirclePreview_Zealot_Plural",
                "zealots",
                summaryParts,
                ref focusCell);

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail(HexgunThrallPawnKindDefName),
                plan.ThrallCount,
                origin,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Thralls", "dominion relay pack"),
                "ABY_CirclePreview_Thrall_Singular",
                "thrall",
                "ABY_CirclePreview_Thrall_Plural",
                "thralls",
                summaryParts,
                ref focusCell);

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail("ABY_RiftSniper"),
                plan.SniperCount,
                origin,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Snipers", "dominion mark pack"),
                "ABY_CirclePreview_Sniper_Singular",
                "rift sniper",
                "ABY_CirclePreview_Sniper_Plural",
                "rift snipers",
                summaryParts,
                ref focusCell);

            anySpawned |= TrySpawnSingleKindSummary(
                map,
                hostileFaction,
                DefDatabase<PawnKindDef>.GetNamedSilentFail("ABY_HaloHusk"),
                plan.HaloHuskCount,
                origin,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_HaloHusks", "dominion halo cell"),
                "ABY_CirclePreview_HaloHusk_Singular",
                "halo husk",
                "ABY_CirclePreview_HaloHusk_Plural",
                "halo husks",
                summaryParts,
                ref focusCell);

            PawnKindDef priestKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(NullPriestPawnKindDefName);
            if (plan.PriestCount > 0 && priestKind != null && TrySpawnNullPriestManifestationPack(
                map,
                hostileFaction,
                priestKind,
                plan.PriestCount,
                origin,
                NullPriestManifestationWarmupTicksGate,
                out IntVec3 priestArrivalCell))
            {
                focusCell = focusCell.IsValid ? focusCell : priestArrivalCell;
                anySpawned = true;
                summaryParts.Add(GetCountLabel(plan.PriestCount, "ABY_CirclePreview_Priest_Singular", "null priest", "ABY_CirclePreview_Priest_Plural", "null priests"));
            }

            if (!anySpawned)
            {
                return false;
            }

            if (focusCell.IsValid)
            {
                ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", focusCell, map);
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.14f);
            }

            summary = summaryParts.Count > 0 ? string.Join(" + ", summaryParts) : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionGate_CallSummary", "gate reinforcements");
            return true;
        }

        private static bool TrySpawnSingleKindSummary(
            Map map,
            Faction hostileFaction,
            PawnKindDef kindDef,
            int count,
            IntVec3 origin,
            string packLabel,
            string singularKey,
            string singularFallback,
            string pluralKey,
            string pluralFallback,
            List<string> summaryParts,
            ref IntVec3 focusCell)
        {
            if (count <= 0 || kindDef == null || !origin.IsValid)
            {
                return false;
            }

            if (!TrySpawnSingleKindPack(map, hostileFaction, kindDef, count, origin, packLabel, out IntVec3 arrivalCell))
            {
                return false;
            }

            focusCell = focusCell.IsValid ? focusCell : arrivalCell;
            summaryParts.Add(GetCountLabel(count, singularKey, singularFallback, pluralKey, pluralFallback));
            return true;
        }

        private static bool TrySpawnSingleKindPack(
            Map map,
            Faction hostileFaction,
            PawnKindDef kindDef,
            int count,
            IntVec3 origin,
            string packLabel,
            out IntVec3 arrivalCell)
        {
            arrivalCell = IntVec3.Invalid;
            if (map == null || hostileFaction == null || kindDef == null || count <= 0 || !origin.IsValid)
            {
                return false;
            }

            List<AbyssalHostileSummonUtility.HostilePackEntry> entries = new List<AbyssalHostileSummonUtility.HostilePackEntry>
            {
                new AbyssalHostileSummonUtility.HostilePackEntry
                {
                    KindDef = kindDef,
                    Count = count
                }
            };

            return AbyssalHostileSummonUtility.TrySpawnHostilePack(
                map,
                entries,
                hostileFaction,
                origin,
                packLabel,
                string.Empty,
                string.Empty,
                false,
                out arrivalCell,
                out string _);
        }

        private static Building_AbyssalDominionAnchor GetPreferredAnchorFromRoles(List<Building_AbyssalDominionAnchor> anchors, params DominionAnchorRole[] roles)
        {
            if (roles != null)
            {
                for (int i = 0; i < roles.Length; i++)
                {
                    Building_AbyssalDominionAnchor preferred = GetPreferredAnchor(anchors, roles[i]);
                    if (preferred != null)
                    {
                        return preferred;
                    }
                }
            }

            return GetAnyAnchor(anchors);
        }

        private static bool TrySpawnNullPriestManifestationPack(
            Map map,
            Faction faction,
            PawnKindDef priestKind,
            int count,
            IntVec3 origin,
            int warmupTicks,
            out IntVec3 arrivalCell)
        {
            arrivalCell = IntVec3.Invalid;

            if (map == null || faction == null || priestKind == null || count <= 0 || !origin.IsValid)
            {
                return false;
            }

            List<ABY_HostileManifestEntry> entries = new List<ABY_HostileManifestEntry>
            {
                new ABY_HostileManifestEntry(priestKind, count)
            };

            if (ABY_ArrivalManifestationUtility.TrySpawnSeamBreach(
                map,
                entries,
                faction,
                origin,
                Mathf.Max(60, warmupTicks),
                out Thing manifestation,
                out string _,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Priests", "dominion null cell"),
                string.Empty,
                string.Empty))
            {
                arrivalCell = manifestation != null ? manifestation.PositionHeld : origin;
                return true;
            }

            if (ABY_ArrivalManifestationUtility.TrySpawnStaticPhaseIn(
                map,
                entries,
                faction,
                origin,
                Mathf.Max(60, warmupTicks),
                out Thing fallbackManifestation,
                out string _,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Priests", "dominion null cell"),
                string.Empty,
                string.Empty))
            {
                arrivalCell = fallbackManifestation != null ? fallbackManifestation.PositionHeld : origin;
                return true;
            }

            return false;
        }

        public static int CountActiveAbyssalHostiles(Map map)
        {
            if (map?.mapPawns == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.HostileTo(Faction.OfPlayer))
                {
                    continue;
                }

                DefModExtension_AbyssalDifficultyScaling extension = pawn.kindDef?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
                if (extension?.encounterPools == null)
                {
                    continue;
                }

                if (extension.encounterPools.Contains("dominion_wave") || extension.encounterPools.Contains("dominion_gate_support"))
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountActivePortals(Map map)
        {
            if (map == null)
            {
                return 0;
            }

            int count = 0;
            count += CountThingsOfDef(map, ImpPortalDefName);
            count += CountThingsOfDef(map, RupturePortalDefName);
            return count;
        }

        private static int CountThingsOfDef(Map map, string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return 0;
            }

            int count = 0;
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null && thing.Spawned && !thing.Destroyed)
                {
                    count++;
                }
            }

            return count;
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

        private static Building_AbyssalDominionAnchor GetPreferredAnchor(List<Building_AbyssalDominionAnchor> anchors, DominionAnchorRole role)
        {
            if (anchors == null)
            {
                return null;
            }

            for (int i = 0; i < anchors.Count; i++)
            {
                Building_AbyssalDominionAnchor anchor = anchors[i];
                if (anchor != null && !anchor.Destroyed && anchor.AnchorRole == role)
                {
                    return anchor;
                }
            }

            return null;
        }

        private static Building_AbyssalDominionAnchor GetAnyAnchor(List<Building_AbyssalDominionAnchor> anchors)
        {
            if (anchors == null)
            {
                return null;
            }

            for (int i = 0; i < anchors.Count; i++)
            {
                Building_AbyssalDominionAnchor anchor = anchors[i];
                if (anchor != null && !anchor.Destroyed)
                {
                    return anchor;
                }
            }

            return null;
        }

        private static string GetCompositionText(ThreatPlan plan)
        {
            if (plan == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            if (plan.PortalCount > 0 && plan.TotalImpCount > 0)
            {
                parts.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_DominionWaveComposition_Imps",
                    "{0} via {1}",
                    GetCountLabel(plan.TotalImpCount, "ABY_CirclePreview_Imp_Singular", "imp", "ABY_CirclePreview_Imp_Plural", "imps"),
                    plan.PortalCount == 1
                        ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveSummary_PortalSingle", "1 portal")
                        : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveSummary_PortalPlural", "{0} portals", plan.PortalCount)));
            }
            else if (plan.TotalImpCount > 0)
            {
                parts.Add(GetCountLabel(plan.TotalImpCount, "ABY_CirclePreview_Imp_Singular", "imp", "ABY_CirclePreview_Imp_Plural", "imps"));
            }

            if (plan.HoundCount > 0)
            {
                parts.Add(GetCountLabel(plan.HoundCount, "ABY_CirclePreview_Hound_Singular", "hound", "ABY_CirclePreview_Hound_Plural", "hounds"));
            }

            if (plan.ZealotCount > 0)
            {
                parts.Add(GetCountLabel(plan.ZealotCount, "ABY_CirclePreview_Zealot_Singular", "zealot", "ABY_CirclePreview_Zealot_Plural", "zealots"));
            }

            if (plan.ThrallCount > 0)
            {
                parts.Add(GetCountLabel(plan.ThrallCount, "ABY_CirclePreview_Thrall_Singular", "thrall", "ABY_CirclePreview_Thrall_Plural", "thralls"));
            }

            if (plan.SniperCount > 0)
            {
                parts.Add(GetCountLabel(plan.SniperCount, "ABY_CirclePreview_Sniper_Singular", "rift sniper", "ABY_CirclePreview_Sniper_Plural", "rift snipers"));
            }

            if (plan.PriestCount > 0)
            {
                parts.Add(GetCountLabel(plan.PriestCount, "ABY_CirclePreview_Priest_Singular", "null priest", "ABY_CirclePreview_Priest_Plural", "null priests"));
            }

            if (plan.HaloHuskCount > 0)
            {
                parts.Add(GetCountLabel(plan.HaloHuskCount, "ABY_CirclePreview_HaloHusk_Singular", "halo husk", "ABY_CirclePreview_HaloHusk_Plural", "halo husks"));
            }

            return parts.Count == 0
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePreviewStatus_Quiet", "quiet")
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
