using RimWorld;
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
            if (!AbyssalThreatPawnUtility.CanOperateHostilePawn(pawn))
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

            CompProperties_AbyssalPawnController controller = AbyssalThreatPawnUtility.GetControllerProperties(pawn);
            bool preferRangedTargets = controller == null || controller.preferRangedTargets;
            bool preferLowHealthTargets = controller == null || controller.preferLowHealthTargets;

            Pawn target = AbyssalThreatPawnUtility.FindBestHostilePawnTarget(
                pawn,
                Props.minRange,
                Props.maxRange,
                requireRanged: preferRangedTargets,
                preferRangedTargets: preferRangedTargets,
                preferLowHealthTargets: preferLowHealthTargets,
                preferFarthestTargets: false,
                rangedTargetBonus: 2.4f,
                lowHealthWeight: 2.0f)
                ?? AbyssalThreatPawnUtility.FindBestHostilePawnTarget(
                    pawn,
                    Props.minRange,
                    Props.maxRange,
                    requireRanged: false,
                    preferRangedTargets: preferRangedTargets,
                    preferLowHealthTargets: preferLowHealthTargets,
                    preferFarthestTargets: false,
                    rangedTargetBonus: 2.4f,
                    lowHealthWeight: 2.0f);

            if (target == null)
            {
                return;
            }

            if (!AbyssalThreatPawnUtility.TryFindAdjacentLandingCell(pawn, target, out IntVec3 landingCell))
            {
                return;
            }

            DoPounce(pawn, target, landingCell);
            nextPounceTick = currentTick
                + Mathf.Max(60, Props.cooldownTicks)
                + Rand.RangeInclusive(-Mathf.Max(0, Props.cooldownJitterTicks), Mathf.Max(0, Props.cooldownJitterTicks));
        }

        private void DoPounce(Pawn pawn, Pawn target, IntVec3 landingCell)
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
            AbyssalThreatPawnUtility.ApplyOrRefreshHediff(target, Props.impactHediffDefName, 0f);
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

            MoteMaker.MakeStaticMote(cell.ToVector3Shifted(), map, moteDef, 0.82f);
        }
    }
}
