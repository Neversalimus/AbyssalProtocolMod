using HarmonyLib;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class ABY_HoverArmorDrawPatches_RenderPawnAt
    {
        public static void Prefix(Pawn ___pawn, ref Vector3 drawLoc)
        {
            try
            {
                if (ABY_HoverArmorUtility.TryGetPawnVisualOffset(___pawn, drawLoc, out Vector3 offset))
                {
                    drawLoc += offset;
                }
            }
            catch (System.Exception ex)
            {
                ABY_LogThrottleUtility.Warning("hoverArmorRenderOffset", "[Abyssal Protocol] Failed to apply hover armor visual offset: " + ex.Message, 600);
            }
        }
    }
}
