using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            var harmony = new Harmony("neversalimus.abyssalprotocol");
            harmony.PatchAll();
            Log.Message("[Abyssal Protocol] Loaded source assembly and applied Harmony patches.");
        }
    }
}
