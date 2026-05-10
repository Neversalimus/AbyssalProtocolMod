using System;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HoverArmorUtility
    {
        private const string GatebreakerDefName = "ABY_GatebreakerCarapace";
        private const string GravplateDefName = "ABY_AbyssalGravplatePrototype";

        private static readonly ABY_HoverArmorExtension GatebreakerFallback = new ABY_HoverArmorExtension
        {
            draftedOnly = true,
            enableUnderfootFx = true,
            enablePawnBob = true,
            pawnVisualLiftZ = 0.095f,
            pawnBobAmplitudeZ = 0.030f,
            pawnBobPeriodTicks = 112,
            pawnAltitudeLayerOffset = 0.018f,
            ringScale = 0.70f,
            ringPulseScale = 0.050f,
            ringAlpha = 0.54f,
            shadowScale = 0.42f,
            sparkScale = 0.080f
        };

        private static readonly ABY_HoverArmorExtension GravplateFallback = new ABY_HoverArmorExtension
        {
            draftedOnly = true,
            enableUnderfootFx = true,
            enablePawnBob = true,
            pawnVisualLiftZ = 0.120f,
            pawnBobAmplitudeZ = 0.036f,
            pawnBobPeriodTicks = 126,
            pawnAltitudeLayerOffset = 0.020f,
            ringScale = 0.76f,
            ringPulseScale = 0.060f,
            ringAlpha = 0.62f,
            shadowScale = 0.46f,
            sparkScale = 0.090f
        };

        public static bool TryGetActiveHover(Pawn pawn, out ABY_HoverArmorExtension extension)
        {
            extension = null;
            if (!IsPawnEligibleForHover(pawn))
            {
                return false;
            }

            Apparel apparel = FindHoverApparel(pawn, out extension);
            if (apparel == null || extension == null)
            {
                return false;
            }

            if (extension.draftedOnly && !pawn.Drafted)
            {
                return false;
            }

            return true;
        }

        public static bool ShouldApplyWorldDrawOffset(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null || pawn.Dead)
            {
                return false;
            }

            if (Find.CurrentMap != null && Find.CurrentMap != pawn.Map)
            {
                return false;
            }

            return TryGetActiveHover(pawn, out _);
        }

        public static float ComputePawnLiftZ(Pawn pawn, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null)
            {
                return 0f;
            }

            float lift = Math.Max(0f, extension.pawnVisualLiftZ);
            if (extension.enablePawnBob)
            {
                int period = Math.Max(24, extension.pawnBobPeriodTicks);
                int ticks = SafeTicksGame() + pawn.thingIDNumber * 17;
                float phase = (ticks % period) / (float)period;
                lift += (float)Math.Sin(phase * Math.PI * 2.0) * Math.Max(0f, extension.pawnBobAmplitudeZ);
            }

            return lift;
        }

        public static int SafeTicksGame()
        {
            try
            {
                return Find.TickManager?.TicksGame ?? 0;
            }
            catch
            {
                return Environment.TickCount & int.MaxValue;
            }
        }

        private static bool IsPawnEligibleForHover(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && pawn.Map != null
                && !pawn.Dead
                && pawn.apparel != null
                && pawn.apparel.WornApparel != null;
        }

        private static Apparel FindHoverApparel(Pawn pawn, out ABY_HoverArmorExtension extension)
        {
            extension = null;
            if (pawn?.apparel?.WornApparel == null)
            {
                return null;
            }

            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                Apparel apparel = pawn.apparel.WornApparel[i];
                if (apparel?.def == null)
                {
                    continue;
                }

                ABY_HoverArmorExtension xmlExtension = apparel.def.GetModExtension<ABY_HoverArmorExtension>();
                if (xmlExtension != null)
                {
                    extension = xmlExtension;
                    return apparel;
                }

                string defName = apparel.def.defName;
                if (defName == GatebreakerDefName)
                {
                    extension = GatebreakerFallback;
                    return apparel;
                }

                if (defName == GravplateDefName)
                {
                    extension = GravplateFallback;
                    return apparel;
                }
            }

            return null;
        }
    }
}
