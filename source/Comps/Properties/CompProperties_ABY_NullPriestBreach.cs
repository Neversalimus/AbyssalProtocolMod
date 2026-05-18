using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_NullPriestBreach : CompProperties
    {
        public int warmupTicks = 150;
        public int breachIntervalTicks = 420;
        public int breachIntervalVariance = 90;
        public float minTargetRange = 5.5f;
        public float maxTargetRange = 20.5f;
        public int maxActiveManifestations = 2;
        public int maxLifetimeSummons = 4;
        public float seamBreachChance = 0.58f;
        public float emberHoundChance = 0.22f;
        public int manifestationWarmupMinTicks = 95;
        public int manifestationWarmupMaxTicks = 145;
        public float pulseRadius = 2.7f;
        public float pulseSeverity = 0.34f;
        public float pulseVisualScale = 1.45f;
        public string pulseSoundDefName = "ABY_SigilChargePulse";

        public CompProperties_ABY_NullPriestBreach()
        {
            compClass = typeof(CompABY_NullPriestBreach);
        }
    }
}
