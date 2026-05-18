using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompEmberPounce : ThingComp
    {
        private int nextPounceTick;

        public CompProperties_EmberPounce Props => (CompProperties_EmberPounce)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextPounceTick, "nextPounceTick");
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead || pawn.Downed)
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
            if (currentTick < nextPounceTick)
            {
                return;
            }

            Pawn target = AbyssalThreatPawnUtility.FindBestTarget(pawn, Props.minRange, Props.maxRange, false, true, true, 2.4f, 2.0f)
                ?? AbyssalThreatPawnUtility.FindBestTarget(pawn, Props.minRange, Props.maxRange, false, false, false, 0f, 2.0f);
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
                "ember_pounce"))
            {
                nextPounceTick = currentTick + Mathf.Max(60, Props.cooldownTicks) + Rand.RangeInclusive(-Mathf.Max(0, Props.cooldownJitterTicks), Mathf.Max(0, Props.cooldownJitterTicks));
            }
        }
    }
}
