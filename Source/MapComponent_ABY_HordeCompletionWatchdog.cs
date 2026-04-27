using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_ABY_HordeCompletionWatchdog : MapComponent
    {
        private const int WatchdogIntervalTicks = 180;
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
            if (portalWave == null)
            {
                return;
            }

            bool activeHordeWave = GetPrivateField<bool>(portalWave, "activeHordeWave");
            if (!activeHordeWave)
            {
                return;
            }

            IList queuedPortals = GetPrivateField<IList>(portalWave, "queuedPortals");
            if (queuedPortals != null && queuedPortals.Count > 0)
            {
                return;
            }

            if (HasActiveAbyssalPortals())
            {
                return;
            }

            Building_AbyssalHordeCommandGate gate = GetPrivateField<Building_AbyssalHordeCommandGate>(portalWave, "activeCommandGate");
            if (gate != null && gate.Spawned && !gate.Destroyed)
            {
                return;
            }

            if (HasLiveCombatCapableAbyssalPawns())
            {
                return;
            }

            try
            {
                MethodInfo resetWave = typeof(MapComponent_AbyssalPortalWave).GetMethod("ResetWave", BindingFlags.Instance | BindingFlags.NonPublic);
                if (resetWave != null)
                {
                    resetWave.Invoke(portalWave, null);
                    Log.Message("[Abyssal Protocol] Horde watchdog resolved a completed horde wave with no remaining portals, command gate, or combat-capable abyssal pawns.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Horde watchdog could not reset completed wave: " + ex.Message);
            }
        }

        private bool HasActiveAbyssalPortals()
        {
            if (map?.listerThings?.AllThings == null)
            {
                return false;
            }

            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.def == null)
                {
                    continue;
                }

                string defName = thing.def.defName ?? string.Empty;
                if (defName.IndexOf("ABY_ImpPortal", StringComparison.OrdinalIgnoreCase) >= 0
                    || defName.IndexOf("AbyssalPortal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasLiveCombatCapableAbyssalPawns()
        {
            if (map?.mapPawns == null)
            {
                return false;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!ABY_AntiTameUtility.IsAbyssalPawn(pawn))
                {
                    continue;
                }

                if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned || pawn.Downed)
                {
                    continue;
                }

                if (pawn.Faction == Faction.OfPlayer)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static T GetPrivateField<T>(object instance, string fieldName)
        {
            if (instance == null || fieldName.NullOrEmpty())
            {
                return default(T);
            }

            try
            {
                FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field == null)
                {
                    return default(T);
                }

                object value = field.GetValue(instance);
                if (value is T cast)
                {
                    return cast;
                }
            }
            catch
            {
            }

            return default(T);
        }
    }
}
