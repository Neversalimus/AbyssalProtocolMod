using System;
using Verse;

namespace AbyssalProtocol
{
    public sealed class MapComponent_ABY_DominionAtmosphere : MapComponent
    {
        private const int ScanIntervalTicks = 600;
        private const int MaintenanceIntervalTicks = 1800;
        private const int MaintenanceChunkIntervalTicks = 30;
        private const int MaintenanceCellsPerRun = 2200;
        private const int AmbientIntervalMinTicks = 1250;
        private const int AmbientIntervalMaxTicks = 2600;
        private const int WeatherIntervalMinTicks = 80;
        private const int WeatherIntervalMaxTicks = 150;
        private const int ReducedWeatherIntervalMinTicks = 520;
        private const int ReducedWeatherIntervalMaxTicks = 900;
        private const int WeatherStateMinTicks = 5200;
        private const int WeatherStateMaxTicks = 9400;

        private bool markedAsDominionSlice;
        private int nextScanTick;
        private int nextMaintenanceTick;
        private int nextAmbientPulseTick;
        private int nextWeatherTick;
        private int nextWeatherStateChangeTick;
        private int dominionWeatherStateInt;
        private int maintenanceRuns;
        private int maintenanceCellIndex;
        private int ambientPulses;
        private int weatherBursts;

        public MapComponent_ABY_DominionAtmosphere(Map map) : base(map)
        {
            ScheduleInitialTicks();
        }

        public bool MarkedAsDominionSlice => markedAsDominionSlice;

        public ABY_DominionWeatherState CurrentWeatherState
        {
            get
            {
                if (dominionWeatherStateInt < 0 || dominionWeatherStateInt > (int)ABY_DominionWeatherState.FurnaceDrift)
                {
                    dominionWeatherStateInt = (int)ABY_DominionWeatherState.Ashfall;
                }

                return (ABY_DominionWeatherState)dominionWeatherStateInt;
            }
        }

        public void MarkDominionSlice(ABY_DominionPocketSession session = null, string source = null)
        {
            markedAsDominionSlice = true;
            int now = CurrentTick;
            if (nextMaintenanceTick <= now)
            {
                nextMaintenanceTick = now + Rand.Range(90, 210);
            }

            if (nextAmbientPulseTick <= now)
            {
                nextAmbientPulseTick = now + Rand.Range(320, 720);
            }

            if (nextWeatherTick <= now)
            {
                nextWeatherTick = now + Rand.Range(30, 90);
            }

            if (nextWeatherStateChangeTick <= now)
            {
                ChooseNextWeatherState(now, true);
            }

            // Make the weather layer visible immediately after the pocket map is prepared.
            // Previously the first burst could be delayed enough that it looked disabled.
            if (AbyssalProtocolMod.Settings != null && AbyssalProtocolMod.Settings.enableDominionWeather)
            {
                try
                {
                    ABY_DominionWeatherUtility.EmitWeatherBurst(map, CurrentWeatherState, AbyssalProtocolMod.Settings.ResolveDominionWeatherIntensity(), AbyssalProtocolMod.Settings.reducedMotion);
                }
                catch
                {
                }
            }

            if (AbyssalProtocolMod.Settings?.verboseDiagnostics ?? false)
            {
                string sessionText = session != null ? session.sessionId : "none";
                ABY_LogThrottleUtility.Message("dominion-atmosphere-mark-" + map.uniqueID, "[Abyssal Protocol] Dominion atmosphere controller marked map " + map.uniqueID + " from " + (source ?? "unknown") + ", session=" + sessionText + ", weather=" + CurrentWeatherState + ".", 2500);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref markedAsDominionSlice, "ABY_markedAsDominionSlice", false);
            Scribe_Values.Look(ref nextScanTick, "ABY_nextAtmosphereScanTick", 0);
            Scribe_Values.Look(ref nextMaintenanceTick, "ABY_nextAtmosphereMaintenanceTick", 0);
            Scribe_Values.Look(ref nextAmbientPulseTick, "ABY_nextAmbientPulseTick", 0);
            Scribe_Values.Look(ref nextWeatherTick, "ABY_nextDominionWeatherTick", 0);
            Scribe_Values.Look(ref nextWeatherStateChangeTick, "ABY_nextDominionWeatherStateChangeTick", 0);
            Scribe_Values.Look(ref dominionWeatherStateInt, "ABY_dominionWeatherState", (int)ABY_DominionWeatherState.Ashfall);
            Scribe_Values.Look(ref maintenanceRuns, "ABY_atmosphereMaintenanceRuns", 0);
            Scribe_Values.Look(ref maintenanceCellIndex, "ABY_atmosphereMaintenanceCellIndex", 0);
            Scribe_Values.Look(ref ambientPulses, "ABY_atmosphereAmbientPulses", 0);
            Scribe_Values.Look(ref weatherBursts, "ABY_dominionWeatherBursts", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ScheduleInitialTicks();
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (map == null)
            {
                return;
            }

            int now = CurrentTick;
            if (!markedAsDominionSlice)
            {
                if (now < nextScanTick)
                {
                    return;
                }

                nextScanTick = now + ScanIntervalTicks;
                if (!ABY_DominionAtmosphereUtility.IsDominionPocketMap(map))
                {
                    return;
                }

                MarkDominionSlice(null, "runtime-detect");
            }

            if (now >= nextMaintenanceTick)
            {
                RunDominionMaintenance();
            }

            if (now >= nextAmbientPulseTick)
            {
                TryRunAmbientPulse();
                nextAmbientPulseTick = now + Rand.Range(AmbientIntervalMinTicks, AmbientIntervalMaxTicks);
            }

            if (now >= nextWeatherStateChangeTick)
            {
                ChooseNextWeatherState(now, false);
            }

            if (now >= nextWeatherTick)
            {
                TryRunDominionWeather();
                nextWeatherTick = now + ResolveWeatherInterval();
            }
        }

        private void RunDominionMaintenance()
        {
            if (!markedAsDominionSlice || map == null)
            {
                return;
            }

            try
            {
                int width = map.Size.x;
                int height = map.Size.z;
                int totalCells = Math.Max(0, width * height);
                if (totalCells <= 0)
                {
                    nextMaintenanceTick = CurrentTick + MaintenanceIntervalTicks;
                    return;
                }

                if (maintenanceCellIndex < 0 || maintenanceCellIndex >= totalCells)
                {
                    maintenanceCellIndex = 0;
                }

                TerrainDef baseTerrain = ABY_DominionAtmosphereUtility.ResolveDominionBaseTerrain();
                int processed = 0;
                while (processed < MaintenanceCellsPerRun && maintenanceCellIndex < totalCells)
                {
                    int index = maintenanceCellIndex++;
                    processed++;

                    IntVec3 cell = new IntVec3(index % width, 0, index / width);
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    if (map.snowGrid != null)
                    {
                        map.snowGrid.SetDepth(cell, 0f);
                    }

                    if (map.roofGrid != null && map.roofGrid.RoofAt(cell) != null)
                    {
                        map.roofGrid.SetRoof(cell, null);
                    }

                    if (map.fogGrid != null && map.fogGrid.IsFogged(cell))
                    {
                        map.fogGrid.Unfog(cell);
                    }

                    TerrainDef terrain = map.terrainGrid?.TerrainAt(cell);
                    if (terrain != null && terrain.IsWater && baseTerrain != null)
                    {
                        map.terrainGrid.SetTerrain(cell, baseTerrain);
                    }
                }

                if (maintenanceCellIndex >= totalCells)
                {
                    maintenanceCellIndex = 0;
                    maintenanceRuns++;
                    nextMaintenanceTick = CurrentTick + MaintenanceIntervalTicks;
                    if (AbyssalProtocolMod.Settings?.verboseDiagnostics ?? false)
                    {
                        ABY_LogThrottleUtility.Message("dominion-atmosphere-maintenance-" + map.uniqueID, "[Abyssal Protocol] Dominion atmosphere maintenance completed on map " + map.uniqueID + " (runs=" + maintenanceRuns + ").", 5000);
                    }
                }
                else
                {
                    nextMaintenanceTick = CurrentTick + MaintenanceChunkIntervalTicks;
                }
            }
            catch (Exception ex)
            {
                maintenanceCellIndex = 0;
                nextMaintenanceTick = CurrentTick + MaintenanceIntervalTicks;
                ABY_LogThrottleUtility.Warning("dominion-atmosphere-maintenance", "[Abyssal Protocol] Dominion atmosphere maintenance failed: " + ex.GetType().Name + ": " + ex.Message, 5000);
            }
        }

        private void TryRunAmbientPulse()
        {
            if (!markedAsDominionSlice || map == null)
            {
                return;
            }

            AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
            if (settings != null && (!ABY_PerformanceSettingsUtility.ShouldRunDominionAmbientVfx(settings) || settings.reducedMotion))
            {
                return;
            }

            if (!ABY_VfxBudget.TrySpend(map, ABY_VfxBudgetCategory.DominionAmbient, 3))
            {
                return;
            }

            ABY_DominionAtmosphereUtility.ThrowQuietAtmospherePulse(map);
            ambientPulses++;
        }

        private void TryRunDominionWeather()
        {
            if (!markedAsDominionSlice || map == null)
            {
                return;
            }

            AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
            if (settings == null || !settings.enableDominionWeather)
            {
                return;
            }

            try
            {
                float intensity = settings.ResolveDominionWeatherIntensity();
                bool reduced = settings.reducedMotion || settings.visualIntensity != ABY_VisualIntensity.Full;
                ABY_DominionWeatherUtility.EmitWeatherBurst(map, CurrentWeatherState, intensity, reduced);
                weatherBursts++;

                if (settings.verboseDiagnostics && weatherBursts % 16 == 1)
                {
                    ABY_LogThrottleUtility.Message("dominion-weather-" + map.uniqueID, "[Abyssal Protocol] Dominion weather burst " + weatherBursts + " on map " + map.uniqueID + " state=" + CurrentWeatherState + " intensity=" + intensity.ToString("F2") + ".", 3500);
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("dominion-weather-burst", "[Abyssal Protocol] Dominion weather burst skipped: " + ex.GetType().Name + ": " + ex.Message, 5000);
            }
        }

        private int ResolveWeatherInterval()
        {
            AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
            if (settings == null || settings.reducedMotion)
            {
                return Rand.Range(ReducedWeatherIntervalMinTicks, ReducedWeatherIntervalMaxTicks);
            }

            float intensity = settings.ResolveDominionWeatherIntensity();
            float min = WeatherIntervalMinTicks / Math.Max(0.65f, intensity);
            float max = WeatherIntervalMaxTicks / Math.Max(0.65f, intensity);
            int interval = Rand.Range(Math.Max(80, (int)min), Math.Max(120, (int)max));
            return ABY_PerformanceSettingsUtility.ScaleVfxInterval(interval, settings);
        }

        private void ChooseNextWeatherState(int now, bool initial)
        {
            ABY_DominionWeatherState oldState = CurrentWeatherState;
            ABY_DominionWeatherState nextState;
            if (initial)
            {
                nextState = Rand.Chance(0.58f) ? ABY_DominionWeatherState.Ashfall : (Rand.Chance(0.5f) ? ABY_DominionWeatherState.StaticVeil : ABY_DominionWeatherState.FurnaceDrift);
            }
            else
            {
                nextState = oldState;
                for (int i = 0; i < 5 && nextState == oldState; i++)
                {
                    nextState = (ABY_DominionWeatherState)Rand.RangeInclusive(0, (int)ABY_DominionWeatherState.FurnaceDrift);
                }
            }

            dominionWeatherStateInt = (int)nextState;
            nextWeatherStateChangeTick = now + Rand.Range(WeatherStateMinTicks, WeatherStateMaxTicks);

            if (markedAsDominionSlice && (AbyssalProtocolMod.Settings?.verboseDiagnostics ?? false))
            {
                ABY_LogThrottleUtility.Message("dominion-weather-state-" + map.uniqueID, "[Abyssal Protocol] Dominion weather state changed to " + nextState + " on map " + map.uniqueID + ".", 3500);
            }
        }

        private void ScheduleInitialTicks()
        {
            int now = CurrentTick;
            nextScanTick = now + Rand.Range(120, 420);
            if (nextMaintenanceTick <= now)
            {
                nextMaintenanceTick = now + Rand.Range(300, 900);
            }

            if (nextAmbientPulseTick <= now)
            {
                nextAmbientPulseTick = now + Rand.Range(AmbientIntervalMinTicks, AmbientIntervalMaxTicks);
            }

            if (nextWeatherTick <= now)
            {
                nextWeatherTick = now + Rand.Range(40, 100);
            }

            if (nextWeatherStateChangeTick <= now)
            {
                nextWeatherStateChangeTick = now + Rand.Range(WeatherStateMinTicks, WeatherStateMaxTicks);
            }

            if (dominionWeatherStateInt < 0 || dominionWeatherStateInt > (int)ABY_DominionWeatherState.FurnaceDrift)
            {
                dominionWeatherStateInt = (int)ABY_DominionWeatherState.Ashfall;
            }
        }

        private static int CurrentTick => Find.TickManager != null ? Find.TickManager.TicksGame : 0;
    }
}
