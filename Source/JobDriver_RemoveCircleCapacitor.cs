using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class JobDriver_RemoveCircleCapacitor : JobDriver
    {
        private const TargetIndex CircleInd = TargetIndex.A;
        private const TargetIndex BayInd = TargetIndex.B;

        private Building_AbyssalSummoningCircle Circle => job.GetTarget(CircleInd).Thing as Building_AbyssalSummoningCircle;
        private AbyssalCircleCapacitorBay Bay => (AbyssalCircleCapacitorBay)job.GetTarget(BayInd).Cell.x;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Circle == null || Circle.MapHeld != pawn.MapHeld)
            {
                return false;
            }

            if (!Circle.CanRemoveInstalledCapacitor(Bay, out string failReason))
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            return pawn.Reserve(Circle, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(CircleInd);
            this.FailOn(() => Circle == null || !Circle.CanRemoveInstalledCapacitor(Bay, out _));

            yield return Toils_Goto.GotoThing(CircleInd, PathEndMode.InteractionCell);

            Toil warmup = Toils_General.Wait(90);
            warmup.WithProgressBarToilDelay(CircleInd);
            warmup.FailOn(() => Circle == null || Circle.Destroyed || Circle.RitualActive);
            yield return warmup;

            Toil remove = new Toil();
            remove.initAction = () =>
            {
                Pawn actor = remove.actor;
                if (actor == null || Circle == null || actor.MapHeld == null)
                {
                    actor?.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                ThingDef removedDef = Circle.RemoveInstalledCapacitor(Bay);
                if (removedDef == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                Thing capacitorThing = ThingMaker.MakeThing(removedDef);
                capacitorThing.stackCount = 1;
                IntVec3 placeCell = Circle.InteractionCell.IsValid ? Circle.InteractionCell : actor.PositionHeld;
                GenPlace.TryPlaceThing(capacitorThing, placeCell, actor.MapHeld, ThingPlaceMode.Near);

                actor.rotationTracker?.FaceCell(Circle.Position);
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", Circle.RitualFocusCell, actor.MapHeld);
                Messages.Message("ABY_CapacitorRemoveSuccess".Translate(removedDef.label.CapitalizeFirst(), AbyssalCircleCapacitorUtility.GetBayLabel(Bay)), Circle, MessageTypeDefOf.TaskCompletion, false);
            };
            remove.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return remove;
        }
    }
}
