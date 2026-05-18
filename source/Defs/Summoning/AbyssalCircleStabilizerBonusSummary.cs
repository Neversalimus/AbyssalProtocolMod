namespace AbyssalProtocol
{
    public struct AbyssalCircleStabilizerBonusSummary
    {
        public int InstalledCount;
        public int OpposingPairs;
        public int HighestTier;
        public int LowestTier;
        public bool FullRing;
        public bool UniformTier;
        public float ContainmentBonus;
        public float HeatMultiplier;
        public float ContaminationMultiplier;
        public float ContaminationPenaltyMultiplier;
        public float EventChanceMultiplier;
        public float EventSeverityMultiplier;
        public float PurgeEfficiencyMultiplier;
        public float VentEfficiencyMultiplier;

        public bool AnyInstalled => InstalledCount > 0;
    }
}
