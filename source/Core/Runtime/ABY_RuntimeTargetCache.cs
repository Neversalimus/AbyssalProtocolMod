using System;
using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Shared low-frequency map target cache for Abyssal combat/runtime systems.
    /// Avoids each pawn, turret, halo and compatibility component re-scanning mapPawns/AllThings on its own tick cadence.
    /// The cache is intentionally conservative: it refreshes often enough for combat responsiveness, while callers still
    /// validate faction, range, LOS and spawned state before acting.
    /// </summary>
    public static class ABY_RuntimeTargetCache
    {
        private const int PawnRefreshIntervalTicks = 30;
        private const int FullCleanupIntervalTicks = 1800;

        private static readonly IReadOnlyList<Pawn> EmptyPawnList = new List<Pawn>(0);
        private static readonly IReadOnlyList<Thing> EmptyThingList = new List<Thing>(0);
        private static readonly Dictionary<int, MapCache> CachesByMapId = new Dictionary<int, MapCache>();

        private sealed class MapCache
        {
            public int nextPawnRefreshTick = -1;
            public int lastSeenTick;
            public readonly List<Pawn> spawnedLivingPawns = new List<Pawn>(96);
            public readonly List<Pawn> combatTargetPawns = new List<Pawn>(96);
        }

        public static IReadOnlyList<Pawn> SpawnedLivingPawnsFor(Map map)
        {
            MapCache cache = ResolveCache(map);
            return cache != null ? cache.spawnedLivingPawns : EmptyPawnList;
        }

        public static IReadOnlyList<Pawn> CombatTargetPawnsFor(Map map)
        {
            MapCache cache = ResolveCache(map);
            return cache != null ? cache.combatTargetPawns : EmptyPawnList;
        }

        public static IReadOnlyList<Thing> SpawnedThingsOfDefName(Map map, string defName)
        {
            if (map?.listerThings == null || defName.NullOrEmpty())
            {
                return EmptyThingList;
            }

            ThingDef def = ABY_DefCache.ThingDefNamed(defName);
            if (def == null)
            {
                return EmptyThingList;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(def);
            return things ?? EmptyThingList;
        }

        public static bool HasSpawnedThingDef(Map map, string defName)
        {
            IReadOnlyList<Thing> things = SpawnedThingsOfDefName(map, defName);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null && !thing.Destroyed && thing.Spawned)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryFindThingById(Map map, int thingId, out Thing thing)
        {
            thing = null;
            if (thingId < 0 || map == null)
            {
                return false;
            }

            IReadOnlyList<Pawn> pawns = SpawnedLivingPawnsFor(map);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && pawn.thingIDNumber == thingId)
                {
                    thing = pawn;
                    return true;
                }
            }

            List<Thing> allThings = map.listerThings?.AllThings;
            if (allThings == null)
            {
                return false;
            }

            for (int i = 0; i < allThings.Count; i++)
            {
                Thing candidate = allThings[i];
                if (candidate != null && candidate.thingIDNumber == thingId)
                {
                    thing = candidate;
                    return true;
                }
            }

            return false;
        }

        public static void NotifyLikelyStateChanged(Map map)
        {
            if (map == null)
            {
                return;
            }

            if (CachesByMapId.TryGetValue(map.uniqueID, out MapCache cache))
            {
                cache.nextPawnRefreshTick = -1;
            }
        }

        private static MapCache ResolveCache(Map map)
        {
            if (map == null)
            {
                return null;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            CleanupDeadMapCaches(now);

            int id = map.uniqueID;
            if (!CachesByMapId.TryGetValue(id, out MapCache cache))
            {
                cache = new MapCache();
                CachesByMapId[id] = cache;
            }

            cache.lastSeenTick = now;
            if (cache.nextPawnRefreshTick < 0 || now >= cache.nextPawnRefreshTick)
            {
                RefreshPawnLists(map, cache, now);
            }

            return cache;
        }

        private static void RefreshPawnLists(Map map, MapCache cache, int now)
        {
            cache.spawnedLivingPawns.Clear();
            cache.combatTargetPawns.Clear();

            IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Dead || pawn.MapHeld != map)
                    {
                        continue;
                    }

                    cache.spawnedLivingPawns.Add(pawn);

                    if (!pawn.Downed && pawn.Faction != null)
                    {
                        cache.combatTargetPawns.Add(pawn);
                    }
                }
            }

            int stagger = Math.Abs(map.uniqueID % 7);
            cache.nextPawnRefreshTick = now + PawnRefreshIntervalTicks + stagger;
        }

        private static void CleanupDeadMapCaches(int now)
        {
            if (CachesByMapId.Count == 0 || now % FullCleanupIntervalTicks != 0)
            {
                return;
            }

            List<int> remove = null;
            foreach (KeyValuePair<int, MapCache> pair in CachesByMapId)
            {
                if (now - pair.Value.lastSeenTick > FullCleanupIntervalTicks * 2)
                {
                    if (remove == null)
                    {
                        remove = new List<int>();
                    }

                    remove.Add(pair.Key);
                }
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                CachesByMapId.Remove(remove[i]);
            }
        }
    }
}
