using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Package 12: reduces external relation/thought crashes during pocket-map removal by removing temporary
    /// non-player abyssal pawns from sterile pocket maps after the strike team has left.
    /// </summary>
    public class MapComponent_ABY_DominionPocketDeinitGuard : MapComponent
    {
        private const int GuardIntervalTicks = 240;
        private int nextGuardTick;

        public MapComponent_ABY_DominionPocketDeinitGuard(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextGuardTick, "abyDominionDeinitGuard_nextTick", 0);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null || Find.TickManager == null || !MapComponent_ABY_SterileAbyssalMap.IsSterile(map))
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (now < nextGuardTick)
            {
                return;
            }

            nextGuardTick = now + GuardIntervalTicks;
            TryRemoveTemporaryHostilesIfStrikeTeamGone();
        }

        private void TryRemoveTemporaryHostilesIfStrikeTeamGone()
        {
            if (map?.mapPawns == null)
            {
                return;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }

            bool hasPlayerPawn = false;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && !pawn.Dead && pawn.IsColonistPlayerControlled)
                {
                    hasPlayerPawn = true;
                    break;
                }
            }

            if (hasPlayerPawn)
            {
                return;
            }

            List<Pawn> toRemove = new List<Pawn>();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Destroyed || pawn.IsColonistPlayerControlled)
                {
                    continue;
                }

                if (ABY_AntiTameUtility.IsAbyssalPawn(pawn))
                {
                    toRemove.Add(pawn);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                Pawn pawn = toRemove[i];
                try
                {
                    if (pawn.Spawned)
                    {
                        pawn.DeSpawn(DestroyMode.Vanish);
                    }

                    if (!pawn.Destroyed)
                    {
                        pawn.Destroy(DestroyMode.Vanish);
                    }
                }
                catch
                {
                }
            }
        }
    }
}
