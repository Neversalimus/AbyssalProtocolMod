using HarmonyLib;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    // Reliable visual-lift hook. RimWorld resolves the pawn's root draw position through
    // Pawn_DrawTracker.DrawPos before rendering body/apparel/head. Offsetting this result moves the
    // whole pawn on screen, instead of only changing render altitude. Keep this separate from
    // RenderPawnAt so the lift survives renderer argument/signature changes between RimWorld builds.
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

    // The renderer prefix keeps only ground/background-safe FX. The actual pawn lift is applied by the
    // DrawPos postfix above, which keeps vanilla body/apparel/head rendering intact.
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
    public static class ABY_HoverArmorDrawPatches_RenderPawnAt
    {
        public static void Prefix(Pawn ___pawn, Vector3 drawLoc)
        {
            if (!ABY_HoverArmorUtility.TryGetActiveHover(___pawn, out ABY_HoverArmorExtension extension))
            {
                return;
            }

            ABY_HoverArmorRenderUtility.DrawBackFlightRigFx(___pawn, drawLoc, extension);

            Vector3 groundLoc = drawLoc;
            groundLoc.z -= ABY_HoverArmorUtility.ComputePawnLiftZ(___pawn, extension);
            ABY_HoverArmorRenderUtility.DrawUnderfootFx(___pawn, groundLoc, extension);
        }

        public static void Postfix(Pawn ___pawn, Vector3 drawLoc)
        {
            if (!ABY_HoverArmorUtility.TryGetActiveHover(___pawn, out ABY_HoverArmorExtension extension))
            {
                return;
            }

            // Draw the ground wake after the pawn as well: pure MoteLow prefix drawing was too easy to
            // lose under terrain/pawn sorting and became invisible in-game. The texture itself stays under
            // the feet visually; this ordering just makes the pressure wake readable.
            Vector3 groundLoc = drawLoc;
            groundLoc.z -= ABY_HoverArmorUtility.ComputePawnLiftZ(___pawn, extension);
            ABY_HoverArmorRenderUtility.DrawGroundWakeFx(___pawn, groundLoc, extension);

            // Vector thrusters are now secondary: visible mainly from the rear and intentionally muted
            // from front/side angles so the hover read comes from the ground wake rather than face-level jets.
            ABY_HoverArmorRenderUtility.DrawVectorThrusterFx(___pawn, drawLoc, extension);
        }
    }
}
