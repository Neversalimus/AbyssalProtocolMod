using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    /// <summary>
    /// Normal player invocation route. The sigil stays in the carrier's hands at the
    /// circle interaction cell; presentation never depends on a ground staging cell.
    /// </summary>
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

            Thing sigil = ResolveUsableSigil(pawn) ?? SigilThing;
            Building_AbyssalSummoningCircle preferredCircle = Circle;
            if (!ABY_SigilUseValidator.TryBuildContext(
                    pawn,
                    sigil,
                    preferredCircle,
                    true,
                    out ABY_SigilUseValidator.SigilUseContext context,
                    out string failReason))
            {
                ReportFailure(failReason);
                return false;
            }

            job.targetA = context.Sigil;
            job.targetB = context.Circle;
            return ABY_SigilUseValidator.TryReserveContext(pawn, job, context, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil validateStart = new Toil();
            validateStart.initAction = () =>
            {
                Pawn actor = validateStart.actor;
                if (!TryValidateInvocation(actor, false, out string failReason))
                {
                    FailInvocation(actor, failReason);
                }
            };
            validateStart.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return validateStart;

            yield return Toils_Goto.GotoThing(SigilInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(SigilInd);

            Toil validateHeldSigil = new Toil();
            validateHeldSigil.initAction = () =>
            {
                Pawn actor = validateHeldSigil.actor;
                if (!TryValidateInvocation(actor, true, out string failReason))
                {
                    FailInvocation(actor, failReason);
                }
            };
            validateHeldSigil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return validateHeldSigil;

            yield return Toils_Goto.GotoThing(CircleInd, PathEndMode.InteractionCell);

            Toil beginPriming = new Toil();
            beginPriming.initAction = () =>
            {
                Pawn actor = beginPriming.actor;
                Building_AbyssalSummoningCircle circle = Circle;
                if (!TryValidateInvocation(actor, true, out string failReason))
                {
                    FailInvocation(actor, failReason);
                    return;
                }

                circle.NotifySigilPriming(0f, actor.thingIDNumber);
                ABY_SoundUtility.PlayAt("ABY_SigilActivate", circle.InteractionCell, actor.MapHeld);
            };
            beginPriming.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return beginPriming;

            Toil warmup = Toils_General.Wait(GetWarmupTicks());
            warmup.WithProgressBarToilDelay(CircleInd);
            warmup.tickAction = () =>
            {
                Pawn actor = warmup.actor;
                Building_AbyssalSummoningCircle circle = Circle;
                if (actor == null || circle == null)
                {
                    return;
                }

                if (actor.IsHashIntervalTick(10) && !TryValidateInvocation(actor, true, out string failReason))
                {
                    FailInvocation(actor, failReason);
                    return;
                }

                int warmupTicks = GetWarmupTicks();
                int ticksLeft = actor.jobs?.curDriver != null ? actor.jobs.curDriver.ticksLeftThisToil : 0;
                float primingProgress = warmupTicks > 0
                    ? 1f - Mathf.Clamp01((float)ticksLeft / warmupTicks)
                    : 1f;
                circle.NotifySigilPriming(primingProgress, actor.thingIDNumber);

                if ((actor.IsHashIntervalTick(30) || ticksLeft == warmupTicks - 1) && circle.IsPoweredForRitual)
                {
                    ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", circle.InteractionCell, actor.MapHeld);
                }
            };
            warmup.AddFinishAction(delegate
            {
                Circle?.NotifySigilPrimingEnded();
            });
            yield return warmup;

            Toil invoke = new Toil();
            invoke.initAction = () =>
            {
                Pawn actor = invoke.actor;
                if (!TryValidateInvocation(actor, true, out string failReason))
                {
                    FailInvocation(actor, failReason);
                    return;
                }

                Thing sigil = ResolveUsableSigil(actor);
                CompUseEffect_SummonBoss comp = sigil?.TryGetComp<CompUseEffect_SummonBoss>();
                if (comp == null)
                {
                    FailInvocation(actor, "ABY_SigilInvocationFail_InvalidPayload".Translate());
                    return;
                }

                comp.DoEffect(actor);
            };
            invoke.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return invoke;
        }

        private bool TryValidateInvocation(Pawn actor, bool requireHeldSigil, out string failReason)
        {
            failReason = null;
            if (actor == null || actor.Destroyed || actor.Dead || actor.MapHeld == null)
            {
                failReason = "ABY_SigilInvocationFail_NoPawn".Translate();
                return false;
            }

            Building_AbyssalSummoningCircle circle = Circle;
            Thing sigil = ResolveUsableSigil(actor) ?? SigilThing;
            if (circle == null || sigil == null)
            {
                failReason = "ABY_SigilInvocationFail_TargetInvalid".Translate();
                return false;
            }

            if (requireHeldSigil && !ABY_SigilUseValidator.IsCarryingSigil(actor, sigil))
            {
                failReason = "ABY_SigilInvocationFail_LostHeldSigil".Translate();
                return false;
            }

            if (!circle.IsPoweredForRitual)
            {
                failReason = "ABY_SigilInvocationFail_PowerInterrupted".Translate();
                return false;
            }

            if (requireHeldSigil && actor.PositionHeld != circle.InteractionCell)
            {
                failReason = "ABY_SigilInvocationFail_LeftInteractionCell".Translate();
                return false;
            }

            if (!ABY_SigilUseValidator.TryBuildContext(
                    actor,
                    sigil,
                    circle,
                    true,
                    out ABY_SigilUseValidator.SigilUseContext context,
                    out failReason))
            {
                return false;
            }

            job.targetA = context.Sigil;
            job.targetB = context.Circle;
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

        private Thing ResolveUsableSigil(Pawn actor)
        {
            return ABY_SigilUseValidator.ResolveSigil(actor, SigilThing);
        }

        private void FailInvocation(Pawn actor, string failReason)
        {
            Circle?.NotifySigilPrimingEnded();
            TryReleaseHeldSigil(actor);
            ReportFailure(failReason);
            actor?.jobs?.EndCurrentJob(JobCondition.Incompletable);
        }

        private void TryReleaseHeldSigil(Pawn actor)
        {
            if (actor?.carryTracker?.CarriedThing == null || actor.MapHeld == null)
            {
                return;
            }

            Thing dropped;
            actor.carryTracker.TryDropCarriedThing(actor.PositionHeld, ThingPlaceMode.Near, out dropped);
        }

        private void ReportFailure(string failReason)
        {
            if (!failReason.NullOrEmpty())
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
