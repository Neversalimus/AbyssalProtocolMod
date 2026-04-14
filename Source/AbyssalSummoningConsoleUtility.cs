using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class AbyssalSummoningConsoleUtility
    {
        public sealed class RitualDefinition
        {
            public string Id;
            public string LabelKey;
            public string SubtitleKey;
            public string DescriptionKey;
            public string BossLabel;
            public string SigilThingDefName;
            public string PawnKindDefName;
            public string RewardHintKey;
            public string SideEffectHintKey;
            public float BaseRisk;
            public float InstabilityGain;
            public float ContaminationGain;
            public int SpawnPoints;
        }

        public sealed class StatusEntry
        {
            public string Label;
            public string Value;
            public bool Satisfied;
        }

        public enum CircleRiskTier
        {
            Stable,
            Strained,
            Volatile,
            Catastrophic
        }

        private static readonly List<RitualDefinition> Rituals = new List<RitualDefinition>
        {
            new RitualDefinition
            {
                Id = "unstable_breach",
                LabelKey = "ABY_CircleRitual_Unstable_Label",
                SubtitleKey = "ABY_CircleRitual_Unstable_Subtitle",
                DescriptionKey = "ABY_CircleRitual_Unstable_Desc",
                BossLabel = "Rift imp breach",
                SigilThingDefName = "ABY_UnstableBreachSigil",
                PawnKindDefName = "ABY_RiftImp",
                RewardHintKey = "ABY_CircleRitual_Unstable_Rewards",
                SideEffectHintKey = "ABY_CircleRitual_Unstable_SideEffects",
                BaseRisk = 0.38f,
                InstabilityGain = 0.16f,
                ContaminationGain = 0.08f,
                SpawnPoints = 180
            },
            new RitualDefinition
            {
                Id = "archon_beast",
                LabelKey = "ABY_CircleRitual_Archon_Label",
                SubtitleKey = "ABY_CircleRitual_Archon_Subtitle",
                DescriptionKey = "ABY_CircleRitual_Archon_Desc",
                BossLabel = "Archon Beast",
                SigilThingDefName = "ABY_ArchonSigil",
                PawnKindDefName = "ABY_ArchonBeast",
                RewardHintKey = "ABY_CircleRitual_Archon_Rewards",
                SideEffectHintKey = "ABY_CircleRitual_Archon_SideEffects",
                BaseRisk = 0.68f,
                InstabilityGain = 0.30f,
                ContaminationGain = 0.16f,
                SpawnPoints = 900
            }
        };

        public static IEnumerable<RitualDefinition> GetRituals()
        {
            return Rituals;
        }

        public static RitualDefinition GetRitualById(string ritualId)
        {
            if (ritualId.NullOrEmpty())
            {
                return GetDefaultRitual();
            }

            RitualDefinition ritual = Rituals.FirstOrDefault(r => r.Id == ritualId);
            return ritual ?? GetDefaultRitual();
        }

        public static RitualDefinition GetDefaultRitual()
        {
            return Rituals[0];
        }

        public static string TranslateOrFallback(string key, string fallback)
        {
            string value = key.Translate();
            return value == key ? fallback : value;
        }

        public static string TranslateOrFallback(string key, string fallbackFormat, params object[] args)
        {
            string template = key.Translate();
            if (template == key)
            {
                return string.Format(fallbackFormat, args);
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        public static string GetConsoleTitle()
        {
            return TranslateOrFallback("ABY_CircleConsoleTitle", "abyssal summoning console");
        }

        public static string GetConsoleSubtitle()
        {
            return TranslateOrFallback("ABY_CircleConsoleSubtitle", "Threat-calibrated breach routing. Arm a prepared sigil, validate the circle, then let a colonist perform the invocation sequence.");
        }

        public static string GetConsoleSubtitleActive(string phaseLabel)
        {
            return TranslateOrFallback("ABY_CircleConsoleSubtitleActive", "Ritual active. Current phase: {0}.", phaseLabel);
        }

        public static string GetCompactSubtitle()
        {
            return TranslateOrFallback("ABY_CircleTab_SubtitleShort", "Compact ritual status and console access.");
        }

        public static string GetCompactHint()
        {
            return TranslateOrFallback("ABY_CircleTab_Hint", "Use the main console for full ritual preview, readiness checks, and automated sigil assignment.");
        }

        public static string GetCompactFooter()
        {
            return TranslateOrFallback("ABY_CircleTab_FooterShort", "Use the full console for ritual preview and sigil assignment.");
        }

        public static string GetOpenConsoleLabel()
        {
            return TranslateOrFallback("ABY_CircleCommand_OpenConsole", "Open summoning console");
        }

        public static string GetOpenConsoleDesc()
        {
            return TranslateOrFallback("ABY_CircleCommand_OpenConsoleDesc", "Open the full command console for ritual readiness, threat preview, and sigil assignment.");
        }

        public static string GetAssignSigilLabel()
        {
            return TranslateOrFallback("ABY_CircleCommand_AssignSigil", "Assign sigil and begin invocation");
        }

        public static string GetJumpToSigilLabel()
        {
            return TranslateOrFallback("ABY_CircleCommand_JumpToSigil", "Jump to nearest sigil");
        }

        public static string GetReducedEffectsLabel(bool reduced)
        {
            return reduced
                ? TranslateOrFallback("ABY_CircleReducedEffectsOn", "Effects: reduced")
                : TranslateOrFallback("ABY_CircleReducedEffectsOff", "Effects: full");
        }

        public static string GetInspectSigilsText(int count)
        {
            return TranslateOrFallback("ABY_CircleInspect_Sigils", "Sigils on map: {0}", count);
        }

        public static string GetInspectReadinessText(string readiness)
        {
            return TranslateOrFallback("ABY_CircleInspect_Readiness", "Readiness: {0}", readiness);
        }

        public static string GetInspectRiskText(string risk)
        {
            return TranslateOrFallback("ABY_CircleInspect_Risk", "Risk: {0}", risk);
        }

        public static string GetInspectHeatText(string heat)
        {
            return TranslateOrFallback("ABY_CircleInspect_Heat", "Heat: {0}", heat);
        }

        public static string GetInspectContainmentText(string containment)
        {
            return TranslateOrFallback("ABY_CircleInspect_Containment", "Containment: {0}", containment);
        }

        public static string GetInspectContaminationText(string contamination)
        {
            return TranslateOrFallback("ABY_CircleInspect_Contamination", "Contamination: {0}", contamination);
        }

        public static string GetPhaseText(string phaseLabel, int progressPercent)
        {
            return TranslateOrFallback("ABY_CircleInspect_Phase", "Phase: {0} ({1}%)", phaseLabel, progressPercent);
        }

        public static string GetReadyText()
        {
            return TranslateOrFallback("ABY_CircleInspect_Ready", "Ready for activation.");
        }

        public static string GetNotReadyText(string failReason)
        {
            return TranslateOrFallback("ABY_CircleInspect_NotReady", "Not ready: {0}", failReason);
        }

        public static string GetCompactStatusLine(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CircleConsoleFail_NoCircle", "No valid summoning circle is available for console control.");
            }

            if (circle.RitualActive)
            {
                return GetPhaseText(circle.GetCurrentPhaseTranslated(), Mathf.RoundToInt(circle.RitualProgress * 100f));
            }

            if (circle.IsReadyForSigil(out string failReason))
            {
                return GetReadyText();
            }

            return GetNotReadyText(Shorten(failReason, 72));
        }

        public static string GetRitualLabel(RitualDefinition ritual)
        {
            if (ritual == null)
            {
                return "Unknown ritual";
            }

            if (ritual.Id == "unstable_breach")
            {
                return TranslateOrFallback(ritual.LabelKey, "Open unstable breach");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.LabelKey, "Invoke Archon Beast");
            }

            return TranslateOrFallback(ritual.LabelKey, ritual.Id);
        }

        public static string GetRitualSubtitle(RitualDefinition ritual)
        {
            if (ritual == null)
            {
                return string.Empty;
            }

            if (ritual.Id == "unstable_breach")
            {
                return TranslateOrFallback(ritual.SubtitleKey, "Pre-boss hostile breach pattern");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.SubtitleKey, "First boss breach pattern");
            }

            return TranslateOrFallback(ritual.SubtitleKey, ritual.Id);
        }

        public static string GetRitualDescription(RitualDefinition ritual)
        {
            if (ritual == null)
            {
                return string.Empty;
            }

            if (ritual.Id == "unstable_breach")
            {
                return TranslateOrFallback(ritual.DescriptionKey, "Consumes one unstable breach sigil, routes a colonist to the circle, and tears open a smaller hostile rift that disgorges early abyssal attackers before the first boss tier.");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.DescriptionKey, "Consumes one prepared archon sigil, routes a colonist to the circle, charges the breach, and calls the first hostile techno-demonic boss encounter.");
            }

            return TranslateOrFallback(ritual.DescriptionKey, ritual.Id);
        }

        public static string GetRitualRewardHint(RitualDefinition ritual)
        {
            if (ritual == null)
            {
                return string.Empty;
            }

            if (ritual.Id == "unstable_breach")
            {
                return TranslateOrFallback(ritual.RewardHintKey, @"• Early hostile summoning rehearsal
• Smaller breach pressure before the first boss
• A cleaner bridge between the circle and the Archon Beast fight");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.RewardHintKey, @"• First-loop boss progression
• Archon-linked drops and unlock gating
• Early abyssal loot and boss-side material gating");
            }

            return TranslateOrFallback(ritual.RewardHintKey, ritual.Id);
        }

        public static string GetRitualSideEffectHint(RitualDefinition ritual)
        {
            if (ritual == null)
            {
                return string.Empty;
            }

            if (ritual.Id == "unstable_breach")
            {
                return TranslateOrFallback(ritual.SideEffectHintKey, @"• Spawns a hostile rift imp breach tied to the circle
• Still counts as an active abyssal encounter while the breach is live
• Lower threat than the Archon Beast, but meant to pressure unprepared colonies");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.SideEffectHintKey, @"• Opens a hostile breach on the current map
• Can trigger escalation pressure if the colony is underprepared
• Ritual phases intensify visuals, sound, and encounter pressure");
            }

            return TranslateOrFallback(ritual.SideEffectHintKey, ritual.Id);
        }

        private static string Shorten(string text, int maxLength)
        {
            if (text.NullOrEmpty() || text.Length <= maxLength)
            {
                return text ?? string.Empty;
            }

            return text.Substring(0, Mathf.Max(0, maxLength - 1)).TrimEnd() + "…";
        }

        public static ThingDef GetSigilDef(RitualDefinition ritual)
        {
            return ritual == null ? null : DefDatabase<ThingDef>.GetNamedSilentFail(ritual.SigilThingDefName);
        }

        public static int CountSigilsOnMap(Map map, RitualDefinition ritual)
        {
            ThingDef sigilDef = GetSigilDef(ritual);
            if (map == null || sigilDef == null)
            {
                return 0;
            }

            int count = 0;
            List<Thing> things = map.listerThings.ThingsOfDef(sigilDef);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                count += Math.Max(1, thing.stackCount);
            }

            return count;
        }

        public static int CountAvailableOperators(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            if (circle == null || circle.Map == null)
            {
                return 0;
            }

            int count = 0;
            List<Pawn> pawns = circle.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (CanUseCircle(pawn, circle, ritual, false))
                {
                    count++;
                }
            }

            return count;
        }

        public static bool TryAssignInvocation(Building_AbyssalSummoningCircle circle, RitualDefinition ritual, out string failReason)
        {
            failReason = null;

            if (circle == null || circle.Destroyed || !circle.Spawned || circle.Map == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoCircle", "No valid summoning circle is available for console control.");
                return false;
            }

            if (!circle.IsReadyForSigil(out failReason))
            {
                return false;
            }

            Thing sigil = FindBestSigil(circle, ritual, out failReason);
            if (sigil == null)
            {
                return false;
            }

            Pawn pawn = FindBestOperator(circle, ritual, sigil, out failReason);
            if (pawn == null)
            {
                return false;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ABY_CarrySigilToCircle");
            if (jobDef == null)
            {
                failReason = "Missing JobDef: ABY_CarrySigilToCircle";
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, sigil, circle);
            job.count = 1;
            pawn.jobs.TryTakeOrderedJob(job);
            return true;
        }

        public static bool TryAssignModuleInstall(Building_AbyssalSummoningCircle circle, Thing moduleThing, AbyssalCircleModuleEdge edge, out string failReason)
        {
            failReason = null;
            if (circle == null || moduleThing == null)
            {
                failReason = TranslateOrFallback("ABY_CircleModuleFail_NoModuleThing", "No valid stabilizer module is available for installation.");
                return false;
            }

            if (!circle.CanInstallModule(moduleThing.def, edge, out failReason))
            {
                return false;
            }

            Pawn pawn = FindBestModuleOperator(circle, moduleThing, out failReason);
            if (pawn == null)
            {
                return false;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ABY_InstallCircleModule");
            if (jobDef == null)
            {
                failReason = "Missing JobDef: ABY_InstallCircleModule";
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, moduleThing, circle);
            job.count = 1;
            job.targetC = new LocalTargetInfo(new IntVec3((int)edge, 0, 0));
            pawn.jobs.TryTakeOrderedJob(job);
            return true;
        }

        public static bool TryAssignModuleRemove(Building_AbyssalSummoningCircle circle, AbyssalCircleModuleEdge edge, out string failReason)
        {
            failReason = null;
            if (circle == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoCircle", "No valid summoning circle is available for console control.");
                return false;
            }

            if (!circle.CanRemoveInstalledModule(edge, out failReason))
            {
                return false;
            }

            Pawn pawn = FindBestModuleOperator(circle, null, out failReason);
            if (pawn == null)
            {
                return false;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ABY_RemoveCircleModule");
            if (jobDef == null)
            {
                failReason = "Missing JobDef: ABY_RemoveCircleModule";
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, circle);
            job.targetC = new LocalTargetInfo(new IntVec3((int)edge, 0, 0));
            pawn.jobs.TryTakeOrderedJob(job);
            return true;
        }

        public static Thing FindBestSigil(Building_AbyssalSummoningCircle circle, RitualDefinition ritual, out string failReason)
        {
            failReason = null;
            if (circle?.Map == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoCircle", "No valid summoning circle is available for console control.");
                return null;
            }

            ThingDef sigilDef = GetSigilDef(ritual);
            if (sigilDef == null)
            {
                failReason = "Missing ThingDef: " + ritual?.SigilThingDefName;
                return null;
            }

            Thing best = null;
            float bestScore = float.MaxValue;
            List<Thing> things = circle.Map.listerThings.ThingsOfDef(sigilDef);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.MapHeld != circle.Map)
                {
                    continue;
                }

                float score = thing.PositionHeld.DistanceToSquared(circle.PositionHeld);
                if (score < bestScore)
                {
                    best = thing;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                string sigilLabel = sigilDef.label ?? ritual?.SigilThingDefName ?? TranslateOrFallback("ABY_CircleConsoleFail_NoSigilFallback", "prepared sigil");
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoSpecificSigil", "No prepared {0} was found on the current map.", sigilLabel);
            }

            return best;
        }

        public static Pawn FindBestOperator(Building_AbyssalSummoningCircle circle, RitualDefinition ritual, Thing sigil, out string failReason)
        {
            failReason = null;
            if (circle?.Map == null || sigil == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoOperator", "No suitable colonist is currently available to carry a sigil to the circle.");
                return null;
            }

            Pawn best = null;
            float bestScore = float.MaxValue;
            List<Pawn> pawns = circle.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!CanUseCircle(pawn, circle, ritual, true))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(sigil, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(circle, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    continue;
                }

                float score = pawn.PositionHeld.DistanceToSquared(sigil.PositionHeld);
                if (pawn.Drafted)
                {
                    score += 4000f;
                }
                if (score < bestScore)
                {
                    best = pawn;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoOperator", "No suitable colonist is currently available to carry a sigil to the circle.");
            }

            return best;
        }

        public static Pawn FindBestModuleOperator(Building_AbyssalSummoningCircle circle, Thing moduleThing, out string failReason)
        {
            failReason = null;
            if (circle?.Map == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoOperator", "No suitable colonist is currently available to carry a sigil to the circle.");
                return null;
            }

            Pawn best = null;
            float bestScore = float.MaxValue;
            List<Pawn> pawns = circle.Map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.MapHeld != circle.Map || pawn.Dead || pawn.Downed || pawn.InMentalState)
                {
                    continue;
                }

                if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    continue;
                }

                if (moduleThing != null && !pawn.CanReserveAndReach(moduleThing, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(circle, PathEndMode.InteractionCell, Danger.Deadly))
                {
                    continue;
                }

                float score = pawn.PositionHeld.DistanceToSquared(circle.PositionHeld) * 0.25f;
                if (moduleThing != null)
                {
                    score += pawn.PositionHeld.DistanceToSquared(moduleThing.PositionHeld);
                }
                if (pawn.Drafted)
                {
                    score += 4000f;
                }
                if (score < bestScore)
                {
                    best = pawn;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoOperator", "No suitable colonist is currently available to carry a sigil to the circle.");
            }

            return best;
        }

        public static bool CanUseCircle(Pawn pawn, Building_AbyssalSummoningCircle circle, RitualDefinition ritual, bool requireReservations)
        {
            if (pawn == null || circle == null || pawn.MapHeld != circle.Map || pawn.Dead || pawn.Downed || pawn.InMentalState)
            {
                return false;
            }

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return false;
            }

            if (!circle.IsReadyForSigil(out _))
            {
                return false;
            }

            if (!pawn.CanReach(circle, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return false;
            }

            if (requireReservations && !pawn.CanReserve(circle))
            {
                return false;
            }

            return true;
        }

        public static List<StatusEntry> GetStatusEntries(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            List<StatusEntry> entries = new List<StatusEntry>();
            if (circle == null)
            {
                return entries;
            }

            bool interactionOk = circle.HasValidInteractionCell(out string interactionFail);
            bool focusOk = circle.HasClearRitualFocus(out string focusFail);
            bool encounterClear = circle.Map != null && !AbyssalBossSummonUtility.HasActiveAbyssalEncounter(circle.Map);
            int sigils = CountSigilsOnMap(circle.Map, ritual);
            int operators = CountAvailableOperators(circle, ritual);

            entries.Add(new StatusEntry
            {
                Label = TranslateOrFallback("ABY_CircleStatus_Power", "Power"),
                Value = circle.IsPoweredForRitual ? TranslateOrFallback("ABY_CircleStatus_Online", "online") : TranslateOrFallback("ABY_CircleStatus_Offline", "offline"),
                Satisfied = circle.IsPoweredForRitual
            });
            entries.Add(new StatusEntry
            {
                Label = TranslateOrFallback("ABY_CircleStatus_Focus", "Focus"),
                Value = focusOk ? TranslateOrFallback("ABY_CircleStatus_Clear", "clear") : focusFail,
                Satisfied = focusOk
            });
            entries.Add(new StatusEntry
            {
                Label = TranslateOrFallback("ABY_CircleStatus_Interaction", "Access"),
                Value = interactionOk ? TranslateOrFallback("ABY_CircleStatus_Clear", "clear") : interactionFail,
                Satisfied = interactionOk
            });
            entries.Add(new StatusEntry
            {
                Label = TranslateOrFallback("ABY_CircleStatus_Sigils", "Sigils"),
                Value = sigils.ToString(),
                Satisfied = sigils > 0
            });
            entries.Add(new StatusEntry
            {
                Label = TranslateOrFallback("ABY_CircleStatus_Operators", "Operators"),
                Value = operators.ToString(),
                Satisfied = operators > 0
            });
            entries.Add(new StatusEntry
            {
                Label = TranslateOrFallback("ABY_CircleStatus_Encounter", "Encounter"),
                Value = encounterClear ? TranslateOrFallback("ABY_CircleStatus_Clear", "clear") : TranslateOrFallback("ABY_BossSummonFail_EncounterActive", "An abyssal encounter is already active on this map."),
                Satisfied = encounterClear
            });

            return entries;
        }

        public static CircleRiskTier GetRiskTier(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            float risk = GetRiskValue(circle, ritual);
            if (risk <= 0.35f)
            {
                return CircleRiskTier.Stable;
            }

            if (risk <= 0.60f)
            {
                return CircleRiskTier.Strained;
            }

            if (risk <= 0.80f)
            {
                return CircleRiskTier.Volatile;
            }

            return CircleRiskTier.Catastrophic;
        }

        public static float GetRiskFill(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            return GetRiskValue(circle, ritual);
        }

        private static float GetRiskValue(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            return AbyssalCircleInstabilityUtility.GetRiskValue(circle, ritual);
        }

        public static Color GetRiskColor(CircleRiskTier tier)
        {
            switch (tier)
            {
                case CircleRiskTier.Stable:
                    return new Color(0.86f, 0.40f, 0.18f, 1f);
                case CircleRiskTier.Strained:
                    return new Color(0.96f, 0.52f, 0.16f, 1f);
                case CircleRiskTier.Volatile:
                    return new Color(1f, 0.38f, 0.16f, 1f);
                case CircleRiskTier.Catastrophic:
                    return new Color(1f, 0.22f, 0.14f, 1f);
                default:
                    return Color.white;
            }
        }

        public static string GetRiskLabel(CircleRiskTier tier)
        {
            switch (tier)
            {
                case CircleRiskTier.Stable:
                    return TranslateOrFallback("ABY_CircleRisk_Stable", "stable");
                case CircleRiskTier.Strained:
                    return TranslateOrFallback("ABY_CircleRisk_Strained", "strained");
                case CircleRiskTier.Volatile:
                    return TranslateOrFallback("ABY_CircleRisk_Volatile", "volatile");
                case CircleRiskTier.Catastrophic:
                    return TranslateOrFallback("ABY_CircleRisk_Catastrophic", "catastrophic");
                default:
                    return TranslateOrFallback("ABY_CircleRisk_Stable", "stable");
            }
        }

        public static string GetHeatDisplay(Building_AbyssalSummoningCircle circle)
        {
            int percent = Mathf.RoundToInt((circle?.InstabilityHeat ?? 0f) * 100f);
            return TranslateOrFallback("ABY_CircleTelemetry_HeatValue", "{0}%", percent);
        }

        public static string GetContainmentDisplay(Building_AbyssalSummoningCircle circle)
        {
            int percent = Mathf.RoundToInt((circle?.ContainmentRating ?? 0f) * 100f);
            return TranslateOrFallback("ABY_CircleTelemetry_ContainmentValue", "{0}%", percent);
        }

        public static string GetContaminationDisplay(Building_AbyssalSummoningCircle circle)
        {
            int percent = Mathf.RoundToInt((circle?.ResidualContamination ?? 0f) * 100f);
            return TranslateOrFallback("ABY_CircleTelemetry_ContaminationValue", "{0}%", percent);
        }

        public static string GetProjectedDeltaDisplay(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            int percent = Mathf.RoundToInt(AbyssalCircleInstabilityUtility.GetProjectedHeatGain(circle, ritual) * 100f);
            return TranslateOrFallback("ABY_CircleTelemetry_DeltaValue", "+{0}%", percent);
        }

        public static string GetProjectedContaminationDisplay(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            int percent = Mathf.RoundToInt(AbyssalCircleInstabilityUtility.GetProjectedContaminationGain(circle, ritual) * 100f);
            return TranslateOrFallback("ABY_CircleTelemetry_ContaminationDeltaValue", "+{0}%", percent);
        }

        public static string GetProjectedStateSummary(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            CircleRiskTier tier = GetRiskTier(circle, ritual);
            return TranslateOrFallback("ABY_CircleProjectedState", "Projected state: {0}", GetRiskLabel(tier));
        }

        public static string GetLikelyAnomalySummary(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            float projectedHeat = AbyssalCircleInstabilityUtility.GetProjectedPostInvokeHeat(circle, ritual);
            float contamination = circle?.ResidualContamination ?? 0f;
            string key;
            string fallback;
            if (projectedHeat + contamination * 0.40f >= 0.88f)
            {
                key = "ABY_CircleAnomaly_ImpSpill";
                fallback = "Likely anomaly: containment lash or imp spill";
            }
            else if (projectedHeat + contamination * 0.25f >= 0.58f)
            {
                key = "ABY_CircleAnomaly_Lash";
                fallback = "Likely anomaly: containment lash";
            }
            else
            {
                key = "ABY_CircleAnomaly_None";
                fallback = "Likely anomaly: none expected";
            }

            return TranslateOrFallback(key, fallback);
        }

        public static string GetProjectedInstabilityBlock(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            return TranslateOrFallback("ABY_CircleConsequencesProjected", "• Ritual heat load: {0}\n• Residue pressure after breach: {1}\n• {2}\n• {3}", GetProjectedDeltaDisplay(circle, ritual), GetProjectedContaminationDisplay(circle, ritual), GetProjectedStateSummary(circle, ritual), GetLikelyAnomalySummary(circle, ritual));
        }

        public static string FormatTicksShort(int ticks)
        {
            if (ticks <= 0)
            {
                return TranslateOrFallback("ABY_CircleCooldownReady", "ready");
            }

            int seconds = Mathf.CeilToInt(ticks / 60f);
            if (seconds < 60)
            {
                return TranslateOrFallback("ABY_CircleCooldownSeconds", "{0}s", seconds);
            }

            int minutes = seconds / 60;
            int remainder = seconds % 60;
            if (remainder <= 0)
            {
                return TranslateOrFallback("ABY_CircleCooldownMinutes", "{0}m", minutes);
            }

            return TranslateOrFallback("ABY_CircleCooldownMinutesSeconds", "{0}m {1}s", minutes, remainder);
        }

        public static string GetStabilizerPatternSummary(Building_AbyssalSummoningCircle circle)
        {
            AbyssalCircleStabilizerBonusSummary summary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            string key = AbyssalCircleModuleUtility.GetPatternKey(summary);
            string fallback;
            switch (key)
            {
                case "ABY_CircleStabilizerPattern_Symmetry":
                    fallback = "Pattern: symmetric ring";
                    break;
                case "ABY_CircleStabilizerPattern_FullRing":
                    fallback = "Pattern: full ring";
                    break;
                case "ABY_CircleStabilizerPattern_Paired":
                    fallback = "Pattern: paired lattice";
                    break;
                case "ABY_CircleStabilizerPattern_Partial":
                    fallback = "Pattern: partial lattice";
                    break;
                default:
                    fallback = "Pattern: open ring";
                    break;
            }

            return TranslateOrFallback(key, fallback);
        }

        public static string GetStabilizerContainmentBonusDisplay(Building_AbyssalSummoningCircle circle)
        {
            AbyssalCircleStabilizerBonusSummary summary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            int percent = Mathf.RoundToInt(summary.ContainmentBonus * 100f);
            return TranslateOrFallback("ABY_CircleStabilizerContainmentValue", "+{0}%", percent);
        }

        public static string GetStabilizerHeatDampingDisplay(Building_AbyssalSummoningCircle circle)
        {
            AbyssalCircleStabilizerBonusSummary summary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            int percent = Mathf.RoundToInt((1f - summary.HeatMultiplier) * 100f);
            return TranslateOrFallback("ABY_CircleStabilizerHeatDampingValue", "-{0}%", percent);
        }

        public static string GetStabilizerResidueSuppressionDisplay(Building_AbyssalSummoningCircle circle)
        {
            AbyssalCircleStabilizerBonusSummary summary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            int percent = Mathf.RoundToInt((1f - summary.ContaminationMultiplier) * 100f);
            return TranslateOrFallback("ABY_CircleStabilizerResidueValue", "-{0}%", percent);
        }

        public static string GetStabilizerAnomalyShieldingDisplay(Building_AbyssalSummoningCircle circle)
        {
            AbyssalCircleStabilizerBonusSummary summary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            int percent = Mathf.RoundToInt((1f - summary.EventChanceMultiplier) * 100f);
            return TranslateOrFallback("ABY_CircleStabilizerAnomalyValue", "-{0}%", percent);
        }

        public static string GetShortRequirementSummary(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            int ready = GetStatusEntries(circle, ritual).Count(entry => entry.Satisfied);
            return TranslateOrFallback("ABY_CircleReadinessSummary", "{0} / {1} gates clear", ready, 6);
        }
    }
}
