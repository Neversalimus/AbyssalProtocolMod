using HarmonyLib;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch(typeof(UIRoot_Play), nameof(UIRoot_Play.UIRootOnGUI))]
    public static class Patch_UIRoot_Play_UIRootOnGUI
    {
        public static void Postfix()
        {
            if (Current.ProgramState != ProgramState.Playing || Current.Game == null)
                return;

            AbyssalBossScreenFXGameComponent comp =
                Current.Game.GetComponent<AbyssalBossScreenFXGameComponent>();

            comp?.DrawOverlay();
        }
    }
}
