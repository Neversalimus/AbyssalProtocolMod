using System;
using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_LogThrottleUtility
    {
        private static Dictionary<string, int> nextLogTickByKey = new Dictionary<string, int>();

        public static void Warning(string key, string message, int throttleTicks = 2500)
        {
            try
            {
                if (CanLog(key, throttleTicks))
                {
                    Log.Warning(message);
                }
            }
            catch
            {
                // Logging must never break static constructors or Harmony finalizers.
                // RimWorld can call early static constructors before Find.TickManager is safe to touch.
            }
        }

        public static void Message(string key, string message, int throttleTicks = 2500)
        {
            try
            {
                if (CanLog(key, throttleTicks))
                {
                    Log.Message(message);
                }
            }
            catch
            {
                // Logging must remain best-effort only.
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

                if (nextLogTickByKey == null)
                {
                    nextLogTickByKey = new Dictionary<string, int>();
                }

                int now = SafeTicksGame();
                if (nextLogTickByKey.TryGetValue(key, out int nextTick) && now < nextTick)
                {
                    return false;
                }

                nextLogTickByKey[key] = now + Math.Max(1, throttleTicks);
                return true;
            }
            catch
            {
                // If throttling itself fails, allow the caller to attempt one log instead of crashing.
                return true;
            }
        }

        private static int SafeTicksGame()
        {
            try
            {
                TickManager tickManager = Find.TickManager;
                return tickManager != null ? tickManager.TicksGame : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
