using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
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

        public static bool TryBuildContext(Pawn pawn, Thing sigil, Building_AbyssalSummoningCircle preferredCircle, bool requireReachability, out SigilUseContext context, out string failReason)
        {
            context = null;
            failReason = null;

            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                failReason = "No valid pawn is available to invoke this sigil.";
                return false;
            }

            Thing resolvedSigil = ResolveSigil(pawn, sigil);
            if (resolvedSigil == null || resolvedSigil.Destroyed)
            {
                failReason = "The sigil is no longer available.";
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
                failReason = "The sigil does not contain a valid abyssal invocation payload.";
                return false;
            }

            CompProperties_UseEffectSummonBoss props = summonComp.Props;
            if (AbyssalDominionAccessUtility.IsDominionRitualId(props.ritualId) && !AbyssalDominionAccessUtility.IsUserFacingDominionContentEnabled())
            {
                failReason = "ABY_DominionSigilDisabled".Translate();
                return false;
            }

            Building_AbyssalSummoningCircle circle = ResolveCircle(pawn, map, preferredCircle, out failReason);
            if (circle == null)
            {
                return false;
            }

            if (!circle.IsReadyForSigil(out failReason))
            {
                return false;
            }

            bool pawnAlreadyCarriesSigil = pawn.carryTracker != null && pawn.carryTracker.CarriedThing == resolvedSigil;
            if (requireReachability && !CanReachRequiredTargets(pawn, resolvedSigil, circle, pawnAlreadyCarriesSigil, out failReason))
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

        public static bool CanReachRequiredTargets(Pawn pawn, Thing sigil, Building_AbyssalSummoningCircle circle, bool pawnAlreadyCarriesSigil, out string failReason)
        {
            failReason = null;
            if (pawn == null || sigil == null || circle == null)
            {
                failReason = "The sigil invocation target is no longer valid.";
                return false;
            }

            if (!pawnAlreadyCarriesSigil)
            {
                if (!sigil.Spawned)
                {
                    failReason = "The sigil is not accessible.";
                    return false;
                }

                if (!pawn.CanReserveAndReach(sigil, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    failReason = "The sigil cannot be reached or reserved.";
                    return false;
                }
            }

            if (!pawn.CanReserveAndReach(circle, PathEndMode.InteractionCell, Danger.Deadly))
            {
                failReason = "The abyssal summoning circle cannot be reached or reserved.";
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

        private static Building_AbyssalSummoningCircle ResolveCircle(Pawn pawn, Map map, Building_AbyssalSummoningCircle preferredCircle, out string failReason)
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
