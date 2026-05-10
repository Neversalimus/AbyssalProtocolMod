using Verse;

namespace AbyssalProtocol
{
    public class ABY_HoverArmorExtension : DefModExtension
    {
        public bool draftedOnly = true;
        public bool enableUnderfootFx = true;
        public bool enableMovingSparks = true;
        public bool enableFlightRigFx = true;
        public string hoverFxIntensity = "Subtle";
        public string ringTexPath = "Effects/ABY_HoverGravRing";
        public string sparkTexPath = "Effects/ABY_HoverSpark";
        public string flightRigTexPathSouth = "Effects/FlightRig/ABY_FlightRig_South";
        public string flightRigTexPathEast = "Effects/FlightRig/ABY_FlightRig_East";
        public string flightRigTexPathNorth = "Effects/FlightRig/ABY_FlightRig_North";
        public float ringScale = 0.74f;
        public float movingRingScaleBonus = 0.08f;
        public float ringAlpha = 0.34f;
        public float pulseAmplitude = 0.07f;
        public float flightRigScale = 2.85f;
        public float flightRigAlpha = 0.96f;
        public float flightRigPulseScale = 0.055f;
        public float flightRigPulseAlpha = 0.10f;
        public float flightRigBobAmplitude = 0.032f;
        public float flightRigOffsetSouthX = 0f;
        public float flightRigOffsetSouthZ = 0.10f;
        public float flightRigOffsetEastX = -0.10f;
        public float flightRigOffsetEastZ = 0.02f;
        public float flightRigOffsetNorthX = 0f;
        public float flightRigOffsetNorthZ = -0.06f;
        public float draftedMoveSpeedBonus = 0f;
        public int drawPriority = 0;
        public int sparkIntervalTicks = 14;
        public int sparkLifetimeTicks = 18;
        public float sparkScale = 0.18f;
        public float sparkAlpha = 0.58f;
    }
}
