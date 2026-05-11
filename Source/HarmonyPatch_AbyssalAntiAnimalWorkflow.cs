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
            MethodInfo[] methods = typeof(DesignationManager).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || method.Name != "AddDesignation")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(Designation))
                {
                    yield return method;
                }
            }
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
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalAntiAnimalJobGate
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = typeof(Pawn_JobTracker).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null)
                {
                    continue;
                }

                if (method.Name != "StartJob" && method.Name != "TryTakeOrderedJob")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(Job))
                {
                    yield return method;
                }
            }
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
    }

    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalAntiTameFactionGate
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo[] methods = typeof(Pawn).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || method.Name != "SetFaction")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(Faction))
                {
                    yield return method;
                }
            }
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
    }
}
