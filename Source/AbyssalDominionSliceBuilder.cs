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
                if (thing == null || thing.Destroyed || thing is Pawn)
                {
                    continue;
                }

                if (thing.def.category == ThingCategory.Mote || thing.def.category == ThingCategory.Attachment)
                {
                    continue;
                }

                if (ShouldPreserveThing(thing))
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
            TerrainDef baseTerrain = ResolveTerrain("ABY_DominionAshMetal", "Concrete", "MetalTile");
            TerrainDef laneTerrain = ResolveTerrain("ABY_DominionBloodChannel", "MetalTile", "PavedTile");
            TerrainDef focalTerrain = ResolveTerrain("ABY_DominionBrassSigil", "SilverTile", "SterileTile");
            TerrainDef supportTerrain = ResolveTerrain("ABY_DominionScorchedPlate", "Concrete", "PavedTile");

            PaintWholeMap(map, baseTerrain);

            IntVec3 center = map.Center + new IntVec3(0, 0, 2);
            IntVec3 entry = ClampToInterior(map, new IntVec3(center.x - 2, 0, 15));
            IntVec3 extraction = entry + new IntVec3(0, 0, 3);
            IntVec3 anchorA = ClampToInterior(map, new IntVec3(center.x - 26, 0, center.z + 18));
            IntVec3 anchorB = ClampToInterior(map, new IntVec3(center.x + 24, 0, center.z + 14));
            IntVec3 anchorC = ClampToInterior(map, new IntVec3(center.x + 4, 0, center.z - 27));
            IntVec3 sidePocket = ClampToInterior(map, new IntVec3(center.x - 31, 0, center.z - 16));

            session.pocketEntryCell = entry;
            session.extractionCell = extraction;
            session.heartCell = center;
            session.anchorCells = new List<IntVec3> { anchorA, anchorB, anchorC };

            PaintDock(map, entry, extraction, laneTerrain, supportTerrain);
            PaintCentralDais(map, center, focalTerrain, laneTerrain);
            PaintAnchorPlatform(map, anchorA, focalTerrain, laneTerrain);
            PaintAnchorPlatform(map, anchorB, focalTerrain, laneTerrain);
            PaintAnchorPlatform(map, anchorC, focalTerrain, laneTerrain);
            PaintSidePocket(map, sidePocket, supportTerrain, laneTerrain);

            PaintCorridor(map, extraction, center + new IntVec3(0, 0, -9), 4, laneTerrain);
            PaintCorridor(map, center + new IntVec3(-5, 0, 5), anchorA, 3, laneTerrain);
            PaintCorridor(map, center + new IntVec3(5, 0, 5), anchorB, 3, laneTerrain);
            PaintCorridor(map, center + new IntVec3(0, 0, -7), anchorC, 3, laneTerrain);
            PaintCorridor(map, center + new IntVec3(-10, 0, -4), sidePocket, 2, supportTerrain);

            SpawnPad(map, extraction);
            SpawnPad(map, center);
            SpawnPad(map, anchorA);
            SpawnPad(map, anchorB);
            SpawnPad(map, anchorC);
            SpawnPad(map, sidePocket);

            SpawnShellFragments(map, center);
            SpawnLaneSupports(map, extraction, center, anchorA, anchorB, anchorC, sidePocket);
        }

        private static bool ShouldPreserveThing(Thing thing)
        {
            if (thing == null || thing.def == null)
            {
                return true;
            }

            string defName = thing.def.defName ?? string.Empty;
            if (defName.Contains("CaveExit") || defName.Contains("PocketMapExit") || defName.Contains("PitGate"))
            {
                return true;
            }

            return false;
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

        private static void PaintDock(Map map, IntVec3 entry, IntVec3 extraction, TerrainDef laneTerrain, TerrainDef supportTerrain)
        {
            PaintRect(map, new CellRect(entry.x - 8, entry.z - 3, 16, 11), supportTerrain);
            PaintRect(map, new CellRect(entry.x - 5, entry.z - 1, 10, 7), laneTerrain);
            PaintCircle(map, extraction, 4, laneTerrain);
        }

        private static void PaintCentralDais(Map map, IntVec3 center, TerrainDef focalTerrain, TerrainDef laneTerrain)
        {
            PaintRect(map, new CellRect(center.x - 12, center.z - 10, 24, 20), laneTerrain);
            PaintRect(map, new CellRect(center.x - 8, center.z - 8, 16, 16), focalTerrain);
            PaintCircle(map, center, 6, focalTerrain);
            PaintCircle(map, center, 10, laneTerrain);
            PaintRect(map, new CellRect(center.x - 2, center.z - 16, 4, 10), laneTerrain);
            PaintRect(map, new CellRect(center.x - 14, center.z - 2, 8, 4), laneTerrain);
            PaintRect(map, new CellRect(center.x + 6, center.z - 2, 8, 4), laneTerrain);
            PaintRect(map, new CellRect(center.x - 2, center.z + 6, 4, 8), laneTerrain);
        }

        private static void PaintAnchorPlatform(Map map, IntVec3 center, TerrainDef focalTerrain, TerrainDef laneTerrain)
        {
            PaintRect(map, new CellRect(center.x - 4, center.z - 4, 8, 8), laneTerrain);
            PaintCircle(map, center, 5, laneTerrain);
            PaintCircle(map, center, 3, focalTerrain);
        }

        private static void PaintSidePocket(Map map, IntVec3 center, TerrainDef supportTerrain, TerrainDef laneTerrain)
        {
            PaintRect(map, new CellRect(center.x - 7, center.z - 5, 14, 10), supportTerrain);
            PaintRect(map, new CellRect(center.x - 4, center.z - 3, 8, 6), laneTerrain);
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

        private static void SpawnShellFragments(Map map, IntVec3 center)
        {
            SpawnArc(map, center, 50f, 190f, 327f, 11f, BastionDefName);
            SpawnArc(map, center, 41f, 212f, 294f, 13f, BastionDefName);
            SpawnArc(map, center, 34f, 38f, 149f, 16f, SpireDefName);
            SpawnArc(map, center, 28f, 205f, 338f, 18f, SpireDefName);

            SpawnProp(map, BastionDefName, center + new IntVec3(-34, 0, 4), Rot4.West);
            SpawnProp(map, BastionDefName, center + new IntVec3(33, 0, -2), Rot4.East);
            SpawnProp(map, BastionDefName, center + new IntVec3(-10, 0, 32), Rot4.North);
            SpawnProp(map, BastionDefName, center + new IntVec3(8, 0, -34), Rot4.South);
        }

        private static void SpawnLaneSupports(Map map, IntVec3 extraction, IntVec3 center, IntVec3 anchorA, IntVec3 anchorB, IntVec3 anchorC, IntVec3 sidePocket)
        {
            SpawnProp(map, BastionDefName, extraction + new IntVec3(-7, 0, 0), Rot4.West);
            SpawnProp(map, BastionDefName, extraction + new IntVec3(7, 0, 0), Rot4.East);

            SpawnProp(map, SpireDefName, center + new IntVec3(-13, 0, 11), Rot4.North);
            SpawnProp(map, SpireDefName, center + new IntVec3(13, 0, 11), Rot4.North);
            SpawnProp(map, SpireDefName, center + new IntVec3(-15, 0, -8), Rot4.South);
            SpawnProp(map, SpireDefName, center + new IntVec3(15, 0, -9), Rot4.South);

            SpawnFlankPair(map, anchorA, 5);
            SpawnFlankPair(map, anchorB, 5);
            SpawnFlankPair(map, anchorC, 5);

            SpawnProp(map, BastionDefName, sidePocket + new IntVec3(-5, 0, 4), Rot4.West);
            SpawnProp(map, BastionDefName, sidePocket + new IntVec3(6, 0, -4), Rot4.East);
        }

        private static void SpawnFlankPair(Map map, IntVec3 center, int offset)
        {
            SpawnProp(map, SpireDefName, center + new IntVec3(-offset, 0, 0), Rot4.West);
            SpawnProp(map, SpireDefName, center + new IntVec3(offset, 0, 0), Rot4.East);
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

                thing.Destroy(DestroyMode.Vanish);
            }

            Thing spawned = ThingMaker.MakeThing(def);
            if (spawned != null)
            {
                GenSpawn.Spawn(spawned, cell, map, rot);
            }
        }
    }
}
