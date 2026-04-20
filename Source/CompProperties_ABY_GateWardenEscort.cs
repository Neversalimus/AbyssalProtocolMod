using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_GateWardenEscort : CompProperties
    {
        public List<string> anchorDefNames = new List<string>();
        public int scanIntervalTicks = 30;
        public float anchorSearchRadius = 28f;
        public float defendRadius = 11.5f;
        public float leashDistance = 6.4f;
        public float returnRadiusMin = 1.4f;
        public float returnRadiusMax = 4.6f;
        public int interceptJobExpiryTicks = 120;
        public int returnJobExpiryTicks = 90;
        public string rushHediffDefName = "ABY_GateWardenRush";
        public float rushSeverity = 0.55f;

        public CompProperties_ABY_GateWardenEscort()
        {
            compClass = typeof(CompABY_GateWardenEscort);
        }
    }
}
