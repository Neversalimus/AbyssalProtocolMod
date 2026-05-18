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
        public int dashDurationTicks = 12;
        public float dashMoteScale = 0.9f;
        public string dashMoteDefName = ABY_AbyssalDashRuntime.DefaultTrailMoteDefName;
        public string dashSoundDefName = ABY_AbyssalDashRuntime.DefaultDashSoundDefName;
        public string impactHediffDefName = "ABY_ChainSnared";

        public CompProperties_ChainSnag()
        {
            compClass = typeof(CompChainSnag);
        }
    }
}
