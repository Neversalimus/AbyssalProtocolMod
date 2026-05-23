using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Small negative-cache helper for frequently resolved defs. This is not a replacement
    /// for DefDatabase, but it prevents repeated silent-fail lookups in UI/runtime hot paths
    /// and keeps future C# systems away from ad-hoc per-file caches.
    /// </summary>
    public static class ABY_DefCache
    {
        private static readonly Dictionary<string, HediffDef> HediffDefsByName = new Dictionary<string, HediffDef>();
        private static readonly HashSet<string> MissingHediffDefs = new HashSet<string>();

        private static readonly Dictionary<string, ThingDef> ThingDefsByName = new Dictionary<string, ThingDef>();
        private static readonly HashSet<string> MissingThingDefs = new HashSet<string>();

        private static readonly Dictionary<string, PawnKindDef> PawnKindDefsByName = new Dictionary<string, PawnKindDef>();
        private static readonly HashSet<string> MissingPawnKindDefs = new HashSet<string>();

        private static readonly Dictionary<string, SoundDef> SoundDefsByName = new Dictionary<string, SoundDef>();
        private static readonly HashSet<string> MissingSoundDefs = new HashSet<string>();

        private static readonly Dictionary<string, SongDef> SongDefsByName = new Dictionary<string, SongDef>();
        private static readonly HashSet<string> MissingSongDefs = new HashSet<string>();

        private static readonly Dictionary<string, ResearchProjectDef> ResearchProjectDefsByName = new Dictionary<string, ResearchProjectDef>();
        private static readonly HashSet<string> MissingResearchProjectDefs = new HashSet<string>();

        private static readonly Dictionary<string, RecipeDef> RecipeDefsByName = new Dictionary<string, RecipeDef>();
        private static readonly HashSet<string> MissingRecipeDefs = new HashSet<string>();

        private static readonly Dictionary<string, FactionDef> FactionDefsByName = new Dictionary<string, FactionDef>();
        private static readonly HashSet<string> MissingFactionDefs = new HashSet<string>();

        private static readonly Dictionary<string, TerrainDef> TerrainDefsByName = new Dictionary<string, TerrainDef>();
        private static readonly HashSet<string> MissingTerrainDefs = new HashSet<string>();

        private static readonly Dictionary<string, MapGeneratorDef> MapGeneratorDefsByName = new Dictionary<string, MapGeneratorDef>();
        private static readonly HashSet<string> MissingMapGeneratorDefs = new HashSet<string>();

        public static HediffDef HediffDefNamed(string defName)
        {
            return CachedDefNamed(defName, HediffDefsByName, MissingHediffDefs);
        }

        public static ThingDef ThingDefNamed(string defName)
        {
            return CachedDefNamed(defName, ThingDefsByName, MissingThingDefs);
        }

        public static PawnKindDef PawnKindDefNamed(string defName)
        {
            return CachedDefNamed(defName, PawnKindDefsByName, MissingPawnKindDefs);
        }

        public static SoundDef SoundDefNamed(string defName)
        {
            return CachedDefNamed(defName, SoundDefsByName, MissingSoundDefs);
        }

        public static SongDef SongDefNamed(string defName)
        {
            return CachedDefNamed(defName, SongDefsByName, MissingSongDefs);
        }

        public static ResearchProjectDef ResearchProjectDefNamed(string defName)
        {
            return CachedDefNamed(defName, ResearchProjectDefsByName, MissingResearchProjectDefs);
        }

        public static RecipeDef RecipeDefNamed(string defName)
        {
            return CachedDefNamed(defName, RecipeDefsByName, MissingRecipeDefs);
        }

        public static FactionDef FactionDefNamed(string defName)
        {
            return CachedDefNamed(defName, FactionDefsByName, MissingFactionDefs);
        }

        public static TerrainDef TerrainDefNamed(string defName)
        {
            return CachedDefNamed(defName, TerrainDefsByName, MissingTerrainDefs);
        }

        public static MapGeneratorDef MapGeneratorDefNamed(string defName)
        {
            return CachedDefNamed(defName, MapGeneratorDefsByName, MissingMapGeneratorDefs);
        }

        public static void ClearAll()
        {
            HediffDefsByName.Clear();
            MissingHediffDefs.Clear();
            ThingDefsByName.Clear();
            MissingThingDefs.Clear();
            PawnKindDefsByName.Clear();
            MissingPawnKindDefs.Clear();
            SoundDefsByName.Clear();
            MissingSoundDefs.Clear();
            SongDefsByName.Clear();
            MissingSongDefs.Clear();
            ResearchProjectDefsByName.Clear();
            MissingResearchProjectDefs.Clear();
            RecipeDefsByName.Clear();
            MissingRecipeDefs.Clear();
            FactionDefsByName.Clear();
            MissingFactionDefs.Clear();
            TerrainDefsByName.Clear();
            MissingTerrainDefs.Clear();
            MapGeneratorDefsByName.Clear();
            MissingMapGeneratorDefs.Clear();
        }

        private static T CachedDefNamed<T>(string defName, Dictionary<string, T> cache, HashSet<string> missing) where T : Def
        {
            if (defName.NullOrEmpty())
            {
                return null;
            }

            if (cache.TryGetValue(defName, out T cached))
            {
                return cached;
            }

            if (missing.Contains(defName))
            {
                return null;
            }

            T resolved = DefDatabase<T>.GetNamedSilentFail(defName);
            if (resolved == null)
            {
                missing.Add(defName);
                return null;
            }

            cache[defName] = resolved;
            return resolved;
        }
    }
}
