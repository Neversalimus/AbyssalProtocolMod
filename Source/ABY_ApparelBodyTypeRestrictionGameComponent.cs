using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_ApparelBodyTypeRestrictionGameComponent : GameComponent
    {
        private const int InitialDelayTicks = 120;
        private const int CheckIntervalTicks = 251;

        private int nextCheckTick = -1;

        public ABY_ApparelBodyTypeRestrictionGameComponent(Game game)
        {
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            ScheduleInitialCheck();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            ScheduleInitialCheck();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextCheckTick, "abyBodyArmorBodyTypeRestrictionNextCheckTick", -1);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (nextCheckTick < 0)
            {
                nextCheckTick = ticksGame + InitialDelayTicks;
            }

            if (ticksGame < nextCheckTick)
            {
                return;
            }

            nextCheckTick = ticksGame + CheckIntervalTicks;
            EnforceSpawnedPawns();
        }

        private void ScheduleInitialCheck()
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextCheckTick = ticksGame + InitialDelayTicks;
        }

        private static void EnforceSpawnedPawns()
        {
            List<Map> maps = Find.Maps;
            if (maps == null)
            {
                return;
            }

            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                IReadOnlyList<Pawn> pawns = map?.mapPawns?.AllPawnsSpawned;
                if (pawns == null || pawns.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < pawns.Count; j++)
                {
                    ABY_ApparelBodyTypeRestrictionUtility.TryRemoveIncompatibleWornApparel(pawns[j], true);
                }
            }
        }
    }
}
