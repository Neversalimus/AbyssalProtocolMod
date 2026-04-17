using Verse;

namespace AbyssalProtocol
{
    public class CompAbyssalAutoHostile : ThingComp
    {
        private const int DefaultSpawnGraceTicks = 90;
        private const int DefaultLordRetryTicks = 120;

        private int lastAggroTick = -99999;
        private int spawnTick = -1;

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
            TryEnsureHostility();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            TryEnsureHostility();
        }

        private void TryEnsureHostility()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null)
            {
                return;
            }

            CompProperties_AbyssalPawnController controllerProps = AbyssalThreatPawnUtility.GetControllerProps(pawn);
            bool useSappers = controllerProps?.useSapperAssaultLord ?? true;
            int spawnGraceTicks = controllerProps?.spawnGraceTicks ?? DefaultSpawnGraceTicks;
            int lordRetryTicks = controllerProps?.lordRetryTicks ?? DefaultLordRetryTicks;

            AbyssalThreatPawnUtility.TryEnsureHostileAggression(
                pawn,
                useSappers,
                spawnGraceTicks,
                lordRetryTicks,
                ref spawnTick,
                ref lastAggroTick);
        }
    }
}
