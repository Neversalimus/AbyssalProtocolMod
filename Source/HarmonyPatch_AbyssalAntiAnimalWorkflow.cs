using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalAntiAnimalDesignationGate
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            List<MethodBase> targets = new List<MethodBase>();
            try
            {
                MethodInfo[] methods = typeof(DesignationManager).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (IsKnownAddDesignationMethod(method))
                    {
                        targets.Add(method);
                    }
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "anti-animal-designation-target-scan-failed",
                    "[Abyssal Protocol] Anti-animal designation Harmony target scan failed; designation guard may be disabled. " + ex.GetType().Name + ": " + ex.Message,
                    999999);
            }

            LogTargets("anti-animal-designation-targets", "designation", targets);
            return targets;
        }

        public static bool Prefix(Designation __0)
        {
            if (__0 == null || !ABY_AntiTameUtility.IsAnimalWorkflowDesignationDef(__0.def))
            {
                return true;
            }

            Pawn pawn = __0.target.Thing as Pawn;
            if (!ABY_AntiTameUtility.IsAbyssalPawn(pawn))
            {
                return true;
            }

            Reject(pawn);
            return false;
        }

        private static bool IsKnownAddDesignationMethod(MethodInfo method)
        {
            if (method == null || method.IsGenericMethod || method.Name != "AddDesignation" || method.ReturnType != typeof(void))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length >= 1 && parameters[0].ParameterType == typeof(Designation);
        }

        private static void Reject(Pawn pawn)
        {
            try
            {
                Messages.Message("Abyssal entities cannot be tamed, trained, slaughtered, released or handled as animals.", pawn, MessageTypeDefOf.RejectInput, false);
            }
            catch
            {
            }
        }

        private static void LogTargets(string key, string label, List<MethodBase> targets)
        {
            try
            {
                if (!(AbyssalProtocolMod.Settings?.showHarmonyPatchReportOnLoad ?? true))
                {
                    return;
                }

                ABY_LogThrottleUtility.Message(
                    key,
                    "[Abyssal Protocol] Anti-animal Harmony " + label + " targets: " + FormatTargets(targets),
                    999999);
            }
            catch
            {
            }
        }

        private static string FormatTargets(List<MethodBase> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                return "none";
            }

            List<string> names = new List<string>();
            for (int i = 0; i < targets.Count; i++)
            {
                MethodBase target = targets[i];
                if (target != null)
                {
                    names.Add(target.DeclaringType?.FullName + "." + target.Name + "(" + target.GetParameters().Length + " params)");
                }
            }

            return string.Join(", ", names.ToArray());
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalAntiAnimalJobGate
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            List<MethodBase> targets = new List<MethodBase>();
            try
            {
                MethodInfo[] methods = typeof(Pawn_JobTracker).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (IsKnownJobGateMethod(method))
                    {
                        targets.Add(method);
                    }
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "anti-animal-job-target-scan-failed",
                    "[Abyssal Protocol] Anti-animal job Harmony target scan failed; job guard may be disabled. " + ex.GetType().Name + ": " + ex.Message,
                    999999);
            }

            LogTargets("anti-animal-job-targets", "job", targets);
            return targets;
        }

        public static bool Prefix(Pawn_JobTracker __instance, Job __0)
        {
            if (!ABY_AntiTameUtility.IsAbyssalAnimalWorkflowJob(__0))
            {
                return true;
            }

            Pawn target = ResolveTargetPawn(__0);
            Reject(target);
            return false;
        }

        private static bool IsKnownJobGateMethod(MethodInfo method)
        {
            if (method == null || method.IsGenericMethod)
            {
                return false;
            }

            if (method.Name != "StartJob" && method.Name != "TryTakeOrderedJob")
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length < 1 || parameters[0].ParameterType != typeof(Job))
            {
                return false;
            }

            if (method.Name == "StartJob")
            {
                return method.ReturnType == typeof(void);
            }

            return method.ReturnType == typeof(bool);
        }

        private static Pawn ResolveTargetPawn(Job job)
        {
            if (job == null)
            {
                return null;
            }

            Pawn pawn = job.targetA.Thing as Pawn;
            if (pawn != null)
            {
                return pawn;
            }

            pawn = job.targetB.Thing as Pawn;
            if (pawn != null)
            {
                return pawn;
            }

            return job.targetC.Thing as Pawn;
        }

        private static void Reject(Pawn pawn)
        {
            try
            {
                Messages.Message("Abyssal entities cannot be tamed, trained, slaughtered, released or handled as animals.", pawn, MessageTypeDefOf.RejectInput, false);
            }
            catch
            {
            }
        }

        private static void LogTargets(string key, string label, List<MethodBase> targets)
        {
            try
            {
                if (!(AbyssalProtocolMod.Settings?.showHarmonyPatchReportOnLoad ?? true))
                {
                    return;
                }

                ABY_LogThrottleUtility.Message(
                    key,
                    "[Abyssal Protocol] Anti-animal Harmony " + label + " targets: " + FormatTargets(targets),
                    999999);
            }
            catch
            {
            }
        }

        private static string FormatTargets(List<MethodBase> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                return "none";
            }

            List<string> names = new List<string>();
            for (int i = 0; i < targets.Count; i++)
            {
                MethodBase target = targets[i];
                if (target != null)
                {
                    names.Add(target.DeclaringType?.FullName + "." + target.Name + "(" + target.GetParameters().Length + " params)");
                }
            }

            return string.Join(", ", names.ToArray());
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalAntiTameFactionGate
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            List<MethodBase> targets = new List<MethodBase>();
            try
            {
                MethodInfo[] methods = typeof(Pawn).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (IsKnownSetFactionMethod(method))
                    {
                        targets.Add(method);
                    }
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "anti-animal-faction-target-scan-failed",
                    "[Abyssal Protocol] Anti-animal faction Harmony target scan failed; faction guard may be disabled. " + ex.GetType().Name + ": " + ex.Message,
                    999999);
            }

            LogTargets("anti-animal-faction-targets", "faction", targets);
            return targets;
        }

        public static bool Prefix(Pawn __instance, ref Faction __0)
        {
            if (!ABY_AntiTameUtility.IsAbyssalPawn(__instance) || Faction.OfPlayer == null || __0 != Faction.OfPlayer)
            {
                return true;
            }

            Faction abyssal = ABY_LargeModpackHotfixBUtility.ResolveAbyssalFaction();
            if (abyssal != null)
            {
                __0 = abyssal;
                return true;
            }

            return false;
        }

        private static bool IsKnownSetFactionMethod(MethodInfo method)
        {
            if (method == null || method.IsGenericMethod || method.Name != "SetFaction" || method.ReturnType != typeof(void))
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length >= 1 && parameters[0].ParameterType == typeof(Faction);
        }

        private static void LogTargets(string key, string label, List<MethodBase> targets)
        {
            try
            {
                if (!(AbyssalProtocolMod.Settings?.showHarmonyPatchReportOnLoad ?? true))
                {
                    return;
                }

                ABY_LogThrottleUtility.Message(
                    key,
                    "[Abyssal Protocol] Anti-animal Harmony " + label + " targets: " + FormatTargets(targets),
                    999999);
            }
            catch
            {
            }
        }

        private static string FormatTargets(List<MethodBase> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                return "none";
            }

            List<string> names = new List<string>();
            for (int i = 0; i < targets.Count; i++)
            {
                MethodBase target = targets[i];
                if (target != null)
                {
                    names.Add(target.DeclaringType?.FullName + "." + target.Name + "(" + target.GetParameters().Length + " params)");
                }
            }

            return string.Join(", ", names.ToArray());
        }
    }
}
