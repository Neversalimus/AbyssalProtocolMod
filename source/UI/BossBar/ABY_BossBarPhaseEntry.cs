using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_BossBarPhaseEntry
    {
        public int phaseIndex = 1;
        public float triggerHealthPct = 1f;
        public string label;
        public string labelKey;
        public string specialStateTag;

        public string ResolveLabel()
        {
            if (!labelKey.NullOrEmpty())
            {
                return labelKey.Translate();
            }

            if (!label.NullOrEmpty())
            {
                return label;
            }

            return AbyssalBossBarUtility.ResolvePhaseLabel(phaseIndex);
        }
    }
}
