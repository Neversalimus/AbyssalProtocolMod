using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{

    [HarmonyPatch]
    public static class ABY_Patch_FloatMenuOptionProvider_Wear_BodyTypeRestriction
    {
        private static readonly MethodBase Target = AccessTools.Method(typeof(FloatMenuOptionProvider_Wear), "GetSingleOptionFor", new[] { typeof(Thing), typeof(FloatMenuContext) });

        private static bool Prepare()
        {
            return Target != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        private static bool Prefix(Thing clickedThing, FloatMenuContext context, ref FloatMenuOption __result)
        {
            Apparel apparel = clickedThing as Apparel;
            Pawn pawn = context?.FirstSelectedPawn;
            if (pawn == null || apparel?.def == null)
            {
                return true;
            }

            if (ABY_ApparelBodyTypeRestrictionUtility.CanWear(pawn, apparel.def, out DefModExtension_ABY_ApparelBodyTypeRestriction restriction, out _))
            {
                return true;
            }

            __result = ABY_ApparelBodyTypeRestrictionUtility.BuildFloatMenuRejectOption(pawn, apparel, restriction);
            return false;
        }
    }

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
    public static class ABY_Patch_PawnApparelTracker_CanWearWithoutDroppingAnything_BodyTypeRestriction
    {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_ApparelTracker), "pawn");
        private static readonly MethodBase Target = AccessTools.Method(typeof(Pawn_ApparelTracker), "CanWearWithoutDroppingAnything", new[] { typeof(ThingDef) });

        private static bool Prepare()
        {
            return Target != null && PawnField != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        private static void Postfix(Pawn_ApparelTracker __instance, ThingDef apDef, ref bool __result)
        {
            if (!__result || apDef == null)
            {
                return;
            }

            Pawn pawn = PawnField.GetValue(__instance) as Pawn;
            if (!ABY_ApparelBodyTypeRestrictionUtility.CanWear(pawn, apDef))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    public static class ABY_Patch_PawnApparelTracker_Wear_BodyTypeRestriction
    {
        private static readonly FieldInfo PawnField = AccessTools.Field(typeof(Pawn_ApparelTracker), "pawn");
        private static readonly MethodBase Target = AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear", new[] { typeof(Apparel), typeof(bool), typeof(bool) })
            ?? AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear", new[] { typeof(Apparel), typeof(bool), typeof(bool), typeof(bool) })
            ?? AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear", new[] { typeof(Apparel), typeof(bool) })
            ?? AccessTools.Method(typeof(Pawn_ApparelTracker), "Wear", new[] { typeof(Apparel) });

        private static bool Prepare()
        {
            return Target != null && PawnField != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        private static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
        {
            Pawn pawn = PawnField.GetValue(__instance) as Pawn;
            if (newApparel?.def == null || pawn == null)
            {
                return true;
            }

            return !ABY_ApparelBodyTypeRestrictionUtility.TryRejectIncompatibleWear(pawn, newApparel.def);
        }
    }

    [HarmonyPatch]
    public static class ABY_Patch_JobDriverWear_TryMakePreToilReservations_BodyTypeRestriction
    {
        private static readonly PropertyInfo ApparelProperty = AccessTools.Property(typeof(JobDriver_Wear), "Apparel");
        private static readonly MethodBase Target = AccessTools.Method(typeof(JobDriver_Wear), "TryMakePreToilReservations", new[] { typeof(bool) });

        private static bool Prepare()
        {
            return Target != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        private static bool Prefix(JobDriver_Wear __instance, ref bool __result)
        {
            Apparel apparel = ApparelProperty?.GetValue(__instance, null) as Apparel
                ?? __instance?.job?.targetA.Thing as Apparel
                ?? __instance?.job?.targetB.Thing as Apparel;

            Pawn pawn = __instance?.pawn;
            if (pawn == null || apparel?.def == null)
            {
                return true;
            }

            if (!ABY_ApparelBodyTypeRestrictionUtility.TryRejectIncompatibleWear(pawn, apparel.def))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch]
    public static class ABY_Patch_JobDriverForceTargetWear_TryMakePreToilReservations_BodyTypeRestriction
    {
        private static readonly PropertyInfo ApparelProperty = AccessTools.Property(typeof(JobDriver_ForceTargetWear), "Apparel");
        private static readonly PropertyInfo TargetPawnProperty = AccessTools.Property(typeof(JobDriver_ForceTargetWear), "TargetPawn");
        private static readonly MethodBase Target = AccessTools.Method(typeof(JobDriver_ForceTargetWear), "TryMakePreToilReservations", new[] { typeof(bool) });

        private static bool Prepare()
        {
            return Target != null;
        }

        private static MethodBase TargetMethod()
        {
            return Target;
        }

        private static bool Prefix(JobDriver_ForceTargetWear __instance, ref bool __result)
        {
            Apparel apparel = ApparelProperty?.GetValue(__instance, null) as Apparel
                ?? __instance?.job?.targetA.Thing as Apparel
                ?? __instance?.job?.targetB.Thing as Apparel;

            Pawn targetPawn = TargetPawnProperty?.GetValue(__instance, null) as Pawn
                ?? __instance?.job?.targetA.Thing as Pawn
                ?? __instance?.job?.targetB.Thing as Pawn;

            Pawn actorPawn = __instance?.pawn;
            Pawn pawnToDress = targetPawn ?? actorPawn;
            if (pawnToDress == null || apparel?.def == null)
            {
                return true;
            }

            if (!ABY_ApparelBodyTypeRestrictionUtility.TryRejectIncompatibleWear(pawnToDress, apparel.def))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch]
    public static class ABY_Patch_JobGiverOptimizeApparel_ApparelScoreGain_BodyTypeRestriction
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo scoreGainModern = AccessTools.Method(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain", new[] { typeof(Pawn), typeof(Apparel), typeof(List<float>) });
            if (scoreGainModern != null)
            {
                yield return scoreGainModern;
            }

            MethodInfo scoreGainLegacy = AccessTools.Method(typeof(JobGiver_OptimizeApparel), "ApparelScoreGain", new[] { typeof(Pawn), typeof(Apparel) });
            if (scoreGainLegacy != null)
            {
                yield return scoreGainLegacy;
            }

            MethodInfo scoreRaw = AccessTools.Method(typeof(JobGiver_OptimizeApparel), "ApparelScoreRaw", new[] { typeof(Pawn), typeof(Apparel) });
            if (scoreRaw != null)
            {
                yield return scoreRaw;
            }
        }

        private static void Postfix(Pawn pawn, Apparel ap, ref float __result)
        {
            if (ap?.def == null || pawn == null)
            {
                return;
            }

            if (!ABY_ApparelBodyTypeRestrictionUtility.CanWear(pawn, ap.def))
            {
                __result = -1000000f;
            }
        }
    }
}
