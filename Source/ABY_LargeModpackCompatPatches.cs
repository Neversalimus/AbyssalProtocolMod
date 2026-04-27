using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_LargeModpackCompatPatches
    {
        private static bool installAttempted;

        static ABY_LargeModpackCompatPatches()
        {
            TryInstall();
        }

        public static void TryInstall()
        {
            if (installAttempted)
            {
                return;
            }

            installAttempted = true;

            Type harmonyType = Type.GetType("HarmonyLib.Harmony, 0Harmony");
            Type harmonyMethodType = Type.GetType("HarmonyLib.HarmonyMethod, 0Harmony");
            if (harmonyType == null || harmonyMethodType == null)
            {
                Log.Message("[Abyssal Protocol] Package 13 optional Harmony compat patches skipped: HarmonyLib was not available by reflection.");
                return;
            }

            object harmony;
            try
            {
                harmony = Activator.CreateInstance(harmonyType, "abyssalprotocol.large_modpack_hotfix_b.package13");
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Package 13 optional Harmony compat patches could not create Harmony instance: " + ex.Message);
                return;
            }

            int helixienPatched = TryPatchHelixienGasGeneration(harmony, harmonyType, harmonyMethodType);
            bool thinkNodePatched = TryPatchThinkNodeTraitBehaviors(harmony, harmonyType, harmonyMethodType);

            if (helixienPatched > 0 || thinkNodePatched)
            {
                Log.Message("[Abyssal Protocol] Package 13 optional compat patches active. Helixien gas methods: " + helixienPatched + "; ThinkNode_TraitBehaviors guard: " + thinkNodePatched + ".");
            }
        }

        public static bool HelixienGasSterileMapPrefix(object[] __args)
        {
            Map map = FindMapArgument(__args);
            if (AbyssalDominionSterileMapUtility.ShouldSuppressExternalMapGeneration(map))
            {
                return false;
            }

            return true;
        }

        public static Exception ThinkNodeTraitBehaviorsFinalizer(Exception __exception, object[] __args)
        {
            if (__exception == null)
            {
                return null;
            }

            Pawn pawn = FindPawnArgument(__args);
            if (ABY_LargeModpackHotfixBUtility.IsAbyssalPawn(pawn))
            {
                Log.Warning("[Abyssal Protocol] Package 13 suppressed ThinkNode_TraitBehaviors exception for abyssal pawn " + SafePawnLabel(pawn) + ": " + __exception.GetType().Name + " - " + __exception.Message);
                return null;
            }

            return __exception;
        }

        private static int TryPatchHelixienGasGeneration(object harmony, Type harmonyType, Type harmonyMethodType)
        {
            MethodInfo prefix = typeof(ABY_LargeModpackCompatPatches).GetMethod(nameof(HelixienGasSterileMapPrefix), BindingFlags.Static | BindingFlags.Public);
            if (prefix == null)
            {
                return 0;
            }

            int patched = 0;
            HashSet<MethodBase> seen = new HashSet<MethodBase>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == null || !AssemblyLooksHelixienRelated(assembly))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }
                catch
                {
                    continue;
                }

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type == null || !TypeLooksHelixienRelated(type))
                    {
                        continue;
                    }

                    MethodInfo[] methods;
                    try
                    {
                        methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    catch
                    {
                        continue;
                    }

                    for (int m = 0; m < methods.Length; m++)
                    {
                        MethodInfo method = methods[m];
                        if (!LooksLikeHelixienMapGenerationMethod(method) || seen.Contains(method))
                        {
                            continue;
                        }

                        if (TryPatch(harmony, harmonyType, harmonyMethodType, method, prefix, null))
                        {
                            seen.Add(method);
                            patched++;
                        }
                    }
                }
            }

            return patched;
        }

        private static bool TryPatchThinkNodeTraitBehaviors(object harmony, Type harmonyType, Type harmonyMethodType)
        {
            Type type = Type.GetType("RimWorld.ThinkNode_TraitBehaviors, Assembly-CSharp");
            if (type == null)
            {
                return false;
            }

            MethodInfo original = type.GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo finalizer = typeof(ABY_LargeModpackCompatPatches).GetMethod(nameof(ThinkNodeTraitBehaviorsFinalizer), BindingFlags.Static | BindingFlags.Public);
            if (original == null || finalizer == null)
            {
                return false;
            }

            return TryPatch(harmony, harmonyType, harmonyMethodType, original, null, finalizer);
        }

        private static bool TryPatch(object harmony, Type harmonyType, Type harmonyMethodType, MethodBase original, MethodInfo prefix, MethodInfo finalizer)
        {
            if (harmony == null || harmonyType == null || harmonyMethodType == null || original == null)
            {
                return false;
            }

            try
            {
                MethodInfo patchMethod = harmonyType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m => m.Name == "Patch" && m.GetParameters().Length >= 2 && m.GetParameters()[0].ParameterType == typeof(MethodBase));
                if (patchMethod == null)
                {
                    return false;
                }

                object prefixHarmonyMethod = prefix != null ? Activator.CreateInstance(harmonyMethodType, prefix) : null;
                object finalizerHarmonyMethod = finalizer != null ? Activator.CreateInstance(harmonyMethodType, finalizer) : null;

                ParameterInfo[] parameters = patchMethod.GetParameters();
                object[] args = new object[parameters.Length];
                args[0] = original;
                for (int i = 1; i < args.Length; i++)
                {
                    string name = parameters[i].Name ?? string.Empty;
                    if (string.Equals(name, "prefix", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = prefixHarmonyMethod;
                    }
                    else if (string.Equals(name, "finalizer", StringComparison.OrdinalIgnoreCase))
                    {
                        args[i] = finalizerHarmonyMethod;
                    }
                    else
                    {
                        args[i] = null;
                    }
                }

                patchMethod.Invoke(harmony, args);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Package 13 optional compat patch failed for " + original.DeclaringType?.FullName + "." + original.Name + ": " + ex.Message);
                return false;
            }
        }

        private static bool AssemblyLooksHelixienRelated(Assembly assembly)
        {
            string name = assembly.GetName().Name ?? string.Empty;
            return name.IndexOf("Helixien", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Gas", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("VFEPower", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("VanillaExpanded", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TypeLooksHelixienRelated(Type type)
        {
            string name = type.FullName ?? type.Name ?? string.Empty;
            return name.IndexOf("Helixien", StringComparison.OrdinalIgnoreCase) >= 0
                || (name.IndexOf("Gas", StringComparison.OrdinalIgnoreCase) >= 0 && name.IndexOf("Deposit", StringComparison.OrdinalIgnoreCase) >= 0)
                || (name.IndexOf("Gas", StringComparison.OrdinalIgnoreCase) >= 0 && name.IndexOf("Gen", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool LooksLikeHelixienMapGenerationMethod(MethodInfo method)
        {
            if (method == null || method.IsAbstract || method.ContainsGenericParameters)
            {
                return false;
            }

            ParameterInfo[] parameters = method.GetParameters();
            bool hasMap = parameters.Any(p => p.ParameterType == typeof(Map));
            if (!hasMap)
            {
                return false;
            }

            string name = method.Name ?? string.Empty;
            string full = (method.DeclaringType?.FullName ?? string.Empty) + "." + name;
            if (full.IndexOf("Helixien", StringComparison.OrdinalIgnoreCase) < 0 && full.IndexOf("Gas", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            return name.IndexOf("MapGenerated", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Generate", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Spawn", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Deposit", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Gas", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Map FindMapArgument(object[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is Map map)
                {
                    return map;
                }
            }

            return null;
        }

        private static Pawn FindPawnArgument(object[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is Pawn pawn)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static string SafePawnLabel(Pawn pawn)
        {
            try
            {
                return pawn?.LabelShortCap ?? pawn?.def?.defName ?? "unknown pawn";
            }
            catch
            {
                return "unknown pawn";
            }
        }
    }
}
