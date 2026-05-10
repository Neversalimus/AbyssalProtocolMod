using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HoverArmorUtility
    {
        private const string GatebreakerDefName = "ABY_GatebreakerCarapace";
        private const string GatebreakerHarnessDefName = "ABY_GatebreakerAnchorHarness";
        private const string GravplateDefName = "ABY_AbyssalGravplatePrototype";

        private static readonly FieldInfo PawnDrawerPawnField = typeof(Pawn_DrawTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly ABY_HoverArmorExtension GatebreakerFallback = new ABY_HoverArmorExtension
        {
            draftedOnly = true,
            enableUnderfootFx = true,
            enablePawnBob = true,
            enableHaloFx = false,
            enableFlightRigFx = true,
            pawnVisualLiftZ = 0.28f,
            pawnBobAmplitudeZ = 0.045f,
            pawnBobPeriodTicks = 112,
            pawnAltitudeLayerOffset = 0.018f,
            ringScale = 0.78f,
            ringPulseScale = 0.065f,
            ringAlpha = 0.72f,
            shadowScale = 0.52f,
            sparkScale = 0.105f,
            flightRigScale = 2.85f,
            flightRigPulseScale = 0.070f,
            flightRigAlpha = 0.98f,
            flightRigGlowAlpha = 0.24f,
            flightRigOffsetZ = 0.20f,
            flightRigFrameTicks = 8
        };

        private static readonly ABY_HoverArmorExtension GravplateFallback = new ABY_HoverArmorExtension
        {
            draftedOnly = true,
            enableUnderfootFx = true,
            enablePawnBob = true,
            enableHaloFx = false,
            enableFlightRigFx = true,
            pawnVisualLiftZ = 0.32f,
            pawnBobAmplitudeZ = 0.052f,
            pawnBobPeriodTicks = 126,
            pawnAltitudeLayerOffset = 0.020f,
            ringScale = 0.84f,
            ringPulseScale = 0.075f,
            ringAlpha = 0.78f,
            shadowScale = 0.56f,
            sparkScale = 0.115f,
            flightRigScale = 3.05f,
            flightRigPulseScale = 0.075f,
            flightRigAlpha = 0.98f,
            flightRigGlowAlpha = 0.26f,
            flightRigOffsetZ = 0.22f,
            flightRigFrameTicks = 8
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

            if (extension.draftedOnly && !IsDrafted(pawn))
            {
                return false;
            }

            return true;
        }

        public static Pawn ResolvePawnFromDrawer(Pawn_DrawTracker drawer)
        {
            if (drawer == null || PawnDrawerPawnField == null)
            {
                return null;
            }

            try
            {
                return PawnDrawerPawnField.GetValue(drawer) as Pawn;
            }
            catch
            {
                return null;
            }
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

        private static bool IsDrafted(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            try
            {
                if (pawn.drafter != null)
                {
                    return pawn.drafter.Drafted;
                }
            }
            catch
            {
            }

            try
            {
                return pawn.Drafted;
            }
            catch
            {
                return false;
            }
        }

        private static Apparel FindHoverApparel(Pawn pawn, out ABY_HoverArmorExtension extension)
        {
            extension = null;
            if (pawn?.apparel?.WornApparel == null)
            {
                return null;
            }

            Apparel fallbackApparel = null;
            ABY_HoverArmorExtension fallbackExtension = null;

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
                if (defName == GravplateDefName)
                {
                    extension = GravplateFallback;
                    return apparel;
                }

                if (defName == GatebreakerDefName)
                {
                    extension = GatebreakerFallback;
                    return apparel;
                }

                // Some Gatebreaker visual loadouts include the anchor harness as the visible over-layer.
                // Treat it as eligible only as a fallback, so the real carapace/XML extension still wins.
                if (defName == GatebreakerHarnessDefName || defName.StartsWith("ABY_Gatebreaker", StringComparison.Ordinal))
                {
                    fallbackApparel = apparel;
                    fallbackExtension = GatebreakerFallback;
                }
            }

            if (fallbackApparel != null)
            {
                extension = fallbackExtension;
                return fallbackApparel;
            }

            return null;
        }
    }
}
