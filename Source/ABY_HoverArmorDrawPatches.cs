using HarmonyLib;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    // Drafted hover is applied at the pawn renderer entry point instead of generating extra frame textures.
    // The prefix draws the back rig/underfoot FX first, then lifts the pawn draw location so vanilla body,
    // apparel, headgear and weapons still render normally over the rig.
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class ABY_HoverArmorDrawPatches_RenderPawnAt
    {
        public static void Prefix(Pawn ___pawn, ref Vector3 drawLoc)
        {
            if (!ABY_HoverArmorUtility.TryGetActiveHover(___pawn, out ABY_HoverArmorExtension extension))
            {
                return;
            }

            Vector3 groundLoc = drawLoc;
            float lift = ABY_HoverArmorUtility.ComputePawnLiftZ(___pawn, extension);
            Vector3 liftedPawnLoc = drawLoc;
            liftedPawnLoc.z += lift;

            ABY_HoverArmorRenderUtility.DrawBackFlightRigFx(___pawn, liftedPawnLoc, extension);
            ABY_HoverArmorRenderUtility.DrawUnderfootFx(___pawn, groundLoc, extension);

            drawLoc = liftedPawnLoc;
        }
    }
}
