using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompUsable_ArchonSigil : CompUsable
    {
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn myPawn)
        {
            if (!TryValidateUse(myPawn, out Building_AbyssalSummoningCircle circle, out string failReason, false))
            {
                string suffix = failReason.NullOrEmpty() ? string.Empty : " (" + failReason + ")";
                yield return new FloatMenuOption(FloatMenuOptionLabel(myPawn) + suffix, null);
                yield break;
            }

            yield return new FloatMenuOption(FloatMenuOptionLabel(myPawn), delegate
            {
                if (!TryValidateUse(myPawn, out circle, out failReason, true))
                {
                    if (!failReason.NullOrEmpty())
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    }

                    return;
                }

                foreach (CompUseEffect comp in parent.AllComps.OfType<CompUseEffect>())
                {
                    if (comp.SelectedUseOption(myPawn))
                    {
                        return;
                    }
                }

                StartValidatedUseJob(myPawn, new LocalTargetInfo(circle));
            });
        }

        private void StartValidatedUseJob(Pawn pawn, LocalTargetInfo extraTarget)
        {
            if (!TryValidateUse(pawn, out Building_AbyssalSummoningCircle circle, out string failReason, true, extraTarget))
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            StringBuilder confirmBuilder = new StringBuilder();
            foreach (CompUseEffect comp in parent.AllComps.OfType<CompUseEffect>())
            {
                TaggedString tagged = comp.ConfirmMessage(pawn);
                if (!tagged.NullOrEmpty())
                {
                    if (confirmBuilder.Length > 0)
                    {
                        confirmBuilder.AppendLine();
                    }

                    confirmBuilder.AppendTagged(tagged);
                }
            }

            void StartJob()
            {
                Job job = JobMaker.MakeJob(Props.useJob, parent, circle);
                job.count = 1;
                pawn.jobs.TryTakeOrderedJob(job);
            }

            string confirmText = confirmBuilder.ToString();
            if (confirmText.NullOrEmpty())
            {
                StartJob();
            }
            else
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(confirmText, StartJob));
            }
        }

        private bool TryValidateUse(
            Pawn pawn,
            out Building_AbyssalSummoningCircle circle,
            out string failReason,
            bool includeReservationChecks,
            LocalTargetInfo preferredCircleTarget = default)
        {
            circle = null;
            failReason = null;

            if (pawn == null)
            {
                failReason = "No pawn available.";
                return false;
            }

            if (!CanBeUsedByEffects(pawn, out failReason))
            {
                return false;
            }

            if (!pawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
            {
                failReason = "NoPath".Translate();
                return false;
            }

            if (includeReservationChecks && !pawn.CanReserve(parent))
            {
                failReason = "Reserved".Translate();
                return false;
            }

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                failReason = "Incapable".Translate();
                return false;
            }

            circle = ResolvePreferredCircle(pawn, preferredCircleTarget);
            if (circle == null)
            {
                if (!AbyssalBossSummonUtility.TryFindNearestAvailableCircle(
                        pawn.MapHeld,
                        pawn.PositionHeld,
                        out circle,
                        out failReason))
                {
                    return false;
                }
            }

            if (!pawn.CanReach(circle, PathEndMode.InteractionCell, Danger.Deadly))
            {
                failReason = "NoPath".Translate();
                return false;
            }

            if (includeReservationChecks && !pawn.CanReserve(circle))
            {
                failReason = "Reserved".Translate();
                return false;
            }

            return true;
        }

        private bool CanBeUsedByEffects(Pawn pawn, out string failReason)
        {
            List<ThingComp> comps = parent.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is CompUseEffect useEffect && !useEffect.CanBeUsedBy(pawn))
                {
                    failReason = "Cannot use now.";
                    return false;
                }
            }

            failReason = null;
            return true;
        }

        private Building_AbyssalSummoningCircle ResolvePreferredCircle(Pawn pawn, LocalTargetInfo preferredCircleTarget)
        {
            Building_AbyssalSummoningCircle circle = preferredCircleTarget.Thing as Building_AbyssalSummoningCircle;
            if (IsValidCircleForPawn(pawn, circle))
            {
                return circle;
            }

            if (pawn?.CurJob != null)
            {
                circle = pawn.CurJob.GetTarget(TargetIndex.B).Thing as Building_AbyssalSummoningCircle;
                if (IsValidCircleForPawn(pawn, circle))
                {
                    return circle;
                }
            }

            return null;
        }

        private bool IsValidCircleForPawn(Pawn pawn, Building_AbyssalSummoningCircle circle)
        {
            return circle != null
                && !circle.Destroyed
                && circle.Spawned
                && circle.MapHeld == pawn?.MapHeld
                && !circle.RitualActive
                && circle.IsPoweredForRitual;
        }
    }
}
