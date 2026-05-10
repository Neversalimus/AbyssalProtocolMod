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
            if (___pawn == null)
            {
                return;
            }

            if (!ABY_HoverArmorUtility.TryGetActiveHover(___pawn, out ABY_HoverArmorExtension extension))
            {
                return;
            }

            Vector3 originalDrawLoc = drawLoc;
            ABY_HoverArmorRenderUtility.DrawUnderfootFx(___pawn, originalDrawLoc, extension);

            float visualLiftZ = ABY_HoverArmorUtility.ComputePawnLiftZ(___pawn, extension);
            drawLoc.z += visualLiftZ;
            drawLoc.y += Mathf.Max(0f, extension.pawnAltitudeLayerOffset);
        }
    }
}
