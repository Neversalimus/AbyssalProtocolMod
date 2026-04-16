using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class CompAbyssalAutoHostile : ThingComp
    {
        private int spawnedAtTick = -1;
        private int lastAggroTick = -99999;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            spawnedAtTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            TryEnsureHostility();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            TryEnsureHostility();
        }

        private void TryEnsureHostility()
        {
            if (!(parent is Pawn pawn) || pawn.Map == null || pawn.Dead)
            {
                return;
            }

            AbyssalThreatPawnUtility.PrepareThreatPawn(pawn);

            if (pawn.Faction == null)
            {
                Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
                if (hostileFaction != null)
                {
                    pawn.SetFaction(hostileFaction);
                }
            }

            Faction playerFaction = Faction.OfPlayer;
            if (pawn.Faction == null || playerFaction == null || !pawn.Faction.HostileTo(playerFaction))
            {
                return;
            }

            if (AbyssalThreatPawnUtility.GetCurrentLord(pawn) != null || !pawn.Spawned || !pawn.Map.IsPlayerHome)
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (spawnedAtTick >= 0 && ticksGame - spawnedAtTick < 60)
            {
                return;
            }

            if (ticksGame - lastAggroTick < 120)
            {
                return;
            }

            lastAggroTick = ticksGame;
            LordJob lordJob = new LordJob_AssaultColony(
                pawn.Faction,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: true,
                useAvoidGridSmart: true,
                canSteal: false);
            LordMaker.MakeNewLord(pawn.Faction, lordJob, pawn.Map, new List<Pawn> { pawn });
        }
    }
}
