using Verse;

namespace AbyssalProtocol
{
    public class ABY_HoverArmorExtension : DefModExtension
    {
        public bool draftedOnly = true;
        public bool enableUnderfootFx = true;
        public bool enableMovingSparks = true;
        public bool enablePawnBob = true;
        public string hoverFxIntensity = "Subtle";
        public string ringTexPath = "Effects/ABY_HoverGravRing";
        public string sparkTexPath = "Effects/ABY_HoverSpark";
        public float ringScale = 0.74f;
        public float movingRingScaleBonus = 0.08f;
        public float ringAlpha = 0.34f;
        public float pulseAmplitude = 0.07f;
        public int drawPriority = 0;
        public int sparkIntervalTicks = 14;
        public int sparkLifetimeTicks = 18;
        public float sparkScale = 0.18f;
        public float sparkAlpha = 0.58f;

        // Harmony-driven visual lift for drafted hover armor.
        // This is intentionally a screen/map-position offset, not pathfinding flight.
        public float pawnVisualLift = 0.055f;
        public float pawnBobAmplitude = 0.018f;
        public int pawnBobPeriodTicks = 92;
        public float pawnAltitudeLayerOffset = 0.006f;
    }
}
