using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_ReactorSaintPhaseController : CompProperties
    {
        public float phase2HealthPct = 0.68f;
        public float phase3HealthPct = 0.34f;
        public string phaseTransitionSoundDefName = "ABY_ReactorSaintCharge";
        public float phaseTransitionFlashScale = 2.6f;

        public float phase1AegisMaxFactor = 1.0f;
        public float phase2AegisMaxFactor = 0.92f;
        public float phase3AegisMaxFactor = 0.82f;
        public float phase1AegisRechargeFactor = 1.0f;
        public float phase2AegisRechargeFactor = 1.08f;
        public float phase3AegisRechargeFactor = 1.18f;
        public float phase1AegisDelayFactor = 1.0f;
        public float phase2AegisDelayFactor = 0.92f;
        public float phase3AegisDelayFactor = 0.84f;

        public float phase1OverheatRadius = 9.4f;
        public float phase2OverheatRadius = 10.6f;
        public float phase3OverheatRadius = 11.8f;
        public float phase1OverheatSeverity = 0.18f;
        public float phase2OverheatSeverity = 0.27f;
        public float phase3OverheatSeverity = 0.36f;
        public int phase1OverheatIntervalTicks = 50;
        public int phase2OverheatIntervalTicks = 42;
        public int phase3OverheatIntervalTicks = 34;

        public float phase1PrimaryCooldownFactor = 1.0f;
        public float phase2PrimaryCooldownFactor = 0.88f;
        public float phase3PrimaryCooldownFactor = 0.76f;
        public float phase1BarrageCooldownFactor = 1.0f;
        public float phase2BarrageCooldownFactor = 0.86f;
        public float phase3BarrageCooldownFactor = 0.72f;
        public float phase1WarmupFactor = 1.0f;
        public float phase2WarmupFactor = 0.93f;
        public float phase3WarmupFactor = 0.86f;
        public float phase1BarrageChanceBonus = 0.0f;
        public float phase2BarrageChanceBonus = 0.10f;
        public float phase3BarrageChanceBonus = 0.18f;
        public int phase1BarrageShotBonus = 0;
        public int phase2BarrageShotBonus = 0;
        public int phase3BarrageShotBonus = 1;

        public CompProperties_ABY_ReactorSaintPhaseController()
        {
            compClass = typeof(CompABY_ReactorSaintPhaseController);
        }
    }
}
