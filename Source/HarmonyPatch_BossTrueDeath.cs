using System.Reflection;
using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class HarmonyPatch_BossTrueDeath_PawnKill
    {
        private static bool Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            if (ABY_BossTrueDeathUtility.TrySuppressPawnKill(__instance, dinfo, exactCulprit))
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossTrueDeath_ShouldBeDead
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.ShouldBeDead));
        }

        private static void Postfix(Pawn_HealthTracker __instance, ref bool __result)
        {
            if (__result && ABY_BossTrueDeathUtility.ShouldSuppressVanillaHealthState(ABY_BossTrueDeathUtility.GetPawn(__instance)))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossTrueDeath_ShouldBeDowned
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.ShouldBeDowned));
        }

        private static void Postfix(Pawn_HealthTracker __instance, ref bool __result)
        {
            if (__result && ABY_BossTrueDeathUtility.ShouldSuppressVanillaHealthState(ABY_BossTrueDeathUtility.GetPawn(__instance)))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossTrueDeath_MakeDowned
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_HealthTracker), "MakeDowned", new[] { typeof(DamageInfo?), typeof(Hediff) });
        }

        private static bool Prefix(Pawn_HealthTracker __instance, DamageInfo? dinfo, Hediff hediff)
        {
            Pawn pawn = ABY_BossTrueDeathUtility.GetPawn(__instance);
            if (!ABY_BossTrueDeathUtility.ShouldSuppressVanillaHealthState(pawn))
            {
                return true;
            }

            ABY_BossTrueDeathUtility.SuppressDowned(pawn, dinfo, hediff);
            return false;
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossTrueDeath_SetDead
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.SetDead));
        }

        private static bool Prefix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = ABY_BossTrueDeathUtility.GetPawn(__instance);
            return !ABY_BossTrueDeathUtility.ShouldSuppressVanillaHealthState(pawn);
        }
    }
}
