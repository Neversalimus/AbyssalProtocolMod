using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_WardenOfAshPulse : CompProperties
    {
        public int warmupTicks = 240;
        public int pulseIntervalTicks = 360;
        public int pulseIntervalVariance = 75;
        public float pulseRadius = 3.6f;
        public int pulseDamage = 14;
        public float pulseArmorPenetration = 0.16f;
        public float igniteChancePerCell = 0.14f;
        public int maxCellsToIgnite = 4;
        public float visualScale = 1.9f;
        public string pulseSoundDefName = "ABY_SigilChargePulse";

        public CompProperties_ABY_WardenOfAshPulse()
        {
            compClass = typeof(CompABY_WardenOfAshPulse);
        }
    }
}
