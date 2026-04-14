using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_EmberPounce : CompProperties
    {
        public float minRange = 4f;
        public float maxRange = 14f;
        public int cooldownTicks = 270;
        public int cooldownJitterTicks = 45;
        public int scanIntervalTicks = 30;
        public string impactHediffDefName = "ABY_EmberShock";

        public CompProperties_EmberPounce()
        {
            compClass = typeof(CompEmberPounce);
        }
    }
}
