using System;
using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_EncounterTelemetryEntry : IExposable
    {
        public string poolId;
        public string templateDefName;
        public string dominantKindDefName;
        public int totalUnits;
        public int supportCount;
        public int eliteCount;
        public int bossCount;
        public int sniperCount;
        public int tick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref poolId, "poolId");
            Scribe_Values.Look(ref templateDefName, "templateDefName");
            Scribe_Values.Look(ref dominantKindDefName, "dominantKindDefName");
            Scribe_Values.Look(ref totalUnits, "totalUnits", 0);
            Scribe_Values.Look(ref supportCount, "supportCount", 0);
            Scribe_Values.Look(ref eliteCount, "eliteCount", 0);
            Scribe_Values.Look(ref bossCount, "bossCount", 0);
            Scribe_Values.Look(ref sniperCount, "sniperCount", 0);
            Scribe_Values.Look(ref tick, "tick", 0);
        }
    }

    public sealed class ABY_EncounterTelemetryGameComponent : GameComponent
    {
        private const int MaxEntries = 18;
        private List<ABY_EncounterTelemetryEntry> recentEntries = new List<ABY_EncounterTelemetryEntry>();

        public ABY_EncounterTelemetryGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref recentEntries, "recentEntries", LookMode.Deep);
            if (recentEntries == null)
            {
                recentEntries = new List<ABY_EncounterTelemetryEntry>();
            }

            Prune();
        }

        public void RecordPlan(AbyssalEncounterDirectorUtility.EncounterPlan plan)
        {
            if (plan == null)
            {
                return;
            }

            ABY_EncounterTelemetryEntry entry = new ABY_EncounterTelemetryEntry
            {
                poolId = plan.PoolId ?? string.Empty,
                templateDefName = plan.TemplateDefName ?? string.Empty,
                dominantKindDefName = ResolveDominantKindDefName(plan),
                totalUnits = plan.TotalUnits,
                supportCount = plan.GetRoleCount("support"),
                eliteCount = plan.GetRoleCount("elite"),
                bossCount = plan.GetRoleCount("boss"),
                sniperCount = plan.GetCount("ABY_RiftSniper"),
                tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0
            };

            recentEntries.Add(entry);
            Prune();
        }

        public List<ABY_EncounterTelemetryEntry> GetRecentEntries(string poolId, int lookback)
        {
            List<ABY_EncounterTelemetryEntry> result = new List<ABY_EncounterTelemetryEntry>();
            if (lookback <= 0 || recentEntries == null || recentEntries.Count == 0)
            {
                return result;
            }

            for (int i = recentEntries.Count - 1; i >= 0 && result.Count < lookback; i--)
            {
                ABY_EncounterTelemetryEntry entry = recentEntries[i];
                if (entry == null)
                {
                    continue;
                }

                if (!string.Equals(entry.poolId ?? string.Empty, poolId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(entry);
            }

            return result;
        }

        private static string ResolveDominantKindDefName(AbyssalEncounterDirectorUtility.EncounterPlan plan)
        {
            string best = string.Empty;
            int bestCount = -1;
            if (plan.Entries != null)
            {
                for (int i = 0; i < plan.Entries.Count; i++)
                {
                    AbyssalEncounterDirectorUtility.DirectedEntry entry = plan.Entries[i];
                    if (entry == null || entry.KindDef == null || entry.Count <= 0)
                    {
                        continue;
                    }

                    if (entry.Count > bestCount)
                    {
                        bestCount = entry.Count;
                        best = entry.KindDef.defName;
                    }
                }
            }

            return best ?? string.Empty;
        }

        private void Prune()
        {
            if (recentEntries == null)
            {
                recentEntries = new List<ABY_EncounterTelemetryEntry>();
                return;
            }

            recentEntries.RemoveAll(entry => entry == null);
            if (recentEntries.Count <= MaxEntries)
            {
                return;
            }

            int removeCount = recentEntries.Count - MaxEntries;
            recentEntries.RemoveRange(0, removeCount);
        }
    }

    public static class ABY_EncounterTelemetryUtility
    {
        private static ABY_EncounterTelemetryGameComponent GetComponent()
        {
            return Current.Game?.GetComponent<ABY_EncounterTelemetryGameComponent>();
        }

        public static void RecordPlan(AbyssalEncounterDirectorUtility.EncounterPlan plan)
        {
            GetComponent()?.RecordPlan(plan);
        }

        public static int GetRecentTemplateHits(string poolId, string templateDefName, int lookback)
        {
            if (templateDefName.NullOrEmpty() || lookback <= 0)
            {
                return 0;
            }

            int hits = 0;
            List<ABY_EncounterTelemetryEntry> entries = GetComponent()?.GetRecentEntries(poolId, lookback);
            if (entries == null)
            {
                return 0;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                ABY_EncounterTelemetryEntry entry = entries[i];
                if (entry != null && string.Equals(entry.templateDefName ?? string.Empty, templateDefName, StringComparison.OrdinalIgnoreCase))
                {
                    hits++;
                }
            }

            return hits;
        }

        public static int GetRecentKindHits(string poolId, string kindDefName, int lookback)
        {
            if (kindDefName.NullOrEmpty() || lookback <= 0)
            {
                return 0;
            }

            int hits = 0;
            List<ABY_EncounterTelemetryEntry> entries = GetComponent()?.GetRecentEntries(poolId, lookback);
            if (entries == null)
            {
                return 0;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                ABY_EncounterTelemetryEntry entry = entries[i];
                if (entry != null && string.Equals(entry.dominantKindDefName ?? string.Empty, kindDefName, StringComparison.OrdinalIgnoreCase))
                {
                    hits++;
                }
            }

            return hits;
        }

        public static bool HadRecentSniperPressure(string poolId, int lookback)
        {
            List<ABY_EncounterTelemetryEntry> entries = GetComponent()?.GetRecentEntries(poolId, lookback);
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if ((entries[i]?.sniperCount ?? 0) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HadRecentSupportPressure(string poolId, int lookback)
        {
            List<ABY_EncounterTelemetryEntry> entries = GetComponent()?.GetRecentEntries(poolId, lookback);
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if ((entries[i]?.supportCount ?? 0) >= 2)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HadRecentLargeWavePressure(string poolId, int lookback)
        {
            List<ABY_EncounterTelemetryEntry> entries = GetComponent()?.GetRecentEntries(poolId, lookback);
            if (entries == null)
            {
                return false;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if ((entries[i]?.totalUnits ?? 0) >= 8)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
