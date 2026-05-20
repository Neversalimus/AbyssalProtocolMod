using System;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ProtocolResearchGateUtility
    {
        public static bool GatingEnabled => AbyssalProtocolMod.Settings?.enableProtocolNexusGating ?? false;

        public static bool IsDecodedForForge(RecipeDef recipe)
        {
            if (!GatingEnabled)
            {
                return true;
            }

            string required = GetRequiredProtocolResearchDefName(recipe);
            if (required.NullOrEmpty())
            {
                return true;
            }

            return IsDecoded(required);
        }

        public static bool IsDecoded(string projectDefName)
        {
            if (projectDefName.NullOrEmpty())
            {
                return false;
            }

            if (!GatingEnabled)
            {
                return true;
            }

            ABY_ProtocolResearchProgressGameComponent progress = ABY_ProtocolResearchProgressGameComponent.Current;
            return progress != null && progress.IsDecoded(projectDefName);
        }

        public static void MarkDecoded(string projectDefName)
        {
            if (projectDefName.NullOrEmpty())
            {
                return;
            }

            ABY_ProtocolResearchProgressGameComponent.Current?.MarkDecoded(projectDefName);
        }

        public static string GetRequiredProtocolResearchDefName(RecipeDef recipe)
        {
            DefModExtension_AbyssalForgeUnlock extension = AbyssalForgeProgressUtility.GetUnlockExtension(recipe);
            if (extension != null && !extension.requiredProtocolResearchDefName.NullOrEmpty())
            {
                return extension.requiredProtocolResearchDefName;
            }

            return InferDefaultProtocolResearchDefName(recipe, extension);
        }

        public static string GetForgeDisplayLabel(RecipeDef recipe)
        {
            if (IsDecodedForForge(recipe))
            {
                return AbyssalForgeProgressUtility.GetRecipeDisplayLabel(recipe);
            }

            DefModExtension_AbyssalForgeUnlock extension = AbyssalForgeProgressUtility.GetUnlockExtension(recipe);
            if (extension != null && !extension.unknownLabel.NullOrEmpty())
            {
                return extension.unknownLabel;
            }

            string category = AbyssalForgeProgressUtility.GetCategory(recipe);
            switch (category)
            {
                case AbyssalForgeProgressUtility.WeaponsCategory:
                    return TranslateOrFallback("ABY_ForgeUnknownWeaponPattern", "UNKNOWN WEAPON PATTERN");
                case AbyssalForgeProgressUtility.ArmorCategory:
                    return TranslateOrFallback("ABY_ForgeUnknownArmorPattern", "UNKNOWN ARMOR PATTERN");
                case AbyssalForgeProgressUtility.ImplantsCategory:
                    return TranslateOrFallback("ABY_ForgeUnknownImplantPattern", "UNKNOWN IMPLANT PROCEDURE");
                case AbyssalForgeProgressUtility.RitualCategory:
                    return TranslateOrFallback("ABY_ForgeUnknownRitualPattern", "UNKNOWN RITUAL PROTOCOL");
                case AbyssalForgeProgressUtility.HeraldCategory:
                    return TranslateOrFallback("ABY_ForgeUnknownHeraldPattern", "UNKNOWN HERALDIC SCHEMA");
                case AbyssalForgeProgressUtility.TurretSystemsCategory:
                    return TranslateOrFallback("ABY_ForgeUnknownTurretPattern", "UNKNOWN TURRET MODULE");
                default:
                    return TranslateOrFallback("ABY_ForgeUnknownPattern", "UNKNOWN FORGE PATTERN");
            }
        }

        public static string GetUnknownHint(RecipeDef recipe)
        {
            DefModExtension_AbyssalForgeUnlock extension = AbyssalForgeProgressUtility.GetUnlockExtension(recipe);
            if (extension != null && !extension.unknownHint.NullOrEmpty())
            {
                return extension.unknownHint;
            }

            string required = GetRequiredProtocolResearchDefName(recipe);
            if (!required.NullOrEmpty())
            {
                return TranslateOrFallback("ABY_ForgeUnknownDecodeHint", "Decode required in Protocol Nexus: {0}", GetProtocolProjectLabel(required));
            }

            return TranslateOrFallback("ABY_ForgeUnknownGenericHint", "The Forge detects a pattern, but its operational schema is not yet decoded.");
        }

        public static string GetProtocolProjectLabel(string projectDefName)
        {
            if (projectDefName.NullOrEmpty())
            {
                return TranslateOrFallback("ABY_ProtocolResearch_UnknownProject", "unknown protocol");
            }

            try
            {
                ABY_ProtocolResearchDef project = DefDatabase<ABY_ProtocolResearchDef>.GetNamedSilentFail(projectDefName);
                if (project != null)
                {
                    return project.LabelCap;
                }
            }
            catch
            {
                // Keep Forge removable-safe: if the experimental def class/database is absent or broken,
                // the bridge should degrade to a raw name rather than blocking the forge.
            }

            return projectDefName;
        }

        public static string InferDefaultProtocolResearchDefName(RecipeDef recipe, DefModExtension_AbyssalForgeUnlock extension = null)
        {
            if (recipe == null)
            {
                return null;
            }

            string category = extension?.category;
            if (category.NullOrEmpty())
            {
                category = AbyssalForgeProgressUtility.GetCategory(recipe);
            }

            int residue = Math.Max(0, extension?.requiredResidue ?? AbyssalForgeProgressUtility.GetRequiredResidue(recipe));
            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            string defName = ((Def)product ?? recipe).defName ?? string.Empty;
            string lowered = defName.ToLowerInvariant();

            if (category == AbyssalForgeProgressUtility.HeraldCategory || lowered.Contains("herald") || lowered.Contains("oblivion") || lowered.Contains("crownspike") || lowered.Contains("crownshard") || lowered.Contains("ultraplasma"))
            {
                return residue >= 1800 ? "ABY_PR_OblivionChoirInterface" : "ABY_PR_HeraldicFragmentAnalysis";
            }

            if (lowered.Contains("dominion") || lowered.Contains("crownedcore"))
            {
                return "ABY_PR_CrownedCoreExtraction";
            }

            switch (category)
            {
                case AbyssalForgeProgressUtility.WeaponsCategory:
                    if (residue <= 180) return "ABY_PR_BasicAbyssalArms";
                    if (lowered.Contains("null")) return "ABY_PR_NullGeometryHandling";
                    if (residue <= 700) return "ABY_PR_RiftBallistics";
                    if (residue <= 1700) return "ABY_PR_HeavyInfernalSystems";
                    return "ABY_PR_ApexWeaponry";
                case AbyssalForgeProgressUtility.ArmorCategory:
                    if (residue <= 280) return "ABY_PR_AshboundCombatKit";
                    if (residue <= 850) return "ABY_PR_AbyssalArmorSystems";
                    if (residue <= 1900) return "ABY_PR_GatebreakerCarapaceLogic";
                    return "ABY_PR_DominionSurvivalFrames";
                case AbyssalForgeProgressUtility.ImplantsCategory:
                    if (residue <= 320) return "ABY_PR_BasicAbyssalImplants";
                    if (residue <= 1300) return "ABY_PR_AdvancedImplants";
                    return "ABY_PR_DominionBiology";
                case AbyssalForgeProgressUtility.RitualCategory:
                    if (residue <= 200) return "ABY_PR_PrimitiveBreachProtocols";
                    if (residue <= 850) return "ABY_PR_EliteSummoningPatterns";
                    if (residue <= 1500) return "ABY_PR_MajorBossInvocation";
                    return "ABY_PR_DominionGateBootstrapping";
                case AbyssalForgeProgressUtility.TurretSystemsCategory:
                    if (residue <= 220) return "ABY_PR_ModularTurretInterface";
                    if (residue <= 1000) return "ABY_PR_BreachLockdownSystems";
                    return "ABY_PR_CrownfireSepulcherCalibration";
                case AbyssalForgeProgressUtility.CoreCategory:
                    if (lowered.Contains("capacitor")) return "ABY_PR_AshboundCapacitance";
                    if (lowered.Contains("stabilizer") || lowered.Contains("module")) return "ABY_PR_CircleStabilizerFrames";
                    if (residue >= 1600) return "ABY_PR_CrownLogicDecoding";
                    return "ABY_PR_ResidueProcessing";
                default:
                    return residue >= 1600 ? "ABY_PR_CrownLogicDecoding" : "ABY_PR_ForgeContactVocabulary";
            }
        }

        private static string TranslateOrFallback(string key, string fallback)
        {
            string translated = key.Translate();
            return translated == key ? fallback : translated;
        }

        private static string TranslateOrFallback(string key, string fallbackFormat, params object[] args)
        {
            string translated = key.Translate();
            string template = translated == key ? fallbackFormat : translated;
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return fallbackFormat;
            }
        }
    }
}
