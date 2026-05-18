using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HarmonyPatchReportUtility
    {
        private const string OwnerId = "neversalimus.abyssalprotocol.core";
        private static bool captured;
        private static int ownedMethodCount;
        private static int ownedPrefixCount;
        private static int ownedPostfixCount;
        private static int ownedTranspilerCount;
        private static int ownedFinalizerCount;
        private static string captureError;

        public static bool Captured => captured;
        public static int OwnedMethodCount => ownedMethodCount;
        public static int OwnedPrefixCount => ownedPrefixCount;
        public static int OwnedPostfixCount => ownedPostfixCount;
        public static int OwnedTranspilerCount => ownedTranspilerCount;
        public static int OwnedFinalizerCount => ownedFinalizerCount;
        public static string CaptureError => captureError;

        public static void Capture(Harmony harmony)
        {
            try
            {
                ownedMethodCount = 0;
                ownedPrefixCount = 0;
                ownedPostfixCount = 0;
                ownedTranspilerCount = 0;
                ownedFinalizerCount = 0;
                captureError = null;

                IEnumerable<MethodBase> methods = Harmony.GetAllPatchedMethods();
                if (methods != null)
                {
                    foreach (MethodBase method in methods)
                    {
                        if (method == null)
                        {
                            continue;
                        }

                        Patches patches = Harmony.GetPatchInfo(method);
                        if (patches == null)
                        {
                            continue;
                        }

                        int prefixes = CountOwned(patches.Prefixes);
                        int postfixes = CountOwned(patches.Postfixes);
                        int transpilers = CountOwned(patches.Transpilers);
                        int finalizers = CountOwned(patches.Finalizers);
                        if (prefixes + postfixes + transpilers + finalizers <= 0)
                        {
                            continue;
                        }

                        ownedMethodCount++;
                        ownedPrefixCount += prefixes;
                        ownedPostfixCount += postfixes;
                        ownedTranspilerCount += transpilers;
                        ownedFinalizerCount += finalizers;
                    }
                }

                captured = true;
            }
            catch (Exception ex)
            {
                captured = false;
                captureError = ex.GetType().Name + ": " + ex.Message;
                ABY_LogThrottleUtility.Warning("harmony-report-capture", "[Abyssal Protocol] Harmony patch report capture failed: " + captureError, 5000);
            }
        }

        public static List<string> BuildReportLines()
        {
            List<string> lines = new List<string>();
            lines.Add("Abyssal Protocol diagnostics: " + ABY_StabilityDiagnosticsUtility.PackageTag);
            lines.Add("Harmony bootstrap: " + (ABY_HarmonyBootstrap.BootstrapSucceeded ? "OK" : "FAILED"));
            if (!ABY_HarmonyBootstrap.BootstrapError.NullOrEmpty())
            {
                lines.Add("Harmony bootstrap error: " + ABY_HarmonyBootstrap.BootstrapError);
            }

            if (captured)
            {
                lines.Add("Harmony owned patched methods: " + ownedMethodCount);
                lines.Add("Harmony patches: prefixes=" + ownedPrefixCount + ", postfixes=" + ownedPostfixCount + ", transpilers=" + ownedTranspilerCount + ", finalizers=" + ownedFinalizerCount);
            }
            else if (!captureError.NullOrEmpty())
            {
                lines.Add("Harmony report unavailable: " + captureError);
            }
            else
            {
                lines.Add("Harmony report unavailable: not captured yet");
            }

            return lines;
        }

        public static void LogReport(bool force = false)
        {
            try
            {
                if (!force && !(AbyssalProtocolMod.Settings?.showHarmonyPatchReportOnLoad ?? true))
                {
                    return;
                }

                string report = string.Join(" | ", BuildReportLines().ToArray());
                ABY_LogThrottleUtility.Message("harmony-report-startup", "[Abyssal Protocol] " + report, force ? 1 : 999999);
            }
            catch
            {
            }
        }

        private static int CountOwned(System.Collections.Generic.IEnumerable<Patch> patches)
        {
            if (patches == null)
            {
                return 0;
            }

            int count = 0;
            foreach (Patch patch in patches)
            {
                if (patch != null && string.Equals(patch.owner, OwnerId, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
