using RimWorld;
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
            if (pawn == null || pawn.Map == null || pawn.Dead)
            {
                return;
            }

            if (AbyssalThreatPawnUtility.GetController(pawn) != null)
            {
                return;
            }

            AbyssalThreatPawnUtility.EnsureHostileFaction(pawn);
            Faction playerFaction = Faction.OfPlayer;
            if (pawn.Faction == null || playerFaction == null || !pawn.Faction.HostileTo(playerFaction))
            {
                return;
            }

            if (!pawn.Spawned || !pawn.Map.IsPlayerHome)
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (spawnTick < 0)
            {
                spawnTick = ticksGame;
            }

            if (ticksGame - spawnTick < SpawnGraceTicks)
            {
                return;
            }

            if (AbyssalLordUtility.FindLordFor(pawn) != null)
            {
                return;
            }

            if (ticksGame - lastAggroTick < LordRetryTicks)
            {
                return;
            }

            lastAggroTick = ticksGame;
            AbyssalThreatPawnUtility.EnsureAssaultLordForPawn(pawn, sappers: true);
        }
    }
}
