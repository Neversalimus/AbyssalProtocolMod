using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_ChoirEngineRelay : CompProperties
    {
        public int warmupTicks = 210;
        public int pulseIntervalTicks = 410;
        public int pulseIntervalVariance = 70;
        public float pulseRadius = 11.4f;
        public float pulseAllySeverity = 0.82f;
        public float pulseEnemySeverity = 0.62f;
        public int turretSuppressionIntervalTicks = 185;
        public int turretSuppressionVariance = 45;
        public float turretSuppressionRadius = 18f;
        public float turretEmpDamage = 5.2f;
        public float infrastructureEmpDamage = 3.2f;
        public int maxInfrastructureTargets = 4;
        public int maxPulseInfrastructureTargets = 6;
        public float pulseVisualScale = 2.45f;
        public string pulseSoundDefName = "ABY_SigilChargePulse";

        public CompProperties_ABY_ChoirEngineRelay()
        {
            compClass = typeof(CompABY_ChoirEngineRelay);
        }
    }
}
