using Verse;

namespace AbyssalProtocol
{
    public class RiftDashWeaponExtension : DefModExtension
    {
        public float maxRange = 10f;
        public int cooldownTicks = 240;

        public string entryMoteDef = "ABY_Mote_RiftBladeDashEntry";
        public string exitMoteDef = "ABY_Mote_RiftBladeDashExit";
        public string trailMoteDef = "ABY_Mote_RiftBladeDashTrail";
        public string soundDef = "ABY_RiftBladeDash";

        public float entryMoteScale = 0.92f;
        public float exitMoteScale = 1.02f;
        public float trailMoteScale = 0.78f;
        public int trailSteps = 5;
        public bool requireLineOfSight = true;
    }
}
