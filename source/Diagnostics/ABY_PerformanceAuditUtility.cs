using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_PerformanceAuditUtility
    {
        public static List<string> BuildStatusLines()
        {
            List<string> lines = new List<string>();
            AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
            lines.Add("Abyssal Protocol performance audit");
            lines.Add("Visual intensity: " + ABY_PerformanceSettingsUtility.ResolveLabel(settings.visualIntensity));
            lines.Add("Reduced motion: " + settings.reducedMotion);
            lines.Add("Reduced UI animation: " + settings.reduceAbyssalUIAnimation);
            lines.Add("Dominion ambient VFX: " + settings.enableDominionAmbientVfx);
            lines.Add("Boss map effects: " + settings.enableBossMapPresentationEffects);
            lines.Add("Boss title cards: " + settings.enableBossPresentationTitleCards);
            lines.Add("Dominion weather: " + settings.enableDominionWeather + " @ " + settings.ResolveDominionWeatherIntensity().ToString("F2"));
            lines.Add("VFX intensity scale: " + ABY_PerformanceSettingsUtility.ResolveVfxIntensityScale(settings).ToString("F2"));
            lines.Add("Sample VFX interval 120 ticks -> " + ABY_PerformanceSettingsUtility.ScaleVfxInterval(120, settings) + " ticks");

            Map map = Find.CurrentMap;
            if (map == null)
            {
                lines.Add("Current map: none");
                return lines;
            }

            lines.Add("Current map: " + map.uniqueID + " / " + map.Size.x + "x" + map.Size.z);
            lines.Add("Spawned pawns: " + SafeCount(map.mapPawns?.AllPawnsSpawned));
            lines.Add("Player pawns: " + SafeCount(map.mapPawns?.FreeColonistsSpawned));
            lines.Add("Spawned things: " + (map.listerThings?.AllThings?.Count ?? 0));
            lines.Add("Motes/effects: " + CountThingsByCategory(map, ThingCategory.Mote));
            lines.Add("Buildings: " + CountThingsByCategory(map, ThingCategory.Building));
            lines.Add("Abyssal things: " + CountAbyssalThings(map));
            lines.Add("Abyssal pawns: " + CountAbyssalPawns(map));
            lines.Add("Dominion pocket map: " + ABY_DominionAtmosphereUtility.IsDominionPocketMap(map));
            lines.Add("Dominion ambient VFX allowed now: " + ABY_PerformanceSettingsUtility.ShouldRunDominionAmbientVfx(settings));

            TryAddComponentLine<MapComponent_ABY_DominionAtmosphere>(map, lines, "Dominion atmosphere component");
            TryAddComponentLine<MapComponent_DominionSliceEncounter>(map, lines, "Dominion slice encounter component");
            TryAddComponentLine<MapComponent_DominionCrisis>(map, lines, "Dominion crisis component");
            TryAddComponentLine<MapComponent_AbyssalPortalWave>(map, lines, "Portal wave component");
            TryAddComponentLine<MapComponent_AbyssalForgeProgress>(map, lines, "Forge progress component");
            return lines;
        }

        public static string BuildPlainTextReport()
        {
            return string.Join("\n", BuildStatusLines().ToArray());
        }

        public static void LogSnapshot()
        {
            Log.Message("[Abyssal Protocol] Performance audit snapshot | " + string.Join(" | ", BuildStatusLines().ToArray()));
        }

        private static int SafeCount<T>(IEnumerable<T> values)
        {
            return values == null ? 0 : values.Count();
        }

        private static int CountThingsByCategory(Map map, ThingCategory category)
        {
            if (map?.listerThings?.AllThings == null)
            {
                return 0;
            }

            int count = 0;
            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i]?.def?.category == category)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAbyssalThings(Map map)
        {
            if (map?.listerThings?.AllThings == null)
            {
                return 0;
            }

            int count = 0;
            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                if (IsAbyssalDefName(things[i]?.def?.defName))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAbyssalPawns(Map map)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return 0;
            }

            int count = 0;
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (IsAbyssalDefName(pawn?.def?.defName) || IsAbyssalDefName(pawn?.kindDef?.defName))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsAbyssalDefName(string defName)
        {
            return !defName.NullOrEmpty() && (defName.StartsWith("ABY_") || defName.Contains("Abyssal") || defName.Contains("Dominion"));
        }

        private static void TryAddComponentLine<T>(Map map, List<string> lines, string label) where T : MapComponent
        {
            try
            {
                lines.Add(label + ": " + (map.GetComponent<T>() != null ? "present" : "missing"));
            }
            catch
            {
                lines.Add(label + ": unavailable");
            }
        }
    }
}
