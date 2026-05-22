using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Defensive recovery helper for rare stale RimWorld PowerNet graphs after large abyssal encounters.
    /// The normal path is intentionally soft for mod compatibility: it asks vanilla to rebuild the map
    /// power graph and refreshes overlays, but does not force manual reconnects on every power comp.
    /// Deep reconnect is reserved for explicit dev/manual recovery commands.
    /// </summary>
    public static class ABY_PowerNetRecoveryUtility
    {
        private const int DefaultThrottleTicks = 1200;
        private static readonly Dictionary<int, int> LastRefreshTickByMap = new Dictionary<int, int>();

        public static bool TryRebuildPowerNetsNow(
            Map map,
            string reason = null,
            bool showMessage = false,
            bool ignoreThrottle = false,
            bool deepReconnect = false)
        {
            if (map == null || map.powerNetManager == null)
            {
                return false;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int mapId = map.uniqueID;
            if (!ignoreThrottle && LastRefreshTickByMap.TryGetValue(mapId, out int lastTick) && ticksGame - lastTick < DefaultThrottleTicks)
            {
                return false;
            }

            LastRefreshTickByMap[mapId] = ticksGame;

            int beforeNets = SafePowerNetCount(map);
            int powerCompCount = 0;
            int transmitterCount = 0;
            int manualReconnectCount = 0;

            try
            {
                map.powerNetManager.UpdatePowerNetsAndConnections_First();

                if (deepReconnect)
                {
                    RunDeepReconnectPass(map, ref powerCompCount, ref transmitterCount, ref manualReconnectCount);
                    map.powerNetManager.UpdatePowerNetsAndConnections_First();
                }

                DirtyPowerGridOverlay(map);
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Power net rebuild failed" + FormatReason(reason) + ": " + ex.GetType().Name + ": " + ex.Message + "\n" + ex);
                return false;
            }

            int afterNets = SafePowerNetCount(map);
            string modeLabel = deepReconnect ? "deep manual" : "soft";
            string summary = "[Abyssal Protocol] Rebuilt map power nets (" + modeLabel + ")" + FormatReason(reason) + ". Nets " + beforeNets + " -> " + afterNets;
            if (deepReconnect)
            {
                summary += ", power comps " + powerCompCount + ", transmitters " + transmitterCount + ", reconnect nudges " + manualReconnectCount;
            }
            summary += ".";

            Log.Message(summary);
            if (showMessage)
            {
                Messages.Message("Abyssal power-net recovery: " + beforeNets + " -> " + afterNets + " nets" + (deepReconnect ? " (deep)." : " (soft)."), MessageTypeDefOf.PositiveEvent, false);
            }

            return true;
        }

        public static bool TryRebuildPowerNetsForThing(Thing thing, string reason = null, bool showMessage = false, bool ignoreThrottle = false, bool deepReconnect = false)
        {
            return TryRebuildPowerNetsNow(thing?.MapHeld, reason, showMessage, ignoreThrottle, deepReconnect);
        }

        private static void RunDeepReconnectPass(Map map, ref int powerCompCount, ref int transmitterCount, ref int manualReconnectCount)
        {
            List<Thing> things = map.listerThings?.AllThings;
            if (things == null)
            {
                return;
            }

            for (int i = 0; i < things.Count; i++)
            {
                if (!(things[i] is ThingWithComps thingWithComps) || thingWithComps.Destroyed || !thingWithComps.Spawned)
                {
                    continue;
                }

                List<ThingComp> comps = thingWithComps.AllComps;
                if (comps == null)
                {
                    continue;
                }

                for (int j = 0; j < comps.Count; j++)
                {
                    if (!(comps[j] is CompPower powerComp))
                    {
                        continue;
                    }

                    powerCompCount++;
                    if (powerComp.TransmitsPowerNow)
                    {
                        transmitterCount++;
                        try
                        {
                            map.powerNetManager.Notfiy_TransmitterTransmitsPowerNowChanged(powerComp);
                        }
                        catch
                        {
                            // Some power comps may already be reconciled or in a transient modded state.
                        }
                    }

                    try
                    {
                        powerComp.TryManualReconnect(false);
                        manualReconnectCount++;
                    }
                    catch
                    {
                        // Deep recovery is a best-effort dev/manual pass; individual comp failures are non-fatal.
                    }
                }
            }
        }

        private static int SafePowerNetCount(Map map)
        {
            try
            {
                return map?.powerNetManager?.AllNetsListForReading?.Count ?? -1;
            }
            catch
            {
                return -1;
            }
        }

        private static void DirtyPowerGridOverlay(Map map)
        {
            try
            {
                if (map?.mapDrawer == null)
                {
                    return;
                }

                List<Thing> things = map.listerThings?.AllThings;
                if (things == null)
                {
                    map.mapDrawer.MapMeshDirty(map.Center, MapMeshFlagDefOf.PowerGrid, true, true);
                    return;
                }

                int dirtied = 0;
                for (int i = 0; i < things.Count && dirtied < 256; i++)
                {
                    if (!(things[i] is ThingWithComps thingWithComps) || thingWithComps.Destroyed || !thingWithComps.Spawned)
                    {
                        continue;
                    }

                    bool hasPowerComp = false;
                    List<ThingComp> comps = thingWithComps.AllComps;
                    if (comps != null)
                    {
                        for (int j = 0; j < comps.Count; j++)
                        {
                            if (comps[j] is CompPower)
                            {
                                hasPowerComp = true;
                                break;
                            }
                        }
                    }

                    if (!hasPowerComp)
                    {
                        continue;
                    }

                    map.mapDrawer.MapMeshDirty(thingWithComps.PositionHeld, MapMeshFlagDefOf.PowerGrid, true, false);
                    dirtied++;
                }

                if (dirtied == 0)
                {
                    map.mapDrawer.MapMeshDirty(map.Center, MapMeshFlagDefOf.PowerGrid, true, true);
                }
            }
            catch
            {
                // Visual-only refresh; ignore failures.
            }
        }

        private static string FormatReason(string reason)
        {
            return reason.NullOrEmpty() ? string.Empty : " (" + reason + ")";
        }
    }
}
