using HarmonyLib;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    // Reliable visual-lift hook. RimWorld resolves the pawn's root draw position through
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
                ABY_HoverArmorRenderUtility.NotifyHoverInactive(pawn);
                return;
            }

            __result.z += ABY_HoverArmorUtility.ComputePawnLiftZ(pawn, extension);
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class ABY_HoverArmorDrawPatches_RenderPawnAt
    {
        // Everything here is drawn before the pawn. The animated halo is intentionally a backplate:
        // it should frame the drafted pawn rather than cover the head/face as a foreground mask.
        public static void Prefix(Pawn ___pawn, Vector3 drawLoc)
        {
            if (!ABY_HoverArmorUtility.TryGetActiveHover(___pawn, out ABY_HoverArmorExtension extension))
            {
                ABY_HoverArmorRenderUtility.NotifyHoverInactive(___pawn);
                return;
            }

            Vector3 groundLoc = drawLoc;
            groundLoc.z -= ABY_HoverArmorUtility.ComputePawnLiftZ(___pawn, extension);
            ABY_HoverArmorRenderUtility.DrawUnderfootFx(___pawn, groundLoc, extension);
            ABY_HoverArmorRenderUtility.DrawHaloFx(___pawn, drawLoc, extension);
        }
    }
}
