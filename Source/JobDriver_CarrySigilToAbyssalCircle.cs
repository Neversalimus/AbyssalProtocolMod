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
            if (IsCircleEdgeSigilCell(existing, map, occupiedRect, actor) && CanActorReachCell(actor, existing))
            {
                result = existing;
                return true;
            }

            IntVec3 interactionCell = circle.InteractionCell;
            if (IsCircleEdgeSigilCell(interactionCell, map, occupiedRect, actor) && CanActorReachCell(actor, interactionCell))
            {
                result = interactionCell;
                return true;
            }

            // Never use the pawn's current remote position as the staging cell. The staging cell must be
            // on the outside rim of the circle so the sigil is visually and mechanically carried to the ritual site.
            if (TryFindBestCircleEdgeCell(actor, circle, occupiedRect, out result))
            {
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
            if (IsCircleEdgeSigilCell(preferredCell, map, occupiedRect, actor))
            {
                result = preferredCell;
                return true;
            }

            return TryFindBestCircleEdgeCell(actor, circle, occupiedRect, out result);
        }

        private bool TryFindBestCircleEdgeCell(Pawn actor, Building_AbyssalSummoningCircle circle, CellRect occupiedRect, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            Map map = actor?.MapHeld;
            if (map == null || circle == null)
            {
                return false;
            }

            float bestScore = float.MaxValue;
            CellRect searchRect = occupiedRect.ExpandedBy(2).ClipInsideMap(map);
            foreach (IntVec3 cell in searchRect.Cells)
            {
                if (!IsCircleEdgeSigilCell(cell, map, occupiedRect, actor) || !CanActorReachCell(actor, cell))
                {
                    continue;
                }

                // Must be close to the circle, but choose the side the pawn can reach most naturally.
                float score = 0f;
                score += cell.DistanceToSquared(actor.PositionHeld) * 1.0f;
                score += cell.DistanceToSquared(circle.InteractionCell) * 0.65f;
                score += cell.DistanceToSquared(circle.Position) * 0.18f;

                if (score < bestScore)
                {
                    bestScore = score;
                    result = cell;
                }
            }

            return result.IsValid;
        }

        private bool IsCircleEdgeSigilCell(IntVec3 cell, Map map, CellRect occupiedRect, Pawn actor)
        {
            if (!IsSafeSigilPlacementCell(cell, map, occupiedRect, actor))
            {
                return false;
            }

            // ExpandedBy(1) minus occupiedRect means the cell is visually at the circle,
            // but never inside/center of the circle footprint.
            return occupiedRect.ExpandedBy(1).Contains(cell);
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
