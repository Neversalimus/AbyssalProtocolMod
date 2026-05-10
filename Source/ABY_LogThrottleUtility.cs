using System;
using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_LogThrottleUtility
    {
        private static readonly Dictionary<string, int> NextLogTickByKey = new Dictionary<string, int>();

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
                // Logging must never break static constructors or Harmony finalizers.
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
                // Logging must never break static constructors or Harmony finalizers.
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

                int now = 0;
                try
                {
                    TickManager tickManager = Find.TickManager;
                    if (tickManager != null)
                    {
                        now = tickManager.TicksGame;
                    }
                }
                catch
                {
                    now = Environment.TickCount & int.MaxValue;
                }

                if (NextLogTickByKey.TryGetValue(key, out int nextTick) && now < nextTick)
                {
                    return false;
                }

                NextLogTickByKey[key] = now + Math.Max(1, throttleTicks);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
