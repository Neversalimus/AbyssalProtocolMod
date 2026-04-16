using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ChainSnag : CompProperties
    {
        public float minRange = 4f;
        public float maxRange = 11f;
        public int cooldownTicks = 330;
        public int cooldownJitterTicks = 70;
        public int scanIntervalTicks = 30;
        public string impactHediffDefName = "ABY_ChainSnared";

        public CompProperties_ChainSnag()
        {
            compClass = typeof(CompChainSnag);
        }
    }
}
