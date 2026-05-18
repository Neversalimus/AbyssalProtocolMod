using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class JobDriver_RemoveCircleModule : JobDriver
    {
        private const TargetIndex CircleInd = TargetIndex.A;
        private const TargetIndex EdgeInd = TargetIndex.C;

        private Building_AbyssalSummoningCircle Circle => job.GetTarget(CircleInd).Thing as Building_AbyssalSummoningCircle;
        private AbyssalCircleModuleEdge Edge => (AbyssalCircleModuleEdge)job.GetTarget(EdgeInd).Cell.x;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Circle == null || Circle.MapHeld != pawn.MapHeld)
            {
                return false;
            }

            if (!Circle.CanRemoveInstalledModule(Edge, out string failReason))
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
            this.FailOn(() => Circle == null || !Circle.CanRemoveInstalledModule(Edge, out _));

            yield return Toils_Goto.GotoThing(CircleInd, PathEndMode.InteractionCell);

            Toil warmup = Toils_General.Wait(75);
            warmup.WithProgressBarToilDelay(CircleInd);
            warmup.FailOn(() => Circle == null || Circle.Destroyed || Circle.RitualActive);
            yield return warmup;

            Toil remove = new Toil();
            remove.initAction = () =>
            {
                Pawn actor = remove.actor;
                if (Circle == null || actor?.MapHeld == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                ThingDef removedDef = Circle.RemoveInstalledModule(Edge);
                if (removedDef == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                Thing moduleThing = ThingMaker.MakeThing(removedDef);
                moduleThing.stackCount = 1;
                IntVec3 placeCell = Circle.InteractionCell.IsValid ? Circle.InteractionCell : actor.PositionHeld;
                GenPlace.TryPlaceThing(moduleThing, placeCell, actor.MapHeld, ThingPlaceMode.Near);

                if (actor.rotationTracker != null)
                {
                    actor.rotationTracker.FaceCell(Circle.Position);
                }

                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", Circle.RitualFocusCell, actor.MapHeld);
                Messages.Message("ABY_CircleModuleRemoveSuccess".Translate(removedDef.label.CapitalizeFirst(), AbyssalCircleModuleUtility.GetEdgeLabel(Edge)), Circle, MessageTypeDefOf.TaskCompletion, false);
            };
            remove.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return remove;
        }
    }
}
