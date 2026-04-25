using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_DefCache
    {
        private static readonly Dictionary<string, HediffDef> HediffDefsByName = new Dictionary<string, HediffDef>();
        private static readonly HashSet<string> MissingHediffDefs = new HashSet<string>();

        private static readonly Dictionary<string, ThingDef> ThingDefsByName = new Dictionary<string, ThingDef>();
        private static readonly HashSet<string> MissingThingDefs = new HashSet<string>();

        private static readonly Dictionary<string, SongDef> SongDefsByName = new Dictionary<string, SongDef>();
        private static readonly HashSet<string> MissingSongDefs = new HashSet<string>();

        public static HediffDef HediffDefNamed(string defName)
        {
            if (defName.NullOrEmpty())
            {
                return null;
            }

            if (HediffDefsByName.TryGetValue(defName, out HediffDef cached))
            {
                return cached;
            }

            if (MissingHediffDefs.Contains(defName))
            {
                return null;
            }

            HediffDef resolved = DefDatabase<HediffDef>.GetNamedSilentFail(defName);
            if (resolved == null)
            {
                MissingHediffDefs.Add(defName);
                return null;
            }

            HediffDefsByName[defName] = resolved;
            return resolved;
        }

        public static ThingDef ThingDefNamed(string defName)
        {
            if (defName.NullOrEmpty())
            {
                return null;
            }

            if (ThingDefsByName.TryGetValue(defName, out ThingDef cached))
            {
                return cached;
            }

            if (MissingThingDefs.Contains(defName))
            {
                return null;
            }

            ThingDef resolved = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (resolved == null)
            {
                MissingThingDefs.Add(defName);
                return null;
            }

            ThingDefsByName[defName] = resolved;
            return resolved;
        }

        public static SongDef SongDefNamed(string defName)
        {
            if (defName.NullOrEmpty())
            {
                return null;
            }

            if (SongDefsByName.TryGetValue(defName, out SongDef cached))
            {
                return cached;
            }

            if (MissingSongDefs.Contains(defName))
            {
                return null;
            }

            SongDef resolved = DefDatabase<SongDef>.GetNamedSilentFail(defName);
            if (resolved == null)
            {
                MissingSongDefs.Add(defName);
                return null;
            }

            SongDefsByName[defName] = resolved;
            return resolved;
        }
    }
}
