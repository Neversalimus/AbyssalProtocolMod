using System;
using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_LogThrottleUtility
    {
        private static readonly object SyncRoot = new object();
        private static Dictionary<string, int> nextLogTickByKey;

        public static void Warning(string key, string message, int throttleTicks = 2500)
        {
            try
            {
                if (CanLog(key, throttleTicks))
                {
                    Log.Warning(message ?? string.Empty);
                }
            }
            catch
            {
                // Logging must never break static constructors, Harmony finalizers, or early PlayData loading.
            }
        }

        public static void Message(string key, string message, int throttleTicks = 2500)
        {
            try
            {
                if (CanLog(key, throttleTicks))
                {
                    Log.Message(message ?? string.Empty);
                }
            }
            catch
            {
                // Logging must never break static constructors, Harmony finalizers, or early PlayData loading.
            }
        }

        private static bool CanLog(string key, int throttleTicks)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                {
                    key = "default";
                }

                int now = SafeTicks();
                int delay = Math.Max(1, throttleTicks);
                if (!SafeSuppressRepeatedWarnings())
                {
                    delay = 1;
                }

                lock (SyncRoot)
                {
                    Dictionary<string, int> map = nextLogTickByKey;
                    if (map == null)
                    {
                        map = new Dictionary<string, int>();
                        nextLogTickByKey = map;
                    }

                    if (map.TryGetValue(key, out int nextTick) && now < nextTick)
                    {
                        return false;
                    }

                    map[key] = now + delay;
                    return true;
                }
            }
            catch
            {
                // If throttle state itself is unavailable during early startup, prefer silence over a red error.
                return false;
            }
        }

        private static bool SafeSuppressRepeatedWarnings()
        {
            try
            {
                AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
                return settings == null || settings.suppressRepeatedWarnings;
            }
            catch
            {
                // Mod settings can be unavailable while StaticConstructorOnStartup classes are being called.
                return true;
            }
        }

        private static int SafeTicks()
        {
            try
            {
                TickManager tickManager = Find.TickManager;
                if (tickManager != null)
                {
                    return tickManager.TicksGame;
                }
            }
            catch
            {
                // Find.TickManager can dereference Current.Game during very early static construction.
            }

            return Environment.TickCount & int.MaxValue;
        }
    }
}
