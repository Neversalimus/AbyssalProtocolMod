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
        public string TagLineKey;
        public string TagLineFallback;
        public string SummaryKey;
        public string SummaryFallback;
        public string TacticalKey;
        public string TacticalFallback;
        public string StudiedKey;
        public string StudiedFallback;
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
        public const int TacticalKillThreshold = 5;
        public const int StudiedKillThreshold = 15;

        private static readonly List<ABY_BestiaryEntryDefinition> Catalog = new List<ABY_BestiaryEntryDefinition>
        {
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_RiftImp",
                FallbackLabel = "Rift imp",
                PortraitPath = "Pawn/RiftImp/ABY_RiftImp_south",
                Category = ABY_BestiaryCategory.Assault,
                TagLineKey = "ABY_BestiaryEntry_RiftImp_Tags",
                TagLineFallback = "swarm / rush / breach filler",
                SummaryKey = "ABY_BestiaryEntry_RiftImp_Summary",
                SummaryFallback = "The most disposable abyssal pattern in the archive. Rift imps pour through weak breaches, close distance fast, and turn a thin perimeter into a melee problem.",
                TacticalKey = "ABY_BestiaryEntry_RiftImp_Tactical",
                TacticalFallback = "Do not waste long windups on isolated imps. Kill them in lanes, deny doors, and keep backline shooters screened so the swarm cannot chain engagements.",
                StudiedKey = "ABY_BestiaryEntry_RiftImp_Studied",
                StudiedFallback = "Rift imps are not sent to win cleanly. They are sent to overload reaction time, reveal weak sectors, and buy space for heavier patterns behind them."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_EmberHound",
                FallbackLabel = "Ember hound",
                PortraitPath = "Pawn/EmberHound/ABY_EmberHound_south",
                Category = ABY_BestiaryCategory.Assault,
                TagLineKey = "ABY_BestiaryEntry_EmberHound_Tags",
                TagLineFallback = "flanker / hunter / backline pressure",
                SummaryKey = "ABY_BestiaryEntry_EmberHound_Summary",
                SummaryFallback = "A fast infernal hunter built to break firing rhythm. Ember hounds circle toward exposed shooters and punish any gap between the front line and the colony interior.",
                TacticalKey = "ABY_BestiaryEntry_EmberHound_Tactical",
                TacticalFallback = "They become far less dangerous when forced into straight approaches. Staggered chokepoints, side blockers, and short-range interceptors keep them from reaching vulnerable guns.",
                StudiedKey = "ABY_BestiaryEntry_EmberHound_Studied",
                StudiedFallback = "Ember hounds exist to make range feel unsafe. They are less a front assault than a tempo weapon aimed at panic, repositioning, and failed screening."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_HexgunThrall",
                FallbackLabel = "Hexgun thrall",
                PortraitPath = "Pawn/HexgunThrall/ABY_HexgunThrall_south",
                Category = ABY_BestiaryCategory.Assault,
                TagLineKey = "ABY_BestiaryEntry_HexgunThrall_Tags",
                TagLineFallback = "ranged line / suppression / target marking",
                SummaryKey = "ABY_BestiaryEntry_HexgunThrall_Summary",
                SummaryFallback = "A disciplined infernal gun-servitor that supports the breach from medium range. Hexgun thralls punish exposed defenders and keep steady fire on any lane the host is trying to claim.",
                TacticalKey = "ABY_BestiaryEntry_HexgunThrall_Tactical",
                TacticalFallback = "Treat them as formation glue, not as harmless filler. Breaking line of sight, forcing retargets, and collapsing on them quickly reduces a lot of incoming pressure.",
                StudiedKey = "ABY_BestiaryEntry_HexgunThrall_Studied",
                StudiedFallback = "Thralls are the archive's first proof that the host does not rely on beasts alone. They are doctrinal infantry: cheap enough to field, disciplined enough to anchor the rest."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_ChainZealot",
                FallbackLabel = "Chain zealot",
                PortraitPath = "Pawn/ChainZealot/ABY_ChainZealot_south",
                Category = ABY_BestiaryCategory.Elite,
                TagLineKey = "ABY_BestiaryEntry_ChainZealot_Tags",
                TagLineFallback = "shock melee / drag pressure / line breaker",
                SummaryKey = "ABY_BestiaryEntry_ChainZealot_Summary",
                SummaryFallback = "A chained fanatic built to crash directly into defended lanes. Chain zealots absorb the first punishment, then pin shooters long enough for the rest of the breach to arrive.",
                TacticalKey = "ABY_BestiaryEntry_ChainZealot_Tactical",
                TacticalFallback = "Do not meet them with isolated marksmen. Focused burst fire, layered melee blockers, and clean retreat routes matter more than raw kiting once a zealot commits.",
                StudiedKey = "ABY_BestiaryEntry_ChainZealot_Studied",
                StudiedFallback = "The zealot pattern is less about dueling and more about disruption. Its job is to force bodies out of cover, break neat firing geometry, and open a hole for others."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_RiftSapper",
                FallbackLabel = "Rift sapper",
                PortraitPath = "Pawn/RiftSapper/ABY_RiftSapper_south",
                Category = ABY_BestiaryCategory.Support,
                TagLineKey = "ABY_BestiaryEntry_RiftSapper_Tags",
                TagLineFallback = "breach utility / cover breaker / demolition",
                SummaryKey = "ABY_BestiaryEntry_RiftSapper_Summary",
                SummaryFallback = "A breach-utility abyssal infantry form built to destabilize defended firing lines. Rift sappers advance behind lighter rushers, probe for weak cover, and drive infernal spike charges through barricades, doors, and clustered defenders.",
                TacticalKey = "ABY_BestiaryEntry_RiftSapper_Tactical",
                TacticalFallback = "If left alone, sapper fire will chew through doors, turrets, and sandbags until safer units can walk in. Prioritize them whenever your defense depends on static cover.",
                StudiedKey = "ABY_BestiaryEntry_RiftSapper_Studied",
                StudiedFallback = "Sappers are a support-demolition layer, not a pure assault form. They arrive to make existing fortifications stop functioning as intended."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_RiftSniper",
                FallbackLabel = "Rift sniper",
                PortraitPath = "Pawn/RiftSniper/ABY_RiftSniper_south",
                Category = ABY_BestiaryCategory.Support,
                TagLineKey = "ABY_BestiaryEntry_RiftSniper_Tags",
                TagLineFallback = "long range / precision kill / vision disruption",
                SummaryKey = "ABY_BestiaryEntry_RiftSniper_Summary",
                SummaryFallback = "An elite abyssal marksman grown for disciplined long-angle execution fire. Rift snipers hold extreme distance, settle into a brief target lock, and punish exposed shooters with distortion-laced lance rounds that blur vision and collapse aim.",
                TacticalKey = "ABY_BestiaryEntry_RiftSniper_Tactical",
                TacticalFallback = "Never give them a calm lane. Smoke, hard cover rotation, and pressure on their firing angle matter more than armor once they settle into target lock.",
                StudiedKey = "ABY_BestiaryEntry_RiftSniper_Studied",
                StudiedFallback = "The sniper pattern shows that abyssal escalation is comfortable with patience. It exists to punish colonies that believe distance alone is safety."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_NullPriest",
                FallbackLabel = "Null Priest",
                PortraitPath = "Pawn/NullPriest/ABY_NullPriest_south",
                Category = ABY_BestiaryCategory.Support,
                TagLineKey = "ABY_BestiaryEntry_NullPriest_Tags",
                TagLineFallback = "support caster / armor unravel / flank catalyst",
                SummaryKey = "ABY_BestiaryEntry_NullPriest_Summary",
                SummaryFallback = "A mid-tier abyssal field-priest built to make the rest of the breach more dangerous without serving as a raw damage monster itself. Null priests harden nearby abyssal bodies, unravel defensive layers on priority targets, and open short-lived micro-rifts that spill flank pressure into otherwise stable firing lines.",
                TacticalKey = "ABY_BestiaryEntry_NullPriest_Tactical",
                TacticalFallback = "Killing the priest often weakens the entire local push. When one appears, watch for allied hardening and micro-rift flank pressure instead of tunneling only on frontliners.",
                StudiedKey = "ABY_BestiaryEntry_NullPriest_Studied",
                StudiedFallback = "Null priests are force multipliers. They rarely win the breach by raw damage, but they make every nearby hostile behave above baseline."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_BreachBrute",
                FallbackLabel = "Breach Brute",
                PortraitPath = "Pawn/BreachBrute/ABY_BreachBrute_south",
                Category = ABY_BestiaryCategory.Elite,
                TagLineKey = "ABY_BestiaryEntry_BreachBrute_Tags",
                TagLineFallback = "heavy frontliner / soak / breach push",
                SummaryKey = "ABY_BestiaryEntry_BreachBrute_Summary",
                SummaryFallback = "A furnace-heavy abyssal assault mass grown to cross firing lanes and stay upright long after lighter manifestations have burned away. Breach brutes advance with crushing patience, soak disproportionate punishment, and turn any line they reach into a short-range demolition problem.",
                TacticalKey = "ABY_BestiaryEntry_BreachBrute_Tactical",
                TacticalFallback = "Do not panic when a brute stays upright under fire — that is the point. Slow it, redirect it, and kill the support behind it before it reaches short-range demolition distance.",
                StudiedKey = "ABY_BestiaryEntry_BreachBrute_Studied",
                StudiedFallback = "The brute pattern is a moving patience test. It does not need to be efficient; it only needs to survive long enough for the rest of the host to inherit the lane."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_SiegeIdol",
                FallbackLabel = "Siege Idol",
                PortraitPath = "Pawn/SiegeIdol/ABY_SiegeIdol_south",
                Category = ABY_BestiaryCategory.Support,
                TagLineKey = "ABY_BestiaryEntry_SiegeIdol_Tags",
                TagLineFallback = "siege support / turret pressure / plasma anchor",
                SummaryKey = "ABY_BestiaryEntry_SiegeIdol_Summary",
                SummaryFallback = "A semi-mechanical breach idol built to follow the abyssal line rather than lead it. Siege idols advance behind the first contact wave, lock their stabilizers into the ground, and answer turrets, doors, cover, and static gunlines with furnace-bright plasma fire.",
                TacticalKey = "ABY_BestiaryEntry_SiegeIdol_Tactical",
                TacticalFallback = "An idol becomes far more dangerous when allowed to root and fire repeatedly. Break line of sight, force displacement, or alpha it before it stabilizes behind the breach line.",
                StudiedKey = "ABY_BestiaryEntry_SiegeIdol_Studied",
                StudiedFallback = "The idol is infrastructure in walking form. It follows assaults to solve static defenses, not to lead them."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_Harvester",
                FallbackLabel = "Harvester",
                PortraitPath = "Pawn/Harvester/ABY_Harvester_south",
                Category = ABY_BestiaryCategory.Elite,
                TagLineKey = "ABY_BestiaryEntry_Harvester_Tags",
                TagLineFallback = "escalation collector / residue feed / late threat",
                SummaryKey = "ABY_BestiaryEntry_Harvester_Summary",
                SummaryFallback = "An essence-scavenging abyssal collector built to grow more dangerous as the rest of the host dies around it. Harvesters prowl just behind the first breach line, feeding on fresh allied losses, hardening themselves in the process, and stripping still-hot corpses into more usable infernal residue.",
                TacticalKey = "ABY_BestiaryEntry_Harvester_Tactical",
                TacticalFallback = "The longer allied corpses remain around it, the worse it gets. Clean up the harvester early whenever a fight is already becoming crowded with abyssal dead.",
                StudiedKey = "ABY_BestiaryEntry_Harvester_Studied",
                StudiedFallback = "Harvester pressure turns attrition into economy. It is the codex pattern most openly built to profit from battlefield loss."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_GateWarden",
                FallbackLabel = "Gate Warden",
                PortraitPath = "Pawn/GateWarden/ABY_GateWarden_south",
                Category = ABY_BestiaryCategory.Elite,
                TagLineKey = "ABY_BestiaryEntry_GateWarden_Tags",
                TagLineFallback = "elite guard / shield wall / interception",
                SummaryKey = "ABY_BestiaryEntry_GateWarden_Summary",
                SummaryFallback = "An infernal pretorian forged to hold space for stronger abyssal authorities. Gate wardens brace into plated wall-postures, surge into short interception bursts when their assigned node is threatened, and punish anything that reaches them with a crushing shield bash.",
                TacticalKey = "ABY_BestiaryEntry_GateWarden_Tactical",
                TacticalFallback = "Wardens are easier to manage before they anchor around a protected objective or boss. Pull them off their post or burst them before the rest of the encounter hides behind them.",
                StudiedKey = "ABY_BestiaryEntry_GateWarden_Studied",
                StudiedFallback = "This is a custody pattern. Gate wardens exist to hold space for something more important than themselves."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_WardenOfAsh",
                FallbackLabel = "Warden of Ash",
                PortraitPath = "Pawn/WardenOfAsh/WardenOfAsh_south",
                Category = ABY_BestiaryCategory.Boss,
                TagLineKey = "ABY_BestiaryEntry_WardenOfAsh_Tags",
                TagLineFallback = "mini-boss / anti-cluster / ash pulse",
                SummaryKey = "ABY_BestiaryEntry_WardenOfAsh_Summary",
                SummaryFallback = "An infernal techno-warden built as the first disciplined overseer of abyssal breach pressure. Its armor is ash-black and furnace-heavy, with a contained portal embedded in the chest to mark it as both executioner and living gate-node.",
                TacticalKey = "ABY_BestiaryEntry_WardenOfAsh_Tactical",
                TacticalFallback = "Spacing matters more than bravery. Its local pulse pressure and reinforcement behavior punish colonies that stack too tightly around one firing knot.",
                StudiedKey = "ABY_BestiaryEntry_WardenOfAsh_Studied",
                StudiedFallback = "The Warden is the archive's first true overseer-tier pattern: not yet a full domain boss, but clearly designed to command a breach instead of merely joining it."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_ChoirEngine",
                FallbackLabel = "Choir Engine",
                PortraitPath = "Pawn/ChoirEngine/ABY_ChoirEngine_south",
                Category = ABY_BestiaryCategory.Boss,
                TagLineKey = "ABY_BestiaryEntry_ChoirEngine_Tags",
                TagLineFallback = "command relay / signal noise / support suppression",
                SummaryKey = "ABY_BestiaryEntry_ChoirEngine_Summary",
                SummaryFallback = "A post-first-boss techno-infernal relay node that enters battle as a walking command choir rather than a pure kill-form. Choir engines turn nearby abyssal troops into a tighter, deadlier formation while flooding defenders, turrets, and local electronics with hostile signal noise.",
                TacticalKey = "ABY_BestiaryEntry_ChoirEngine_Tactical",
                TacticalFallback = "The danger is cumulative. Leaving a choir engine alive makes every nearby abyssal pawn cleaner, steadier, and harder to answer while your electronics perform worse.",
                StudiedKey = "ABY_BestiaryEntry_ChoirEngine_Studied",
                StudiedFallback = "Choir engines are command architecture given legs. Their purpose is coordination, signal warfare, and forcing defenders to fight the entire formation at once."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_ReactorSaint",
                FallbackLabel = "Infernal Reactor Saint",
                PortraitPath = "Pawn/ReactorSaint/ABY_ReactorSaint_south",
                Category = ABY_BestiaryCategory.Boss,
                TagLineKey = "ABY_BestiaryEntry_ReactorSaint_Tags",
                TagLineFallback = "full boss / phase shield / artillery lattice",
                SummaryKey = "ABY_BestiaryEntry_ReactorSaint_Summary",
                SummaryFallback = "A colossal techno-infernal saint built around a bound reactor heart and armored missile shoulders. Its phase shield, saturation field, and artillery lattice mark it as a second-tier full boss built to stand well above ordinary abyssal troops and punish static defenses.",
                TacticalKey = "ABY_BestiaryEntry_ReactorSaint_Tactical",
                TacticalFallback = "Static defenses and clumped shooters give the saint exactly the battlefield it wants. Use spread, rotation, and shield-break timing instead of trying to facetank the saturation field.",
                StudiedKey = "ABY_BestiaryEntry_ReactorSaint_Studied",
                StudiedFallback = "The saint is not just larger than earlier patterns; it is a different tier of answer. It combines boss durability, field control, and siege logic in one chassis."
            },
            new ABY_BestiaryEntryDefinition
            {
                EntryId = "ABY_ArchonBeast",
                FallbackLabel = "Archon Beast",
                PortraitPath = "Pawn/ArchonBeast/ArchonBeast_south",
                Category = ABY_BestiaryCategory.Boss,
                TagLineKey = "ABY_BestiaryEntry_ArchonBeast_Tags",
                TagLineFallback = "full boss / rupture herald / phase assault",
                SummaryKey = "ABY_BestiaryEntry_ArchonBeast_Summary",
                SummaryFallback = "A colossal infernal war-beast grown as the living herald of a deeper rupture. It is not the true archon, only the chained predator sent ahead of it.",
                TacticalKey = "ABY_BestiaryEntry_ArchonBeast_Tactical",
                TacticalFallback = "Respect every phase transition. The beast is most lethal when defenders assume the current rhythm will continue and fail to reposition before the next rupture sequence.",
                StudiedKey = "ABY_BestiaryEntry_ArchonBeast_Studied",
                StudiedFallback = "The archive reads the beast as a herald predator, not a final authority. Even in death it implies that something higher delegated it forward."
            }
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

        public static bool IsTacticalNoteUnlocked(string entryId)
        {
            return GetKillCount(entryId) >= TacticalKillThreshold;
        }

        public static string GetTagLine(string entryId)
        {
            ABY_BestiaryEntryDefinition definition = GetDefinition(entryId);
            return ResolveEntryText(definition?.TagLineKey, definition?.TagLineFallback);
        }

        public static string GetSummaryText(string entryId)
        {
            ABY_BestiaryEntryDefinition definition = GetDefinition(entryId);
            return ResolveEntryText(definition?.SummaryKey, definition?.SummaryFallback);
        }

        public static string GetTacticalText(string entryId)
        {
            ABY_BestiaryEntryDefinition definition = GetDefinition(entryId);
            return ResolveEntryText(definition?.TacticalKey, definition?.TacticalFallback);
        }

        public static string GetStudiedText(string entryId)
        {
            ABY_BestiaryEntryDefinition definition = GetDefinition(entryId);
            return ResolveEntryText(definition?.StudiedKey, definition?.StudiedFallback);
        }

        public static string GetArchiveStateLabel(string entryId)
        {
            if (IsStudied(entryId))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryStatus_Studied", "Studied");
            }

            if (IsUnlocked(entryId))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryStatus_Discovered", "Discovered");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryStatus_Locked", "Locked");
        }

        private static string ResolveEntryText(string key, string fallback)
        {
            return key.NullOrEmpty()
                ? (fallback ?? string.Empty)
                : AbyssalSummoningConsoleUtility.TranslateOrFallback(key, fallback ?? string.Empty);
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
