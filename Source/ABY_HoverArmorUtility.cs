using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HoverArmorUtility
    {
        private const float MaxWorldDrawLocDrift = 2.25f;
        private static readonly HashSet<string> BuiltInHoverArmorDefNames = new HashSet<string>
        {
            "ABY_AbyssalGravplatePrototype",
            "ABY_GatebreakerCarapace"
        };

        private static readonly ABY_HoverArmorExtension FallbackGatebreakerExtension = new ABY_HoverArmorExtension
        {
            draftedOnly = true,
            enableUnderfootFx = true,
            enableMovingSparks = true,
            enablePawnBob = true,
            ringScale = 0.82f,
            movingRingScaleBonus = 0.08f,
            ringAlpha = 0.50f,
            pulseAmplitude = 0.075f,
            sparkIntervalTicks = 12,
            sparkLifetimeTicks = 20,
            sparkScale = 0.18f,
            sparkAlpha = 0.66f,
            pawnVisualLift = 0.085f,
            pawnBobAmplitude = 0.020f,
            pawnBobPeriodTicks = 98,
            pawnAltitudeLayerOffset = 0.006f,
            drawPriority = 10
        };

        private static readonly ABY_HoverArmorExtension FallbackGravplateExtension = new ABY_HoverArmorExtension
        {
            draftedOnly = true,
            enableUnderfootFx = true,
            enableMovingSparks = true,
            enablePawnBob = true,
            ringScale = 0.88f,
            movingRingScaleBonus = 0.10f,
            ringAlpha = 0.56f,
            pulseAmplitude = 0.085f,
            sparkIntervalTicks = 10,
            sparkLifetimeTicks = 22,
            sparkScale = 0.21f,
            sparkAlpha = 0.72f,
            pawnVisualLift = 0.105f,
            pawnBobAmplitude = 0.026f,
            pawnBobPeriodTicks = 92,
            pawnAltitudeLayerOffset = 0.007f,
            drawPriority = 20
        };

        public static bool IsHoverActive(Pawn pawn)
        {
            return TryGetActiveHoverExtension(pawn, out _);
        }

        public static bool TryGetActiveHoverExtension(Pawn pawn, out ABY_HoverArmorExtension extension)
        {
            extension = null;

            if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned || pawn.MapHeld == null)
            {
                return false;
            }

            if (pawn.apparel == null)
            {
                return false;
            }

            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            if (wornApparel == null || wornApparel.Count == 0)
            {
                return false;
            }

            ABY_HoverArmorExtension best = null;
            int bestPriority = int.MinValue;

            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel = wornApparel[i];
                if (apparel == null || apparel.def == null)
                {
                    continue;
                }

                ABY_HoverArmorExtension current = apparel.def.GetModExtension<ABY_HoverArmorExtension>();
                if (current == null)
                {
                    current = FallbackExtensionFor(apparel.def.defName);
                }

                if (current == null)
                {
                    continue;
                }

                if (current.draftedOnly && !pawn.Drafted)
                {
                    continue;
                }

                if (current.drawPriority >= bestPriority)
                {
                    best = current;
                    bestPriority = current.drawPriority;
                }
            }

            extension = best;
            return extension != null;
        }

        public static bool IsKnownHoverArmorDefName(string defName)
        {
            return !string.IsNullOrEmpty(defName) && BuiltInHoverArmorDefNames.Contains(defName);
        }

        public static bool TryGetPawnVisualOffset(Pawn pawn, Vector3 incomingDrawLoc, out Vector3 offset)
        {
            offset = Vector3.zero;

            if (Current.ProgramState != ProgramState.Playing)
            {
                return false;
            }

            if (!TryGetActiveHoverExtension(pawn, out ABY_HoverArmorExtension extension))
            {
                return false;
            }

            if (extension == null || !extension.enablePawnBob)
            {
                return false;
            }

            if (!IsWorldPawnDraw(pawn, incomingDrawLoc))
            {
                return false;
            }

            float lift = Mathf.Max(0f, extension.pawnVisualLift);
            float amplitude = Mathf.Max(0f, extension.pawnBobAmplitude);
            int period = Mathf.Max(30, extension.pawnBobPeriodTicks);
            float phaseSeed = Mathf.Abs((pawn.thingIDNumber * 19) % period);
            float phase = ((SafeTicksGame() + phaseSeed) / period) * Mathf.PI * 2f;
            float bob = Mathf.Sin(phase) * amplitude;

            offset = new Vector3(0f, Mathf.Max(0f, extension.pawnAltitudeLayerOffset), lift + bob);
            return offset.sqrMagnitude > 0.000001f;
        }

        public static bool IsWorldPawnDraw(Pawn pawn, Vector3 incomingDrawLoc)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return false;
            }

            if (Current.ProgramState != ProgramState.Playing)
            {
                return false;
            }

            if (Find.CurrentMap != null && Find.CurrentMap != pawn.Map)
            {
                return false;
            }

            Vector3 actualDrawPos = pawn.DrawPos;
            if (Mathf.Abs(incomingDrawLoc.x - actualDrawPos.x) > MaxWorldDrawLocDrift)
            {
                return false;
            }

            if (Mathf.Abs(incomingDrawLoc.z - actualDrawPos.z) > MaxWorldDrawLocDrift)
            {
                return false;
            }

            return true;
        }

        public static int SafeTicksGame()
        {
            try
            {
                return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static ABY_HoverArmorExtension FallbackExtensionFor(string defName)
        {
            if (defName == "ABY_AbyssalGravplatePrototype")
            {
                return FallbackGravplateExtension;
            }

            if (defName == "ABY_GatebreakerCarapace")
            {
                return FallbackGatebreakerExtension;
            }

            return null;
        }
    }
}
