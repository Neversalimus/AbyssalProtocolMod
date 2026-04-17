using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompAbyssalPawnController : ThingComp
    {
        private int lastLordRetryTick = -99999;
        private int spawnTick = -1;

        public CompProperties_AbyssalPawnController Props => (CompProperties_AbyssalPawnController)props;

        public static CompAbyssalPawnController GetFor(Pawn pawn)
        {
            return pawn?.GetComp<CompAbyssalPawnController>();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastLordRetryTick, "lastLordRetryTick", -99999);
            Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            spawnTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            ConfigurePawn();
            TryEnsureHostilityAndLord(skipSpawnGrace: true);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            ConfigurePawn();
            TryEnsureHostilityAndLord(skipSpawnGrace: false);
        }

        private void ConfigurePawn()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            AbyssalThreatPawnUtility.PrepareThreatPawn(pawn, this);
        }

        private void TryEnsureHostilityAndLord(bool skipSpawnGrace)
        {
            if (!Props.autoHostile)
            {
                return;
            }

            Pawn pawn = parent as Pawn;
            if (pawn == null || pawn.Map == null || pawn.Dead)
            {
                return;
            }

            AbyssalThreatPawnUtility.EnsureHostileFaction(pawn);
            if (!Props.assignAssaultLord)
            {
                return;
            }

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

            if (!skipSpawnGrace && ticksGame - spawnTick < Props.spawnGraceTicks)
            {
                return;
            }

            if (AbyssalLordUtility.FindLordFor(pawn) != null)
            {
                return;
            }

            if (ticksGame - lastLordRetryTick < Props.lordRetryTicks)
            {
                return;
            }

            lastLordRetryTick = ticksGame;
            AbyssalThreatPawnUtility.EnsureAssaultLordForPawn(pawn, Props.useSapperLord);
        }
    }
}
