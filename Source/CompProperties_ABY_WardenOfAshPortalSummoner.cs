using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_WardenOfAshPortalSummoner : CompProperties
    {
        public int threshold75Count = 3;
        public int threshold40Count = 4;
        public int threshold15Count = 3;
        public float portalMinRadius = 1.35f;
        public float portalMaxRadius = 2.65f;
        public int portalWarmupTicks = 26;
        public int portalSpawnIntervalTicks = 9;
        public int portalLingerTicks = 95;
        public float portalFlashScale = 2.2f;

        public CompProperties_ABY_WardenOfAshPortalSummoner()
        {
            compClass = typeof(CompABY_WardenOfAshPortalSummoner);
        }
    }
}
