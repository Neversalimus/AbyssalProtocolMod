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

            public void ExposeData()
            {
                Scribe_Defs.Look(ref PawnKindDef, "pawnKindDef");
                Scribe_Values.Look(ref SpawnCount, "spawnCount", 1);
                Scribe_Values.Look(ref WarmupTicks, "warmupTicks", 150);
                Scribe_Values.Look(ref SpawnIntervalTicks, "spawnIntervalTicks", 24);
                Scribe_Values.Look(ref LingerTicks, "lingerTicks", 180);
            }
        }

        private const string ImpPortalDefName = "ABY_ImpPortal";
        private const string RiftImpPawnKindDefName = "ABY_RiftImp";

        private const int PortalCadenceTicks = 48;
        private const int RetryCadenceTicks = 24;
        private const float UsedPortalMinSeparation = 11.9f;
        private const float UnsafeBaseRadius = 9f;
        private const float LocalBuildingBlockRadius = 2.9f;

        private const float EmberDoubleSpawnChance = 0.25f;
        private const float BonusImpPortalChance = 0.20f;
        private const int EmberSingleSpawnCount = 1;
        private const int EmberDoubleSpawnCount = 2;
        private const int BonusImpMinSpawnCount = 2;
        private const int BonusImpMaxSpawnCount = 4;

        private List<PortalWaveRequest> queuedPortals = new List<PortalWaveRequest>();
        private List<IntVec3> usedPortalCells = new List<IntVec3>();
        private Faction waveFaction;
        private int nextPortalOpenTick = -1;

        public MapComponent_AbyssalPortalWave(Map map) : base(map)
        {
        }

        public bool IsWaveActive => (queuedPortals != null && queuedPortals.Count > 0) || nextPortalOpenTick >= 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref queuedPortals, "queuedPortals", LookMode.Deep);
            Scribe_Collections.Look(ref usedPortalCells, "usedPortalCells", LookMode.Value);
            Scribe_References.Look(ref waveFaction, "waveFaction");
            Scribe_Values.Look(ref nextPortalOpenTick, "nextPortalOpenTick", -1);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                queuedPortals ??= new List<PortalWaveRequest>();
                usedPortalCells ??= new List<IntVec3>();
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
                nextPortalOpenTick = -1;
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
                    waveFaction = null;
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
            waveFaction = faction;
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
                    LingerTicks = emberPortalLingerTicks
                });

                if (bonusImpKind != null && Rand.Chance(BonusImpPortalChance))
                {
                    requests.Add(new PortalWaveRequest
                    {
                        PawnKindDef = bonusImpKind,
                        SpawnCount = Rand.RangeInclusive(BonusImpMinSpawnCount, BonusImpMaxSpawnCount),
                        WarmupTicks = bonusImpWarmupTicks,
                        SpawnIntervalTicks = bonusImpSpawnIntervalTicks,
                        LingerTicks = bonusImpLingerTicks
                    });
                }
            }

            Shuffle(requests);
            return requests;
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

            if (!TryFindWavePortalCell(out openedCell))
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

        private bool TryFindWavePortalCell(out IntVec3 cell)
        {
            cell = IntVec3.Invalid;

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            for (int i = 0; i < 360; i++)
            {
                IntVec3 candidate = new IntVec3(
                    Rand.Range(3, Mathf.Max(4, map.Size.x - 3)),
                    0,
                    Rand.Range(3, Mathf.Max(4, map.Size.z - 3)));

                if (!IsValidWavePortalCell(candidate))
                {
                    continue;
                }

                float score = Rand.Value * 0.25f;
                score += Mathf.Min(GetDistanceToNearestUsedPortal(candidate), 24f) * 0.12f;

                if (!candidate.Roofed(map))
                {
                    score += 0.20f;
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

        private bool IsValidWavePortalCell(IntVec3 candidate)
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

            if (map.areaManager?.Home != null && map.areaManager.Home[candidate])
            {
                return false;
            }

            if (HasHomeAreaNearby(candidate, UnsafeBaseRadius))
            {
                return false;
            }

            if (HasPlayerBuildingNearby(candidate, UnsafeBaseRadius))
            {
                return false;
            }

            if (HasPlayerBuildingNearby(candidate, LocalBuildingBlockRadius))
            {
                return false;
            }

            if (GetDistanceToNearestUsedPortal(candidate) < UsedPortalMinSeparation)
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
            if (usedPortalCells == null || usedPortalCells.Count == 0)
            {
                return 999f;
            }

            float nearest = 999f;
            for (int i = 0; i < usedPortalCells.Count; i++)
            {
                IntVec3 other = usedPortalCells[i];
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

        private void ResetWave()
        {
            queuedPortals?.Clear();
            usedPortalCells?.Clear();
            waveFaction = null;
            nextPortalOpenTick = -1;
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
    }
}
