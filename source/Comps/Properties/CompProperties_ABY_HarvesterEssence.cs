using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_HarvesterEssence : CompProperties
    {
        public int scanIntervalTicks = 30;
        public float nearbyDeathRadius = 12.9f;
        public float harvestRadius = 1.85f;
        public float hostileInterferenceRadius = 2.1f;
        public int maxEssenceStacks = 5;
        public int stackGainPerDeath = 1;
        public int stackGainPerHarvest = 1;
        public int corpseRecognitionMaxAgeTicks = 1500;
        public int freshCorpseMaxAgeTicks = 2600;
        public int harvestWarmupTicks = 120;
        public float healInjuryAmount = 14f;
        public string essenceHediffDefName = "ABY_HarvesterEssenceSurge";
        public float glowScale = 1.20f;

        public CompProperties_ABY_HarvesterEssence()
        {
            compClass = typeof(CompABY_HarvesterEssence);
        }
    }
}
