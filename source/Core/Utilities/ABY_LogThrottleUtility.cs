using System;
using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_LogThrottleUtility
    {
        private const int MaxTrackedKeys = 512;
        private const int MaxKeyLength = 160;
        private const int PruneIntervalTicks = 2500;
        private const int MaxFutureTickWindow = 250000;

        private static readonly object SyncRoot = new object();
        private static Dictionary<string, int> nextLogTickByKey;
        private static int nextPruneTick = int.MinValue;

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

        public static void Clear()
        {
            try
            {
                lock (SyncRoot)
                {
                    nextLogTickByKey?.Clear();
                    nextPruneTick = int.MinValue;
                }
            }
            catch
            {
                // Log-throttle cleanup must remain best-effort and non-fatal.
            }
        }

        private static bool CanLog(string key, int throttleTicks)
        {
            try
            {
                key = NormalizeKey(key);

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

                    PruneExpiredOrStaleKeys(map, now, map.Count >= MaxTrackedKeys);

                    if (map.TryGetValue(key, out int nextTick) && !IsExpiredOrStale(nextTick, now))
                    {
                        return false;
                    }

                    if (map.Count >= MaxTrackedKeys && !map.ContainsKey(key))
                    {
                        EvictOneKey(map, now);
                    }

                    map[key] = SafeAddTicks(now, delay);
                    return true;
                }
            }
            catch
            {
                // If throttle state itself is unavailable during early startup, prefer silence over a red error.
                return false;
            }
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "default";
            }

            key = key.Trim();
            if (key.Length <= 0)
            {
                return "default";
            }

            if (key.Length <= MaxKeyLength)
            {
                return key;
            }

            return key.Substring(0, MaxKeyLength);
        }

        private static void PruneExpiredOrStaleKeys(Dictionary<string, int> map, int now, bool force)
        {
            if (map == null || map.Count == 0)
            {
                return;
            }

            if (!force && !IsPruneDue(now))
            {
                return;
            }

            nextPruneTick = SafeAddTicks(now, PruneIntervalTicks);

            List<string> keysToRemove = null;
            foreach (KeyValuePair<string, int> entry in map)
            {
                if (!IsExpiredOrStale(entry.Value, now))
                {
                    continue;
                }

                if (keysToRemove == null)
                {
                    keysToRemove = new List<string>();
                }

                keysToRemove.Add(entry.Key);
            }

            if (keysToRemove != null)
            {
                for (int i = 0; i < keysToRemove.Count; i++)
                {
                    map.Remove(keysToRemove[i]);
                }
            }

            while (map.Count > MaxTrackedKeys)
            {
                EvictOneKey(map, now);
            }
        }

        private static bool IsPruneDue(int now)
        {
            if (nextPruneTick == int.MinValue)
            {
                return true;
            }

            long ticksUntilPrune = (long)nextPruneTick - now;
            return ticksUntilPrune <= 0L || ticksUntilPrune > MaxFutureTickWindow;
        }

        private static void EvictOneKey(Dictionary<string, int> map, int now)
        {
            if (map == null || map.Count == 0)
            {
                return;
            }

            string candidateKey = null;
            int candidateTick = int.MaxValue;

            foreach (KeyValuePair<string, int> entry in map)
            {
                if (IsExpiredOrStale(entry.Value, now))
                {
                    candidateKey = entry.Key;
                    break;
                }

                if (candidateKey == null || entry.Value < candidateTick)
                {
                    candidateKey = entry.Key;
                    candidateTick = entry.Value;
                }
            }

            if (candidateKey != null)
            {
                map.Remove(candidateKey);
            }
        }

        private static bool IsExpiredOrStale(int nextTick, int now)
        {
            long delta = (long)nextTick - now;
            return delta <= 0L || delta > MaxFutureTickWindow;
        }

        private static int SafeAddTicks(int now, int delay)
        {
            long value = (long)now + Math.Max(1, delay);
            if (value >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)value;
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
