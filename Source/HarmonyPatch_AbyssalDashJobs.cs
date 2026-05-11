using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalDashPathFollower
    {
        private static readonly AccessTools.FieldRef<Pawn_PathFollower, Pawn> PawnRef = AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn");

        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo method = AccessTools.Method(typeof(Pawn_PathFollower), "PatherTick");
            if (method != null)
            {
                yield return method;
            }
        }

        private static bool Prefix(Pawn_PathFollower __instance)
        {
            Pawn pawn = SafeGetPawn(__instance);
            return !ABY_AbyssalDashRuntime.IsDashing(pawn);
        }

        private static Pawn SafeGetPawn(Pawn_PathFollower follower)
        {
            try
            {
                return follower != null ? PawnRef(follower) : null;
            }
            catch
            {
                return null;
            }
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalDashJobTracker
    {
        private static readonly AccessTools.FieldRef<Pawn_JobTracker, Pawn> PawnRef = AccessTools.FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");

        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = typeof(Pawn_JobTracker).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || method.IsGenericMethod)
                {
                    continue;
                }

                if (method.Name == "StartJob" || method.Name == "TryTakeOrderedJob")
                {
                    yield return method;
                }
            }
        }

        private static bool Prefix(Pawn_JobTracker __instance)
        {
            Pawn pawn = SafeGetPawn(__instance);
            return !ABY_AbyssalDashRuntime.IsDashing(pawn);
        }

        private static Pawn SafeGetPawn(Pawn_JobTracker tracker)
        {
            try
            {
                return tracker != null ? PawnRef(tracker) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
