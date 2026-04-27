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
            if (job.count <= 0)
            {
                job.count = 1;
            }

            Thing sigil = ResolveUsableSigil(pawn) ?? SigilThing;
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

            if (!IsCircleUsableForSigilJob(circle, out failReason))
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            if (!TryFindBestStagingCell(pawn, circle, out IntVec3 stagingCell))
            {
                Messages.Message("ABY_SigilPlacementFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            job.targetA = sigil;
            job.targetB = circle;
            job.targetC = stagingCell;

            bool pawnAlreadyCarriesSigil = pawn.carryTracker != null && pawn.carryTracker.CarriedThing == sigil;
            if (!pawnAlreadyCarriesSigil && !pawn.CanReserveAndReach(sigil, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                return false;
            }

            if (!pawn.CanReach(stagingCell, PathEndMode.OnCell, Danger.Deadly))
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

            Toil validateStart = new Toil();
            validateStart.initAction = () =>
            {
                Pawn actor = validateStart.actor;
                if (actor == null || actor.MapHeld == null)
                {
                    actor?.jobs?.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                Building_AbyssalSummoningCircle circle = Circle ?? ResolveCircle(actor.MapHeld, out _);
                Thing sigil = ResolveUsableSigil(actor) ?? SigilThing;
                if (sigil == null || circle == null || !IsCircleUsableForSigilJob(circle, out string failReason))
                {
                    if (!failReason.NullOrEmpty())
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    }

                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                if (!TryFindBestStagingCell(actor, circle, out IntVec3 stagingCell))
                {
                    Messages.Message("ABY_SigilPlacementFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                job.targetA = sigil;
                job.targetB = circle;
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
                Building_AbyssalSummoningCircle circle = Circle;
                if (actor == null || circle == null || !IsCircleUsableForSigilJob(circle, out string failReason))
                {
                    if (!failReason.NullOrEmpty())
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    }

                    actor?.jobs?.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                if (!TryFindBestStagingCell(actor, circle, out IntVec3 stagingCell))
                {
                    Messages.Message("ABY_SigilPlacementFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                job.targetC = stagingCell;
            };
            resolveStagingAfterPickup.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return resolveStagingAfterPickup;

            yield return Toils_Goto.GotoCell(StagingInd, PathEndMode.OnCell);

            Toil placeSigil = new Toil();
            placeSigil.initAction = () =>
            {
                Pawn actor = placeSigil.actor;
                if (!TryPlaceSigilAtStagingCell(actor, Circle))
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

        private bool IsCircleUsableForSigilJob(Building_AbyssalSummoningCircle circle, out string failReason)
        {
            failReason = null;
            if (circle == null || circle.Destroyed || !circle.Spawned || circle.Map == null)
            {
                failReason = "ABY_CircleFail_NotPlaced".Translate();
                return false;
            }

            if (circle.RitualActive)
            {
                failReason = "ABY_CircleFail_Busy".Translate();
                return false;
            }

            if (!circle.IsPoweredForRitual)
            {
                failReason = "ABY_CircleFail_NoPower".Translate();
                return false;
            }

            if (!circle.HasValidInteractionCell(out failReason))
            {
                return false;
            }

            if (AbyssalBossSummonUtility.HasActiveAbyssalEncounter(circle.Map))
            {
                failReason = "ABY_BossSummonFail_EncounterActive".Translate();
                return false;
            }

            return true;
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

        private bool TryPlaceSigilAtStagingCell(Pawn actor, Building_AbyssalSummoningCircle circle)
        {
            if (actor?.carryTracker?.CarriedThing == null || circle == null || circle.MapHeld != actor.MapHeld)
            {
                return false;
            }

            IntVec3 dropCell = job.GetTarget(StagingInd).Cell;
            if (!IsSafeSigilPlacementCell(dropCell, actor.MapHeld, GetCircleOccupiedRect(circle), actor))
            {
                if (!TryFindBestStagingCell(actor, circle, out dropCell))
                {
                    return false;
                }

                job.targetC = dropCell;
            }

            if (actor.PositionHeld != dropCell)
            {
                return false;
            }

            Thing droppedThing;
            if (actor.carryTracker.TryDropCarriedThing(dropCell, ThingPlaceMode.Direct, out droppedThing) && droppedThing != null)
            {
                job.targetA = droppedThing;
                return true;
            }

            return false;
        }

        private bool TryFindBestStagingCell(Pawn actor, Building_AbyssalSummoningCircle circle, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            Map map = actor?.MapHeld;
            if (map == null || circle == null || circle.def == null)
            {
                return false;
            }

            CellRect occupiedRect = GetCircleOccupiedRect(circle);
            IntVec3 origin = actor.PositionHeld;
            int bestScore = int.MaxValue;
            IntVec3 interactionCell = circle.InteractionCell;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(circle.Position, 10.9f, true))
            {
                if (!IsOuterRingCell(cell, occupiedRect))
                {
                    continue;
                }

                if (!IsSafeSigilPlacementCell(cell, map, occupiedRect, actor))
                {
                    continue;
                }

                if (!actor.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                int score = cell.DistanceToSquared(origin) * 100 + cell.DistanceToSquared(circle.Position);
                if (cell == interactionCell)
                {
                    score -= 4;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    result = cell;
                }
            }

            return result.IsValid;
        }

        private CellRect GetCircleOccupiedRect(Building_AbyssalSummoningCircle circle)
        {
            return GenAdj.OccupiedRect(circle.Position, circle.Rotation, circle.def.Size);
        }

        private bool IsOuterRingCell(IntVec3 cell, CellRect occupiedRect)
        {
            if (!cell.IsValid || occupiedRect.Contains(cell))
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                if (occupiedRect.Contains(cell + GenAdj.CardinalDirections[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSafeSigilPlacementCell(IntVec3 cell, Map map, CellRect occupiedRect, Pawn actor)
        {
            if (!cell.IsValid || map == null || !cell.InBounds(map) || cell.Fogged(map) || occupiedRect.Contains(cell) || !cell.Standable(map))
            {
                return false;
            }

            Building edifice = cell.GetEdifice(map);
            if (edifice != null)
            {
                return false;
            }

            Pawn pawn = cell.GetFirstPawn(map);
            if (pawn != null && pawn != actor)
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
