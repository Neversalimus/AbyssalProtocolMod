using Verse;

namespace AbyssalProtocol
{
    public class ABY_HoverArmorExtension : DefModExtension
    {
        public bool draftedOnly = true;
        public bool enableUnderfootFx = true;
        public bool enableMovingSparks = true;
        public bool enableFlightRigFx = false;
        public bool enableVectorThrusterFx = false;
        public bool enableGroundWakeFx = false;
        public bool enableHaloFx = false;

        public string hoverFxIntensity = "Subtle";
        public string ringTexPath = "Effects/ABY_HoverGravRing";
        public string sparkTexPath = "Effects/ABY_HoverSpark";
        public string flightRigTexPathSouth = "Effects/FlightRig/ABY_FlightRig_South";
        public string flightRigTexPathEast = "Effects/FlightRig/ABY_FlightRig_East";
        public string flightRigTexPathWest = "Effects/FlightRig/ABY_FlightRig_West";
        public string flightRigTexPathNorth = "Effects/FlightRig/ABY_FlightRig_North";
        public string vectorThrusterBurstTexPath = "Effects/Hover/ABY_VectorThrusterBurst";
        public string vectorThrusterGlowTexPath = "Effects/Hover/ABY_VectorThrusterGlow";
        public string groundWakeTexPath = "Effects/Hover/ABY_GravWake";
        public string groundDistortionTexPath = "Effects/Hover/ABY_GravDistortion";

        public float ringScale = 0.74f;
        public float movingRingScaleBonus = 0.08f;
        public float ringAlpha = 0.34f;
        public float pulseAmplitude = 0.07f;

        public float pawnLiftZ = 0.18f;
        public float pawnLiftBobAmplitude = 0.028f;

        public float flightRigScale = 2.85f;
        public float flightRigAlpha = 0.96f;
        public float flightRigPulseScale = 0.055f;
        public float flightRigPulseAlpha = 0.10f;
        public float flightRigBobAmplitude = 0.032f;
        public float flightRigGlowAlpha = 0.16f;
        public float flightRigAltitudeOffset = -0.034f;
        public float flightRigOffsetSouthX = 0f;
        public float flightRigOffsetSouthZ = 0.10f;
        public float flightRigOffsetEastX = -0.10f;
        public float flightRigOffsetEastZ = 0.02f;
        public float flightRigOffsetNorthX = 0f;
        public float flightRigOffsetNorthZ = -0.06f;

        public float vectorThrusterBurstScale = 0.18f;
        public float vectorThrusterGlowScale = 0.34f;
        public float vectorThrusterAlpha = 0.72f;
        public float vectorThrusterGlowAlpha = 0.24f;
        public float vectorThrusterPulseScale = 0.045f;
        public float vectorThrusterPulseAlpha = 0.22f;
        public float vectorThrusterMotionAlphaBonus = 0.26f;
        public float vectorThrusterMotionScaleBonus = 0.060f;
        public float vectorThrusterBackOffset = 0.14f;
        public float vectorThrusterSideOffset = 0.145f;
        public float vectorThrusterLowerBackOffset = 0.255f;
        public float vectorThrusterLowerSideOffset = 0.090f;
        public float vectorThrusterShoulderScale = 0.92f;
        public float vectorThrusterHipScale = 1.00f;
        public float vectorThrusterAltitudeOffset = 0.020f;

        public float groundWakeScale = 0.62f;
        public float groundWakeLengthScale = 1.25f;
        public float groundWakeWidthScale = 0.54f;
        public float groundWakeAlpha = 0.30f;
        public float groundWakeMotionAlphaBonus = 0.20f;
        public float groundWakeMotionLengthBonus = 0.26f;
        public float groundWakePulseScale = 0.055f;
        public float groundWakePulseAlpha = 0.10f;
        public float groundWakeBackOffset = 0.30f;
        public float groundWakeSideOffset = 0.075f;
        public float groundWakeAltitudeOffset = 0.026f;

        public float draftedMoveSpeedBonus = 0f;
        public int drawPriority = 0;
        public int sparkIntervalTicks = 14;
        public int sparkLifetimeTicks = 18;
        public float sparkScale = 0.18f;
        public float sparkAlpha = 0.58f;
    }
}
