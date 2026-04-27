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
        private const TargetIndex StagingInd = TargetIndex.C;

        private Thing SigilThing => job.GetTarget(SigilInd).Thing;
        private Building_AbyssalSummoningCircle Circle => job.GetTarget(CircleInd).Thing as Building_AbyssalSummoningCircle;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Thing sigil = ABY_SigilUseValidator.ResolveSigil(pawn, SigilThing);
            Building_AbyssalSummoningCircle preferredCircle = Circle;
            ABY_SigilUseValidator.SigilUseContext context;
            string failReason;

            if (!ABY_SigilUseValidator.TryBuildContext(pawn, sigil, preferredCircle, true, out context, out failReason))
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            job.targetA = context.Sigil;
            job.targetB = context.Circle;

            if (TryResolveSigilStagingCell(pawn, context.Circle, out IntVec3 stagingCell))
            {
                job.targetC = stagingCell;
            }

            return ABY_SigilUseValidator.TryReserveContext(pawn, job, context, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(SigilInd);
            this.FailOnDestroyedOrNull(CircleInd);
            this.FailOn(() => Circle == null || !Circle.IsReadyForSigil(out _));

            Toil validateStart = new Toil();
            validateStart.initAction = () =>
            {
                Pawn actor = validateStart.actor;
                ABY_SigilUseValidator.SigilUseContext context;
                string failReason;
                if (!ABY_SigilUseValidator.TryBuildContext(actor, SigilThing, Circle, false, out context, out failReason))
                {
                    if (!failReason.NullOrEmpty())
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    }

                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                job.targetA = context.Sigil;
                job.targetB = context.Circle;

                if (!TryResolveSigilStagingCell(actor, context.Circle, out IntVec3 stagingCell))
                {
                    Messages.Message("ABY_SigilPlacementFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                job.targetC = stagingCell;
            };
            validateStart.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return validateStart;

            yield return Toils_Goto.GotoThing(SigilInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(SigilInd);

            Toil resolveStagingAfterPickup = new Toil();
            resolveStagingAfterPickup.initAction = () =>
            {
                Pawn actor = resolveStagingAfterPickup.actor;
                if (!TryResolveSigilStagingCell(actor, Circle, out IntVec3 stagingCell))
                {
                    Messages.Message("ABY_SigilPlacementFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                job.targetC = stagingCell;
            };
            resolveStagingAfterPickup.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return resolveStagingAfterPickup;

            Toil gotoStaging = Toils_Goto.GotoCell(StagingInd, PathEndMode.OnCell);
            gotoStaging.FailOn(() => Circle == null || Circle.Destroyed || !Circle.Spawned || Circle.RitualActive || !Circle.IsPoweredForRitual);
            yield return gotoStaging;

            Toil placeSigil = new Toil();
            placeSigil.initAction = () =>
            {
                Pawn actor = placeSigil.actor;
                IntVec3 preferredDropCell = job.GetTarget(StagingInd).Cell;
                if (!TryPlaceSigilNearCircle(actor, Circle, preferredDropCell))
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
                if (actor == null || Circle == null || actor.MapHeld == null || actor.jobs?.curDriver == null)
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

        private bool TryPlaceSigilNearCircle(Pawn actor, Building_AbyssalSummoningCircle circle, IntVec3 preferredDropCell)
        {
            if (actor?.carryTracker?.CarriedThing == null || circle == null || circle.MapHeld != actor.MapHeld)
            {
                return false;
            }

            if (!TryFindSafeSigilPlacementCell(actor, circle, preferredDropCell, out IntVec3 dropCell))
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

        private bool TryResolveSigilStagingCell(Pawn actor, Building_AbyssalSummoningCircle circle, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            Map map = actor?.MapHeld;
            if (map == null || circle == null || circle.def == null)
            {
                return false;
            }

            CellRect occupiedRect = GenAdj.OccupiedRect(circle.Position, circle.Rotation, circle.def.Size);

            IntVec3 existing = job.GetTarget(StagingInd).Cell;
            if (IsSafeSigilPlacementCell(existing, map, occupiedRect, actor) && CanActorReachCell(actor, existing))
            {
                result = existing;
                return true;
            }

            IntVec3 actorCell = actor.PositionHeld;
            if (IsSafeSigilPlacementCell(actorCell, map, occupiedRect, actor))
            {
                result = actorCell;
                return true;
            }

            if (TryFindClosestExternalCell(actor, circle, actorCell, occupiedRect, 8.9f, out result))
            {
                return true;
            }

            if (TryFindClosestExternalCell(actor, circle, circle.Position, occupiedRect, 9.9f, out result))
            {
                return true;
            }

            IntVec3 interactionCell = circle.InteractionCell;
            if (IsSafeSigilPlacementCell(interactionCell, map, occupiedRect, actor) && CanActorReachCell(actor, interactionCell))
            {
                result = interactionCell;
                return true;
            }

            return false;
        }

        private bool TryFindSafeSigilPlacementCell(Pawn actor, Building_AbyssalSummoningCircle circle, IntVec3 preferredCell, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            Map map = actor?.MapHeld;
            if (map == null || circle == null || circle.def == null)
            {
                return false;
            }

            CellRect occupiedRect = GenAdj.OccupiedRect(circle.Position, circle.Rotation, circle.def.Size);
            if (IsSafeSigilPlacementCell(preferredCell, map, occupiedRect, actor))
            {
                result = preferredCell;
                return true;
            }

            IntVec3 actorCell = actor.PositionHeld;
            if (IsSafeSigilPlacementCell(actorCell, map, occupiedRect, actor))
            {
                result = actorCell;
                return true;
            }

            return TryFindClosestExternalCell(actor, circle, actorCell, occupiedRect, 6.9f, out result)
                || TryFindClosestExternalCell(actor, circle, circle.Position, occupiedRect, 8.9f, out result);
        }

        private bool TryFindClosestExternalCell(Pawn actor, Building_AbyssalSummoningCircle circle, IntVec3 root, CellRect occupiedRect, float radius, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            Map map = actor?.MapHeld;
            if (map == null || circle == null)
            {
                return false;
            }

            float bestScore = float.MaxValue;
            int maxCells = GenRadial.NumCellsInRadius(radius);
            for (int i = 0; i < maxCells && i < GenRadial.RadialPattern.Length; i++)
            {
                IntVec3 cell = root + GenRadial.RadialPattern[i];
                if (!IsSafeSigilPlacementCell(cell, map, occupiedRect, actor) || !CanActorReachCell(actor, cell))
                {
                    continue;
                }

                float score = cell.DistanceToSquared(actor.PositionHeld) * 1.7f + cell.DistanceToSquared(circle.Position) * 0.25f;
                if (score < bestScore)
                {
                    bestScore = score;
                    result = cell;
                }
            }

            return result.IsValid;
        }

        private bool IsSafeSigilPlacementCell(IntVec3 cell, Map map, CellRect occupiedRect, Pawn actor)
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

            Pawn firstPawn = cell.GetFirstPawn(map);
            if (firstPawn != null && firstPawn != actor)
            {
                return false;
            }

            return true;
        }

        private bool CanActorReachCell(Pawn actor, IntVec3 cell)
        {
            return actor != null
                && actor.MapHeld != null
                && cell.IsValid
                && cell.InBounds(actor.MapHeld)
                && actor.CanReach(cell, PathEndMode.OnCell, Danger.Deadly);
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
