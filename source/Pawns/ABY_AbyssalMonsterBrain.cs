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

        public static bool TryStabilizeAIGotoNearestHostileResult(Pawn pawn, ref Job job)
        {
            if (!ABY_AbyssalMonsterRoleUtility.ShouldUseMonsterBrain(pawn) || job == null)
            {
                return false;
            }

            if (job.def != JobDefOf.Goto && job.def != JobDefOf.Wait_Combat)
            {
                return false;
            }

            Pawn target = ResolveJobTargetPawn(pawn, job) ?? FindClosestHostilePawn(pawn, 45f, false);
            if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, target))
            {
                return false;
            }

            if (!TryCreateTacticalJob(pawn, target, job.def, out Job tacticalJob))
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

            if (currentJob.def != JobDefOf.Wait_Combat && currentJob.def != JobDefOf.Goto)
            {
                return false;
            }

            ABY_AbyssalMonsterCombatProfile profile = ABY_AbyssalMonsterRoleUtility.ResolveProfile(pawn);
            if (!profile.HasRangedStance)
            {
                return false;
            }

            Pawn target = ResolveJobTargetPawn(pawn, currentJob) ?? FindClosestHostilePawn(pawn, Math.Max(profile.MaxRange, 35f), false);
            if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, target))
            {
                return false;
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
            tacticalJob = null;
            if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, target))
            {
                return false;
            }

            ABY_AbyssalMonsterCombatProfile profile = ABY_AbyssalMonsterRoleUtility.ResolveProfile(pawn);
            if (!profile.IsValid)
            {
                return false;
            }

            float distance = pawn.Position.DistanceTo(target.Position);
            if (distance <= Math.Max(1.95f, profile.PanicMeleeRange) && ShouldMeleeNow(pawn, target, profile, distance))
            {
                tacticalJob = MakeMeleeJob(target);
                return true;
            }

            if (!profile.HasRangedStance)
            {
                if (distance <= 1.95f)
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

                if (distance <= Math.Max(1.95f, profile.PanicMeleeRange))
                {
                    tacticalJob = MakeMeleeJob(target);
                    return true;
                }
            }

            if (HasFireSolution(pawn, target, profile))
            {
                pawn.rotationTracker?.FaceTarget(target.Position);
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

        private static bool TryCreateRepositionJob(Pawn pawn, Pawn target, ABY_AbyssalMonsterCombatProfile profile, out Job job)
        {
            job = null;
            if (!TryFindFiringCell(pawn, target, profile, out IntVec3 firingCell))
            {
                return false;
            }

            job = MakeGotoJob(firingCell, RepositionJobExpiry, LocomotionUrgency.Jog);
            return true;
        }

        private static bool TryFindFiringCell(Pawn pawn, Pawn target, ABY_AbyssalMonsterCombatProfile profile, out IntVec3 firingCell)
        {
            firingCell = IntVec3.Invalid;
            Map map = pawn?.Map;
            if (map == null || target == null)
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

                float targetDistance = cell.DistanceTo(target.Position);
                if (targetDistance < Math.Max(0f, profile.MinRange) || targetDistance > Math.Max(2f, profile.MaxRange))
                {
                    continue;
                }

                if (!GenSight.LineOfSight(cell, target.Position, map))
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

        private static bool HasFireSolution(Pawn pawn, Pawn target, ABY_AbyssalMonsterCombatProfile profile)
        {
            if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, target))
            {
                return false;
            }

            float distance = pawn.Position.DistanceTo(target.Position);
            if (distance < Math.Max(0f, profile.MinRange) || distance > Math.Max(2f, profile.MaxRange))
            {
                return false;
            }

            return GenSight.LineOfSight(pawn.Position, target.Position, pawn.Map);
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

        private static Pawn ResolveJobTargetPawn(Pawn pawn, Job job)
        {
            if (job == null)
            {
                return null;
            }

            Pawn target = job.targetA.Thing as Pawn;
            if (AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, target))
            {
                return target;
            }

            target = job.targetB.Thing as Pawn;
            if (AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, target))
            {
                return target;
            }

            return null;
        }

        private static Job MakeMeleeJob(Pawn target)
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
