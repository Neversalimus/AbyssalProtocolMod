using System;
using Verse;

namespace AbyssalProtocol
{
    public sealed class MapComponent_ABY_DominionAtmosphere : MapComponent
    {
        private const int ScanIntervalTicks = 600;
        private const int MaintenanceIntervalTicks = 1800;
        private const int AmbientIntervalMinTicks = 1250;
        private const int AmbientIntervalMaxTicks = 2600;

        private bool markedAsDominionSlice;
        private int nextScanTick;
        private int nextMaintenanceTick;
        private int nextAmbientPulseTick;
        private int maintenanceRuns;
        private int ambientPulses;

        public MapComponent_ABY_DominionAtmosphere(Map map) : base(map)
        {
            ScheduleInitialTicks();
        }

        public bool MarkedAsDominionSlice => markedAsDominionSlice;

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

            if (AbyssalProtocolMod.Settings?.verboseDiagnostics ?? false)
            {
                string sessionText = session != null ? session.sessionId : "none";
                ABY_LogThrottleUtility.Message("dominion-atmosphere-mark-" + map.uniqueID, "[Abyssal Protocol] Dominion atmosphere controller marked map " + map.uniqueID + " from " + (source ?? "unknown") + ", session=" + sessionText + ".", 2500);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref markedAsDominionSlice, "ABY_markedAsDominionSlice", false);
            Scribe_Values.Look(ref nextScanTick, "ABY_nextAtmosphereScanTick", 0);
            Scribe_Values.Look(ref nextMaintenanceTick, "ABY_nextAtmosphereMaintenanceTick", 0);
            Scribe_Values.Look(ref nextAmbientPulseTick, "ABY_nextAmbientPulseTick", 0);
            Scribe_Values.Look(ref maintenanceRuns, "ABY_atmosphereMaintenanceRuns", 0);
            Scribe_Values.Look(ref ambientPulses, "ABY_atmosphereAmbientPulses", 0);

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
                nextMaintenanceTick = now + MaintenanceIntervalTicks;
            }

            if (now >= nextAmbientPulseTick)
            {
                TryRunAmbientPulse();
                nextAmbientPulseTick = now + Rand.Range(AmbientIntervalMinTicks, AmbientIntervalMaxTicks);
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
                TerrainDef baseTerrain = ABY_DominionAtmosphereUtility.ResolveDominionBaseTerrain();
                CellRect whole = new CellRect(0, 0, map.Size.x, map.Size.z);
                foreach (IntVec3 cell in whole)
                {
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

                maintenanceRuns++;
                if (AbyssalProtocolMod.Settings?.verboseDiagnostics ?? false)
                {
                    ABY_LogThrottleUtility.Message("dominion-atmosphere-maintenance-" + map.uniqueID, "[Abyssal Protocol] Dominion atmosphere maintenance ran on map " + map.uniqueID + " (runs=" + maintenanceRuns + ").", 5000);
                }
            }
            catch (Exception ex)
            {
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
            if (settings != null && (!settings.enableBossMapPresentationEffects || settings.reducedMotion))
            {
                return;
            }

            ABY_DominionAtmosphereUtility.ThrowQuietAtmospherePulse(map);
            ambientPulses++;
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
        }

        private static int CurrentTick => Find.TickManager != null ? Find.TickManager.TicksGame : 0;
    }
}
