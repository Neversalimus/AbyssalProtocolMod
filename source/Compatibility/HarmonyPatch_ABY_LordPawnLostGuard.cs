using System;
using HarmonyLib;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    /// <summary>
    /// Large-modpack guard for abyssal wave pawns whose Lord state can become partially stale during
    /// Dominion map transfers, Yayo/VEF/vehicle kill stacks, or mass projectile deaths.
    /// </summary>
    [HarmonyPatch(typeof(Lord), nameof(Lord.Notify_PawnLost))]
    public static class HarmonyPatch_ABY_LordPawnLostGuard
    {
        public static Exception Finalizer(Exception __exception, Lord __instance, Pawn pawn)
        {
            if (__exception == null)
            {
                return null;
            }

            if (!ABY_FactionHostilityUtility.IsAbyssalPawn(pawn))
            {
                return __exception;
            }

            try
            {
                if (__instance?.ownedPawns != null)
                {
                    __instance.ownedPawns.Remove(pawn);
                }

                if (pawn?.MapHeld != null)
                {
                    ABY_RuntimeTargetCache.NotifyLikelyStateChanged(pawn.MapHeld);
                }
            }
            catch
            {
                // Best-effort cleanup only. Do not rethrow inside a kill-path finalizer.
            }

            ABY_LogThrottleUtility.Message(
                "aby-lord-pawnlost-guard-" + (pawn?.kindDef?.defName ?? "unknown"),
                "[Abyssal Protocol] Suppressed stale Lord.Notify_PawnLost exception for abyssal pawn "
                    + (pawn?.LabelShortCap ?? pawn?.ThingID ?? "unknown pawn") + ": "
                    + __exception.GetType().Name,
                2500);

            return null;
        }
    }
}
