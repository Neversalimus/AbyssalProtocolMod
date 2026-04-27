using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_LogThrottleUtility
    {
        private static readonly Dictionary<string, int> NextLogTickByKey = new Dictionary<string, int>();

        public static void Warning(string key, string message, int throttleTicks = 2500)
        {
            if (CanLog(key, throttleTicks))
            {
                Log.Warning(message);
            }
        }

        public static void Message(string key, string message, int throttleTicks = 2500)
        {
            if (CanLog(key, throttleTicks))
            {
                Log.Message(message);
            }
        }

        private static bool CanLog(string key, int throttleTicks)
        {
            if (key.NullOrEmpty())
            {
                key = "default";
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (NextLogTickByKey.TryGetValue(key, out int nextTick) && now < nextTick)
            {
                return false;
            }

            NextLogTickByKey[key] = now + System.Math.Max(1, throttleTicks);
            return true;
        }
    }
}
