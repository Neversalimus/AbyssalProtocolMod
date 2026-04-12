using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class JobDriver_CarrySigilToAbyssalCircle : JobDriver
    {
        private const TargetIndex SigilInd = TargetIndex.A;
        private const TargetIndex CircleInd = TargetIndex.B;

        private Thing SigilThing => job.GetTarget(SigilInd).Thing;
        private Building_AbyssalSummoningCircle Circle => job.GetTarget(CircleInd).Thing as Building_AbyssalSummoningCircle;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (job.count <= 0)
            {
                job.count = 1;
            }

            Thing sigil = SigilThing;
            if (sigil == null)
            {
                return false;
            }

            Map map = pawn.MapHeld;
            if (map == null)
            {
                return false;
            }

            if (!AbyssalBossSummonUtility.TryFindNearestAvailableCircle(
                    map,
                    pawn.PositionHeld,
                    out Building_AbyssalSummoningCircle circle,
                    out string failReason))
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            job.targetB = circle;

            if (!pawn.Reserve(sigil, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            if (!pawn.Reserve(circle, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(SigilInd);
            this.FailOnDestroyedOrNull(CircleInd);
            this.FailOn(() => Circle == null || Circle.Destroyed || !Circle.Spawned || Circle.RitualActive || !Circle.IsPoweredForRitual);

            yield return Toils_Goto.GotoThing(SigilInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(SigilInd);
            yield return Toils_Goto.GotoThing(CircleInd, PathEndMode.InteractionCell);

            Toil placeSigil = new Toil();
            placeSigil.initAction = () =>
            {
                Pawn actor = placeSigil.actor;
                TryPlaceSigilOnCircle(actor, Circle);

                if (Circle != null && actor.rotationTracker != null)
                {
                    actor.rotationTracker.FaceCell(Circle.Position);
                    ABY_SoundUtility.PlayAt("ABY_SigilActivate", Circle.RitualFocusCell, actor.MapHeld);
                }
            };
            placeSigil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return placeSigil;

            Toil warmup = Toils_General.Wait(GetWarmupTicks());
            warmup.FailOn(() => Circle == null || Circle.Destroyed || !Circle.Spawned || Circle.RitualActive || !Circle.IsPoweredForRitual);
            warmup.WithProgressBarToilDelay(CircleInd);
            warmup.tickAction = () =>
            {
                Pawn actor = warmup.actor;
                if (actor == null || Circle == null || actor.MapHeld == null)
                {
                    return;
                }

                if ((actor.IsHashIntervalTick(30) || actor.jobs.curDriver.ticksLeftThisToil == GetWarmupTicks() - 1) && Circle.IsPoweredForRitual)
                {
                    ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", Circle.RitualFocusCell, actor.MapHeld);
                }
            };
            yield return warmup;

            Toil invoke = new Toil();
            invoke.initAction = () =>
            {
                Pawn actor = invoke.actor;
                Thing sigil = ResolveUsableSigil(actor);
                CompUseEffect_SummonBoss comp = sigil?.TryGetComp<CompUseEffect_SummonBoss>();
                if (comp == null)
                {
                    Messages.Message("The sigil could not be activated.", MessageTypeDefOf.RejectInput, false);
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                comp.DoEffect(actor);
            };
            invoke.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return invoke;
        }

        private int GetWarmupTicks()
        {
            Thing sigil = SigilThing;
            CompUseEffect_SummonBoss comp = sigil?.TryGetComp<CompUseEffect_SummonBoss>();
            if (comp != null && comp.Props != null && comp.Props.ritualWarmupTicks > 0)
            {
                return comp.Props.ritualWarmupTicks;
            }

            return 180;
        }

        private void TryPlaceSigilOnCircle(Pawn actor, Building_AbyssalSummoningCircle circle)
        {
            if (actor?.carryTracker?.CarriedThing == null || circle == null || circle.MapHeld != actor.MapHeld)
            {
                return;
            }

            IntVec3 focusCell = circle.RitualFocusCell;
            if (!focusCell.IsValid || !focusCell.InBounds(actor.MapHeld))
            {
                return;
            }

            Thing droppedThing;
            if (actor.carryTracker.TryDropCarriedThing(focusCell, ThingPlaceMode.Direct, out droppedThing) ||
                actor.carryTracker.TryDropCarriedThing(focusCell, ThingPlaceMode.Near, out droppedThing))
            {
                if (droppedThing != null)
                {
                    job.targetA = droppedThing;
                }
            }
        }

        private Thing ResolveUsableSigil(Pawn actor)
        {
            Thing targetThing = job.GetTarget(SigilInd).Thing;
            if (targetThing != null && !targetThing.Destroyed)
            {
                return targetThing;
            }

            return actor?.carryTracker?.CarriedThing;
        }
    }
}
