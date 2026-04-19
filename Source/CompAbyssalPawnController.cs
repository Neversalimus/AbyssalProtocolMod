using Verse;

namespace AbyssalProtocol
{
    public class CompAbyssalPawnController : ThingComp
    {
        private int lastAggroTick = -99999;
        private int spawnTick = -1;
        private bool prepared;

        public CompProperties_AbyssalPawnController Props => (CompProperties_AbyssalPawnController)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastAggroTick, "lastAggroTick", -99999);
            Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
            Scribe_Values.Look(ref prepared, "prepared", false);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            spawnTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            prepared = false;
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
            if (prepared)
            {
                return;
            }

            Pawn pawn = parent as Pawn;
            if (pawn == null || pawn.Dead || pawn.Destroyed || pawn.Corpse != null)
            {
                return;
            }

            AbyssalThreatPawnUtility.PrepareThreatPawn(pawn, Props);
            prepared = true;
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
