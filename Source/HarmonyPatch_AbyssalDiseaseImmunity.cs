using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalDiseaseImmunity_AdjustSeverity
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
            if (!ABY_AbyssalDiseaseUtility.MightBlockHediff(hdDef))
            {
                return true;
            }

            return !ABY_AbyssalDiseaseUtility.TryBlockHediffAdd(pawn, hdDef);
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalDiseaseImmunity_AddHediffDef
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
            if (!ABY_AbyssalDiseaseUtility.MightBlockHediff(def))
            {
                return true;
            }

            Pawn pawn = ABY_AbyssalDiseaseUtility.GetPawn(__instance);
            if (!ABY_AbyssalDiseaseUtility.TryBlockHediffAdd(pawn, def))
            {
                return true;
            }

            __result = null;
            return false;
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalDiseaseImmunity_GetOrAddHediffDef
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(Pawn_HealthTracker),
                nameof(Pawn_HealthTracker.GetOrAddHediff),
                new[] { typeof(HediffDef), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) });
        }

        private static bool Prefix(Pawn_HealthTracker __instance, HediffDef def, ref Hediff __result)
        {
            if (!ABY_AbyssalDiseaseUtility.MightBlockHediff(def))
            {
                return true;
            }

            Pawn pawn = ABY_AbyssalDiseaseUtility.GetPawn(__instance);
            if (!ABY_AbyssalDiseaseUtility.TryBlockHediffAdd(pawn, def))
            {
                return true;
            }

            __result = null;
            return false;
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalDiseaseImmunity_AddHediffInstance
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
            if (!ABY_AbyssalDiseaseUtility.MightBlockHediff(hediff))
            {
                return true;
            }

            Pawn pawn = ABY_AbyssalDiseaseUtility.GetPawn(__instance);
            return !ABY_AbyssalDiseaseUtility.TryBlockHediffAdd(pawn, hediff);
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalDiseaseImmunity_HediffSetAddDirect
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
            if (!ABY_AbyssalDiseaseUtility.MightBlockHediff(hediff))
            {
                return true;
            }

            Pawn pawn = ABY_AbyssalDiseaseUtility.GetPawn(__instance);
            return !ABY_AbyssalDiseaseUtility.TryBlockHediffAdd(pawn, hediff);
        }
    }
}
