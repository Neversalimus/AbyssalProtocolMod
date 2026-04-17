using RimWorld;
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

            DoSnag(pawn, target, landingCell);
            nextSnagTick = currentTick + Mathf.Max(60, Props.cooldownTicks) + Rand.RangeInclusive(-Mathf.Max(0, Props.cooldownJitterTicks), Mathf.Max(0, Props.cooldownJitterTicks));
        }

        private void DoSnag(Pawn pawn, Pawn target, IntVec3 landingCell)
        {
            Map map = pawn.Map;
            IntVec3 sourceCell = pawn.Position;
            SpawnMote(map, sourceCell);

            pawn.pather?.StopDead();
            pawn.Position = landingCell;
            pawn.Drawer?.tweener?.ResetTweenedPosToRoot();
            pawn.stances?.CancelBusyStanceSoft();
            pawn.rotationTracker?.FaceCell(target.Position);

            SpawnMote(map, landingCell);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", landingCell, map);
            AbyssalThreatPawnUtility.ApplyOrRefreshHediff(target, Props.impactHediffDefName);
        }

        private void SpawnMote(Map map, IntVec3 cell)
        {
            if (map == null || !cell.IsValid)
            {
                return;
            }

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_Mote_ArchonDashTrail");
            if (moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(cell.ToVector3Shifted(), map, moteDef, 0.9f);
        }
    }
}
