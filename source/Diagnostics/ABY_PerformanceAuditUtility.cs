using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_PerformanceAuditUtility
    {
        private const int TopListLimit = 12;
        private const string HeartDefName = "ABY_DominionSliceHeart";
        private const string ExitDefName = "ABY_DominionPocketExit";

        private sealed class CountEntry
        {
            public string Label;
            public int Count;

            public CountEntry(string label)
            {
                Label = label;
                Count = 1;
            }
        }

        public static List<string> BuildStatusLines()
        {
            List<string> lines = new List<string>();
            AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
            lines.Add("Abyssal Protocol performance audit");
            lines.Add("Visual intensity: " + ABY_PerformanceSettingsUtility.ResolveLabel(settings.visualIntensity));
            lines.Add("Reduced motion: " + settings.reducedMotion);
            lines.Add("Reduced UI animation: " + settings.reduceAbyssalUIAnimation);
            lines.Add("Dominion ambient VFX setting: " + settings.enableDominionAmbientVfx);
            lines.Add("Boss map effects: " + settings.enableBossMapPresentationEffects);
            lines.Add("Boss title cards: " + settings.enableBossPresentationTitleCards);
            lines.Add("Dominion weather setting: " + settings.enableDominionWeather + " @ " + settings.ResolveDominionWeatherIntensity().ToString("F2"));
            lines.Add("VFX intensity scale: " + ABY_PerformanceSettingsUtility.ResolveVfxIntensityScale(settings).ToString("F2"));
            lines.Add("Sample VFX interval 120 ticks -> " + ABY_PerformanceSettingsUtility.ScaleVfxInterval(120, settings) + " ticks");

            Map map = Find.CurrentMap;
            if (map == null)
            {
                lines.Add("Current map: none");
                return lines;
            }

            lines.Add("");
            AddMapSummary(lines, map);
            lines.Add("");
            AddAbyssalEntityBreakdown(lines, map);
            lines.Add("");
            AddPortalWaveState(lines, map);
            lines.Add("");
            AddDominionState(lines, map, settings);
            lines.Add("");
            AddComponentPresence(lines, map);
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

        private static void AddMapSummary(List<string> lines, Map map)
        {
            lines.Add("=== Map summary ===");
            lines.Add("Current map: " + map.uniqueID + " / " + map.Size.x + "x" + map.Size.z);
            lines.Add("Spawned pawns: " + SafeCount(map.mapPawns?.AllPawnsSpawned));
            lines.Add("Player pawns: " + SafeCount(map.mapPawns?.FreeColonistsSpawned));
            lines.Add("Spawned things: " + (map.listerThings?.AllThings?.Count ?? 0));
            lines.Add("Motes/effects: " + CountThingsByCategory(map, ThingCategory.Mote));
            lines.Add("Buildings: " + CountThingsByCategory(map, ThingCategory.Building));
            lines.Add("Abyssal things raw: " + CountAbyssalThings(map, true) + " (includes pawns/corpses/items/buildings if present in listers)");
            lines.Add("Abyssal things excluding pawns: " + CountAbyssalThings(map, false));
            lines.Add("Abyssal pawns: " + CountAbyssalPawns(map));
        }

        private static void AddAbyssalEntityBreakdown(List<string> lines, Map map)
        {
            lines.Add("=== Abyssal entity breakdown ===");
            IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns == null || pawns.Count == 0)
            {
                lines.Add("Abyssal pawn kinds: none");
            }
            else
            {
                Dictionary<string, CountEntry> pawnKinds = new Dictionary<string, CountEntry>();
                Dictionary<string, CountEntry> pawnFactions = new Dictionary<string, CountEntry>();
                Dictionary<string, CountEntry> pawnLifeState = new Dictionary<string, CountEntry>();
                int abyssalPawnCount = 0;

                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (!IsAbyssalPawn(pawn))
                    {
                        continue;
                    }

                    abyssalPawnCount++;
                    AddCount(pawnKinds, pawn.kindDef?.defName ?? pawn.def?.defName ?? "(unknown pawn)");
                    AddCount(pawnFactions, pawn.Faction?.def?.defName ?? "(no faction)");
                    AddCount(pawnLifeState, ResolvePawnStateLabel(pawn));
                }

                lines.Add("Abyssal pawn total: " + abyssalPawnCount);
                AddTopCounts(lines, "Top Abyssal PawnKinds", pawnKinds, TopListLimit);
                AddTopCounts(lines, "Abyssal pawn factions", pawnFactions, TopListLimit);
                AddTopCounts(lines, "Abyssal pawn states", pawnLifeState, TopListLimit);
            }

            List<Thing> things = map.listerThings?.AllThings;
            if (things == null || things.Count == 0)
            {
                lines.Add("Abyssal ThingDefs: none");
                return;
            }

            Dictionary<string, CountEntry> thingDefs = new Dictionary<string, CountEntry>();
            Dictionary<string, CountEntry> categories = new Dictionary<string, CountEntry>();
            Dictionary<string, CountEntry> corpseInnerKinds = new Dictionary<string, CountEntry>();
            int abyssalThingCount = 0;
            int abyssalCorpseCount = 0;

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null)
                {
                    continue;
                }

                bool isAbyssalThing = IsAbyssalThing(thing);
                Corpse corpse = thing as Corpse;
                bool isAbyssalCorpse = corpse?.InnerPawn != null && IsAbyssalPawn(corpse.InnerPawn);
                if (!isAbyssalThing && !isAbyssalCorpse)
                {
                    continue;
                }

                abyssalThingCount++;
                AddCount(thingDefs, thing.def?.defName ?? "(unknown thing)");
                AddCount(categories, ResolveThingCategoryLabel(thing));

                if (isAbyssalCorpse)
                {
                    abyssalCorpseCount++;
                    AddCount(corpseInnerKinds, corpse.InnerPawn.kindDef?.defName ?? corpse.InnerPawn.def?.defName ?? "(unknown corpse pawn)");
                }
            }

            lines.Add("Abyssal raw thing total: " + abyssalThingCount);
            lines.Add("Abyssal corpses: " + abyssalCorpseCount);
            AddTopCounts(lines, "Top Abyssal ThingDefs", thingDefs, TopListLimit);
            AddTopCounts(lines, "Abyssal thing categories", categories, TopListLimit);
            AddTopCounts(lines, "Abyssal corpse inner PawnKinds", corpseInnerKinds, TopListLimit);
        }

        private static void AddPortalWaveState(List<string> lines, Map map)
        {
            lines.Add("=== Horde / portal wave state ===");
            MapComponent_AbyssalPortalWave portal = SafeGetComponent<MapComponent_AbyssalPortalWave>(map);
            if (portal == null)
            {
                lines.Add("Portal wave component: missing");
                return;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int nextPortalOpenTick = GetPrivateField<int>(portal, "nextPortalOpenTick", -1);
            int ticksUntilNextPortal = nextPortalOpenTick >= 0 ? Math.Max(0, nextPortalOpenTick - now) : -1;
            object activeGate = GetPrivateField<object>(portal, "activeCommandGate", null);

            lines.Add("Portal wave component: present");
            lines.Add("Portal wave active: " + portal.IsWaveActive);
            lines.Add("Queued portal requests: " + GetPrivateCollectionCount(portal, "queuedPortals"));
            lines.Add("Used portal cells: " + GetPrivateCollectionCount(portal, "usedPortalCells"));
            lines.Add("Front anchor cells: " + GetPrivateCollectionCount(portal, "frontAnchorCells"));
            lines.Add("Next portal open tick: " + (nextPortalOpenTick >= 0 ? nextPortalOpenTick.ToString() : "none") + (ticksUntilNextPortal >= 0 ? " (" + ticksUntilNextPortal + " ticks)" : ""));
            lines.Add("Active horde wave: " + GetPrivateField<bool>(portal, "activeHordeWave", false));
            lines.Add("Closure reward pending: " + GetPrivateField<bool>(portal, "closureRewardPending", false));
            lines.Add("Command reward granted: " + GetPrivateField<bool>(portal, "commandRewardGranted", false));
            lines.Add("Command gate collapsed: " + GetPrivateField<bool>(portal, "commandGateCollapsed", false));
            lines.Add("Active command gate: " + ResolveThingStateLabel(activeGate as Thing));

            MapComponent_ABY_HordeCompletionWatchdog watchdog = SafeGetComponent<MapComponent_ABY_HordeCompletionWatchdog>(map);
            if (watchdog != null)
            {
                int nextWatchdogTick = GetPrivateField<int>(watchdog, "nextWatchdogTick", 0);
                lines.Add("Horde watchdog: present; next check in " + Math.Max(0, nextWatchdogTick - now) + " ticks");
            }
            else
            {
                lines.Add("Horde watchdog: missing");
            }
        }

        private static void AddDominionState(List<string> lines, Map map, AbyssalProtocolModSettings settings)
        {
            lines.Add("=== Dominion state ===");

            MapComponent_ABY_DominionAtmosphere atmosphere = SafeGetComponent<MapComponent_ABY_DominionAtmosphere>(map);
            MapComponent_DominionSliceEncounter slice = SafeGetComponent<MapComponent_DominionSliceEncounter>(map);
            MapComponent_DominionCrisis crisis = SafeGetComponent<MapComponent_DominionCrisis>(map);

            bool settingAllowsAmbient = ABY_PerformanceSettingsUtility.ShouldRunDominionAmbientVfx(settings);
            bool isPocket = ABY_DominionAtmosphereUtility.IsDominionPocketMap(map);
            bool markedAtmosphere = atmosphere != null && atmosphere.MarkedAsDominionSlice;
            bool hasSession = ABY_DominionAtmosphereUtility.TryResolveSession(map, out ABY_DominionPocketSession session);
            bool sliceActive = slice != null && slice.IsActiveEncounter;
            bool atmosphereCanPulse = settingAllowsAmbient && markedAtmosphere;
            bool sliceAmbientCanRun = settingAllowsAmbient && sliceActive;

            lines.Add("Dominion pocket map: " + isPocket + " (" + ResolveDominionPocketReason(map, atmosphere, hasSession, session) + ")");
            lines.Add("Dominion atmosphere marked: " + markedAtmosphere);
            lines.Add("Dominion session linked: " + hasSession + (session != null ? " (" + (session.sessionId ?? "unknown session") + ")" : ""));
            lines.Add("Dominion ambient setting allows VFX: " + settingAllowsAmbient);
            lines.Add("Dominion atmosphere pulse active on this map: " + atmosphereCanPulse + " (" + ResolveDominionAtmosphereReason(settingAllowsAmbient, markedAtmosphere) + ")");
            lines.Add("Dominion slice ambient active on this map: " + sliceAmbientCanRun + " (" + ResolveDominionSliceAmbientReason(settingAllowsAmbient, sliceActive) + ")");

            if (atmosphere != null)
            {
                int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                lines.Add("Dominion atmosphere weather state: " + atmosphere.CurrentWeatherState);
                lines.Add("Dominion atmosphere counters: maintenance=" + GetPrivateField<int>(atmosphere, "maintenanceRuns", 0)
                    + ", ambientPulses=" + GetPrivateField<int>(atmosphere, "ambientPulses", 0)
                    + ", weatherBursts=" + GetPrivateField<int>(atmosphere, "weatherBursts", 0));
                lines.Add("Dominion atmosphere next ticks: scan=" + TicksUntil(GetPrivateField<int>(atmosphere, "nextScanTick", 0), now)
                    + ", ambient=" + TicksUntil(GetPrivateField<int>(atmosphere, "nextAmbientPulseTick", 0), now)
                    + ", weather=" + TicksUntil(GetPrivateField<int>(atmosphere, "nextWeatherTick", 0), now));
            }

            if (slice != null)
            {
                lines.Add("Dominion slice encounter: phase=" + slice.CurrentPhase
                    + ", active=" + slice.IsActiveEncounter
                    + ", liveAnchors=" + slice.LiveAnchorCount
                    + ", liveHeartGuardians=" + slice.LiveHeartGuardianCount
                    + ", waves=" + slice.WavesTriggeredCount
                    + ", hazard=" + slice.HazardPressure);
                lines.Add("Dominion slice last wave: " + NullOr(slice.LastWaveLabel, "none") + " / " + NullOr(slice.LastWaveSummary, "none"));
                lines.Add("Dominion slice references: heart=" + ResolveThingStateLabel(slice.HeartBuilding)
                    + ", tracked anchors=" + GetPrivateCollectionCount(slice, "anchors")
                    + ", tracked fissures=" + GetPrivateCollectionCount(slice, "fissureVisuals")
                    + ", tracked guardians=" + GetPrivateCollectionCount(slice, "heartGuardians"));
            }
            else
            {
                lines.Add("Dominion slice encounter: missing");
            }

            if (crisis != null)
            {
                lines.Add("Dominion crisis: phase=" + crisis.Phase
                    + ", active=" + crisis.IsActive
                    + ", activeAnchors=" + crisis.ActiveAnchorCount + "/" + crisis.InitialAnchorCount
                    + ", waves=" + crisis.WavesTriggered
                    + ", cooldown=" + crisis.HasCooldown
                    + ", terminal=" + crisis.IsTerminal);
                lines.Add("Dominion crisis source: circle=" + ResolveThingStateLabel(crisis.SourceCircle)
                    + ", gate=" + ResolveThingStateLabel(crisis.GateCore)
                    + ", sourceCell=" + crisis.SourceCell);
                lines.Add("Dominion crisis last: wave=" + NullOr(crisis.LastWaveSummary, "none")
                    + ", outcome=" + NullOr(crisis.LastOutcomeReason, "none")
                    + ", maintenance=" + NullOr(crisis.LastMaintenanceSummary, "none"));
            }
            else
            {
                lines.Add("Dominion crisis: missing");
            }

            lines.Add("Dominion map objects: heart=" + CountThingsOfDef(map, HeartDefName)
                + ", exit=" + CountThingsOfDef(map, ExitDefName)
                + ", slice anchors=" + CountDefsContaining(map, "DominionSliceAnchor")
                + ", dominion gates=" + CountDefsContaining(map, "DominionGate"));
        }

        private static void AddComponentPresence(List<string> lines, Map map)
        {
            lines.Add("=== Component presence ===");
            TryAddComponentLine<MapComponent_ABY_DominionAtmosphere>(map, lines, "Dominion atmosphere component");
            TryAddComponentLine<MapComponent_DominionSliceEncounter>(map, lines, "Dominion slice encounter component");
            TryAddComponentLine<MapComponent_DominionCrisis>(map, lines, "Dominion crisis component");
            TryAddComponentLine<MapComponent_AbyssalPortalWave>(map, lines, "Portal wave component");
            TryAddComponentLine<MapComponent_ABY_HordeCompletionWatchdog>(map, lines, "Horde completion watchdog component");
            TryAddComponentLine<MapComponent_AbyssalForgeProgress>(map, lines, "Forge progress component");
        }

        private static string ResolvePawnStateLabel(Pawn pawn)
        {
            if (pawn == null)
            {
                return "(null)";
            }

            if (pawn.Dead)
            {
                return "dead";
            }

            if (pawn.Downed)
            {
                return "downed";
            }

            if (pawn.Spawned)
            {
                return "spawned-active";
            }

            return "not-spawned";
        }

        private static string ResolveThingCategoryLabel(Thing thing)
        {
            if (thing == null)
            {
                return "(null)";
            }

            if (thing is Pawn)
            {
                return "Pawn";
            }

            if (thing is Corpse)
            {
                return "Corpse";
            }

            return thing.def?.category.ToString() ?? "(unknown category)";
        }

        private static string ResolveThingStateLabel(Thing thing)
        {
            if (thing == null)
            {
                return "none";
            }

            string pos = thing.Spawned ? thing.Position.ToString() : "not-spawned";
            return thing.def?.defName + "@" + pos + (thing.Destroyed ? " destroyed" : "");
        }

        private static string NullOr(string value, string fallback)
        {
            return value.NullOrEmpty() ? fallback : value;
        }

        private static string TicksUntil(int tick, int now)
        {
            if (tick <= 0)
            {
                return "none";
            }

            return Math.Max(0, tick - now) + " ticks";
        }

        private static string ResolveDominionPocketReason(Map map, MapComponent_ABY_DominionAtmosphere atmosphere, bool hasSession, ABY_DominionPocketSession session)
        {
            List<string> reasons = new List<string>();
            if (atmosphere != null && atmosphere.MarkedAsDominionSlice)
            {
                reasons.Add("atmosphere marked");
            }

            if (hasSession)
            {
                reasons.Add("runtime session " + (session?.sessionId ?? "unknown"));
            }

            if (CountThingsOfDef(map, HeartDefName) > 0)
            {
                reasons.Add("heart present");
            }

            if (CountThingsOfDef(map, ExitDefName) > 0)
            {
                reasons.Add("exit present");
            }

            return reasons.Count == 0 ? "no pocket markers" : string.Join(", ", reasons.ToArray());
        }

        private static string ResolveDominionAtmosphereReason(bool settingAllowsAmbient, bool markedAtmosphere)
        {
            if (!settingAllowsAmbient)
            {
                return "blocked by visual settings";
            }

            if (!markedAtmosphere)
            {
                return "not marked as Dominion slice";
            }

            return "settings allow and map is marked";
        }

        private static string ResolveDominionSliceAmbientReason(bool settingAllowsAmbient, bool sliceActive)
        {
            if (!settingAllowsAmbient)
            {
                return "blocked by visual settings";
            }

            if (!sliceActive)
            {
                return "slice encounter inactive";
            }

            return "settings allow and slice encounter is active";
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

        private static int CountAbyssalThings(Map map, bool includePawns)
        {
            if (map?.listerThings?.AllThings == null)
            {
                return 0;
            }

            int count = 0;
            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (!includePawns && thing is Pawn)
                {
                    continue;
                }

                if (IsAbyssalThing(thing))
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
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (IsAbyssalPawn(pawns[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsAbyssalThing(Thing thing)
        {
            if (thing == null)
            {
                return false;
            }

            if (thing is Pawn pawn)
            {
                return IsAbyssalPawn(pawn);
            }

            Corpse corpse = thing as Corpse;
            if (corpse?.InnerPawn != null && IsAbyssalPawn(corpse.InnerPawn))
            {
                return true;
            }

            return IsAbyssalDefName(thing.def?.defName);
        }

        private static bool IsAbyssalPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            return IsAbyssalDefName(pawn.def?.defName)
                || IsAbyssalDefName(pawn.kindDef?.defName)
                || IsAbyssalDefName(pawn.Faction?.def?.defName);
        }

        private static bool IsAbyssalDefName(string defName)
        {
            return !defName.NullOrEmpty() && (defName.StartsWith("ABY_") || defName.Contains("Abyssal") || defName.Contains("Dominion"));
        }

        private static void AddCount(Dictionary<string, CountEntry> counts, string label)
        {
            label = label.NullOrEmpty() ? "(empty)" : label;
            if (counts.TryGetValue(label, out CountEntry entry))
            {
                entry.Count++;
            }
            else
            {
                counts[label] = new CountEntry(label);
            }
        }

        private static void AddTopCounts(List<string> lines, string label, Dictionary<string, CountEntry> counts, int limit)
        {
            if (counts == null || counts.Count == 0)
            {
                lines.Add(label + ": none");
                return;
            }

            List<CountEntry> ordered = counts.Values
                .OrderByDescending(e => e.Count)
                .ThenBy(e => e.Label)
                .Take(Mathf.Max(1, limit))
                .ToList();

            lines.Add(label + ":");
            for (int i = 0; i < ordered.Count; i++)
            {
                lines.Add("  - " + ordered[i].Label + ": " + ordered[i].Count);
            }
        }

        private static int CountThingsOfDef(Map map, string defName)
        {
            if (map?.listerThings == null || defName.NullOrEmpty())
            {
                return 0;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return 0;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(def);
            if (things == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null && !thing.Destroyed)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountDefsContaining(Map map, string token)
        {
            if (map?.listerThings?.AllThings == null || token.NullOrEmpty())
            {
                return 0;
            }

            int count = 0;
            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                string defName = things[i]?.def?.defName;
                if (!defName.NullOrEmpty() && defName.Contains(token) && !things[i].Destroyed)
                {
                    count++;
                }
            }

            return count;
        }

        private static T SafeGetComponent<T>(Map map) where T : MapComponent
        {
            try
            {
                return map?.GetComponent<T>();
            }
            catch
            {
                return null;
            }
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

        private static int GetPrivateCollectionCount(object target, string fieldName)
        {
            object value = GetPrivateField<object>(target, fieldName, null);
            if (value == null)
            {
                return 0;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            return 0;
        }

        private static T GetPrivateField<T>(object target, string fieldName, T fallback)
        {
            if (target == null || fieldName.NullOrEmpty())
            {
                return fallback;
            }

            try
            {
                FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field == null)
                {
                    return fallback;
                }

                object value = field.GetValue(target);
                if (value is T cast)
                {
                    return cast;
                }
            }
            catch
            {
            }

            return fallback;
        }
    }
}
