using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_HoverArmorExtension : DefModExtension
    {
        public bool draftedOnly = true;
        public bool enableUnderfootFx = true;
        public bool enablePawnBob = true;
        public bool enableHaloFx = false;
        public bool enableFlightRigFx = true;

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

        // Legacy upper aura/backplate. Disabled by default now that the readable drafted-flight cue is
        // the animated back-mounted flight rig. Kept as a fallback/optional accent for future tuning.
        public float haloScale = 1.08f;
        public float haloPulseScale = 0.060f;
        public float haloAlpha = 0.70f;
        public float haloOffsetZ = 0.42f;
        public float haloAltitudeOffset = 0.120f;

        // Animated Gatebreaker-style flight rig: 00-02 are deploy frames, 03-07 are idle hover loop.
        // It is drawn behind the pawn while drafted, so the pawn body remains readable but the silhouette
        // clearly gains a powered anti-grav backpack/thruster assembly.
        public float flightRigScale = 2.85f;
        public float flightRigPulseScale = 0.070f;
        public float flightRigAlpha = 0.98f;
        public float flightRigGlowAlpha = 0.24f;
        public float flightRigOffsetX = 0.00f;
        public float flightRigOffsetZ = 0.20f;
        public float flightRigAltitudeOffset = -0.030f;
        public int flightRigFrameTicks = 8;
    }
}
