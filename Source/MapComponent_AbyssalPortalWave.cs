using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_AbyssalPortalWave : MapComponent
    {
        private sealed class PortalWaveRequest : IExposable
        {
            public PawnKindDef PawnKindDef;
            public int SpawnCount;
            public int WarmupTicks;
            public int SpawnIntervalTicks;
            public int LingerTicks;
            public int DelayAfterTicks = PortalCadenceTicks;
            public bool PreferPerimeter;
            public int FrontIndex = -1;
            public int PhaseIndex = -1;
            public string PhaseId = string.Empty;
            public bool RequiresCommandGate;
            public bool PreferCommandGate;
            public bool ConsumesCommandBurst;
            public string AssignedFrontRoleId = string.Empty;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref PawnKindDef, "pawnKindDef");
                Scribe_Values.Look(ref SpawnCount, "spawnCount", 1);
                Scribe_Values.Look(ref WarmupTicks, "warmupTicks", 150);
                Scribe_Values.Look(ref SpawnIntervalTicks, "spawnIntervalTicks", 24);
                Scribe_Values.Look(ref LingerTicks, "lingerTicks", 180);
                Scribe_Values.Look(ref DelayAfterTicks, "delayAfterTicks", PortalCadenceTicks);
                Scribe_Values.Look(ref PreferPerimeter, "preferPerimeter", false);
                Scribe_Values.Look(ref FrontIndex, "frontIndex", -1);
                Scribe_Values.Look(ref PhaseIndex, "phaseIndex", -1);
                Scribe_Values.Look(ref PhaseId, "phaseId", string.Empty);
                Scribe_Values.Look(ref RequiresCommandGate, "requiresCommandGate", false);
                Scribe_Values.Look(ref PreferCommandGate, "preferCommandGate", false);
                Scribe_Values.Look(ref ConsumesCommandBurst, "consumesCommandBurst", false);
                Scribe_Values.Look(ref AssignedFrontRoleId, "assignedFrontRoleId", string.Empty);
            }
        }

        private const string ImpPortalDefName = "ABY_ImpPortal";
        private const string CommandGateDefName = "ABY_HordeCommandGate";
        private const string RiftImpPawnKindDefName = "ABY_RiftImp";

        private const int PortalCadenceTicks = 48;
        private const int RetryCadenceTicks = 24;
        private const float UsedPortalMinSeparation = 11.9f;
        private const float HordeUsedPortalMinSeparation = 13.9f;
        private const float UnsafeBaseRadius = 9f;
        private const float LocalBuildingBlockRadius = 2.9f;
        private const float HordeUnsafeBaseRadius = 18f;
        private const float HordeLocalBuildingBlockRadius = 5.9f;
        private const float HordeFrontAnchorMinSeparation = 28f;
        private const float HordeFrontRadius = 16f;
        private const int HordePerimeterBand = 14;

        private const float EmberDoubleSpawnChance = 0.25f;
        private const float BonusImpPortalChance = 0.20f;
        private const int EmberSingleSpawnCount = 1;
        private const int EmberDoubleSpawnCount = 2;
        private const int BonusImpMinSpawnCount = 2;
        private const int BonusImpMaxSpawnCount = 4;

        private List<PortalWaveRequest> queuedPortals = new List<PortalWaveRequest>();
        private List<IntVec3> usedPortalCells = new List<IntVec3>();
        private List<IntVec3> frontAnchorCells = new List<IntVec3>();
        private Faction waveFaction;
        private int nextPortalOpenTick = -1;
        private bool activeWavePrefersPerimeter;
        private Building_AbyssalHordeCommandGate activeCommandGate;
        private int activeCommandFrontIndex = -1;
        private bool commandGateCollapsed;
        private IntVec3 playerStrongholdCenter = IntVec3.Invalid;
        private bool activeHordeWave;
        private bool closureRewardPending;
        private bool commandRewardGranted;
        private IntVec3 lastOpenedPortalCell = IntVec3.Invalid;
        private AbyssalHordeRewardUtility.RewardSnapshot activeHordeRewardSnapshot;

        public MapComponent_AbyssalPortalWave(Map map) : base(map)
        {
        }

        public bool IsWaveActive => (queuedPortals != null && queuedPortals.Count > 0) || nextPortalOpenTick >= 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref queuedPortals, "queuedPortals", LookMode.Deep);
            Scribe_Collections.Look(ref usedPortalCells, "usedPortalCells", LookMode.Value);
            Scribe_Collections.Look(ref frontAnchorCells, "frontAnchorCells", LookMode.Value);
            Scribe_References.Look(ref waveFaction, "waveFaction");
            Scribe_Values.Look(ref nextPortalOpenTick, "nextPortalOpenTick", -1);
            Scribe_Values.Look(ref activeWavePrefersPerimeter, "activeWavePrefersPerimeter", false);
            Scribe_References.Look(ref activeCommandGate, "activeCommandGate");
            Scribe_Values.Look(ref activeCommandFrontIndex, "activeCommandFrontIndex", -1);
            Scribe_Values.Look(ref commandGateCollapsed, "commandGateCollapsed", false);
            Scribe_Values.Look(ref playerStrongholdCenter, "playerStrongholdCenter", IntVec3.Invalid);
            Scribe_Values.Look(ref activeHordeWave, "activeHordeWave", false);
            Scribe_Values.Look(ref closureRewardPending, "closureRewardPending", false);
            Scribe_Values.Look(ref commandRewardGranted, "commandRewardGranted", false);
            Scribe_Values.Look(ref lastOpenedPortalCell, "lastOpenedPortalCell", IntVec3.Invalid);
            Scribe_Deep.Look(ref activeHordeRewardSnapshot, "activeHordeRewardSnapshot");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                queuedPortals ??= new List<PortalWaveRequest>();
                usedPortalCells ??= new List<IntVec3>();
                frontAnchorCells ??= new List<IntVec3>();
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (!IsWaveActive || map == null || Find.TickManager == null)
            {
                return;
            }

            if (queuedPortals == null || queuedPortals.Count == 0)
            {
                ResetWave();
                return;
            }

            if (Find.TickManager.TicksGame < nextPortalOpenTick)
            {
                return;
            }

            if (TryOpenNextPortal(out _, out int delayAfterTicks))
            {
                nextPortalOpenTick = queuedPortals.Count > 0 ? Find.TickManager.TicksGame + Mathf.Max(8, delayAfterTicks) : -1;
                if (queuedPortals.Count == 0)
                {
                    ResetWave();
                }
            }
            else
            {
                nextPortalOpenTick = Find.TickManager.TicksGame + RetryCadenceTicks;
            }
        }

        public bool TryBeginEmberPortalWave(
            Faction faction,
            PawnKindDef emberPawnKind,
            int emberPortalWarmupTicks,
            int emberSpawnIntervalTicks,
            int emberPortalLingerTicks,
            out IntVec3 firstPortalCell,
            out string failReason)
        {
            firstPortalCell = IntVec3.Invalid;
            failReason = null;

            if (map == null)
            {
                failReason = "No map available for ember portal wave.";
                return false;
            }

            if (faction == null)
            {
                failReason = "ABY_CircleFail_NoHostileFaction".Translate();
                return false;
            }

            if (emberPawnKind == null)
            {
                failReason = "Missing Ember Hound pawn definition.";
                return false;
            }

            if (IsWaveActive)
            {
                failReason = "ABY_BossSummonFail_EncounterActive".Translate();
                return false;
            }

            PawnKindDef bonusImpKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(RiftImpPawnKindDefName);
            int activeColonists = Mathf.Max(1, ABY_Phase2PortalUtility.CountActivePlayerColonists(map));

            queuedPortals = BuildPortalQueue(
                activeColonists,
                emberPawnKind,
                bonusImpKind,
                Mathf.Max(90, emberPortalWarmupTicks),
                Mathf.Max(14, emberSpawnIntervalTicks),
                Mathf.Max(120, emberPortalLingerTicks));

            if (queuedPortals.Count == 0)
            {
                failReason = "ABY_CircleFail_NoPortalSpawn".Translate();
                return false;
            }

            usedPortalCells = new List<IntVec3>();
            frontAnchorCells = new List<IntVec3>();
            playerStrongholdCenter = ResolvePlayerStrongholdCenter();
            waveFaction = faction;
            activeWavePrefersPerimeter = false;
            activeHordeWave = false;
            closureRewardPending = false;
            commandRewardGranted = false;
            lastOpenedPortalCell = IntVec3.Invalid;
            activeHordeRewardSnapshot = null;
            nextPortalOpenTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (!TryOpenNextPortal(out firstPortalCell, out int initialDelay))
            {
                ResetWave();
                failReason = "ABY_CircleFail_NoPortalSpawn".Translate();
                return false;
            }

            nextPortalOpenTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + Mathf.Max(8, initialDelay);
            Messages.Message(
                "ABY_CircleEmberHoundLetterDesc".Translate(),
                new TargetInfo(firstPortalCell, map),
                MessageTypeDefOf.ThreatBig);
            return true;
        }

        public bool TryBeginHordePortalWave(
            Faction faction,
            AbyssalHordeSigilUtility.HordePlan hordePlan,
            int baseWarmupTicks,
            int baseSpawnIntervalTicks,
            int baseLingerTicks,
            out IntVec3 firstPortalCell,
            out string failReason)
        {
            firstPortalCell = IntVec3.Invalid;
            failReason = null;

            if (map == null)
            {
                failReason = "No map available for horde portal wave.";
                return false;
            }

            if (faction == null)
            {
                failReason = "ABY_CircleFail_NoHostileFaction".Translate();
                return false;
            }

            if (IsWaveActive)
            {
                failReason = "ABY_BossSummonFail_EncounterActive".Translate();
                return false;
            }

            AbyssalHordeSigilUtility.HordePlan resolvedPlan = hordePlan ?? AbyssalHordeSigilUtility.GetHordePlan(map);
            if (resolvedPlan == null || resolvedPlan.PulsePlans == null || resolvedPlan.PulsePlans.Count == 0)
            {
                failReason = "ABY_CircleFail_NoPortalSpawn".Translate();
                return false;
            }

            playerStrongholdCenter = ResolvePlayerStrongholdCenter();
            if (!TryBuildFrontAnchors(resolvedPlan, out List<IntVec3> anchors))
            {
                failReason = "ABY_CircleFail_NoPortalSpawn".Translate();
                return false;
            }

            queuedPortals = BuildHordePortalQueue(
                resolvedPlan,
                Mathf.Max(90, baseWarmupTicks),
                Mathf.Max(10, baseSpawnIntervalTicks),
                Mathf.Max(120, baseLingerTicks));

            if (queuedPortals.Count == 0)
            {
                failReason = "ABY_CircleFail_NoPortalSpawn".Translate();
                return false;
            }

            usedPortalCells = new List<IntVec3>();
            frontAnchorCells = anchors ?? new List<IntVec3>();
            waveFaction = faction;
            activeWavePrefersPerimeter = true;
            activeHordeWave = true;
            closureRewardPending = false;
            commandRewardGranted = false;
            lastOpenedPortalCell = IntVec3.Invalid;
            activeHordeRewardSnapshot = AbyssalHordeRewardUtility.BuildSnapshot(resolvedPlan);
            activeCommandFrontIndex = ResolveCommandFrontIndex(resolvedPlan, frontAnchorCells?.Count ?? 0);
            commandGateCollapsed = false;
            activeCommandGate = null;
            if (resolvedPlan.UsesCommandGate)
            {
                TrySpawnCommandGate(resolvedPlan, activeCommandFrontIndex);
            }
            nextPortalOpenTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (!TryOpenNextPortal(out firstPortalCell, out int initialDelay))
            {
                ResetWave();
                failReason = "ABY_CircleFail_NoPortalSpawn".Translate();
                return false;
            }

            nextPortalOpenTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + Mathf.Max(8, initialDelay);
            Messages.Message(
                AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_HordeOperation_Begin",
                    "{0} stabilizes: {1}. {2}.",
                    AbyssalHordeSigilUtility.GetDoctrineLabel(resolvedPlan),
                    AbyssalHordeSigilUtility.GetOperationBulletin(resolvedPlan),
                    AbyssalHordeSigilUtility.GetDoctrineWarning(resolvedPlan)),
                new TargetInfo(firstPortalCell, map),
                MessageTypeDefOf.ThreatBig);
            return true;
        }

        private List<PortalWaveRequest> BuildPortalQueue(
            int activeColonists,
            PawnKindDef emberPawnKind,
            PawnKindDef bonusImpKind,
            int emberPortalWarmupTicks,
            int emberSpawnIntervalTicks,
            int emberPortalLingerTicks)
        {
            List<PortalWaveRequest> requests = new List<PortalWaveRequest>();

            int bonusImpWarmupTicks = Mathf.Max(75, Mathf.RoundToInt(emberPortalWarmupTicks * 0.72f));
            int bonusImpSpawnIntervalTicks = Mathf.Max(10, emberSpawnIntervalTicks - 8);
            int bonusImpLingerTicks = Mathf.Max(90, emberPortalLingerTicks - 30);

            for (int i = 0; i < activeColonists; i++)
            {
                requests.Add(new PortalWaveRequest
                {
                    PawnKindDef = emberPawnKind,
                    SpawnCount = Rand.Chance(EmberDoubleSpawnChance) ? EmberDoubleSpawnCount : EmberSingleSpawnCount,
                    WarmupTicks = emberPortalWarmupTicks,
                    SpawnIntervalTicks = emberSpawnIntervalTicks,
                    LingerTicks = emberPortalLingerTicks,
                    PreferPerimeter = false,
                    FrontIndex = -1
                });

                if (bonusImpKind != null && Rand.Chance(BonusImpPortalChance))
                {
                    requests.Add(new PortalWaveRequest
                    {
                        PawnKindDef = bonusImpKind,
                        SpawnCount = Rand.RangeInclusive(BonusImpMinSpawnCount, BonusImpMaxSpawnCount),
                        WarmupTicks = bonusImpWarmupTicks,
                        SpawnIntervalTicks = bonusImpSpawnIntervalTicks,
                        LingerTicks = bonusImpLingerTicks,
                        PreferPerimeter = false,
                        FrontIndex = -1,
                        DelayAfterTicks = PortalCadenceTicks,
                        PhaseIndex = -1,
                        PhaseId = string.Empty
                    });
                }
            }

            Shuffle(requests);
            return requests;
        }

        private List<PortalWaveRequest> BuildHordePortalQueue(
            AbyssalHordeSigilUtility.HordePlan hordePlan,
            int baseWarmupTicks,
            int baseSpawnIntervalTicks,
            int baseLingerTicks)
        {
            List<PortalWaveRequest> requests = new List<PortalWaveRequest>();
            if (hordePlan == null)
            {
                return requests;
            }

            List<AbyssalHordeSigilUtility.HordePhasePlan> phases = hordePlan.Phases;
            if (phases == null || phases.Count == 0)
            {
                AbyssalHordeSigilUtility.HordePhasePlan fallbackPhase = new AbyssalHordeSigilUtility.HordePhasePlan
                {
                    PhaseId = "lattice",
                    SequenceIndex = 0,
                    FrontCount = Mathf.Max(1, hordePlan.FrontCount),
                    TotalBudget = hordePlan.TotalBudget,
                    PulsePlans = new List<AbyssalEncounterDirectorUtility.EncounterPlan>(hordePlan.PulsePlans ?? new List<AbyssalEncounterDirectorUtility.EncounterPlan>())
                };
                phases = new List<AbyssalHordeSigilUtility.HordePhasePlan> { fallbackPhase };
            }

            int overallFrontCount = Mathf.Max(1, hordePlan.FrontCount);
            int commandFrontIndex = ResolveCommandFrontIndex(hordePlan, overallFrontCount);
            int remainingCommandBursts = hordePlan.UsesCommandGate ? Mathf.Max(0, hordePlan.CommandGateReservedBursts) : 0;
            int phaseFrontBase = 0;
            Dictionary<int, int> frontLoad = new Dictionary<int, int>();

            for (int phaseIndex = 0; phaseIndex < phases.Count; phaseIndex++)
            {
                AbyssalHordeSigilUtility.HordePhasePlan phase = phases[phaseIndex];
                if (phase?.PulsePlans == null || phase.PulsePlans.Count == 0)
                {
                    continue;
                }

                int phaseFrontSpan = GetPhaseFrontSpan(phase, overallFrontCount);
                List<int> allowedFrontIndices = BuildPhaseFrontIndices(phaseFrontBase, phaseFrontSpan, overallFrontCount);
                int phaseBaseWarmup = GetPhaseWarmupTicks(phase, baseWarmupTicks);
                int phaseBaseInterval = GetPhaseSpawnIntervalTicks(phase, baseSpawnIntervalTicks);
                int phaseBaseLinger = GetPhaseLingerTicks(phase, baseLingerTicks);

                for (int pulseIndex = 0; pulseIndex < phase.PulsePlans.Count; pulseIndex++)
                {
                    AbyssalEncounterDirectorUtility.EncounterPlan pulsePlan = phase.PulsePlans[pulseIndex];
                    if (pulsePlan?.Entries == null || pulsePlan.Entries.Count == 0)
                    {
                        continue;
                    }

                    List<AbyssalEncounterDirectorUtility.DirectedEntry> entries = new List<AbyssalEncounterDirectorUtility.DirectedEntry>(pulsePlan.Entries);
                    SortDirectedEntriesForHorde(entries);

                    int pulseWarmup = Mathf.Max(72, phaseBaseWarmup - pulseIndex * (phase.IsSurgePhase ? 8 : 4));
                    int pulseInterval = Mathf.Max(8, phaseBaseInterval - Mathf.Min(4, pulseIndex));
                    int pulseLinger = Mathf.Max(90, phaseBaseLinger + pulseIndex * (phase.IsSurgePhase ? 22 : 12));
                    int pulseStartIndex = requests.Count;
                    int localFrontCursor = 0;

                    for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                    {
                        AbyssalEncounterDirectorUtility.DirectedEntry entry = entries[entryIndex];
                        if (entry?.KindDef == null || entry.Count <= 0)
                        {
                            continue;
                        }

                        int remaining = entry.Count;
                        int chunkSize = Mathf.Max(1, GetPortalChunkSize(entry.KindDef.defName));
                        if (phase.IsSurgePhase && string.Equals(entry.KindDef.defName, "ABY_RiftImp", StringComparison.OrdinalIgnoreCase))
                        {
                            chunkSize = Mathf.Max(3, chunkSize - 2);
                        }

                        string desiredFrontRole = AbyssalHordeSigilUtility.ResolveEntryFrontRoleId(entry.KindDef.defName, entry.Role);
                        while (remaining > 0)
                        {
                            int spawnCount = Mathf.Min(remaining, chunkSize);
                            int selectedFront = SelectFrontIndexForRole(hordePlan, allowedFrontIndices, desiredFrontRole, frontLoad, localFrontCursor);
                            requests.Add(new PortalWaveRequest
                            {
                                PawnKindDef = entry.KindDef,
                                SpawnCount = spawnCount,
                                WarmupTicks = pulseWarmup,
                                SpawnIntervalTicks = pulseInterval,
                                LingerTicks = pulseLinger,
                                DelayAfterTicks = PortalCadenceTicks,
                                PreferPerimeter = true,
                                FrontIndex = selectedFront,
                                PhaseIndex = phase.SequenceIndex,
                                PhaseId = phase.PhaseId ?? string.Empty,
                                AssignedFrontRoleId = desiredFrontRole ?? string.Empty
                            });

                            frontLoad[selectedFront] = frontLoad.TryGetValue(selectedFront, out int existing) ? existing + spawnCount : spawnCount;
                            localFrontCursor++;
                            remaining -= spawnCount;
                        }
                    }

                    if (ShouldAppendCommandBurst(hordePlan, phase, pulseIndex, remainingCommandBursts))
                    {
                        AppendCommandBurstRequests(
                            requests,
                            hordePlan,
                            phase,
                            commandFrontIndex,
                            ref remainingCommandBursts,
                            Mathf.Max(64, pulseWarmup - 10),
                            Mathf.Max(8, pulseInterval - 1),
                            Mathf.Max(110, pulseLinger + 20));
                    }

                    if (requests.Count > pulseStartIndex)
                    {
                        bool isLastPulseInPhase = pulseIndex >= phase.PulsePlans.Count - 1;
                        bool hasNextPhase = phaseIndex < phases.Count - 1;
                        requests[requests.Count - 1].DelayAfterTicks = GetDelayAfterPulse(phase, isLastPulseInPhase, hasNextPhase);
                    }
                }

                phaseFrontBase = (phaseFrontBase + Mathf.Max(1, phaseFrontSpan)) % overallFrontCount;
            }

            return requests;
        }

        private static List<int> BuildPhaseFrontIndices(int phaseFrontBase, int phaseFrontSpan, int overallFrontCount)
        {
            List<int> indices = new List<int>();
            int span = Mathf.Clamp(phaseFrontSpan, 1, Math.Max(1, overallFrontCount));
            for (int i = 0; i < span; i++)
            {
                indices.Add((phaseFrontBase + i) % Math.Max(1, overallFrontCount));
            }

            return indices;
        }

        private static int SelectFrontIndexForRole(
            AbyssalHordeSigilUtility.HordePlan hordePlan,
            List<int> allowedFrontIndices,
            string desiredFrontRole,
            Dictionary<int, int> frontLoad,
            int fallbackCursor)
        {
            if (allowedFrontIndices == null || allowedFrontIndices.Count == 0)
            {
                return 0;
            }

            int bestFront = -1;
            int bestLoad = int.MaxValue;
            for (int i = 0; i < allowedFrontIndices.Count; i++)
            {
                int frontIndex = allowedFrontIndices[i];
                AbyssalHordeSigilUtility.FrontDirective directive = AbyssalHordeSigilUtility.GetFrontDirective(hordePlan, frontIndex);
                if (directive == null || !string.Equals(directive.RoleId ?? string.Empty, desiredFrontRole ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int load = frontLoad != null && frontLoad.TryGetValue(frontIndex, out int existing) ? existing : 0;
                if (load < bestLoad)
                {
                    bestLoad = load;
                    bestFront = frontIndex;
                }
            }

            if (bestFront >= 0)
            {
                return bestFront;
            }

            bestFront = allowedFrontIndices[Mathf.Abs(fallbackCursor) % allowedFrontIndices.Count];
            bestLoad = frontLoad != null && frontLoad.TryGetValue(bestFront, out int fallbackLoad) ? fallbackLoad : 0;
            for (int i = 0; i < allowedFrontIndices.Count; i++)
            {
                int frontIndex = allowedFrontIndices[i];
                int load = frontLoad != null && frontLoad.TryGetValue(frontIndex, out int existing) ? existing : 0;
                if (load < bestLoad)
                {
                    bestLoad = load;
                    bestFront = frontIndex;
                }
            }

            return bestFront;
        }

        private static int ResolveCommandFrontIndex(AbyssalHordeSigilUtility.HordePlan hordePlan, int frontCount)
        {
            return Mathf.Clamp(AbyssalHordeSigilUtility.ResolveCommandFrontIndex(hordePlan), 0, Math.Max(0, frontCount - 1));
        }

        private static bool ShouldAppendCommandBurst(AbyssalHordeSigilUtility.HordePlan hordePlan, AbyssalHordeSigilUtility.HordePhasePlan phase, int pulseIndex, int remainingCommandBursts)
        {
            if (hordePlan == null || !hordePlan.UsesCommandGate || phase == null || remainingCommandBursts <= 0)
            {
                return false;
            }

            if (string.Equals(phase.PhaseId, "lattice", StringComparison.OrdinalIgnoreCase))
            {
                if (pulseIndex == 0)
                {
                    return true;
                }

                bool lastPulse = pulseIndex >= Math.Max(0, phase.PulseCount - 1);
                if (lastPulse && remainingCommandBursts > 1 && (hordePlan.Band >= 2 || hordePlan.HasSurgePhase))
                {
                    return true;
                }
            }

            if (phase.IsSurgePhase && pulseIndex == 0)
            {
                return true;
            }

            return false;
        }

        private void AppendCommandBurstRequests(
            List<PortalWaveRequest> requests,
            AbyssalHordeSigilUtility.HordePlan hordePlan,
            AbyssalHordeSigilUtility.HordePhasePlan phase,
            int commandFrontIndex,
            ref int remainingCommandBursts,
            int warmupTicks,
            int spawnIntervalTicks,
            int lingerTicks)
        {
            if (requests == null || hordePlan == null || phase == null || remainingCommandBursts <= 0)
            {
                return;
            }

            List<(string defName, int count)> burstEntries = GetCommandBurstEntries(hordePlan, remainingCommandBursts);
            if (burstEntries == null || burstEntries.Count == 0)
            {
                return;
            }

            int beforeCount = requests.Count;
            for (int i = 0; i < burstEntries.Count; i++)
            {
                QueueCommandEntryRequests(
                    requests,
                    burstEntries[i].defName,
                    burstEntries[i].count,
                    commandFrontIndex,
                    phase,
                    warmupTicks,
                    spawnIntervalTicks,
                    lingerTicks);
            }

            if (requests.Count > beforeCount)
            {
                requests[beforeCount].ConsumesCommandBurst = true;
                requests[requests.Count - 1].DelayAfterTicks = Mathf.Max(30, Mathf.RoundToInt(PortalCadenceTicks * 0.82f));
                remainingCommandBursts--;
            }
        }

        private void QueueCommandEntryRequests(
            List<PortalWaveRequest> requests,
            string pawnKindDefName,
            int totalCount,
            int commandFrontIndex,
            AbyssalHordeSigilUtility.HordePhasePlan phase,
            int warmupTicks,
            int spawnIntervalTicks,
            int lingerTicks)
        {
            if (requests == null || pawnKindDefName.NullOrEmpty() || totalCount <= 0)
            {
                return;
            }

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(pawnKindDefName);
            if (kindDef == null)
            {
                return;
            }

            int remaining = totalCount;
            int chunkSize = Mathf.Max(1, GetPortalChunkSize(pawnKindDefName));
            while (remaining > 0)
            {
                int spawnCount = Mathf.Min(remaining, chunkSize);
                requests.Add(new PortalWaveRequest
                {
                    PawnKindDef = kindDef,
                    SpawnCount = spawnCount,
                    WarmupTicks = Mathf.Max(60, warmupTicks),
                    SpawnIntervalTicks = Mathf.Max(8, spawnIntervalTicks),
                    LingerTicks = Mathf.Max(100, lingerTicks),
                    DelayAfterTicks = Mathf.Max(30, Mathf.RoundToInt(PortalCadenceTicks * 0.82f)),
                    PreferPerimeter = true,
                    FrontIndex = Mathf.Max(0, commandFrontIndex),
                    PhaseIndex = phase.SequenceIndex,
                    PhaseId = phase.PhaseId ?? string.Empty,
                    RequiresCommandGate = true,
                    PreferCommandGate = true,
                    AssignedFrontRoleId = AbyssalHordeSigilUtility.ResolveEntryFrontRoleId(pawnKindDefName, null)
                });

                remaining -= spawnCount;
            }
        }

        private static List<(string defName, int count)> GetCommandBurstEntries(AbyssalHordeSigilUtility.HordePlan hordePlan, int remainingCommandBursts)
        {
            List<(string defName, int count)> entries = new List<(string defName, int count)>();
            if (hordePlan == null)
            {
                return entries;
            }

            int band = Mathf.Clamp(hordePlan.Band, 0, 3);
            int difficultyOrder = AbyssalDifficultyUtility.GetCurrentProfileOrder();
            string doctrine = hordePlan.PrimaryDoctrineDefName ?? string.Empty;

            if (string.Equals(doctrine, "ABY_Doctrine_HordeFireline", StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(("ABY_HexgunThrall", 2 + band));
                if (band >= 2 || difficultyOrder >= 2) entries.Add(("ABY_RiftSapper", 1));
                entries.Add(("ABY_RiftSniper", 1));
                if (band >= 1 || difficultyOrder >= 2) entries.Add(("ABY_NullPriest", 1));
                if (band >= 2 || difficultyOrder >= 3) entries.Add(("ABY_HaloHusk", 1));
                return entries;
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeGrinder", StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(("ABY_ChainZealot", 2 + band));
                entries.Add(("ABY_EmberHound", 2 + Mathf.Max(0, band - 1)));
                if (band >= 2 || difficultyOrder >= 2) entries.Add(("ABY_RiftSapper", 1));
                if (band >= 1 || difficultyOrder >= 1) entries.Add(("ABY_BreachBrute", 1));
                if (band >= 2 || difficultyOrder >= 2) entries.Add(("ABY_Harvester", 1));
                return entries;
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeSiege", StringComparison.OrdinalIgnoreCase))
            {
                entries.Add(("ABY_HexgunThrall", 2 + band));
                entries.Add(("ABY_RiftSapper", 1));
                entries.Add(("ABY_HaloHusk", 1));
                if (band >= 1 || difficultyOrder >= 2) entries.Add(("ABY_BreachBrute", 1));
                if (remainingCommandBursts <= 1 && (band >= 3 || difficultyOrder >= 3)) entries.Add(("ABY_SiegeIdol", 1));
                return entries;
            }

            entries.Add(("ABY_RiftImp", 4 + band * 2));
            entries.Add(("ABY_EmberHound", 2 + band));
            if (band >= 2 || difficultyOrder >= 2) entries.Add(("ABY_RiftSapper", 1));
            entries.Add(("ABY_ChainZealot", 1 + (band >= 2 ? 1 : 0)));
            if (band >= 2 || difficultyOrder >= 2) entries.Add(("ABY_Harvester", 1));
            return entries;
        }

        private static int GetPhaseFrontSpan(AbyssalHordeSigilUtility.HordePhasePlan phase, int overallFrontCount)
        {
            if (phase == null)
            {
                return Math.Max(1, overallFrontCount);
            }

            if (phase.IsSurgePhase)
            {
                return Mathf.Clamp(phase.FrontCount > 0 ? phase.FrontCount : Mathf.Clamp(Mathf.CeilToInt(overallFrontCount * 0.5f), 1, 2), 1, overallFrontCount);
            }

            if (phase.IsCollapsePhase)
            {
                return Mathf.Clamp(phase.FrontCount > 0 ? phase.FrontCount : Math.Max(1, overallFrontCount - 2), 1, overallFrontCount);
            }

            return Mathf.Clamp(phase.FrontCount > 0 ? phase.FrontCount : overallFrontCount, 1, overallFrontCount);
        }

        private static int GetPhaseWarmupTicks(AbyssalHordeSigilUtility.HordePhasePlan phase, int baseWarmupTicks)
        {
            if (phase == null)
            {
                return Mathf.Max(90, baseWarmupTicks);
            }

            if (string.Equals(phase.PhaseId, "marking", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Max(96, baseWarmupTicks + 28);
            }

            if (phase.IsSurgePhase)
            {
                return Mathf.Max(72, baseWarmupTicks - 14);
            }

            if (phase.IsCollapsePhase)
            {
                return Mathf.Max(90, baseWarmupTicks + 8);
            }

            return Mathf.Max(84, baseWarmupTicks);
        }

        private static int GetPhaseSpawnIntervalTicks(AbyssalHordeSigilUtility.HordePhasePlan phase, int baseSpawnIntervalTicks)
        {
            if (phase == null)
            {
                return Mathf.Max(10, baseSpawnIntervalTicks);
            }

            if (string.Equals(phase.PhaseId, "marking", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Max(10, baseSpawnIntervalTicks + 4);
            }

            if (phase.IsSurgePhase)
            {
                return Mathf.Max(8, baseSpawnIntervalTicks - 3);
            }

            if (phase.IsCollapsePhase)
            {
                return Mathf.Max(10, baseSpawnIntervalTicks + 2);
            }

            return Mathf.Max(9, baseSpawnIntervalTicks);
        }

        private static int GetPhaseLingerTicks(AbyssalHordeSigilUtility.HordePhasePlan phase, int baseLingerTicks)
        {
            if (phase == null)
            {
                return Mathf.Max(120, baseLingerTicks);
            }

            if (string.Equals(phase.PhaseId, "marking", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Max(110, baseLingerTicks - 24);
            }

            if (phase.IsSurgePhase)
            {
                return Mathf.Max(150, baseLingerTicks + 48);
            }

            if (phase.IsCollapsePhase)
            {
                return Mathf.Max(110, baseLingerTicks - 8);
            }

            return Mathf.Max(120, baseLingerTicks + 18);
        }

        private static int GetDelayAfterPulse(AbyssalHordeSigilUtility.HordePhasePlan phase, bool isLastPulseInPhase, bool hasNextPhase)
        {
            if (phase == null)
            {
                return PortalCadenceTicks;
            }

            if (!isLastPulseInPhase)
            {
                if (string.Equals(phase.PhaseId, "marking", StringComparison.OrdinalIgnoreCase))
                {
                    return 78;
                }

                if (phase.IsSurgePhase)
                {
                    return 42;
                }

                if (phase.IsCollapsePhase)
                {
                    return 84;
                }

                return 60;
            }

            if (!hasNextPhase)
            {
                return PortalCadenceTicks;
            }

            if (string.Equals(phase.PhaseId, "marking", StringComparison.OrdinalIgnoreCase))
            {
                return 240;
            }

            if (phase.IsSurgePhase)
            {
                return 48;
            }

            if (phase.IsCollapsePhase)
            {
                return 96;
            }

            return 300;
        }

        private static void SortDirectedEntriesForHorde(List<AbyssalEncounterDirectorUtility.DirectedEntry> entries)
        {
            if (entries == null || entries.Count <= 1)
            {
                return;
            }

            entries.Sort((left, right) =>
            {
                int leftPriority = GetRolePriority(left);
                int rightPriority = GetRolePriority(right);
                int comparison = leftPriority.CompareTo(rightPriority);
                if (comparison != 0)
                {
                    return comparison;
                }

                int leftCount = left != null ? left.Count : 0;
                int rightCount = right != null ? right.Count : 0;
                return rightCount.CompareTo(leftCount);
            });
        }

        private static int GetRolePriority(AbyssalEncounterDirectorUtility.DirectedEntry entry)
        {
            string defName = entry?.KindDef?.defName ?? string.Empty;
            if (string.Equals(defName, "ABY_SiegeIdol", StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }

            if (string.Equals(defName, "ABY_RiftSniper", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_NullPriest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_HaloHusk", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            if (string.Equals(defName, "ABY_BreachBrute", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_Harvester", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (string.Equals(defName, "ABY_HexgunThrall", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_RiftSapper", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return 0;
        }

        private static int GetPortalChunkSize(string pawnKindDefName)
        {
            if (string.Equals(pawnKindDefName, "ABY_RiftImp", StringComparison.OrdinalIgnoreCase))
            {
                return 6;
            }

            if (string.Equals(pawnKindDefName, "ABY_EmberHound", StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }

            if (string.Equals(pawnKindDefName, "ABY_HexgunThrall", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pawnKindDefName, "ABY_RiftSapper", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pawnKindDefName, "ABY_ChainZealot", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            if (string.Equals(pawnKindDefName, "ABY_Harvester", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return 1;
        }

        private bool TryOpenNextPortal()
        {
            return TryOpenNextPortal(out _, out _);
        }

        private bool TryOpenNextPortal(out IntVec3 openedCell)
        {
            return TryOpenNextPortal(out openedCell, out _);
        }

        private bool TryOpenNextPortal(out IntVec3 openedCell, out int delayAfterTicks)
        {
            openedCell = IntVec3.Invalid;
            delayAfterTicks = PortalCadenceTicks;

            if (queuedPortals == null || queuedPortals.Count == 0 || map == null || waveFaction == null)
            {
                return false;
            }

            PortalWaveRequest request = null;
            while (queuedPortals.Count > 0)
            {
                request = queuedPortals[0];
                if (request == null || request.PawnKindDef == null)
                {
                    queuedPortals.RemoveAt(0);
                    continue;
                }

                bool commandGateActive = activeCommandGate != null && activeCommandGate.Spawned && !activeCommandGate.Destroyed;
                if (request.RequiresCommandGate && !commandGateActive)
                {
                    queuedPortals.RemoveAt(0);
                    continue;
                }

                break;
            }

            if (request == null || request.PawnKindDef == null)
            {
                return queuedPortals.Count > 0;
            }

            if (!TryFindWavePortalCell(request, out openedCell))
            {
                return false;
            }

            if (!TrySpawnPortal(
                    openedCell,
                    request.PawnKindDef,
                    Mathf.Max(1, request.SpawnCount),
                    Mathf.Max(30, request.WarmupTicks),
                    Mathf.Max(8, request.SpawnIntervalTicks),
                    Mathf.Max(60, request.LingerTicks)))
            {
                return false;
            }

            delayAfterTicks = Mathf.Max(8, request.DelayAfterTicks);
            bool commandGateActiveAfterSpawn = activeCommandGate != null && activeCommandGate.Spawned && !activeCommandGate.Destroyed;
            if (commandGateActiveAfterSpawn)
            {
                delayAfterTicks = Mathf.Max(8, Mathf.RoundToInt(delayAfterTicks * activeCommandGate.CadenceFactor));
            }
            else if (commandGateCollapsed && activeWavePrefersPerimeter)
            {
                delayAfterTicks = Mathf.Max(8, Mathf.RoundToInt(delayAfterTicks * 1.18f));
            }

            if (request.RequiresCommandGate && request.ConsumesCommandBurst && commandGateActiveAfterSpawn)
            {
                activeCommandGate.NotifyCommandBurstSpent();
            }

            usedPortalCells.Add(openedCell);
            lastOpenedPortalCell = openedCell;
            if (activeHordeWave)
            {
                closureRewardPending = true;
            }
            queuedPortals.RemoveAt(0);

            ArchonInfernalVFXUtility.DoSummonVFX(map, openedCell);
            ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", openedCell, map);
            return true;
        }

        private bool TrySpawnPortal(
            IntVec3 cell,
            PawnKindDef pawnKindDef,
            int spawnCount,
            int warmupTicks,
            int spawnIntervalTicks,
            int lingerTicks)
        {
            if (map == null || !cell.IsValid || pawnKindDef == null || waveFaction == null)
            {
                return false;
            }

            ThingDef portalDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpPortalDefName);
            if (portalDef == null)
            {
                return false;
            }

            if (!ABY_SafeSpawnUtility.TrySpawnThingDefSafe(
                    portalDef,
                    cell,
                    map,
                    out Thing spawnedThing,
                    null,
                    Rot4.Random,
                    WipeMode.Vanish,
                    false,
                    false,
                    "portal wave imp portal spawn"))
            {
                return false;
            }

            Building_AbyssalImpPortal portal = spawnedThing as Building_AbyssalImpPortal;
            if (portal == null)
            {
                if (spawnedThing != null && !spawnedThing.Destroyed)
                {
                    spawnedThing.Destroy(DestroyMode.Vanish);
                }
                return false;
            }

            try
            {
                portal.Initialize(waveFaction, pawnKindDef, spawnCount, warmupTicks, spawnIntervalTicks, lingerTicks);
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Portal wave initialization failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex);
                if (!portal.Destroyed)
                {
                    portal.Destroy(DestroyMode.Vanish);
                }
                return false;
            }

            return true;
        }

        private bool TryFindWavePortalCell(PortalWaveRequest request, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;

            bool preferPerimeter = request != null && request.PreferPerimeter;
            if (request != null && request.PreferCommandGate && TryFindCommandGateBiasedPortalCell(out cell))
            {
                return true;
            }

            if (preferPerimeter && TryFindFrontBiasedPortalCell(request, out cell))
            {
                return true;
            }

            return TryFindGenericPortalCell(preferPerimeter || activeWavePrefersPerimeter, out cell);
        }

        private bool TryFindCommandGateBiasedPortalCell(out IntVec3 cell)
        {
            cell = IntVec3.Invalid;

            IntVec3 anchor = activeCommandGate != null && activeCommandGate.Spawned && !activeCommandGate.Destroyed
                ? activeCommandGate.PositionHeld
                : (frontAnchorCells != null && frontAnchorCells.Count > 0 && activeCommandFrontIndex >= 0
                    ? frontAnchorCells[Mathf.Abs(activeCommandFrontIndex) % frontAnchorCells.Count]
                    : IntVec3.Invalid);

            if (!anchor.IsValid)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;
            for (int i = 0; i < 220; i++)
            {
                IntVec3 candidate = MakeRadialCandidate(anchor, 4f, 10.5f);
                if (!IsValidWavePortalCell(candidate, true))
                {
                    continue;
                }

                float score = Rand.Value * 0.12f;
                score += Mathf.InverseLerp(12f, 4f, candidate.DistanceTo(anchor)) * 0.34f;
                score += Mathf.InverseLerp(HordePerimeterBand + 8f, 0f, GetDistanceToEdge(candidate)) * 0.34f;
                score += Mathf.Min(GetDistanceToNearestUsedPortal(candidate), 22f) * 0.08f;
                if (!candidate.Roofed(map))
                {
                    score += 0.08f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryFindFrontBiasedPortalCell(PortalWaveRequest request, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;

            if (request == null || frontAnchorCells == null || frontAnchorCells.Count == 0)
            {
                return false;
            }

            IntVec3 anchor = frontAnchorCells[Mathf.Abs(request.FrontIndex) % frontAnchorCells.Count];
            if (!anchor.IsValid)
            {
                return false;
            }

            string frontRole = request.AssignedFrontRoleId ?? string.Empty;
            IntVec3 stronghold = ResolvePlayerStrongholdCenter();
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            for (int i = 0; i < 260; i++)
            {
                IntVec3 candidate = MakeRadialCandidate(anchor, 2f, HordeFrontRadius);
                if (!IsValidWavePortalCell(candidate, true))
                {
                    continue;
                }

                float score = ScoreFrontCandidate(candidate, anchor, stronghold, frontRole);
                if (request.PreferCommandGate && activeCommandGate != null && activeCommandGate.Spawned && !activeCommandGate.Destroyed)
                {
                    score += Mathf.InverseLerp(14f, 3f, candidate.DistanceTo(activeCommandGate.PositionHeld)) * 0.16f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryFindGenericPortalCell(bool preferPerimeter, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;
            IntVec3 stronghold = ResolvePlayerStrongholdCenter();

            for (int i = 0; i < 360; i++)
            {
                IntVec3 candidate = preferPerimeter ? GetRandomPerimeterCandidate(-1) : GetRandomInteriorCandidate();
                if (!IsValidWavePortalCell(candidate, preferPerimeter))
                {
                    continue;
                }

                float score = Rand.Value * 0.18f;
                score += Mathf.Min(GetDistanceToNearestUsedPortal(candidate), 24f) * 0.12f;
                score += GetOpenGroundScore(candidate, 5f) * 0.16f;
                score += GetApproachLaneScore(candidate, stronghold) * 0.18f;

                if (!candidate.Roofed(map))
                {
                    score += 0.16f;
                }

                if (preferPerimeter)
                {
                    score += Mathf.InverseLerp(HordePerimeterBand + 8f, 0f, GetDistanceToEdge(candidate)) * 0.26f;
                    score += Mathf.InverseLerp(Mathf.Max(18f, map.Size.LengthHorizontal * 0.34f), 8f, candidate.DistanceTo(stronghold)) * 0.10f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool TryBuildFrontAnchors(AbyssalHordeSigilUtility.HordePlan hordePlan, out List<IntVec3> anchors)
        {
            anchors = new List<IntVec3>();
            if (map == null || hordePlan == null || hordePlan.FrontCount <= 0)
            {
                return false;
            }

            List<AbyssalHordeSigilUtility.FrontDirective> directives = hordePlan.FrontDirectives;
            if (directives == null || directives.Count == 0)
            {
                directives = new List<AbyssalHordeSigilUtility.FrontDirective>();
                for (int i = 0; i < hordePlan.FrontCount; i++)
                {
                    directives.Add(new AbyssalHordeSigilUtility.FrontDirective { FrontIndex = i, RoleId = "assault", PreferredSide = -1 });
                }
            }

            for (int index = 0; index < hordePlan.FrontCount; index++)
            {
                AbyssalHordeSigilUtility.FrontDirective directive = directives[Math.Min(index, directives.Count - 1)];
                int preferredSide = directive != null ? directive.PreferredSide : -1;
                string roleId = directive?.RoleId ?? "assault";
                if (!TryFindPerimeterAnchor(anchors, preferredSide, roleId, out IntVec3 anchor))
                {
                    if (!TryFindPerimeterAnchor(anchors, -1, roleId, out anchor))
                    {
                        continue;
                    }
                }

                anchors.Add(anchor);
            }

            return anchors.Count > 0;
        }

        private bool TryFindPerimeterAnchor(List<IntVec3> existingAnchors, int preferredSide, string roleId, out IntVec3 anchor)
        {
            anchor = IntVec3.Invalid;
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;
            IntVec3 stronghold = ResolvePlayerStrongholdCenter();

            for (int i = 0; i < 260; i++)
            {
                IntVec3 candidate = GetRandomPerimeterCandidate(preferredSide);
                if (!IsValidWavePortalCell(candidate, true, true))
                {
                    continue;
                }

                if (GetDistanceToNearest(existingAnchors, candidate) < HordeFrontAnchorMinSeparation)
                {
                    continue;
                }

                float score = ScoreAnchorCandidate(candidate, stronghold, roleId);
                score += Mathf.Min(GetDistanceToNearest(existingAnchors, candidate), 40f) * 0.02f;
                if (preferredSide >= 0 && GetPerimeterSide(candidate) == preferredSide)
                {
                    score += 0.18f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            anchor = bestCell;
            return true;
        }

        private IntVec3 MakeRadialCandidate(IntVec3 center, float minRadius, float maxRadius)
        {
            float radius = Rand.Range(minRadius, Mathf.Max(minRadius, maxRadius));
            float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
            return new IntVec3(
                center.x + Mathf.RoundToInt(Mathf.Cos(angle) * radius),
                0,
                center.z + Mathf.RoundToInt(Mathf.Sin(angle) * radius));
        }

        private IntVec3 GetRandomInteriorCandidate()
        {
            return new IntVec3(
                Rand.Range(3, Mathf.Max(4, map.Size.x - 3)),
                0,
                Rand.Range(3, Mathf.Max(4, map.Size.z - 3)));
        }

        private IntVec3 GetRandomPerimeterCandidate(int preferredSide)
        {
            int side = preferredSide >= 0 ? preferredSide : Rand.RangeInclusive(0, 3);
            int bandX = Mathf.Clamp(HordePerimeterBand, 4, Mathf.Max(5, map.Size.x / 3));
            int bandZ = Mathf.Clamp(HordePerimeterBand, 4, Mathf.Max(5, map.Size.z / 3));

            switch (side)
            {
                case 0:
                    return new IntVec3(
                        Rand.RangeInclusive(2, Mathf.Min(map.Size.x - 3, 2 + bandX)),
                        0,
                        Rand.RangeInclusive(2, Mathf.Max(2, map.Size.z - 3)));
                case 1:
                    return new IntVec3(
                        Rand.RangeInclusive(Mathf.Max(2, map.Size.x - 3 - bandX), Mathf.Max(2, map.Size.x - 3)),
                        0,
                        Rand.RangeInclusive(2, Mathf.Max(2, map.Size.z - 3)));
                case 2:
                    return new IntVec3(
                        Rand.RangeInclusive(2, Mathf.Max(2, map.Size.x - 3)),
                        0,
                        Rand.RangeInclusive(2, Mathf.Min(map.Size.z - 3, 2 + bandZ)));
                default:
                    return new IntVec3(
                        Rand.RangeInclusive(2, Mathf.Max(2, map.Size.x - 3)),
                        0,
                        Rand.RangeInclusive(Mathf.Max(2, map.Size.z - 3 - bandZ), Mathf.Max(2, map.Size.z - 3)));
            }
        }

        private IntVec3 ResolvePlayerStrongholdCenter()
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            if (playerStrongholdCenter.IsValid && playerStrongholdCenter.InBounds(map))
            {
                return playerStrongholdCenter;
            }

            int totalX = 0;
            int totalZ = 0;
            int count = 0;
            List<Thing> things = map.listerThings?.AllThings;
            if (things != null)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed || thing.Faction != Faction.OfPlayer)
                    {
                        continue;
                    }

                    if (thing.def != null && thing.def.category == ThingCategory.Building)
                    {
                        totalX += thing.Position.x;
                        totalZ += thing.Position.z;
                        count++;
                    }
                }
            }

            if (count <= 0)
            {
                List<Pawn> pawns = map.mapPawns?.FreeColonistsSpawned;
                if (pawns != null)
                {
                    for (int i = 0; i < pawns.Count; i++)
                    {
                        Pawn pawn = pawns[i];
                        if (pawn == null || !pawn.Spawned)
                        {
                            continue;
                        }

                        totalX += pawn.Position.x;
                        totalZ += pawn.Position.z;
                        count++;
                    }
                }
            }

            playerStrongholdCenter = count > 0
                ? new IntVec3(Mathf.RoundToInt((float)totalX / count), 0, Mathf.RoundToInt((float)totalZ / count))
                : new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
            return playerStrongholdCenter;
        }

        private float ScoreFrontCandidate(IntVec3 candidate, IntVec3 anchor, IntVec3 stronghold, string frontRole)
        {
            float laneScore = GetApproachLaneScore(candidate, stronghold);
            float openScore = GetOpenGroundScore(candidate, 6f);
            float edgeScore = Mathf.InverseLerp(HordePerimeterBand + 8f, 0f, GetDistanceToEdge(candidate));
            float anchorScore = Mathf.InverseLerp(HordeFrontRadius + 2f, 0f, candidate.DistanceTo(anchor));
            float strongholdDistanceNorm = Mathf.Clamp01(candidate.DistanceTo(stronghold) / Mathf.Max(24f, Mathf.Min(map.Size.x, map.Size.z) * 0.65f));
            float midDistanceScore = 1f - Mathf.Clamp01(Mathf.Abs(strongholdDistanceNorm - 0.52f) / 0.52f);

            float score = Rand.Value * 0.10f;
            switch ((frontRole ?? string.Empty).ToLowerInvariant())
            {
                case "flank":
                    score += midDistanceScore * 0.26f;
                    score += laneScore * 0.24f;
                    score += edgeScore * 0.18f;
                    score += anchorScore * 0.18f;
                    score += openScore * 0.14f;
                    break;
                case "fire_support":
                    score += openScore * 0.30f;
                    score += laneScore * 0.24f;
                    score += strongholdDistanceNorm * 0.18f;
                    score += edgeScore * 0.14f;
                    score += anchorScore * 0.14f;
                    break;
                case "siege":
                    score += openScore * 0.34f;
                    score += strongholdDistanceNorm * 0.24f;
                    score += laneScore * 0.16f;
                    score += edgeScore * 0.14f;
                    score += anchorScore * 0.12f;
                    break;
                default:
                    score += (1f - strongholdDistanceNorm) * 0.26f;
                    score += laneScore * 0.26f;
                    score += edgeScore * 0.18f;
                    score += anchorScore * 0.18f;
                    score += openScore * 0.12f;
                    break;
            }

            if (!candidate.Roofed(map))
            {
                score += 0.08f;
            }

            return score;
        }

        private float ScoreAnchorCandidate(IntVec3 candidate, IntVec3 stronghold, string roleId)
        {
            float laneScore = GetApproachLaneScore(candidate, stronghold);
            float openScore = GetOpenGroundScore(candidate, 7f);
            float edgeScore = Mathf.InverseLerp(HordePerimeterBand + 8f, 0f, GetDistanceToEdge(candidate));
            float strongholdDistanceNorm = Mathf.Clamp01(candidate.DistanceTo(stronghold) / Mathf.Max(24f, Mathf.Min(map.Size.x, map.Size.z) * 0.65f));
            float score = Rand.Value * 0.10f;

            switch ((roleId ?? string.Empty).ToLowerInvariant())
            {
                case "flank":
                    score += laneScore * 0.24f;
                    score += openScore * 0.16f;
                    score += edgeScore * 0.30f;
                    score += (1f - Mathf.Abs(strongholdDistanceNorm - 0.55f)) * 0.20f;
                    break;
                case "fire_support":
                    score += openScore * 0.28f;
                    score += laneScore * 0.24f;
                    score += edgeScore * 0.22f;
                    score += strongholdDistanceNorm * 0.16f;
                    break;
                case "siege":
                    score += openScore * 0.32f;
                    score += strongholdDistanceNorm * 0.22f;
                    score += laneScore * 0.18f;
                    score += edgeScore * 0.20f;
                    break;
                default:
                    score += (1f - strongholdDistanceNorm) * 0.24f;
                    score += laneScore * 0.26f;
                    score += edgeScore * 0.28f;
                    score += openScore * 0.14f;
                    break;
            }

            if (!candidate.Roofed(map))
            {
                score += 0.08f;
            }

            return score;
        }

        private float GetOpenGroundScore(IntVec3 center, float radius)
        {
            if (map == null)
            {
                return 0f;
            }

            int total = 0;
            int open = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                total++;
                if (cell.Standable(map) && cell.GetEdifice(map) == null)
                {
                    open++;
                }
            }

            return total > 0 ? Mathf.Clamp01((float)open / total) : 0f;
        }

        private float GetApproachLaneScore(IntVec3 from, IntVec3 to)
        {
            if (map == null || !from.IsValid || !to.IsValid)
            {
                return 0f;
            }

            int steps = Mathf.Clamp(Mathf.RoundToInt(from.DistanceTo(to) / 3f), 6, 22);
            int passable = 0;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                IntVec3 sample = new IntVec3(
                    Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t)),
                    0,
                    Mathf.RoundToInt(Mathf.Lerp(from.z, to.z, t)));
                if (!sample.InBounds(map))
                {
                    continue;
                }

                if (sample.Standable(map) && sample.GetEdifice(map) == null)
                {
                    passable++;
                }
            }

            return Mathf.Clamp01(passable / (float)Math.Max(1, steps));
        }

        private int GetPerimeterSide(IntVec3 cell)
        {
            if (map == null || !cell.IsValid)
            {
                return -1;
            }

            int west = cell.x;
            int east = Math.Abs(map.Size.x - 1 - cell.x);
            int south = cell.z;
            int north = Math.Abs(map.Size.z - 1 - cell.z);

            int bestSide = 0;
            int bestDistance = west;
            if (east < bestDistance)
            {
                bestDistance = east;
                bestSide = 1;
            }

            if (south < bestDistance)
            {
                bestDistance = south;
                bestSide = 2;
            }

            if (north < bestDistance)
            {
                bestSide = 3;
            }

            return bestSide;
        }

        private bool IsValidWavePortalCell(IntVec3 candidate, bool preferPerimeter)
        {
            return IsValidWavePortalCell(candidate, preferPerimeter, activeWavePrefersPerimeter);
        }

        private bool IsValidWavePortalCell(IntVec3 candidate, bool preferPerimeter, bool hordeMode)
        {
            if (map == null || !candidate.IsValid || !candidate.InBounds(map))
            {
                return false;
            }

            if (candidate.Fogged(map) || !candidate.Standable(map))
            {
                return false;
            }

            if (candidate.GetFirstPawn(map) != null)
            {
                return false;
            }

            if (candidate.GetEdifice(map) != null)
            {
                return false;
            }

            if (preferPerimeter && GetDistanceToEdge(candidate) > HordePerimeterBand + 8f)
            {
                return false;
            }

            if (map.areaManager?.Home != null && map.areaManager.Home[candidate])
            {
                return false;
            }

            float unsafeRadius = hordeMode ? HordeUnsafeBaseRadius : UnsafeBaseRadius;
            float buildingRadius = hordeMode ? HordeLocalBuildingBlockRadius : LocalBuildingBlockRadius;
            float usedSeparation = hordeMode ? HordeUsedPortalMinSeparation : UsedPortalMinSeparation;

            if (HasHomeAreaNearby(candidate, unsafeRadius))
            {
                return false;
            }

            if (HasPlayerBuildingNearby(candidate, unsafeRadius))
            {
                return false;
            }

            if (HasPlayerBuildingNearby(candidate, buildingRadius))
            {
                return false;
            }

            if (hordeMode)
            {
                if (GetNearbyPlayerBuildingCount(candidate, 12f) >= 2)
                {
                    return false;
                }

                if (GetNearbyHomeAreaCount(candidate, 10f) >= 8)
                {
                    return false;
                }

                if (GetNearbyPlayerBuildingCount(candidate, HordeUnsafeBaseRadius + 6f) >= 5)
                {
                    return false;
                }
            }

            if (GetDistanceToNearestUsedPortal(candidate) < usedSeparation)
            {
                return false;
            }

            return true;
        }

        private bool HasHomeAreaNearby(IntVec3 center, float radius)
        {
            if (map?.areaManager?.Home == null)
            {
                return false;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (cell.InBounds(map) && map.areaManager.Home[cell])
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasPlayerBuildingNearby(IntVec3 center, float radius)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed || thing.Faction != Faction.OfPlayer)
                    {
                        continue;
                    }

                    if (thing.def != null && thing.def.category == ThingCategory.Building)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private int GetNearbyHomeAreaCount(IntVec3 center, float radius)
        {
            if (map?.areaManager?.Home == null)
            {
                return 0;
            }

            int count = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (cell.InBounds(map) && map.areaManager.Home[cell])
                {
                    count++;
                }
            }

            return count;
        }

        private int GetNearbyPlayerBuildingCount(IntVec3 center, float radius)
        {
            int count = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed || thing.Faction != Faction.OfPlayer)
                    {
                        continue;
                    }

                    if (thing.def != null && thing.def.category == ThingCategory.Building)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private float GetDistanceToNearestUsedPortal(IntVec3 candidate)
        {
            return GetDistanceToNearest(usedPortalCells, candidate);
        }

        private float GetDistanceToNearest(List<IntVec3> cells, IntVec3 candidate)
        {
            if (cells == null || cells.Count == 0)
            {
                return 999f;
            }

            float nearest = 999f;
            for (int i = 0; i < cells.Count; i++)
            {
                IntVec3 other = cells[i];
                if (!other.IsValid)
                {
                    continue;
                }

                float distance = candidate.DistanceTo(other);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            return nearest;
        }

        private float GetDistanceToEdge(IntVec3 cell)
        {
            if (map == null || !cell.IsValid)
            {
                return 999f;
            }

            int dx = Math.Min(cell.x, map.Size.x - 1 - cell.x);
            int dz = Math.Min(cell.z, map.Size.z - 1 - cell.z);
            return Math.Min(dx, dz);
        }

        private bool TrySpawnCommandGate(AbyssalHordeSigilUtility.HordePlan hordePlan, int commandFrontIndex)
        {
            if (map == null || waveFaction == null || hordePlan == null || !hordePlan.UsesCommandGate)
            {
                return false;
            }

            if (!TryFindCommandGateCell(commandFrontIndex, out IntVec3 cell))
            {
                return false;
            }

            ThingDef gateDef = DefDatabase<ThingDef>.GetNamedSilentFail(CommandGateDefName);
            if (gateDef == null)
            {
                return false;
            }

            if (!ABY_SafeSpawnUtility.TrySpawnThingDefSafe(
                    gateDef,
                    cell,
                    map,
                    out Thing spawnedThing,
                    null,
                    Rot4.Random,
                    WipeMode.Vanish,
                    false,
                    false,
                    "horde command gate spawn"))
            {
                return false;
            }

            Building_AbyssalHordeCommandGate gate = spawnedThing as Building_AbyssalHordeCommandGate;
            if (gate == null)
            {
                if (spawnedThing != null && !spawnedThing.Destroyed)
                {
                    spawnedThing.Destroy(DestroyMode.Vanish);
                }
                return false;
            }

            try
            {
                gate.SetFaction(waveFaction);
                gate.Initialize(hordePlan.CommandGateReservedBursts, hordePlan.CommandGateCadenceFactor, hordePlan.CommandGateHitPoints, AbyssalHordeSigilUtility.GetDoctrineLabel(hordePlan), activeHordeRewardSnapshot);
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Horde command gate initialization failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex);
                if (!gate.Destroyed)
                {
                    gate.Destroy(DestroyMode.Vanish);
                }
                return false;
            }

            activeCommandGate = gate;
            activeCommandFrontIndex = Mathf.Max(0, commandFrontIndex);
            usedPortalCells?.Add(cell);

            string sideLabel = AbyssalHordeSigilUtility.GetFrontDirective(hordePlan, activeCommandFrontIndex)?.SideLabel ?? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeSide_Unknown", "unstable edge");
            Messages.Message(
                AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_HordeCommandGate_Spawned",
                    "A {0} command gate node stabilizes on the {1} perimeter. Destroying it cancels reserved reinforcements and slows the offensive.",
                    AbyssalHordeSigilUtility.GetDoctrineLabel(hordePlan),
                    sideLabel),
                new TargetInfo(cell, map),
                MessageTypeDefOf.ThreatBig);
            return true;
        }

        private bool TryFindCommandGateCell(int commandFrontIndex, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (frontAnchorCells == null || frontAnchorCells.Count == 0)
            {
                return false;
            }

            IntVec3 anchor = frontAnchorCells[Mathf.Abs(commandFrontIndex) % frontAnchorCells.Count];
            if (!anchor.IsValid)
            {
                return false;
            }

            string frontRole = AbyssalHordeSigilUtility.GetFrontDirective(AbyssalHordeSigilUtility.GetHordePlan(map), commandFrontIndex)?.RoleId ?? "fire_support";
            IntVec3 stronghold = ResolvePlayerStrongholdCenter();
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;
            for (int i = 0; i < 220; i++)
            {
                IntVec3 candidate = MakeRadialCandidate(anchor, 2.8f, 7.8f);
                if (!IsValidWavePortalCell(candidate, true, true))
                {
                    continue;
                }

                float score = ScoreFrontCandidate(candidate, anchor, stronghold, frontRole) + GetOpenGroundScore(candidate, 6f) * 0.12f;
                if (candidate.DistanceTo(anchor) > 7.5f)
                {
                    score -= 0.08f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        public void NotifyCommandGateDestroyed(Building_AbyssalHordeCommandGate gate)
        {
            if (gate == null || activeCommandGate != gate)
            {
                return;
            }

            activeCommandGate = null;
            commandGateCollapsed = true;
            commandRewardGranted = true;
            if (queuedPortals != null && queuedPortals.Count > 0)
            {
                queuedPortals.RemoveAll(request => request != null && request.RequiresCommandGate);
            }

            if (map != null)
            {
                Messages.Message(
                    AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_HordeCommandGate_Destroyed",
                        "The {0} command gate collapses. Remaining command-linked reinforcements are severed and the breach loses tempo.",
                        gate.LabelNoCount),
                    new TargetInfo(gate.PositionHeld, map),
                    MessageTypeDefOf.PositiveEvent);
            }

            TryForceCompleteStaleHorde(true, "command gate collapsed with no active portals or combat-capable abyssal pawns");
        }

        public bool TryForceCompleteStaleHorde(bool allowQueuedPortals, string reason)
        {
            if (!activeHordeWave)
            {
                return false;
            }

            if (!allowQueuedPortals && queuedPortals != null && queuedPortals.Count > 0)
            {
                return false;
            }

            if (activeCommandGate != null && activeCommandGate.Spawned && !activeCommandGate.Destroyed)
            {
                return false;
            }

            if (HasActiveAbyssalPortalsOnMap() || HasLiveCombatCapableAbyssalPawnsOnMap())
            {
                return false;
            }

            int discarded = queuedPortals != null ? queuedPortals.Count : 0;
            queuedPortals?.Clear();
            ResetWave();
            ABY_LogThrottleUtility.Message(
                "horde-hard-stop-" + (map != null ? map.uniqueID.ToString() : "unknown"),
                "[Abyssal Protocol] Horde hard-stop completed a stale horde encounter" + (discarded > 0 ? " and discarded " + discarded + " hidden queued portal requests" : string.Empty) + (reason.NullOrEmpty() ? "." : ": " + reason),
                2500);
            return true;
        }

        private bool HasActiveAbyssalPortalsOnMap()
        {
            if (map?.listerThings?.AllThings == null)
            {
                return false;
            }

            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.def == null)
                {
                    continue;
                }

                if (ABY_DominionTargetUtility.IsHostileHordePortal(thing))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasLiveCombatCapableAbyssalPawnsOnMap()
        {
            if (map?.mapPawns == null)
            {
                return false;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                if (ABY_AntiTameUtility.IsLiveCombatCapableAbyssalPawn(pawns[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetWave()
        {
            Building_AbyssalHordeCommandGate gate = activeCommandGate;
            if (activeHordeWave && closureRewardPending)
            {
                IntVec3 rewardCell = lastOpenedPortalCell.IsValid
                    ? lastOpenedPortalCell
                    : (gate != null && gate.Spawned ? gate.PositionHeld : playerStrongholdCenter);
                if (rewardCell.IsValid)
                {
                    AbyssalHordeRewardUtility.SpawnClosureRewards(map, rewardCell, activeHordeRewardSnapshot);
                }
            }

            activeCommandGate = null;
            activeCommandFrontIndex = -1;
            commandGateCollapsed = false;

            if (gate != null && gate.Spawned && !gate.Destroyed)
            {
                gate.DismissWithoutRewards();
            }

            queuedPortals?.Clear();
            usedPortalCells?.Clear();
            frontAnchorCells?.Clear();
            waveFaction = null;
            nextPortalOpenTick = -1;
            activeWavePrefersPerimeter = false;
            playerStrongholdCenter = IntVec3.Invalid;
            activeHordeWave = false;
            closureRewardPending = false;
            commandRewardGranted = false;
            lastOpenedPortalCell = IntVec3.Invalid;
            activeHordeRewardSnapshot = null;
        }

        private static void Shuffle(List<PortalWaveRequest> requests)
        {
            if (requests == null)
            {
                return;
            }

            for (int i = requests.Count - 1; i > 0; i--)
            {
                int swapIndex = Rand.RangeInclusive(0, i);
                PortalWaveRequest temp = requests[i];
                requests[i] = requests[swapIndex];
                requests[swapIndex] = temp;
            }
        }

        private static void ShuffleInts(List<int> values)
        {
            if (values == null)
            {
                return;
            }

            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = Rand.RangeInclusive(0, i);
                int temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }
    }
}
