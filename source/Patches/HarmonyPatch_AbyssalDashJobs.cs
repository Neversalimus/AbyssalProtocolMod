using System;
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
        private static AccessTools.FieldRef<Pawn_PathFollower, Pawn> pawnRef;
        private static bool pawnRefResolveAttempted;
        private static bool pawnRefResolveFailed;

        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo method = AccessTools.Method(typeof(Pawn_PathFollower), "PatherTick");
            if (method != null)
            {
                yield return method;
            }
            else
            {
                ABY_LogThrottleUtility.Warning(
                    "dash-pather-target-missing",
                    "[Abyssal Protocol] Dash Harmony patch could not resolve Pawn_PathFollower.PatherTick; dash path-freeze guard disabled for this runtime.",
                    999999);
            }
        }

        private static bool Prefix(Pawn_PathFollower __instance)
        {
            Pawn pawn = SafeGetPawn(__instance);
            return !ABY_AbyssalDashRuntime.IsDashing(pawn);
        }

        private static Pawn SafeGetPawn(Pawn_PathFollower follower)
        {
            if (follower == null)
            {
                return null;
            }

            AccessTools.FieldRef<Pawn_PathFollower, Pawn> resolvedRef = ResolvePawnRef();
            if (resolvedRef == null)
            {
                return null;
            }

            try
            {
                return resolvedRef(follower);
            }
            catch (Exception ex)
            {
                pawnRefResolveFailed = true;
                ABY_LogThrottleUtility.Warning(
                    "dash-pather-pawnref-read-failed",
                    "[Abyssal Protocol] Dash Harmony patch failed to read Pawn_PathFollower.pawn; dash path-freeze guard disabled. " + ex.GetType().Name + ": " + ex.Message,
                    999999);
                return null;
            }
        }

        private static AccessTools.FieldRef<Pawn_PathFollower, Pawn> ResolvePawnRef()
        {
            if (pawnRefResolveAttempted)
            {
                return pawnRefResolveFailed ? null : pawnRef;
            }

            pawnRefResolveAttempted = true;
            try
            {
                pawnRef = AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn");
                pawnRefResolveFailed = pawnRef == null;
            }
            catch (Exception ex)
            {
                pawnRef = null;
                pawnRefResolveFailed = true;
                ABY_LogThrottleUtility.Warning(
                    "dash-pather-pawnref-bind-failed",
                    "[Abyssal Protocol] Dash Harmony patch could not bind Pawn_PathFollower.pawn; dash path-freeze guard disabled for this runtime. " + ex.GetType().Name + ": " + ex.Message,
                    999999);
            }

            return pawnRefResolveFailed ? null : pawnRef;
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalDashJobTracker
    {
        private static AccessTools.FieldRef<Pawn_JobTracker, Pawn> pawnRef;
        private static bool pawnRefResolveAttempted;
        private static bool pawnRefResolveFailed;

        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = typeof(Pawn_JobTracker).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
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
            if (tracker == null)
            {
                return null;
            }

            AccessTools.FieldRef<Pawn_JobTracker, Pawn> resolvedRef = ResolvePawnRef();
            if (resolvedRef == null)
            {
                return null;
            }

            try
            {
                return resolvedRef(tracker);
            }
            catch (Exception ex)
            {
                pawnRefResolveFailed = true;
                ABY_LogThrottleUtility.Warning(
                    "dash-jobtracker-pawnref-read-failed",
                    "[Abyssal Protocol] Dash Harmony patch failed to read Pawn_JobTracker.pawn; dash job-freeze guard disabled. " + ex.GetType().Name + ": " + ex.Message,
                    999999);
                return null;
            }
        }

        private static AccessTools.FieldRef<Pawn_JobTracker, Pawn> ResolvePawnRef()
        {
            if (pawnRefResolveAttempted)
            {
                return pawnRefResolveFailed ? null : pawnRef;
            }

            pawnRefResolveAttempted = true;
            try
            {
                pawnRef = AccessTools.FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");
                pawnRefResolveFailed = pawnRef == null;
            }
            catch (Exception ex)
            {
                pawnRef = null;
                pawnRefResolveFailed = true;
                ABY_LogThrottleUtility.Warning(
                    "dash-jobtracker-pawnref-bind-failed",
                    "[Abyssal Protocol] Dash Harmony patch could not bind Pawn_JobTracker.pawn; dash job-freeze guard disabled for this runtime. " + ex.GetType().Name + ": " + ex.Message,
                    999999);
            }

            return pawnRefResolveFailed ? null : pawnRef;
        }
    }
}
