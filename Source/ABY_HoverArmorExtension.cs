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

        // Readable above-pawn hover aura. This is intentionally not a spinning horizontal ring:
        // it is a static, pulsing backplate/halo that sits behind the lifted pawn silhouette.
        public float haloScale = 1.08f;
        public float haloPulseScale = 0.060f;
        public float haloAlpha = 0.88f;
        public float haloOffsetZ = 0.42f;
        public float haloAltitudeOffset = 0.120f;
    }
}
