using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionSliceBuilder
    {
        private const string BastionDefName = "ABY_DominionSliceBastion";
        private const string SpireDefName = "ABY_DominionSliceSpire";
        private const string SigilPadDefName = "ABY_DominionSliceSigilPad";

        public static bool TryPrepareDominionSlice(Map map, ABY_DominionPocketSession session, out string failReason)
        {
            failReason = null;
            if (map == null || session == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

            try
            {
                ClearMap(map);
                BuildLayout(map, session);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[Abyssal Protocol] Failed to prepare dominion slice: " + ex);
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }
        }

        private static void ClearMap(Map map)
        {
            List<Thing> all = new List<Thing>();
            if (map.listerThings != null && map.listerThings.AllThings != null)
            {
                all.AddRange(map.listerThings.AllThings);
            }

            for (int i = all.Count - 1; i >= 0; i--)
            {
                Thing thing = all[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (thing is Pawn)
                {
                    continue;
                }

                if (thing.def.destroyable || thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Item)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            CellRect whole = new CellRect(0, 0, map.Size.x, map.Size.z);
            foreach (IntVec3 cell in whole)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                if (map.fogGrid != null && map.fogGrid.IsFogged(cell))
                {
                    map.fogGrid.Unfog(cell);
                }

                if (map.roofGrid != null)
                {
                    map.roofGrid.SetRoof(cell, null);
                }

                if (map.snowGrid != null)
                {
                    map.snowGrid.SetDepth(cell, 0f);
                }
            }
        }

        private static void BuildLayout(Map map, ABY_DominionPocketSession session)
        {
            TerrainDef baseTerrain = ResolveTerrain("MetalTile", "Concrete", "PavedTile");
            TerrainDef pathTerrain = ResolveTerrain("PavedTile", "Concrete", "MetalTile");
            TerrainDef focalTerrain = ResolveTerrain("SterileTile", "MetalTile", "Concrete");

            PaintWholeMap(map, baseTerrain);

            IntVec3 center = map.Center;
            IntVec3 entry = ClampToInterior(map, new IntVec3(center.x, 0, Math.Max(14, map.Size.z / 6)));
            IntVec3 anchorA = ClampToInterior(map, new IntVec3(center.x - 22, 0, center.z + 18));
            IntVec3 anchorB = ClampToInterior(map, new IntVec3(center.x + 22, 0, center.z + 18));
            IntVec3 anchorC = ClampToInterior(map, new IntVec3(center.x + 28, 0, center.z - 16));
            IntVec3 sidePocket = ClampToInterior(map, new IntVec3(center.x - 30, 0, center.z - 18));

            session.pocketEntryCell = entry;
            session.extractionCell = entry;
            session.heartCell = center;
            session.anchorCells = new List<IntVec3> { anchorA, anchorB, anchorC };

            PaintCircle(map, entry, 9, focalTerrain);
            PaintCircle(map, center, 14, pathTerrain);
            PaintCircle(map, center, 9, focalTerrain);
            PaintCircle(map, sidePocket, 6, pathTerrain);

            PaintCorridor(map, entry, center + new IntVec3(0, 0, -10), 4, pathTerrain);
            PaintCorridor(map, center, anchorA, 3, pathTerrain);
            PaintCorridor(map, center, anchorB, 3, pathTerrain);
            PaintCorridor(map, center, anchorC, 3, pathTerrain);
            PaintCorridor(map, center, sidePocket, 2, pathTerrain);

            for (int i = 0; i < session.anchorCells.Count; i++)
            {
                PaintCircle(map, session.anchorCells[i], 5, focalTerrain);
            }

            SpawnPad(map, entry);
            SpawnPad(map, center);
            SpawnPad(map, sidePocket);
            for (int i = 0; i < session.anchorCells.Count; i++)
            {
                SpawnPad(map, session.anchorCells[i]);
            }

            SpawnPerimeter(map);
            SpawnInnerFrame(map, center, entry, session.anchorCells, sidePocket);
        }

        private static TerrainDef ResolveTerrain(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                TerrainDef def = DefDatabase<TerrainDef>.GetNamedSilentFail(names[i]);
                if (def != null)
                {
                    return def;
                }
            }

            return TerrainDefOf.Concrete;
        }

        private static IntVec3 ClampToInterior(Map map, IntVec3 cell)
        {
            int x = Mathf.Clamp(cell.x, 10, map.Size.x - 11);
            int z = Mathf.Clamp(cell.z, 10, map.Size.z - 11);
            return new IntVec3(x, 0, z);
        }

        private static void PaintWholeMap(Map map, TerrainDef terrain)
        {
            CellRect whole = new CellRect(0, 0, map.Size.x, map.Size.z);
            foreach (IntVec3 cell in whole)
            {
                if (cell.InBounds(map))
                {
                    map.terrainGrid.SetTerrain(cell, terrain);
                }
            }
        }

        private static void PaintCircle(Map map, IntVec3 center, int radius, TerrainDef terrain)
        {
            int radiusSq = radius * radius;
            foreach (IntVec3 cell in CellRect.CenteredOn(center, radius))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                int dx = cell.x - center.x;
                int dz = cell.z - center.z;
                if (dx * dx + dz * dz <= radiusSq)
                {
                    map.terrainGrid.SetTerrain(cell, terrain);
                }
            }
        }

        private static void PaintCorridor(Map map, IntVec3 from, IntVec3 to, int halfWidth, TerrainDef terrain)
        {
            int steps = Mathf.Max(Math.Abs(to.x - from.x), Math.Abs(to.z - from.z));
            if (steps <= 0)
            {
                PaintCircle(map, from, halfWidth, terrain);
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = GenMath.RoundRandom(Mathf.Lerp(from.x, to.x, t));
                int z = GenMath.RoundRandom(Mathf.Lerp(from.z, to.z, t));
                PaintCircle(map, new IntVec3(x, 0, z), halfWidth, terrain);
            }
        }

        private static void SpawnPerimeter(Map map)
        {
            int minX = 6;
            int maxX = map.Size.x - 7;
            int minZ = 6;
            int maxZ = map.Size.z - 7;
            for (int x = minX; x <= maxX; x += 12)
            {
                SpawnProp(map, BastionDefName, new IntVec3(x, 0, minZ), Rot4.North);
                SpawnProp(map, SpireDefName, new IntVec3(x, 0, maxZ), Rot4.South);
            }

            for (int z = minZ + 12; z < maxZ; z += 12)
            {
                SpawnProp(map, SpireDefName, new IntVec3(minX, 0, z), Rot4.West);
                SpawnProp(map, BastionDefName, new IntVec3(maxX, 0, z), Rot4.East);
            }
        }

        private static void SpawnInnerFrame(Map map, IntVec3 center, IntVec3 entry, List<IntVec3> anchors, IntVec3 sidePocket)
        {
            SpawnProp(map, BastionDefName, entry + new IntVec3(-8, 0, 0), Rot4.West);
            SpawnProp(map, BastionDefName, entry + new IntVec3(8, 0, 0), Rot4.East);

            SpawnProp(map, SpireDefName, center + new IntVec3(-16, 0, 0), Rot4.West);
            SpawnProp(map, SpireDefName, center + new IntVec3(16, 0, 0), Rot4.East);
            SpawnProp(map, SpireDefName, center + new IntVec3(0, 0, 16), Rot4.North);
            SpawnProp(map, SpireDefName, center + new IntVec3(0, 0, -16), Rot4.South);

            for (int i = 0; i < anchors.Count; i++)
            {
                SpawnProp(map, SpireDefName, anchors[i] + new IntVec3(-5, 0, 0), Rot4.West);
                SpawnProp(map, SpireDefName, anchors[i] + new IntVec3(5, 0, 0), Rot4.East);
            }

            SpawnProp(map, BastionDefName, sidePocket + new IntVec3(-5, 0, 0), Rot4.West);
            SpawnProp(map, BastionDefName, sidePocket + new IntVec3(5, 0, 0), Rot4.East);
        }

        private static void SpawnPad(Map map, IntVec3 cell)
        {
            SpawnProp(map, SigilPadDefName, cell, Rot4.North);
        }

        private static void SpawnProp(Map map, string defName, IntVec3 cell, Rot4 rot)
        {
            if (map == null || !cell.InBounds(map))
            {
                return;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return;
            }

            List<Thing> things = cell.GetThingList(map);
            for (int i = things.Count - 1; i >= 0; i--)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (thing is Pawn)
                {
                    continue;
                }

                if (thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Item)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            Thing spawned = ThingMaker.MakeThing(def);
            if (spawned != null)
            {
                GenSpawn.Spawn(spawned, cell, map, rot);
            }
        }
    }
}
