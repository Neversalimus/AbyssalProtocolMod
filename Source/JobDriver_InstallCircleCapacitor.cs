using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class JobDriver_InstallCircleCapacitor : JobDriver
    {
        private const TargetIndex CapacitorInd = TargetIndex.A;
        private const TargetIndex CircleInd = TargetIndex.B;
        private const TargetIndex BayInd = TargetIndex.C;

        private Thing CapacitorThing => job.GetTarget(CapacitorInd).Thing;
        private Building_AbyssalSummoningCircle Circle => job.GetTarget(CircleInd).Thing as Building_AbyssalSummoningCircle;
        private AbyssalCircleCapacitorBay Bay => (AbyssalCircleCapacitorBay)job.GetTarget(BayInd).Cell.x;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (job.count <= 0)
            {
                job.count = 1;
            }

            if (CapacitorThing == null || Circle == null || Circle.MapHeld != pawn.MapHeld)
            {
                return false;
            }

            if (!Circle.CanInstallCapacitor(CapacitorThing.def, Bay, out string failReason))
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            if (!pawn.CanReserveAndReach(CapacitorThing, PathEndMode.Touch, Danger.Deadly))
            {
                return false;
            }

            if (!pawn.CanReserveAndReach(Circle, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return false;
            }

            if (!pawn.Reserve(CapacitorThing, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            if (!pawn.Reserve(Circle, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(CapacitorInd);
            this.FailOnDestroyedOrNull(CircleInd);
            this.FailOn(() => Circle == null || !Circle.CanInstallCapacitor(CapacitorThing?.def, Bay, out _));

            yield return Toils_Goto.GotoThing(CapacitorInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(CapacitorInd);
            yield return Toils_Goto.GotoThing(CircleInd, PathEndMode.InteractionCell);

            Toil warmup = Toils_General.Wait(105);
            warmup.WithProgressBarToilDelay(CircleInd);
            warmup.FailOn(() => Circle == null || Circle.Destroyed || Circle.RitualActive);
            yield return warmup;

            Toil install = new Toil();
            install.initAction = () =>
            {
                Pawn actor = install.actor;
                Thing carriedThing = actor?.carryTracker?.CarriedThing;
                if (actor == null || carriedThing == null || Circle == null)
                {
                    actor?.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                string installedLabel = carriedThing.LabelCap;
                if (!Circle.TryInstallCapacitorDirect(carriedThing.def, Bay, out string failReason))
                {
                    if (!failReason.NullOrEmpty())
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    }

                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                actor.carryTracker.innerContainer.Remove(carriedThing);
                carriedThing.Destroy(DestroyMode.Vanish);

                actor.rotationTracker?.FaceCell(Circle.Position);
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", Circle.RitualFocusCell, actor.MapHeld);
                Messages.Message("ABY_CapacitorInstallSuccess".Translate(installedLabel, AbyssalCircleCapacitorUtility.GetBayLabel(Bay)), Circle, MessageTypeDefOf.TaskCompletion, false);
            };
            install.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return install;
        }
    }
}
