using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_ABY_HordeHardStopWatchdog : MapComponent
    {
        private const int WatchdogIntervalTicks = 120;
        private int nextWatchdogTick;

        public MapComponent_ABY_HordeHardStopWatchdog(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextWatchdogTick, "abyHordeHardStopV2_nextTick", 0);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null || Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (now < nextWatchdogTick)
            {
                return;
            }

            nextWatchdogTick = now + WatchdogIntervalTicks;
            ABY_LargeModpackHotfixBUtility.TryHardStopStaleHorde(map, "watchdog");
        }
    }
}
