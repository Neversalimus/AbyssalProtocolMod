using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_BestiaryEntryProgress : IExposable
    {
        public string entryId;
        public int killCount;
        public int firstUnlockTick = -1;
        public int lastKillTick = -1;

        public void RecordKill(int tick)
        {
            killCount = Math.Max(0, killCount) + 1;
            if (firstUnlockTick < 0)
            {
                firstUnlockTick = tick;
            }

            lastKillTick = tick;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref entryId, "entryId");
            Scribe_Values.Look(ref killCount, "killCount", 0);
            Scribe_Values.Look(ref firstUnlockTick, "firstUnlockTick", -1);
            Scribe_Values.Look(ref lastKillTick, "lastKillTick", -1);
        }
    }

    public enum ABY_BestiaryCategory
    {
        All,
        Assault,
        Support,
        Elite,
        Boss
    }

    public sealed class ABY_BestiaryEntryDefinition
    {
        public string EntryId;
        public string FallbackLabel;
        public string PortraitPath;
        public ABY_BestiaryCategory Category;
    }

    public sealed class ABY_BestiaryGameComponent : GameComponent
    {
        private const int ScanIntervalTicks = 120;

        private int nextScanTick;
        private int totalTrackedKills;
        private List<int> processedCorpseIds = new List<int>();
        private List<ABY_BestiaryEntryProgress> entryProgress = new List<ABY_BestiaryEntryProgress>();

        private HashSet<int> processedCorpseIdCache;
        private Dictionary<string, ABY_BestiaryEntryProgress> progressCache;

        public ABY_BestiaryGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
            Scribe_Values.Look(ref totalTrackedKills, "totalTrackedKills", 0);
            Scribe_Collections.Look(ref processedCorpseIds, "processedCorpseIds", LookMode.Value);
            Scribe_Collections.Look(ref entryProgress, "entryProgress", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureCollections();
                SanitizeData();
                RebuildCaches();
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            EnsureCollections();
            SanitizeData();
            RebuildCaches();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager == null || Find.Maps == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < nextScanTick)
            {
                return;
            }

            nextScanTick = ticksGame + ScanIntervalTicks;
            ScanMapsForTrackedCorpses();
        }

        public int GetKillCount(string entryId)
        {
            if (entryId.NullOrEmpty())
            {
                return 0;
            }

            EnsureCaches();
            return progressCache.TryGetValue(entryId, out ABY_BestiaryEntryProgress progress) && progress != null
                ? Math.Max(0, progress.killCount)
                : 0;
        }

        public bool IsUnlocked(string entryId)
        {
            return GetKillCount(entryId) > 0;
        }

        public int GetUnlockedEntryCount()
        {
            EnsureCaches();
            int count = 0;
            foreach (ABY_BestiaryEntryDefinition definition in ABY_BestiaryUtility.GetCatalog())
            {
                if (definition != null && IsUnlocked(definition.EntryId))
                {
                    count++;
                }
            }

            return count;
        }

        public int GetTotalTrackedKills()
        {
            return Math.Max(0, totalTrackedKills);
        }

        public ABY_BestiaryEntryProgress GetProgress(string entryId)
        {
            if (entryId.NullOrEmpty())
            {
                return null;
            }

            EnsureCaches();
            progressCache.TryGetValue(entryId, out ABY_BestiaryEntryProgress progress);
            return progress;
        }

        public bool TryRecordCorpse(Corpse corpse)
        {
            if (corpse == null || corpse.Destroyed || corpse.InnerPawn == null)
            {
                return false;
            }

            EnsureCaches();
            int corpseId = corpse.thingIDNumber;
            if (corpseId <= 0 || processedCorpseIdCache.Contains(corpseId))
            {
                return false;
            }

            string entryId = ABY_BestiaryUtility.ResolveTrackedEntryId(corpse.InnerPawn);
            if (entryId.NullOrEmpty())
            {
                return false;
            }

            processedCorpseIds.Add(corpseId);
            processedCorpseIdCache.Add(corpseId);

            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            GetOrCreateProgress(entryId).RecordKill(tick);
            totalTrackedKills = Math.Max(0, totalTrackedKills) + 1;
            return true;
        }

        private void ScanMapsForTrackedCorpses()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map?.listerThings == null)
                {
                    continue;
                }

                List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                if (corpses == null)
                {
                    continue;
                }

                for (int j = 0; j < corpses.Count; j++)
                {
                    if (corpses[j] is Corpse corpse)
                    {
                        TryRecordCorpse(corpse);
                    }
                }
            }
        }

        private ABY_BestiaryEntryProgress GetOrCreateProgress(string entryId)
        {
            EnsureCaches();
            if (progressCache.TryGetValue(entryId, out ABY_BestiaryEntryProgress existing) && existing != null)
            {
                return existing;
            }

            ABY_BestiaryEntryProgress progress = new ABY_BestiaryEntryProgress
            {
                entryId = entryId
            };
            entryProgress.Add(progress);
            progressCache[entryId] = progress;
            return progress;
        }

        private void EnsureCollections()
        {
            processedCorpseIds ??= new List<int>();
            entryProgress ??= new List<ABY_BestiaryEntryProgress>();
        }

        private void EnsureCaches()
        {
            EnsureCollections();
            if (processedCorpseIdCache == null || progressCache == null)
            {
                RebuildCaches();
            }
        }

        private void RebuildCaches()
        {
            EnsureCollections();
            processedCorpseIdCache = new HashSet<int>();
            for (int i = 0; i < processedCorpseIds.Count; i++)
            {
                if (processedCorpseIds[i] > 0)
                {
                    processedCorpseIdCache.Add(processedCorpseIds[i]);
                }
            }

            progressCache = new Dictionary<string, ABY_BestiaryEntryProgress>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entryProgress.Count; i++)
            {
                ABY_BestiaryEntryProgress progress = entryProgress[i];
                if (progress == null || progress.entryId.NullOrEmpty())
                {
                    continue;
                }

                progressCache[progress.entryId] = progress;
            }
        }

        private void SanitizeData()
        {
            EnsureCollections();

            HashSet<int> uniqueCorpseIds = new HashSet<int>();
            processedCorpseIds.RemoveAll(id => id <= 0 || !uniqueCorpseIds.Add(id));
            entryProgress.RemoveAll(progress => progress == null || progress.entryId.NullOrEmpty());

            Dictionary<string, ABY_BestiaryEntryProgress> merged = new Dictionary<string, ABY_BestiaryEntryProgress>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < entryProgress.Count; i++)
            {
                ABY_BestiaryEntryProgress progress = entryProgress[i];
                progress.killCount = Math.Max(0, progress.killCount);

                if (!merged.TryGetValue(progress.entryId, out ABY_BestiaryEntryProgress existing))
                {
                    if (progress.killCount > 0 && progress.firstUnlockTick < 0)
                    {
                        progress.firstUnlockTick = progress.lastKillTick;
                    }

                    merged[progress.entryId] = progress;
                    continue;
                }

                existing.killCount += progress.killCount;
                if (existing.firstUnlockTick < 0 || (progress.firstUnlockTick >= 0 && progress.firstUnlockTick < existing.firstUnlockTick))
                {
                    existing.firstUnlockTick = progress.firstUnlockTick;
                }

                if (progress.lastKillTick > existing.lastKillTick)
                {
                    existing.lastKillTick = progress.lastKillTick;
                }
            }

            entryProgress = new List<ABY_BestiaryEntryProgress>(merged.Values);

            int recomputedTotal = 0;
            for (int i = 0; i < entryProgress.Count; i++)
            {
                recomputedTotal += Math.Max(0, entryProgress[i].killCount);
            }

            totalTrackedKills = Math.Max(totalTrackedKills, recomputedTotal);
        }
    }

    public static class ABY_BestiaryUtility
    {
        public const int StudiedKillThreshold = 15;

        private static readonly List<ABY_BestiaryEntryDefinition> Catalog = new List<ABY_BestiaryEntryDefinition>
        {
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_RiftImp", FallbackLabel = "Rift imp", PortraitPath = "Pawn/RiftImp/ABY_RiftImp_south", Category = ABY_BestiaryCategory.Assault },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_EmberHound", FallbackLabel = "Ember hound", PortraitPath = "Pawn/EmberHound/ABY_EmberHound_south", Category = ABY_BestiaryCategory.Assault },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_HexgunThrall", FallbackLabel = "Hexgun thrall", PortraitPath = "Pawn/HexgunThrall/ABY_HexgunThrall_south", Category = ABY_BestiaryCategory.Assault },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_ChainZealot", FallbackLabel = "Chain zealot", PortraitPath = "Pawn/ChainZealot/ABY_ChainZealot_south", Category = ABY_BestiaryCategory.Elite },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_RiftSapper", FallbackLabel = "Rift sapper", PortraitPath = "Pawn/RiftSapper/ABY_RiftSapper_south", Category = ABY_BestiaryCategory.Support },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_RiftSniper", FallbackLabel = "Rift sniper", PortraitPath = "Pawn/RiftSniper/ABY_RiftSniper_south", Category = ABY_BestiaryCategory.Support },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_NullPriest", FallbackLabel = "Null Priest", PortraitPath = "Pawn/NullPriest/ABY_NullPriest_south", Category = ABY_BestiaryCategory.Support },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_BreachBrute", FallbackLabel = "Breach Brute", PortraitPath = "Pawn/BreachBrute/ABY_BreachBrute_south", Category = ABY_BestiaryCategory.Elite },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_SiegeIdol", FallbackLabel = "Siege Idol", PortraitPath = "Pawn/SiegeIdol/ABY_SiegeIdol_south", Category = ABY_BestiaryCategory.Support },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_Harvester", FallbackLabel = "Harvester", PortraitPath = "Pawn/Harvester/ABY_Harvester_south", Category = ABY_BestiaryCategory.Elite },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_GateWarden", FallbackLabel = "Gate Warden", PortraitPath = "Pawn/GateWarden/ABY_GateWarden_south", Category = ABY_BestiaryCategory.Elite },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_WardenOfAsh", FallbackLabel = "Warden of Ash", PortraitPath = "Pawn/WardenOfAsh/WardenOfAsh_south", Category = ABY_BestiaryCategory.Boss },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_ChoirEngine", FallbackLabel = "Choir Engine", PortraitPath = "Pawn/ChoirEngine/ABY_ChoirEngine_south", Category = ABY_BestiaryCategory.Boss },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_ReactorSaint", FallbackLabel = "Infernal Reactor Saint", PortraitPath = "Pawn/ReactorSaint/ABY_ReactorSaint_south", Category = ABY_BestiaryCategory.Boss },
            new ABY_BestiaryEntryDefinition { EntryId = "ABY_ArchonBeast", FallbackLabel = "Archon Beast", PortraitPath = "Pawn/ArchonBeast/ArchonBeast_south", Category = ABY_BestiaryCategory.Boss }
        };

        private static readonly HashSet<string> TrackedEntryLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ABY_BestiaryEntryDefinition> CatalogLookup = new Dictionary<string, ABY_BestiaryEntryDefinition>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Texture2D> PortraitCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        static ABY_BestiaryUtility()
        {
            for (int i = 0; i < Catalog.Count; i++)
            {
                ABY_BestiaryEntryDefinition definition = Catalog[i];
                if (definition == null || definition.EntryId.NullOrEmpty())
                {
                    continue;
                }

                TrackedEntryLookup.Add(definition.EntryId);
                CatalogLookup[definition.EntryId] = definition;
            }
        }

        private static ABY_BestiaryGameComponent GetComponent()
        {
            return Current.Game?.GetComponent<ABY_BestiaryGameComponent>();
        }

        public static IEnumerable<ABY_BestiaryEntryDefinition> GetCatalog()
        {
            return Catalog;
        }

        public static IEnumerable<ABY_BestiaryEntryDefinition> GetEntriesForCategory(ABY_BestiaryCategory category)
        {
            for (int i = 0; i < Catalog.Count; i++)
            {
                ABY_BestiaryEntryDefinition definition = Catalog[i];
                if (definition == null)
                {
                    continue;
                }

                if (category == ABY_BestiaryCategory.All || definition.Category == category)
                {
                    yield return definition;
                }
            }
        }

        public static int GetTrackedEntryCount()
        {
            return Catalog.Count;
        }

        public static int GetCategoryEntryCount(ABY_BestiaryCategory category)
        {
            int count = 0;
            foreach (ABY_BestiaryEntryDefinition definition in GetEntriesForCategory(category))
            {
                if (definition != null)
                {
                    count++;
                }
            }

            return count;
        }

        public static ABY_BestiaryEntryDefinition GetDefinition(string entryId)
        {
            if (entryId.NullOrEmpty())
            {
                return null;
            }

            CatalogLookup.TryGetValue(entryId, out ABY_BestiaryEntryDefinition definition);
            return definition;
        }

        public static string GetFirstAvailableEntryId(ABY_BestiaryCategory category)
        {
            foreach (ABY_BestiaryEntryDefinition definition in GetEntriesForCategory(category))
            {
                if (definition != null)
                {
                    return definition.EntryId;
                }
            }

            return string.Empty;
        }

        public static bool IsTrackedEntryId(string entryId)
        {
            return !entryId.NullOrEmpty() && TrackedEntryLookup.Contains(entryId);
        }

        public static string ResolveTrackedEntryId(Pawn pawn)
        {
            if (pawn == null)
            {
                return string.Empty;
            }

            string kindDefName = pawn.kindDef?.defName;
            if (IsTrackedEntryId(kindDefName))
            {
                return kindDefName;
            }

            string raceDefName = pawn.def?.defName;
            if (IsTrackedEntryId(raceDefName))
            {
                return raceDefName;
            }

            string kindRaceDefName = pawn.kindDef?.race?.defName;
            if (IsTrackedEntryId(kindRaceDefName))
            {
                return kindRaceDefName;
            }

            return string.Empty;
        }

        public static Texture2D GetPortrait(string entryId)
        {
            if (entryId.NullOrEmpty())
            {
                return null;
            }

            if (PortraitCache.TryGetValue(entryId, out Texture2D cached))
            {
                return cached;
            }

            ABY_BestiaryEntryDefinition definition = GetDefinition(entryId);
            Texture2D texture = definition != null && !definition.PortraitPath.NullOrEmpty()
                ? ContentFinder<Texture2D>.Get(definition.PortraitPath, false)
                : null;
            PortraitCache[entryId] = texture;
            return texture;
        }

        public static string GetDisplayLabel(string entryId)
        {
            if (entryId.NullOrEmpty())
            {
                return string.Empty;
            }

            PawnKindDef pawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(entryId);
            if (pawnKindDef != null && !pawnKindDef.label.NullOrEmpty())
            {
                return pawnKindDef.LabelCap;
            }

            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail(entryId);
            if (thingDef != null && !thingDef.label.NullOrEmpty())
            {
                return thingDef.LabelCap;
            }

            ABY_BestiaryEntryDefinition definition = GetDefinition(entryId);
            return definition?.FallbackLabel ?? entryId;
        }

        public static string GetCategoryLabel(ABY_BestiaryCategory category)
        {
            switch (category)
            {
                case ABY_BestiaryCategory.Assault:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCategory_Assault", "Assault");
                case ABY_BestiaryCategory.Support:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCategory_Support", "Support");
                case ABY_BestiaryCategory.Elite:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCategory_Elite", "Elite");
                case ABY_BestiaryCategory.Boss:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCategory_Boss", "Boss");
                default:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCategory_All", "All");
            }
        }

        public static int GetKillCount(string entryId)
        {
            return GetComponent()?.GetKillCount(entryId) ?? 0;
        }

        public static bool IsUnlocked(string entryId)
        {
            return GetComponent()?.IsUnlocked(entryId) ?? false;
        }

        public static bool IsStudied(string entryId)
        {
            return GetKillCount(entryId) >= StudiedKillThreshold;
        }

        public static int GetUnlockedEntryCount()
        {
            return GetComponent()?.GetUnlockedEntryCount() ?? 0;
        }

        public static int GetStudiedEntryCount()
        {
            int count = 0;
            foreach (ABY_BestiaryEntryDefinition definition in Catalog)
            {
                if (definition != null && IsStudied(definition.EntryId))
                {
                    count++;
                }
            }

            return count;
        }

        public static int GetTotalTrackedKills()
        {
            return GetComponent()?.GetTotalTrackedKills() ?? 0;
        }

        public static ABY_BestiaryEntryProgress GetProgress(string entryId)
        {
            return GetComponent()?.GetProgress(entryId);
        }
    }
}
