using System;
using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HarvesterEssenceEventUtility
    {
        public static void NotifyPawnKilled(Pawn deadPawn, Map map, IntVec3 deathCell)
        {
            if (deadPawn == null || map == null || !deathCell.IsValid)
            {
                return;
            }

            NotifyHarvesters(deadPawn, map, deathCell, -1);
        }

        public static void NotifyCorpseSpawned(Corpse corpse)
        {
            if (corpse?.InnerPawn == null || corpse.MapHeld == null || !corpse.PositionHeld.IsValid)
            {
                return;
            }

            NotifyHarvesters(corpse.InnerPawn, corpse.MapHeld, corpse.PositionHeld, corpse.thingIDNumber);
        }

        private static void NotifyHarvesters(Pawn deadPawn, Map map, IntVec3 focusCell, int corpseThingId)
        {
            IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn harvester = pawns[i];
                if (harvester == null || harvester == deadPawn || harvester.Dead || harvester.Downed || !harvester.Spawned)
                {
                    continue;
                }

                CompABY_HarvesterEssence comp = harvester.TryGetComp<CompABY_HarvesterEssence>();
                if (comp == null)
                {
                    continue;
                }

                try
                {
                    comp.NotifyNearbyAbyssalDeath(deadPawn, focusCell, map, corpseThingId);
                }
                catch (Exception ex)
                {
                    ABY_LogThrottleUtility.Warning("harvester-essence-event", "[Abyssal Protocol] Harvester essence event failed: " + ex.GetType().Name + ": " + ex.Message, 3000);
                }
            }
        }
    }
}
