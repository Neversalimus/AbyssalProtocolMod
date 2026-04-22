using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionSliceBuilder
    {
        private const string BastionDefName = "ABY_DominionSliceBastion";
        private const string SpireDefName = "ABY_DominionSliceSpire";
        private const string SigilPadDefName = "ABY_DominionSliceSigilPad";
        private const int PerimeterStep = 8;

        public static bool TryPrepareDominionSlice(Map map, ABY_DominionPocketSession session, out string failReason)
        {
            failReason = null;
            if (map == null || session == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_EnvironmentBuild".Translate();
                return false;
            }

            try
            {
                ClearGeneratedPocketMap(map);
                BuildLayout(map, session);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[Abyssal Protocol] Failed to sculpt dominion slice environment: {ex}");
                failReason = "ABY_DominionPocketRuntimeFail_EnvironmentBuild".Translate();
                return false;
            }
        }

        private static void ClearGeneratedPocketMap(Map map)
        {
            if (map?.listerThings?.AllThings != null)
            {
                List<Thing> things = map.listerThings.AllThings.ToList();
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed)
                    {
                        continue;
                    }

                    if (thing is Pawn pawn && pawn.Faction == Faction.OfPlayer)
                    {
                        continue;
                    }

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

                map.roofGrid?.SetRoof(cell, null);
                map.snowGrid?.SetDepth(cell, 0f);
            }
        }

        private static void BuildLayout(Map map, ABY_DominionPocketSession session)
        {
            TerrainDef borderTerrain = ResolveTerrain("PavedTile", "Concrete", "MetalTile");
            TerrainDef routeTerrain = ResolveTerrain("MetalTile", "PavedTile", "Concrete");
            TerrainDef focalTerrain = ResolveTerrain("SterileTile", "MetalTile", "Concrete");
            TerrainDef accentTerrain = ResolveTerrain("Concrete", "PavedTile", "MetalTile");

            PaintWholeMap(map, borderTerrain);

            IntVec3 center = map.Center;
            IntVec3 entry = new IntVec3(center.x, 0, Math.Max(14, map.Size.z / 6));
            IntVec3 nwAnchor = ClampToInterior(map, new IntVec3(center.x - 24, 0, center.z + 22));
            IntVec3 neAnchor = ClampToInterior(map, new IntVec3(center.x + 24, 0, center.z + 22));
            IntVec3 seAnchor = ClampToInterior(map, new IntVec3(center.x + 30, 0, center.z - 18));
            IntVec3 rewardPocket = ClampToInterior(map, new IntVec3(center.x - 32, 0, center.z - 18));

            session.pocketEntryCell = entry;
            session.extractionCell = entry;
            session.heartCell = center;
            session.anchorCells = new List<IntVec3> { nwAnchor, neAnchor, seAnchor };

            PaintCircle(map, center, 16, routeTerrain);
            PaintCircle(map, center, 11, focalTerrain);
            PaintCircle(map, entry, 9, routeTerrain);
            PaintCircle(map, rewardPocket, 7, accentTerrain);

            for (int i = 0; i < session.anchorCells.Count; i++)
            {
                PaintCircle(map, session.anchorCells[i], 6, focalTerrain);
            }

            PaintCorridor(map, entry, new IntVec3(center.x, 0, center.z - 12), 4, routeTerrain);
            PaintCorridor(map, center, nwAnchor, 4, routeTerrain);
            PaintCorridor(map, center, neAnchor, 4, routeTerrain);
            PaintCorridor(map, center, seAnchor, 4, routeTerrain);
            PaintCorridor(map, center, rewardPocket, 3, accentTerrain);

            PaintRect(map, new CellRect(center.x - 8, center.z - 3, 17, 7), focalTerrain);
            PaintRect(map, new CellRect(entry.x - 7, entry.z - 4, 15, 9), focalTerrain);

            SpawnPad(map, entry);
            SpawnPad(map, center);
            SpawnPad(map, rewardPocket);
            for (int i = 0; i < session.anchorCells.Count; i++)
            {
                SpawnPad(map, session.anchorCells[i]);
            }

            SpawnCentralRing(map, center);
            SpawnPerimeter(map);
            SpawnRoutePylons(map, entry, center, session.anchorCells, rewardPocket);
        }

        private static TerrainDef ResolveTerrain(params string[] terrainNames)
        {
            for (int i = 0; i < terrainNames.Length; i++)
            {
                TerrainDef terrain = DefDatabase<TerrainDef>.GetNamedSilentFail(terrainNames[i]);
                if (terrain != null)
                {
                    return terrain;
                }
            }

            return TerrainDefOf.Concrete;
        }

        private static IntVec3 ClampToInterior(Map map, IntVec3 cell)
        {
            int x = Math.Max(10, Math.Min(map.Size.x - 11, cell.x));
            int z = Math.Max(10, Math.Min(map.Size.z - 11, cell.z));
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

        private static void PaintRect(Map map, CellRect rect, TerrainDef terrain)
        {
            foreach (IntVec3 cell in rect)
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
            CellRect rect = CellRect.CenteredOn(center, radius);
            foreach (IntVec3 cell in rect)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                int dx = cell.x - center.x;
                int dz = cell.z - center.z;
                if ((dx * dx) + (dz * dz) <= radiusSq)
                {
                    map.terrainGrid.SetTerrain(cell, terrain);
                }
            }
        }

        private static void PaintCorridor(Map map, IntVec3 from, IntVec3 to, int halfWidth, TerrainDef terrain)
        {
            int steps = Math.Max(Math.Abs(to.x - from.x), Math.Abs(to.z - from.z));
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

        private static void SpawnCentralRing(Map map, IntVec3 center)
        {
            SpawnProp(map, SpireDefName, center + new IntVec3(0, 0, 20), Rot4.North);
            SpawnProp(map, SpireDefName, center + new IntVec3(14, 0, 14), Rot4.East);
            SpawnProp(map, SpireDefName, center + new IntVec3(20, 0, 0), Rot4.East);
            SpawnProp(map, SpireDefName, center + new IntVec3(14, 0, -14), Rot4.South);
            SpawnProp(map, SpireDefName, center + new IntVec3(0, 0, -20), Rot4.South);
            SpawnProp(map, SpireDefName, center + new IntVec3(-14, 0, -14), Rot4.West);
            SpawnProp(map, SpireDefName, center + new IntVec3(-20, 0, 0), Rot4.West);
            SpawnProp(map, SpireDefName, center + new IntVec3(-14, 0, 14), Rot4.North);
        }

        private static void SpawnPerimeter(Map map)
        {
            int minX = 6;
            int maxX = map.Size.x - 7;
            int minZ = 6;
            int maxZ = map.Size.z - 7;

            for (int x = minX; x <= maxX; x += PerimeterStep)
            {
                SpawnProp(map, (x / PerimeterStep) % 2 == 0 ? BastionDefName : SpireDefName, new IntVec3(x, 0, minZ), Rot4.North);
                SpawnProp(map, (x / PerimeterStep) % 2 == 0 ? SpireDefName : BastionDefName, new IntVec3(x, 0, maxZ), Rot4.South);
            }

            for (int z = minZ + PerimeterStep; z < maxZ; z += PerimeterStep)
            {
                SpawnProp(map, (z / PerimeterStep) % 2 == 0 ? BastionDefName : SpireDefName, new IntVec3(minX, 0, z), Rot4.West);
                SpawnProp(map, (z / PerimeterStep) % 2 == 0 ? SpireDefName : BastionDefName, new IntVec3(maxX, 0, z), Rot4.East);
            }
        }

        private static void SpawnRoutePylons(Map map, IntVec3 entry, IntVec3 center, List<IntVec3> anchors, IntVec3 rewardPocket)
        {
            SpawnProp(map, BastionDefName, entry + new IntVec3(-9, 0, 0), Rot4.West);
            SpawnProp(map, BastionDefName, entry + new IntVec3(9, 0, 0), Rot4.East);
            SpawnProp(map, SpireDefName, center + new IntVec3(-10, 0, -8), Rot4.West);
            SpawnProp(map, SpireDefName, center + new IntVec3(10, 0, -8), Rot4.East);
            SpawnProp(map, BastionDefName, rewardPocket + new IntVec3(-7, 0, 0), Rot4.West);
            SpawnProp(map, BastionDefName, rewardPocket + new IntVec3(7, 0, 0), Rot4.East);

            for (int i = 0; i < anchors.Count; i++)
            {
                IntVec3 anchor = anchors[i];
                SpawnProp(map, SpireDefName, anchor + new IntVec3(-6, 0, 0), Rot4.West);
                SpawnProp(map, SpireDefName, anchor + new IntVec3(6, 0, 0), Rot4.East);
                SpawnProp(map, BastionDefName, anchor + new IntVec3(0, 0, 6), Rot4.North);
            }
        }

        private static void SpawnPad(Map map, IntVec3 cell)
        {
            SpawnProp(map, SigilPadDefName, cell, Rot4.North);
        }

        private static void SpawnProp(Map map, string defName, IntVec3 cell, Rot4 rotation)
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

                if (thing.def.passability == Traversability.Impassable || thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Building)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            Thing spawned = ThingMaker.MakeThing(def);
            if (spawned != null)
            {
                GenSpawn.Spawn(spawned, cell, map, rotation);
            }
        }
    }
}
