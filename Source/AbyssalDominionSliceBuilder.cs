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
            if (map.listerThings?.AllThings != null)
            {
                all.AddRange(map.listerThings.AllThings);
            }

            for (int i = all.Count - 1; i >= 0; i--)
            {
                Thing thing = all[i];
                if (thing == null || thing.Destroyed || thing is Pawn)
                {
                    continue;
                }

                if (thing.def.category == ThingCategory.Mote || thing.def.category == ThingCategory.Attachment)
                {
                    continue;
                }

                try
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[Abyssal Protocol] Dominion slice cleanup skipped {thing.LabelCap}: {ex.GetType().Name}");
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
            TerrainDef ashTerrain = ResolveTerrain("ABY_DominionAshMetal", "Concrete", "MetalTile");
            TerrainDef plateTerrain = ResolveTerrain("ABY_DominionScorchedPlate", "PavedTile", "Concrete");
            TerrainDef channelTerrain = ResolveTerrain("ABY_DominionBloodChannel", "MetalTile", "PavedTile");
            TerrainDef sigilTerrain = ResolveTerrain("ABY_DominionBrassSigil", "MetalTile", "SilverTile");

            PaintWholeMap(map, ashTerrain);

            IntVec3 center = ClampToInterior(map, map.Center + new IntVec3(0, 0, 4));
            IntVec3 entry = ClampToInterior(map, new IntVec3(center.x, 0, center.z - 40));
            IntVec3 extraction = ClampToInterior(map, entry + new IntVec3(0, 0, 5));
            IntVec3 anchorWest = ClampToInterior(map, new IntVec3(center.x - 26, 0, center.z + 10));
            IntVec3 anchorEast = ClampToInterior(map, new IntVec3(center.x + 25, 0, center.z + 12));
            IntVec3 anchorNorth = ClampToInterior(map, new IntVec3(center.x + 4, 0, center.z + 33));
            IntVec3 rewardPocket = ClampToInterior(map, new IntVec3(center.x - 32, 0, center.z - 8));

            session.pocketEntryCell = entry;
            session.extractionCell = extraction;
            session.heartCell = center;
            session.anchorCells = new List<IntVec3> { anchorWest, anchorEast, anchorNorth };

            PaintEntryCauseway(map, entry, extraction, center, plateTerrain, channelTerrain, sigilTerrain);
            PaintHeartDais(map, center, plateTerrain, channelTerrain, sigilTerrain);
            PaintAnchorPlatform(map, anchorWest, plateTerrain, channelTerrain, sigilTerrain, Rot4.West);
            PaintAnchorPlatform(map, anchorEast, plateTerrain, channelTerrain, sigilTerrain, Rot4.East);
            PaintAnchorPlatform(map, anchorNorth, plateTerrain, channelTerrain, sigilTerrain, Rot4.North);
            PaintRewardPocket(map, rewardPocket, plateTerrain, sigilTerrain);

            PaintCorridor(map, center + new IntVec3(-10, 0, 4), anchorWest, 3, plateTerrain);
            PaintCorridor(map, center + new IntVec3(10, 0, 4), anchorEast, 3, plateTerrain);
            PaintCorridor(map, center + new IntVec3(0, 0, 12), anchorNorth, 3, plateTerrain);
            PaintCorridor(map, center + new IntVec3(-16, 0, -2), rewardPocket, 2, plateTerrain);

            PaintCorridor(map, center + new IntVec3(-10, 0, 4), anchorWest, 1, channelTerrain);
            PaintCorridor(map, center + new IntVec3(10, 0, 4), anchorEast, 1, channelTerrain);
            PaintCorridor(map, center + new IntVec3(0, 0, 12), anchorNorth, 1, channelTerrain);
            PaintCorridor(map, extraction, center + new IntVec3(0, 0, -14), 1, channelTerrain);

            SpawnPad(map, extraction);
            SpawnPad(map, center);
            SpawnPad(map, anchorWest);
            SpawnPad(map, anchorEast);
            SpawnPad(map, anchorNorth);
            SpawnPad(map, rewardPocket);

            SpawnPerimeterShell(map, center);
            SpawnLaneSupports(map, extraction, center, anchorWest, anchorEast, anchorNorth, rewardPocket);
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
            int x = Mathf.Clamp(cell.x, 8, map.Size.x - 9);
            int z = Mathf.Clamp(cell.z, 8, map.Size.z - 9);
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

        private static void PaintEntryCauseway(Map map, IntVec3 entry, IntVec3 extraction, IntVec3 center, TerrainDef plate, TerrainDef channel, TerrainDef sigil)
        {
            PaintRect(map, new CellRect(entry.x - 8, entry.z - 4, 16, 11), plate);
            PaintRect(map, new CellRect(entry.x - 4, entry.z - 2, 8, 7), sigil);
            PaintCircle(map, extraction, 4, sigil);
            PaintCorridor(map, extraction, center + new IntVec3(0, 0, -14), 4, plate);
            PaintCorridor(map, extraction, center + new IntVec3(0, 0, -14), 1, channel);
        }

        private static void PaintHeartDais(Map map, IntVec3 center, TerrainDef plate, TerrainDef channel, TerrainDef sigil)
        {
            PaintCircle(map, center, 14, plate);
            PaintCircle(map, center, 10, sigil);
            PaintCircle(map, center, 6, plate);
            PaintCircle(map, center, 3, sigil);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                IntVec3 tip = new IntVec3(center.x + GenMath.RoundRandom(Mathf.Cos(angle) * 14f), 0, center.z + GenMath.RoundRandom(Mathf.Sin(angle) * 14f));
                PaintCorridor(map, center, tip, i % 2 == 0 ? 1 : 0, channel);
            }
        }

        private static void PaintAnchorPlatform(Map map, IntVec3 center, TerrainDef plate, TerrainDef channel, TerrainDef sigil, Rot4 facing)
        {
            PaintCircle(map, center, 6, plate);
            PaintCircle(map, center, 4, sigil);
            PaintCircle(map, center, 2, plate);
            IntVec3 front = center + facing.FacingCell * 3;
            PaintCorridor(map, center, front, 1, channel);
        }

        private static void PaintRewardPocket(Map map, IntVec3 center, TerrainDef plate, TerrainDef sigil)
        {
            PaintRect(map, new CellRect(center.x - 6, center.z - 5, 12, 10), plate);
            PaintRect(map, new CellRect(center.x - 3, center.z - 2, 6, 4), sigil);
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

        private static void PaintCorridor(Map map, IntVec3 from, IntVec3 to, int halfWidth, TerrainDef terrain)
        {
            int steps = Mathf.Max(Math.Abs(to.x - from.x), Math.Abs(to.z - from.z));
            if (steps <= 0)
            {
                PaintCircle(map, from, Math.Max(halfWidth, 0), terrain);
                return;
            }

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = GenMath.RoundRandom(Mathf.Lerp(from.x, to.x, t));
                int z = GenMath.RoundRandom(Mathf.Lerp(from.z, to.z, t));
                PaintCircle(map, new IntVec3(x, 0, z), Math.Max(halfWidth, 0), terrain);
            }
        }

        private static void SpawnPerimeterShell(Map map, IntVec3 center)
        {
            SpawnArc(map, center, 34f, 198f, 338f, 20f, BastionDefName);
            SpawnArc(map, center, 42f, 210f, 324f, 28f, SpireDefName);
            SpawnArc(map, center, 29f, 32f, 146f, 23f, SpireDefName);
        }

        private static void SpawnLaneSupports(Map map, IntVec3 extraction, IntVec3 center, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket)
        {
            SpawnProp(map, BastionDefName, extraction + new IntVec3(-7, 0, 1), Rot4.West);
            SpawnProp(map, BastionDefName, extraction + new IntVec3(7, 0, 1), Rot4.East);
            SpawnProp(map, SpireDefName, center + new IntVec3(-15, 0, -6), Rot4.West);
            SpawnProp(map, SpireDefName, center + new IntVec3(15, 0, -6), Rot4.East);
            SpawnProp(map, SpireDefName, center + new IntVec3(-15, 0, 11), Rot4.West);
            SpawnProp(map, SpireDefName, center + new IntVec3(15, 0, 11), Rot4.East);
            SpawnProp(map, BastionDefName, north + new IntVec3(-4, 0, 6), Rot4.North);
            SpawnProp(map, BastionDefName, north + new IntVec3(4, 0, 6), Rot4.North);
            SpawnProp(map, BastionDefName, west + new IntVec3(-6, 0, 2), Rot4.West);
            SpawnProp(map, BastionDefName, east + new IntVec3(6, 0, 2), Rot4.East);
            SpawnProp(map, SpireDefName, rewardPocket + new IntVec3(-4, 0, 4), Rot4.West);
            SpawnProp(map, SpireDefName, rewardPocket + new IntVec3(4, 0, -4), Rot4.East);
        }

        private static void SpawnArc(Map map, IntVec3 center, float radius, float startDeg, float endDeg, float stepDeg, string defName)
        {
            for (float angle = startDeg; angle <= endDeg; angle += stepDeg)
            {
                float rad = angle * Mathf.Deg2Rad;
                int x = center.x + GenMath.RoundRandom(Mathf.Cos(rad) * radius);
                int z = center.z + GenMath.RoundRandom(Mathf.Sin(rad) * radius);
                IntVec3 cell = ClampToInterior(map, new IntVec3(x, 0, z));
                Rot4 rot = Mathf.Abs(Mathf.Cos(rad)) > Mathf.Abs(Mathf.Sin(rad))
                    ? (Mathf.Cos(rad) > 0f ? Rot4.East : Rot4.West)
                    : (Mathf.Sin(rad) > 0f ? Rot4.North : Rot4.South);
                SpawnProp(map, defName, cell, rot);
            }
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
                if (thing == null || thing.Destroyed || thing is Pawn)
                {
                    continue;
                }

                if (thing.def.category == ThingCategory.Mote || thing.def.category == ThingCategory.Attachment)
                {
                    continue;
                }

                try
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
                catch
                {
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
