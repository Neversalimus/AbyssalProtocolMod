using RimWorld;
using Verse;

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

            if (!AbyssalBossSummonUtility.TryFindNearestAvailableCircle(
                    map,
                    usedBy.PositionHeld,
                    out Building_AbyssalSummoningCircle circle,
                    out string findFailReason))
            {
                Messages.Message(findFailReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!circle.TryStartBossSummonSequence(usedBy, Props, out string startFailReason))
            {
                Messages.Message(startFailReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            ConsumeOneUse();

            Messages.Message(
                "The abyssal circle begins to awaken.",
                MessageTypeDefOf.PositiveEvent,
                false);
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
