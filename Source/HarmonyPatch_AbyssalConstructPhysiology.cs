using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalConstructPhysiology_AdjustSeverity
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(HealthUtility),
                nameof(HealthUtility.AdjustSeverity),
                new[] { typeof(Pawn), typeof(HediffDef), typeof(float) });
        }

        private static bool Prefix(Pawn pawn, HediffDef hdDef)
        {
            return !ABY_AbyssalConstructPhysiologyUtility.TryBlockBloodLossAdd(pawn, hdDef);
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalConstructPhysiology_AddHediffDef
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(Pawn_HealthTracker),
                nameof(Pawn_HealthTracker.AddHediff),
                new[] { typeof(HediffDef), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) });
        }

        private static bool Prefix(Pawn_HealthTracker __instance, HediffDef def, ref Hediff __result)
        {
            Pawn pawn = ABY_AbyssalConstructPhysiologyUtility.GetPawn(__instance);
            if (!ABY_AbyssalConstructPhysiologyUtility.TryBlockBloodLossAdd(pawn, def))
            {
                return true;
            }

            __result = null;
            return false;
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalConstructPhysiology_AddHediffInstance
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(Pawn_HealthTracker),
                nameof(Pawn_HealthTracker.AddHediff),
                new[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) });
        }

        private static bool Prefix(Pawn_HealthTracker __instance, Hediff hediff)
        {
            Pawn pawn = ABY_AbyssalConstructPhysiologyUtility.GetPawn(__instance);
            return !ABY_AbyssalConstructPhysiologyUtility.TryBlockBloodLossAdd(pawn, hediff);
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalConstructPhysiology_HediffSetAddDirect
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(HediffSet),
                nameof(HediffSet.AddDirect),
                new[] { typeof(Hediff), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) });
        }

        private static bool Prefix(HediffSet __instance, Hediff hediff)
        {
            Pawn pawn = ABY_AbyssalConstructPhysiologyUtility.GetPawn(__instance);
            return !ABY_AbyssalConstructPhysiologyUtility.TryBlockBloodLossAdd(pawn, hediff);
        }
    }
}
