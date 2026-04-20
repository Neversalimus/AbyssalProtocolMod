using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompABY_GateWardenShieldBash : ThingComp
    {
        private int nextBashTick = -1;

        public CompProperties_ABY_GateWardenShieldBash Props => (CompProperties_ABY_GateWardenShieldBash)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextBashTick, "nextBashTick", -1);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || !parent.IsHashIntervalTick(Mathf.Max(10, Props.scanIntervalTicks)))
            {
                return;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (currentTick < nextBashTick)
            {
                return;
            }

            Pawn target = ResolveBashTarget(pawn);
            if (target == null)
            {
                return;
            }

            DoBash(pawn, target);
            nextBashTick = currentTick
                + Mathf.Max(60, Props.cooldownTicks)
                + Rand.RangeInclusive(-Mathf.Max(0, Props.cooldownJitterTicks), Mathf.Max(0, Props.cooldownJitterTicks));
        }

        private Pawn ResolveBashTarget(Pawn pawn)
        {
            Thing currentJobTarget = pawn.jobs?.curJob?.targetA.Thing;
            if (currentJobTarget is Pawn hostilePawn
                && AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, hostilePawn)
                && pawn.PositionHeld.DistanceTo(hostilePawn.PositionHeld) <= Props.bashRange)
            {
                return hostilePawn;
            }

            return AbyssalThreatPawnUtility.FindClosestThreatWithin(pawn, Props.bashRange);
        }

        private void DoBash(Pawn pawn, Pawn target)
        {
            pawn.rotationTracker?.FaceTarget(target.PositionHeld);

            DamageInfo dinfo = new DamageInfo(
                DamageDefOf.Blunt,
                Mathf.Max(1f, Props.bashDamage),
                Mathf.Max(0f, Props.bashArmorPenetration),
                -1f,
                pawn,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown,
                target);
            target.TakeDamage(dinfo);

            AbyssalThreatPawnUtility.ApplyOrRefreshHediff(target, Props.bashHediffDefName, Props.bashSeverity);
            target.pather?.StopDead();

            if (target.jobs != null)
            {
                Job staggerJob = JobMaker.MakeJob(JobDefOf.Wait_Combat);
                staggerJob.expiryInterval = Mathf.Max(15, Props.staggerTicks);
                staggerJob.checkOverrideOnExpire = true;
                target.jobs.TryTakeOrderedJob(staggerJob, JobTag.Misc);
            }

            if (!Props.bashSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.bashSoundDefName, target.PositionHeld, target.MapHeld);
            }

            if (target.MapHeld != null)
            {
                FleckMaker.Static(target.PositionHeld, target.MapHeld, FleckDefOf.ExplosionFlash, Props.bashFlashScale);
            }
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed;
        }
    }
}
