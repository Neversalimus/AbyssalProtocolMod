using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    /// <summary>
    /// Shared authoritative validation and operator-routing surface for every normal sigil path:
    /// direct use, Console, Sigil Vault, job reservation, warmup and final activation.
    /// </summary>
    public static class ABY_SigilUseValidator
    {
        public sealed class SigilUseContext
        {
            public Pawn Pawn;
            public Thing Sigil;
            public Map Map;
            public CompUseEffect_SummonBoss SummonComp;
            public CompProperties_UseEffectSummonBoss Props;
            public Building_AbyssalSummoningCircle Circle;
            public bool PawnAlreadyCarriesSigil;
        }

        public sealed class OperatorRouteReport
        {
            public Pawn BestOperator;
            public string FailureReason;
            public int FreeColonists;
            public int HealthyCandidates;
            public int ManipulationCandidates;
            public int SigilReachCandidates;
            public int CircleReachCandidates;
            public int BothReachCandidates;
            public int EligibleCandidates;
            public bool SigilForbidden;
            public bool SigilAlreadyCarried;

            public bool HasEligibleOperator => BestOperator != null;

            public void AppendDiagnosticReport(System.Text.StringBuilder sb)
            {
                if (sb == null)
                {
                    return;
                }

                sb.AppendLine("Operator route:");
                sb.AppendLine(" - free=" + FreeColonists
                    + " | healthy=" + HealthyCandidates
                    + " | manipulation=" + ManipulationCandidates
                    + " | sigilReach=" + SigilReachCandidates
                    + " | circleReach=" + CircleReachCandidates
                    + " | bothReach=" + BothReachCandidates
                    + " | eligible=" + EligibleCandidates
                    + " | sigilForbidden=" + SigilForbidden
                    + " | sigilCarried=" + SigilAlreadyCarried);
                sb.AppendLine(" - best operator: " + (BestOperator?.LabelShortCap ?? "none")
                    + " | failure: " + (FailureReason ?? "none"));
            }
        }

        public static bool TryBuildContext(
            Pawn pawn,
            Thing sigil,
            Building_AbyssalSummoningCircle preferredCircle,
            bool requireReachability,
            out SigilUseContext context,
            out string failReason)
        {
            context = null;
            failReason = null;

            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                failReason = "ABY_SigilInvocationFail_NoPawn".Translate();
                return false;
            }

            Thing resolvedSigil = ResolveSigil(pawn, sigil);
            if (resolvedSigil == null || resolvedSigil.Destroyed)
            {
                failReason = "ABY_SigilInvocationFail_SigilMissing".Translate();
                return false;
            }

            Map map = pawn.MapHeld ?? resolvedSigil.MapHeld ?? preferredCircle?.MapHeld;
            if (map == null)
            {
                failReason = "ABY_BossSummonFail_NoMap".Translate();
                return false;
            }

            CompUseEffect_SummonBoss summonComp = resolvedSigil.TryGetComp<CompUseEffect_SummonBoss>();
            if (summonComp == null || summonComp.Props == null)
            {
                failReason = "ABY_SigilInvocationFail_InvalidPayload".Translate();
                return false;
            }

            CompProperties_UseEffectSummonBoss props = summonComp.Props;
            if (AbyssalDominionAccessUtility.IsDominionRitualId(props.ritualId)
                && !AbyssalDominionAccessUtility.IsUserFacingDominionContentEnabled())
            {
                failReason = "ABY_DominionSigilDisabled".Translate();
                return false;
            }

            AbyssalBossSummonUtility.TryCleanupStaleEncounterBeforeSummon(map, "sigil use pre-summon active encounter check");

            Building_AbyssalSummoningCircle circle = ResolveCircle(pawn, map, preferredCircle, out failReason);
            if (circle == null)
            {
                return false;
            }

            ABY_SummonPreflightReport preflight = ABY_SummonPreflightReport.Create(
                circle,
                props,
                pawn,
                resolvedSigil,
                requireReachability,
                true);
            if (!preflight.CanStart)
            {
                failReason = preflight.PrimaryBlocker ?? "ABY_SigilInvocationFail_Preflight".Translate();
                return false;
            }

            bool pawnAlreadyCarriesSigil = IsCarryingSigil(pawn, resolvedSigil);
            if (requireReachability
                && !CanReachRequiredTargets(pawn, resolvedSigil, circle, pawnAlreadyCarriesSigil, out failReason))
            {
                return false;
            }

            context = new SigilUseContext
            {
                Pawn = pawn,
                Sigil = resolvedSigil,
                Map = map,
                SummonComp = summonComp,
                Props = props,
                Circle = circle,
                PawnAlreadyCarriesSigil = pawnAlreadyCarriesSigil
            };
            return true;
        }

        public static bool TryReserveContext(Pawn pawn, Job job, SigilUseContext context, bool errorOnFailed)
        {
            if (pawn == null || job == null || context == null || context.Sigil == null || context.Circle == null)
            {
                return false;
            }

            if (job.count <= 0)
            {
                job.count = 1;
            }

            bool reservedSigil = false;
            if (!context.PawnAlreadyCarriesSigil && context.Sigil.Spawned)
            {
                if (!pawn.Reserve(context.Sigil, job, 1, job.count, null, errorOnFailed))
                {
                    return false;
                }

                reservedSigil = true;
            }

            if (!pawn.Reserve(context.Circle, job, 1, -1, null, errorOnFailed))
            {
                if (reservedSigil)
                {
                    pawn.MapHeld?.reservationManager?.Release(context.Sigil, pawn, job);
                }

                return false;
            }

            return true;
        }

        public static OperatorRouteReport EvaluateOperatorRoute(
            Building_AbyssalSummoningCircle circle,
            Thing sigil,
            Pawn requiredPawn = null,
            bool requireReservations = true)
        {
            OperatorRouteReport report = new OperatorRouteReport();
            if (circle == null || circle.Destroyed || !circle.Spawned || circle.Map == null)
            {
                report.FailureReason = "ABY_CircleFail_NotPlaced".Translate();
                return report;
            }

            if (sigil == null || sigil.Destroyed)
            {
                report.FailureReason = "ABY_SigilInvocationFail_SigilMissing".Translate();
                return report;
            }

            report.SigilForbidden = sigil.Spawned && sigil.IsForbidden(Faction.OfPlayer);
            if (requiredPawn != null)
            {
                EvaluateCandidate(requiredPawn, circle, sigil, requireReservations, report, true);
                if (report.BestOperator == null && report.FailureReason.NullOrEmpty())
                {
                    report.FailureReason = BuildRouteFailureReason(report);
                }

                return report;
            }

            List<Pawn> pawns = circle.Map.mapPawns?.FreeColonistsSpawned;
            if (pawns == null || pawns.Count == 0)
            {
                report.FailureReason = "ABY_SigilInvocationFail_NoFreeColonist".Translate();
                return report;
            }

            float bestScore = float.MaxValue;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                bool eligible = EvaluateCandidate(pawn, circle, sigil, requireReservations, report, false);
                if (!eligible)
                {
                    continue;
                }

                float score = pawn.PositionHeld.DistanceToSquared(sigil.PositionHeld)
                    + sigil.PositionHeld.DistanceToSquared(circle.InteractionCell) * 0.45f;
                if (pawn.Drafted)
                {
                    score += 4000f;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    report.BestOperator = pawn;
                }
            }

            if (report.BestOperator == null)
            {
                report.FailureReason = BuildRouteFailureReason(report);
            }

            return report;
        }

        public static bool TryFindBestOperator(
            Building_AbyssalSummoningCircle circle,
            Thing sigil,
            out Pawn bestOperator,
            out string failReason)
        {
            OperatorRouteReport report = EvaluateOperatorRoute(circle, sigil, null, true);
            bestOperator = report.BestOperator;
            failReason = report.FailureReason;
            return bestOperator != null;
        }

        public static bool CanReachRequiredTargets(
            Pawn pawn,
            Thing sigil,
            Building_AbyssalSummoningCircle circle,
            bool pawnAlreadyCarriesSigil,
            out string failReason)
        {
            failReason = null;
            if (pawn == null || sigil == null || circle == null)
            {
                failReason = "ABY_SigilInvocationFail_TargetInvalid".Translate();
                return false;
            }

            OperatorRouteReport report = EvaluateOperatorRoute(circle, sigil, pawn, true);
            if (report.BestOperator == null)
            {
                failReason = report.FailureReason ?? "ABY_SigilInvocationFail_OperatorRoute".Translate();
                return false;
            }

            return true;
        }

        public static bool IsValidCircle(Building_AbyssalSummoningCircle circle, Map map)
        {
            return circle != null
                && !circle.Destroyed
                && circle.Spawned
                && circle.MapHeld == map
                && !circle.RitualActive
                && circle.IsPoweredForRitual;
        }

        public static Thing ResolveSigil(Pawn pawn, Thing sigil)
        {
            if (sigil != null && !sigil.Destroyed)
            {
                return sigil;
            }

            Thing carried = pawn?.carryTracker?.CarriedThing;
            if (carried != null && !carried.Destroyed && carried.TryGetComp<CompUseEffect_SummonBoss>() != null)
            {
                return carried;
            }

            return null;
        }

        public static bool IsCarryingSigil(Pawn pawn, Thing sigil)
        {
            return pawn?.carryTracker != null
                && pawn.carryTracker.CarriedThing == sigil;
        }

        private static bool EvaluateCandidate(
            Pawn pawn,
            Building_AbyssalSummoningCircle circle,
            Thing sigil,
            bool requireReservations,
            OperatorRouteReport report,
            bool requiredPawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Downed || pawn.jobs == null || pawn.InMentalState)
            {
                return false;
            }

            report.FreeColonists++;
            report.HealthyCandidates++;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return false;
            }

            report.ManipulationCandidates++;
            bool carriesSigil = IsCarryingSigil(pawn, sigil);
            report.SigilAlreadyCarried |= carriesSigil;

            bool canReachSigil = carriesSigil
                || (!sigil.Spawned
                    ? false
                    : (requireReservations
                        ? pawn.CanReserveAndReach(sigil, PathEndMode.ClosestTouch, Danger.Deadly)
                        : pawn.CanReach(sigil, PathEndMode.ClosestTouch, Danger.Deadly)));
            if (canReachSigil)
            {
                report.SigilReachCandidates++;
            }

            bool canReachCircle = requireReservations
                ? pawn.CanReserveAndReach(circle, PathEndMode.InteractionCell, Danger.Deadly)
                : pawn.CanReach(circle, PathEndMode.InteractionCell, Danger.Deadly);
            if (canReachCircle)
            {
                report.CircleReachCandidates++;
            }

            if (!canReachSigil || !canReachCircle)
            {
                return false;
            }

            report.BothReachCandidates++;
            report.EligibleCandidates++;
            if (requiredPawn)
            {
                report.BestOperator = pawn;
            }

            return true;
        }

        private static string BuildRouteFailureReason(OperatorRouteReport report)
        {
            if (report == null)
            {
                return "ABY_SigilInvocationFail_OperatorRoute".Translate();
            }

            if (report.FreeColonists <= 0)
            {
                return "ABY_SigilInvocationFail_NoFreeColonist".Translate();
            }

            if (report.ManipulationCandidates <= 0)
            {
                return "ABY_SigilInvocationFail_NoManipulation".Translate();
            }

            if (report.SigilReachCandidates <= 0)
            {
                return "ABY_SigilInvocationFail_SigilUnreachable".Translate();
            }

            if (report.CircleReachCandidates <= 0)
            {
                return "ABY_SigilInvocationFail_CircleUnreachable".Translate();
            }

            if (report.BothReachCandidates <= 0)
            {
                return "ABY_SigilInvocationFail_NoSharedRoute".Translate();
            }

            return "ABY_SigilInvocationFail_Reserved".Translate();
        }

        private static Building_AbyssalSummoningCircle ResolveCircle(
            Pawn pawn,
            Map map,
            Building_AbyssalSummoningCircle preferredCircle,
            out string failReason)
        {
            failReason = null;
            if (IsValidCircle(preferredCircle, map) && preferredCircle.IsReadyForSigil(out failReason))
            {
                return preferredCircle;
            }

            if (AbyssalBossSummonUtility.TryFindNearestAvailableCircle(map, pawn.PositionHeld, out Building_AbyssalSummoningCircle found, out failReason))
            {
                return found;
            }

            return null;
        }
    }
}
