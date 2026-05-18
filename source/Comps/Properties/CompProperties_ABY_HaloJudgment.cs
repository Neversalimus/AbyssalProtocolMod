using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_HaloJudgment : CompProperties
    {
        public int initialWarmupTicks = 420;
        public int markCooldownTicks = 1260;
        public int markCooldownVariance = 180;
        public int markDurationTicks = 600;
        public int scanIntervalTicks = 18;
        public float minTargetRange = 7.5f;
        public float maxTargetRange = 24.5f;
        public bool preferRangedTargets = true;
        public string markHediffDefName = "ABY_HaloJudgmentMarked";
        public string soundDefName = "ABY_RuptureVerdict";
        public float applicationVisualScale = 1.35f;
        public int minRefreshTicksRemaining = 180;

        public CompProperties_ABY_HaloJudgment()
        {
            compClass = typeof(CompABY_HaloJudgment);
        }
    }
}
