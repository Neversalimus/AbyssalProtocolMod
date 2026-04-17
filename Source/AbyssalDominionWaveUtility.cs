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
            public int MaxActiveHostiles;
            public int MaxActivePortals;
            public int ActiveAnchorCount;
            public int SuppressionAnchors;
            public int DrainAnchors;
            public int WardAnchors;
            public int BreachAnchors;

            public int TotalUnits => Math.Max(0, TotalImpCount) + Math.Max(0, HoundCount) + Math.Max(0, ThrallCount) + Math.Max(0, PriestCount);
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

            plan.PortalCount = plan.BreachAnchors > 0
                ? (plan.Tier >= 3 ? 2 : 1)
                : (plan.ActiveAnchorCount >= 3 && plan.Tier >= 4 ? 1 : 0);

            if (plan.PortalCount > 0)
            {
                plan.TotalImpCount = 2 + plan.Tier + Mathf.Min(2, plan.BreachAnchors) + Mathf.Min(1, plan.DrainAnchors) + Mathf.Min(crisis.WavesTriggered, 2);
                if (profile.StageTier >= 4)
                {
                    plan.TotalImpCount += 1;
                }

                if (plan.ActiveAnchorCount <= 2)
                {
                    plan.TotalImpCount = Mathf.Max(2, plan.TotalImpCount - 1);
                }
            }

            if (plan.Tier >= 2)
            {
                plan.HoundCount = 1;
            }

            if (plan.WardAnchors > 0 && plan.Tier >= 3)
            {
                plan.HoundCount += 1;
            }

            if (profile.StageTier >= 5)
            {
                plan.HoundCount += 1;
            }

            if (plan.ActiveAnchorCount <= 2)
            {
                plan.HoundCount = Mathf.Max(0, plan.HoundCount - 1);
            }

            if (plan.SuppressionAnchors > 0 && plan.Tier >= 2)
            {
                plan.ThrallCount = 1;
            }

            if (plan.DrainAnchors > 0 && plan.Tier >= 4)
            {
                plan.ThrallCount += 1;
            }

            if (crisis.WavesTriggered >= 3 && plan.Tier >= 5)
            {
                plan.ThrallCount = Mathf.Min(2, plan.ThrallCount + 1);
            }

            if (plan.ActiveAnchorCount <= 1)
            {
                plan.ThrallCount = Mathf.Min(plan.ThrallCount, 1);
            }

            if (plan.Tier >= 4 && (plan.SuppressionAnchors > 0 || plan.WardAnchors > 0 || plan.DrainAnchors > 0))
            {
                plan.PriestCount = 1;
            }

            if (plan.ActiveAnchorCount <= 2)
            {
                plan.PriestCount = 0;
            }

            plan.MaxActiveHostiles = profile.MaxActiveHostiles;
            plan.MaxActivePortals = Mathf.Min(profile.MaxActivePortals, plan.PortalCount >= 2 ? 3 : 2);
            return plan;
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

            PawnKindDef impKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(RiftImpPawnKindDefName);
            PawnKindDef houndKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(EmberHoundPawnKindDefName);
            PawnKindDef thrallKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(HexgunThrallPawnKindDefName);
            PawnKindDef priestKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(NullPriestPawnKindDefName);

            List<string> summaryParts = new List<string>();
            bool anySpawned = false;

            if (plan.PortalCount > 0 && impKind != null)
            {
                int portalsOpened = 0;
                int impsSpawned = 0;
                List<Building_AbyssalDominionAnchor> anchors = crisis.GetLiveAnchors();
                for (int i = 0; i < plan.PortalCount; i++)
                {
                    int remainingPortals = Mathf.Max(1, plan.PortalCount - i);
                    int impsForPulse = Mathf.Clamp(Mathf.CeilToInt((float)(plan.TotalImpCount - impsSpawned) / remainingPortals), 1, Math.Max(1, plan.TotalImpCount - impsSpawned));
                    Building_AbyssalDominionAnchor preferredAnchor = GetPreferredAnchor(anchors, DominionAnchorRole.Breach) ?? GetPreferredAnchor(anchors, DominionAnchorRole.Drain) ?? GetPreferredAnchor(anchors, DominionAnchorRole.Ward) ?? GetAnyAnchor(anchors);
                    IntVec3 origin = preferredAnchor?.PositionHeld ?? crisis.SourceCell;
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
                        List<AbyssalHostileSummonUtility.HostilePackEntry> impEntries = new List<AbyssalHostileSummonUtility.HostilePackEntry>
                        {
                            new AbyssalHostileSummonUtility.HostilePackEntry
                            {
                                KindDef = impKind,
                                Count = impsForPulse
                            }
                        };

                        spawnedPortal = AbyssalHostileSummonUtility.TrySpawnHostilePack(
                            map,
                            impEntries,
                            hostileFaction,
                            origin,
                            AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Imps", "dominion imp pulse"),
                            string.Empty,
                            string.Empty,
                            false,
                            out IntVec3 arrivalCell,
                            out string _);

                        if (spawnedPortal)
                        {
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

                if (portalsOpened > 0)
                {
                    summaryParts.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_DominionWaveSummary_Imps",
                        "{0} via {1}",
                        GetCountLabel(impsSpawned, "ABY_CirclePreview_Imp_Singular", "imp", "ABY_CirclePreview_Imp_Plural", "imps"),
                        portalsOpened == 1
                            ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveSummary_PortalSingle", "1 portal")
                            : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveSummary_PortalPlural", "{0} portals", portalsOpened)));
                }
            }

            if (plan.HoundCount > 0 && houndKind != null)
            {
                Building_AbyssalDominionAnchor houndAnchor = GetPreferredAnchor(crisis.GetLiveAnchors(), DominionAnchorRole.Ward) ?? GetPreferredAnchor(crisis.GetLiveAnchors(), DominionAnchorRole.Breach) ?? GetAnyAnchor(crisis.GetLiveAnchors());
                if (houndAnchor != null)
                {
                    List<AbyssalHostileSummonUtility.HostilePackEntry> houndEntries = new List<AbyssalHostileSummonUtility.HostilePackEntry>
                    {
                        new AbyssalHostileSummonUtility.HostilePackEntry
                        {
                            KindDef = houndKind,
                            Count = plan.HoundCount
                        }
                    };

                    if (AbyssalHostileSummonUtility.TrySpawnHostilePack(
                        map,
                        houndEntries,
                        hostileFaction,
                        houndAnchor.PositionHeld,
                        AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Hounds", "dominion hunter pack"),
                        string.Empty,
                        string.Empty,
                        false,
                        out IntVec3 arrivalCell,
                        out string _))
                    {
                        focusCell = focusCell.IsValid ? focusCell : arrivalCell;
                        anySpawned = true;
                        summaryParts.Add(GetCountLabel(plan.HoundCount, "ABY_CirclePreview_Hound_Singular", "hound", "ABY_CirclePreview_Hound_Plural", "hounds"));
                    }
                }
            }

            if (plan.ThrallCount > 0 && thrallKind != null)
            {
                Building_AbyssalDominionAnchor thrallAnchor = GetPreferredAnchor(crisis.GetLiveAnchors(), DominionAnchorRole.Suppression) ?? GetPreferredAnchor(crisis.GetLiveAnchors(), DominionAnchorRole.Drain) ?? GetAnyAnchor(crisis.GetLiveAnchors());
                if (thrallAnchor != null)
                {
                    List<AbyssalHostileSummonUtility.HostilePackEntry> thrallEntries = new List<AbyssalHostileSummonUtility.HostilePackEntry>
                    {
                        new AbyssalHostileSummonUtility.HostilePackEntry
                        {
                            KindDef = thrallKind,
                            Count = plan.ThrallCount
                        }
                    };

                    if (AbyssalHostileSummonUtility.TrySpawnHostilePack(
                        map,
                        thrallEntries,
                        hostileFaction,
                        thrallAnchor.PositionHeld,
                        AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Thralls", "dominion relay pack"),
                        string.Empty,
                        string.Empty,
                        false,
                        out IntVec3 arrivalCell,
                        out string _))
                    {
                        focusCell = focusCell.IsValid ? focusCell : arrivalCell;
                        anySpawned = true;
                        summaryParts.Add(GetCountLabel(plan.ThrallCount, "ABY_CirclePreview_Thrall_Singular", "thrall", "ABY_CirclePreview_Thrall_Plural", "thralls"));
                    }
                }
            }

            if (plan.PriestCount > 0 && priestKind != null)
            {
                Building_AbyssalDominionAnchor priestAnchor = GetPreferredAnchor(crisis.GetLiveAnchors(), DominionAnchorRole.Suppression)
                    ?? GetPreferredAnchor(crisis.GetLiveAnchors(), DominionAnchorRole.Ward)
                    ?? GetPreferredAnchor(crisis.GetLiveAnchors(), DominionAnchorRole.Drain)
                    ?? GetAnyAnchor(crisis.GetLiveAnchors());

                if (priestAnchor != null)
                {
                    List<AbyssalHostileSummonUtility.HostilePackEntry> priestEntries = new List<AbyssalHostileSummonUtility.HostilePackEntry>
                    {
                        new AbyssalHostileSummonUtility.HostilePackEntry
                        {
                            KindDef = priestKind,
                            Count = plan.PriestCount
                        }
                    };

                    if (AbyssalHostileSummonUtility.TrySpawnHostilePack(
                        map,
                        priestEntries,
                        hostileFaction,
                        priestAnchor.PositionHeld,
                        AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Priests", "dominion null cell"),
                        string.Empty,
                        string.Empty,
                        false,
                        out IntVec3 arrivalCell,
                        out string _))
                    {
                        focusCell = focusCell.IsValid ? focusCell : arrivalCell;
                        anySpawned = true;
                        summaryParts.Add(GetCountLabel(plan.PriestCount, "ABY_CirclePreview_Priest_Singular", "null priest", "ABY_CirclePreview_Priest_Plural", "null priests"));
                    }
                }
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
            int impCount = Mathf.Clamp(3 + tier + Mathf.Min(2, crisis.WavesTriggered / 2), 3, 9);
            int houndCount = tier >= 1 ? 1 + (tier >= 4 ? 1 : 0) : 0;
            int thrallCount = tier >= 2 ? 1 + (tier >= 5 ? 1 : 0) : 0;
            int priestCount = tier >= 4 ? 1 : 0;

            PawnKindDef impKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(RiftImpPawnKindDefName);
            PawnKindDef houndKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(EmberHoundPawnKindDefName);
            PawnKindDef thrallKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(HexgunThrallPawnKindDefName);
            PawnKindDef priestKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(NullPriestPawnKindDefName);

            bool anySpawned = false;
            List<string> summaryParts = new List<string>();

            if (impKind != null)
            {
                bool spawnedPortal = ABY_Phase2PortalUtility.TrySpawnImpPortalNear(
                    map,
                    hostileFaction,
                    origin,
                    PortalMinRadius,
                    PortalMaxRadius + 1.8f,
                    impCount,
                    PortalWarmupBaseTicks + 12,
                    PortalSpawnIntervalFastTicks,
                    PortalLingerBaseTicks + 50,
                    out Building_AbyssalImpPortal portal);

                if (!spawnedPortal)
                {
                    List<AbyssalHostileSummonUtility.HostilePackEntry> impEntries = new List<AbyssalHostileSummonUtility.HostilePackEntry>
                    {
                        new AbyssalHostileSummonUtility.HostilePackEntry
                        {
                            KindDef = impKind,
                            Count = impCount
                        }
                    };

                    spawnedPortal = AbyssalHostileSummonUtility.TrySpawnHostilePack(
                        map,
                        impEntries,
                        hostileFaction,
                        origin,
                        AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Imps", "dominion imp pulse"),
                        string.Empty,
                        string.Empty,
                        false,
                        out IntVec3 arrivalCell,
                        out string _);

                    if (spawnedPortal)
                    {
                        focusCell = arrivalCell;
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
                        GetCountLabel(impCount, "ABY_CirclePreview_Imp_Singular", "imp", "ABY_CirclePreview_Imp_Plural", "imps"),
                        AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWaveSummary_PortalSingle", "1 portal")));
                }
            }

            if (houndCount > 0 && houndKind != null)
            {
                List<AbyssalHostileSummonUtility.HostilePackEntry> entries = new List<AbyssalHostileSummonUtility.HostilePackEntry>
                {
                    new AbyssalHostileSummonUtility.HostilePackEntry
                    {
                        KindDef = houndKind,
                        Count = houndCount
                    }
                };

                if (AbyssalHostileSummonUtility.TrySpawnHostilePack(
                    map,
                    entries,
                    hostileFaction,
                    origin,
                    AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Hounds", "dominion hunter pack"),
                    string.Empty,
                    string.Empty,
                    false,
                    out IntVec3 arrivalCell,
                    out string _))
                {
                    focusCell = focusCell.IsValid ? focusCell : arrivalCell;
                    anySpawned = true;
                    summaryParts.Add(GetCountLabel(houndCount, "ABY_CirclePreview_Hound_Singular", "hound", "ABY_CirclePreview_Hound_Plural", "hounds"));
                }
            }

            if (thrallCount > 0 && thrallKind != null)
            {
                List<AbyssalHostileSummonUtility.HostilePackEntry> entries = new List<AbyssalHostileSummonUtility.HostilePackEntry>
                {
                    new AbyssalHostileSummonUtility.HostilePackEntry
                    {
                        KindDef = thrallKind,
                        Count = thrallCount
                    }
                };

                if (AbyssalHostileSummonUtility.TrySpawnHostilePack(
                    map,
                    entries,
                    hostileFaction,
                    origin,
                    AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Thralls", "dominion relay pack"),
                    string.Empty,
                    string.Empty,
                    false,
                    out IntVec3 arrivalCell,
                    out string _))
                {
                    focusCell = focusCell.IsValid ? focusCell : arrivalCell;
                    anySpawned = true;
                    summaryParts.Add(GetCountLabel(thrallCount, "ABY_CirclePreview_Thrall_Singular", "thrall", "ABY_CirclePreview_Thrall_Plural", "thralls"));
                }
            }

            if (priestCount > 0 && priestKind != null)
            {
                List<AbyssalHostileSummonUtility.HostilePackEntry> entries = new List<AbyssalHostileSummonUtility.HostilePackEntry>
                {
                    new AbyssalHostileSummonUtility.HostilePackEntry
                    {
                        KindDef = priestKind,
                        Count = priestCount
                    }
                };

                if (AbyssalHostileSummonUtility.TrySpawnHostilePack(
                    map,
                    entries,
                    hostileFaction,
                    origin,
                    AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWavePack_Priests", "dominion null cell"),
                    string.Empty,
                    string.Empty,
                    false,
                    out IntVec3 arrivalCell,
                    out string _))
                {
                    focusCell = focusCell.IsValid ? focusCell : arrivalCell;
                    anySpawned = true;
                    summaryParts.Add(GetCountLabel(priestCount, "ABY_CirclePreview_Priest_Singular", "null priest", "ABY_CirclePreview_Priest_Plural", "null priests"));
                }
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

        public static int CountActiveAbyssalHostiles(Map map)
        {
            if (map?.mapPawns == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    continue;
                }

                string defName = pawn.def?.defName;
                if (defName == RiftImpPawnKindDefName || defName == EmberHoundPawnKindDefName || defName == HexgunThrallPawnKindDefName || defName == NullPriestPawnKindDefName)
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

            if (plan.HoundCount > 0)
            {
                parts.Add(GetCountLabel(plan.HoundCount, "ABY_CirclePreview_Hound_Singular", "hound", "ABY_CirclePreview_Hound_Plural", "hounds"));
            }

            if (plan.ThrallCount > 0)
            {
                parts.Add(GetCountLabel(plan.ThrallCount, "ABY_CirclePreview_Thrall_Singular", "thrall", "ABY_CirclePreview_Thrall_Plural", "thralls"));
            }

            if (plan.PriestCount > 0)
            {
                parts.Add(GetCountLabel(plan.PriestCount, "ABY_CirclePreview_Priest_Singular", "null priest", "ABY_CirclePreview_Priest_Plural", "null priests"));
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
