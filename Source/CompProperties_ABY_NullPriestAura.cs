using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_NullPriestAura : CompProperties
    {
        public int scanIntervalTicks = 30;
        public float allyRadius = 8.8f;
        public float allySeverity = 0.56f;
        public string allyHediffDefName = "ABY_NullPriestWard";

        public CompProperties_ABY_NullPriestAura()
        {
            compClass = typeof(CompABY_NullPriestAura);
        }
    }
}
