using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompAbyssalAutoHostile : ThingComp
    {
        private const int SpawnGraceTicks = 120;
        private int lastAggroTick = -99999;
        private int spawnTick = -99999;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            spawnTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            EnsureHostileFaction();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastAggroTick, "lastAggroTick", -99999);
            Scribe_Values.Look(ref spawnTick, "spawnTick", -99999);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            TryEnsureHostility();
        }

        private void EnsureHostileFaction()
        {
            if (!(parent is Pawn pawn) || pawn.Faction != null)
            {
                return;
            }

            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            if (hostileFaction != null)
            {
                pawn.SetFaction(hostileFaction);
            }
        }

        private void TryEnsureHostility()
        {
            if (!(parent is Pawn pawn) || pawn.Map == null || pawn.Dead)
            {
                return;
            }

            EnsureHostileFaction();

            Faction playerFaction = Faction.OfPlayer;
            if (pawn.Faction == null || playerFaction == null || !pawn.Faction.HostileTo(playerFaction))
            {
                return;
            }

            if (!pawn.Spawned || !pawn.Map.IsPlayerHome || AbyssalLordUtility.FindLord(pawn) != null)
            {
                return;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (currentTick - spawnTick < SpawnGraceTicks)
            {
                return;
            }

            if (currentTick - lastAggroTick < 120)
            {
                return;
            }

            lastAggroTick = currentTick;
            AbyssalLordUtility.EnsureAssaultLord(pawn, sappers: true);
        }
    }
}
