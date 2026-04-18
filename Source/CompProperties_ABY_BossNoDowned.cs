using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_BossNoDowned : CompProperties
    {
        public float bloodLossClamp = 0.18f;
        public float heatstrokeClamp = 0.22f;
        public float healWorstInjuryAmount = 24f;
        public int maxHealPasses = 4;
        public bool forceLordReengage = true;

        public CompProperties_ABY_BossNoDowned()
        {
            compClass = typeof(CompABY_BossNoDowned);
        }
    }
}
