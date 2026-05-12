using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionSliceBuilder
    {
        private const string SigilPadDefName = "ABY_DominionSliceSigilPad";
        private const string PerimeterWallDefName = "ABY_DominionSlicePerimeterWall";
        private const string HeartPlatformUnderlayDefName = "ABY_DominionHeartPlatformUnderlay";
        private const string AnchorPlatformUnderlaySealDefName = "ABY_DominionAnchorPlatformUnderlay_Seal";
        private const string AnchorPlatformUnderlayChoirDefName = "ABY_DominionAnchorPlatformUnderlay_Choir";
        private const string AnchorPlatformUnderlayLawDefName = "ABY_DominionAnchorPlatformUnderlay_Law";

        private const string HeartFloorFractureDefName = "ABY_DominionHeartFloorFractureLarge";
        private const string EntrySeamScarDefName = "ABY_DominionEntrySeamScarSouth";
        private const string SideRoomFloorUnderlayDefName = "ABY_DominionSideRoomFloorUnderlayA";
        private const string SideRoomMachineBayWestDefName = "ABY_DominionSideRoomMachineBayWest";
        private const string SideRoomReliquaryBayEastDefName = "ABY_DominionSideRoomReliquaryBayEast";
        private const string BackWallShellNorthDefName = "ABY_DominionBackWallShellNorth";
        private const string EdgeMegastructureStraightDefName = "ABY_DominionEdgeMegastructureStraightA";
        private const string EdgeMegastructureCornerDefName = "ABY_DominionEdgeMegastructureCornerA";
        private const string MachineWreckReactorRibDefName = "ABY_DominionMachineWreckReactorRibA";
        private const string BrokenConduitSpineDefName = "ABY_DominionBrokenConduitSpineA";
        private const string PlateCollapseLargeDefName = "ABY_DominionPlateCollapseLargeA";
        private const string EdgeVoidTearDefName = "ABY_DominionEdgeVoidTearA";

        private const string DominionFissureStraightDefName = "ABY_DominionFissureStraight";
        private const string DominionFissureCornerDefName = "ABY_DominionFissureCorner";
        private const string DominionFissureEndcapDefName = "ABY_DominionFissureEndcap";
        private const string DominionFissureNodeDefName = "ABY_DominionFissureNode";
        private const string DominionFissureBlockerDefName = "ABY_DominionFissureBlocker";
        private const string DominionFissurePathNorthWestDefName = "ABY_DominionFissurePathNorthWest";
        private const string DominionFissurePathNorthEastDefName = "ABY_DominionFissurePathNorthEast";
        private const string DominionFissurePathWestMidDefName = "ABY_DominionFissurePathWestMid";
        private const string DominionFissurePathShortWestMidDefName = "ABY_DominionFissurePathShortWestMid";
        private const string DominionFissurePathSouthEastDefName = "ABY_DominionFissurePathSouthEast";
        private const string DominionFissurePathSouthWestDefName = "ABY_DominionFissurePathSouthWest";

        private static readonly string[] DominionRuinWallDefs =
        {
            "ABY_DominionRuinWallStraightA",
            "ABY_DominionRuinWallStraightBrokenA",
            "ABY_DominionRuinWallCornerA",
            "ABY_DominionRuinWallCornerBrokenA",
            "ABY_DominionRuinWallTJunctionA",
            "ABY_DominionRuinWallTJunctionBrokenA",
            "ABY_DominionPerimeterBarricadeA",
            "ABY_DominionPerimeterBarricadeBrokenA",
            "ABY_DominionPerimeterCollapseA",
            "ABY_DominionPerimeterCollapseB"
        };

        private static readonly string[] DominionRubbleDefs =
        {
            "ABY_DominionRubblePileA",
            "ABY_DominionPlateClusterA",
            "ABY_DominionPlateClusterB",
            "ABY_DominionConduitScrapA",
            "ABY_DominionConduitScrapB",
            "ABY_DominionIndustrialWreckageA",
            "ABY_DominionReactorRingFragmentA"
        };

        private static readonly string[] DominionDecalDefs =
        {
            "ABY_DominionSootPatchA",
            "ABY_DominionScorchCraterA",
            "ABY_DominionAshPatchA",
            "ABY_DominionAshPatchB",
            "ABY_DominionBrokenPlateDustA",
            "ABY_DominionDarkScatterA",
            "ABY_DominionDustClusterA",
            "ABY_DominionExcavationScarA",
            "ABY_DominionExcavationScarB",
            "ABY_DominionSootMoundA"
        };


        private sealed class DominionFissurePlacement
        {
            public string visualDefName;
            public IntVec3 visualCell;
            public IntVec3[] pathCells;
            public int supportHalfWidth;

            public DominionFissurePlacement(string visualDefName, IntVec3 visualCell, int supportHalfWidth, params IntVec3[] pathCells)
            {
                this.visualDefName = visualDefName;
                this.visualCell = visualCell;
                this.supportHalfWidth = supportHalfWidth;
                this.pathCells = pathCells ?? new IntVec3[0];
            }
        }

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

                if (thing.def == null)
                {
                    continue;
                }

                if (thing.def.category == ThingCategory.Mote || thing.def.category == ThingCategory.Attachment)
                {
                    continue;
                }

                string defName = thing.def.defName ?? string.Empty;
                if (defName == "PocketMapExit" || defName == "CaveExit" || defName == "PitGate")
                {
                    continue;
                }

                if (!thing.def.destroyable)
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

                if (map.terrainGrid?.TerrainAt(cell) != null && map.terrainGrid.TerrainAt(cell).IsWater)
                {
                    map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
                }
            }
        }

        private static void BuildLayout(Map map, ABY_DominionPocketSession session)
        {
            TerrainDef baseTerrain = ResolveTerrain("ABY_DominionAshMetal", "Concrete");
            TerrainDef plateTerrain = ResolveTerrain("ABY_DominionScorchedPlate", "PavedTile", "Concrete");
            TerrainDef channelTerrain = ResolveTerrain("ABY_DominionBloodChannel", "MetalTile", "PavedTile");
            TerrainDef sigilTerrain = ResolveTerrain("ABY_DominionBrassSigil", "MetalTile", "PavedTile");
            TerrainDef[] fissureSupportTerrains = ResolveTerrainSet(
                "ABY_DominionFissureSupport_A01", "ABY_DominionFissureSupport_A02",
                "ABY_DominionFissureSupport_A03", "ABY_DominionFissureSupport_A04",
                "ABY_DominionFissureSupport_A05", "ABY_DominionFissureSupport_A06",
                "ABY_DominionFissureSupport_A07", "ABY_DominionFissureSupport_A08",
                "ABY_DominionFissureSupport_A09", "ABY_DominionFissureSupport_A10",
                "ABY_DominionFissureSupport_A11", "ABY_DominionFissureSupport_A12",
                "ABY_DominionFissureSupport_A13", "ABY_DominionFissureSupport_A14",
                "ABY_DominionFissureSupport_A15", "ABY_DominionFissureSupport_A16");

            PaintWholeMap(map, baseTerrain);
            ScatterQuietFloorVariation(map, map.Center, baseTerrain, plateTerrain);
            SpawnEdgePerimeterWalls(map, 3);

            IntVec3 center = ClampToInterior(map, map.Center + new IntVec3(0, 0, 4));
            IntVec3 entry = ClampToInterior(map, new IntVec3(center.x, 0, center.z - 42));
            IntVec3 extraction = ClampToInterior(map, entry + new IntVec3(0, 0, 7));

            IntVec3 anchorWest = ClampToInterior(map, new IntVec3(center.x - 30, 0, center.z + 7));
            IntVec3 anchorEast = ClampToInterior(map, new IntVec3(center.x + 26, 0, center.z + 13));
            IntVec3 anchorNorth = ClampToInterior(map, new IntVec3(center.x + 4, 0, center.z + 33));
            IntVec3 rewardPocket = ClampToInterior(map, new IntVec3(center.x - 36, 0, center.z - 9));

            session.pocketEntryCell = entry;
            session.extractionCell = extraction;
            session.heartCell = center;
            session.anchorCells = new List<IntVec3> { anchorWest, anchorEast, anchorNorth };

            PaintPerimeterVoid(map, center, 54, 48, baseTerrain);
            PaintEntryBridge(map, entry, extraction, center, plateTerrain, channelTerrain, sigilTerrain);
            PaintHeartDais(map, center, plateTerrain, channelTerrain, sigilTerrain);
            PaintAnchorPlatform(map, anchorWest, plateTerrain, channelTerrain, sigilTerrain, Rot4.West);
            PaintAnchorPlatform(map, anchorEast, plateTerrain, channelTerrain, sigilTerrain, Rot4.East);
            PaintAnchorPlatform(map, anchorNorth, plateTerrain, channelTerrain, sigilTerrain, Rot4.North);
            PaintRewardPocket(map, rewardPocket, plateTerrain, sigilTerrain);

            PaintCorridor(map, center + new IntVec3(-13, 0, 6), anchorWest, 4, plateTerrain);
            PaintCorridor(map, center + new IntVec3(13, 0, 6), anchorEast, 4, plateTerrain);
            PaintCorridor(map, center + new IntVec3(0, 0, 15), anchorNorth, 4, plateTerrain);
            PaintCorridor(map, center + new IntVec3(-18, 0, -4), rewardPocket, 3, plateTerrain);

            PaintCorridor(map, center + new IntVec3(-10, 0, 4), anchorWest, 1, channelTerrain);
            PaintCorridor(map, center + new IntVec3(10, 0, 4), anchorEast, 1, channelTerrain);
            PaintCorridor(map, center + new IntVec3(0, 0, 12), anchorNorth, 1, channelTerrain);
            PaintCorridor(map, extraction, center + new IntVec3(0, 0, -15), 1, channelTerrain);

            PaintBrokenFactoryFloorStrips(map, center, extraction, anchorWest, anchorEast, anchorNorth, rewardPocket, plateTerrain, channelTerrain);
            PaintSideArchitectureFootprints(map, center, extraction, anchorWest, anchorEast, anchorNorth, plateTerrain, channelTerrain);
            // Objective platform underlays are drawn directly by the heart/anchor buildings.
            // Spawning same-cell underlay buildings was unreliable because RimWorld can wipe or hide
            // lower buildings when the real objective building is spawned later by the encounter component.
            ScatterBurnScars(map, center, plateTerrain, baseTerrain);
            PaintFissureSupportTerrainNetwork(map, center, fissureSupportTerrains);
            // Objective cells now use dedicated industrial underlays. Keep only non-objective pads.
            SpawnPad(map, extraction);
            SpawnPad(map, rewardPocket);

            SpawnPerimeterShell(map, center);
            SpawnLaneSupports(map, extraction, center, anchorWest, anchorEast, anchorNorth, rewardPocket);
            SpawnDecorativeDressings(map, entry, extraction, center, anchorWest, anchorEast, anchorNorth, rewardPocket);
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


        private static TerrainDef[] ResolveTerrainSet(params string[] names)
        {
            List<TerrainDef> defs = new List<TerrainDef>();
            if (names != null)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    TerrainDef def = DefDatabase<TerrainDef>.GetNamedSilentFail(names[i]);
                    if (def != null && !defs.Contains(def))
                    {
                        defs.Add(def);
                    }
                }
            }

            return defs.ToArray();
        }

        private static void PaintFissureSupportTerrainNetwork(Map map, IntVec3 center, TerrainDef[] supportTerrains)
        {
            if (map == null || supportTerrains == null || supportTerrains.Length == 0)
            {
                return;
            }

            List<DominionFissurePlacement> placements = BuildDominionFissurePlacements(map, center);
            for (int i = 0; i < placements.Count; i++)
            {
                DominionFissurePlacement placement = placements[i];
                if (placement == null)
                {
                    continue;
                }

                PaintFissureSupportPath(map, supportTerrains, placement.supportHalfWidth, placement.pathCells);
            }
        }

        private static void PaintFissureSupportPath(Map map, TerrainDef[] supportTerrains, int halfWidth, params IntVec3[] pathCells)
        {
            if (map == null || supportTerrains == null || supportTerrains.Length == 0 || pathCells == null || pathCells.Length < 2)
            {
                return;
            }

            int safeHalfWidth = Mathf.Clamp(halfWidth, 1, 3);
            for (int i = 0; i < pathCells.Length - 1; i++)
            {
                IntVec3 a = pathCells[i];
                IntVec3 b = pathCells[i + 1];
                int dx = b.x - a.x;
                int dz = b.z - a.z;
                if (dx != 0 && dz != 0)
                {
                    continue;
                }

                int distance = Mathf.Max(Math.Abs(dx), Math.Abs(dz));
                int dirX = dx == 0 ? 0 : Math.Sign(dx);
                int dirZ = dz == 0 ? 0 : Math.Sign(dz);
                for (int d = 0; d <= distance; d++)
                {
                    IntVec3 cell = new IntVec3(a.x + dirX * d, 0, a.z + dirZ * d);
                    for (int w = -safeHalfWidth; w <= safeHalfWidth; w++)
                    {
                        bool edgeBand = Math.Abs(w) == safeHalfWidth;
                        if (edgeBand)
                        {
                            uint edgeHash = FissureCellHash(cell + new IntVec3(w, 0, safeHalfWidth));
                            if ((edgeHash & 3u) == 0u)
                            {
                                continue;
                            }
                        }

                        IntVec3 widened = dirX != 0
                            ? new IntVec3(cell.x, 0, cell.z + w)
                            : new IntVec3(cell.x + w, 0, cell.z);
                        PaintFissureSupportCell(map, widened, supportTerrains);
                    }

                    if (d % 5 == 0)
                    {
                        int spurSide = ((int)(FissureCellHash(cell) % 3u)) - 1;
                        if (spurSide != 0)
                        {
                            IntVec3 spur = dirX != 0
                                ? new IntVec3(cell.x, 0, cell.z + (safeHalfWidth + spurSide))
                                : new IntVec3(cell.x + (safeHalfWidth + spurSide), 0, cell.z);
                            PaintFissureSupportCell(map, spur, supportTerrains);
                        }
                    }
                }

                PaintFissureSupportPatch(map, a, supportTerrains, safeHalfWidth);
                PaintFissureSupportPatch(map, b, supportTerrains, safeHalfWidth);
            }
        }

        private static void PaintFissureSupportPatch(Map map, IntVec3 center, TerrainDef[] supportTerrains, int radius)
        {
            int safeRadius = Mathf.Max(1, radius);
            for (int dx = -safeRadius; dx <= safeRadius; dx++)
            {
                for (int dz = -safeRadius; dz <= safeRadius; dz++)
                {
                    int distSq = dx * dx + dz * dz;
                    if (distSq > safeRadius * safeRadius + 1)
                    {
                        continue;
                    }

                    if (distSq > 1 && (FissureCellHash(center + new IntVec3(dx, 0, dz)) & 3u) == 0u)
                    {
                        continue;
                    }

                    PaintFissureSupportCell(map, center + new IntVec3(dx, 0, dz), supportTerrains);
                }
            }
        }

        private static void PaintFissureSupportCell(Map map, IntVec3 cell, TerrainDef[] supportTerrains)
        {
            if (map == null || supportTerrains == null || supportTerrains.Length == 0)
            {
                return;
            }

            cell = ClampToInterior(map, cell, 4);
            if (!cell.InBounds(map))
            {
                return;
            }

            map.terrainGrid.SetTerrain(cell, PickFissureSupportTerrain(supportTerrains, cell));
        }

        private static TerrainDef PickFissureSupportTerrain(TerrainDef[] supportTerrains, IntVec3 cell)
        {
            if (supportTerrains == null || supportTerrains.Length == 0)
            {
                return TerrainDefOf.Concrete;
            }

            uint hash = FissureCellHash(cell);
            int index = (int)(hash % (uint)supportTerrains.Length);
            return supportTerrains[index] ?? supportTerrains[0];
        }

        private static uint FissureCellHash(IntVec3 cell)
        {
            unchecked
            {
                uint hash = (uint)(cell.x * 73856093) ^ (uint)(cell.z * 19349663) ^ 0x9E3779B9u;
                hash ^= hash >> 13;
                hash *= 1274126177u;
                hash ^= hash >> 16;
                return hash;
            }
        }

        private static IntVec3[] OffsetPath(IntVec3 jitter, params IntVec3[] offsets)
        {
            if (offsets == null)
            {
                return new IntVec3[0];
            }

            IntVec3[] shifted = new IntVec3[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                shifted[i] = offsets[i] + jitter;
            }

            return shifted;
        }

        private static IntVec3 FissureJitter(Map map, int index, int maxX, int maxZ)
        {
            int tile = map != null ? map.Tile : 0;
            unchecked
            {
                uint hash = (uint)(tile * 92837111) ^ (uint)(index * 689287499) ^ 0x6D2B79F5u;
                int xRange = Mathf.Max(0, maxX) * 2 + 1;
                int zRange = Mathf.Max(0, maxZ) * 2 + 1;
                int x = xRange <= 1 ? 0 : (int)(hash % (uint)xRange) - maxX;
                int z = zRange <= 1 ? 0 : (int)((hash >> 8) % (uint)zRange) - maxZ;
                return new IntVec3(x, 0, z);
            }
        }

        private static bool ShouldUseOptionalFissure(Map map, int index, int chancePercent)
        {
            int tile = map != null ? map.Tile : 0;
            unchecked
            {
                uint hash = (uint)(tile * 1103515245) ^ (uint)(index * 12345) ^ 0xA5A5A5A5u;
                return (hash % 100u) < Mathf.Clamp(chancePercent, 0, 100);
            }
        }


        private static List<DominionFissurePlacement> BuildDominionFissurePlacements(Map map, IntVec3 center)
        {
            List<DominionFissurePlacement> placements = new List<DominionFissurePlacement>();
            if (map == null)
            {
                return placements;
            }

            int leftWallX = 4;
            int rightWallX = map.Size.x - 5;
            int bottomWallZ = 4;
            int topWallZ = map.Size.z - 5;

            int westLanePrimary = Mathf.Clamp(center.z - 18 + FissureJitter(map, 31, 0, 3).z, bottomWallZ + 14, center.z - 9);
            int eastLanePrimary = Mathf.Clamp(center.z - 21 + FissureJitter(map, 32, 0, 3).z, bottomWallZ + 12, center.z - 11);
            int lowLane = Mathf.Clamp(center.z - 40 + FissureJitter(map, 33, 0, 4).z, bottomWallZ + 9, center.z - 28);

            placements.Add(CreateNorthWestWallFissure(leftWallX, topWallZ));
            placements.Add(CreateSouthEastWallFissure(rightWallX, bottomWallZ));

            if (ShouldUseOptionalFissure(map, 34, 58))
            {
                placements.Add(CreateNorthEastWallFissure(rightWallX, topWallZ));
            }

            if (ShouldUseOptionalFissure(map, 35, 44))
            {
                placements.Add(CreateSouthWestWallFissure(leftWallX, bottomWallZ));
            }

            placements.Add(CreateWestWallRun(leftWallX, westLanePrimary, 30));

            if (ShouldUseOptionalFissure(map, 36, 72))
            {
                placements.Add(CreateEastWallRun(rightWallX, eastLanePrimary, 30));
            }

            if (ShouldUseOptionalFissure(map, 37, 57))
            {
                placements.Add(ShouldUseOptionalFissure(map, 38, 50)
                    ? CreateWestWallRun(leftWallX, lowLane, 28)
                    : CreateEastWallRun(rightWallX, lowLane, 28));
            }

            return placements;
        }

        private static DominionFissurePlacement CreateNorthWestWallFissure(int leftWallX, int topWallZ)
        {
            IntVec3 top = new IntVec3(leftWallX + 18, 0, topWallZ);
            IntVec3 junction = new IntVec3(leftWallX + 18, 0, topWallZ - 18);
            IntVec3 wall = new IntVec3(leftWallX, 0, topWallZ - 18);
            IntVec3 visual = new IntVec3(top.x, 0, topWallZ - 14);
            return new DominionFissurePlacement(DominionFissurePathNorthWestDefName, visual, 2, top, junction, wall);
        }

        private static DominionFissurePlacement CreateNorthEastWallFissure(int rightWallX, int topWallZ)
        {
            IntVec3 top = new IntVec3(rightWallX - 19, 0, topWallZ);
            IntVec3 junction = new IntVec3(rightWallX - 19, 0, topWallZ - 18);
            IntVec3 wall = new IntVec3(rightWallX, 0, topWallZ - 18);
            IntVec3 visual = new IntVec3(top.x, 0, topWallZ - 14);
            return new DominionFissurePlacement(DominionFissurePathNorthEastDefName, visual, 2, top, junction, wall);
        }

        private static DominionFissurePlacement CreateSouthWestWallFissure(int leftWallX, int bottomWallZ)
        {
            IntVec3 bottom = new IntVec3(leftWallX + 13, 0, bottomWallZ);
            IntVec3 junction = new IntVec3(leftWallX + 13, 0, bottomWallZ + 16);
            IntVec3 wall = new IntVec3(leftWallX, 0, bottomWallZ + 16);
            IntVec3 visual = new IntVec3(bottom.x, 0, bottomWallZ + 14);
            return new DominionFissurePlacement(DominionFissurePathSouthWestDefName, visual, 2, bottom, junction, wall);
        }

        private static DominionFissurePlacement CreateSouthEastWallFissure(int rightWallX, int bottomWallZ)
        {
            IntVec3 bottom = new IntVec3(rightWallX - 15, 0, bottomWallZ);
            IntVec3 junction = new IntVec3(rightWallX - 15, 0, bottomWallZ + 19);
            IntVec3 wall = new IntVec3(rightWallX, 0, bottomWallZ + 19);
            IntVec3 visual = new IntVec3(bottom.x, 0, bottomWallZ + 14);
            return new DominionFissurePlacement(DominionFissurePathSouthEastDefName, visual, 2, bottom, junction, wall);
        }

        private static DominionFissurePlacement CreateWestWallRun(int leftWallX, int z, int length)
        {
            int safeLength = Mathf.Max(24, length);
            IntVec3 start = new IntVec3(leftWallX, 0, z);
            IntVec3 end = new IntVec3(leftWallX + safeLength, 0, z);
            IntVec3 visual = new IntVec3(leftWallX + Mathf.RoundToInt(safeLength * 0.5f), 0, z);
            return new DominionFissurePlacement(DominionFissurePathWestMidDefName, visual, 2, start, end);
        }

        private static DominionFissurePlacement CreateEastWallRun(int rightWallX, int z, int length)
        {
            int safeLength = Mathf.Max(24, length);
            IntVec3 start = new IntVec3(rightWallX - safeLength, 0, z);
            IntVec3 end = new IntVec3(rightWallX, 0, z);
            IntVec3 visual = new IntVec3(rightWallX - Mathf.RoundToInt(safeLength * 0.5f), 0, z);
            return new DominionFissurePlacement(DominionFissurePathWestMidDefName, visual, 2, start, end);
        }

        private static IntVec3 ClampToInterior(Map map, IntVec3 cell)
        {
            return ClampToInterior(map, cell, 8);
        }

        private static IntVec3 ClampToInterior(Map map, IntVec3 cell, int margin)
        {
            if (map == null)
            {
                return cell;
            }

            int safeMargin = Mathf.Clamp(margin, 1, Mathf.Max(1, Mathf.Min(map.Size.x, map.Size.z) / 2 - 1));
            int x = Mathf.Clamp(cell.x, safeMargin, map.Size.x - safeMargin - 1);
            int z = Mathf.Clamp(cell.z, safeMargin, map.Size.z - safeMargin - 1);
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

        private static void PaintPerimeterVoid(Map map, IntVec3 center, int outerRadius, int innerRadius, TerrainDef terrain)
        {
            foreach (IntVec3 cell in CellRect.CenteredOn(center, outerRadius))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                int dx = cell.x - center.x;
                int dz = cell.z - center.z;
                int distSq = dx * dx + dz * dz;
                if (distSq > innerRadius * innerRadius && distSq <= outerRadius * outerRadius)
                {
                    map.terrainGrid.SetTerrain(cell, terrain);
                }
            }
        }

        private static void SpawnEdgePerimeterWalls(Map map, int layers)
        {
            if (map == null)
            {
                return;
            }

            int safeLayers = Mathf.Clamp(layers, 1, Mathf.Min(map.Size.x, map.Size.z) / 2);
            for (int layer = 0; layer < safeLayers; layer++)
            {
                int minX = layer;
                int maxX = map.Size.x - 1 - layer;
                int minZ = layer;
                int maxZ = map.Size.z - 1 - layer;

                for (int x = minX; x <= maxX; x++)
                {
                    SpawnProp(map, PerimeterWallDefName, new IntVec3(x, 0, minZ), Rot4.North);
                    if (maxZ != minZ)
                    {
                        SpawnProp(map, PerimeterWallDefName, new IntVec3(x, 0, maxZ), Rot4.North);
                    }
                }

                for (int z = minZ + 1; z < maxZ; z++)
                {
                    SpawnProp(map, PerimeterWallDefName, new IntVec3(minX, 0, z), Rot4.North);
                    if (maxX != minX)
                    {
                        SpawnProp(map, PerimeterWallDefName, new IntVec3(maxX, 0, z), Rot4.North);
                    }
                }
            }
        }


        private static void PaintEntryBridge(Map map, IntVec3 entry, IntVec3 extraction, IntVec3 center, TerrainDef plate, TerrainDef channel, TerrainDef sigil)
        {
            PaintRect(map, new CellRect(entry.x - 7, entry.z - 4, 14, 11), plate);
            PaintRect(map, new CellRect(entry.x - 3, entry.z - 1, 6, 5), sigil);
            PaintCircle(map, extraction, 5, sigil);
            PaintCorridor(map, extraction, center + new IntVec3(0, 0, -16), 5, plate);
            PaintCorridor(map, extraction, center + new IntVec3(0, 0, -16), 1, channel);
        }

        private static void PaintHeartDais(Map map, IntVec3 center, TerrainDef plate, TerrainDef channel, TerrainDef sigil)
        {
            // Dominion Sepulcher redesign: avoid large terrain circles under the heart.
            // The heart building texture now provides the focal industrial platform, while the
            // terrain layer stays as structured machinery and subtle access plating.
            PaintRect(map, new CellRect(center.x - 11, center.z - 10, 22, 20), plate);
            PaintRect(map, new CellRect(center.x - 5, center.z - 4, 10, 8), sigil);
            PaintRect(map, new CellRect(center.x - 2, center.z - 2, 4, 4), sigil);

            PaintCorridor(map, center, center + new IntVec3(0, 0, 16), 1, channel);
            PaintCorridor(map, center, center + new IntVec3(0, 0, -16), 1, channel);
            PaintCorridor(map, center, center + new IntVec3(16, 0, 0), 1, channel);
            PaintCorridor(map, center, center + new IntVec3(-16, 0, 0), 1, channel);
        }

        private static void PaintAnchorPlatform(Map map, IntVec3 center, TerrainDef plate, TerrainDef channel, TerrainDef sigil, Rot4 facing)
        {
            // Rectilinear access machinery instead of magical floor circles.
            PaintRect(map, new CellRect(center.x - 5, center.z - 5, 10, 10), plate);
            PaintRect(map, new CellRect(center.x - 2, center.z - 2, 4, 4), sigil);
            IntVec3 front = center + facing.FacingCell * 5;
            PaintCorridor(map, center, front, 1, channel);
        }

        private static void PaintRewardPocket(Map map, IntVec3 center, TerrainDef plate, TerrainDef sigil)
        {
            PaintRect(map, new CellRect(center.x - 6, center.z - 5, 12, 10), plate);
            PaintRect(map, new CellRect(center.x - 2, center.z - 1, 4, 2), sigil);
            PaintCircle(map, center + new IntVec3(-2,0,2), 2, sigil);
        }

        private static void PaintBrokenFactoryFloorStrips(Map map, IntVec3 center, IntVec3 extraction, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket, TerrainDef plate, TerrainDef channel)
        {
            if (map == null)
            {
                return;
            }

            // Large-scale but low-contrast terrain composition: gives the slice a built/ruined
            // factory-cathedral footprint without adding blocking objects or noisy props.
            PaintCorridor(map, center + new IntVec3(-43, 0, -16), center + new IntVec3(-25, 0, -13), 2, plate);
            PaintCorridor(map, center + new IntVec3(25, 0, -12), center + new IntVec3(43, 0, -16), 2, plate);
            PaintCorridor(map, center + new IntVec3(-44, 0, 20), center + new IntVec3(-30, 0, 28), 2, plate);
            PaintCorridor(map, center + new IntVec3(31, 0, 26), center + new IntVec3(45, 0, 18), 2, plate);
            PaintCorridor(map, extraction + new IntVec3(-22, 0, -12), extraction + new IntVec3(-8, 0, -9), 2, plate);
            PaintCorridor(map, extraction + new IntVec3(8, 0, -10), extraction + new IntVec3(22, 0, -13), 2, plate);

            PaintCorridor(map, center + new IntVec3(-40, 0, -16), center + new IntVec3(-31, 0, -15), 0, channel);
            PaintCorridor(map, center + new IntVec3(31, 0, -14), center + new IntVec3(40, 0, -16), 0, channel);
            PaintCorridor(map, center + new IntVec3(-40, 0, 22), center + new IntVec3(-33, 0, 26), 0, channel);
            PaintCorridor(map, center + new IntVec3(33, 0, 25), center + new IntVec3(41, 0, 20), 0, channel);

            PaintOrganicPatch(map, west + new IntVec3(-14, 0, 8), 3, plate);
            PaintOrganicPatch(map, east + new IntVec3(14, 0, 8), 3, plate);
            PaintOrganicPatch(map, north + new IntVec3(0, 0, 18), 3, plate);
            PaintOrganicPatch(map, rewardPocket + new IntVec3(-11, 0, 7), 2, plate);
            PaintOrganicPatch(map, rewardPocket + new IntVec3(12, 0, -7), 2, plate);

            // Dead factory floor districts: broad, non-blocking terrain mass so the slice reads
            // as a ruined industrial domain rather than a mostly empty dark board.
            PaintRect(map, new CellRect(center.x - 48, center.z + 4, 17, 9), plate);
            PaintRect(map, new CellRect(center.x + 31, center.z + 5, 16, 8), plate);
            PaintRect(map, new CellRect(center.x - 48, center.z - 34, 18, 7), plate);
            PaintRect(map, new CellRect(center.x + 29, center.z - 35, 18, 7), plate);
            PaintRect(map, new CellRect(center.x - 8, center.z + 30, 16, 9), plate);
            PaintCorridor(map, center + new IntVec3(-47, 0, 8), center + new IntVec3(-29, 0, 8), 1, channel);
            PaintCorridor(map, center + new IntVec3(30, 0, 9), center + new IntVec3(47, 0, 9), 1, channel);
            PaintCorridor(map, center + new IntVec3(-46, 0, -31), center + new IntVec3(-31, 0, -31), 1, channel);
            PaintCorridor(map, center + new IntVec3(31, 0, -32), center + new IntVec3(46, 0, -32), 1, channel);

            PaintOrganicPatch(map, center + new IntVec3(-39, 0, 9), 5, plate);
            PaintOrganicPatch(map, center + new IntVec3(39, 0, 10), 5, plate);
            PaintOrganicPatch(map, center + new IntVec3(-39, 0, -31), 4, plate);
            PaintOrganicPatch(map, center + new IntVec3(39, 0, -32), 4, plate);
            PaintOrganicPatch(map, center + new IntVec3(0, 0, 35), 4, plate);
        }

        private static void SpawnObjectivePlatformUnderlays(Map map, IntVec3 heart, IntVec3 sealAnchor, IntVec3 choirAnchor, IntVec3 lawAnchor)
        {
            // Lower-layer platform art. Heart and anchor buildings are spawned later by the
            // encounter component and keep their own actual object graphics above these bases.
            SpawnProp(map, HeartPlatformUnderlayDefName, heart, Rot4.North, false);
            SpawnProp(map, AnchorPlatformUnderlaySealDefName, sealAnchor, Rot4.North, false);
            SpawnProp(map, AnchorPlatformUnderlayChoirDefName, choirAnchor, Rot4.North, false);
            SpawnProp(map, AnchorPlatformUnderlayLawDefName, lawAnchor, Rot4.North, false);
        }

        private static void PaintSideArchitectureFootprints(Map map, IntVec3 center, IntVec3 extraction, IntVec3 west, IntVec3 east, IntVec3 north, TerrainDef plate, TerrainDef channel)
        {
            if (map == null || plate == null)
            {
                return;
            }

            // Broad low-contrast industrial side rooms. They make the slice read as a ruined
            // factory-cathedral without introducing new art or blocking the main combat lanes.
            PaintRect(map, new CellRect(center.x - 58, center.z - 2, 16, 20), plate);
            PaintRect(map, new CellRect(center.x + 42, center.z + 0, 16, 19), plate);
            PaintRect(map, new CellRect(center.x - 56, center.z - 39, 22, 12), plate);
            PaintRect(map, new CellRect(center.x + 35, center.z - 41, 22, 12), plate);
            PaintRect(map, new CellRect(center.x - 12, center.z + 42, 24, 10), plate);
            PaintRect(map, new CellRect(extraction.x - 33, extraction.z - 16, 18, 9), plate);
            PaintRect(map, new CellRect(extraction.x + 15, extraction.z - 17, 18, 9), plate);

            if (channel != null)
            {
                PaintCorridor(map, center + new IntVec3(-55, 0, 8), center + new IntVec3(-42, 0, 8), 1, channel);
                PaintCorridor(map, center + new IntVec3(42, 0, 9), center + new IntVec3(55, 0, 9), 1, channel);
                PaintCorridor(map, center + new IntVec3(-53, 0, -33), center + new IntVec3(-35, 0, -33), 1, channel);
                PaintCorridor(map, center + new IntVec3(36, 0, -35), center + new IntVec3(54, 0, -35), 1, channel);
                PaintCorridor(map, center + new IntVec3(-10, 0, 46), center + new IntVec3(10, 0, 46), 1, channel);
            }

            PaintOrganicPatch(map, center + new IntVec3(-49, 0, 8), 5, plate);
            PaintOrganicPatch(map, center + new IntVec3(49, 0, 9), 5, plate);
            PaintOrganicPatch(map, center + new IntVec3(-44, 0, -33), 5, plate);
            PaintOrganicPatch(map, center + new IntVec3(45, 0, -35), 5, plate);
            PaintOrganicPatch(map, center + new IntVec3(0, 0, 46), 5, plate);
        }

        private static void ScatterBurnScars(Map map, IntVec3 center, TerrainDef scarTerrain, TerrainDef baseTerrain)
        {
            IntRange radiusRange = new IntRange(18, 44);
            for (int i = 0; i < 14; i++)
            {
                int radius = radiusRange.RandomInRange;
                float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
                IntVec3 scar = ClampToInterior(map, new IntVec3(
                    center.x + GenMath.RoundRandom(Mathf.Cos(angle) * radius),
                    0,
                    center.z + GenMath.RoundRandom(Mathf.Sin(angle) * radius)));
                PaintCircle(map, scar, Rand.RangeInclusive(1, 2), Rand.Chance(0.65f) ? scarTerrain : baseTerrain);
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
            // Dominion Sepulcher hotfix: old Bastion/Spire shell art is intentionally disabled.
            // Boundary containment is handled by SpawnEdgePerimeterWalls; environmental mass now
            // comes from the newer ruin/rubble/decal library so the slice reads as dead industry,
            // not a reused magic-spire arena.
        }

        private static void SpawnLaneSupports(Map map, IntVec3 extraction, IntVec3 center, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket)
        {
            // Old Dominion Bastion Fragment / Dominion Spire lane supports disabled by request.
            // New composition groups are spawned in SpawnDecorativeDressings using the approved
            // Dominion ruins/rubble/decals already present in the project.
        }

        private static void SpawnDecorativeDressings(Map map, IntVec3 entry, IntVec3 extraction, IntVec3 center, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket)
        {
            List<IntVec3> reserved = BuildReservedCells(map, entry, extraction, center, west, east, north, rewardPocket);

            SpawnAnimatedFissureNetwork(map, center, entry, extraction, west, east, north, rewardPocket, reserved);
            SpawnGeneratedDominionSetpieces(map, entry, extraction, center, west, east, north, rewardPocket, reserved);
            SpawnFlankRibs(map, center, extraction, reserved);
            SpawnAnchorBacklineDressings(map, west, east, north, reserved);
            SpawnRewardPocketDressings(map, rewardPocket, reserved);
            SpawnPeripheralSpines(map, center, reserved);
            SpawnEntryDressings(map, entry, extraction, reserved);
            SpawnInteriorDeadMachineFields(map, center, extraction, west, east, north, rewardPocket, reserved);
            SpawnSideArchitectureLayer(map, center, extraction, west, east, north, rewardPocket, reserved);
            SpawnLargeEdgeArchitectureShells(map, center, extraction, reserved);

            // Package 3: integrate the new Dominion Sepulcher decor library as a restrained
            // environmental layer. These props are deliberately kept away from heart/anchor/entry
            // routes so the redesign adds atmosphere without turning the pocket map into a maze.
            SpawnDominionRuinWallLayer(map, center, extraction, west, east, north, rewardPocket, reserved);
            SpawnDominionRubbleLayer(map, center, extraction, west, east, north, rewardPocket, reserved);
            SpawnDominionQuietDecalLayer(map, center, extraction, west, east, north, rewardPocket, reserved);
        }

        private static List<IntVec3> BuildReservedCells(Map map, IntVec3 entry, IntVec3 extraction, IntVec3 center, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket)
        {
            List<IntVec3> reserved = new List<IntVec3>
            {
                entry,
                extraction,
                center,
                west,
                east,
                north,
                rewardPocket
            };

            AddLineSamples(reserved, extraction, center + new IntVec3(0, 0, -16), 3);
            AddLineSamples(reserved, center + new IntVec3(-13, 0, 6), west, 3);
            AddLineSamples(reserved, center + new IntVec3(13, 0, 6), east, 3);
            AddLineSamples(reserved, center + new IntVec3(0, 0, 15), north, 3);
            AddLineSamples(reserved, center + new IntVec3(-18, 0, -4), rewardPocket, 3);
            return reserved;
        }

        private static void AddLineSamples(List<IntVec3> cells, IntVec3 from, IntVec3 to, int spacing)
        {
            int steps = Mathf.Max(Math.Abs(to.x - from.x), Math.Abs(to.z - from.z));
            if (steps <= 0)
            {
                cells.Add(from);
                return;
            }

            int safeSpacing = Mathf.Max(1, spacing);
            for (int i = 0; i <= steps; i += safeSpacing)
            {
                float t = i / (float)steps;
                cells.Add(new IntVec3(GenMath.RoundRandom(Mathf.Lerp(from.x, to.x, t)), 0, GenMath.RoundRandom(Mathf.Lerp(from.z, to.z, t))));
            }
        }


        private static void SpawnAnimatedFissureNetwork(Map map, IntVec3 center, IntVec3 entry, IntVec3 extraction, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket, List<IntVec3> reserved)
        {
            if (map == null || reserved == null)
            {
                return;
            }

            List<IntVec3> blockerCells = new List<IntVec3>();
            List<DominionFissurePlacement> placements = BuildDominionFissurePlacements(map, center);
            for (int i = 0; i < placements.Count; i++)
            {
                DominionFissurePlacement placement = placements[i];
                if (placement == null)
                {
                    continue;
                }

                SpawnContinuousFissurePath(map, reserved, blockerCells, placement);
            }

            if (blockerCells.Count > 0)
            {
                reserved.AddRange(blockerCells);
            }
        }

        private static void SpawnContinuousFissurePath(Map map, List<IntVec3> protectedCells, List<IntVec3> blockerCells, DominionFissurePlacement placement)
        {
            if (map == null || placement == null || placement.pathCells == null || placement.pathCells.Length < 2)
            {
                return;
            }

            TrySpawnDominionFissureVisual(map, placement.visualDefName, placement.visualCell, protectedCells, blockerCells);
            SpawnInvisibleFissureBlockers(map, protectedCells, blockerCells, placement.pathCells);
        }

        private static void SpawnInvisibleFissureBlockers(Map map, List<IntVec3> protectedCells, List<IntVec3> blockerCells, IntVec3[] pathCells)
        {
            ThingDef blockerDef = DefDatabase<ThingDef>.GetNamedSilentFail(DominionFissureBlockerDefName);
            if (blockerDef == null)
            {
                return;
            }

            const float protectedMinDistance = 4.25f;
            const int halfWidth = 1;

            for (int i = 0; i < pathCells.Length - 1; i++)
            {
                IntVec3 a = pathCells[i];
                IntVec3 b = pathCells[i + 1];
                int dx = b.x - a.x;
                int dz = b.z - a.z;
                if (dx != 0 && dz != 0)
                {
                    continue;
                }

                int distance = Mathf.Max(Math.Abs(dx), Math.Abs(dz));
                int dirX = dx == 0 ? 0 : Math.Sign(dx);
                int dirZ = dz == 0 ? 0 : Math.Sign(dz);

                for (int d = 0; d <= distance; d++)
                {
                    IntVec3 cell = new IntVec3(a.x + dirX * d, 0, a.z + dirZ * d);
                    for (int w = -halfWidth; w <= halfWidth; w++)
                    {
                        IntVec3 widened = dirX != 0
                            ? new IntVec3(cell.x, 0, cell.z + w)
                            : new IntVec3(cell.x + w, 0, cell.z);
                        TrySpawnInvisibleFissureBlocker(map, blockerDef, widened, protectedCells, blockerCells, protectedMinDistance);
                    }
                }
            }
        }

        private static bool TrySpawnDominionFissureVisual(Map map, string defName, IntVec3 cell, List<IntVec3> protectedCells, List<IntVec3> blockerCells)
        {
            if (string.IsNullOrEmpty(defName) || map == null)
            {
                return false;
            }

            cell = ClampToInterior(map, cell, 11);
            if (!cell.InBounds(map) || CellContainsNonEphemeralThing(map, cell))
            {
                return false;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return false;
            }

            try
            {
                Thing thing = ThingMaker.MakeThing(def);
                if (thing == null)
                {
                    return false;
                }

                GenSpawn.Spawn(thing, cell, map, Rot4.North);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Skipped continuous Dominion fissure visual " + defName + " at " + cell + ": " + ex.GetType().Name);
                return false;
            }
        }

        private static bool TrySpawnInvisibleFissureBlocker(Map map, ThingDef blockerDef, IntVec3 cell, List<IntVec3> protectedCells, List<IntVec3> blockerCells, float protectedMinDistance)
        {
            if (map == null || blockerDef == null)
            {
                return false;
            }

            cell = ClampToInterior(map, cell, 4);
            if (!cell.InBounds(map))
            {
                return false;
            }

            if (TooCloseToAny(cell, protectedCells, protectedMinDistance) || TooCloseToAny(cell, blockerCells, 0.1f))
            {
                return false;
            }

            if (CellContainsNonEphemeralThing(map, cell))
            {
                return false;
            }

            try
            {
                Thing blocker = ThingMaker.MakeThing(blockerDef);
                if (blocker == null)
                {
                    return false;
                }

                GenSpawn.Spawn(blocker, cell, map, Rot4.North);
                blockerCells.Add(cell);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Skipped invisible Dominion fissure blocker at " + cell + ": " + ex.GetType().Name);
                return false;
            }
        }

        private static void SpawnFissurePath(Map map, IntVec3 center, List<IntVec3> protectedCells, List<IntVec3> spawnedFissures, params IntVec3[] offsets)
        {
            if (map == null || offsets == null || offsets.Length < 2)
            {
                return;
            }

            const float protectedMinDistance = 5.25f;
            const float chainMinDistance = 0.10f;
            const int step = 6;
            const int bendInset = 5;

            for (int i = 0; i < offsets.Length; i++)
            {
                IntVec3 cell = center + offsets[i];
                if (i == 0 || i == offsets.Length - 1)
                {
                    Rot4 endRot = EndcapRotation(offsets, i);
                    TrySpawnDominionFissure(map, DominionFissureEndcapDefName, cell, endRot, protectedCells, spawnedFissures, protectedMinDistance, chainMinDistance);
                }
                else
                {
                    Rot4 nodeRot = CornerRotation(offsets[i - 1], offsets[i], offsets[i + 1]);
                    TrySpawnDominionFissure(map, DominionFissureNodeDefName, cell, nodeRot, protectedCells, spawnedFissures, protectedMinDistance, chainMinDistance);
                }
            }

            for (int i = 0; i < offsets.Length - 1; i++)
            {
                SpawnFissureRun(map, center, offsets[i], offsets[i + 1], protectedCells, spawnedFissures, protectedMinDistance, chainMinDistance, bendInset, step);
            }
        }

        private static void SpawnFissureRun(Map map, IntVec3 center, IntVec3 fromOffset, IntVec3 toOffset, List<IntVec3> protectedCells, List<IntVec3> spawnedFissures, float protectedMinDistance, float chainMinDistance, int inset, int step)
        {
            int dx = toOffset.x - fromOffset.x;
            int dz = toOffset.z - fromOffset.z;
            if (dx != 0 && dz != 0)
            {
                return;
            }

            int distance = Mathf.Max(Math.Abs(dx), Math.Abs(dz));
            if (distance <= inset * 2)
            {
                return;
            }

            int dirX = dx == 0 ? 0 : Math.Sign(dx);
            int dirZ = dz == 0 ? 0 : Math.Sign(dz);
            Rot4 rot = dz != 0 ? Rot4.East : Rot4.North;

            for (int d = inset; d <= distance - inset; d += Mathf.Max(1, step))
            {
                IntVec3 offset = new IntVec3(fromOffset.x + dirX * d, 0, fromOffset.z + dirZ * d);
                TrySpawnDominionFissure(map, DominionFissureStraightDefName, center + offset, rot, protectedCells, spawnedFissures, protectedMinDistance, chainMinDistance);
            }
        }

        private static Rot4 EndcapRotation(IntVec3[] offsets, int index)
        {
            if (offsets == null || offsets.Length < 2)
            {
                return Rot4.North;
            }

            IntVec3 here = offsets[index];
            IntVec3 next = index == 0 ? offsets[1] : offsets[index - 1];
            int dx = next.x - here.x;
            int dz = next.z - here.z;
            if (Math.Abs(dx) >= Math.Abs(dz))
            {
                return dx >= 0 ? Rot4.East : Rot4.West;
            }
            return dz >= 0 ? Rot4.North : Rot4.South;
        }

        private static Rot4 CornerRotation(IntVec3 previous, IntVec3 current, IntVec3 next)
        {
            int dxA = current.x - previous.x;
            int dzA = current.z - previous.z;
            int dxB = next.x - current.x;
            int dzB = next.z - current.z;

            if ((dxA < 0 && dzB < 0) || (dzA > 0 && dxB > 0))
            {
                return Rot4.North;
            }
            if ((dxA > 0 && dzB < 0) || (dzA > 0 && dxB < 0))
            {
                return Rot4.East;
            }
            if ((dxA > 0 && dzB > 0) || (dzA < 0 && dxB < 0))
            {
                return Rot4.South;
            }
            return Rot4.West;
        }

        private static bool TrySpawnDominionFissure(Map map, string defName, IntVec3 cell, Rot4 rot, List<IntVec3> protectedCells, List<IntVec3> spawnedFissures, float protectedMinDistance, float fissureMinDistance)
        {
            if (string.IsNullOrEmpty(defName) || map == null)
            {
                return false;
            }

            cell = ClampToInterior(map, cell, 11);
            if (!cell.InBounds(map))
            {
                return false;
            }

            if (TooCloseToAny(cell, protectedCells, protectedMinDistance) || TooCloseToAny(cell, spawnedFissures, fissureMinDistance))
            {
                return false;
            }

            if (CellContainsNonEphemeralThing(map, cell))
            {
                return false;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return false;
            }

            try
            {
                Thing thing = ThingMaker.MakeThing(def);
                if (thing == null)
                {
                    return false;
                }

                GenSpawn.Spawn(thing, cell, map, rot);
                spawnedFissures.Add(cell);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Skipped Dominion fissure spawn " + defName + " at " + cell + ": " + ex.GetType().Name);
                return false;
            }
        }

        private static bool TooCloseToAny(IntVec3 cell, List<IntVec3> others, float minDistance)
        {
            if (others == null || others.Count == 0 || minDistance <= 0f)
            {
                return false;
            }

            float minDistanceSq = minDistance * minDistance;
            for (int i = 0; i < others.Count; i++)
            {
                IntVec3 other = others[i];
                int dx = cell.x - other.x;
                int dz = cell.z - other.z;
                if (dx * dx + dz * dz <= minDistanceSq)
                {
                    return true;
                }
            }

            return false;
        }

        private static void SpawnGeneratedDominionSetpieces(Map map, IntVec3 entry, IntVec3 extraction, IntVec3 center, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket, List<IntVec3> reserved)
        {
            if (map == null)
            {
                return;
            }

            // Generated hotfix assets: big readable silhouettes first, then old small props.
            // These are pass-through visual layers, so they make the slice feel built without
            // adding maze blockers around heart, anchors, entry, extraction or reward cells.
            // Hotfix 5.2: clamp large art by draw-footprint margin and keep it on lower altitude
            // defs so oversized silhouettes cannot visually sit on top of the pocket perimeter wall.
            SpawnProp(map, HeartFloorFractureDefName, ClampToInterior(map, center + new IntVec3(0, 0, -1), 12), Rot4.North, false);

            // The generated south seam scar read too much like a broken wall under arriving pawns.
            // Enemy emergence remains VFX-driven in MapComponent_DominionSliceEncounter; static entry
            // dressing is kept as flanking floor tears instead of a central structure under pawns.
            TrySpawnDominionDecoration(map, EdgeVoidTearDefName, ClampToInterior(map, entry + new IntVec3(-18, 0, -7), 12), Rot4.North, reserved, 5.5f, false);
            TrySpawnDominionDecoration(map, EdgeVoidTearDefName, ClampToInterior(map, entry + new IntVec3(18, 0, -7), 12), Rot4.North, reserved, 5.5f, false);

            SpawnProp(map, SideRoomFloorUnderlayDefName, ClampToInterior(map, center + new IntVec3(-43, 0, 4), 18), Rot4.North, false);
            TrySpawnDominionDecoration(map, SideRoomMachineBayWestDefName, ClampToInterior(map, center + new IntVec3(-43, 0, 4), 18), Rot4.North, reserved, 7.0f, false);

            SpawnProp(map, SideRoomFloorUnderlayDefName, ClampToInterior(map, center + new IntVec3(43, 0, 5), 18), Rot4.North, false);
            TrySpawnDominionDecoration(map, SideRoomReliquaryBayEastDefName, ClampToInterior(map, center + new IntVec3(43, 0, 5), 18), Rot4.North, reserved, 7.0f, false);

            TrySpawnDominionDecoration(map, BackWallShellNorthDefName, ClampToInterior(map, center + new IntVec3(0, 0, 39), 16), Rot4.North, reserved, 8.5f, false);
            TrySpawnDominionDecoration(map, EdgeMegastructureStraightDefName, ClampToInterior(map, center + new IntVec3(-31, 0, 37), 16), Rot4.North, reserved, 6.5f, false);
            TrySpawnDominionDecoration(map, EdgeMegastructureStraightDefName, ClampToInterior(map, center + new IntVec3(31, 0, 36), 16), Rot4.North, reserved, 6.5f, false);
            TrySpawnDominionDecoration(map, EdgeMegastructureCornerDefName, ClampToInterior(map, center + new IntVec3(-43, 0, -30), 16), Rot4.North, reserved, 6.5f, false);
            TrySpawnDominionDecoration(map, EdgeMegastructureCornerDefName, ClampToInterior(map, center + new IntVec3(43, 0, -30), 16), Rot4.North, reserved, 6.5f, false);

            TrySpawnDominionDecoration(map, MachineWreckReactorRibDefName, ClampToInterior(map, center + new IntVec3(36, 0, -29), 14), Rot4.North, reserved, 6.5f, false);
            TrySpawnDominionDecoration(map, PlateCollapseLargeDefName, ClampToInterior(map, center + new IntVec3(-36, 0, -29), 14), Rot4.North, reserved, 6.5f, false);

            TrySpawnDominionDecoration(map, BrokenConduitSpineDefName, ClampToInterior(map, center + new IntVec3(-16, 0, 8), 12), Rot4.North, reserved, 5.0f, false);
            TrySpawnDominionDecoration(map, BrokenConduitSpineDefName, ClampToInterior(map, center + new IntVec3(17, 0, 10), 12), Rot4.North, reserved, 5.0f, false);
            TrySpawnDominionDecoration(map, PlateCollapseLargeDefName, ClampToInterior(map, center + new IntVec3(0, 0, 32), 14), Rot4.North, reserved, 6.5f, false);
        }

        private static void SpawnFlankRibs(Map map, IntVec3 center, IntVec3 extraction, List<IntVec3> reserved)
        {
            // Composed side ruin fields. These replace the old spires with lower, industrial ruins.
            SpawnDecorationCluster(map, center + new IntVec3(-28, 0, -12), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 1, new IntVec3(-3, 0, 0), Rot4.West, 5.8f, false),
                new DecorationEntry(DominionRubbleDefs, 1, new IntVec3(1, 0, -2), Rot4.North, 3.5f, false),
                new DecorationEntry(DominionDecalDefs, 0, new IntVec3(2, 0, 2), Rot4.North, 1.6f, false),
                new DecorationEntry(DominionDecalDefs, 7, new IntVec3(-1, 0, 3), Rot4.North, 1.6f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(29, 0, -10), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 3, new IntVec3(3, 0, 0), Rot4.East, 5.8f, false),
                new DecorationEntry(DominionRubbleDefs, 2, new IntVec3(-1, 0, -2), Rot4.North, 3.5f, false),
                new DecorationEntry(DominionDecalDefs, 1, new IntVec3(-2, 0, 2), Rot4.North, 1.6f, false),
                new DecorationEntry(DominionDecalDefs, 8, new IntVec3(1, 0, 3), Rot4.North, 1.6f, false)
            });

            SpawnDecorationCluster(map, extraction + new IntVec3(-18, 0, -2), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 7, new IntVec3(-2, 0, 0), Rot4.West, 5.0f, false),
                new DecorationEntry(DominionRubbleDefs, 3, new IntVec3(1, 0, 2), Rot4.West, 3.2f, false),
                new DecorationEntry(DominionDecalDefs, 5, new IntVec3(3, 0, -1), Rot4.North, 1.4f, false)
            });

            SpawnDecorationCluster(map, extraction + new IntVec3(18, 0, -2), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 6, new IntVec3(2, 0, 0), Rot4.East, 5.0f, false),
                new DecorationEntry(DominionRubbleDefs, 4, new IntVec3(-1, 0, 2), Rot4.East, 3.2f, false),
                new DecorationEntry(DominionDecalDefs, 6, new IntVec3(-3, 0, -1), Rot4.North, 1.4f, false)
            });
        }

        private static void SpawnAnchorBacklineDressings(Map map, IntVec3 west, IntVec3 east, IntVec3 north, List<IntVec3> reserved)
        {
            SpawnDecorationCluster(map, west + new IntVec3(-15, 0, 8), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 8, IntVec3.Zero, Rot4.West, 6.5f, false),
                new DecorationEntry(DominionRubbleDefs, 5, new IntVec3(-2, 0, -3), Rot4.North, 3.2f, false),
                new DecorationEntry(DominionDecalDefs, 2, new IntVec3(2, 0, 2), Rot4.North, 1.5f, false)
            });

            SpawnDecorationCluster(map, east + new IntVec3(15, 0, 8), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 9, IntVec3.Zero, Rot4.East, 6.5f, false),
                new DecorationEntry(DominionRubbleDefs, 5, new IntVec3(2, 0, -3), Rot4.North, 3.2f, false),
                new DecorationEntry(DominionDecalDefs, 3, new IntVec3(-2, 0, 2), Rot4.North, 1.5f, false)
            });

            SpawnDecorationCluster(map, north + new IntVec3(0, 0, 18), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 5, IntVec3.Zero, Rot4.North, 7.2f, false),
                new DecorationEntry(DominionRubbleDefs, 6, new IntVec3(-4, 0, 2), Rot4.North, 3.4f, false),
                new DecorationEntry(DominionRubbleDefs, 0, new IntVec3(4, 0, 2), Rot4.North, 3.4f, false),
                new DecorationEntry(DominionDecalDefs, 9, new IntVec3(0, 0, -3), Rot4.North, 1.5f, false)
            });
        }

        private static void SpawnRewardPocketDressings(Map map, IntVec3 rewardPocket, List<IntVec3> reserved)
        {
            SpawnDecorationCluster(map, rewardPocket + new IntVec3(-11, 0, 6), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 1, IntVec3.Zero, Rot4.West, 5.0f, false),
                new DecorationEntry(DominionRubbleDefs, 1, new IntVec3(3, 0, -2), Rot4.North, 3.0f, false),
                new DecorationEntry(DominionDecalDefs, 0, new IntVec3(2, 0, 2), Rot4.North, 1.4f, false)
            });

            SpawnDecorationCluster(map, rewardPocket + new IntVec3(12, 0, -6), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 3, IntVec3.Zero, Rot4.East, 5.0f, false),
                new DecorationEntry(DominionRubbleDefs, 2, new IntVec3(-3, 0, 2), Rot4.North, 3.0f, false),
                new DecorationEntry(DominionDecalDefs, 4, new IntVec3(-2, 0, -2), Rot4.North, 1.4f, false)
            });
        }

        private static void SpawnPeripheralSpines(Map map, IntVec3 center, List<IntVec3> reserved)
        {
            // Non-uniform perimeter groups make the dominion slice feel intentionally ruined rather
            // than uniformly empty. They are kept well away from combat lanes.
            SpawnDecorationCluster(map, center + new IntVec3(-44, 0, 22), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 2, IntVec3.Zero, Rot4.West, 7.5f, false),
                new DecorationEntry(DominionRubbleDefs, 0, new IntVec3(4, 0, -3), Rot4.North, 3.5f, false),
                new DecorationEntry(DominionDecalDefs, 5, new IntVec3(1, 0, 4), Rot4.North, 1.8f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(43, 0, 20), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 4, IntVec3.Zero, Rot4.East, 7.5f, false),
                new DecorationEntry(DominionRubbleDefs, 3, new IntVec3(-4, 0, -3), Rot4.North, 3.5f, false),
                new DecorationEntry(DominionDecalDefs, 6, new IntVec3(-1, 0, 4), Rot4.North, 1.8f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(-42, 0, -31), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 8, IntVec3.Zero, Rot4.West, 7.5f, false),
                new DecorationEntry(DominionRubbleDefs, 4, new IntVec3(3, 0, 3), Rot4.North, 3.5f, false),
                new DecorationEntry(DominionDecalDefs, 1, new IntVec3(-2, 0, -3), Rot4.North, 1.8f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(42, 0, -32), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 9, IntVec3.Zero, Rot4.East, 7.5f, false),
                new DecorationEntry(DominionRubbleDefs, 6, new IntVec3(-3, 0, 3), Rot4.North, 3.5f, false),
                new DecorationEntry(DominionDecalDefs, 8, new IntVec3(2, 0, -3), Rot4.North, 1.8f, false)
            });
        }

        private static void SpawnEntryDressings(Map map, IntVec3 entry, IntVec3 extraction, List<IntVec3> reserved)
        {
            SpawnDecorationCluster(map, entry + new IntVec3(-13, 0, 5), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 7, IntVec3.Zero, Rot4.West, 5.0f, false),
                new DecorationEntry(DominionRubbleDefs, 0, new IntVec3(3, 0, -1), Rot4.North, 3.0f, false),
                new DecorationEntry(DominionDecalDefs, 9, new IntVec3(1, 0, 3), Rot4.North, 1.3f, false)
            });

            SpawnDecorationCluster(map, entry + new IntVec3(13, 0, 5), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 6, IntVec3.Zero, Rot4.East, 5.0f, false),
                new DecorationEntry(DominionRubbleDefs, 1, new IntVec3(-3, 0, -1), Rot4.North, 3.0f, false),
                new DecorationEntry(DominionDecalDefs, 4, new IntVec3(-1, 0, 3), Rot4.North, 1.3f, false)
            });
        }

        private static void SpawnInteriorDeadMachineFields(Map map, IntVec3 center, IntVec3 extraction, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket, List<IntVec3> reserved)
        {
            // Mid-field visual mass. These are not evenly scattered decorations: each group reads
            // as a broken machine bay or dead process lane, and all use the existing approved
            // ruins/rubble/decals. They are non-critical to pathing and kept off heart/anchor cells.
            SpawnDecorationCluster(map, center + new IntVec3(-39, 0, 9), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 0, new IntVec3(-3, 0, 0), Rot4.West, 4.8f, false),
                new DecorationEntry(DominionRuinWallDefs, 6, new IntVec3(3, 0, 1), Rot4.East, 3.6f, false),
                new DecorationEntry(DominionRubbleDefs, 2, new IntVec3(0, 0, -3), Rot4.North, 2.6f, false),
                new DecorationEntry(DominionRubbleDefs, 5, new IntVec3(2, 0, 4), Rot4.North, 2.6f, false),
                new DecorationEntry(DominionDecalDefs, 5, new IntVec3(-1, 0, 4), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(39, 0, 10), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 1, new IntVec3(3, 0, 0), Rot4.East, 4.8f, false),
                new DecorationEntry(DominionRuinWallDefs, 7, new IntVec3(-3, 0, 1), Rot4.West, 3.6f, false),
                new DecorationEntry(DominionRubbleDefs, 1, new IntVec3(0, 0, -3), Rot4.North, 2.6f, false),
                new DecorationEntry(DominionRubbleDefs, 6, new IntVec3(-2, 0, 4), Rot4.North, 2.6f, false),
                new DecorationEntry(DominionDecalDefs, 6, new IntVec3(1, 0, 4), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(-39, 0, -31), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 8, new IntVec3(-2, 0, 0), Rot4.West, 4.8f, false),
                new DecorationEntry(DominionRubbleDefs, 0, new IntVec3(2, 0, -2), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionRubbleDefs, 3, new IntVec3(4, 0, 2), Rot4.West, 2.5f, false),
                new DecorationEntry(DominionDecalDefs, 0, new IntVec3(-1, 0, 3), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(39, 0, -32), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 9, new IntVec3(2, 0, 0), Rot4.East, 4.8f, false),
                new DecorationEntry(DominionRubbleDefs, 4, new IntVec3(-2, 0, -2), Rot4.East, 2.5f, false),
                new DecorationEntry(DominionRubbleDefs, 0, new IntVec3(-4, 0, 2), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionDecalDefs, 1, new IntVec3(1, 0, 3), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(0, 0, 36), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 5, new IntVec3(0, 0, 1), Rot4.North, 5.0f, false),
                new DecorationEntry(DominionRubbleDefs, 6, new IntVec3(-5, 0, -1), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionRubbleDefs, 2, new IntVec3(5, 0, -1), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionDecalDefs, 8, new IntVec3(0, 0, -4), Rot4.North, 1.2f, false)
            });
        }

        private static void SpawnSideArchitectureLayer(Map map, IntVec3 center, IntVec3 extraction, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket, List<IntVec3> reserved)
        {
            // Stronger architectural silhouette: broken side rooms, wall shells and machine bays.
            // These use existing approved Dominion ruin/rubble/decals and avoid objective cells/lane samples.
            SpawnDecorationCluster(map, center + new IntVec3(-51, 0, 8), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 0, new IntVec3(-3, 0, 0), Rot4.West, 4.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 2, new IntVec3(-6, 0, 4), Rot4.West, 4.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 6, new IntVec3(2, 0, -5), Rot4.North, 3.8f, false),
                new DecorationEntry(DominionRubbleDefs, 2, new IntVec3(2, 0, 3), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionDecalDefs, 0, new IntVec3(5, 0, 0), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(51, 0, 9), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 1, new IntVec3(3, 0, 0), Rot4.East, 4.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 3, new IntVec3(6, 0, 4), Rot4.East, 4.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 7, new IntVec3(-2, 0, -5), Rot4.North, 3.8f, false),
                new DecorationEntry(DominionRubbleDefs, 1, new IntVec3(-2, 0, 3), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionDecalDefs, 1, new IntVec3(-5, 0, 0), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(-45, 0, -34), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 4, new IntVec3(-4, 0, 0), Rot4.West, 4.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 8, new IntVec3(2, 0, 4), Rot4.North, 3.6f, false),
                new DecorationEntry(DominionRubbleDefs, 5, new IntVec3(3, 0, -3), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionRubbleDefs, 0, new IntVec3(-2, 0, -4), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionDecalDefs, 5, new IntVec3(5, 0, 2), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(45, 0, -35), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 5, new IntVec3(4, 0, 0), Rot4.East, 4.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 9, new IntVec3(-2, 0, 4), Rot4.North, 3.6f, false),
                new DecorationEntry(DominionRubbleDefs, 6, new IntVec3(-3, 0, -3), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionRubbleDefs, 0, new IntVec3(2, 0, -4), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionDecalDefs, 6, new IntVec3(-5, 0, 2), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(0, 0, 46), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 0, new IntVec3(-7, 0, 0), Rot4.North, 3.8f, false),
                new DecorationEntry(DominionRuinWallDefs, 1, new IntVec3(7, 0, 0), Rot4.North, 3.8f, false),
                new DecorationEntry(DominionRuinWallDefs, 5, new IntVec3(0, 0, 4), Rot4.North, 3.8f, false),
                new DecorationEntry(DominionRubbleDefs, 3, new IntVec3(-3, 0, -3), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionRubbleDefs, 4, new IntVec3(3, 0, -3), Rot4.North, 2.8f, false),
                new DecorationEntry(DominionDecalDefs, 9, new IntVec3(0, 0, -5), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, extraction + new IntVec3(-27, 0, -12), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 2, new IntVec3(-3, 0, 0), Rot4.West, 3.8f, false),
                new DecorationEntry(DominionRuinWallDefs, 6, new IntVec3(4, 0, 1), Rot4.North, 3.6f, false),
                new DecorationEntry(DominionRubbleDefs, 1, new IntVec3(1, 0, 4), Rot4.North, 2.6f, false),
                new DecorationEntry(DominionDecalDefs, 2, new IntVec3(4, 0, -3), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, extraction + new IntVec3(27, 0, -13), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 3, new IntVec3(3, 0, 0), Rot4.East, 3.8f, false),
                new DecorationEntry(DominionRuinWallDefs, 7, new IntVec3(-4, 0, 1), Rot4.North, 3.6f, false),
                new DecorationEntry(DominionRubbleDefs, 2, new IntVec3(-1, 0, 4), Rot4.North, 2.6f, false),
                new DecorationEntry(DominionDecalDefs, 3, new IntVec3(-4, 0, -3), Rot4.North, 1.2f, false)
            });
        }

        private struct DecorationEntry
        {
            public readonly string[] defs;
            public readonly int index;
            public readonly IntVec3 offset;
            public readonly Rot4 rot;
            public readonly float minDistance;
            public readonly bool clearExisting;

            public DecorationEntry(string[] defs, int index, IntVec3 offset, Rot4 rot, float minDistance, bool clearExisting)
            {
                this.defs = defs;
                this.index = index;
                this.offset = offset;
                this.rot = rot;
                this.minDistance = minDistance;
                this.clearExisting = clearExisting;
            }
        }

        private static void SpawnDecorationCluster(Map map, IntVec3 origin, List<IntVec3> reserved, DecorationEntry[] entries)
        {
            if (map == null || entries == null)
            {
                return;
            }

            // Important: cluster members must be allowed to sit near each other.
            // The previous implementation added each spawned prop to the global reserved list
            // immediately, so most multi-piece side rooms collapsed into a single tiny object.
            // This is why the slice still looked empty at maximum zoom. Check distance only
            // against pre-existing reserved objective/lane cells, then commit the whole cluster.
            List<IntVec3> spawnedLocal = new List<IntVec3>();
            for (int i = 0; i < entries.Length; i++)
            {
                DecorationEntry entry = entries[i];
                TrySpawnDominionDecorationInCluster(map, SelectDef(entry.defs, entry.index), origin + entry.offset, entry.rot, reserved, spawnedLocal, entry.minDistance, entry.clearExisting);
            }

            if (spawnedLocal.Count > 0)
            {
                reserved.AddRange(spawnedLocal);
            }
        }

        private static void SpawnLargeEdgeArchitectureShells(Map map, IntVec3 center, IntVec3 extraction, List<IntVec3> reserved)
        {
            // Maximum-zoom readability layer: big side shells and dead industrial rooms.
            // Uses the same approved ruin/rubble/decals, but the cluster-distance fix above lets
            // the pieces actually appear together as structures instead of isolated specks.
            SpawnDecorationCluster(map, center + new IntVec3(-52, 0, 24), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 0, new IntVec3(-5, 0, 0), Rot4.West, 5.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 2, new IntVec3(-5, 0, 6), Rot4.West, 5.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 6, new IntVec3(1, 0, -5), Rot4.North, 5.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 8, new IntVec3(3, 0, 4), Rot4.North, 4.5f, false),
                new DecorationEntry(DominionRubbleDefs, 5, new IntVec3(2, 0, 0), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionRubbleDefs, 2, new IntVec3(5, 0, 6), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionDecalDefs, 0, new IntVec3(4, 0, -4), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(52, 0, 25), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 1, new IntVec3(5, 0, 0), Rot4.East, 5.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 3, new IntVec3(5, 0, 6), Rot4.East, 5.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 7, new IntVec3(-1, 0, -5), Rot4.North, 5.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 9, new IntVec3(-3, 0, 4), Rot4.North, 4.5f, false),
                new DecorationEntry(DominionRubbleDefs, 6, new IntVec3(-2, 0, 0), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionRubbleDefs, 1, new IntVec3(-5, 0, 6), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionDecalDefs, 1, new IntVec3(-4, 0, -4), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(-53, 0, -34), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 4, new IntVec3(-4, 0, 0), Rot4.West, 5.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 8, new IntVec3(2, 0, 5), Rot4.North, 4.5f, false),
                new DecorationEntry(DominionRuinWallDefs, 6, new IntVec3(5, 0, -3), Rot4.North, 4.5f, false),
                new DecorationEntry(DominionRubbleDefs, 0, new IntVec3(1, 0, -1), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionRubbleDefs, 3, new IntVec3(5, 0, 4), Rot4.West, 2.5f, false),
                new DecorationEntry(DominionDecalDefs, 8, new IntVec3(-1, 0, 4), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(53, 0, -35), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 5, new IntVec3(4, 0, 0), Rot4.East, 5.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 9, new IntVec3(-2, 0, 5), Rot4.North, 4.5f, false),
                new DecorationEntry(DominionRuinWallDefs, 7, new IntVec3(-5, 0, -3), Rot4.North, 4.5f, false),
                new DecorationEntry(DominionRubbleDefs, 4, new IntVec3(-1, 0, -1), Rot4.East, 2.5f, false),
                new DecorationEntry(DominionRubbleDefs, 2, new IntVec3(-5, 0, 4), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionDecalDefs, 9, new IntVec3(1, 0, 4), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, center + new IntVec3(0, 0, 49), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 5, new IntVec3(0, 0, 0), Rot4.North, 6.0f, false),
                new DecorationEntry(DominionRuinWallDefs, 6, new IntVec3(-8, 0, -2), Rot4.West, 4.2f, false),
                new DecorationEntry(DominionRuinWallDefs, 7, new IntVec3(8, 0, -2), Rot4.East, 4.2f, false),
                new DecorationEntry(DominionRubbleDefs, 0, new IntVec3(-4, 0, 3), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionRubbleDefs, 6, new IntVec3(4, 0, 3), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionDecalDefs, 5, new IntVec3(0, 0, -4), Rot4.North, 1.2f, false)
            });

            SpawnDecorationCluster(map, extraction + new IntVec3(0, 0, -24), reserved, new[]
            {
                new DecorationEntry(DominionRuinWallDefs, 0, new IntVec3(-8, 0, 0), Rot4.West, 4.2f, false),
                new DecorationEntry(DominionRuinWallDefs, 1, new IntVec3(8, 0, 0), Rot4.East, 4.2f, false),
                new DecorationEntry(DominionRubbleDefs, 5, new IntVec3(-3, 0, 3), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionRubbleDefs, 1, new IntVec3(3, 0, 3), Rot4.North, 2.5f, false),
                new DecorationEntry(DominionDecalDefs, 6, new IntVec3(0, 0, -3), Rot4.North, 1.2f, false)
            });
        }

        private static void SpawnDominionRuinWallLayer(Map map, IntVec3 center, IntVec3 extraction, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket, List<IntVec3> reserved)
        {
            // Hotfix 5.1: keep the industrial ruin dressing sparse. The previous pass made
            // the map look like a decorated arena rather than a bleak dominion floor.
            TrySpawnDominionDecoration(map, SelectDef(DominionRuinWallDefs, 1), center + new IntVec3(-37, 0, -23), Rot4.West, reserved, 9.0f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRuinWallDefs, 3), center + new IntVec3(37, 0, -20), Rot4.East, reserved, 9.0f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRuinWallDefs, 8), center + new IntVec3(-33, 0, 29), Rot4.North, reserved, 9.0f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRuinWallDefs, 9), center + new IntVec3(32, 0, 31), Rot4.North, reserved, 9.0f, false);

            TrySpawnDominionDecoration(map, SelectDef(DominionRuinWallDefs, 7), extraction + new IntVec3(-18, 0, 10), Rot4.West, reserved, 7.5f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRuinWallDefs, 6), extraction + new IntVec3(18, 0, 10), Rot4.East, reserved, 7.5f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRuinWallDefs, 8), north + new IntVec3(-16, 0, 16), Rot4.North, reserved, 8.0f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRuinWallDefs, 9), north + new IntVec3(16, 0, 16), Rot4.North, reserved, 8.0f, false);
        }

        private static void SpawnDominionRubbleLayer(Map map, IntVec3 center, IntVec3 extraction, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket, List<IntVec3> reserved)
        {
            TrySpawnDominionDecoration(map, SelectDef(DominionRubbleDefs, 0), center + new IntVec3(-22, 0, -17), Rot4.North, reserved, 5.5f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRubbleDefs, 1), center + new IntVec3(22, 0, -15), Rot4.North, reserved, 5.5f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRubbleDefs, 3), center + new IntVec3(-30, 0, 5), Rot4.West, reserved, 6.0f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRubbleDefs, 4), center + new IntVec3(31, 0, 4), Rot4.East, reserved, 6.0f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRubbleDefs, 6), center + new IntVec3(4, 0, -29), Rot4.North, reserved, 5.5f, false);

            TrySpawnDominionDecoration(map, SelectDef(DominionRubbleDefs, 2), west + new IntVec3(-9, 0, -9), Rot4.North, reserved, 5.0f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRubbleDefs, 5), east + new IntVec3(9, 0, 11), Rot4.North, reserved, 5.0f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionRubbleDefs, 0), rewardPocket + new IntVec3(-9, 0, -9), Rot4.North, reserved, 5.0f, false);
        }

        private static void SpawnDominionQuietDecalLayer(Map map, IntVec3 center, IntVec3 extraction, IntVec3 west, IntVec3 east, IntVec3 north, IntVec3 rewardPocket, List<IntVec3> reserved)
        {
            float[] radii = { 23f, 34f, 43f };
            float[] angles = { 17f, 61f, 119f, 173f, 229f, 287f, 331f };
            int index = 0;
            for (int r = 0; r < radii.Length; r++)
            {
                for (int a = 0; a < angles.Length; a += 2)
                {
                    float angle = (angles[(a + r) % angles.Length] + r * 8f) * Mathf.Deg2Rad;
                    IntVec3 cell = ClampToInterior(map, new IntVec3(
                        center.x + GenMath.RoundRandom(Mathf.Cos(angle) * radii[r]),
                        0,
                        center.z + GenMath.RoundRandom(Mathf.Sin(angle) * radii[r])));
                    TrySpawnDominionDecoration(map, SelectDef(DominionDecalDefs, index++), cell, Rot4.North, reserved, 4.25f, false);
                }
            }

            TrySpawnDominionDecoration(map, SelectDef(DominionDecalDefs, index++), extraction + new IntVec3(-7, 0, -7), Rot4.North, reserved, 3.75f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionDecalDefs, index++), extraction + new IntVec3(7, 0, -7), Rot4.North, reserved, 3.75f, false);
            TrySpawnDominionDecoration(map, SelectDef(DominionDecalDefs, index++), north + new IntVec3(0, 0, 13), Rot4.North, reserved, 3.75f, false);
        }

        private static void ScatterQuietFloorVariation(Map map, IntVec3 center, TerrainDef baseTerrain, TerrainDef plateTerrain)
        {
            if (map == null || plateTerrain == null)
            {
                return;
            }

            // Irregular low-contrast plate islands stop the dominion map from reading as one
            // repeated square texture, while avoiding large clean arena blocks.
            for (int i = 0; i < 42; i++)
            {
                float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Rand.Range(14f, Mathf.Min(map.Size.x, map.Size.z) * 0.46f);
                IntVec3 patch = ClampToInterior(map, new IntVec3(
                    center.x + GenMath.RoundRandom(Mathf.Cos(angle) * radius),
                    0,
                    center.z + GenMath.RoundRandom(Mathf.Sin(angle) * radius)));

                int patchRadius = Rand.RangeInclusive(1, 3);
                PaintOrganicPatch(map, patch, patchRadius, plateTerrain);
            }
        }

        private static void PaintOrganicPatch(Map map, IntVec3 center, int radius, TerrainDef terrain)
        {
            if (map == null || terrain == null)
            {
                return;
            }

            int radiusSq = radius * radius;
            foreach (IntVec3 cell in CellRect.CenteredOn(center, radius))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                int dx = cell.x - center.x;
                int dz = cell.z - center.z;
                int distSq = dx * dx + dz * dz;
                if (distSq <= radiusSq && Rand.Chance(distSq == 0 ? 1f : 0.72f))
                {
                    map.terrainGrid.SetTerrain(cell, terrain);
                }
            }
        }

        private static string SelectDef(string[] defs, int index)
        {
            if (defs == null || defs.Length == 0)
            {
                return null;
            }

            int safeIndex = Mathf.Abs(index) % defs.Length;
            return defs[safeIndex];
        }

        private static bool TrySpawnDominionDecorationInCluster(Map map, string defName, IntVec3 cell, Rot4 rot, List<IntVec3> externalReserved, List<IntVec3> spawnedLocal, float minDistance, bool clearExisting)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return false;
            }

            if (map == null || !cell.InBounds(map))
            {
                return false;
            }

            if (cell.x < 8 || cell.z < 8 || cell.x > map.Size.x - 9 || cell.z > map.Size.z - 9)
            {
                return false;
            }

            float minDistanceSq = minDistance * minDistance;
            for (int i = 0; i < externalReserved.Count; i++)
            {
                IntVec3 reservedCell = externalReserved[i];
                int dx = cell.x - reservedCell.x;
                int dz = cell.z - reservedCell.z;
                if (dx * dx + dz * dz <= minDistanceSq)
                {
                    return false;
                }
            }

            for (int i = 0; i < spawnedLocal.Count; i++)
            {
                if (spawnedLocal[i] == cell)
                {
                    return false;
                }
            }

            if (!clearExisting && CellContainsNonEphemeralThing(map, cell))
            {
                return false;
            }

            SpawnProp(map, defName, cell, rot, clearExisting);
            spawnedLocal.Add(cell);
            return true;
        }

        private static bool TrySpawnDominionDecoration(Map map, string defName, IntVec3 cell, Rot4 rot, List<IntVec3> reserved, float minDistance, bool clearExisting)
        {
            if (string.IsNullOrEmpty(defName))
            {
                return false;
            }

            if (map == null || !cell.InBounds(map))
            {
                return false;
            }

            if (cell.x < 8 || cell.z < 8 || cell.x > map.Size.x - 9 || cell.z > map.Size.z - 9)
            {
                return false;
            }

            float minDistanceSq = minDistance * minDistance;
            for (int i = 0; i < reserved.Count; i++)
            {
                IntVec3 reservedCell = reserved[i];
                int dx = cell.x - reservedCell.x;
                int dz = cell.z - reservedCell.z;
                if (dx * dx + dz * dz <= minDistanceSq)
                {
                    return false;
                }
            }

            if (!clearExisting && CellContainsNonEphemeralThing(map, cell))
            {
                return false;
            }

            SpawnProp(map, defName, cell, rot, clearExisting);
            reserved.Add(cell);
            return true;
        }

        private static bool CellContainsNonEphemeralThing(Map map, IntVec3 cell)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || thing is Pawn || thing.def == null)
                {
                    continue;
                }

                if (thing.def.category == ThingCategory.Mote || thing.def.category == ThingCategory.Attachment)
                {
                    continue;
                }

                string defName = thing.def.defName ?? string.Empty;
                if (defName == "PocketMapExit" || defName == "CaveExit" || defName == "PitGate")
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool TrySpawnDecorativeProp(Map map, string defName, IntVec3 cell, Rot4 rot, List<IntVec3> reserved, float minDistance)
        {
            if (map == null || !cell.InBounds(map))
            {
                return false;
            }

            if (cell.x < 8 || cell.z < 8 || cell.x > map.Size.x - 9 || cell.z > map.Size.z - 9)
            {
                return false;
            }

            float minDistanceSq = minDistance * minDistance;
            for (int i = 0; i < reserved.Count; i++)
            {
                IntVec3 reservedCell = reserved[i];
                int dx = cell.x - reservedCell.x;
                int dz = cell.z - reservedCell.z;
                if (dx * dx + dz * dz <= minDistanceSq)
                {
                    return false;
                }
            }

            SpawnProp(map, defName, cell, rot);
            reserved.Add(cell);
            return true;
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
            SpawnProp(map, defName, cell, rot, true);
        }

        private static void SpawnProp(Map map, string defName, IntVec3 cell, Rot4 rot, bool clearExisting)
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

            if (clearExisting)
            {
                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed || thing is Pawn || thing.def == null)
                    {
                        continue;
                    }

                    if (thing.def.category == ThingCategory.Mote || thing.def.category == ThingCategory.Attachment)
                    {
                        continue;
                    }

                    string existingDefName = thing.def.defName ?? string.Empty;
                    if (!thing.def.useHitPoints || existingDefName == "PocketMapExit" || existingDefName == "CaveExit" || existingDefName == "PitGate")
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
            }

            Thing spawned = ThingMaker.MakeThing(def);
            if (spawned != null)
            {
                GenSpawn.Spawn(spawned, cell, map, rot);
            }
        }
    }
}
