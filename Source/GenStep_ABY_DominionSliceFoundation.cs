using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public sealed class GenStep_ABY_DominionSliceFoundation : GenStep
    {
        public override int SeedPart => 873451921;

        public override void Generate(Map map, GenStepParams parms)
        {
            if (map == null)
            {
                return;
            }

            TerrainDef baseTerrain = DefDatabase<TerrainDef>.GetNamedSilentFail("ABY_DominionAshMetal")
                ?? DefDatabase<TerrainDef>.GetNamedSilentFail("ABY_DominionScorchedPlate")
                ?? TerrainDefOf.Concrete;

            CellRect whole = new CellRect(0, 0, map.Size.x, map.Size.z);
            foreach (IntVec3 cell in whole)
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                map.terrainGrid.SetTerrain(cell, baseTerrain);

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

            SeedExternalMapHookCompatibilityRock(map);
        }

        /// <summary>
        /// Some external MapGenerated hooks, especially resource-deposit systems, assume every generated map has
        /// at least a few natural mineable cells. The dominion slice is intentionally sterile and fully plated,
        /// which can make those hooks emit red errors while looking for starting cells.
        /// These temporary corner rocks exist only during MapGenerated. Package 2 cleanup removes them immediately
        /// after map creation, together with any external gas/deposit artifacts spawned there.
        /// </summary>
        private static void SeedExternalMapHookCompatibilityRock(Map map)
        {
            ThingDef mineable = DefDatabase<ThingDef>.GetNamedSilentFail("MineableGranite")
                ?? DefDatabase<ThingDef>.GetNamedSilentFail("MineableSandstone")
                ?? DefDatabase<ThingDef>.GetNamedSilentFail("MineableSlate");

            if (mineable == null)
            {
                return;
            }

            TerrainDef roughStone = DefDatabase<TerrainDef>.GetNamedSilentFail("RoughStone")
                ?? DefDatabase<TerrainDef>.GetNamedSilentFail("RoughGranite")
                ?? TerrainDefOf.Concrete;

            SeedCornerRockPocket(map, mineable, roughStone, 5, 5);
            SeedCornerRockPocket(map, mineable, roughStone, map.Size.x - 10, 5);
            SeedCornerRockPocket(map, mineable, roughStone, 5, map.Size.z - 10);
            SeedCornerRockPocket(map, mineable, roughStone, map.Size.x - 10, map.Size.z - 10);
        }

        private static void SeedCornerRockPocket(Map map, ThingDef mineable, TerrainDef terrain, int startX, int startZ)
        {
            for (int x = startX; x < startX + 5; x++)
            {
                for (int z = startZ; z < startZ + 5; z++)
                {
                    IntVec3 cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    map.terrainGrid.SetTerrain(cell, terrain);
                    if (cell.GetEdifice(map) == null && cell.GetFirstThing(map, mineable) == null)
                    {
                        Thing rock = ThingMaker.MakeThing(mineable);
                        GenSpawn.Spawn(rock, cell, map, Rot4.North, WipeMode.Vanish, false, false);
                    }
                }
            }
        }
    }
}
