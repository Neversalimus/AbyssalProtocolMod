using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_HoverArmorExtension : DefModExtension
    {
        public bool draftedOnly = true;
        public bool enableUnderfootFx = true;
        public bool enablePawnBob = true;

        // World-space visual offset. z is intentionally used for visible screen lift in RimWorld's top-down view.
        public float pawnVisualLiftZ = 0.105f;
        public float pawnBobAmplitudeZ = 0.032f;
        public int pawnBobPeriodTicks = 116;
        public float pawnAltitudeLayerOffset = 0.018f;

        public float ringScale = 0.72f;
        public float ringPulseScale = 0.055f;
        public float ringAlpha = 0.58f;
        public float shadowScale = 0.44f;
        public float sparkScale = 0.085f;
    }
}
