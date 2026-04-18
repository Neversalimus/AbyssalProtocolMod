using Verse;

namespace AbyssalProtocol
{
    public class HediffCompProperties_ABY_HaloJudgmentMark : HediffCompProperties
    {
        public int visualPulseIntervalTicks = 45;
        public float visualScale = 0.95f;

        public HediffCompProperties_ABY_HaloJudgmentMark()
        {
            compClass = typeof(HediffComp_ABY_HaloJudgmentMark);
        }
    }
}
