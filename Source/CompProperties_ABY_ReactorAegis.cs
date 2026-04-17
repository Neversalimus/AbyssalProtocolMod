using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_ReactorAegis : CompProperties
    {
        public float maxAegisPoints = 1900f;
        public int rechargeDelayTicks = 360;
        public int rechargeIntervalTicks = 30;
        public float rechargePerInterval = 105f;
        public string breakSoundDefName = "ABY_ReactorSaintImpact";
        public string restoreSoundDefName = "ABY_ReactorSaintCharge";
        public float breakFlashScale = 3.0f;
        public float restoreFlashScale = 2.4f;

        public CompProperties_ABY_ReactorAegis()
        {
            compClass = typeof(CompABY_ReactorAegis);
        }
    }
}
