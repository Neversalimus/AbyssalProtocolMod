using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public enum ABY_BestiaryCategory
    {
        Assault,
        Elite,
        Support,
        Boss
    }

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

    public sealed class ABY_BestiaryEntryDefinition
    {
        public string EntryId;
        public ABY_BestiaryCategory Category;
        public string PortraitPath;
        public string LabelKey;
        public string LabelFallback;
        public string TagKey;
        public string TagFallback;
        public string SummaryKey;
        public string SummaryFallback;
        public string TacticalKey;
        public string TacticalFallback;
        public string DeepKey;
        public string DeepFallback;
    }

    public sealed class ABY_BestiaryGameComponent : GameComponent
    {
        private const int ScanIntervalTicks = 120;
        private const int NotificationWarmupTicks = 240;

        private int nextScanTick;
        private int totalTrackedKills;
        private int notificationArmedTick;
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
            Scribe_Values.Look(ref notificationArmedTick, "notificationArmedTick", 0);
            Scribe_Collections.Look(ref processedCorpseIds, "processedCorpseIds", LookMode.Value);
            Scribe_Collections.Look(ref entryProgress, "entryProgress", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureCollections();
                SanitizeData();
                RebuildCaches();
                ArmNotifications();
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            EnsureCollections();
            SanitizeData();
            RebuildCaches();
            ArmNotifications();
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
            return progressCache.TryGetValue(entryId, out ABY_BestiaryEntryProgress progress) && progress != null ? Math.Max(0, progress.killCount) : 0;
        }

        public bool IsUnlocked(string entryId) => GetKillCount(entryId) > 0;
        public bool HasTacticalData(string entryId) => GetKillCount(entryId) >= ABY_BestiaryUtility.TacticalThreshold;
        public bool IsStudied(string entryId) => GetKillCount(entryId) >= ABY_BestiaryUtility.StudiedThreshold;

        public int GetUnlockedEntryCount()
        {
            EnsureCaches();
            int count = 0;
            foreach (ABY_BestiaryEntryDefinition entry in ABY_BestiaryUtility.GetTrackedEntries())
            {
                if (entry != null && IsUnlocked(entry.EntryId))
                {
                    count++;
                }
            }
            return count;
        }

        public int GetStudiedEntryCount()
        {
            EnsureCaches();
            int count = 0;
            foreach (ABY_BestiaryEntryDefinition entry in ABY_BestiaryUtility.GetTrackedEntries())
            {
                if (entry != null && IsStudied(entry.EntryId))
                {
                    count++;
                }
            }
            return count;
        }

        public int GetTotalTrackedKills() => Math.Max(0, totalTrackedKills);

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
            int previousBonus = ABY_BestiaryRewardUtility.GetExtractionBonusPercent();
            ABY_BestiaryEntryProgress progress = GetOrCreateProgress(entryId);
            int previousKills = Math.Max(0, progress.killCount);
            processedCorpseIds.Add(corpseId);
            processedCorpseIdCache.Add(corpseId);
            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            progress.RecordKill(tick);
            totalTrackedKills = Math.Max(0, totalTrackedKills) + 1;
            TrySendNotifications(entryId, previousKills, progress.killCount, previousBonus, tick);
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

        private void TrySendNotifications(string entryId, int previousKills, int currentKills, int previousBonus, int tick)
        {
            if (tick < notificationArmedTick)
            {
                return;
            }
            string label = ABY_BestiaryUtility.GetEntryLabel(entryId);
            if (previousKills <= 0 && currentKills > 0)
            {
                Messages.Message(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryMessage_Unlocked", "New threat codex entry unlocked: {0}", label), MessageTypeDefOf.PositiveEvent, false);
            }
            if (previousKills < ABY_BestiaryUtility.TacticalThreshold && currentKills >= ABY_BestiaryUtility.TacticalThreshold)
            {
                Messages.Message(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryMessage_FieldNotes", "Field notes expanded: {0}", label), MessageTypeDefOf.PositiveEvent, false);
            }
            if (previousKills < ABY_BestiaryUtility.StudiedThreshold && currentKills >= ABY_BestiaryUtility.StudiedThreshold)
            {
                int newBonus = ABY_BestiaryRewardUtility.GetExtractionBonusPercent();
                string studied = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryMessage_Studied", "Threat codex entry studied: {0}", label);
                if (newBonus > previousBonus)
                {
                    studied += "\n" + AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryMessage_BonusUp", "Archive extraction bonus increased to +{0}%.", newBonus);
                }
                Messages.Message(studied, MessageTypeDefOf.PositiveEvent, false);
            }
        }

        private ABY_BestiaryEntryProgress GetOrCreateProgress(string entryId)
        {
            EnsureCaches();
            if (progressCache.TryGetValue(entryId, out ABY_BestiaryEntryProgress existing) && existing != null)
            {
                return existing;
            }
            ABY_BestiaryEntryProgress progress = new ABY_BestiaryEntryProgress { entryId = entryId };
            entryProgress.Add(progress);
            progressCache[entryId] = progress;
            return progress;
        }

        private void EnsureCollections()
        {
            if (processedCorpseIds == null) processedCorpseIds = new List<int>();
            if (entryProgress == null) entryProgress = new List<ABY_BestiaryEntryProgress>();
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
                if (processedCorpseIds[i] > 0) processedCorpseIdCache.Add(processedCorpseIds[i]);
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

        private void ArmNotifications()
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (notificationArmedTick <= 0)
            {
                notificationArmedTick = ticks + NotificationWarmupTicks;
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class ABY_BestiaryUtility
    {
        public const int TacticalThreshold = 5;
        public const int StudiedThreshold = 15;

        private static readonly List<ABY_BestiaryEntryDefinition> TrackedEntries = new List<ABY_BestiaryEntryDefinition>
        {
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_RiftImp",
                Category = ABY_BestiaryCategory.Assault,
                PortraitPath = "Pawn/RiftImp/ABY_RiftImp_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_RiftImp_Label",
                LabelFallback = @"Rift Imp",
                TagKey = "ABY_Bestiary_Entry_ABY_RiftImp_Tag",
                TagFallback = @"Fast expendable breach-form.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_RiftImp_Summary",
                SummaryFallback = @"Cheap shock fauna pushed through unstable tears to overload firing lines and consume attention.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_RiftImp_Tactical",
                TacticalFallback = @"Rift Imps are weak alone but dangerous in numbers. Expect them to flood doors, punish isolated shooters and screen heavier abyssal units.",
                DeepKey = "ABY_Bestiary_Entry_ABY_RiftImp_Deep",
                DeepFallback = @"The codex classifies Rift Imps as disposable breach biomass: not soldiers, but living pressure used to turn a contained engagement into a chaotic opening."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_EmberHound",
                Category = ABY_BestiaryCategory.Assault,
                PortraitPath = "Pawn/EmberHound/ABY_EmberHound_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_EmberHound_Label",
                LabelFallback = @"Ember Hound",
                TagKey = "ABY_Bestiary_Entry_ABY_EmberHound_Tag",
                TagFallback = @"Flanking predator bred for pursuit.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_EmberHound_Summary",
                SummaryFallback = @"Fast abyssal hounds used to reach backlines, punish kiting and force the colony to defend more than its main firing lane.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_EmberHound_Tactical",
                TacticalFallback = @"Watch for sudden lateral pressure. Ember Hounds excel at collapsing weak side rooms, chasing wounded pawns and punishing overextended ranged teams.",
                DeepKey = "ABY_Bestiary_Entry_ABY_EmberHound_Deep",
                DeepFallback = @"Unlike imps, hounds are not pure disposable mass. They embody pursuit doctrine: speed, angle denial and panic spread across the colony interior."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_HexgunThrall",
                Category = ABY_BestiaryCategory.Assault,
                PortraitPath = "Pawn/HexgunThrall/ABY_HexgunThrall_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_HexgunThrall_Label",
                LabelFallback = @"Hexgun Thrall",
                TagKey = "ABY_Bestiary_Entry_ABY_HexgunThrall_Tag",
                TagFallback = @"Disciplined line shooter of the breach.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_HexgunThrall_Summary",
                SummaryFallback = @"Armed cult-infantry that stabilizes abyssal pressure with direct fire, creating a more military breach pattern than feral swarm forms.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_HexgunThrall_Tactical",
                TacticalFallback = @"Thralls are the first sign a breach has shifted from feral pressure to doctrine. Prioritize them if you need to reduce sustained ranged attrition.",
                DeepKey = "ABY_Bestiary_Entry_ABY_HexgunThrall_Deep",
                DeepFallback = @"The archive notes that thralls are important psychologically: they prove the abyss does not merely spawn monsters — it fields obedient armed cadres."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_ChainZealot",
                Category = ABY_BestiaryCategory.Elite,
                PortraitPath = "Pawn/ChainZealot/ABY_ChainZealot_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_ChainZealot_Label",
                LabelFallback = @"Chain Zealot",
                TagKey = "ABY_Bestiary_Entry_ABY_ChainZealot_Tag",
                TagFallback = @"Armored fanatic for breach pressure.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_ChainZealot_Summary",
                SummaryFallback = @"Mid-tier assault fanatic combining pressure armor, melee threat and morale impact inside narrow approach lanes.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_ChainZealot_Tactical",
                TacticalFallback = @"Chain Zealots are most dangerous when imps or hounds already occupy your shooters. Kill their screen first or they will arrive on favorable terms.",
                DeepKey = "ABY_Bestiary_Entry_ABY_ChainZealot_Deep",
                DeepFallback = @"Their presence suggests the breach has committed enough value to justify armored doctrinal melee, not just opportunistic swarm attrition."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_RiftSniper",
                Category = ABY_BestiaryCategory.Support,
                PortraitPath = "Pawn/RiftSniper/ABY_RiftSniper_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_RiftSniper_Label",
                LabelFallback = @"Rift Sniper",
                TagKey = "ABY_Bestiary_Entry_ABY_RiftSniper_Tag",
                TagFallback = @"Precision eliminator of exposed assets.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_RiftSniper_Summary",
                SummaryFallback = @"A long-range abyssal marksman used to punish exposed specialists, turret crews and thinly protected operators.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_RiftSniper_Tactical",
                TacticalFallback = @"Treat a Rift Sniper as a force multiplier. Even a single sightline can make ordinary breach pressure dramatically more lethal.",
                DeepKey = "ABY_Bestiary_Entry_ABY_RiftSniper_Deep",
                DeepFallback = @"Codex inference: the sniper role indicates the abyss can prosecute selective target denial, not merely broad aggression."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_NullPriest",
                Category = ABY_BestiaryCategory.Support,
                PortraitPath = "Pawn/NullPriest/ABY_NullPriest_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_NullPriest_Label",
                LabelFallback = @"Null Priest",
                TagKey = "ABY_Bestiary_Entry_ABY_NullPriest_Tag",
                TagFallback = @"Ritual support and field corruption node.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_NullPriest_Summary",
                SummaryFallback = @"A support conductor that turns a normal firefight into a ritualized pressure zone through debuffs, local distortion and allied support.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_NullPriest_Tactical",
                TacticalFallback = @"Null Priests are priority disruption targets. Leaving one alive too long usually means every other abyssal pawn becomes harder to cleanly stabilize.",
                DeepKey = "ABY_Bestiary_Entry_ABY_NullPriest_Deep",
                DeepFallback = @"The archive classifies priests as proof that some abyssal hostiles do not fight for kills alone; they reshape the combat state itself."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_BreachBrute",
                Category = ABY_BestiaryCategory.Elite,
                PortraitPath = "Pawn/BreachBrute/ABY_BreachBrute_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_BreachBrute_Label",
                LabelFallback = @"Breach Brute",
                TagKey = "ABY_Bestiary_Entry_ABY_BreachBrute_Tag",
                TagFallback = @"Heavy breach ram under direct fire.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_BreachBrute_Summary",
                SummaryFallback = @"A slow armored brute intended to absorb concentrated punishment while opening room for the rest of the abyssal force.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_BreachBrute_Tactical",
                TacticalFallback = @"Brutes punish colonies that rely on a single perfect firing lane. If you cannot burst them down, be ready to rotate and delay.",
                DeepKey = "ABY_Bestiary_Entry_ABY_BreachBrute_Deep",
                DeepFallback = @"The codex records breach brutes as living siege mass: a body designed less to duel and more to force the defender to spend ammunition, time and formation discipline."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_SiegeIdol",
                Category = ABY_BestiaryCategory.Support,
                PortraitPath = "Pawn/SiegeIdol/ABY_SiegeIdol_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_SiegeIdol_Label",
                LabelFallback = @"Siege Idol",
                TagKey = "ABY_Bestiary_Entry_ABY_SiegeIdol_Tag",
                TagFallback = @"Artillery node for breach escalation.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_SiegeIdol_Summary",
                SummaryFallback = @"A semi-mechanical support engine that translates abyssal pressure into long-range demolition and positional punishment.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_SiegeIdol_Tactical",
                TacticalFallback = @"Once a Siege Idol is online, cover geometry becomes unstable. Static colonies will bleed value if the idol is not answered quickly.",
                DeepKey = "ABY_Bestiary_Entry_ABY_SiegeIdol_Deep",
                DeepFallback = @"The archive treats idols as an important doctrinal marker: the abyss is not adapting human artillery — it is digesting siege logic into its own ritual machine form."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_Harvester",
                Category = ABY_BestiaryCategory.Elite,
                PortraitPath = "Pawn/Harvester/ABY_Harvester_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_Harvester_Label",
                LabelFallback = @"Harvester",
                TagKey = "ABY_Bestiary_Entry_ABY_Harvester_Tag",
                TagFallback = @"Battlefield recycler of death-state energy.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_Harvester_Summary",
                SummaryFallback = @"A specialist entity that becomes more dangerous as bodies accumulate, exploiting the battlefield as a resource rather than mere terrain.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_Harvester_Tactical",
                TacticalFallback = @"Do not let a Harvester operate uncontested during a messy fight. It thrives when the colony loses tempo and leaves corpses in active lanes.",
                DeepKey = "ABY_Bestiary_Entry_ABY_Harvester_Deep",
                DeepFallback = @"Codex note: Harvesters demonstrate the abyssal principle that slaughter itself is material — not only a result, but a usable throughput."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_GateWarden",
                Category = ABY_BestiaryCategory.Elite,
                PortraitPath = "Pawn/GateWarden/ABY_GateWarden_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_GateWarden_Label",
                LabelFallback = @"Gate Warden",
                TagKey = "ABY_Bestiary_Entry_ABY_GateWarden_Tag",
                TagFallback = @"Elite frontliner guarding high-value nodes.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_GateWarden_Summary",
                SummaryFallback = @"A compact elite guardian used to anchor important breach positions and protect boss-side assets from direct retaliation.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_GateWarden_Tactical",
                TacticalFallback = @"Wardens trade mobility for denial. Expect them near bosses, objectives and high-value breach anchors rather than in expendable waves.",
                DeepKey = "ABY_Bestiary_Entry_ABY_GateWarden_Deep",
                DeepFallback = @"The archive marks wardens as personalized security, not generic mass: the abyss assigns them where a local right of access must be defended."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_WardenOfAsh",
                Category = ABY_BestiaryCategory.Boss,
                PortraitPath = "Pawn/WardenOfAsh/WardenOfAsh_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_WardenOfAsh_Label",
                LabelFallback = @"Warden of Ash",
                TagKey = "ABY_Bestiary_Entry_ABY_WardenOfAsh_Tag",
                TagFallback = @"First structured mini-boss of the ashbound tier.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_WardenOfAsh_Summary",
                SummaryFallback = @"An early progression boss that combines armor, area pulse pressure and reinforcement timing into a disciplined breach test.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_WardenOfAsh_Tactical",
                TacticalFallback = @"This is the point where the abyss stops only probing and starts evaluating. Clustering and lazy melee swarms are especially punishable here.",
                DeepKey = "ABY_Bestiary_Entry_ABY_WardenOfAsh_Deep",
                DeepFallback = @"The codex treats the Warden of Ash as a threshold administrator: not a supreme entity, but the first hostile expressly built to test readiness for deeper escalation."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_ChoirEngine",
                Category = ABY_BestiaryCategory.Boss,
                PortraitPath = "Pawn/ChoirEngine/ABY_ChoirEngine_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_ChoirEngine_Label",
                LabelFallback = @"Choir Engine",
                TagKey = "ABY_Bestiary_Entry_ABY_ChoirEngine_Tag",
                TagFallback = @"Support boss that weaponizes coordination.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_ChoirEngine_Summary",
                SummaryFallback = @"A technoritual support engine focused less on raw damage and more on turning surrounding hostiles into a synchronized pressure lattice.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_ChoirEngine_Tactical",
                TacticalFallback = @"The Choir Engine is a priority command target. The longer it remains online, the less any single hostile near it behaves like an isolated threat.",
                DeepKey = "ABY_Bestiary_Entry_ABY_ChoirEngine_Deep",
                DeepFallback = @"Archive inference: the Choir Engine proves the abyssal war-machine contains a coordination layer akin to liturgy, computation and command at once."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_ReactorSaint",
                Category = ABY_BestiaryCategory.Boss,
                PortraitPath = "Pawn/ReactorSaint/ABY_ReactorSaint_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_ReactorSaint_Label",
                LabelFallback = @"Infernal Reactor Saint",
                TagKey = "ABY_Bestiary_Entry_ABY_ReactorSaint_Tag",
                TagFallback = @"Biothermal saint-machine of late escalation.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_ReactorSaint_Summary",
                SummaryFallback = @"A late boss blending sanctified imagery, reactor pressure, defensive layering and artillery-grade punishment.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_ReactorSaint_Tactical",
                TacticalFallback = @"The Reactor Saint punishes static confidence. It forces the colony to answer shields, pressure zones and heavy ranged spikes in one encounter.",
                DeepKey = "ABY_Bestiary_Entry_ABY_ReactorSaint_Deep",
                DeepFallback = @"The codex treats this entity as evidence that the abyss can industrialize sanctity itself, turning reverence, reactor logic and violence into a single chassis."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_ArchonBeast",
                Category = ABY_BestiaryCategory.Boss,
                PortraitPath = "Pawn/ArchonBeast/ArchonBeast_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_ArchonBeast_Label",
                LabelFallback = @"Archon Beast",
                TagKey = "ABY_Bestiary_Entry_ABY_ArchonBeast_Tag",
                TagFallback = @"Domain boss carrying local law of rupture.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_ArchonBeast_Summary",
                SummaryFallback = @"The first true archon-class boss: a hostile that does not merely attack a map, but attempts to impose a deeper abyssal state upon it.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_ArchonBeast_Tactical",
                TacticalFallback = @"Treat every phase as a state change, not a damage race. The Archon Beast is dangerous because it changes the encounter frame around itself.",
                DeepKey = "ABY_Bestiary_Entry_ABY_ArchonBeast_Deep",
                DeepFallback = @"Archive classification: local administrator of rupture pressure. The visible body is only the readable front of a larger hostile authority."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_RiftSapper",
                Category = ABY_BestiaryCategory.Support,
                PortraitPath = "Pawn/RiftSapper/ABY_RiftSapper_east",
                LabelKey = "ABY_Bestiary_Entry_ABY_RiftSapper_Label",
                LabelFallback = @"Rift Sapper",
                TagKey = "ABY_Bestiary_Entry_ABY_RiftSapper_Tag",
                TagFallback = @"Demolition specialist of breach geometry.",
                SummaryKey = "ABY_Bestiary_Entry_ABY_RiftSapper_Summary",
                SummaryFallback = @"A specialist hostile used to destabilize structures, openings and prepared defensive geometry rather than simply trading fire head-on.",
                TacticalKey = "ABY_Bestiary_Entry_ABY_RiftSapper_Tactical",
                TacticalFallback = @"Rift Sappers should be intercepted early. Their value comes from changing the map on favorable terms for the rest of the breach package.",
                DeepKey = "ABY_Bestiary_Entry_ABY_RiftSapper_Deep",
                DeepFallback = @"The codex tags sappers as applied geometry warfare: proof that abyssal pressure can target fortification logic, not just bodies."
            },
        };

        private static readonly Dictionary<string, ABY_BestiaryEntryDefinition> EntryLookup = BuildEntryLookup();
        private static readonly Dictionary<string, Texture2D> PortraitCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        private static ABY_BestiaryGameComponent GetComponent() => Current.Game?.GetComponent<ABY_BestiaryGameComponent>();

        private static Dictionary<string, ABY_BestiaryEntryDefinition> BuildEntryLookup()
        {
            Dictionary<string, ABY_BestiaryEntryDefinition> dict = new Dictionary<string, ABY_BestiaryEntryDefinition>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < TrackedEntries.Count; i++)
            {
                ABY_BestiaryEntryDefinition entry = TrackedEntries[i];
                if (entry != null && !entry.EntryId.NullOrEmpty())
                {
                    dict[entry.EntryId] = entry;
                }
            }
            return dict;
        }

        public static IEnumerable<ABY_BestiaryEntryDefinition> GetTrackedEntries() => TrackedEntries;
        public static IEnumerable<string> GetTrackedEntryIds() { for (int i = 0; i < TrackedEntries.Count; i++) yield return TrackedEntries[i].EntryId; }
        public static int GetTrackedEntryCount() => TrackedEntries.Count;

        public static ABY_BestiaryEntryDefinition GetEntry(string entryId)
        {
            if (entryId.NullOrEmpty()) return null;
            EntryLookup.TryGetValue(entryId, out ABY_BestiaryEntryDefinition entry);
            return entry;
        }

        public static bool IsTrackedEntryId(string entryId) => !entryId.NullOrEmpty() && EntryLookup.ContainsKey(entryId);

        public static string ResolveTrackedEntryId(Pawn pawn)
        {
            if (pawn == null) return string.Empty;
            string kindDefName = pawn.kindDef?.defName;
            if (IsTrackedEntryId(kindDefName)) return kindDefName;
            string raceDefName = pawn.def?.defName;
            if (IsTrackedEntryId(raceDefName)) return raceDefName;
            string kindRaceDefName = pawn.kindDef?.race?.defName;
            if (IsTrackedEntryId(kindRaceDefName)) return kindRaceDefName;
            return string.Empty;
        }

        public static int GetKillCount(string entryId) => GetComponent()?.GetKillCount(entryId) ?? 0;
        public static bool IsUnlocked(string entryId) => GetComponent()?.IsUnlocked(entryId) ?? false;
        public static bool HasTacticalData(string entryId) => GetComponent()?.HasTacticalData(entryId) ?? false;
        public static bool IsStudied(string entryId) => GetComponent()?.IsStudied(entryId) ?? false;
        public static int GetUnlockedEntryCount() => GetComponent()?.GetUnlockedEntryCount() ?? 0;
        public static int GetStudiedEntryCount() => GetComponent()?.GetStudiedEntryCount() ?? 0;
        public static int GetTotalTrackedKills() => GetComponent()?.GetTotalTrackedKills() ?? 0;
        public static ABY_BestiaryEntryProgress GetProgress(string entryId) => GetComponent()?.GetProgress(entryId);

        public static string GetEntryLabel(string entryId)
        {
            ABY_BestiaryEntryDefinition entry = GetEntry(entryId);
            return entry != null ? AbyssalSummoningConsoleUtility.TranslateOrFallback(entry.LabelKey, entry.LabelFallback) : entryId ?? string.Empty;
        }

        public static string GetUnknownEntryLabel() => AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_UnknownEntry", "Unknown hostile pattern");
        public static string GetTagline(string entryId) { ABY_BestiaryEntryDefinition entry = GetEntry(entryId); return entry != null ? AbyssalSummoningConsoleUtility.TranslateOrFallback(entry.TagKey, entry.TagFallback) : string.Empty; }
        public static string GetSummary(string entryId) { ABY_BestiaryEntryDefinition entry = GetEntry(entryId); return entry != null ? AbyssalSummoningConsoleUtility.TranslateOrFallback(entry.SummaryKey, entry.SummaryFallback) : string.Empty; }
        public static string GetTacticalNote(string entryId) { ABY_BestiaryEntryDefinition entry = GetEntry(entryId); return entry != null ? AbyssalSummoningConsoleUtility.TranslateOrFallback(entry.TacticalKey, entry.TacticalFallback) : string.Empty; }
        public static string GetDeepRecord(string entryId) { ABY_BestiaryEntryDefinition entry = GetEntry(entryId); return entry != null ? AbyssalSummoningConsoleUtility.TranslateOrFallback(entry.DeepKey, entry.DeepFallback) : string.Empty; }

        public static string GetCategoryLabel(ABY_BestiaryCategory category)
        {
            switch (category)
            {
                case ABY_BestiaryCategory.Elite: return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Category_Elite", "Elite");
                case ABY_BestiaryCategory.Support: return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Category_Support", "Support");
                case ABY_BestiaryCategory.Boss: return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Category_Boss", "Boss");
                default: return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Category_Assault", "Assault");
            }
        }

        public static string GetCategoryLabel(string entryId)
        {
            ABY_BestiaryEntryDefinition entry = GetEntry(entryId);
            return entry != null ? GetCategoryLabel(entry.Category) : string.Empty;
        }

        public static Texture2D GetPortrait(string entryId)
        {
            if (entryId.NullOrEmpty()) return null;
            if (PortraitCache.TryGetValue(entryId, out Texture2D cached)) return cached;
            ABY_BestiaryEntryDefinition entry = GetEntry(entryId);
            Texture2D texture = entry != null && !entry.PortraitPath.NullOrEmpty() ? ContentFinder<Texture2D>.Get(entry.PortraitPath, false) : null;
            PortraitCache[entryId] = texture;
            return texture;
        }

        public static int GetKillsUntilNextStage(string entryId)
        {
            int kills = GetKillCount(entryId);
            if (kills < TacticalThreshold) return TacticalThreshold - kills;
            if (kills < StudiedThreshold) return StudiedThreshold - kills;
            return 0;
        }

        public static string GetStatusLabel(string entryId)
        {
            if (IsStudied(entryId)) return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Status_Studied", "Studied");
            if (IsUnlocked(entryId)) return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Status_Discovered", "Discovered");
            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Status_Locked", "Locked");
        }

        public static string GetMilestoneSummary(string entryId)
        {
            int kills = GetKillCount(entryId);
            if (kills < TacticalThreshold)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Milestone_Tactical", "{0} kills until field notes unlock.", TacticalThreshold - kills);
            }
            if (kills < StudiedThreshold)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Milestone_Studied", "{0} kills until studied status.", StudiedThreshold - kills);
            }
            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Milestone_Complete", "Archive thresholds complete.");
        }
    }
}