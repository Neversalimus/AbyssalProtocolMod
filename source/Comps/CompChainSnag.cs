using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompChainSnag : ThingComp
    {
        private int nextSnagTick;

        public CompProperties_ChainSnag Props => (CompProperties_ChainSnag)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextSnagTick, "nextSnagTick");
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed || pawn.stances == null)
            {
                return;
            }

            if (ABY_AbyssalDashRuntime.IsDashing(pawn))
            {
                return;
            }

            if (!parent.IsHashIntervalTick(Mathf.Max(15, Props.scanIntervalTicks)))
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick < nextSnagTick)
            {
                return;
            }

            Pawn target = AbyssalThreatPawnUtility.FindBestTarget(pawn, Props.minRange, Props.maxRange, false, true, true, 2.6f, 1.8f)
                ?? AbyssalThreatPawnUtility.FindBestTarget(pawn, Props.minRange, Props.maxRange, false, false, false, 0f, 1.8f);
            if (target == null)
            {
                return;
            }

            if (!AbyssalThreatPawnUtility.TryFindAdjacentLandingCell(pawn, target, out IntVec3 landingCell))
            {
                return;
            }

            if (ABY_AbyssalDashRuntime.TryStartDash(
                pawn,
                target,
                landingCell,
                Props.impactHediffDefName,
                Props.dashDurationTicks,
                Props.dashMoteDefName,
                Props.dashMoteScale,
                Props.dashSoundDefName,
                "chain_snag"))
            {
                nextSnagTick = currentTick + Mathf.Max(60, Props.cooldownTicks) + Rand.RangeInclusive(-Mathf.Max(0, Props.cooldownJitterTicks), Mathf.Max(0, Props.cooldownJitterTicks));
            }
        }
    }
}
