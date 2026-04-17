using Verse;

namespace AbyssalProtocol
{
    public class CompAbyssalPawnController : ThingComp
    {
        private int lastAggroTick = -99999;
        private int spawnTick = -1;

        public CompProperties_AbyssalPawnController Props => (CompProperties_AbyssalPawnController)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastAggroTick, "lastAggroTick", -99999);
            Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            spawnTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            EnsurePreparedState();
            EnsureAggressionState();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            EnsurePreparedState();
            EnsureAggressionState();
        }

        private void EnsurePreparedState()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null)
            {
                return;
            }

            AbyssalThreatPawnUtility.PrepareThreatPawn(pawn, Props);
        }

        private void EnsureAggressionState()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null)
            {
                return;
            }

            AbyssalThreatPawnUtility.TryEnsureHostileAggression(
                pawn,
                Props.useSapperAssaultLord,
                Props.spawnGraceTicks,
                Props.lordRetryTicks,
                ref spawnTick,
                ref lastAggroTick);
        }
    }
}
