using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_GateWardenShieldBash : CompProperties
    {
        public int scanIntervalTicks = 12;
        public int cooldownTicks = 210;
        public int cooldownJitterTicks = 35;
        public float bashRange = 1.42f;
        public float bashDamage = 11f;
        public float bashArmorPenetration = 0.18f;
        public int staggerTicks = 35;
        public string bashHediffDefName = "ABY_GateWardenBashed";
        public float bashSeverity = 0.52f;
        public string bashSoundDefName = "ABY_SigilChargePulse";
        public float bashFlashScale = 0.78f;

        public CompProperties_ABY_GateWardenShieldBash()
        {
            compClass = typeof(CompABY_GateWardenShieldBash);
        }
    }
}
