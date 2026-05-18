using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_GateWardenBrace : CompProperties
    {
        public int scanIntervalTicks = 15;
        public float triggerEnemyRange = 6.4f;
        public string braceHediffDefName = "ABY_GateWardenBrace";
        public float braceSeverity = 0.62f;
        public float woundedHealthThreshold = 0.58f;
        public float woundedBraceSeverity = 0.92f;

        public CompProperties_ABY_GateWardenBrace()
        {
            compClass = typeof(CompABY_GateWardenBrace);
        }
    }
}
