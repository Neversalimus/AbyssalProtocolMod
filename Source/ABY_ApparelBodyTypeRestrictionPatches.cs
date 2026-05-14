using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class ABY_Patch_ApparelUtility_HasPartsToWear_BodyTypeRestriction
    {
        private static readonly MethodBase Target = AccessTools.Method(typeof(ApparelUtility), "HasPartsToWear", new[] { typeof(Pawn), typeof(ThingDef) });

        private static bool Prepare()
        {
            return Target != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        private static void Postfix(Pawn __0, ThingDef __1, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            if (!ABY_ApparelBodyTypeRestrictionUtility.CanWear(__0, __1))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    public static class ABY_Patch_PawnApparelTracker_Wear_BodyTypeRestriction
    {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_ApparelTracker), "pawn");
        private static readonly MethodBase Target = AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear", new[] { typeof(Apparel), typeof(bool), typeof(bool), typeof(bool) })
            ?? AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear", new[] { typeof(Apparel), typeof(bool), typeof(bool) })
            ?? AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear", new[] { typeof(Apparel) });

        private static bool Prepare()
        {
            return Target != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        private static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            Pawn pawn = PawnField?.GetValue(__instance) as Pawn;
            if (newApparel?.def == null || pawn == null)
            {
                return true;
            }

            if (ABY_ApparelBodyTypeRestrictionUtility.CanWear(pawn, newApparel.def, out DefModExtension_ABY_ApparelBodyTypeRestriction restriction, out _))
            {
                return true;
            }

            ABY_ApparelBodyTypeRestrictionUtility.TryShowRejectMessage(pawn, newApparel.def, restriction);
            return false;
        }
    }

    [HarmonyPatch]
    public static class ABY_Patch_JobGiverOptimizeApparel_ApparelScoreGain_BodyTypeRestriction
    {
        private static readonly MethodBase Target = AccessTools.Method(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain", new[] { typeof(Pawn), typeof(Apparel) });

        private static bool Prepare()
        {
            return Target != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        private static void Postfix(Pawn pawn, Apparel ap, ref float __result)
        {
            if (ap?.def == null || pawn == null)
            {
                return;
            }

            if (!ABY_ApparelBodyTypeRestrictionUtility.CanWear(pawn, ap.def))
            {
                __result = -1000f;
            }
        }
    }
}
