using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Sterile cleanup layer for dominion pocket maps in heavy modpacks.
    /// Package 12 v2: removes external gas/deposit artifacts without calling Destroy() on non-destroyable geysers.
    /// </summary>
    public static class AbyssalDominionSterileMapUtility
    {
        public static bool IsDominionSliceMap(Map map)
        {
            if (map == null)
            {
                return false;
            }

            if (MapComponent_ABY_SterileAbyssalMap.IsSterile(map))
            {
                return true;
            }

            string parentDefName = map.Parent?.def?.defName ?? string.Empty;
            if (string.Equals(parentDefName, "ABY_DominionSliceSite", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string generatorDefName = ResolveGeneratorDefName(map);
            return string.Equals(generatorDefName, "ABY_DominionSlicePocketMap", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveGeneratorDefName(Map map)
        {
            try
            {
                FieldInfo field = typeof(Map).GetField("generatorDef", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.GetValue(map) is Def def)
                {
                    return def.defName ?? string.Empty;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        public static bool ShouldSkipExternalMapGeneratedDepositLogic(Map map)
        {
            return IsDominionSliceMap(map);
        }

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
                if (!ShouldRemoveExternalArtifact(thing, map))
                {
                    continue;
                }

                TryRemoveExternalArtifact(thing);
            }
        }

        private static void TryRemoveExternalArtifact(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return;
            }

            try
            {
                if (thing.Spawned)
                {
                    thing.DeSpawn(DestroyMode.Vanish);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Could not despawn external dominion map artifact " + SafeThingDefName(thing) + ": " + ex.Message);
                return;
            }

            try
            {
                if (!thing.Destroyed && thing.def != null && thing.def.destroyable)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Could not destroy external dominion map artifact " + SafeThingDefName(thing) + ": " + ex.Message);
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

            if (!IsDominionSliceMap(map))
            {
                return false;
            }

            if (thing is Pawn)
            {
                return false;
            }

            string defName = thing.def.defName ?? string.Empty;
            if (defName.StartsWith("ABY_", StringComparison.OrdinalIgnoreCase))
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

            if (text.Contains("helixien") || text.Contains("vhelixien") || text.Contains("vhge") || text.Contains("vfe_gas"))
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
            if (!defName.StartsWith("Mineable", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            IntVec3 cell = thing.PositionHeld;
            return cell.x <= 20 || cell.z <= 20 || cell.x >= map.Size.x - 21 || cell.z >= map.Size.z - 21
                || cell.DistanceTo(map.Center) <= 18f;
        }

        private static string SafeThingDefName(Thing thing)
        {
            try
            {
                return thing?.def?.defName ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
