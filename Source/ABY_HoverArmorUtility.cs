using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HoverArmorUtility
    {
        private const float MaxWorldDrawLocDrift = 2.25f;

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
                ABY_HoverArmorExtension current = apparel?.def?.GetModExtension<ABY_HoverArmorExtension>();
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
            float phase = ((Find.TickManager?.TicksGame ?? 0) + phaseSeed) / period * Mathf.PI * 2f;
            float bob = Mathf.Sin(phase) * amplitude;

            offset = new Vector3(0f, Mathf.Max(0f, extension.pawnAltitudeLayerOffset), lift + bob);
            return offset.sqrMagnitude > 0.000001f;
        }

        private static bool IsWorldPawnDraw(Pawn pawn, Vector3 incomingDrawLoc)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return false;
            }

            if (Find.CurrentMap != pawn.Map)
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
    }
}
