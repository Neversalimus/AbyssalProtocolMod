using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    /// <summary>
    /// Prevents pocket-map damage from cascading into red errors when vanilla impact-sound code asks an
    /// unparented/transitioning map or held thing for PositionHeld during melee/projectile damage.
    /// Damage has already been applied by this point; only the optional impact sound is suppressed.
    /// </summary>
    [HarmonyPatch(typeof(ImpactSoundUtility), nameof(ImpactSoundUtility.PlayImpactSound), new Type[] { typeof(Thing), typeof(ImpactSoundTypeDef), typeof(Map) })]
    public static class HarmonyPatch_ABY_DominionImpactSoundGuard
    {
        public static Exception Finalizer(Exception __exception, Thing hitThing, Map map)
        {
            if (__exception == null)
            {
                return null;
            }

            if (!(__exception is NullReferenceException))
            {
                return __exception;
            }

            if (IsAbyssalOrDominionContext(hitThing, map))
            {
                ABY_LogThrottleUtility.Message(
                    "aby-impact-sound-suppressed",
                    "[Abyssal Protocol] Suppressed a vanilla impact-sound null reference in an Abyssal/Dominion combat context.",
                    5000);
                return null;
            }

            return __exception;
        }

        private static bool IsAbyssalOrDominionContext(Thing hitThing, Map map)
        {
            if (hitThing is Pawn pawn && ABY_FactionHostilityUtility.IsAbyssalPawn(pawn))
            {
                return true;
            }

            if (hitThing?.def?.defName?.StartsWith("ABY_", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            if (map == null)
            {
                return false;
            }

            if (ABY_DominionAtmosphereUtility.IsDominionPocketMap(map) || AbyssalDominionSterileMapUtility.IsDominionSliceMap(map))
            {
                return true;
            }

            try
            {
                return map.GetComponent<MapComponent_DominionSliceEncounter>()?.CurrentPhase != MapComponent_DominionSliceEncounter.SlicePhase.Dormant;
            }
            catch
            {
                return false;
            }
        }
    }
}
