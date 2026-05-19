using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class ABY_AbyssalMonsterBrain
    {
        private const int ShortCombatJobExpiry = 32;
        private const int RepositionJobExpiry = 55;
        private const int MeleeJobExpiry = 70;
        private const float CombatBuildingSearchRange = 45f;
        private const float CombatBuildingThreatBias = 18f;

        public static bool TryStabilizeAIGotoNearestHostileResult(Pawn pawn, ref Job job)
        {
            if (!ABY_AbyssalMonsterRoleUtility.ShouldUseMonsterBrain(pawn))
            {
                return false;
            }

            if (job != null && job.def != JobDefOf.Goto && job.def != JobDefOf.Wait_Combat && job.def != JobDefOf.AttackMelee)
            {
                return false;
            }

            Thing target = ResolveJobTargetThing(pawn, job) ?? FindClosestPriorityHostileThing(pawn, CombatBuildingSearchRange, false);
            if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, target))
            {
                return false;
            }

            if (!TryCreateTacticalJob(pawn, target, job?.def, out Job tacticalJob))
            {
                return false;
            }

            if (tacticalJob == null)
            {
                return false;
            }

            job = tacticalJob;
            return true;
        }

        public static bool TryRecoverStaleCombatJob(Pawn pawn)
        {
            if (!ABY_AbyssalMonsterRoleUtility.ShouldUseMonsterBrain(pawn) || pawn.jobs == null || pawn.Downed)
            {
                return false;
            }

            Job currentJob = pawn.CurJob;
            if (currentJob == null)
            {
                return false;
            }

            if (currentJob.def != JobDefOf.Wait_Combat && currentJob.def != JobDefOf.Goto && currentJob.def != JobDefOf.AttackMelee)
            {
                return false;
            }

            ABY_AbyssalMonsterCombatProfile profile = ABY_AbyssalMonsterRoleUtility.ResolveProfile(pawn);
            if (!profile.HasRangedStance && currentJob.def != JobDefOf.AttackMelee)
            {
                return false;
            }

            Thing target = ResolveJobTargetThing(pawn, currentJob) ?? FindClosestPriorityHostileThing(pawn, Math.Max(profile.MaxRange, 35f), false);
            if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, target))
            {
                return false;
            }

            if (target is Building && currentJob.def != JobDefOf.AttackMelee && !profile.HasRangedStance)
            {
                pawn.jobs.TryTakeOrderedJob(MakeMeleeJob(target), JobTag.Misc);
                return true;
            }

            bool hasFireSolution = HasFireSolution(pawn, target, profile);
            if (currentJob.def == JobDefOf.Wait_Combat && !hasFireSolution)
            {
                if (TryCreateRepositionJob(pawn, target, profile, out Job repositionJob))
                {
                    pawn.jobs.TryTakeOrderedJob(repositionJob, JobTag.Misc);
                    ABY_StabilityDiagnosticsUtility.Verbose("monster-ai-reposition", "Monster AI recovered stale Wait_Combat by repositioning " + ABY_StabilityDiagnosticsUtility.FormatPawnLabel(pawn), 1800);
                    return true;
                }

                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                ABY_StabilityDiagnosticsUtility.Verbose("monster-ai-interrupt", "Monster AI interrupted stale combat job for " + ABY_StabilityDiagnosticsUtility.FormatPawnLabel(pawn), 1800);
                return true;
            }

            if (currentJob.def == JobDefOf.Goto && hasFireSolution)
            {
                Job wait = MakeWaitJob(ShortCombatJobExpiry);
                pawn.jobs.TryTakeOrderedJob(wait, JobTag.Misc);
                ABY_StabilityDiagnosticsUtility.Verbose("monster-ai-hold", "Monster AI converted Goto to hold-fire for " + ABY_StabilityDiagnosticsUtility.FormatPawnLabel(pawn), 1800);
                return true;
            }

            return false;
        }

        public static bool TryCreateTacticalJob(Pawn pawn, Pawn target, JobDef sourceJobDef, out Job tacticalJob)
        {
            return TryCreateTacticalJob(pawn, (Thing)target, sourceJobDef, out tacticalJob);
        }

        public static bool TryCreateTacticalJob(Pawn pawn, Thing target, JobDef sourceJobDef, out Job tacticalJob)
        {
            tacticalJob = null;
            if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, target))
            {
                return false;
            }

            ABY_AbyssalMonsterCombatProfile profile = ABY_AbyssalMonsterRoleUtility.ResolveProfile(pawn);
            if (!profile.IsValid)
            {
                return false;
            }

            float distance = pawn.Position.DistanceTo(target.PositionHeld);
            Pawn targetPawn = target as Pawn;
            bool targetIsCombatBuilding = target is Building building && AbyssalThreatPawnUtility.IsCombatTurretLikeBuilding(building);

            if (targetPawn != null && distance <= Math.Max(1.95f, profile.PanicMeleeRange) && ShouldMeleeNow(pawn, targetPawn, profile, distance))
            {
                tacticalJob = MakeMeleeJob(targetPawn);
                return true;
            }

            if (!profile.HasRangedStance)
            {
                if (targetIsCombatBuilding || distance <= 1.95f)
                {
                    tacticalJob = MakeMeleeJob(target);
                    return true;
                }

                return false;
            }

            if (profile.PreferredMinRange > 0f && distance < profile.PreferredMinRange)
            {
                if (AbyssalThreatPawnUtility.TryFindRetreatCell(pawn, target, profile.PreferredMinRange, profile.RepositionSearchRadius, out IntVec3 retreatCell))
                {
                    tacticalJob = MakeGotoJob(retreatCell, RepositionJobExpiry, LocomotionUrgency.Jog);
                    return true;
                }

                if (targetPawn != null && distance <= Math.Max(1.95f, profile.PanicMeleeRange))
                {
                    tacticalJob = MakeMeleeJob(targetPawn);
                    return true;
                }
            }

            if (HasFireSolution(pawn, target, profile))
            {
                pawn.rotationTracker?.FaceTarget(target.PositionHeld);
                if (profile.HoldPositionWhenReady)
                {
                    pawn.pather?.StopDead();
                }

                tacticalJob = MakeWaitJob(ShortCombatJobExpiry);
                return true;
            }

            if (distance <= profile.MaxRange + 2.5f && TryCreateRepositionJob(pawn, target, profile, out Job repositionJob))
            {
                tacticalJob = repositionJob;
                return true;
            }

            if (targetIsCombatBuilding && distance <= Math.Max(6f, profile.MaxRange + 8f))
            {
                tacticalJob = MakeMeleeJob(target);
                return true;
            }

            return false;
        }

        public static Pawn FindClosestHostilePawn(Pawn pawn, float maxDistance, bool requireLineOfSight)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.CombatTargetPawnsFor(pawn.Map);
            if (pawns == null || pawns.Count == 0)
            {
                return null;
            }

            Pawn best = null;
            float resolvedMax = Math.Max(0.1f, maxDistance);
            float bestDistanceSquared = resolvedMax * resolvedMax;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float distanceSquared = pawn.Position.DistanceToSquared(candidate.Position);
                if (distanceSquared > bestDistanceSquared)
                {
                    continue;
                }

                if (requireLineOfSight && !GenSight.LineOfSight(pawn.Position, candidate.Position, pawn.Map))
                {
                    continue;
                }

                bestDistanceSquared = distanceSquared;
                best = candidate;
            }

            return best;
        }

        private static Thing FindClosestPriorityHostileThing(Pawn pawn, float maxDistance, bool requireLineOfSight)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Thing best = null;
            float resolvedMax = Math.Max(0.1f, maxDistance);
            float bestScore = float.MaxValue;
            float maxDistanceSquared = resolvedMax * resolvedMax;

            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.CombatTargetPawnsFor(pawn.Map);
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn candidate = pawns[i];
                    if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                    {
                        continue;
                    }

                    float distanceSquared = pawn.Position.DistanceToSquared(candidate.PositionHeld);
                    if (distanceSquared > maxDistanceSquared)
                    {
                        continue;
                    }

                    if (requireLineOfSight && !GenSight.LineOfSight(pawn.Position, candidate.PositionHeld, pawn.Map))
                    {
                        continue;
                    }

                    float score = distanceSquared;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
            }

            IReadOnlyList<Building> buildings = ABY_RuntimeTargetCache.CombatTargetBuildingsFor(pawn.Map);
            if (buildings != null)
            {
                for (int i = 0; i < buildings.Count; i++)
                {
                    Building building = buildings[i];
                    if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, building))
                    {
                        continue;
                    }

                    float distanceSquared = pawn.Position.DistanceToSquared(building.PositionHeld);
                    if (distanceSquared > maxDistanceSquared)
                    {
                        continue;
                    }

                    if (requireLineOfSight && !GenSight.LineOfSight(pawn.Position, building.PositionHeld, pawn.Map))
                    {
                        continue;
                    }

                    float score = Mathf.Max(0f, distanceSquared - CombatBuildingThreatBias * CombatBuildingThreatBias);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = building;
                    }
                }
            }

            return best;
        }

        private static bool TryCreateRepositionJob(Pawn pawn, Thing target, ABY_AbyssalMonsterCombatProfile profile, out Job job)
        {
            job = null;
            if (!TryFindFiringCell(pawn, target, profile, out IntVec3 firingCell))
            {
                return false;
            }

            job = MakeGotoJob(firingCell, RepositionJobExpiry, LocomotionUrgency.Jog);
            return true;
        }

        private static bool TryFindFiringCell(Pawn pawn, Thing target, ABY_AbyssalMonsterCombatProfile profile, out IntVec3 firingCell)
        {
            firingCell = IntVec3.Invalid;
            Map map = pawn?.Map;
            if (map == null || target == null || target.Destroyed)
            {
                return false;
            }

            int radius = Mathf.Clamp(profile.RepositionSearchRadius, 5, 18);
            float bestScore = float.MinValue;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, radius, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map) || AbyssalThreatPawnUtility.CellHasOtherPawn(cell, map, pawn))
                {
                    continue;
                }

                if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                float targetDistance = cell.DistanceTo(target.PositionHeld);
                if (targetDistance < Math.Max(0f, profile.MinRange) || targetDistance > Math.Max(2f, profile.MaxRange))
                {
                    continue;
                }

                if (!GenSight.LineOfSight(cell, target.PositionHeld, map))
                {
                    continue;
                }

                float moveDistance = pawn.Position.DistanceTo(cell);
                float preferred = profile.PreferredMinRange > 0f ? profile.PreferredMinRange : Mathf.Min(profile.MaxRange * 0.55f, 9f);
                float preferredPenalty = Math.Abs(targetDistance - preferred) * 0.35f;
                float score = 50f - moveDistance - preferredPenalty;
                if (targetDistance >= profile.MinRange + 1.5f)
                {
                    score += 3f;
                }

                if (cell.GetCover(map) != null)
                {
                    score += 1.25f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    firingCell = cell;
                }
            }

            return firingCell.IsValid;
        }

        private static bool HasFireSolution(Pawn pawn, Thing target, ABY_AbyssalMonsterCombatProfile profile)
        {
            if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, target))
            {
                return false;
            }

            float distance = pawn.Position.DistanceTo(target.PositionHeld);
            if (distance < Math.Max(0f, profile.MinRange) || distance > Math.Max(2f, profile.MaxRange))
            {
                return false;
            }

            return GenSight.LineOfSight(pawn.Position, target.PositionHeld, pawn.Map);
        }

        private static bool ShouldMeleeNow(Pawn pawn, Pawn target, ABY_AbyssalMonsterCombatProfile profile, float distance)
        {
            if (!profile.HasRangedStance)
            {
                return distance <= 1.95f;
            }

            if (distance <= 1.65f)
            {
                return true;
            }

            if (profile.PreferredMinRange > 0f && AbyssalThreatPawnUtility.TryFindRetreatCell(pawn, target, profile.PreferredMinRange, profile.RepositionSearchRadius, out IntVec3 retreatCell))
            {
                return false;
            }

            return distance <= Math.Max(1.95f, profile.PanicMeleeRange);
        }

        private static Thing ResolveJobTargetThing(Pawn pawn, Job job)
        {
            if (job == null)
            {
                return null;
            }

            Thing target = job.targetA.Thing;
            if (AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, target))
            {
                return target;
            }

            target = job.targetB.Thing;
            if (AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, target))
            {
                return target;
            }

            return null;
        }

        private static Job MakeMeleeJob(Thing target)
        {
            Job melee = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            melee.expiryInterval = MeleeJobExpiry;
            melee.checkOverrideOnExpire = true;
            melee.collideWithPawns = true;
            return melee;
        }

        private static Job MakeWaitJob(int expiryTicks)
        {
            Job wait = JobMaker.MakeJob(JobDefOf.Wait_Combat);
            wait.expiryInterval = Math.Max(12, expiryTicks);
            wait.checkOverrideOnExpire = true;
            return wait;
        }

        private static Job MakeGotoJob(IntVec3 cell, int expiryTicks, LocomotionUrgency urgency)
        {
            Job gotoJob = JobMaker.MakeJob(JobDefOf.Goto, cell);
            gotoJob.expiryInterval = Math.Max(18, expiryTicks);
            gotoJob.checkOverrideOnExpire = true;
            gotoJob.locomotionUrgency = urgency;
            gotoJob.collideWithPawns = true;
            return gotoJob;
        }
    }
}
