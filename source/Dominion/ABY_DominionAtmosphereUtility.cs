using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_DominionAtmosphereUtility
    {
        private const string HeartDefName = "ABY_DominionSliceHeart";
        private const string ExitDefName = "ABY_DominionPocketExit";
        private const string BaseTerrainDefName = "ABY_DominionAshMetal";

        public static bool IsDominionPocketMap(Map map)
        {
            if (map == null)
            {
                return false;
            }

            MapComponent_ABY_DominionAtmosphere component = map.GetComponent<MapComponent_ABY_DominionAtmosphere>();
            if (component != null && component.MarkedAsDominionSlice)
            {
                return true;
            }

            ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
            if (runtime != null && runtime.TryGetSessionByPocketMap(map, out _))
            {
                return true;
            }

            return ContainsThing(map, HeartDefName) || ContainsThing(map, ExitDefName);
        }

        public static void MarkDominionSlice(Map map, ABY_DominionPocketSession session = null, string source = null)
        {
            if (map == null)
            {
                return;
            }

            MapComponent_ABY_DominionAtmosphere component = map.GetComponent<MapComponent_ABY_DominionAtmosphere>();
            if (component != null)
            {
                component.MarkDominionSlice(session, source);
            }
        }

        public static TerrainDef ResolveDominionBaseTerrain()
        {
            return DefDatabase<TerrainDef>.GetNamedSilentFail(BaseTerrainDefName) ?? TerrainDefOf.Concrete;
        }

        public static bool TryResolveSession(Map map, out ABY_DominionPocketSession session)
        {
            session = null;
            if (map == null)
            {
                return false;
            }

            ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
            if (runtime != null && runtime.TryGetSessionByPocketMap(map, out session))
            {
                return session != null;
            }

            return false;
        }

        public static bool TryFindFocusCell(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            if (TryResolveSession(map, out ABY_DominionPocketSession session))
            {
                List<IntVec3> candidates = new List<IntVec3>();
                if (session.heartCell.IsValid)
                {
                    candidates.Add(session.heartCell);
                }

                if (session.extractionCell.IsValid)
                {
                    candidates.Add(session.extractionCell);
                }

                if (session.anchorCells != null)
                {
                    for (int i = 0; i < session.anchorCells.Count; i++)
                    {
                        if (session.anchorCells[i].IsValid)
                        {
                            candidates.Add(session.anchorCells[i]);
                        }
                    }
                }

                if (candidates.Count > 0)
                {
                    cell = candidates.RandomElement();
                    return true;
                }
            }

            Thing heart = FindThing(map, HeartDefName);
            if (heart != null)
            {
                cell = heart.Position;
                return true;
            }

            Thing exit = FindThing(map, ExitDefName);
            if (exit != null)
            {
                cell = exit.Position;
                return true;
            }

            cell = map.Center;
            return cell.InBounds(map);
        }

        public static bool TryFindAtmosphereCellNear(Map map, IntVec3 origin, int minRadius, int maxRadius, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            if (map == null || !origin.InBounds(map))
            {
                return false;
            }

            int safeMin = Mathf.Max(0, minRadius);
            int safeMax = Mathf.Max(safeMin, maxRadius);
            for (int i = 0; i < 18; i++)
            {
                float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Rand.Range(safeMin, safeMax + 1);
                IntVec3 candidate = new IntVec3(
                    origin.x + GenMath.RoundRandom(Mathf.Cos(angle) * radius),
                    0,
                    origin.z + GenMath.RoundRandom(Mathf.Sin(angle) * radius));

                if (!candidate.InBounds(map) || candidate.x < 7 || candidate.z < 7 || candidate.x > map.Size.x - 8 || candidate.z > map.Size.z - 8)
                {
                    continue;
                }

                if (candidate.Standable(map) || map.terrainGrid?.TerrainAt(candidate) != null)
                {
                    result = candidate;
                    return true;
                }
            }

            return false;
        }

        public static void ThrowQuietAtmospherePulse(Map map)
        {
            if (map == null)
            {
                return;
            }

            if (!TryFindFocusCell(map, out IntVec3 focus))
            {
                return;
            }

            int pulses = Rand.RangeInclusive(1, 2);
            for (int i = 0; i < pulses; i++)
            {
                if (!TryFindAtmosphereCellNear(map, focus, 8, 42, out IntVec3 cell))
                {
                    continue;
                }

                Vector3 loc = cell.ToVector3Shifted();
                try
                {
                    if (Rand.Chance(0.72f))
                    {
                        FleckMaker.ThrowDustPuff(loc, map, Rand.Range(0.55f, 1.12f));
                    }

                    if (Rand.Chance(0.18f))
                    {
                        FleckMaker.ThrowHeatGlow(cell, map, Rand.Range(0.22f, 0.46f));
                    }

                    if (Rand.Chance(0.10f))
                    {
                        FleckMaker.ThrowLightningGlow(loc, map, Rand.Range(0.14f, 0.28f));
                    }
                }
                catch (Exception ex)
                {
                    ABY_LogThrottleUtility.Warning("dominion-atmosphere-fleck", "[Abyssal Protocol] Dominion atmosphere pulse skipped: " + ex.GetType().Name, 5000);
                    return;
                }
            }
        }

        private static bool ContainsThing(Map map, string defName)
        {
            return FindThing(map, defName) != null;
        }

        private static Thing FindThing(Map map, string defName)
        {
            if (map?.listerThings == null || defName.NullOrEmpty())
            {
                return null;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return null;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(def);
            if (things == null || things.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null && !thing.Destroyed)
                {
                    return thing;
                }
            }

            return null;
        }
    }
}
