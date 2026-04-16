using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class CompAbyssalAutoHostile : ThingComp
    {
        private int lastAggroTick = -99999;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
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

            if (pawn.GetLord() != null || !pawn.Spawned || !pawn.Map.IsPlayerHome)
            {
                return;
            }

            if (Find.TickManager != null && Find.TickManager.TicksGame - lastAggroTick < 120)
            {
                return;
            }

            lastAggroTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
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
