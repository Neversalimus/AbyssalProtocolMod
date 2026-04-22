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
        }
    }
}
