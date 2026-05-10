using System;
using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_HarmonyBootstrap
    {
        static ABY_HarmonyBootstrap()
        {
            try
            {
                Harmony harmony = new Harmony("neversalimus.abyssalprotocol.core");
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("harmony-bootstrap", "[Abyssal Protocol] Harmony bootstrap failed: " + ex.GetType().Name + ": " + ex.Message, 5000);
            }
        }
    }
}
