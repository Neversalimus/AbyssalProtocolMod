using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_HaloFracture : CompProperties
    {
        public float radius = 3.1f;
        public int maxTargets = 6;
        public int hediffDurationTicks = 240;
        public string hediffDefName = "ABY_HaloFractureDazzled";
        public string soundDefName = "ABY_SigilChargePulse";
        public float visualScale = 1.25f;

        public CompProperties_ABY_HaloFracture()
        {
            compClass = typeof(CompABY_HaloFracture);
        }
    }
}
