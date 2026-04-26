using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Package 2: cleanup layer for dominion pocket maps in heavy modpacks.
    /// It deliberately runs only on maps explicitly marked as sterile dominion pockets.
    /// Reactor Saint, normal colony maps, horde maps and boss arrival maps are not touched.
    /// </summary>
    public static class AbyssalDominionSterileMapUtility
    {
        public static void MarkAndSanitizeAfterGeneration(Map map)
        {
            if (map == null)
            {
                return;
            }

            map.GetComponent<MapComponent_ABY_SterileAbyssalMap>()?.MarkSterileDominionPocket();
            NormalizeInitialTerrain(map);
            SanitizeExternalArtifactsOnly(map);
        }

        public static void SanitizeExternalArtifactsOnly(Map map)
        {
            if (map == null)
            {
                return;
            }

            map.GetComponent<MapComponent_ABY_SterileAbyssalMap>()?.MarkSterileDominionPocket();

            if (map.listerThings?.AllThings == null)
            {
                return;
            }

            List<Thing> things = map.listerThings.AllThings.ToList();
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (ShouldRemoveExternalArtifact(thing, map))
                {
                    try
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static void NormalizeInitialTerrain(Map map)
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

                if (map.terrainGrid != null && baseTerrain != null)
                {
                    map.terrainGrid.SetTerrain(cell, baseTerrain);
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

        private static bool ShouldRemoveExternalArtifact(Thing thing, Map map)
        {
            if (thing == null || thing.Destroyed || thing.def == null || map == null)
            {
                return false;
            }

            if (!MapComponent_ABY_SterileAbyssalMap.IsSterile(map))
            {
                return false;
            }

            if (thing is Pawn)
            {
                return false;
            }

            string defName = thing.def.defName ?? string.Empty;
            if (defName.StartsWith("ABY_"))
            {
                return false;
            }

            if (thing.Faction == Faction.OfPlayer)
            {
                return false;
            }

            string label = thing.def.label ?? string.Empty;
            string text = (defName + " " + label).ToLowerInvariant();

            if (IsCompatibilityRock(thing, map))
            {
                return true;
            }

            if (text.Contains("helixien") || text.Contains("vhelixien") || text.Contains("vfe_gas"))
            {
                return true;
            }

            if (text.Contains("gas") && (text.Contains("deposit") || text.Contains("vent") || text.Contains("geyser")))
            {
                return true;
            }

            if (text.Contains("geyser") || text.Contains("steamgeyser"))
            {
                return true;
            }

            if (text.Contains("deposit") && (text.Contains("infinite") || text.Contains("resource") || text.Contains("ore")))
            {
                return true;
            }

            if (text.Contains("ancient") && (text.Contains("ruin") || text.Contains("junk") || text.Contains("debris")))
            {
                return true;
            }

            if (thing.def.category == ThingCategory.Building && thing.def.building != null && thing.def.building.isNaturalRock)
            {
                return true;
            }

            return false;
        }

        private static bool IsCompatibilityRock(Thing thing, Map map)
        {
            if (thing?.def == null || map == null)
            {
                return false;
            }

            string defName = thing.def.defName ?? string.Empty;
            if (!defName.StartsWith("Mineable"))
            {
                return false;
            }

            IntVec3 cell = thing.PositionHeld;
            return cell.x <= 13 || cell.z <= 13 || cell.x >= map.Size.x - 14 || cell.z >= map.Size.z - 14;
        }
    }
}
