using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public static class ABY_SummonThreatRehearsalUtility
    {
        private static readonly string[] RitualOrder =
        {
            "unstable_breach",
            "ember_hunt",
            "warden_of_ash",
            "archon_beast",
            "choir_engine",
            "reactor_saint",
            "horde_gate",
            "rift_butcher",
            "dominion_gate"
        };

        public static void OpenMenu(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null || circle.Destroyed || !circle.Spawned || circle.Map == null)
            {
                Messages.Message("No spawned Abyssal Summoning Circle selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<RitualEntry> rituals = GetRitualEntries();
            if (rituals.Count == 0)
            {
                Messages.Message("No summon sigil use-effect definitions were found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("Rehearse all active summon rituals", delegate
                {
                    LogAll(circle, rituals);
                }),
                new FloatMenuOption("Run preflight reliability pass (all active rituals)", delegate
                {
                    RunPreflightReliabilityPass(circle, rituals);
                })
            };

            for (int i = 0; i < rituals.Count; i++)
            {
                RitualEntry entry = rituals[i];
                string label = GetMenuLabel(entry);
                if (IsRetired(entry.Props))
                {
                    options.Add(new FloatMenuOption("Rehearse retired: " + label, delegate
                    {
                        LogRehearsal(circle, entry);
                    }));
                    continue;
                }

                options.Add(new FloatMenuOption("Rehearse: " + label, delegate
                {
                    LogRehearsal(circle, entry);
                }));

                options.Add(new FloatMenuOption("Force-start without sigil: " + label, delegate
                {
                    ForceStart(circle, entry);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void RunPreflightReliabilityPass(Building_AbyssalSummoningCircle circle, List<RitualEntry> rituals)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Abyssal Protocol] DEV SUMMON PREFLIGHT RELIABILITY PASS");
            sb.AppendLine("Map: " + GetMapLabel(circle?.Map));
            sb.AppendLine("This pass is diagnostic-only. A BLOCKED result may be expected when the current colony lacks a sigil, unlock, power or modules.");
            sb.AppendLine();

            int coherentReports = 0;
            int malformedReports = 0;
            for (int i = 0; i < rituals.Count; i++)
            {
                RitualEntry entry = rituals[i];
                if (entry?.Props == null || IsRetired(entry.Props))
                {
                    continue;
                }

                ABY_SummonPreflightReport report = ABY_SummonPreflightReport.Create(circle, entry.Props);
                bool coherent = report.Entries != null && report.Entries.Count >= 7 && (report.CanStart || !report.PrimaryBlocker.NullOrEmpty());
                if (coherent)
                {
                    coherentReports++;
                }
                else
                {
                    malformedReports++;
                }

                sb.AppendLine("== " + GetMenuLabel(entry) + " ==");
                sb.AppendLine("Report coherence: " + (coherent ? "PASS" : "FAIL"));
                report.AppendDiagnosticReport(sb);
                sb.AppendLine();
            }

            sb.AppendLine("Summary: coherent reports=" + coherentReports + " | malformed reports=" + malformedReports + ".");
            Log.Message(sb.ToString());
            Messages.Message(
                malformedReports == 0
                    ? "Summon preflight reliability pass logged."
                    : "Summon preflight reliability pass found malformed reports. Check the player log.",
                malformedReports == 0 ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.RejectInput,
                false);
        }

        private static void LogAll(Building_AbyssalSummoningCircle circle, List<RitualEntry> rituals)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Abyssal Protocol] DEV THREAT REHEARSAL — all active summon rituals");
            sb.AppendLine("Map: " + GetMapLabel(circle.Map));
            sb.AppendLine("Circle: " + circle.Position + " | powered=" + circle.IsPoweredForRitual + " | ritualActive=" + circle.RitualActive);
            sb.AppendLine("Colonists: " + AbyssalT1SummonScalingUtility.GetActiveColonistCount(circle.Map) + " | wealth=" + Mathf.RoundToInt(circle.Map?.wealthWatcher?.WealthTotal ?? 0f));
            sb.AppendLine();

            for (int i = 0; i < rituals.Count; i++)
            {
                RitualEntry entry = rituals[i];
                if (IsRetired(entry.Props))
                {
                    continue;
                }

                AppendRehearsal(sb, circle, entry, compact: true);
                sb.AppendLine();
            }

            Log.Message(sb.ToString());
            Messages.Message("Abyssal threat rehearsal logged for all active rituals.", MessageTypeDefOf.NeutralEvent, false);
        }

        private static void LogRehearsal(Building_AbyssalSummoningCircle circle, RitualEntry entry)
        {
            StringBuilder sb = new StringBuilder();
            AppendRehearsal(sb, circle, entry, compact: false);
            Log.Message(sb.ToString());
            Messages.Message("Abyssal threat rehearsal logged: " + GetMenuLabel(entry), MessageTypeDefOf.NeutralEvent, false);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
        }

        private static void ForceStart(Building_AbyssalSummoningCircle circle, RitualEntry entry)
        {
            if (circle == null || entry?.Props == null)
            {
                Messages.Message("Missing circle or summon properties for force-start.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (IsRetired(entry.Props))
            {
                Messages.Message("This ritual is retired and cannot be force-started: " + (entry.Props.ritualId ?? entry.Def?.defName ?? "unknown"), MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Do not show a concurrent-encounter confirmation for a request that cannot
            // start on this specific circle anyway.  The dev bypass deliberately ignores
            // only the global map lock, never the circle's own physical readiness.
            if (circle.RitualActive)
            {
                Messages.Message("DEV force-start blocked: the selected Summoning Circle is already running a ritual.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!circle.IsPoweredForRitual)
            {
                Messages.Message("DEV force-start blocked: the selected Summoning Circle is unpowered.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!circle.HasValidInteractionCell(out string interactionReason))
            {
                Messages.Message("DEV force-start blocked: " + (interactionReason ?? "The interaction cell is invalid."), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!circle.HasClearRitualFocus(out string focusReason))
            {
                Messages.Message("DEV force-start blocked: " + (focusReason ?? "The ritual focus is obstructed."), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (AbyssalBossSummonUtility.TryGetActiveAbyssalEncounterBlocker(circle.Map, out string encounterBlocker))
            {
                string confirmation = AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_DevRehearsal_ConcurrentConfirm",
                    "Another Abyssal encounter is already active:\n\n{0}\n\nStart {1} anyway? This DEV-only bypass permits overlapping encounters for testing. The selected circle must still be idle, powered, and unobstructed. Do not use this to validate normal player progression.",
                    encounterBlocker ?? "Active encounter detected.",
                    GetMenuLabel(entry));

                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(confirmation, delegate
                {
                    ForceStartInternal(circle, entry, true);
                }));
                return;
            }

            ForceStartInternal(circle, entry, false);
        }

        private static void ForceStartInternal(Building_AbyssalSummoningCircle circle, RitualEntry entry, bool allowConcurrentEncounter)
        {
            if (circle == null || entry?.Props == null)
            {
                return;
            }

            if (circle.TryStartDevSummonSequence(entry.Props, allowConcurrentEncounter, out string failReason))
            {
                string message = allowConcurrentEncounter
                    ? AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_DevRehearsal_ConcurrentStarted",
                        "DEV concurrent force-started ritual without consuming a sigil: {0}",
                        GetMenuLabel(entry))
                    : "DEV force-started ritual without consuming a sigil: " + GetMenuLabel(entry);

                Messages.Message(message, MessageTypeDefOf.PositiveEvent, false);
                LogRehearsal(circle, entry);
                return;
            }

            Messages.Message("DEV force-start failed: " + (failReason ?? "unknown failure"), MessageTypeDefOf.RejectInput, false);
        }

        private static void AppendRehearsal(StringBuilder sb, Building_AbyssalSummoningCircle circle, RitualEntry entry, bool compact)
        {
            CompProperties_UseEffectSummonBoss props = entry?.Props;
            if (props == null)
            {
                return;
            }

            Map map = circle?.Map;
            string ritualId = props.ritualId ?? string.Empty;
            string summonMode = props.summonMode ?? "Boss";
            string resolvedPawnKindDefName = AbyssalArchonVariantUtility.ResolvePawnKindDefName(props);
            PawnKindDef pawnKindDef = resolvedPawnKindDefName.NullOrEmpty() ? null : DefDatabase<PawnKindDef>.GetNamedSilentFail(resolvedPawnKindDefName);
            string resolvedBossLabel = AbyssalArchonVariantUtility.ResolveBossLabel(props);

            if (!compact)
            {
                sb.AppendLine("[Abyssal Protocol] DEV THREAT REHEARSAL");
                sb.AppendLine("Map: " + GetMapLabel(map));
                sb.AppendLine("Circle: " + circle.Position + " | powered=" + circle.IsPoweredForRitual + " | ritualActive=" + circle.RitualActive);
                sb.AppendLine("Colonists: " + AbyssalT1SummonScalingUtility.GetActiveColonistCount(map) + " | wealth=" + Mathf.RoundToInt(map?.wealthWatcher?.WealthTotal ?? 0f));
                sb.AppendLine();
            }

            sb.AppendLine("== " + GetMenuLabel(entry) + " ==");
            sb.AppendLine("RitualId: " + ritualId + " | mode: " + summonMode + " | sigil: " + (entry.Def?.defName ?? "missing"));
            sb.AppendLine("Boss/pack label: " + (resolvedBossLabel ?? props.bossLabel ?? "none"));
            sb.AppendLine("PawnKind: " + (pawnKindDef?.defName ?? resolvedPawnKindDefName ?? "none") + " | spawnPoints XML: " + props.spawnPoints);
            sb.AppendLine("Retired: " + IsRetired(props));

            bool unlocked = AbyssalSummoningConsoleUtility.IsRitualUnlocked(ritualId, out string unlockFailReason);
            sb.AppendLine("Unlock gate: " + (unlocked ? "PASS" : "LOCKED — " + (unlockFailReason ?? "no reason")));
            if (circle != null)
            {
                bool ready = circle.IsReadyForSigil(out string readinessFailReason);
                sb.AppendLine("Circle readiness: " + (ready ? "PASS" : "BLOCKED — " + (readinessFailReason ?? "no reason")));
                AppendCapacitorReport(sb, circle, props);
            }

            AppendArrivalReport(sb, circle, props, pawnKindDef);
            AppendThreatPlanReport(sb, map, props, pawnKindDef);
            AppendPresentationReport(sb, props);
        }

        private static void AppendCapacitorReport(StringBuilder sb, Building_AbyssalSummoningCircle circle, CompProperties_UseEffectSummonBoss props)
        {
            if (circle == null || props == null)
            {
                return;
            }

            AbyssalCircleCapacitorRitualUtility.CapacitorReadinessReport report = AbyssalCircleCapacitorRitualUtility.CreateReadinessReport(circle, props);
            if (report == null || report.Profile == null)
            {
                sb.AppendLine("Capacitors: no ritual profile required.");
                return;
            }

            string supportState = AbyssalCircleCapacitorRitualUtility.GetSupportStateLabel(report);
            bool forceStart = circle.CapacitorOverchannelEnabled && AbyssalCircleCapacitorRitualUtility.CanForceStart(report);
            sb.AppendLine("Capacitors: " + supportState
                + " | charge " + report.AvailableCharge.ToString("0.0") + "/" + report.EffectiveTotalRequired.ToString("0.0")
                + " | throughput " + report.Throughput.ToString("0.0") + "/" + report.EffectiveThroughputRequired.ToString("0.0")
                + " | overchannel=" + forceStart);
        }

        private static void AppendArrivalReport(StringBuilder sb, Building_AbyssalSummoningCircle circle, CompProperties_UseEffectSummonBoss props, PawnKindDef pawnKindDef)
        {
            Map map = circle?.Map;
            IntVec3 focus = circle?.RitualFocusCell ?? IntVec3.Invalid;
            string summonMode = props.summonMode ?? "Boss";
            string ritualId = props.ritualId ?? string.Empty;

            if (map == null)
            {
                sb.AppendLine("Arrival: no map available.");
                return;
            }

            if (string.Equals(summonMode, "ImpPortal", StringComparison.OrdinalIgnoreCase))
            {
                bool found = ABY_Phase2PortalUtility.TryFindPortalSpawnCellNear(map, focus, 5.9f, 14.9f, out IntVec3 portalCell);
                sb.AppendLine("Arrival: imp portal near focus | predicted portal cell: " + CellOrNone(portalCell, found));
                return;
            }

            if (string.Equals(summonMode, "PortalWave", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("Arrival: portal wave component | first cells chosen by MapComponent_AbyssalPortalWave at runtime; rehearsal logs horde plan below.");
                return;
            }

            if (string.Equals(summonMode, "DominionCrisis", StringComparison.OrdinalIgnoreCase))
            {
                MapComponent_DominionCrisis crisis = map.GetComponent<MapComponent_DominionCrisis>();
                string failReason = null;
                bool canBegin = crisis != null && crisis.CanBegin(circle, out failReason);
                sb.AppendLine("Arrival: Dominion runtime | CanBegin=" + canBegin + (canBegin ? string.Empty : " — " + (failReason ?? "no reason")));
                return;
            }

            IntVec3 arrivalCell = IntVec3.Invalid;
            bool foundArrival;
            if (props.arrivalNearColony)
            {
                foundArrival = AbyssalBossSummonUtility.IsReactorSaintKindDefName(pawnKindDef?.defName)
                    ? AbyssalBossSummonUtility.TryFindReactorSaintArrivalCell(map, focus.IsValid ? focus : circle.Position, props.arrivalNearColonyMinDistance, props.arrivalNearColonyMaxDistance, out arrivalCell)
                    : AbyssalBossSummonUtility.TryFindNearColonyArrivalCell(map, focus.IsValid ? focus : circle.Position, props.arrivalNearColonyMinDistance, props.arrivalNearColonyMaxDistance, out arrivalCell);
            }
            else
            {
                foundArrival = AbyssalBossSummonUtility.TryFindBossArrivalCell(map, out arrivalCell);
            }

            if (string.Equals(summonMode, "HostilePack", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("Arrival: hostile pack | predicted pack cell: " + CellOrNone(arrivalCell, foundArrival));
            }
            else if (!props.arrivalManifestationDefName.NullOrEmpty())
            {
                sb.AppendLine("Arrival: boss manifestation " + props.arrivalManifestationDefName + " | requested cell: " + CellOrNone(arrivalCell, foundArrival) + " | warmup=" + props.arrivalManifestationWarmupTicks);
            }
            else
            {
                sb.AppendLine("Arrival: direct boss spawn | predicted boss cell: " + CellOrNone(arrivalCell, foundArrival));
                if (AbyssalBossOrchestrationUtility.HasBossEscortProfile(ritualId))
                {
                    sb.AppendLine("Escort anchor: boss occupied-rect center after spawn; close-anchor fallback enabled.");
                }
            }
        }

        private static void AppendThreatPlanReport(StringBuilder sb, Map map, CompProperties_UseEffectSummonBoss props, PawnKindDef pawnKindDef)
        {
            if (props == null)
            {
                return;
            }

            string ritualId = props.ritualId ?? string.Empty;
            string summonMode = props.summonMode ?? "Boss";

            if (AbyssalT1SummonScalingUtility.IsSupportedRitual(ritualId))
            {
                AbyssalT1SummonScalingUtility.ThreatPlan t1Plan = AbyssalT1SummonScalingUtility.GetThreatPlan(map, ritualId);
                if (t1Plan != null)
                {
                    sb.AppendLine("T1/T2 scaling: tier=" + t1Plan.Tier + " | colonistTier=" + t1Plan.ColonistTier + " | wealthTier=" + t1Plan.WealthTier + " | budget=" + t1Plan.ThreatBudget);
                    sb.AppendLine("T1/T2 composition: " + SummarizeT1Plan(t1Plan));
                    if (t1Plan.DirectedPlan != null)
                    {
                        AppendDirectedPlan(sb, "Directed pool", t1Plan.DirectedPlan);
                    }
                }
            }

            if (AbyssalHordeSigilUtility.IsSupportedRitual(ritualId))
            {
                AbyssalHordeSigilUtility.HordePlan horde = AbyssalHordeSigilUtility.GetHordePlan(map);
                sb.AppendLine("Horde plan: band=" + horde.Band + " | fronts=" + horde.FrontCount + " | phases=" + horde.PhaseCount + " | pulses=" + horde.PulseCount + " | portals=" + horde.TotalPortalRequests + " | units=" + horde.TotalUnits + " | budget=" + horde.TotalBudget.ToString("0"));
                sb.AppendLine("Horde doctrine: " + (horde.PrimaryDoctrineDefName ?? "none") + " | commandGate=" + horde.UsesCommandGate + " | forecast=" + (horde.ForecastText ?? "none"));
                sb.AppendLine("Horde total counts: " + SummarizeCounts(horde.TotalCounts));
                return;
            }

            if (string.Equals(summonMode, "DominionCrisis", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("Dominion plan: crisis runtime starts first; Anchorfall/Gatecore waves are selected by AbyssalDominionWaveUtility during phase ticks.");
                sb.AppendLine("Dominion pools expected: dominion_wave + dominion_gate_support, with anchor count and replay tier applied at runtime.");
                return;
            }

            ABY_BossDifficultyProfileDef profile = AbyssalBossOrchestrationUtility.ResolveProfileByRitualId(ritualId);
            if (profile == null && pawnKindDef != null)
            {
                profile = AbyssalBossOrchestrationUtility.ResolveProfileByBossKindDefName(pawnKindDef.defName);
            }

            if (profile != null)
            {
                float fallbackBudget = profile.fallbackEscortBudget > 0f ? profile.fallbackEscortBudget : EstimateFallbackBudget(ritualId);
                AbyssalEncounterDirectorUtility.EncounterPlan escortPlan = AbyssalBossOrchestrationUtility.BuildEscortPlan(ritualId, map, fallbackBudget);
                sb.AppendLine("Boss escort profile: " + profile.defName + " | pool=" + profile.escortPoolId + " | baseTier=" + profile.escortBaseContentTier + " | fallbackBudget=" + profile.fallbackEscortBudget.ToString("0"));
                sb.AppendLine("Boss escort doctrines: preferred=" + JoinOrNone(profile.preferredDoctrineDefNames) + " | secondary=" + JoinOrNone(profile.secondaryDoctrineDefNames));
                AppendDirectedPlan(sb, "Boss escort plan", escortPlan);
            }
            else if (HasLegacySupport(props))
            {
                sb.AppendLine("Legacy support counts: imps=" + props.supportImpCount + " thralls=" + props.supportThrallCount + " zealots=" + props.supportZealotCount + " rare=" + props.rareEscortPawnKindDefName + " x" + props.rareEscortCount + " @" + props.rareEscortChance.ToString("0.##"));
            }
            else
            {
                sb.AppendLine("Support plan: none / direct boss only.");
            }
        }

        private static void AppendPresentationReport(StringBuilder sb, CompProperties_UseEffectSummonBoss props)
        {
            string ritualId = props?.ritualId ?? string.Empty;
            string presentation;
            switch (ritualId.ToLowerInvariant())
            {
                case "unstable_breach":
                    presentation = "unstable breach portal pulse + two offset rupture VFX";
                    break;
                case "ember_hunt":
                    presentation = "hunting-pack impulse + three flanking VFX marks";
                    break;
                case "warden_of_ash":
                    presentation = "ash impact impulse + diagonal scorch VFX";
                    break;
                case "choir_engine":
                    presentation = "choir relay pulse + four symmetric relay VFX";
                    break;
                case "rift_butcher":
                    presentation = "butcher seam tear + close triangular rupture VFX";
                    break;
                case "horde_gate":
                    presentation = "portal wave front logic; runtime component owns per-front VFX";
                    break;
                case "dominion_gate":
                    presentation = "Dominion crisis phase runtime; Anchorfall/Gatecore own presentation";
                    break;
                default:
                    presentation = props != null && !props.arrivalManifestationDefName.NullOrEmpty()
                        ? "boss manifestation def: " + props.arrivalManifestationDefName
                        : "generic ritual pulse";
                    break;
            }

            sb.AppendLine("Presentation route: " + presentation);
        }

        private static void AppendDirectedPlan(StringBuilder sb, string label, AbyssalEncounterDirectorUtility.EncounterPlan plan)
        {
            if (plan == null || plan.TotalUnits <= 0)
            {
                sb.AppendLine(label + ": no directed plan produced.");
                return;
            }

            sb.AppendLine(label + ": pool=" + (plan.PoolId ?? "none")
                + " | template=" + (plan.TemplateDefName ?? "none")
                + " | doctrine=" + (plan.DoctrineDefName ?? "none")
                + " | tier=" + plan.AllowedContentTier
                + " | budget=" + plan.Budget.ToString("0")
                + " | units=" + plan.TotalUnits);
            sb.AppendLine(label + " composition: " + plan.GetSummary());
        }

        private static string SummarizeT1Plan(AbyssalT1SummonScalingUtility.ThreatPlan plan)
        {
            if (plan == null)
            {
                return "none";
            }

            List<string> parts = new List<string>();
            AddCount(parts, "portal imps", plan.PortalImpCount);
            AddCount(parts, "pack imps", plan.PackImpCount);
            AddCount(parts, "hounds", plan.HoundCount);
            AddCount(parts, "thralls", plan.ThrallCount);
            AddCount(parts, "sappers", plan.SapperCount);
            AddCount(parts, "zealots", plan.ZealotCount);
            AddCount(parts, "priests", plan.PriestCount);
            AddCount(parts, "snipers", plan.SniperCount);
            return parts.Count > 0 ? string.Join(", ", parts) : "none";
        }

        private static string SummarizeCounts(Dictionary<string, int> counts)
        {
            if (counts == null || counts.Count == 0)
            {
                return "none";
            }

            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key))
            {
                if (pair.Value > 0)
                {
                    parts.Add(pair.Key + " x" + pair.Value);
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "none";
        }

        private static void AddCount(List<string> parts, string label, int count)
        {
            if (parts != null && count > 0)
            {
                parts.Add(label + " x" + count);
            }
        }

        private static bool HasLegacySupport(CompProperties_UseEffectSummonBoss props)
        {
            return props != null
                && (props.supportImpCount > 0
                    || props.supportThrallCount > 0
                    || props.supportZealotCount > 0
                    || (!props.rareEscortPawnKindDefName.NullOrEmpty() && props.rareEscortCount > 0));
        }

        private static float EstimateFallbackBudget(string ritualId)
        {
            switch ((ritualId ?? string.Empty).ToLowerInvariant())
            {
                case "warden_of_ash":
                    return 520f;
                case "archon_beast":
                    return 720f;
                case "archon_of_rupture":
                    return 850f;
                case "choir_engine":
                    return 760f;
                case "rift_butcher":
                    return 780f;
                case "reactor_saint":
                    return 1100f;
                default:
                    return 720f;
            }
        }

        private static string JoinOrNone(List<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "none";
            }

            List<string> cleaned = new List<string>();
            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i];
                if (!value.NullOrEmpty())
                {
                    cleaned.Add(value);
                }
            }

            return cleaned.Count > 0 ? string.Join(", ", cleaned) : "none";
        }

        private static string CellOrNone(IntVec3 cell, bool found)
        {
            return found && cell.IsValid ? cell.ToString() : "none";
        }

        private static string GetMapLabel(Map map)
        {
            if (map == null)
            {
                return "none";
            }

            return map.Parent?.LabelCap ?? ("map " + map.uniqueID);
        }

        private static string GetMenuLabel(RitualEntry entry)
        {
            if (entry?.Props == null)
            {
                return "missing ritual";
            }

            string label = GetRitualLabel(entry.Props.ritualId);
            if (!label.NullOrEmpty())
            {
                return label + " [" + entry.Props.ritualId + "]";
            }

            if (!entry.Props.bossLabel.NullOrEmpty())
            {
                return entry.Props.bossLabel + " [" + entry.Props.ritualId + "]";
            }

            return (entry.Def?.label ?? entry.Def?.defName ?? entry.Props.ritualId ?? "summon") + " [" + (entry.Props.ritualId ?? "unknown") + "]";
        }

        private static string GetRitualLabel(string ritualId)
        {
            string key;
            switch ((ritualId ?? string.Empty).ToLowerInvariant())
            {
                case "unstable_breach":
                    key = "ABY_CircleRitual_Unstable_Label";
                    break;
                case "ember_hunt":
                    key = "ABY_CircleRitual_EmberHound_Label";
                    break;
                case "warden_of_ash":
                    key = "ABY_CircleRitual_Warden_Label";
                    break;
                case "archon_beast":
                    key = "ABY_CircleRitual_Archon_Label";
                    break;
                case "choir_engine":
                    key = "ABY_CircleRitual_Choir_Label";
                    break;
                case "reactor_saint":
                    key = "ABY_CircleRitual_ReactorSaint_Label";
                    break;
                case "horde_gate":
                    key = "ABY_CircleRitual_Horde_Label";
                    break;
                case "rift_butcher":
                    key = "ABY_CircleRitual_RiftButcher_Label";
                    break;
                case "dominion_gate":
                    key = "ABY_CircleRitual_Dominion_Label";
                    break;
                default:
                    return null;
            }

            string translated = key.Translate();
            return translated == key ? ritualId : translated;
        }

        private static bool IsRetired(CompProperties_UseEffectSummonBoss props)
        {
            return props != null && string.Equals(props.ritualId, "hexgun_thralls", StringComparison.OrdinalIgnoreCase);
        }

        private static List<RitualEntry> GetRitualEntries()
        {
            Dictionary<string, RitualEntry> byRitual = new Dictionary<string, RitualEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                CompProperties_UseEffectSummonBoss props = def?.GetCompProperties<CompProperties_UseEffectSummonBoss>();
                if (props == null || props.ritualId.NullOrEmpty())
                {
                    continue;
                }

                if (!byRitual.ContainsKey(props.ritualId))
                {
                    byRitual.Add(props.ritualId, new RitualEntry(def, props));
                }
            }

            List<RitualEntry> entries = new List<RitualEntry>();
            for (int i = 0; i < RitualOrder.Length; i++)
            {
                string ritualId = RitualOrder[i];
                if (byRitual.TryGetValue(ritualId, out RitualEntry entry))
                {
                    entries.Add(entry);
                    byRitual.Remove(ritualId);
                }
            }

            foreach (RitualEntry entry in byRitual.Values.OrderBy(e => e.Props?.ritualId ?? string.Empty))
            {
                entries.Add(entry);
            }

            return entries;
        }

        private sealed class RitualEntry
        {
            public readonly ThingDef Def;
            public readonly CompProperties_UseEffectSummonBoss Props;

            public RitualEntry(ThingDef def, CompProperties_UseEffectSummonBoss props)
            {
                Def = def;
                Props = props;
            }
        }
    }
}
