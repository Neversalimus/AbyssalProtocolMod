using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class ABY_AbyssalJobLoopGuardUtility
    {
        public static void StabilizeAIGotoNearestHostileResult(Pawn pawn, ref Job job)
        {
            if (pawn == null || job == null || job.def != JobDefOf.Goto)
            {
                return;
            }

            if (!ABY_AntiTameUtility.IsAbyssalPawn(pawn))
            {
                return;
            }

            Pawn targetPawn = job.targetA.Thing as Pawn;
            if (targetPawn == null || targetPawn.Dead || targetPawn.Map != pawn.Map)
            {
                return;
            }

            if (pawn.Faction == null || targetPawn.Faction == null || !pawn.Faction.HostileTo(targetPawn.Faction))
            {
                return;
            }

            bool hasCustomRangedController = pawn.TryGetComp<CompHexgunThrallShooter>() != null
                || pawn.TryGetComp<CompABY_RiftSapperShooter>() != null
                || pawn.TryGetComp<CompABY_SiegeIdolSiegeShooter>() != null
                || pawn.TryGetComp<CompABY_ReactorSaintShooter>() != null;

            float distance = pawn.Position.DistanceTo(targetPawn.Position);
            if (!hasCustomRangedController && distance > 1.9f)
            {
                return;
            }

            if (hasCustomRangedController && distance <= 32f && GenSight.LineOfSight(pawn.Position, targetPawn.Position, pawn.Map))
            {
                Job wait = JobMaker.MakeJob(JobDefOf.Wait_Combat);
                wait.expiryInterval = 30;
                wait.checkOverrideOnExpire = true;
                job = wait;
                return;
            }

            if (distance <= 1.9f)
            {
                Job melee = JobMaker.MakeJob(JobDefOf.AttackMelee, targetPawn);
                melee.expiryInterval = 60;
                melee.checkOverrideOnExpire = true;
                melee.collideWithPawns = true;
                job = melee;
            }
        }
    }
}
