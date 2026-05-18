using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_ABY_HordeCompletionWatchdog : MapComponent
    {
        private const int WatchdogIntervalTicks = 120;
        private int nextWatchdogTick;

        public MapComponent_ABY_HordeCompletionWatchdog(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextWatchdogTick, "abyHordeWatchdog_nextTick", 0);
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
            TryResolveStuckHordeWave();
        }

        private void TryResolveStuckHordeWave()
        {
            MapComponent_AbyssalPortalWave portalWave = map.GetComponent<MapComponent_AbyssalPortalWave>();
            if (portalWave == null || !portalWave.IsWaveActive)
            {
                return;
            }

            portalWave.TryForceCompleteStaleHorde(true, "watchdog found no command gate, active horde portals, or combat-capable abyssal pawns");
        }
    }
}
