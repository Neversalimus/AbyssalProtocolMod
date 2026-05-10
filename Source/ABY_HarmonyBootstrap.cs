using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_HarmonyBootstrap
    {
        static ABY_HarmonyBootstrap()
        {
            Harmony harmony = new Harmony("neversalimus.abyssalprotocol");
            harmony.PatchAll();
        }
    }
}
