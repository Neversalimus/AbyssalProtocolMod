using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class JobDriver_InstallCircleModule : JobDriver
    {
        private const TargetIndex ModuleInd = TargetIndex.A;
        private const TargetIndex CircleInd = TargetIndex.B;
        private const TargetIndex EdgeInd = TargetIndex.C;

        private Thing ModuleThing => job.GetTarget(ModuleInd).Thing;
        private Building_AbyssalSummoningCircle Circle => job.GetTarget(CircleInd).Thing as Building_AbyssalSummoningCircle;
        private AbyssalCircleModuleEdge Edge => (AbyssalCircleModuleEdge)job.GetTarget(EdgeInd).Cell.x;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (job.count <= 0)
            {
                job.count = 1;
            }

            if (ModuleThing == null || Circle == null || Circle.MapHeld != pawn.MapHeld)
            {
                return false;
            }

            if (!Circle.CanInstallModule(ModuleThing.def, Edge, out string failReason))
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }
                return false;
            }

            if (!pawn.CanReserveAndReach(ModuleThing, PathEndMode.Touch, Danger.Deadly))
            {
                return false;
            }

            if (!pawn.CanReserveAndReach(Circle, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return false;
            }

            if (!pawn.Reserve(ModuleThing, job, 1, -1, null, errorOnFailed))
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
            this.FailOnDestroyedOrNull(ModuleInd);
            this.FailOnDestroyedOrNull(CircleInd);
            this.FailOn(() => Circle == null || !Circle.CanInstallModule(ModuleThing?.def, Edge, out _));

            yield return Toils_Goto.GotoThing(ModuleInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(ModuleInd);
            yield return Toils_Goto.GotoThing(CircleInd, PathEndMode.InteractionCell);

            Toil warmup = Toils_General.Wait(90);
            warmup.WithProgressBarToilDelay(CircleInd);
            warmup.FailOn(() => Circle == null || Circle.Destroyed || Circle.RitualActive);
            yield return warmup;

            Toil install = new Toil();
            install.initAction = () =>
            {
                Pawn actor = install.actor;
                Thing carriedThing = actor?.carryTracker?.CarriedThing;
                if (carriedThing == null || Circle == null)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                string installedLabel = carriedThing.LabelCap;
                if (!Circle.TryInstallModuleDirect(carriedThing.def, Edge, out string failReason))
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

                if (actor.rotationTracker != null)
                {
                    actor.rotationTracker.FaceCell(Circle.Position);
                }

                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", Circle.RitualFocusCell, actor.MapHeld);
                Messages.Message("ABY_CircleModuleInstallSuccess".Translate(installedLabel, AbyssalCircleModuleUtility.GetEdgeLabel(Edge)), Circle, MessageTypeDefOf.TaskCompletion, false);
            };
            install.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return install;
        }
    }
}
