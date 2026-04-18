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
                InstabilityGain = 0.09f,
                ContaminationGain = 0.03f,
                SpawnPoints = 180
            },
            new RitualDefinition
            {
                Id = "ember_hunt",
                LabelKey = "ABY_CircleRitual_EmberHound_Label",
                SubtitleKey = "ABY_CircleRitual_EmberHound_Subtitle",
                DescriptionKey = "ABY_CircleRitual_EmberHound_Desc",
                BossLabel = "Ember Hound pack",
                SigilThingDefName = "ABY_EmberHoundSigil",
                PawnKindDefName = "ABY_EmberHound",
                RewardHintKey = "ABY_CircleRitual_EmberHound_Rewards",
                SideEffectHintKey = "ABY_CircleRitual_EmberHound_SideEffects",
                BaseRisk = 0.46f,
                InstabilityGain = 0.12f,
                ContaminationGain = 0.05f,
                SpawnPoints = 320
            },
            new RitualDefinition
            {
                Id = "warden_of_ash",
                LabelKey = "ABY_CircleRitual_Warden_Label",
                SubtitleKey = "ABY_CircleRitual_Warden_Subtitle",
                DescriptionKey = "ABY_CircleRitual_Warden_Desc",
                BossLabel = "Warden of Ash",
                SigilThingDefName = "ABY_WardenOfAshSigil",
                PawnKindDefName = "ABY_WardenOfAsh",
                RewardHintKey = "ABY_CircleRitual_Warden_Rewards",
                SideEffectHintKey = "ABY_CircleRitual_Warden_SideEffects",
                BaseRisk = 0.57f,
                InstabilityGain = 0.14f,
                ContaminationGain = 0.06f,
                SpawnPoints = 560
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
                InstabilityGain = 0.18f,
                ContaminationGain = 0.08f,
                SpawnPoints = 900
            },
            new RitualDefinition
            {
                Id = "choir_engine",
                LabelKey = "ABY_CircleRitual_Choir_Label",
                SubtitleKey = "ABY_CircleRitual_Choir_Subtitle",
                DescriptionKey = "ABY_CircleRitual_Choir_Desc",
                BossLabel = "Choir Engine",
                SigilThingDefName = "ABY_ChoirEngineSigil",
                PawnKindDefName = "ABY_ChoirEngine",
                RewardHintKey = "ABY_CircleRitual_Choir_Rewards",
                SideEffectHintKey = "ABY_CircleRitual_Choir_SideEffects",
                BaseRisk = 0.74f,
                InstabilityGain = 0.20f,
                ContaminationGain = 0.09f,
                SpawnPoints = 1040
            },
            new RitualDefinition
            {
                Id = "reactor_saint",
                LabelKey = "ABY_CircleRitual_ReactorSaint_Label",
                SubtitleKey = "ABY_CircleRitual_ReactorSaint_Subtitle",
                DescriptionKey = "ABY_CircleRitual_ReactorSaint_Desc",
                BossLabel = "Infernal Reactor Saint",
                SigilThingDefName = "ABY_ReactorSaintSigil",
                PawnKindDefName = "ABY_ReactorSaint",
                RewardHintKey = "ABY_CircleRitual_ReactorSaint_Rewards",
                SideEffectHintKey = "ABY_CircleRitual_ReactorSaint_SideEffects",
                BaseRisk = 0.79f,
                InstabilityGain = 0.21f,
                ContaminationGain = 0.09f,
                SpawnPoints = 1320
            },
            new RitualDefinition
            {
                Id = "dominion_gate",
                LabelKey = "ABY_CircleRitual_Dominion_Label",
                SubtitleKey = "ABY_CircleRitual_Dominion_Subtitle",
                DescriptionKey = "ABY_CircleRitual_Dominion_Desc",
                BossLabel = "Crowned Gate breach",
                SigilThingDefName = "ABY_DominionSigil",
                PawnKindDefName = string.Empty,
                RewardHintKey = "ABY_CircleRitual_Dominion_Rewards",
                SideEffectHintKey = "ABY_CircleRitual_Dominion_SideEffects",
                BaseRisk = 0.82f,
                InstabilityGain = 0.22f,
                ContaminationGain = 0.10f,
                SpawnPoints = 1600
            }
        };

        public static IEnumerable<RitualDefinition> GetRituals()
        {
            if (AbyssalDominionAccessUtility.IsUserFacingDominionContentEnabled())
            {
                return Rituals;
            }

            return Rituals.Where(ritual => ritual != null && !AbyssalDominionAccessUtility.IsDominionRitualId(ritual.Id));
        }

        public static RitualDefinition GetDefaultRitual()
        {
            return Rituals[0];
        }

        public static IEnumerable<RitualDefinition> GetRitualsForCircle(Building_AbyssalSummoningCircle circle)
        {
            bool exposeDominion = AbyssalDominionAccessUtility.ShouldExposeDominionRitual(circle);
            if (exposeDominion)
            {
                return Rituals;
            }

            return Rituals.Where(ritual => ritual != null && !AbyssalDominionAccessUtility.IsDominionRitualId(ritual.Id));
        }

        public static RitualDefinition GetSuggestedRitual(Building_AbyssalSummoningCircle circle)
        {
            MapComponent_DominionCrisis crisis = GetDominionCrisis(circle);
            if (crisis != null && crisis.IsActive)
            {
                RitualDefinition dominion = Rituals.FirstOrDefault(r => r.Id == "dominion_gate");
                if (dominion != null)
                {
                    return dominion;
                }
            }

            return GetDefaultRitual();
        }

        public static bool IsDominionRitual(RitualDefinition ritual)
        {
            return ritual != null && ritual.Id == "dominion_gate";
        }

        public static MapComponent_DominionCrisis GetDominionCrisis(Building_AbyssalSummoningCircle circle)
        {
            return circle?.Map?.GetComponent<MapComponent_DominionCrisis>();
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

        public static string GetConsoleSubtitleDominionActive(string phaseLabel)
        {
            return TranslateOrFallback("ABY_CircleConsoleSubtitleDominion", "Dominion crisis core active. Current state: {0}.", phaseLabel);
        }

        public static string FormatTicksShort(int ticks)
        {
            return Mathf.Max(0, ticks).ToStringTicksToPeriod();
        }

        public static string GetCompactSubtitle()
        {
            return TranslateOrFallback("ABY_CircleTab_SubtitleShort", "Compact ritual status and console access.");
        }

        public static string GetCompactHint()
        {
            return TranslateOrFallback("ABY_CircleTab_Hint", "Use the main console for full ritual preview, readiness checks, and automated sigil assignment.");
        }

        public static string GetCompactHint(Building_AbyssalSummoningCircle circle)
        {
            MapComponent_DominionCrisis crisis = GetDominionCrisis(circle);
            if (crisis != null && crisis.IsActive)
            {
                return TranslateOrFallback("ABY_DominionCompactHint", "Dominion crisis telemetry is being routed through the circle.");
            }

            if (crisis != null && crisis.CooldownTicksRemaining > 0)
            {
                return TranslateOrFallback("ABY_DominionCompactHintCooldown", "Dominion lattice recovery in progress. Rearm available in {0}.", crisis.GetCooldownValue());
            }

            return GetCompactHint();
        }

        public static string GetCompactFooter()
        {
            return TranslateOrFallback("ABY_CircleTab_FooterShort", "Use the full console for ritual preview and sigil assignment.");
        }

        public static string GetCompactFooter(Building_AbyssalSummoningCircle circle)
        {
            MapComponent_DominionCrisis crisis = GetDominionCrisis(circle);
            if (crisis != null && crisis.IsActive)
            {
                return TranslateOrFallback("ABY_DominionCompactFooter", "Dominion breach active. Use the full console for anchor and gate tracking.");
            }

            if (crisis != null && (crisis.CompletionCount > 0 || crisis.FailureCount > 0 || crisis.CancelledCount > 0 || crisis.CooldownTicksRemaining > 0))
            {
                return TranslateOrFallback("ABY_DominionCompactFooterCooldown", "Dominion record: {0}. Next payout forecast: {1}.", crisis.GetReplayStatusValue(), crisis.GetRewardForecastValue());
            }

            return GetCompactFooter();
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
            return TranslateOrFallback("ABY_CircleCommand_AssignSigil", "Begin invocation");
        }

        public static string GetJumpToSigilLabel()
        {
            return TranslateOrFallback("ABY_CircleCommand_JumpToSigil", "Track sigil");
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

            MapComponent_DominionCrisis crisis = GetDominionCrisis(circle);
            if (crisis != null && crisis.IsActive)
            {
                return Shorten(crisis.GetStatusLine(), 96);
            }

            return Shorten(circle.GetCurrentStatusLine(), 72);
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

            if (ritual.Id == "ember_hunt")
            {
                return TranslateOrFallback(ritual.LabelKey, "Loose ember hounds");
            }

            if (ritual.Id == "warden_of_ash")
            {
                return TranslateOrFallback(ritual.LabelKey, "Mark of the Warden");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.LabelKey, "Invoke Archon Beast");
            }

            if (ritual.Id == "dominion_gate")
            {
                return TranslateOrFallback(ritual.LabelKey, "Open dominion gate");
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

            if (ritual.Id == "ember_hunt")
            {
                return TranslateOrFallback(ritual.SubtitleKey, "Fast flanking pack assault");
            }

            if (ritual.Id == "warden_of_ash")
            {
                return TranslateOrFallback(ritual.SubtitleKey, "Disciplined mini-boss breach pattern");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.SubtitleKey, "First boss breach pattern");
            }

            if (ritual.Id == "dominion_gate")
            {
                return TranslateOrFallback(ritual.SubtitleKey, "Endgame crisis bootstrap");
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
                return TranslateOrFallback(ritual.DescriptionKey, "Consumes one unstable breach sigil, routes a colonist to the circle, and tears open a smaller hostile rift. Colony size and wealth now scale how many rift imps pour through, with a Hexgun Thrall and a Chain Zealot joining only at the highest threat tier.");
            }

            if (ritual.Id == "ember_hunt")
            {
                return TranslateOrFallback(ritual.DescriptionKey, "Consumes one ember hound sigil, routes a colonist to the circle, and injects a colony-scaled hunter-pack onto the map. Ember hounds leap onto ranged pawns, supporting imps widen the pressure band, and a Hexgun Thrall plus Chain Zealot support pair join only at the highest threat tier.");
            }

            if (ritual.Id == "warden_of_ash")
            {
                return TranslateOrFallback(ritual.DescriptionKey, "Consumes one warden sigil, routes a colonist to the circle, and calls the first disciplined mini-boss breach. The Warden of Ash enters as a heavy infernal overseer with a chest-portal core, ash pulse pressure, and staged imp reinforcement triggers.");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.DescriptionKey, "Consumes one prepared archon sigil, routes a colonist to the circle, charges the breach, and calls the first hostile techno-demonic boss encounter.");
            }

            if (ritual.Id == "dominion_gate")
            {
                return TranslateOrFallback(ritual.DescriptionKey, "Consumes one dominion sigil and uses the full circle lattice to bootstrap the Crowned Gate crisis core. Package 3 turns anchorfall into a live hostile pulse director with recurring abyssal waves, portal pressure, and escalation that ramps while anchors remain standing.");
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
• Colony-scaled starter breach instead of a fixed tiny event
• Cleaner bridge between circle setup and the Archon Beast fight");
            }

            if (ritual.Id == "ember_hunt")
            {
                return TranslateOrFallback(ritual.RewardHintKey, @"• Mid-loop pressure test for interior defense
• Scales into mixed assaults for larger colonies
• Clear flanker identity before the first true boss");
            }

            if (ritual.Id == "warden_of_ash")
            {
                return TranslateOrFallback(ritual.RewardHintKey, @"• Mid-loop mini-boss progression step
• Clean bridge between hunter-pack pressure and the first true boss
• Early Warden-tier unlock hook without skipping straight to Archon content");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.RewardHintKey, @"• First-loop boss progression
• Archon-linked drops and unlock gating
• Early abyssal loot and boss-side material gating");
            }

            if (ritual.Id == "dominion_gate")
            {
                return TranslateOrFallback(ritual.RewardHintKey, @"• Initializes the endgame crisis framework in-world
• Drives recurring hostile pulses while the anchor lattice survives
• Establishes real wave routing, portal pressure, and escalation timing for the later Gate-core package");
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
• Breach size scales from small training pressure to a serious wave for bigger colonies
• A Hexgun Thrall and Chain Zealot only appear at the top threat tier");
            }

            if (ritual.Id == "ember_hunt")
            {
                return TranslateOrFallback(ritual.SideEffectHintKey, @"• Drops a fast hostile hunter-pack at the map edge
• Larger colonies can draw mixed hound/imp assaults, with one supporting thrall and one Chain Zealot only at the top tier
• Lower spectacle than the Archon Beast, but deadly against weak backlines");
            }

            if (ritual.Id == "warden_of_ash")
            {
                return TranslateOrFallback(ritual.SideEffectHintKey, @"• Spawns a hostile armored mini-boss instead of a simple pack
• Emits close-range ash pulses that punish clustering and melee swarms
• Chest-portal phases open imp reinforcements as the Warden loses health");
            }

            if (ritual.Id == "archon_beast")
            {
                return TranslateOrFallback(ritual.SideEffectHintKey, @"• Opens a hostile breach on the current map
• Can trigger escalation pressure if the colony is underprepared
• Ritual phases intensify visuals, sound, and encounter pressure");
            }

            if (ritual.Id == "dominion_gate")
            {
                return TranslateOrFallback(ritual.SideEffectHintKey, @"• Requires full stabilizer coverage and both capacitor bays online
• Blocks other abyssal encounters while the crisis core is active
• Anchorfall now launches recurring hostile waves until the anchors are destroyed or the timer collapses");
            }

            return TranslateOrFallback(ritual.SideEffectHintKey, ritual.Id);
        }

        public static List<string> GetRitualRoleTags(RitualDefinition ritual)
        {
            List<string> tags = new List<string>();
            if (ritual == null)
            {
                return tags;
            }

            switch (ritual.Id)
            {
                case "unstable_breach":
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_Swarm", "swarm"));
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_Rehearsal", "rehearsal"));
                    break;
                case "ember_hunt":
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_Flanking", "flanking"));
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_BacklinePressure", "backline pressure"));
                    break;
                case "warden_of_ash":
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_Miniboss", "mini-boss"));
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_AntiCluster", "anti-cluster"));
                    break;
                case "archon_beast":
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_Boss", "boss"));
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_PhaseEncounter", "phase encounter"));
                    break;
                case "choir_engine":
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_SupportSuppression", "support suppression"));
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_TurretPressure", "turret pressure"));
                    break;
                case "reactor_saint":
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_ShieldArtillery", "shield artillery"));
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_AreaDenial", "area denial"));
                    break;
                case "dominion_gate":
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_ObjectiveEncounter", "objective encounter"));
                    tags.Add(TranslateOrFallback("ABY_CircleRoleTag_WavePressure", "wave pressure"));
                    break;
            }

            return tags;
        }

        public static string GetRewardVectorGuaranteed(RitualDefinition ritual)
        {
            if (ritual == null)
            {
                return string.Empty;
            }

            switch (ritual.Id)
            {
                case "unstable_breach":
                    return TranslateOrFallback("ABY_CircleRewardGuaranteed_Unstable", "Guaranteed: residue and cleanup drops from a controlled early breach.");
                case "ember_hunt":
                    return TranslateOrFallback("ABY_CircleRewardGuaranteed_Ember", "Guaranteed: residue, beast cleanup loot, and a stronger hunter-pack rehearsal.");
                case "warden_of_ash":
                    return TranslateOrFallback("ABY_CircleRewardGuaranteed_Warden", "Guaranteed: 1 Ashen Core plus residue from the first true mini-boss step.");
                case "archon_beast":
                    return TranslateOrFallback("ABY_CircleRewardGuaranteed_Archon", "Guaranteed: Herald Core Fragments and an Archon Override Ampoule from the first true boss.");
                case "choir_engine":
                    return TranslateOrFallback("ABY_CircleRewardGuaranteed_Choir", "Guaranteed: 1 Choir Resonance Core and a heavier residue payout.");
                case "reactor_saint":
                    return TranslateOrFallback("ABY_CircleRewardGuaranteed_Reactor", "Guaranteed: 1 Reactor Saint Core, residue, and spacer-grade salvage.");
                case "dominion_gate":
                    return TranslateOrFallback("ABY_CircleRewardGuaranteed_Dominion", "Guaranteed: dominion outcome rewards, shards, and late-stage progression materials if the breach is contained.");
                default:
                    return GetRitualRewardHint(ritual);
            }
        }

        public static string GetRewardVectorProgression(RitualDefinition ritual)
        {
            if (ritual == null)
            {
                return string.Empty;
            }

            switch (ritual.Id)
            {
                case "unstable_breach":
                    return TranslateOrFallback("ABY_CircleRewardProgression_Unstable", "Progression: teaches the circle loop before the first boss tier.");
                case "ember_hunt":
                    return TranslateOrFallback("ABY_CircleRewardProgression_Ember", "Progression: stress-tests backline defense before mini-boss escalation.");
                case "warden_of_ash":
                    return TranslateOrFallback("ABY_CircleRewardProgression_Warden", "Progression: opens the first ashbound weapon and implant branch.");
                case "archon_beast":
                    return TranslateOrFallback("ABY_CircleRewardProgression_Archon", "Progression: clears the first boss gate and makes herald-side follow-up routes matter.");
                case "choir_engine":
                    return TranslateOrFallback("ABY_CircleRewardProgression_Choir", "Progression: unlocks the support-oriented reward line built around Choir implants and the Canticle Driver.");
                case "reactor_saint":
                    return TranslateOrFallback("ABY_CircleRewardProgression_Reactor", "Progression: feeds saint-class fabrication and pushes the colony toward late-game escalation.");
                case "dominion_gate":
                    return TranslateOrFallback("ABY_CircleRewardProgression_Dominion", "Progression: arms the full dominion crisis ladder and outcome routing.");
                default:
                    return GetRitualRewardHint(ritual);
            }
        }

        public static string GetRewardVectorFollowUp(RitualDefinition ritual)
        {
            if (ritual == null)
            {
                return string.Empty;
            }

            RitualDefinition next = GetNextEscalationRitual(ritual);
            if (next == null)
            {
                return TranslateOrFallback("ABY_CircleRewardFollowUp_Final", "Follow-up: this is already the current apex ritual track.");
            }

            return TranslateOrFallback("ABY_CircleRewardFollowUp_Generic", "Follow-up: next clean escalation is {0}.", GetRitualLabel(next));
        }

        public static string GetRoleTagLine(RitualDefinition ritual)
        {
            List<string> tags = GetRitualRoleTags(ritual);
            return tags.Count == 0 ? string.Empty : string.Join("  •  ", tags.ToArray());
        }

        public static RitualDefinition GetNextEscalationRitual(RitualDefinition ritual)
        {
            if (ritual == null)
            {
                return null;
            }

            for (int i = 0; i < Rituals.Count; i++)
            {
                if (Rituals[i].Id == ritual.Id)
                {
                    return i + 1 < Rituals.Count ? Rituals[i + 1] : null;
                }
            }

            return null;
        }

        public static string GetPrimaryInvocationBlocker(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CircleConsoleFail_NoCircle", "No valid summoning circle is available for console control.");
            }

            if (!circle.IsReadyForSigil(out string failReason))
            {
                return failReason;
            }

            if (!AbyssalCircleCapacitorRitualUtility.TryAuthorizeRitualStart(circle, ritual, circle.CapacitorOverchannelEnabled, out _, out _, out failReason))
            {
                return failReason;
            }

            Thing sigil = FindBestSigil(circle, ritual, out failReason);
            if (sigil == null)
            {
                ThingDef sigilDef = GetSigilDef(ritual);
                if (sigilDef != null)
                {
                    List<Building_ABY_SigilVault> linkedVaults = GetLinkedVaults(circle);
                    for (int i = 0; i < linkedVaults.Count; i++)
                    {
                        Building_ABY_SigilVault vault = linkedVaults[i];
                        if (vault.CountStoredSigilsOfDef(sigilDef) > 0)
                        {
                            return TranslateOrFallback("ABY_CircleMilestone_StageFromVault", "Stage a prepared sigil from the linked vault before beginning the invocation.");
                        }
                    }
                }

                return failReason;
            }

            FindBestOperator(circle, ritual, sigil, out failReason);
            return failReason;
        }

        public static bool IsInvocationPathClear(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            return GetPrimaryInvocationBlocker(circle, ritual).NullOrEmpty();
        }

        public static List<StatusEntry> GetMilestoneEntries(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            List<StatusEntry> entries = new List<StatusEntry>();
            bool ready = IsInvocationPathClear(circle, ritual);
            string blocker = GetPrimaryInvocationBlocker(circle, ritual);

            entries.Add(new StatusEntry
            {
                Label = TranslateOrFallback("ABY_CircleMilestone_Command", "Command path"),
                Value = ready
                    ? TranslateOrFallback("ABY_CircleMilestone_CommandReady", "Invocation path clear. A prepared sigil, operator, and circle state are all valid.")
                    : TranslateOrFallback("ABY_CircleMilestone_CommandBlocked", "Clear blocker: {0}", blocker),
                Satisfied = ready
            });

            entries.Add(new StatusEntry
            {
                Label = TranslateOrFallback("ABY_CircleMilestone_Reward", "Payout focus"),
                Value = GetRewardVectorGuaranteed(ritual),
                Satisfied = CountAvailableSigils(circle, ritual) > 0
            });

            RitualDefinition next = GetNextEscalationRitual(ritual);
            entries.Add(new StatusEntry
            {
                Label = TranslateOrFallback("ABY_CircleMilestone_Next", "Next escalation"),
                Value = next != null
                    ? TranslateOrFallback("ABY_CircleMilestone_NextValue", "After this breach, escalate into {0}.", GetRitualLabel(next))
                    : TranslateOrFallback("ABY_CircleMilestone_NextFinal", "This ritual already sits at the current top of the escalation ladder."),
                Satisfied = false
            });

            return entries;
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

        public static int CountAvailableSigils(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            int count = CountSigilsOnMap(circle?.Map, ritual);
            ThingDef sigilDef = GetSigilDef(ritual);
            if (circle?.Map == null || sigilDef == null)
            {
                return count;
            }

            List<Building_ABY_SigilVault> linkedVaults = GetLinkedVaults(circle);
            for (int i = 0; i < linkedVaults.Count; i++)
            {
                count += linkedVaults[i].CountStoredSigilsOfDef(sigilDef);
            }

            return count;
        }

        public static Thing FindBestSigilJumpTarget(Building_AbyssalSummoningCircle circle, RitualDefinition ritual, out string failReason)
        {
            Thing sigil = FindBestSigil(circle, ritual, out failReason);
            if (sigil != null)
            {
                return sigil;
            }

            Building_ABY_SigilVault vault = FindBestLinkedVaultWithSigil(circle, ritual);
            if (vault != null)
            {
                failReason = null;
                return vault;
            }

            return null;
        }

        private static List<Building_ABY_SigilVault> GetLinkedVaults(Building_AbyssalSummoningCircle circle)
        {
            List<Building_ABY_SigilVault> results = new List<Building_ABY_SigilVault>();
            if (circle?.Map == null)
            {
                return results;
            }

            ThingDef vaultDef = DefDatabase<ThingDef>.GetNamedSilentFail("ABY_SigilVault");
            if (vaultDef == null)
            {
                return results;
            }

            List<Thing> things = circle.Map.listerThings.ThingsOfDef(vaultDef);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Building_ABY_SigilVault vault && vault.Spawned && !vault.Destroyed && vault.IsLinkedTo(circle))
                {
                    results.Add(vault);
                }
            }

            return results;
        }

        private static Building_ABY_SigilVault FindBestLinkedVaultWithSigil(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            ThingDef sigilDef = GetSigilDef(ritual);
            if (circle?.Map == null || sigilDef == null)
            {
                return null;
            }

            Building_ABY_SigilVault best = null;
            float bestScore = float.MaxValue;
            List<Building_ABY_SigilVault> linkedVaults = GetLinkedVaults(circle);
            for (int i = 0; i < linkedVaults.Count; i++)
            {
                Building_ABY_SigilVault vault = linkedVaults[i];
                if (vault.CountStoredSigilsOfDef(sigilDef) <= 0)
                {
                    continue;
                }

                float score = vault.PositionHeld.DistanceToSquared(circle.PositionHeld);
                if (score < bestScore)
                {
                    best = vault;
                    bestScore = score;
                }
            }

            return best;
        }

        public static string GetRitualMetaText(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            int sigilCount = CountAvailableSigils(circle, ritual);
            if (IsDominionRitual(ritual))
            {
                MapComponent_DominionCrisis crisis = GetDominionCrisis(circle);
                string state = crisis != null ? crisis.GetPhaseLabel() : TranslateOrFallback("ABY_DominionCrisisPhase_Dormant", "dormant");
                if (crisis != null && crisis.IsActive)
                {
                    if (crisis.IsGatePhaseActive)
                    {
                        return TranslateOrFallback(
                            "ABY_DominionRitualMetaGate",
                            "Sigils on map: {0}   •   Crisis state: {1}   •   Gate integrity: {2}   •   Next surge: {3}",
                            sigilCount,
                            state,
                            crisis.GetGateIntegrityValue(),
                            crisis.GetGatePulseEtaValue());
                    }

                    return TranslateOrFallback(
                        "ABY_DominionRitualMetaActive",
                        "Sigils on map: {0}   •   Crisis state: {1}   •   Anchors: {2}   •   Wave ETA: {3}",
                        sigilCount,
                        state,
                        crisis.GetAnchorStatusValue(),
                        crisis.GetNextWaveEtaValue());
                }

                if (crisis != null && crisis.CooldownTicksRemaining > 0)
                {
                    return TranslateOrFallback(
                        "ABY_DominionRitualMetaCooldown",
                        "Sigils on map: {0}   •   Crisis state: {1}   •   Rearm: {2}   •   Record: {3}",
                        sigilCount,
                        state,
                        crisis.GetCooldownValue(),
                        crisis.GetReplayStatusValue());
                }

                return TranslateOrFallback(
                    "ABY_DominionRitualMeta",
                    "Sigils on map: {0}   •   Crisis state: {1}   •   Challenge tier: endgame",
                    sigilCount,
                    state);
            }

            if (ritual != null && circle?.Map != null && AbyssalT1SummonScalingUtility.IsSupportedRitual(ritual.Id))
            {
                AbyssalT1SummonScalingUtility.ThreatPlan plan = AbyssalT1SummonScalingUtility.GetThreatPlan(circle.Map, ritual.Id);
                if (plan != null)
                {
                    return TranslateOrFallback(
                        "ABY_CircleRitualMetaScaled",
                        "Sigils on map: {0}   •   {1}   •   Forecast: {2}",
                        sigilCount,
                        AbyssalT1SummonScalingUtility.GetThreatTierLabel(plan.Tier),
                        AbyssalT1SummonScalingUtility.GetPreviewText(plan));
                }
            }

            return TranslateOrFallback("ABY_CircleRitualMeta", "Sigils on map: {0}   •   Threat budget: {1}", sigilCount, ritual?.SpawnPoints ?? 0);
        }

        public static bool IsDominionUiMode(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            return IsDominionRitual(ritual) || GetDominionCrisis(circle)?.IsActive == true;
        }

        public static string GetDominionObjectiveButtonLabel(Building_AbyssalSummoningCircle circle)
        {
            MapComponent_DominionCrisis crisis = GetDominionCrisis(circle);
            if (crisis != null)
            {
                if (crisis.IsGatePhaseActive)
                {
                    return TranslateOrFallback("ABY_DominionCommand_TrackGate", "Track gate core");
                }

                if (crisis.IsAnchorPhaseActive)
                {
                    return TranslateOrFallback("ABY_DominionCommand_TrackAnchor", "Track anchor lattice");
                }
            }

            return TranslateOrFallback("ABY_DominionCommand_TrackObjective", "Track objective");
        }

        public static string GetDominionOpsSummary(Building_AbyssalSummoningCircle circle)
        {
            MapComponent_DominionCrisis crisis = GetDominionCrisis(circle);
            if (crisis == null)
            {
                return TranslateOrFallback("ABY_DominionOpsSummary", "Objective: {0}\nDirective: {1}",
                    TranslateOrFallback("ABY_DominionOpsObjective_Dormant", "No active dominion breach on this map."),
                    TranslateOrFallback("ABY_DominionOpsObjective_Dormant", "No active dominion breach on this map."));
            }

            return TranslateOrFallback("ABY_DominionOpsSummary", "Objective: {0}\nDirective: {1}", crisis.GetPrimaryObjectiveLabel(), crisis.GetDirectiveSummary());
        }

        public static bool TryJumpToDominionObjective(Building_AbyssalSummoningCircle circle, out string failReason)
        {
            failReason = null;
            MapComponent_DominionCrisis crisis = GetDominionCrisis(circle);
            Thing target = crisis?.GetPrimaryObjectiveThing();
            if (target == null)
            {
                failReason = TranslateOrFallback("ABY_DominionCommand_NoObjective", "No dominion objective is currently available on this map.");
                return false;
            }

            CameraJumper.TryJumpAndSelect(target);
            return true;
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

            if (!AbyssalCircleCapacitorRitualUtility.TryAuthorizeRitualStart(circle, ritual, circle.CapacitorOverchannelEnabled, out _, out _, out failReason))
            {
                return false;
            }

            Thing sigil = FindBestSigil(circle, ritual, out failReason);
            if (sigil == null)
            {
                return TryAssignInvocationFromLinkedVault(circle, ritual, out failReason);
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

        public static bool TryAssignCapacitorInstall(Building_AbyssalSummoningCircle circle, Thing capacitorThing, AbyssalCircleCapacitorBay bay, out string failReason)
        {
            failReason = null;

            if (circle == null || circle.Destroyed || !circle.Spawned || circle.Map == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoCircle", "No valid summoning circle is available for console control.");
                return false;
            }

            if (capacitorThing == null || capacitorThing.Destroyed || !capacitorThing.Spawned || capacitorThing.MapHeld != circle.Map)
            {
                failReason = TranslateOrFallback("ABY_CapacitorFail_NoAvailable", "No compatible capacitor module is currently available on this map.");
                return false;
            }

            if (!circle.CanInstallCapacitor(capacitorThing.def, bay, out failReason))
            {
                return false;
            }

            Pawn pawn = FindBestCircleTechnician(circle, capacitorThing, true, out failReason);
            if (pawn == null)
            {
                return false;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ABY_InstallCircleCapacitor");
            if (jobDef == null)
            {
                failReason = "Missing JobDef: ABY_InstallCircleCapacitor";
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, capacitorThing, circle, new IntVec3((int)bay, 0, 0));
            job.count = 1;
            pawn.jobs.TryTakeOrderedJob(job);
            return true;
        }

        public static bool TryAssignCapacitorRemove(Building_AbyssalSummoningCircle circle, AbyssalCircleCapacitorBay bay, out string failReason)
        {
            failReason = null;

            if (circle == null || circle.Destroyed || !circle.Spawned || circle.Map == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoCircle", "No valid summoning circle is available for console control.");
                return false;
            }

            if (!circle.CanRemoveInstalledCapacitor(bay, out failReason))
            {
                return false;
            }

            Pawn pawn = FindBestCircleTechnician(circle, null, false, out failReason);
            if (pawn == null)
            {
                return false;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ABY_RemoveCircleCapacitor");
            if (jobDef == null)
            {
                failReason = "Missing JobDef: ABY_RemoveCircleCapacitor";
                return false;
            }

            Job job = JobMaker.MakeJob(jobDef, circle, new IntVec3((int)bay, 0, 0));
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


        private static bool TryAssignInvocationFromLinkedVault(Building_AbyssalSummoningCircle circle, RitualDefinition ritual, out string failReason)
        {
            failReason = null;
            ThingDef sigilDef = GetSigilDef(ritual);
            if (circle?.Map == null || sigilDef == null)
            {
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoCircle", "No valid summoning circle is available for console control.");
                return false;
            }

            List<Building_ABY_SigilVault> linkedVaults = GetLinkedVaults(circle);
            if (linkedVaults.Count == 0)
            {
                string sigilLabel = sigilDef.label ?? ritual?.SigilThingDefName ?? TranslateOrFallback("ABY_CircleConsoleFail_NoSigilFallback", "prepared sigil");
                failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoSpecificSigil", "No prepared {0} was found on the current map.", sigilLabel);
                return false;
            }

            string firstFailure = null;
            for (int i = 0; i < linkedVaults.Count; i++)
            {
                Building_ABY_SigilVault vault = linkedVaults[i];
                if (vault.CountStoredSigilsOfDef(sigilDef) <= 0)
                {
                    continue;
                }

                if (vault.TryStageOneSigilToLinkedCircleFromConsole(sigilDef, out _, out string stageFailReason))
                {
                    return true;
                }

                if (firstFailure.NullOrEmpty() && !stageFailReason.NullOrEmpty())
                {
                    firstFailure = stageFailReason;
                }
            }

            if (!firstFailure.NullOrEmpty())
            {
                failReason = firstFailure;
                return false;
            }

            string fallbackLabel = sigilDef.label ?? ritual?.SigilThingDefName ?? TranslateOrFallback("ABY_CircleConsoleFail_NoSigilFallback", "prepared sigil");
            failReason = TranslateOrFallback("ABY_CircleConsoleFail_NoSpecificSigil", "No prepared {0} was found on the current map.", fallbackLabel);
            return false;
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

        public static Pawn FindBestCircleTechnician(Building_AbyssalSummoningCircle circle, Thing relatedThing, bool requireThingReservations, out string failReason)
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
                if (!CanServiceCircle(pawn, circle, true))
                {
                    continue;
                }

                float score = pawn.PositionHeld.DistanceToSquared(circle.PositionHeld);
                if (requireThingReservations)
                {
                    if (relatedThing == null || !pawn.CanReserveAndReach(relatedThing, PathEndMode.Touch, Danger.Deadly))
                    {
                        continue;
                    }

                    score = pawn.PositionHeld.DistanceToSquared(relatedThing.PositionHeld);
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
                failReason = TranslateOrFallback("ABY_CapacitorFail_NoTechnician", "No suitable colonist is currently available to service the circle.");
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

        public static bool CanServiceCircle(Pawn pawn, Building_AbyssalSummoningCircle circle, bool requireReservations)
        {
            if (pawn == null || circle == null || pawn.MapHeld != circle.Map || pawn.Dead || pawn.Downed || pawn.InMentalState)
            {
                return false;
            }

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return false;
            }

            if (circle.RitualActive)
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
            int sigils = CountAvailableSigils(circle, ritual);

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
                Label = TranslateOrFallback("ABY_CircleStatus_Encounter", "Encounter"),
                Value = encounterClear ? TranslateOrFallback("ABY_CircleStatus_Clear", "clear") : TranslateOrFallback("ABY_BossSummonFail_EncounterActive", "An abyssal encounter is already active on this map."),
                Satisfied = encounterClear
            });

            if (IsDominionRitual(ritual) || GetDominionCrisis(circle)?.IsActive == true)
            {
                MapComponent_DominionCrisis crisis = GetDominionCrisis(circle);
                bool idle = crisis == null || !crisis.IsActive;
                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_Dominion", "Dominion"),
                    Value = crisis != null ? crisis.GetPhaseLabel() : TranslateOrFallback("ABY_DominionCrisisPhase_Dormant", "dormant"),
                    Satisfied = idle
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_Anchors", "Anchors"),
                    Value = crisis != null ? crisis.GetAnchorStatusValue() : TranslateOrFallback("ABY_DominionAnchor_StatusValue_Pending", "pending"),
                    Satisfied = crisis == null || crisis.ActiveAnchorCount <= 0
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_AnchorPressure", "Anchor pressure"),
                    Value = crisis != null ? crisis.GetAnchorPressureLabel() : TranslateOrFallback("ABY_DominionAnchor_Pressure_None", "none"),
                    Satisfied = crisis == null || crisis.ActiveAnchorCount <= 0
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_Waves", "Wave pulse"),
                    Value = crisis != null ? crisis.GetWaveStatusValue() : TranslateOrFallback("ABY_DominionWavePreviewStatus_Dormant", "dormant"),
                    Satisfied = crisis == null || (!crisis.IsAnchorPhaseActive && !crisis.IsGatePhaseActive)
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_NextWave", "Next pulse"),
                    Value = crisis != null ? crisis.GetNextWaveEtaValue() : TranslateOrFallback("ABY_DominionWaveEta_Pending", "pending"),
                    Satisfied = crisis == null || (!crisis.IsAnchorPhaseActive && !crisis.IsGatePhaseActive)
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_Gate", "Gate core"),
                    Value = crisis != null ? crisis.GetGateStatusValue() : TranslateOrFallback("ABY_DominionGate_Status_Dormant", "dormant"),
                    Satisfied = crisis == null || !crisis.IsGatePhaseActive
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_GateIntegrity", "Gate integrity"),
                    Value = crisis != null ? crisis.GetGateIntegrityValue() : TranslateOrFallback("ABY_DominionGate_Integrity_Dormant", "dormant"),
                    Satisfied = crisis == null || !crisis.IsGatePhaseActive
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_DominionCooldown", "Rearm window"),
                    Value = crisis != null ? crisis.GetCooldownValue() : TranslateOrFallback("ABY_DominionCooldown_Ready", "ready"),
                    Satisfied = crisis == null || !crisis.HasCooldown
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_DominionRewards", "Reward track"),
                    Value = crisis != null ? crisis.GetRewardForecastValue() : TranslateOrFallback("ABY_DominionRewardForecast_Pending", "forecast pending"),
                    Satisfied = true
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_DominionReplay", "Replay record"),
                    Value = crisis != null ? crisis.GetReplayStatusValue() : TranslateOrFallback("ABY_DominionReplay_Empty", "no completed dominion runs on this map yet"),
                    Satisfied = true
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_DominionCalibration", "Calibration"),
                    Value = crisis != null ? crisis.GetCalibrationValue() : TranslateOrFallback("ABY_DominionCalibration_Contained", "contained"),
                    Satisfied = true
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_DominionBudget", "Runtime budget"),
                    Value = crisis != null ? crisis.GetRuntimeBudgetValue() : TranslateOrFallback("ABY_DominionBudget_Value", "0/0 hostiles • 0/0 portals", 0, 0, 0, 0),
                    Satisfied = true
                });

                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CircleStatus_DominionFx", "FX mode"),
                    Value = crisis != null ? crisis.GetFxModeValue() : TranslateOrFallback("ABY_DominionFxMode_Standard", "standard"),
                    Satisfied = true
                });
            }

            if (ritual != null)
            {
                AbyssalCircleCapacitorRitualUtility.CapacitorReadinessReport report = AbyssalCircleCapacitorRitualUtility.CreateReadinessReport(circle, ritual);
                bool forceable = circle.CapacitorOverchannelEnabled && AbyssalCircleCapacitorRitualUtility.CanForceStart(report);
                entries.Add(new StatusEntry
                {
                    Label = TranslateOrFallback("ABY_CapacitorStatus_Support", "Capacitors"),
                    Value = AbyssalCircleCapacitorRitualUtility.GetSupportStatusForConsole(circle, ritual),
                    Satisfied = report.FullySatisfied || forceable
                });
            }

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
            if (circle == null)
            {
                return 0.25f;
            }

            float instabilityReduction = AbyssalForgeProgressUtility.GetSummoningInstabilityReduction(circle.Map);

            if (circle.RitualActive)
            {
                float activeRisk;
                switch (circle.CurrentRitualPhase)
                {
                    case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Charging:
                        activeRisk = 0.58f;
                        break;
                    case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Surge:
                        activeRisk = 0.79f;
                        break;
                    case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Breach:
                        activeRisk = 1f;
                        break;
                    case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Cooldown:
                        activeRisk = 0.52f;
                        break;
                    default:
                        activeRisk = 0.58f;
                        break;
                }

                return Mathf.Clamp01(activeRisk - instabilityReduction);
            }

            float baseRisk = ritual != null ? ritual.BaseRisk : 0.25f;
            int missing = GetStatusEntries(circle, ritual).Count(entry => !entry.Satisfied);
            float capacitorRiskReduction = ritual != null ? AbyssalCircleCapacitorRitualUtility.GetRiskReduction(circle, ritual) : 0f;
            float overchannelRisk = (ritual != null && circle.CapacitorOverchannelEnabled && AbyssalCircleCapacitorRitualUtility.WouldForceStart(circle, ritual)) ? 0.12f : 0f;
            float risk = baseRisk + missing * 0.08f + overchannelRisk - instabilityReduction - capacitorRiskReduction;
            return Mathf.Clamp(risk, 0.05f, 1f);
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

        public static string GetStabilizerPatternDetail(Building_AbyssalSummoningCircle circle)
        {
            AbyssalCircleStabilizerBonusSummary summary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            string key = AbyssalCircleModuleUtility.GetPatternKey(summary);
            switch (key)
            {
                case "ABY_CircleStabilizerPattern_Symmetry":
                    return TranslateOrFallback("ABY_CircleStabilizerPatternDetail_Symmetry", "Uniform full ring. Best anomaly shielding and the cleanest breach geometry.");
                case "ABY_CircleStabilizerPattern_FullRing":
                    return TranslateOrFallback("ABY_CircleStabilizerPatternDetail_FullRing", "All four edge sockets are occupied. The circle holds pressure more evenly.");
                case "ABY_CircleStabilizerPattern_Paired":
                    return TranslateOrFallback("ABY_CircleStabilizerPatternDetail_Paired", "Opposing edges are synchronized. Strong value before the ring is fully completed.");
                case "ABY_CircleStabilizerPattern_Partial":
                    return TranslateOrFallback("ABY_CircleStabilizerPatternDetail_Partial", "Some routing is present, but the lattice is still open and bleeds pressure.");
                default:
                    return TranslateOrFallback("ABY_CircleStabilizerPatternDetail_Open", "No stabilizer lattice is installed. The circle relies on raw containment only.");
            }
        }

        public static string GetStabilizerMiniSummary(Building_AbyssalSummoningCircle circle)
        {
            return TranslateOrFallback(
                "ABY_CircleStabilizerMiniSummary",
                "{0} • {1} • {2}",
                GetStabilizerPatternSummary(circle),
                GetStabilizerContainmentBonusDisplay(circle),
                GetStabilizerHeatDampingDisplay(circle));
        }

        public static string GetStabilizerInspectSummary(Building_AbyssalSummoningCircle circle)
        {
            return TranslateOrFallback(
                "ABY_CircleStabilizerInspectSummary",
                "Containment {0} • Heat {1} • Residue {2} • Anomalies {3}",
                GetStabilizerContainmentBonusDisplay(circle),
                GetStabilizerHeatDampingDisplay(circle),
                GetStabilizerResidueSuppressionDisplay(circle),
                GetStabilizerAnomalyShieldingDisplay(circle));
        }

        public static string GetModuleSlotTooltip(Building_AbyssalSummoningCircle circle, AbyssalCircleModuleEdge edge)
        {
            AbyssalCircleModuleSlot slot = circle?.GetModuleSlot(edge);
            string edgeLabel = AbyssalCircleModuleUtility.GetEdgeLabel(edge);
            if (slot == null || !slot.Occupied || slot.InstalledThingDef == null)
            {
                return TranslateOrFallback("ABY_CircleModuleTooltip_Empty", "{0}: empty socket. Install a stabilizer module to improve containment and reduce ritual pressure.", edgeLabel);
            }

            DefModExtension_AbyssalCircleModule ext = AbyssalCircleModuleUtility.GetModuleExtension(slot.InstalledThingDef);
            int containmentPercent = Mathf.RoundToInt(Mathf.Max(0f, (ext?.containmentBonus ?? 0f) * 22f));
            int heatPercent = Mathf.RoundToInt(Mathf.Max(0f, (1f - (ext?.ritualHeatMultiplier ?? 1f)) * 100f));
            int residuePercent = Mathf.RoundToInt(Mathf.Max(0f, (1f - (ext?.contaminationMultiplier ?? 1f)) * 100f));
            return TranslateOrFallback(
                "ABY_CircleModuleTooltip_Installed",
                "{0}: {1} ({2})\nBase containment: +{3}%\nHeat damping: -{4}%\nResidue suppression: -{5}%",
                edgeLabel,
                slot.InstalledThingDef.label.CapitalizeFirst(),
                AbyssalCircleModuleUtility.GetTierLabel(slot.InstalledThingDef),
                containmentPercent,
                heatPercent,
                residuePercent);
        }


        public static string GetShortRequirementSummary(Building_AbyssalSummoningCircle circle, RitualDefinition ritual)
        {
            List<StatusEntry> entries = GetStatusEntries(circle, ritual);
            int ready = entries.Count(entry => entry.Satisfied);
            return TranslateOrFallback("ABY_CircleReadinessSummary", "{0} / {1} gates clear", ready, entries.Count);
        }
    }
}
