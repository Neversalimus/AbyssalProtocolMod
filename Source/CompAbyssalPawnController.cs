using Verse;

namespace AbyssalProtocol
{
    public class CompAbyssalPawnController : ThingComp
    {
        private int nextRefreshTick = -1;

        public CompProperties_AbyssalPawnController Props => (CompProperties_AbyssalPawnController)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            RefreshPawnNow();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextRefreshTick, "nextRefreshTick", -1);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (nextRefreshTick >= 0 && ticksGame < nextRefreshTick)
            {
                return;
            }

            RefreshPawnNow();
        }

        private void RefreshPawnNow()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null)
            {
                return;
            }

            AbyssalThreatPawnUtility.PrepareThreatPawn(pawn);
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextRefreshTick = ticksGame + (Props != null ? Props.refreshIntervalTicks : 250);
        }
    }
}
