using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_HaloStep : CompProperties
    {
        public int scanIntervalTicks = 12;
        public int cooldownTicks = 840;
        public int cooldownVarianceTicks = 120;
        public float triggerEnemyRange = 3.25f;
        public float minStepDistance = 4.6f;
        public float maxStepDistance = 7.8f;
        public float avoidEnemyRadius = 4.2f;
        public float damageThreshold = 18f;
        public int damageWindowTicks = 90;
        public float visualScale = 1.35f;

        public CompProperties_ABY_HaloStep()
        {
            compClass = typeof(CompABY_HaloStep);
        }
    }
}
