using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class ABY_ReactorSaintAIUtility
    {
        private const string ReactorSaintDefName = "ABY_ReactorSaint";
        private const int ShortWaitTicks = 24;
        private const int TacticalGotoExpiryTicks = 120;
        private const int StructureCrushExpiryTicks = 150;

        private struct FireSolutionCacheEntry
        {
            public int untilTick;
            public IntVec3 pawnPosition;
            public float range;
            public bool result;
        }

        private static readonly Dictionary<int, FireSolutionCacheEntry> FireSolutionCache = new Dictionary<int, FireSolutionCacheEntry>();

        public static bool IsReactorSaintPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.TryGetComp<CompABY_ReactorSaintShooter>() != null)
            {
                return true;
            }

            return string.Equals(pawn.def?.defName, ReactorSaintDefName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(pawn.kindDef?.defName, ReactorSaintDefName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanOperate(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && pawn.Map != null
                && !pawn.Dead
                && !pawn.Downed
                && pawn.Faction != null
                && Faction.OfPlayer != null
                && ABY_FactionHostilityUtility.SafeHostileToPlayer(pawn);
        }

        public static bool HasValidFireSolution(Pawn pawn, float maxRange)
        {
            if (!CanOperate(pawn))
            {
                return false;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float range = Mathf.Max(1f, maxRange);
            int key = pawn.thingIDNumber;
            FireSolutionCacheEntry cached;
            if (FireSolutionCache.TryGetValue(key, out cached)
                && ticksGame < cached.untilTick
                && cached.pawnPosition == pawn.Position
                && Mathf.Abs(cached.range - range) <= 0.1f)
            {
                return cached.result;
            }

            bool result = HasValidFireSolutionUncached(pawn, range);
            FireSolutionCache[key] = new FireSolutionCacheEntry
            {
                untilTick = ticksGame + 15,
                pawnPosition = pawn.Position,
                range = range,
                result = result
            };
            return result;
        }

        private static bool HasValidFireSolutionUncached(Pawn pawn, float range)
        {
            Pawn pawnTarget = AbyssalThreatPawnUtility.FindBestTarget(
                pawn,
                0f,
                range,
                true,
                true,
                false,
                5.5f,
                1.1f);
            if (pawnTarget != null)
            {
                return true;
            }

            List<Building> buildings = pawn.Map.listerBuildings?.allBuildingsColonist;
            if (buildings == null)
            {
                return false;
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                Building building = buildings[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, building))
                {
                    continue;
                }

                IntVec3 targetCell = building.Position;
                if (!targetCell.IsValid || pawn.Position.DistanceTo(targetCell) > range)
                {
                    continue;
                }

                if (GenSight.LineOfSight(pawn.Position, targetCell, pawn.Map))
                {
                    return true;
                }
            }

            return false;
        }

        public static void StabilizeAIGotoNearestHostileResult(Pawn pawn, ref Job job)
        {
            if (!IsReactorSaintPawn(pawn) || !CanOperate(pawn))
            {
                return;
            }

            CompProperties_ABY_ReactorSaintShooter props = ResolveProps(pawn);
            float range = props != null ? props.range : 34.9f;
            bool hasFireSolution = HasValidFireSolution(pawn, range);

            if (hasFireSolution)
            {
                if (job == null || job.def == JobDefOf.Goto)
                {
                    job = MakeShortCombatWaitJob();
                }

                return;
            }

            if (TryMakeTacticalJob(pawn, props, true, out Job tacticalJob))
            {
                job = tacticalJob;
                return;
            }

            if (job != null && job.def == JobDefOf.Wait_Combat)
            {
                job = null;
            }
        }

        public static bool TryRunTacticalWatchdog(
            Pawn pawn,
            CompProperties_ABY_ReactorSaintShooter props,
            int lastPositionChangeTick,
            int lastSuccessfulShotTick,
            ref int nextEmergencyRepositionTick)
        {
            if (!IsReactorSaintPawn(pawn) || !CanOperate(pawn) || pawn.jobs == null)
            {
                return false;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float range = props != null ? props.range : 34.9f;
            bool hasFireSolution = HasValidFireSolution(pawn, range);
            Job curJob = pawn.CurJob;

            if (hasFireSolution)
            {
                if (curJob != null && curJob.def == JobDefOf.Goto && pawn.pather != null && !pawn.pather.Moving)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                    return true;
                }

                return false;
            }

            int stalledTicks = lastPositionChangeTick < 0 ? 0 : ticksGame - lastPositionChangeTick;
            int silentTicks = lastSuccessfulShotTick < 0 ? 999999 : ticksGame - lastSuccessfulShotTick;
            bool suspiciousWait = curJob != null && curJob.def == JobDefOf.Wait_Combat;
            bool suspiciousGoto = curJob != null && curJob.def == JobDefOf.Goto && stalledTicks >= 150;
            bool hardStuck = stalledTicks >= 210 && silentTicks >= 180;

            if (!suspiciousWait && !suspiciousGoto && !hardStuck)
            {
                return false;
            }

            bool allowEmergencyReposition = hardStuck && ticksGame >= nextEmergencyRepositionTick;
            if (TryMakeTacticalJob(pawn, props, allowEmergencyReposition, out Job tacticalJob))
            {
                if (allowEmergencyReposition)
                {
                    nextEmergencyRepositionTick = ticksGame + 300;
                }

                pawn.jobs.StartJob(tacticalJob, JobCondition.InterruptForced, null, false, true);
                return true;
            }

            if (allowEmergencyReposition && TryEmergencyPhaseReposition(pawn, props))
            {
                nextEmergencyRepositionTick = ticksGame + 420;
                return true;
            }

            return false;
        }

        public static bool TryMakeTacticalJob(Pawn pawn, CompProperties_ABY_ReactorSaintShooter props, bool force, out Job job)
        {
            job = null;
            if (!CanOperate(pawn))
            {
                return false;
            }

            float range = props != null ? props.range : 34.9f;
            float preferredMinRange = props != null ? props.preferredMinRange : 10.5f;

            Pawn adjacentThreat = FindNearestHostilePawn(pawn, 2.15f, requireLineOfSight: false);
            if (adjacentThreat != null)
            {
                job = JobMaker.MakeJob(JobDefOf.AttackMelee, adjacentThreat);
                job.expiryInterval = 60;
                job.checkOverrideOnExpire = true;
                job.collideWithPawns = true;
                return true;
            }

            Thing anchor = FindBestCombatAnchor(pawn, range, force);
            if (anchor == null)
            {
                return false;
            }

            IntVec3 anchorCell = anchor.PositionHeld;
            if (!anchorCell.IsValid || !anchorCell.InBounds(pawn.Map))
            {
                return false;
            }

            bool currentLos = GenSight.LineOfSight(pawn.Position, anchorCell, pawn.Map);
            float currentDistance = pawn.Position.DistanceTo(anchorCell);
            if (currentLos && currentDistance <= range)
            {
                job = MakeShortCombatWaitJob();
                return true;
            }

            if (TryFindFiringCell(pawn, anchorCell, range, preferredMinRange, out IntVec3 firingCell))
            {
                if (firingCell == pawn.Position)
                {
                    job = MakeShortCombatWaitJob();
                    return true;
                }

                job = JobMaker.MakeJob(JobDefOf.Goto, firingCell);
                job.expiryInterval = TacticalGotoExpiryTicks;
                job.checkOverrideOnExpire = true;
                job.collideWithPawns = true;
                return true;
            }

            Building blockingBuilding = FindBestBlockingBuilding(pawn, anchorCell, force ? 8.5f : 5.5f);
            if (blockingBuilding != null)
            {
                job = JobMaker.MakeJob(JobDefOf.AttackMelee, blockingBuilding);
                job.expiryInterval = StructureCrushExpiryTicks;
                job.checkOverrideOnExpire = true;
                job.collideWithPawns = true;
                return true;
            }

            if (anchor is Building anchorBuilding && AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, anchorBuilding))
            {
                job = JobMaker.MakeJob(JobDefOf.AttackMelee, anchorBuilding);
                job.expiryInterval = StructureCrushExpiryTicks;
                job.checkOverrideOnExpire = true;
                job.collideWithPawns = true;
                return true;
            }

            if (TryFindApproachCell(pawn, anchorCell, out IntVec3 approachCell))
            {
                job = JobMaker.MakeJob(JobDefOf.Goto, approachCell);
                job.expiryInterval = TacticalGotoExpiryTicks;
                job.checkOverrideOnExpire = true;
                job.collideWithPawns = true;
                return true;
            }

            return false;
        }

        private static Job MakeShortCombatWaitJob()
        {
            Job wait = JobMaker.MakeJob(JobDefOf.Wait_Combat);
            wait.expiryInterval = ShortWaitTicks;
            wait.checkOverrideOnExpire = true;
            return wait;
        }

        private static CompProperties_ABY_ReactorSaintShooter ResolveProps(Pawn pawn)
        {
            return pawn?.TryGetComp<CompABY_ReactorSaintShooter>()?.PropsForAI;
        }

        private static Pawn FindNearestHostilePawn(Pawn pawn, float maxDistance, bool requireLineOfSight)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Pawn best = null;
            float bestDistanceSq = maxDistance * maxDistance;
            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float distanceSq = (candidate.Position - pawn.Position).LengthHorizontalSquared;
                if (distanceSq > bestDistanceSq)
                {
                    continue;
                }

                if (requireLineOfSight && !GenSight.LineOfSight(pawn.Position, candidate.Position, pawn.Map))
                {
                    continue;
                }

                bestDistanceSq = distanceSq;
                best = candidate;
            }

            return best;
        }

        private static Thing FindBestCombatAnchor(Pawn pawn, float range, bool includeDistant)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Thing best = null;
            float bestScore = float.MinValue;
            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn candidate = pawns[i];
                    if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                    {
                        continue;
                    }

                    float distance = pawn.Position.DistanceTo(candidate.Position);
                    if (!includeDistant && distance > range + 8f)
                    {
                        continue;
                    }

                    bool hasLos = GenSight.LineOfSight(pawn.Position, candidate.Position, pawn.Map);
                    float score = 120f - distance;
                    if (hasLos)
                    {
                        score += 35f;
                    }

                    if (AbyssalThreatPawnUtility.HasRangedWeapon(candidate))
                    {
                        score += 8f;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
            }

            List<Building> buildings = pawn.Map.listerBuildings?.allBuildingsColonist;
            if (buildings != null)
            {
                for (int i = 0; i < buildings.Count; i++)
                {
                    Building building = buildings[i];
                    if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, building))
                    {
                        continue;
                    }

                    float distance = pawn.Position.DistanceTo(building.Position);
                    if (!includeDistant && distance > range + 8f)
                    {
                        continue;
                    }

                    bool hasLos = GenSight.LineOfSight(pawn.Position, building.Position, pawn.Map);
                    float score = 50f - distance;
                    if (hasLos)
                    {
                        score += 18f;
                    }

                    if (AbyssalThreatPawnUtility.IsCombatTurretLikeBuilding(building))
                    {
                        score += 12f;
                    }

                    if (building.def?.Fillage == FillCategory.Full)
                    {
                        score += 4f;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = building;
                    }
                }
            }

            return best;
        }

        private static bool TryFindFiringCell(Pawn pawn, IntVec3 targetCell, float maxRange, float preferredMinRange, out IntVec3 firingCell)
        {
            firingCell = IntVec3.Invalid;
            Map map = pawn?.Map;
            if (map == null || !targetCell.IsValid || !targetCell.InBounds(map))
            {
                return false;
            }

            float minRange = Mathf.Max(4f, preferredMinRange * 0.65f);
            float idealRange = Mathf.Clamp(maxRange * 0.72f, minRange + 1.5f, maxRange - 1.25f);
            float searchRadius = Mathf.Min(maxRange - 0.5f, Mathf.Max(minRange + 2f, idealRange + 4f));
            int maxCells = Math.Min(GenRadial.NumCellsInRadius(searchRadius), GenRadial.RadialPattern.Length);

            const int CandidateLimit = 28;
            IntVec3[] candidateCells = new IntVec3[CandidateLimit];
            float[] candidateScores = new float[CandidateLimit];
            int candidateCount = 0;

            for (int i = 0; i < maxCells; i++)
            {
                IntVec3 cell = targetCell + GenRadial.RadialPattern[i];
                if (!IsUsableStandCell(cell, map, pawn))
                {
                    continue;
                }

                float targetDistance = cell.DistanceTo(targetCell);
                if (targetDistance < minRange || targetDistance > maxRange)
                {
                    continue;
                }

                float moveDistance = pawn.Position.DistanceTo(cell);
                float rangeError = Mathf.Abs(targetDistance - idealRange);
                float score = 250f - (moveDistance * 2.0f) - (rangeError * 4.0f);
                if (cell == pawn.Position)
                {
                    score += 30f;
                }

                if (targetDistance >= preferredMinRange)
                {
                    score += 10f;
                }

                InsertCellCandidate(candidateCells, candidateScores, ref candidateCount, cell, score);
            }

            float bestScore = float.MinValue;
            for (int i = 0; i < candidateCount; i++)
            {
                IntVec3 cell = candidateCells[i];
                if (!GenSight.LineOfSight(cell, targetCell, map))
                {
                    continue;
                }

                if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                float score = candidateScores[i];
                if (score > bestScore)
                {
                    bestScore = score;
                    firingCell = cell;
                }
            }

            return firingCell.IsValid;
        }

        private static void InsertCellCandidate(IntVec3[] cells, float[] scores, ref int count, IntVec3 cell, float score)
        {
            if (cells == null || scores == null || cells.Length == 0 || scores.Length != cells.Length)
            {
                return;
            }

            if (count < cells.Length)
            {
                cells[count] = cell;
                scores[count] = score;
                count++;
                return;
            }

            int worstIndex = 0;
            float worstScore = scores[0];
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] < worstScore)
                {
                    worstScore = scores[i];
                    worstIndex = i;
                }
            }

            if (score > worstScore)
            {
                cells[worstIndex] = cell;
                scores[worstIndex] = score;
            }
        }

        private static bool TryFindApproachCell(Pawn pawn, IntVec3 targetCell, out IntVec3 approachCell)
        {
            approachCell = IntVec3.Invalid;
            Map map = pawn?.Map;
            if (map == null || !targetCell.IsValid || !targetCell.InBounds(map))
            {
                return false;
            }

            const int CandidateLimit = 18;
            IntVec3[] candidateCells = new IntVec3[CandidateLimit];
            float[] candidateScores = new float[CandidateLimit];
            int candidateCount = 0;
            int maxCells = Math.Min(GenRadial.NumCellsInRadius(11f), GenRadial.RadialPattern.Length);
            for (int i = 0; i < maxCells; i++)
            {
                IntVec3 cell = targetCell + GenRadial.RadialPattern[i];
                if (!IsUsableStandCell(cell, map, pawn))
                {
                    continue;
                }

                float distanceFromPawn = pawn.Position.DistanceTo(cell);
                float distanceToTarget = cell.DistanceTo(targetCell);
                float score = 100f - distanceFromPawn - (distanceToTarget * 0.35f);
                InsertCellCandidate(candidateCells, candidateScores, ref candidateCount, cell, score);
            }

            float bestScore = float.MinValue;
            for (int i = 0; i < candidateCount; i++)
            {
                IntVec3 cell = candidateCells[i];
                if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                float score = candidateScores[i];
                if (GenSight.LineOfSight(cell, targetCell, map))
                {
                    score += 20f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    approachCell = cell;
                }
            }

            return approachCell.IsValid;
        }

        private static bool IsUsableStandCell(IntVec3 cell, Map map, Pawn pawn)
        {
            return cell.IsValid
                && cell.InBounds(map)
                && cell.Walkable(map)
                && cell.Standable(map)
                && !AbyssalThreatPawnUtility.CellHasOtherPawn(cell, map, pawn);
        }

        private static Building FindBestBlockingBuilding(Pawn pawn, IntVec3 anchorCell, float radius)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Building best = null;
            float bestScore = float.MinValue;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, radius, true))
            {
                if (!cell.InBounds(pawn.Map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(pawn.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Building building = things[i] as Building;
                    if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, building))
                    {
                        continue;
                    }

                    float distance = pawn.Position.DistanceTo(building.Position);
                    float anchorDistance = anchorCell.IsValid ? building.Position.DistanceTo(anchorCell) : 0f;
                    float score = 80f - distance - (anchorDistance * 0.15f);
                    if (building.def?.Fillage == FillCategory.Full)
                    {
                        score += 10f;
                    }

                    if (AbyssalThreatPawnUtility.IsCombatTurretLikeBuilding(building))
                    {
                        score += 8f;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = building;
                    }
                }
            }

            return best;
        }

        private static bool TryEmergencyPhaseReposition(Pawn pawn, CompProperties_ABY_ReactorSaintShooter props)
        {
            if (!CanOperate(pawn))
            {
                return false;
            }

            Thing anchor = FindBestCombatAnchor(pawn, props != null ? props.range : 34.9f, true);
            if (anchor == null)
            {
                return false;
            }

            float range = props != null ? props.range : 34.9f;
            float minRange = props != null ? props.preferredMinRange : 10.5f;
            if (!TryFindFiringCell(pawn, anchor.PositionHeld, range, minRange, out IntVec3 destination))
            {
                return false;
            }

            if (destination == pawn.Position)
            {
                return false;
            }

            FleckMaker.ThrowDustPuff(pawn.DrawPos, pawn.Map, 2.2f);
            pawn.pather?.StopDead();
            pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, true, true);
            pawn.Position = destination;
            pawn.rotationTracker?.FaceTarget(anchor.PositionHeld);
            TryNotifyTeleported(pawn);
            FleckMaker.ThrowMicroSparks(pawn.DrawPos, pawn.Map);
            FleckMaker.ThrowDustPuff(pawn.DrawPos, pawn.Map, 2.2f);
            return true;
        }

        private static void TryNotifyTeleported(Pawn pawn)
        {
            try
            {
                System.Reflection.MethodInfo[] methods = typeof(Pawn).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    System.Reflection.MethodInfo method = methods[i];
                    if (method.Name != "Notify_Teleported")
                    {
                        continue;
                    }

                    System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                    object[] args = new object[parameters.Length];
                    for (int p = 0; p < parameters.Length; p++)
                    {
                        if (parameters[p].ParameterType == typeof(bool))
                        {
                            args[p] = false;
                        }
                        else
                        {
                            args[p] = null;
                        }
                    }

                    method.Invoke(pawn, args);
                    return;
                }
            }
            catch
            {
                // Teleport notification signatures differ between RimWorld builds; the position change itself is the fallback.
            }
        }
    }
}
