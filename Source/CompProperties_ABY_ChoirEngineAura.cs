using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_ChoirEngineAura : CompProperties
    {
        public int scanIntervalTicks = 30;
        public float allyRadius = 10.8f;
        public float enemyRadius = 8.6f;
        public float allySeverity = 0.36f;
        public float enemySeverity = 0.34f;
        public string allyHediffDefName = "ABY_ChoirEngineUplift";
        public string enemyHediffDefName = "ABY_ChoirSignalNoise";

        public CompProperties_ABY_ChoirEngineAura()
        {
            compClass = typeof(CompABY_ChoirEngineAura);
        }
    }
}
