using System;
using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Central classification helper for Abyssal pawns. New content should prefer XML
    /// extensions on PawnKindDef/race ThingDef; hardcoded names here are compatibility fallbacks
    /// for older saves and legacy content paths.
    /// </summary>
    public static class ABY_AbyssalPawnClassificationUtility
    {
        private static readonly HashSet<string> LegacyProtectedBossOrMiniBossNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ABY_WardenOfAsh",
            "ABY_ChoirEngine",
            "ABY_ArchonBeast",
            "ABY_ReliquaryArchonBeast",
            "ABY_ArchonOfRupture",
            "ABY_ReactorSaint",
            "ABY_DominionSaint",
            "ABY_DominionHeart",
            "ABY_CrownedGate",
            "ABY_TheCrownedGate"
        };

        private static readonly string[] LegacyProtectedNameFragments =
        {
            "WardenOfAsh",
            "ChoirEngine",
            "Archon",
            "ReactorSaint",
            "Dominion",
            "CrownedGate",
            "Boss"
        };

        private static readonly HashSet<string> LegacyConstructPhysiologyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ABY_ReactorSaint",
            "ABY_SiegeIdol",
            "ABY_SiegeIdolEscort",
            "ABY_HaloHusk",
            "ABY_ChoirEngine"
        };

        public static bool IsAbyssalPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (HasClassificationExtension(pawn, extension => extension.isAbyssal))
            {
                return true;
            }

            if (pawn.TryGetComp<CompAbyssalPawnController>() != null)
            {
                return true;
            }

            if (pawn.kindDef?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>() != null)
            {
                return true;
            }

            return IsAbyssalDefName(pawn.def?.defName) || IsAbyssalDefName(pawn.kindDef?.defName);
        }

        public static bool IsBossOrMiniBoss(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (HasClassificationExtension(pawn, extension => extension.isBoss || extension.isMiniBoss))
            {
                return true;
            }

            DefModExtension_AbyssalDifficultyScaling scaling = pawn.kindDef?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
            if (IsBossRole(scaling?.role))
            {
                return true;
            }

            return IsLegacyProtectedBossOrMiniBossName(pawn.def?.defName) || IsLegacyProtectedBossOrMiniBossName(pawn.kindDef?.defName);
        }

        public static bool IsBossOrMiniBoss(ThingDef raceDef, PawnKindDef kindDef)
        {
            if (HasClassificationExtension(raceDef, extension => extension.isBoss || extension.isMiniBoss)
                || HasClassificationExtension(kindDef, extension => extension.isBoss || extension.isMiniBoss))
            {
                return true;
            }

            DefModExtension_AbyssalDifficultyScaling scaling = kindDef?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
            if (IsBossRole(scaling?.role))
            {
                return true;
            }

            return IsLegacyProtectedBossOrMiniBossName(raceDef?.defName) || IsLegacyProtectedBossOrMiniBossName(kindDef?.defName);
        }

        public static bool IsBossOrMiniBossName(string defName)
        {
            return IsLegacyProtectedBossOrMiniBossName(defName);
        }

        public static bool IsConstructPhysiologyPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Corpse != null)
            {
                return false;
            }

            if (HasClassificationExtension(pawn, extension => extension.constructPhysiology || extension.blockBloodLoss))
            {
                return true;
            }

            if (LegacyConstructPhysiologyNames.Contains(pawn.def?.defName ?? string.Empty)
                || LegacyConstructPhysiologyNames.Contains(pawn.kindDef?.defName ?? string.Empty))
            {
                return true;
            }

            if (pawn.TryGetComp<CompABY_ReactorSaintShooter>() != null)
            {
                return true;
            }

            if (pawn.TryGetComp<CompABY_SiegeIdolSiegeShooter>() != null)
            {
                return true;
            }

            if (pawn.TryGetComp<CompABY_ChoirEngineAura>() != null || pawn.TryGetComp<CompABY_ChoirEngineRelay>() != null)
            {
                return true;
            }

            return false;
        }

        public static bool ShouldBlockBloodLoss(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Corpse != null)
            {
                return false;
            }

            if (HasClassificationExtension(pawn, extension => extension.blockBloodLoss || extension.constructPhysiology))
            {
                return true;
            }

            return IsConstructPhysiologyPawn(pawn);
        }

        public static bool IsAbyssalDefName(string defName)
        {
            return !defName.NullOrEmpty() && defName.StartsWith("ABY_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBossRole(string role)
        {
            if (role.NullOrEmpty())
            {
                return false;
            }

            return string.Equals(role, "boss", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "miniboss", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "miniBoss", StringComparison.OrdinalIgnoreCase)
                || role.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsLegacyProtectedBossOrMiniBossName(string defName)
        {
            if (defName.NullOrEmpty())
            {
                return false;
            }

            if (LegacyProtectedBossOrMiniBossNames.Contains(defName))
            {
                return true;
            }

            for (int i = 0; i < LegacyProtectedNameFragments.Length; i++)
            {
                if (defName.IndexOf(LegacyProtectedNameFragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasClassificationExtension(Pawn pawn, Predicate<ABY_AbyssalPawnClassificationExtension> predicate)
        {
            if (pawn == null || predicate == null)
            {
                return false;
            }

            return HasClassificationExtension(pawn.kindDef, predicate) || HasClassificationExtension(pawn.def, predicate);
        }

        private static bool HasClassificationExtension(Def def, Predicate<ABY_AbyssalPawnClassificationExtension> predicate)
        {
            ABY_AbyssalPawnClassificationExtension extension = def?.GetModExtension<ABY_AbyssalPawnClassificationExtension>();
            return extension != null && predicate(extension);
        }
    }
}
