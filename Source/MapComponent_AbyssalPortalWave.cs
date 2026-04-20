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
            public bool PreferPerimeter;
            public int FrontIndex = -1;

            public void ExposeData()
            {
                Scribe_Defs.Look(ref PawnKindDef, "pawnKindDef");
                Scribe_Values.Look(ref SpawnCount, "spawnCount", 1);
                Scribe_Values.Look(ref WarmupTicks, "warmupTicks", 150);
                Scribe_Values.Look(ref SpawnIntervalTicks, "spawnIntervalTicks", 24);
                Scribe_Values.Look(ref LingerTicks, "lingerTicks", 180);
                Scribe_Values.Look(ref PreferPerimeter, "preferPerimeter", false);
                Scribe_Values.Look(ref FrontIndex, "frontIndex", -1);
            }
        }

        private const string ImpPortalDefName = "ABY_ImpPortal";
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

            if (TryOpenNextPortal())
            {
                nextPortalOpenTick = queuedPortals.Count > 0 ? Find.TickManager.TicksGame + PortalCadenceTicks : -1;
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
            waveFaction = faction;
            activeWavePrefersPerimeter = false;
            nextPortalOpenTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (!TryOpenNextPortal(out firstPortalCell))
            {
                ResetWave();
                failReason = "ABY_CircleFail_NoPortalSpawn".Translate();
                return false;
            }

            nextPortalOpenTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + PortalCadenceTicks;
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

            if (!TryBuildFrontAnchors(Mathf.Max(2, resolvedPlan.FrontCount), out List<IntVec3> anchors))
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
            nextPortalOpenTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (!TryOpenNextPortal(out firstPortalCell))
            {
                ResetWave();
                failReason = "ABY_CircleFail_NoPortalSpawn".Translate();
                return false;
            }

            nextPortalOpenTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + PortalCadenceTicks;
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
                        FrontIndex = -1
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
            if (hordePlan == null || hordePlan.PulsePlans == null || hordePlan.PulsePlans.Count == 0)
            {
                return requests;
            }

            int frontCount = Mathf.Max(1, hordePlan.FrontCount);
            int frontCursor = 0;

            for (int pulseIndex = 0; pulseIndex < hordePlan.PulsePlans.Count; pulseIndex++)
            {
                AbyssalEncounterDirectorUtility.EncounterPlan pulsePlan = hordePlan.PulsePlans[pulseIndex];
                if (pulsePlan?.Entries == null || pulsePlan.Entries.Count == 0)
                {
                    continue;
                }

                List<AbyssalEncounterDirectorUtility.DirectedEntry> entries = new List<AbyssalEncounterDirectorUtility.DirectedEntry>(pulsePlan.Entries);
                SortDirectedEntriesForHorde(entries);

                int pulseWarmup = Mathf.Max(78, baseWarmupTicks - pulseIndex * 4);
                int pulseInterval = Mathf.Max(9, baseSpawnIntervalTicks - Mathf.Min(6, pulseIndex));
                int pulseLinger = Mathf.Max(110, baseLingerTicks + pulseIndex * 16);

                for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    AbyssalEncounterDirectorUtility.DirectedEntry entry = entries[entryIndex];
                    if (entry?.KindDef == null || entry.Count <= 0)
                    {
                        continue;
                    }

                    int remaining = entry.Count;
                    int chunkSize = Mathf.Max(1, GetPortalChunkSize(entry.KindDef.defName));

                    while (remaining > 0)
                    {
                        int spawnCount = Mathf.Min(remaining, chunkSize);
                        requests.Add(new PortalWaveRequest
                        {
                            PawnKindDef = entry.KindDef,
                            SpawnCount = spawnCount,
                            WarmupTicks = pulseWarmup,
                            SpawnIntervalTicks = pulseInterval,
                            LingerTicks = pulseLinger,
                            PreferPerimeter = true,
                            FrontIndex = frontCursor % frontCount
                        });

                        frontCursor++;
                        remaining -= spawnCount;
                    }
                }
            }

            return requests;
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

            if (string.Equals(defName, "ABY_HexgunThrall", StringComparison.OrdinalIgnoreCase))
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
            return TryOpenNextPortal(out _);
        }

        private bool TryOpenNextPortal(out IntVec3 openedCell)
        {
            openedCell = IntVec3.Invalid;

            if (queuedPortals == null || queuedPortals.Count == 0 || map == null || waveFaction == null)
            {
                return false;
            }

            PortalWaveRequest request = queuedPortals[0];
            if (request == null || request.PawnKindDef == null)
            {
                queuedPortals.RemoveAt(0);
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

            usedPortalCells.Add(openedCell);
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

            Building_AbyssalImpPortal portal = ThingMaker.MakeThing(portalDef) as Building_AbyssalImpPortal;
            if (portal == null)
            {
                return false;
            }

            GenSpawn.Spawn(portal, cell, map, Rot4.Random);
            portal.Initialize(waveFaction, pawnKindDef, spawnCount, warmupTicks, spawnIntervalTicks, lingerTicks);
            return true;
        }

        private bool TryFindWavePortalCell(PortalWaveRequest request, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;

            bool preferPerimeter = request != null && request.PreferPerimeter;
            if (preferPerimeter && TryFindFrontBiasedPortalCell(request.FrontIndex, out cell))
            {
                return true;
            }

            return TryFindGenericPortalCell(preferPerimeter || activeWavePrefersPerimeter, out cell);
        }

        private bool TryFindFrontBiasedPortalCell(int frontIndex, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;

            if (frontAnchorCells == null || frontAnchorCells.Count == 0)
            {
                return false;
            }

            IntVec3 anchor = frontAnchorCells[Mathf.Abs(frontIndex) % frontAnchorCells.Count];
            if (!anchor.IsValid)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            for (int i = 0; i < 240; i++)
            {
                IntVec3 candidate = MakeRadialCandidate(anchor, 2f, HordeFrontRadius);
                if (!IsValidWavePortalCell(candidate, true))
                {
                    continue;
                }

                float score = Rand.Value * 0.15f;
                score += Mathf.Min(GetDistanceToNearestUsedPortal(candidate), 26f) * 0.10f;
                score += Mathf.InverseLerp(HordePerimeterBand + 6f, 0f, GetDistanceToEdge(candidate)) * 0.42f;
                score += Mathf.InverseLerp(HordeFrontRadius + 2f, 0f, candidate.DistanceTo(anchor)) * 0.26f;

                if (!candidate.Roofed(map))
                {
                    score += 0.12f;
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

            for (int i = 0; i < 360; i++)
            {
                IntVec3 candidate = preferPerimeter ? GetRandomPerimeterCandidate(-1) : GetRandomInteriorCandidate();
                if (!IsValidWavePortalCell(candidate, preferPerimeter))
                {
                    continue;
                }

                float score = Rand.Value * 0.25f;
                score += Mathf.Min(GetDistanceToNearestUsedPortal(candidate), 24f) * 0.12f;

                if (!candidate.Roofed(map))
                {
                    score += 0.20f;
                }

                if (preferPerimeter)
                {
                    score += Mathf.InverseLerp(HordePerimeterBand + 8f, 0f, GetDistanceToEdge(candidate)) * 0.36f;
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

        private bool TryBuildFrontAnchors(int desiredCount, out List<IntVec3> anchors)
        {
            anchors = new List<IntVec3>();
            if (map == null || desiredCount <= 0)
            {
                return false;
            }

            List<int> sideOrder = new List<int> { 0, 1, 2, 3 };
            ShuffleInts(sideOrder);

            for (int index = 0; index < desiredCount; index++)
            {
                int preferredSide = sideOrder[index % sideOrder.Count];
                if (!TryFindPerimeterAnchor(anchors, preferredSide, out IntVec3 anchor))
                {
                    if (!TryFindPerimeterAnchor(anchors, -1, out anchor))
                    {
                        continue;
                    }
                }

                anchors.Add(anchor);
            }

            return anchors.Count > 0;
        }

        private bool TryFindPerimeterAnchor(List<IntVec3> existingAnchors, int preferredSide, out IntVec3 anchor)
        {
            anchor = IntVec3.Invalid;
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            for (int i = 0; i < 240; i++)
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

                float score = Rand.Value * 0.12f;
                score += Mathf.InverseLerp(HordePerimeterBand + 8f, 0f, GetDistanceToEdge(candidate)) * 0.55f;
                score += Mathf.Min(GetDistanceToNearest(existingAnchors, candidate), 40f) * 0.03f;

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

        private void ResetWave()
        {
            queuedPortals?.Clear();
            usedPortalCells?.Clear();
            frontAnchorCells?.Clear();
            waveFaction = null;
            nextPortalOpenTick = -1;
            activeWavePrefersPerimeter = false;
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
