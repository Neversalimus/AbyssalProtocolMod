using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_LargeModpackCompatPatches
    {
        private static readonly MethodInfo HarmonyPatchMethod;
        private static readonly ConstructorInfo HarmonyMethodCtor;
        private static readonly object HarmonyInstance;

        static ABY_LargeModpackCompatPatches()
        {
            try
            {
                Type harmonyType = ResolveType("HarmonyLib.Harmony");
                Type harmonyMethodType = ResolveType("HarmonyLib.HarmonyMethod");
                if (harmonyType == null || harmonyMethodType == null)
                {
                    return;
                }

                HarmonyInstance = Activator.CreateInstance(harmonyType, "AbyssalProtocol.large_modpack_hotfixes");
                HarmonyMethodCtor = harmonyMethodType.GetConstructor(new[] { typeof(MethodInfo) });
                HarmonyPatchMethod = harmonyType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "Patch" && m.GetParameters().Length == 5)
                    ?? harmonyType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                        .FirstOrDefault(m => m.Name == "Patch" && m.GetParameters().Length == 6);

                if (HarmonyInstance == null || HarmonyMethodCtor == null || HarmonyPatchMethod == null)
                {
                    return;
                }

                TryPatchFinalizer(typeof(ThinkNode_TraitBehaviors), "TryIssueJobPackage", nameof(ThinkNodeTraitBehaviorsFinalizer));
                TryPatchFinalizer(typeof(ThinkNode_ConditionalNeedPercentageAbove), "TryIssueJobPackage", nameof(ThinkNodeNeedPercentageFinalizer));
                TryPatchPostfix(typeof(JobGiver_AIGotoNearestHostile), "TryGiveJob", nameof(AIGotoNearestHostilePostfix));
                TryPatchPrefix("VHelixienGasE.HelixienGasHandler", "MapGenerated", nameof(HelixienGasMapGeneratedPrefix));
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("compat-init", "[Abyssal Protocol] Large modpack compat patches could not initialize: " + ex.GetType().Name + ": " + ex.Message, 5000);
            }
        }

        public static Exception ThinkNodeTraitBehaviorsFinalizer(Exception __exception, object[] __args, ref ThinkResult __result)
        {
            return SuppressThinkNodeExceptionForAbyssalPawn(__exception, __args, ref __result, "ThinkNode_TraitBehaviors");
        }

        public static Exception ThinkNodeNeedPercentageFinalizer(Exception __exception, object[] __args, ref ThinkResult __result)
        {
            return SuppressThinkNodeExceptionForAbyssalPawn(__exception, __args, ref __result, "ThinkNode_ConditionalNeedPercentageAbove");
        }

        public static void AIGotoNearestHostilePostfix(Pawn pawn, ref Job __result)
        {
            ABY_AbyssalJobLoopGuardUtility.StabilizeAIGotoNearestHostileResult(pawn, ref __result);
        }

        public static bool HelixienGasMapGeneratedPrefix(object __instance)
        {
            Map map = ResolveMapFromComponent(__instance);
            if (AbyssalDominionSterileMapUtility.ShouldSkipExternalMapGeneratedDepositLogic(map))
            {
                map.GetComponent<MapComponent_ABY_SterileAbyssalMap>()?.MarkSterileDominionPocket();
                ABY_LogThrottleUtility.Message("helixien-skip-dominion", "[Abyssal Protocol] Skipped Helixien Gas deposit generation on a sterile dominion slice map.", 5000);
                return false;
            }

            return true;
        }

        private static Exception SuppressThinkNodeExceptionForAbyssalPawn(Exception exception, object[] args, ref ThinkResult result, string source)
        {
            if (exception == null)
            {
                return null;
            }

            Pawn pawn = args != null && args.Length > 0 ? args[0] as Pawn : null;
            if (!ABY_AntiTameUtility.IsAbyssalPawn(pawn))
            {
                return exception;
            }

            result = ThinkResult.NoJob;
            string pawnKey = pawn != null ? pawn.thingIDNumber.ToString() : "unknown";
            ABY_LogThrottleUtility.Warning(
                "thinknode-suppressed-" + source + "-" + pawnKey,
                "[Abyssal Protocol] Suppressed " + source + " exception for abyssal pawn " + (pawn?.LabelShortCap ?? "unknown") + ": " + exception.GetType().Name + " - " + exception.Message,
                5000);
            return null;
        }

        private static void TryPatchFinalizer(Type targetType, string methodName, string finalizerName)
        {
            MethodInfo original = targetType?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo finalizer = typeof(ABY_LargeModpackCompatPatches).GetMethod(finalizerName, BindingFlags.Static | BindingFlags.Public);
            if (original == null || finalizer == null)
            {
                return;
            }

            InvokeHarmonyPatch(original, null, null, finalizer);
        }

        private static void TryPatchPostfix(Type targetType, string methodName, string postfixName)
        {
            MethodInfo original = targetType?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo postfix = typeof(ABY_LargeModpackCompatPatches).GetMethod(postfixName, BindingFlags.Static | BindingFlags.Public);
            if (original == null || postfix == null)
            {
                return;
            }

            InvokeHarmonyPatch(original, null, postfix, null);
        }

        private static void TryPatchPrefix(string targetTypeName, string methodName, string prefixName)
        {
            Type targetType = ResolveType(targetTypeName);
            MethodInfo original = targetType?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo prefix = typeof(ABY_LargeModpackCompatPatches).GetMethod(prefixName, BindingFlags.Static | BindingFlags.Public);
            if (original == null || prefix == null)
            {
                return;
            }

            InvokeHarmonyPatch(original, prefix, null, null);
        }

        private static void InvokeHarmonyPatch(MethodInfo original, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer)
        {
            object prefixHm = prefix != null ? HarmonyMethodCtor.Invoke(new object[] { prefix }) : null;
            object postfixHm = postfix != null ? HarmonyMethodCtor.Invoke(new object[] { postfix }) : null;
            object finalizerHm = finalizer != null ? HarmonyMethodCtor.Invoke(new object[] { finalizer }) : null;
            ParameterInfo[] parameters = HarmonyPatchMethod.GetParameters();
            if (parameters.Length == 6)
            {
                HarmonyPatchMethod.Invoke(HarmonyInstance, new object[] { original, prefixHm, postfixHm, null, finalizerHm, null });
                return;
            }

            HarmonyPatchMethod.Invoke(HarmonyInstance, new object[] { original, prefixHm, postfixHm, null, finalizerHm });
        }

        private static Type ResolveType(string fullName)
        {
            if (fullName.NullOrEmpty())
            {
                return null;
            }

            Type direct = Type.GetType(fullName + ", 0Harmony") ?? Type.GetType(fullName);
            if (direct != null)
            {
                return direct;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type = assemblies[i].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Map ResolveMapFromComponent(object component)
        {
            if (component == null)
            {
                return null;
            }

            Type type = component.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField("map", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.GetValue(component) is Map map)
                {
                    return map;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
