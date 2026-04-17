using Verse;

namespace AbyssalProtocol
{
    public class CompAbyssalAutoHostile : ThingComp
    {
        private const int SpawnGraceTicks = 90;
        private const int LordRetryTicks = 120;

        private int lastAggroTick = -99999;
        private int spawnTick = -1;

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
            AbyssalThreatPawnUtility.EnsureHostilityAndLord(
                pawn,
                true,
                true,
                spawnTick,
                ref lastAggroTick,
                SpawnGraceTicks,
                LordRetryTicks);
        }
    }
}
