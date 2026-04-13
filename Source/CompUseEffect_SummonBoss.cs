using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompUseEffect_SummonBoss : CompUseEffect
    {
        public CompProperties_UseEffectSummonBoss Props => (CompProperties_UseEffectSummonBoss)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            Map map = usedBy?.MapHeld;
            if (map == null)
            {
                Messages.Message("ABY_BossSummonFail_NoMap".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Building_AbyssalSummoningCircle circle = TryGetPreferredCircle(usedBy);
            if (circle == null)
            {
                if (!AbyssalBossSummonUtility.TryFindNearestAvailableCircle(
                        map,
                        usedBy.PositionHeld,
                        out circle,
                        out string findFailReason))
                {
                    Messages.Message(findFailReason, MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }

            if (!circle.TryStartBossSummonSequence(usedBy, Props, out string startFailReason))
            {
                Messages.Message(startFailReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            ConsumeOneUse();

            Messages.Message(
                "ABY_SigilActivationStarted".Translate(),
                MessageTypeDefOf.PositiveEvent,
                false);
        }

        private Building_AbyssalSummoningCircle TryGetPreferredCircle(Pawn usedBy)
        {
            if (usedBy?.CurJob == null)
            {
                return null;
            }

            LocalTargetInfo targetB = usedBy.CurJob.GetTarget(TargetIndex.B);
            Building_AbyssalSummoningCircle circle = targetB.Thing as Building_AbyssalSummoningCircle;
            if (circle == null || circle.Destroyed || !circle.Spawned || circle.MapHeld != usedBy.MapHeld)
            {
                return null;
            }

            if (!circle.IsReadyForSigil(out _))
            {
                return null;
            }

            return circle;
        }

        private void ConsumeOneUse()
        {
            if (parent == null || parent.Destroyed)
            {
                return;
            }

            if (parent.stackCount > 1)
            {
                Thing one = parent.SplitOff(1);
                one?.Destroy();
            }
            else
            {
                parent.Destroy();
            }
        }
    }
}
