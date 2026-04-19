using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class ABY_Phase2PortalUtility
    {
        private const string PortalDefName = "ABY_ImpPortal";
        private const string ImpPawnKindDefName = "ABY_RiftImp";

        public static int CountActivePlayerColonists(Map map)
        {
            if (map == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Dead || pawn.Downed)
                {
                    continue;
                }

                if (pawn.Faction == Faction.OfPlayer && pawn.RaceProps != null && pawn.RaceProps.Humanlike)
                {
                    count++;
                }
            }

            return count;
        }

        public static bool TrySpawnImpPortal(
            Map map,
            Faction faction,
            int impCount,
            int warmupTicks,
            int impSpawnIntervalTicks,
            int lingerTicks,
            out Building_AbyssalImpPortal portal)
        {
            portal = null;

            if (map == null || impCount <= 0)
            {
                return false;
            }

            if (!TryFindPortalSpawnCell(map, out IntVec3 cell))
            {
                return false;
            }

            ThingDef portalDef = DefDatabase<ThingDef>.GetNamedSilentFail(PortalDefName);
            PawnKindDef impKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(ImpPawnKindDefName);
            if (portalDef == null || impKindDef == null)
            {
                return false;
            }

            Building_AbyssalImpPortal madePortal = ThingMaker.MakeThing(portalDef) as Building_AbyssalImpPortal;
            if (madePortal == null)
            {
                return false;
            }

            GenSpawn.Spawn(madePortal, cell, map, Rot4.Random);
            madePortal.Initialize(faction, impKindDef, impCount, warmupTicks, impSpawnIntervalTicks, lingerTicks);
            portal = madePortal;
            return true;
        }

        public static bool TrySpawnImpPortalNear(
            Map map,
            Faction faction,
            IntVec3 origin,
            float minRadius,
            float maxRadius,
            int impCount,
            int warmupTicks,
            int impSpawnIntervalTicks,
            int lingerTicks,
            out Building_AbyssalImpPortal portal)
        {
            portal = null;

            if (map == null || impCount <= 0 || !origin.IsValid)
            {
                return false;
            }

            if (!TryFindPortalSpawnCellNear(map, origin, minRadius, maxRadius, out IntVec3 cell))
            {
                return false;
            }

            ThingDef portalDef = DefDatabase<ThingDef>.GetNamedSilentFail(PortalDefName);
            PawnKindDef impKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(ImpPawnKindDefName);
            if (portalDef == null || impKindDef == null)
            {
                return false;
            }

            Building_AbyssalImpPortal madePortal = ThingMaker.MakeThing(portalDef) as Building_AbyssalImpPortal;
            if (madePortal == null)
            {
                return false;
            }

            GenSpawn.Spawn(madePortal, cell, map, Rot4.Random);
            madePortal.Initialize(faction, impKindDef, impCount, warmupTicks, impSpawnIntervalTicks, lingerTicks);
            portal = madePortal;
            return true;
        }

        public static bool TryFindPortalSpawnCellNear(Map map, IntVec3 origin, float minRadius, float maxRadius, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || !origin.IsValid)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;
            float preferredRadius = (minRadius + maxRadius) * 0.5f;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(origin, maxRadius, true))
            {
                if (!candidate.InBounds(map) || candidate.Fogged(map) || !candidate.Standable(map))
                {
                    continue;
                }

                float distance = candidate.DistanceTo(origin);
                if (distance < minRadius || distance > maxRadius)
                {
                    continue;
                }

                if (candidate.GetFirstPawn(map) != null)
                {
                    continue;
                }

                if (candidate.GetEdifice(map) != null)
                {
                    continue;
                }

                if (HasBlockingPlayerBuildingNearby(map, candidate, 2.9f))
                {
                    continue;
                }

                float score = -Mathf.Abs(distance - preferredRadius);
                if (!candidate.Roofed(map))
                {
                    score += 0.35f;
                }

                score += CountAdjacentStandableCells(map, candidate) * 0.08f;
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

        public static bool TryFindRetreatEdgeCell(Map map, Pawn pawn, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            for (int i = 0; i < 160; i++)
            {
                IntVec3 candidate = RandomEdgeCell(map);
                if (!IsBossSafeStandableCell(map, candidate, pawn, false))
                {
                    continue;
                }

                if (pawn != null && pawn.Spawned && pawn.MapHeld == map && !pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                float score = ScoreRetreatCell(map, candidate, pawn);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            if (bestCell.IsValid)
            {
                cell = bestCell;
                return true;
            }

            if (pawn != null && pawn.Spawned && TryFindLocalRetreatCell(map, pawn.Position, pawn, out bestCell))
            {
                cell = bestCell;
                return true;
            }

            return false;
        }

        public static bool TryFindLocalRetreatCell(Map map, IntVec3 origin, Pawn pawn, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || !origin.IsValid)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(origin, 18.9f, true))
            {
                if (!IsBossSafeStandableCell(map, candidate, pawn, false))
                {
                    continue;
                }

                if (pawn != null && pawn.Spawned && pawn.MapHeld == map && !pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                float dist = origin.DistanceTo(candidate);
                if (dist < 8f)
                {
                    continue;
                }

                float score = ScoreRetreatCell(map, candidate, pawn) + dist * 0.35f;
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

        public static bool TryFindSafeDashCellNearTarget(Pawn source, Pawn target, float landingRadius, out IntVec3 dashCell)
        {
            dashCell = IntVec3.Invalid;
            if (source == null || target == null || source.MapHeld == null || target.MapHeld != source.MapHeld)
            {
                return false;
            }

            Map map = source.MapHeld;
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Position, landingRadius, true))
            {
                if (!IsBossSafeStandableCell(map, cell, source, false))
                {
                    continue;
                }

                float distFromSource = source.Position.DistanceTo(cell);
                if (distFromSource < 4f)
                {
                    continue;
                }

                float score = -cell.DistanceTo(target.Position);

                if (GenSight.LineOfSight(cell, target.Position, map, true))
                {
                    score += 1.5f;
                }

                if (!cell.Roofed(map))
                {
                    score += 0.35f;
                }

                score += CountAdjacentStandableCells(map, cell) * 0.1f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            dashCell = bestCell;
            return true;
        }

        public static bool IsBossSafeStandableCell(Map map, IntVec3 cell, Pawn ignoredPawn, bool allowFogged)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            if (!allowFogged && cell.Fogged(map))
            {
                return false;
            }

            if (!cell.Standable(map))
            {
                return false;
            }

            Pawn occupant = cell.GetFirstPawn(map);
            if (occupant != null && occupant != ignoredPawn)
            {
                return false;
            }

            if (map.areaManager?.Home != null && map.areaManager.Home[cell])
            {
                return false;
            }

            Building edifice = cell.GetEdifice(map);
            if (edifice != null && edifice.Faction == Faction.OfPlayer)
            {
                return false;
            }

            return true;
        }

        public static bool TryFindPortalSpawnCell(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            for (int i = 0; i < 300; i++)
            {
                IntVec3 candidate = new IntVec3(Rand.Range(3, map.Size.x - 3), 0, Rand.Range(3, map.Size.z - 3));
                if (!candidate.InBounds(map) || !candidate.Standable(map) || candidate.Fogged(map))
                {
                    continue;
                }

                if (candidate.GetFirstPawn(map) != null)
                {
                    continue;
                }

                if (IsUnsafePortalCell(map, candidate))
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            return false;
        }

        public static bool TryGenerateImp(PawnKindDef kindDef, Faction faction, Map map, out Pawn pawn)
        {
            pawn = null;
            if (kindDef == null || faction == null || map == null)
            {
                return false;
            }

            PawnGenerationRequest request = new PawnGenerationRequest(
                kindDef,
                faction,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                colonistRelationChanceFactor: 0f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: false,
                allowPregnant: false,
                allowFood: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0f,
                biocodeApparelChance: 0f,
                extraPawnForExtraRelationChance: null,
                relationWithExtraPawnChanceFactor: 0f,
                validatorPreGear: null,
                validatorPostGear: null,
                fixedBirthName: null,
                fixedLastName: null,
                fixedGender: null,
                fixedIdeo: null,
                forceNoIdeo: true,
                developmentalStages: DevelopmentalStage.Adult);

            pawn = PawnGenerator.GeneratePawn(request);
            if (pawn != null)
            {
                AbyssalDifficultyUtility.ApplyPawnDifficulty(pawn, kindDef);
                return true;
            }

            return false;
        }

        public static void GiveAssaultLord(Pawn pawn)
        {
            if (pawn == null || pawn.Faction == null || pawn.MapHeld == null)
            {
                return;
            }

            AbyssalLordUtility.EnsureAssaultLord(pawn, sappers: false);
        }

        private static IntVec3 RandomEdgeCell(Map map)
        {
            int edge = Rand.RangeInclusive(0, 3);
            switch (edge)
            {
                case 0:
                    return new IntVec3(1, 0, Rand.RangeInclusive(1, map.Size.z - 2));
                case 1:
                    return new IntVec3(map.Size.x - 2, 0, Rand.RangeInclusive(1, map.Size.z - 2));
                case 2:
                    return new IntVec3(Rand.RangeInclusive(1, map.Size.x - 2), 0, 1);
                default:
                    return new IntVec3(Rand.RangeInclusive(1, map.Size.x - 2), 0, map.Size.z - 2);
            }
        }

        private static float ScoreRetreatCell(Map map, IntVec3 candidate, Pawn pawn)
        {
            float nearestColonistDist = 999f;
            if (map != null)
            {
                foreach (Pawn other in map.mapPawns.AllPawnsSpawned)
                {
                    if (other == null || other.Dead || other.Downed || other.Faction != Faction.OfPlayer)
                    {
                        continue;
                    }

                    float dist = candidate.DistanceTo(other.Position);
                    if (dist < nearestColonistDist)
                    {
                        nearestColonistDist = dist;
                    }
                }
            }

            float edgeBias = 0f;
            if (map != null)
            {
                int edgeDistance = candidate.x;
                edgeDistance = Mathf.Min(edgeDistance, map.Size.x - 1 - candidate.x);
                edgeDistance = Mathf.Min(edgeDistance, candidate.z);
                edgeDistance = Mathf.Min(edgeDistance, map.Size.z - 1 - candidate.z);
                edgeBias = -edgeDistance * 0.6f;
            }

            float originBias = 0f;
            if (pawn != null && pawn.Spawned && pawn.MapHeld == map)
            {
                originBias = candidate.DistanceTo(pawn.Position) * 0.15f;
            }

            return nearestColonistDist + edgeBias + originBias;
        }

        private static int CountAdjacentStandableCells(Map map, IntVec3 center)
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                IntVec3 cell = center + GenAdj.AdjacentCells[i];
                if (cell.InBounds(map) && cell.Standable(map))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasBlockingPlayerBuildingNearby(Map map, IntVec3 center, float radius)
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

        private static bool IsUnsafePortalCell(Map map, IntVec3 cell)
        {
            if (map.areaManager?.Home != null && map.areaManager.Home[cell])
            {
                return true;
            }

            foreach (IntVec3 near in GenRadial.RadialCellsAround(cell, 9f, true))
            {
                if (!near.InBounds(map))
                {
                    continue;
                }

                if (map.areaManager?.Home != null && map.areaManager.Home[near])
                {
                    return true;
                }

                List<Thing> things = near.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed)
                    {
                        continue;
                    }

                    if (thing.Faction == Faction.OfPlayer && thing.def != null && thing.def.category == ThingCategory.Building)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
