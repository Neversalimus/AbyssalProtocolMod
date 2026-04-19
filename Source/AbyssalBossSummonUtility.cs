using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace AbyssalProtocol
{
    public static class AbyssalBossSummonUtility
    {
        private const string ArchonBeastRaceDefName = "ABY_ArchonBeast";
        private const string ArchonOfRuptureRaceDefName = "ABY_ArchonOfRupture";
        private const string WardenOfAshRaceDefName = "ABY_WardenOfAsh";
        private const string RiftImpRaceDefName = "ABY_RiftImp";
        private const string EmberHoundRaceDefName = "ABY_EmberHound";
        private const string HexgunThrallRaceDefName = "ABY_HexgunThrall";
        private const string ChainZealotRaceDefName = "ABY_ChainZealot";
        private const string NullPriestRaceDefName = "ABY_NullPriest";
        private const string ChoirEngineRaceDefName = "ABY_ChoirEngine";
        private const string RupturePortalDefName = "ABY_RupturePortal";
        private const string ImpPortalDefName = "ABY_ImpPortal";

        public static Faction ResolveHostileFaction()
        {
            FactionDef abyssalDef = DefDatabase<FactionDef>.GetNamedSilentFail("ABY_AbyssalHost");
            if (abyssalDef != null)
            {
                Faction abyssal = Find.FactionManager.FirstFactionOfDef(abyssalDef);
                if (abyssal != null)
                {
                    return abyssal;
                }

                try
                {
                    Faction generated = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(abyssalDef));
                    if (generated != null)
                    {
                        if (!Find.FactionManager.AllFactionsListForReading.Contains(generated))
                        {
                            Find.FactionManager.Add(generated);
                        }

                        return generated;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[AbyssalProtocol] Failed to generate ABY_AbyssalHost faction, falling back to vanilla hostile factions: {ex}");
                }
            }

            FactionDef ancientsHostileDef = DefDatabase<FactionDef>.GetNamedSilentFail("AncientsHostile");
            if (ancientsHostileDef != null)
            {
                Faction ancients = Find.FactionManager.FirstFactionOfDef(ancientsHostileDef);
                if (ancients != null)
                {
                    return ancients;
                }
            }

            Faction randomEnemy = Find.FactionManager.RandomEnemyFaction(false, false, false, TechLevel.Spacer);
            if (randomEnemy != null)
            {
                return randomEnemy;
            }

            FactionDef pirateDef = DefDatabase<FactionDef>.GetNamedSilentFail("Pirate")
                ?? DefDatabase<FactionDef>.GetNamedSilentFail("Pirates");
            if (pirateDef != null)
            {
                return Find.FactionManager.FirstFactionOfDef(pirateDef);
            }

            return null;
        }

        public static bool TryFindBossArrivalCell(Map map, out IntVec3 cell)
        {
            for (int i = 0; i < 8; i++)
            {
                if (CellFinder.TryFindRandomEdgeCellWith(
                    c => c.Standable(map) && !c.Fogged(map),
                    map,
                    CellFinder.EdgeRoadChance_Hostile,
                    out cell))
                {
                    return true;
                }
            }

            cell = IntVec3.Invalid;
            return false;
        }

        public static bool TryFindNearColonyArrivalCell(
            Map map,
            IntVec3 fallbackOrigin,
            float minDistance,
            float maxDistance,
            out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            IntVec3 anchor = GetColonyAnchorCell(map, fallbackOrigin);
            float min = (float)System.Math.Max(6d, minDistance);
            float max = (float)System.Math.Max(min + 2f, maxDistance);
            float preferredRadius = (min + max) * 0.5f;
            int radialCount = GenRadial.NumCellsInRadius(max);

            IntVec3 bestReachable = IntVec3.Invalid;
            float bestReachableScore = float.MinValue;
            IntVec3 bestFallback = IntVec3.Invalid;
            float bestFallbackScore = float.MinValue;

            for (int i = 0; i < radialCount; i++)
            {
                IntVec3 candidate = anchor + GenRadial.RadialPattern[i];
                if (!candidate.InBounds(map))
                {
                    continue;
                }

                float distance = candidate.DistanceTo(anchor);
                if (distance < min || distance > max)
                {
                    continue;
                }

                if (!IsValidNearColonyArrivalCell(map, candidate))
                {
                    continue;
                }

                bool reachableToAnchor = CanReachColonyAnchor(map, candidate, anchor);
                float score = ScoreNearColonyArrivalCell(map, candidate, anchor, distance, preferredRadius, reachableToAnchor);

                if (reachableToAnchor)
                {
                    if (score > bestReachableScore)
                    {
                        bestReachableScore = score;
                        bestReachable = candidate;
                    }

                    continue;
                }

                if (score > bestFallbackScore)
                {
                    bestFallbackScore = score;
                    bestFallback = candidate;
                }
            }

            if (bestReachable.IsValid)
            {
                cell = bestReachable;
                return true;
            }

            if (bestFallback.IsValid)
            {
                cell = bestFallback;
                return true;
            }

            if (IsValidNearColonyArrivalCell(map, anchor))
            {
                cell = anchor;
                return true;
            }

            return TryFindBossArrivalCell(map, out cell);
        }

        public static bool TryResolveBossManifestationCell(Map map, ThingDef manifestationDef, IntVec3 requestedCell, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || manifestationDef == null)
            {
                return false;
            }

            if (IsUsableBossManifestationCell(map, manifestationDef, requestedCell))
            {
                cell = requestedCell;
                return true;
            }

            IntVec3 anchor = requestedCell.IsValid && requestedCell.InBounds(map) ? requestedCell : map.Center;
            int maxRadius = System.Math.Min(System.Math.Min(map.Size.x, map.Size.z) / 2, 45);

            for (int radius = 1; radius <= maxRadius; radius += 2)
            {
                int maxCells = System.Math.Min(GenRadial.NumCellsInRadius(radius), GenRadial.RadialPattern.Length);
                for (int i = 0; i < maxCells; i++)
                {
                    IntVec3 candidate = anchor + GenRadial.RadialPattern[i];
                    if (IsUsableBossManifestationCell(map, manifestationDef, candidate))
                    {
                        cell = candidate;
                        return true;
                    }
                }
            }

            return TryFindBossArrivalCell(map, out cell);
        }

        private static bool IsUsableBossManifestationCell(Map map, ThingDef manifestationDef, IntVec3 cell)
        {
            if (map == null || manifestationDef == null || !cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            if (!cell.Standable(map) || !cell.Walkable(map) || cell.Fogged(map) || cell.Roofed(map))
            {
                return false;
            }

            if (cell.GetEdifice(map) != null)
            {
                return false;
            }

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain != null && terrain.IsWater)
            {
                return false;
            }

            if (HasBlockingPlayerBuildingNearby(map, cell, 2.6f))
            {
                return false;
            }

            if (CountAdjacentStandableCells(map, cell) < 4)
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn || things[i] is Building)
                {
                    return false;
                }
            }

            return true;
        }

        public static IntVec3 GetColonyAnchorCell(Map map, IntVec3 fallbackOrigin)
        {
            Area home = map.areaManager?.Home;
            if (home != null)
            {
                int totalX = 0;
                int totalZ = 0;
                int count = 0;

                foreach (IntVec3 cell in home.ActiveCells)
                {
                    totalX += cell.x;
                    totalZ += cell.z;
                    count++;
                }

                if (count > 0)
                {
                    return new IntVec3(totalX / count, 0, totalZ / count);
                }
            }

            List<Building> colonistBuildings = map.listerBuildings?.allBuildingsColonist;
            if (colonistBuildings != null && colonistBuildings.Count > 0)
            {
                int totalX = 0;
                int totalZ = 0;
                int count = 0;
                for (int i = 0; i < colonistBuildings.Count; i++)
                {
                    Building building = colonistBuildings[i];
                    if (building == null || !building.Spawned)
                    {
                        continue;
                    }

                    totalX += building.Position.x;
                    totalZ += building.Position.z;
                    count++;
                }

                if (count > 0)
                {
                    return new IntVec3(totalX / count, 0, totalZ / count);
                }
            }

            List<Pawn> colonists = map.mapPawns?.FreeColonistsSpawned;
            if (colonists != null && colonists.Count > 0)
            {
                int totalX = 0;
                int totalZ = 0;
                int count = 0;
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    if (pawn == null || !pawn.Spawned)
                    {
                        continue;
                    }

                    totalX += pawn.Position.x;
                    totalZ += pawn.Position.z;
                    count++;
                }

                if (count > 0)
                {
                    return new IntVec3(totalX / count, 0, totalZ / count);
                }
            }

            return fallbackOrigin.IsValid ? fallbackOrigin : map.Center;
        }

        private static bool IsValidNearColonyArrivalCell(Map map, IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            if (cell.x < 8 || cell.z < 8 || cell.x >= map.Size.x - 8 || cell.z >= map.Size.z - 8)
            {
                return false;
            }

            if (!cell.Standable(map) || !cell.Walkable(map) || cell.Fogged(map) || cell.Roofed(map))
            {
                return false;
            }

            if (cell.GetEdifice(map) != null)
            {
                return false;
            }

            if (map.areaManager?.Home != null && map.areaManager.Home[cell])
            {
                return false;
            }

            if (HasBlockingPlayerBuildingNearby(map, cell, 2.9f))
            {
                return false;
            }

            if (CountAdjacentStandableCells(map, cell) < 5)
            {
                return false;
            }

            return true;
        }

        public static bool TryFindEscortPortalCellNear(Map map, IntVec3 origin, out IntVec3 cell, float minRadius = 5.5f, float maxRadius = 10.5f)
        {
            cell = IntVec3.Invalid;
            if (map == null || !origin.IsValid)
            {
                return false;
            }

            IntVec3 colonyAnchor = GetColonyAnchorCell(map, origin);
            float preferredRadius = (minRadius + maxRadius) * 0.5f;
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(origin, maxRadius, true))
            {
                if (!candidate.InBounds(map) || candidate.Fogged(map))
                {
                    continue;
                }

                float distance = candidate.DistanceTo(origin);
                if (distance < minRadius || distance > maxRadius)
                {
                    continue;
                }

                if (!IsValidEscortPortalCell(map, candidate))
                {
                    continue;
                }

                float score = -Mathf.Abs(distance - preferredRadius);
                score += CountAdjacentStandableCells(map, candidate) * 0.08f;
                if (!candidate.Roofed(map))
                {
                    score += 0.25f;
                }

                if (colonyAnchor.IsValid)
                {
                    score -= candidate.DistanceTo(colonyAnchor) * 0.04f;
                    if (CanReachColonyAnchor(map, candidate, colonyAnchor))
                    {
                        score += 1.1f;
                    }
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

        private static bool IsValidEscortPortalCell(Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            if (!cell.Standable(map) || cell.Fogged(map))
            {
                return false;
            }

            if (cell.GetFirstPawn(map) != null)
            {
                return false;
            }

            if (cell.GetEdifice(map) != null)
            {
                return false;
            }

            if (map.areaManager?.Home != null && map.areaManager.Home[cell])
            {
                return false;
            }

            if (HasBlockingPlayerBuildingNearby(map, cell, 2.4f))
            {
                return false;
            }

            if (CountAdjacentStandableCells(map, cell) < 4)
            {
                return false;
            }

            return true;
        }

        private static float ScoreNearColonyArrivalCell(Map map, IntVec3 cell, IntVec3 anchor, float distanceToAnchor, float preferredRadius, bool reachableToAnchor)
        {
            float score = -Mathf.Abs(distanceToAnchor - preferredRadius);
            score += CountAdjacentStandableCells(map, cell) * 0.09f;
            if (!cell.Roofed(map))
            {
                score += 0.20f;
            }

            if (reachableToAnchor)
            {
                score += 2.2f;
            }

            if (anchor.IsValid)
            {
                Vector3 anchorVector = new Vector3(anchor.x - cell.x, 0f, anchor.z - cell.z);
                score -= anchorVector.magnitude * 0.015f;
            }

            return score;
        }

        private static bool CanReachColonyAnchor(Map map, IntVec3 origin, IntVec3 anchor)
        {
            if (map == null || !origin.IsValid || !anchor.IsValid || !anchor.InBounds(map))
            {
                return false;
            }

            if (origin == anchor)
            {
                return true;
            }

            try
            {
                return map.reachability != null && map.reachability.CanReach(origin, anchor, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false));
            }
            catch
            {
                return false;
            }
        }

        private static int CountAdjacentStandableCells(Map map, IntVec3 center)
        {
            if (map == null || !center.IsValid)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < GenAdj.AdjacentCells.Length; i++)
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
            if (map == null || !center.IsValid)
            {
                return false;
            }

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

        public static bool TryGenerateBoss(
            Map map,
            PawnKindDef kindDef,
            Faction faction,
            string bossLabel,
            out Pawn pawn,
            out string failReason)
        {
            pawn = null;
            failReason = null;

            if (map == null)
            {
                failReason = "No map available for summoned boss.";
                return false;
            }

            if (kindDef == null)
            {
                failReason = "Missing PawnKindDef for summoned boss.";
                return false;
            }

            if (faction == null)
            {
                failReason = "No hostile faction available for summoned boss.";
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
            if (pawn == null)
            {
                failReason = "Failed to generate the summoned boss pawn.";
                return false;
            }

            PrepareBoss(pawn, bossLabel);
            return true;
        }

        public static void FinalizeBossArrival(
            Pawn pawn,
            Faction faction,
            Map map,
            IntVec3 spawnCell,
            string bossLabel,
            string arrivalSoundDefName = "ABY_ArchonBossArrive",
            string completionLetterLabelKey = null,
            string completionLetterDescKey = null)
        {
            if (pawn == null || faction == null || map == null || !spawnCell.IsValid)
            {
                return;
            }

            GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
            ArchonInfernalVFXUtility.DoSummonVFX(map, spawnCell);
            ABY_SoundUtility.PlayAt(arrivalSoundDefName, spawnCell, map);

            AbyssalBossScreenFXGameComponent fxComp = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();
            fxComp?.RegisterBoss(pawn, bossLabel);

            KickstartBossAggression(pawn);
            string letterLabel = completionLetterLabelKey.NullOrEmpty()
                ? "ABY_BossSummonSuccessLabel".Translate()
                : completionLetterLabelKey.Translate();
            string letterDesc = completionLetterDescKey.NullOrEmpty()
                ? "ABY_BossSummonSuccessDesc".Translate(bossLabel)
                : completionLetterDescKey.Translate();
            Find.LetterStack.ReceiveLetter(
                letterLabel,
                letterDesc,
                LetterDefOf.ThreatBig,
                new TargetInfo(spawnCell, map));
        }

        private static void KickstartBossAggression(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.MapHeld == null)
            {
                return;
            }

            AbyssalThreatPawnUtility.EnsureHostileFaction(pawn);
            AbyssalThreatPawnUtility.PrepareThreatPawn(pawn);
            AbyssalDifficultyUtility.ApplyDifficultyScaling(pawn);
            pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
            pawn.pather?.StopDead();
            AbyssalLordUtility.EnsureAssaultLord(pawn, sappers: true);

            Pawn initialTarget = AbyssalThreatPawnUtility.FindBestTarget(pawn, 0f, 45f, false, true, false, 4f, 0.5f)
                ?? AbyssalThreatPawnUtility.FindClosestThreatWithin(pawn, 45f);

            IntVec3 colonyAnchor = GetColonyAnchorCell(pawn.MapHeld, pawn.PositionHeld);

            if (initialTarget == null)
            {
                if (colonyAnchor.IsValid && colonyAnchor != pawn.PositionHeld)
                {
                    pawn.rotationTracker?.FaceCell(colonyAnchor);
                    pawn.pather?.StartPath(colonyAnchor, PathEndMode.OnCell);
                }

                return;
            }

            pawn.rotationTracker?.FaceTarget(initialTarget.Position);
            if (pawn.Position.DistanceTo(initialTarget.Position) > 8.5f)
            {
                pawn.pather?.StartPath(initialTarget, PathEndMode.Touch);
            }
            else if (colonyAnchor.IsValid && pawn.Position.DistanceTo(colonyAnchor) > 6.5f)
            {
                pawn.pather?.StartPath(colonyAnchor, PathEndMode.OnCell);
            }
        }

        public static bool TryFindNearestAvailableCircle(
            Map map,
            IntVec3 origin,
            out Building_AbyssalSummoningCircle circle,
            out string failReason)
        {
            circle = null;
            failReason = null;

            if (map == null)
            {
                failReason = "No map available.";
                return false;
            }

            if (HasActiveAbyssalEncounter(map))
            {
                failReason = "ABY_BossSummonFail_EncounterActive".Translate();
                return false;
            }

            ThingDef circleDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_SummoningCircle");
            if (circleDef == null)
            {
                failReason = "Missing ThingDef: ABY_SummoningCircle";
                return false;
            }

            IEnumerable<Thing> circles = map.listerThings.ThingsOfDef(circleDef);
            Building_AbyssalSummoningCircle best = null;
            float bestDistance = float.MaxValue;
            bool foundAny = false;
            bool foundBusy = false;
            bool foundUnpowered = false;
            bool foundBlocked = false;

            foreach (Thing thing in circles)
            {
                if (!(thing is Building_AbyssalSummoningCircle candidate) || !candidate.Spawned || candidate.Destroyed)
                {
                    continue;
                }

                foundAny = true;
                if (candidate.RitualActive)
                {
                    foundBusy = true;
                    continue;
                }

                if (!candidate.IsPoweredForRitual)
                {
                    foundUnpowered = true;
                    continue;
                }

                if (!candidate.HasValidInteractionCell(out _) || !candidate.HasClearRitualFocus(out _))
                {
                    foundBlocked = true;
                    continue;
                }

                float distance = candidate.Position.DistanceToSquared(origin);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            if (best == null)
            {
                if (!foundAny)
                {
                    failReason = "ABY_BossSummonFail_NoCircle".Translate();
                }
                else if (foundBusy && !foundUnpowered && !foundBlocked)
                {
                    failReason = "ABY_BossSummonFail_AllBusy".Translate();
                }
                else if (foundUnpowered && !foundBusy && !foundBlocked)
                {
                    failReason = "ABY_BossSummonFail_AllUnpowered".Translate();
                }
                else if (foundBlocked && !foundBusy && !foundUnpowered)
                {
                    failReason = "ABY_BossSummonFail_AllBlocked".Translate();
                }
                else
                {
                    failReason = "ABY_BossSummonFail_NoCircleAvailable".Translate();
                }

                return false;
            }

            circle = best;
            return true;
        }

        public static bool HasActiveAbyssalEncounter(Map map)
        {
            if (map == null)
            {
                return false;
            }

            MapComponent_DominionCrisis dominionCrisis = map.GetComponent<MapComponent_DominionCrisis>();
            if (dominionCrisis != null && dominionCrisis.IsActive)
            {
                return true;
            }

            MapComponent_AbyssalPortalWave portalWave = map.GetComponent<MapComponent_AbyssalPortalWave>();
            if (portalWave != null && portalWave.IsWaveActive)
            {
                return true;
            }

            if (map.mapPawns != null)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn == null || pawn.Destroyed || pawn.Dead)
                    {
                        continue;
                    }

                    if (IsActiveEncounterPawn(pawn.def?.defName))
                    {
                        return true;
                    }
                }
            }

            if (HasActivePortalOfDef(map, RupturePortalDefName))
            {
                return true;
            }

            if (HasActivePortalOfDef(map, ImpPortalDefName))
            {
                return true;
            }

            return false;
        }

        private static bool IsActiveEncounterPawn(string defName)
        {
            return defName == ArchonBeastRaceDefName
                || defName == ArchonOfRuptureRaceDefName
                || defName == RiftImpRaceDefName
                || defName == EmberHoundRaceDefName
                || defName == HexgunThrallRaceDefName
                || defName == ChainZealotRaceDefName
                || defName == NullPriestRaceDefName
                || defName == ChoirEngineRaceDefName;
        }

        private static bool HasActivePortalOfDef(Map map, string defName)
        {
            ThingDef portalDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (portalDef == null)
            {
                return false;
            }

            List<Thing> portals = map.listerThings.ThingsOfDef(portalDef);
            if (portals == null)
            {
                return false;
            }

            for (int i = 0; i < portals.Count; i++)
            {
                Thing portal = portals[i];
                if (portal != null && portal.Spawned && !portal.Destroyed)
                {
                    return true;
                }
            }

            return false;
        }

        private static void PrepareBoss(Pawn pawn, string bossLabel)
        {
            if (pawn == null)
            {
                return;
            }

            pawn.Name = new NameSingle(bossLabel);
            if (pawn.story != null)
            {
                pawn.story.title = bossLabel;
            }

            AbyssalThreatPawnUtility.PrepareThreatPawn(pawn);

            if (pawn.RaceProps != null && pawn.RaceProps.Humanlike)
            {
                PrepareHumanlikeBoss(pawn);
            }
            else
            {
                PrepareMonsterBoss(pawn);
            }
        }

        private static void PrepareHumanlikeBoss(Pawn pawn)
        {
            if (pawn.equipment != null)
            {
                pawn.equipment.DestroyAllEquipment();
            }

            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_RiftCarbine");
            if (weaponDef != null && pawn.equipment != null)
            {
                Thing weapon = ThingMaker.MakeThing(weaponDef);
                if (weapon is ThingWithComps thingWithComps)
                {
                    pawn.equipment.AddEquipment(thingWithComps);
                }
            }

            HediffDef eyeDef = DefDatabase<HediffDef>.GetNamedSilentFail("ABY_InfernalEye_Implant");
            if (eyeDef != null)
            {
                BodyPartRecord eyePart = pawn.health?.hediffSet?.GetNotMissingParts()?.FirstOrDefault(part => part.def == BodyPartDefOf.Eye);
                if (eyePart != null)
                {
                    pawn.health.AddHediff(eyeDef, eyePart);
                }
            }

            if (pawn.skills != null)
            {
                SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (shooting != null)
                {
                    shooting.Level = 16;
                }

                SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                if (melee != null)
                {
                    melee.Level = 12;
                }

                SkillRecord intellectual = pawn.skills.GetSkill(SkillDefOf.Intellectual);
                if (intellectual != null)
                {
                    intellectual.Level = 8;
                }
            }
        }

        private static void PrepareMonsterBoss(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            string raceDefName = pawn.def?.defName;
            string coreDefName = null;
            string carapaceDefName = null;

            if (raceDefName == ArchonOfRuptureRaceDefName)
            {
                coreDefName = "ABY_RuptureCore";
                carapaceDefName = "ABY_RuptureCarapace";
            }
            else if (raceDefName == ArchonBeastRaceDefName)
            {
                coreDefName = "ABY_ArchonCore";
                carapaceDefName = "ABY_ArchonCarapace";
            }
            else if (raceDefName == WardenOfAshRaceDefName)
            {
                // Warden of Ash has its own comp-driven mechanics and must not inherit Archon phase logic.
                return;
            }
            else
            {
                return;
            }

            TryAddUniqueHediff(pawn, coreDefName);
            TryAddUniqueHediff(pawn, carapaceDefName);
        }

        private static void TryAddUniqueHediff(Pawn pawn, string hediffDefName)
        {
            if (pawn == null || string.IsNullOrWhiteSpace(hediffDefName) || pawn.health == null)
            {
                return;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (hediffDef == null)
            {
                return;
            }

            if (pawn.health.hediffSet?.HasHediff(hediffDef) == true)
            {
                return;
            }

            pawn.health.AddHediff(hediffDef);
        }
    }
}
