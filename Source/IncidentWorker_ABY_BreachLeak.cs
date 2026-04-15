using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class IncidentWorker_ABY_BreachLeak : IncidentWorker
    {
        private const string SummoningCircleDefName = "ABY_SummoningCircle";
        private const string ImpPortalDefName = "ABY_ImpPortal";
        private const string RiftImpPawnKindDefName = "ABY_RiftImp";
        private const string EmberHoundPawnKindDefName = "ABY_EmberHound";

        private const int MinActiveColonists = 3;
        private const int PortalWarmupTicks = 42;
        private const int PortalLingerTicks = 160;
        private const int PortalSpawnIntervalFast = 14;
        private const int PortalSpawnIntervalSlow = 18;
        private const float PortalMinRadius = 18f;
        private const float PortalMaxRadius = 42f;
        private const float PortalSpacing = 6.5f;
        private const float HomeAreaExclusionRadius = 8.9f;
        private const float BuildingExclusionRadius = 3.1f;
        private const float HoundSpawnRadius = 6.9f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Map map) || map == null)
            {
                return false;
            }

            if (AbyssalBossSummonUtility.HasActiveAbyssalEncounter(map))
            {
                return false;
            }

            if (ABY_Phase2PortalUtility.CountActivePlayerColonists(map) < MinActiveColonists)
            {
                return false;
            }

            if (!TryFindAnySummoningCircle(map, out _))
            {
                return false;
            }

            return AbyssalBossSummonUtility.ResolveHostileFaction() != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!(parms.target is Map map) || map == null)
            {
                return false;
            }

            if (!TryFindAnySummoningCircle(map, out Building_AbyssalSummoningCircle circle))
            {
                return false;
            }

            if (AbyssalBossSummonUtility.HasActiveAbyssalEncounter(map))
            {
                return false;
            }

            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            if (hostileFaction == null)
            {
                return false;
            }

            ThingDef impPortalDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpPortalDefName);
            PawnKindDef impKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(RiftImpPawnKindDefName);
            PawnKindDef houndKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(EmberHoundPawnKindDefName);
            if (impPortalDef == null || impKindDef == null || houndKindDef == null)
            {
                return false;
            }

            int activeColonists = ABY_Phase2PortalUtility.CountActivePlayerColonists(map);
            float budget = CalculateBudget(map, parms, activeColonists);
            int desiredPortalCount = CalculatePortalCount(budget);
            int totalImpCount = CalculateImpCount(budget, activeColonists);
            int totalHoundCount = CalculateHoundCount(budget, activeColonists);
            int portalInterval = budget >= 280f ? PortalSpawnIntervalFast : PortalSpawnIntervalSlow;

            List<IntVec3> portalCells = new List<IntVec3>(desiredPortalCount);
            for (int i = 0; i < desiredPortalCount; i++)
            {
                if (!TryFindIncidentPortalCell(map, circle.Position, portalCells, out IntVec3 cell))
                {
                    break;
                }

                portalCells.Add(cell);
            }

            if (portalCells.Count == 0 && ABY_Phase2PortalUtility.TryFindPortalSpawnCell(map, out IntVec3 fallbackCell))
            {
                portalCells.Add(fallbackCell);
            }

            if (portalCells.Count == 0)
            {
                return false;
            }

            int remainingImps = totalImpCount;
            for (int i = 0; i < portalCells.Count; i++)
            {
                int portalsRemaining = portalCells.Count - i;
                int impsForPortal = Mathf.Clamp(Mathf.CeilToInt((float)remainingImps / portalsRemaining), 1, remainingImps);

                if (!TrySpawnPortal(map, hostileFaction, impPortalDef, impKindDef, portalCells[i], impsForPortal, portalInterval))
                {
                    return false;
                }

                remainingImps -= impsForPortal;
            }

            int spawnedHounds = 0;
            if (totalHoundCount > 0)
            {
                spawnedHounds = TrySpawnHounds(map, hostileFaction, houndKindDef, portalCells, totalHoundCount);
            }

            string letterLabel = "ABY_IncidentBreachLeakLetterLabel".Translate();
            string letterText = spawnedHounds > 0
                ? "ABY_IncidentBreachLeakLetterText_WithHounds".Translate(portalCells.Count, totalImpCount, spawnedHounds)
                : "ABY_IncidentBreachLeakLetterText_ImpsOnly".Translate(portalCells.Count, totalImpCount);

            LetterDef letterDef = spawnedHounds > 0 || portalCells.Count > 1
                ? LetterDefOf.ThreatBig
                : LetterDefOf.ThreatSmall;

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, letterDef, new TargetInfo(portalCells[0], map));
            return true;
        }

        private static float CalculateBudget(Map map, IncidentParms parms, int activeColonists)
        {
            float colonyWealth = map.wealthWatcher?.WealthTotal ?? 0f;
            float wealthDrivenBudget = 55f + (activeColonists * 22f) + (Mathf.Sqrt(Mathf.Max(0f, colonyWealth)) * 0.55f);
            float storytellerBudget = Mathf.Max(0f, parms.points) * 0.60f;
            return Mathf.Max(wealthDrivenBudget, storytellerBudget);
        }

        private static int CalculatePortalCount(float budget)
        {
            if (budget >= 360f)
            {
                return 3;
            }

            if (budget >= 200f)
            {
                return 2;
            }

            return 1;
        }

        private static int CalculateImpCount(float budget, int activeColonists)
        {
            int colonyBonus = Mathf.Max(0, activeColonists - 4) / 3;
            return Mathf.Clamp(2 + Mathf.RoundToInt(budget / 85f) + colonyBonus, 3, 10);
        }

        private static int CalculateHoundCount(float budget, int activeColonists)
        {
            if (activeColonists < 4 || budget < 190f)
            {
                return 0;
            }

            return budget >= 420f ? 2 : 1;
        }

        private static bool TryFindAnySummoningCircle(Map map, out Building_AbyssalSummoningCircle circle)
        {
            circle = null;
            if (map == null)
            {
                return false;
            }

            ThingDef circleDef = DefDatabase<ThingDef>.GetNamedSilentFail(SummoningCircleDefName);
            if (circleDef == null)
            {
                return false;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(circleDef);
            if (things == null)
            {
                return false;
            }

            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Building_AbyssalSummoningCircle candidate && candidate.Spawned && !candidate.Destroyed)
                {
                    circle = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TrySpawnPortal(
            Map map,
            Faction hostileFaction,
            ThingDef portalDef,
            PawnKindDef impKindDef,
            IntVec3 cell,
            int impCount,
            int spawnIntervalTicks)
        {
            Building_AbyssalImpPortal portal = ThingMaker.MakeThing(portalDef) as Building_AbyssalImpPortal;
            if (portal == null)
            {
                return false;
            }

            GenSpawn.Spawn(portal, cell, map, Rot4.Random);
            portal.Initialize(hostileFaction, impKindDef, impCount, PortalWarmupTicks, spawnIntervalTicks, PortalLingerTicks);
            ArchonInfernalVFXUtility.DoSummonVFX(map, cell);
            ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", cell, map);
            return true;
        }

        private static int TrySpawnHounds(
            Map map,
            Faction hostileFaction,
            PawnKindDef houndKindDef,
            List<IntVec3> portalCells,
            int desiredCount)
        {
            if (desiredCount <= 0 || portalCells == null || portalCells.Count == 0)
            {
                return 0;
            }

            List<Pawn> spawnedHounds = new List<Pawn>(desiredCount);
            List<IntVec3> usedCells = new List<IntVec3>(desiredCount);

            for (int i = 0; i < desiredCount; i++)
            {
                IntVec3 anchor = portalCells[i % portalCells.Count];
                if (!TryFindHoundSpawnCell(map, anchor, usedCells, out IntVec3 spawnCell))
                {
                    continue;
                }

                if (!TryGenerateHostilePawn(map, houndKindDef, hostileFaction, out Pawn hound))
                {
                    continue;
                }

                GenSpawn.Spawn(hound, spawnCell, map, Rot4.Random);
                ArchonInfernalVFXUtility.DoSummonVFX(map, spawnCell);
                usedCells.Add(spawnCell);
                spawnedHounds.Add(hound);
            }

            if (spawnedHounds.Count > 0)
            {
                LordJob lordJob = new LordJob_AssaultColony(
                    hostileFaction,
                    canKidnap: false,
                    canTimeoutOrFlee: false,
                    sappers: false,
                    useAvoidGridSmart: true,
                    canSteal: false);

                LordMaker.MakeNewLord(hostileFaction, lordJob, map, spawnedHounds);
            }

            return spawnedHounds.Count;
        }

        private static bool TryGenerateHostilePawn(Map map, PawnKindDef kindDef, Faction faction, out Pawn pawn)
        {
            pawn = null;
            if (map == null || kindDef == null || faction == null)
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

        private static bool TryFindIncidentPortalCell(
            Map map,
            IntVec3 origin,
            List<IntVec3> reservedPortalCells,
            out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || !origin.IsValid)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;
            float preferredRadius = (PortalMinRadius + PortalMaxRadius) * 0.5f;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(origin, PortalMaxRadius, true))
            {
                if (!candidate.InBounds(map) || candidate.Fogged(map) || !candidate.Standable(map))
                {
                    continue;
                }

                float distance = candidate.DistanceTo(origin);
                if (distance < PortalMinRadius || distance > PortalMaxRadius)
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

                if (HasReservedPortalTooClose(candidate, reservedPortalCells))
                {
                    continue;
                }

                if (HasHomeAreaNearby(map, candidate, HomeAreaExclusionRadius))
                {
                    continue;
                }

                if (HasBlockingPlayerBuildingNearby(map, candidate, BuildingExclusionRadius))
                {
                    continue;
                }

                float score = -Mathf.Abs(distance - preferredRadius);
                score += CountAdjacentStandableCells(map, candidate) * 0.08f;
                if (!candidate.Roofed(map))
                {
                    score += 0.35f;
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

        private static bool TryFindHoundSpawnCell(
            Map map,
            IntVec3 anchor,
            List<IntVec3> reservedCells,
            out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || !anchor.IsValid)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(anchor, HoundSpawnRadius, true))
            {
                if (!ABY_Phase2PortalUtility.IsBossSafeStandableCell(map, candidate, null, false))
                {
                    continue;
                }

                if (candidate.DistanceTo(anchor) < 1.9f)
                {
                    continue;
                }

                if (HasReservedPortalTooClose(candidate, reservedCells))
                {
                    continue;
                }

                float score = -candidate.DistanceTo(anchor);
                score += CountAdjacentStandableCells(map, candidate) * 0.1f;
                if (!candidate.Roofed(map))
                {
                    score += 0.2f;
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

        private static bool HasReservedPortalTooClose(IntVec3 candidate, List<IntVec3> reservedCells)
        {
            if (reservedCells == null)
            {
                return false;
            }

            for (int i = 0; i < reservedCells.Count; i++)
            {
                if (candidate.DistanceTo(reservedCells[i]) < PortalSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasHomeAreaNearby(Map map, IntVec3 center, float radius)
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
    }
}
