using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_HoverArmorExtension : DefModExtension
    {
        public bool draftedOnly = true;
        public bool enableUnderfootFx = true;
        public bool enablePawnBob = true;
        public bool enableHaloFx = true;

        // Visible screen-space lift through Pawn_DrawTracker.DrawPos z-offset.
        public float pawnVisualLiftZ = 0.30f;
        public float pawnBobAmplitudeZ = 0.048f;
        public int pawnBobPeriodTicks = 116;
        public float pawnAltitudeLayerOffset = 0.018f;

        public float ringScale = 0.80f;
        public float ringPulseScale = 0.070f;
        public float ringAlpha = 0.75f;
        public float shadowScale = 0.54f;
        public float sparkScale = 0.110f;

        // Above-pawn halo is intentionally separate from the ground ring: it remains readable even
        // when terrain, pawn shadows, apparel, or other overlays make underfoot FX hard to see.
        public float haloScale = 0.52f;
        public float haloPulseScale = 0.055f;
        public float haloAlpha = 0.78f;
        public float haloOffsetZ = 0.46f;
        public float haloAltitudeOffset = 0.105f;
    }
}
