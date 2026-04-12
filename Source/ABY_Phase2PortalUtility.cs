using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

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

        public static bool TryFindRetreatEdgeCell(Map map, IntVec3 fromCell, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            for (int i = 0; i < 180; i++)
            {
                IntVec3 candidate = RandomEdgeCell(map);
                if (!candidate.InBounds(map) || !candidate.Standable(map) || candidate.Fogged(map))
                {
                    continue;
                }

                if (candidate.GetFirstPawn(map) != null)
                {
                    continue;
                }

                if (fromCell.IsValid && map.reachability != null && !map.reachability.CanReach(fromCell, candidate, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly))
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            return false;
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
                if (!candidate.InBounds(map) || !candidate.Standable(map))
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
            return pawn != null;
        }

        public static void GiveAssaultLord(Pawn pawn)
        {
            if (pawn == null || pawn.Faction == null || pawn.MapHeld == null)
            {
                return;
            }

            LordJob lordJob = new LordJob_AssaultColony(
                pawn.Faction,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: false,
                useAvoidGridSmart: true,
                canSteal: false);

            LordMaker.MakeNewLord(pawn.Faction, lordJob, pawn.MapHeld, new List<Pawn> { pawn });
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
