using Verse;

namespace AbyssalProtocol
{
    public class RiftDashWeaponExtension : DefModExtension
    {
        public float maxRange = 10f;
        public int cooldownTicks = 240;

        public string entryMoteDef = "ABY_Mote_ArchonDashEntry";
        public string exitMoteDef = "ABY_Mote_ArchonDashExit";
        public string trailMoteDef = "ABY_Mote_ArchonDashTrail";

        public float entryMoteScale = 0.95f;
        public float exitMoteScale = 1.10f;
        public float trailMoteScale = 0.75f;
        public int trailSteps = 5;
        public bool requireLineOfSight = true;
    }
}
