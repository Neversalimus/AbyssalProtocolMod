using System;
using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_HarmonyBootstrap
    {
        public const string HarmonyId = "neversalimus.abyssalprotocol.core";
        public static Harmony HarmonyInstance { get; private set; }
        public static bool BootstrapSucceeded { get; private set; }
        public static string BootstrapError { get; private set; }

        static ABY_HarmonyBootstrap()
        {
            try
            {
                Harmony harmony = new Harmony(HarmonyId);
                HarmonyInstance = harmony;
                harmony.PatchAll();
                BootstrapSucceeded = true;
                ABY_HarmonyPatchReportUtility.Capture(harmony);
                LongEventHandler.ExecuteWhenFinished(ABY_StabilityDiagnosticsUtility.ReportStartupSnapshot);
            }
            catch (Exception ex)
            {
                BootstrapSucceeded = false;
                BootstrapError = ex.GetType().Name + ": " + ex.Message;
                ABY_LogThrottleUtility.Warning("harmony-bootstrap", "[Abyssal Protocol] Harmony bootstrap failed: " + BootstrapError, 5000);
            }
        }
    }
}
