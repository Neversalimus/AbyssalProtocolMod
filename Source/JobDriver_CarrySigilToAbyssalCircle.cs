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

            Building_AbyssalSummoningCircle circle = ResolveCircle(map, out string failReason);
            if (circle == null)
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            if (!circle.IsReadyForSigil(out failReason))
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            job.targetB = circle;

            bool pawnAlreadyCarriesSigil = pawn.carryTracker != null && pawn.carryTracker.CarriedThing == sigil;
            if (!pawnAlreadyCarriesSigil && !pawn.CanReserveAndReach(sigil, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return false;
            }

            if (!pawn.CanReserveAndReach(circle, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return false;
            }

            if (!pawnAlreadyCarriesSigil && !pawn.Reserve(sigil, job, 1, job.count, null, errorOnFailed))
            {
                return false;
            }

            if (!pawn.Reserve(circle, job, 1, -1, null, errorOnFailed))
            {
                if (!pawnAlreadyCarriesSigil)
                {
                    pawn.MapHeld?.reservationManager?.Release(sigil, pawn, job);
                }

                return false;
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(SigilInd);
            this.FailOnDestroyedOrNull(CircleInd);
            this.FailOn(() => Circle == null || !Circle.IsReadyForSigil(out _));

            yield return Toils_Goto.GotoThing(SigilInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(SigilInd);
            yield return Toils_Goto.GotoThing(CircleInd, PathEndMode.InteractionCell);

            Toil placeSigil = new Toil();
            placeSigil.initAction = () =>
            {
                Pawn actor = placeSigil.actor;
                if (!TryPlaceSigilNearCircle(actor, Circle))
                {
                    Messages.Message("ABY_SigilPlacementFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                if (Circle != null && actor.rotationTracker != null)
                {
                    actor.rotationTracker.FaceCell(Circle.Position);
                    ABY_SoundUtility.PlayAt("ABY_SigilActivate", job.GetTarget(SigilInd).Cell, actor.MapHeld);
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
                    ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", job.GetTarget(SigilInd).Cell, actor.MapHeld);
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

        private Building_AbyssalSummoningCircle ResolveCircle(Map map, out string failReason)
        {
            Building_AbyssalSummoningCircle existing = Circle;
            if (IsValidCircle(existing, map))
            {
                failReason = null;
                return existing;
            }

            if (AbyssalBossSummonUtility.TryFindNearestAvailableCircle(
                    map,
                    pawn.PositionHeld,
                    out Building_AbyssalSummoningCircle found,
                    out failReason))
            {
                return found;
            }

            return null;
        }

        private bool IsValidCircle(Building_AbyssalSummoningCircle circle, Map map)
        {
            return circle != null
                && !circle.Destroyed
                && circle.Spawned
                && circle.MapHeld == map
                && !circle.RitualActive
                && circle.IsPoweredForRitual;
        }

        private int GetWarmupTicks()
        {
            Thing sigil = ResolveUsableSigil(pawn) ?? SigilThing;
            CompUseEffect_SummonBoss comp = sigil?.TryGetComp<CompUseEffect_SummonBoss>();
            if (comp != null && comp.Props != null && comp.Props.ritualWarmupTicks > 0)
            {
                return comp.Props.ritualWarmupTicks;
            }

            return 180;
        }

        private bool TryPlaceSigilNearCircle(Pawn actor, Building_AbyssalSummoningCircle circle)
        {
            if (actor?.carryTracker?.CarriedThing == null || circle == null || circle.MapHeld != actor.MapHeld)
            {
                return false;
            }

            if (!TryFindSafeSigilPlacementCell(actor, circle, out IntVec3 dropCell))
            {
                return false;
            }

            Thing droppedThing;
            if (actor.carryTracker.TryDropCarriedThing(dropCell, ThingPlaceMode.Direct, out droppedThing) ||
                actor.carryTracker.TryDropCarriedThing(dropCell, ThingPlaceMode.Near, out droppedThing))
            {
                if (droppedThing != null)
                {
                    job.targetA = droppedThing;
                    return true;
                }
            }

            return false;
        }

        private bool TryFindSafeSigilPlacementCell(Pawn actor, Building_AbyssalSummoningCircle circle, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            Map map = actor?.MapHeld;
            if (map == null || circle == null || circle.def == null)
            {
                return false;
            }

            CellRect occupiedRect = GenAdj.OccupiedRect(circle.Position, circle.Rotation, circle.def.Size);
            IntVec3 interactionCell = circle.InteractionCell;
            if (IsSafeSigilPlacementCell(interactionCell, map, occupiedRect))
            {
                result = interactionCell;
                return true;
            }

            IntVec3 actorCell = actor.PositionHeld;
            if (IsSafeSigilPlacementCell(actorCell, map, occupiedRect))
            {
                result = actorCell;
                return true;
            }

            for (int i = 0; i < GenRadial.NumCellsInRadius(6.9f); i++)
            {
                IntVec3 cell = circle.Position + GenRadial.RadialPattern[i];
                if (IsSafeSigilPlacementCell(cell, map, occupiedRect))
                {
                    result = cell;
                    return true;
                }
            }

            return false;
        }

        private bool IsSafeSigilPlacementCell(IntVec3 cell, Map map, CellRect occupiedRect)
        {
            if (!cell.IsValid || !cell.InBounds(map) || cell.Fogged(map) || occupiedRect.Contains(cell) || !cell.Standable(map))
            {
                return false;
            }

            Building edifice = cell.GetEdifice(map);
            if (edifice != null)
            {
                return false;
            }

            return true;
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
