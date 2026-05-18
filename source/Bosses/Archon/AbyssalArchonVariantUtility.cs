using System;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalArchonVariantUtility
    {
        public const string ArchonRitualId = "archon_beast";
        public const string ArchonBeastDefName = "ABY_ArchonBeast";
        public const string ReliquaryArchonBeastDefName = "ABY_ReliquaryArchonBeast";
        public const string DefaultReliquaryDifficultyFloorDefName = "ABY_Difficulty_Rupture";

        public static bool IsArchonBeastFamily(Pawn pawn)
        {
            return pawn != null && (IsArchonBeastFamilyDefName(pawn.def?.defName) || IsArchonBeastFamilyDefName(pawn.kindDef?.defName));
        }

        public static bool IsArchonBeastFamily(PawnKindDef kindDef)
        {
            return kindDef != null && IsArchonBeastFamilyDefName(kindDef.defName);
        }

        public static bool IsArchonBeastFamilyDefName(string defName)
        {
            return string.Equals(defName, ArchonBeastDefName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, ReliquaryArchonBeastDefName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsReliquaryArchonBeastDefName(string defName)
        {
            return string.Equals(defName, ReliquaryArchonBeastDefName, StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolvePawnKindDefName(CompProperties_UseEffectSummonBoss props)
        {
            if (ShouldUseReliquaryArchonVariant(props))
            {
                return props.alternatePawnKindDefName;
            }

            return props?.pawnKindDefName;
        }

        public static string ResolveBossLabel(CompProperties_UseEffectSummonBoss props)
        {
            if (ShouldUseReliquaryArchonVariant(props) && !props.alternateBossLabel.NullOrEmpty())
            {
                return props.alternateBossLabel;
            }

            return props?.bossLabel;
        }

        public static bool ShouldUseReliquaryArchonVariant(CompProperties_UseEffectSummonBoss props)
        {
            if (props == null || props.alternatePawnKindDefName.NullOrEmpty())
            {
                return false;
            }

            if (!string.Equals(props.ritualId, ArchonRitualId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string floorDefName = props.alternateDifficultyFloorDefName.NullOrEmpty()
                ? DefaultReliquaryDifficultyFloorDefName
                : props.alternateDifficultyFloorDefName;

            if (AbyssalDifficultyUtility.GetCurrentProfileOrder() < AbyssalDifficultyUtility.GetProfileOrder(floorDefName))
            {
                return false;
            }

            return DefDatabase<PawnKindDef>.GetNamedSilentFail(props.alternatePawnKindDefName) != null;
        }

        public static float ResolveArchonEscortFallbackBudget(PawnKindDef bossKindDef, float normalBudget)
        {
            if (bossKindDef != null && IsReliquaryArchonBeastDefName(bossKindDef.defName))
            {
                return Math.Max(normalBudget, 720f);
            }

            return normalBudget;
        }
    }
}
