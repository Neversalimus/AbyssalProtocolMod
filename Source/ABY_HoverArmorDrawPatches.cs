using HarmonyLib;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    // This is the reliable visual-lift hook. RimWorld resolves the pawn's root draw position through
    // Pawn_DrawTracker.DrawPos before rendering body/apparel/head. Offsetting this result moves the
    // whole pawn on screen, instead of only changing render altitude.
    [HarmonyPatch(typeof(Pawn_DrawTracker), "get_DrawPos")]
    public static class ABY_HoverArmorDrawPatches_DrawPos
    {
        public static void Postfix(Pawn_DrawTracker __instance, ref Vector3 __result)
        {
            Pawn pawn = ABY_HoverArmorUtility.ResolvePawnFromDrawer(__instance);
            if (!ABY_HoverArmorUtility.TryGetActiveHover(pawn, out ABY_HoverArmorExtension extension))
            {
                return;
            }

            __result.z += ABY_HoverArmorUtility.ComputePawnLiftZ(pawn, extension);
        }
    }

    // Render the ground-locked hover VFX from the same render pass as the pawn. The pawn's drawLoc may
    // already include the DrawPos lift above, so the ring is explicitly moved back to the true ground anchor.
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class ABY_HoverArmorDrawPatches_RenderPawnAt
    {
        public static void Prefix(Pawn ___pawn, Vector3 drawLoc)
        {
            if (!ABY_HoverArmorUtility.TryGetActiveHover(___pawn, out ABY_HoverArmorExtension extension))
            {
                return;
            }

            Vector3 groundLoc = drawLoc;
            groundLoc.z -= ABY_HoverArmorUtility.ComputePawnLiftZ(___pawn, extension);
            ABY_HoverArmorRenderUtility.DrawUnderfootFx(___pawn, groundLoc, extension);
        }
    }
}
