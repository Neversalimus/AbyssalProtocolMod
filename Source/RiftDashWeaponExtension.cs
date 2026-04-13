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
        public string sparkMoteDef = "ABY_Mote_RiftBladeDashSpark";
        public string shardMoteDef = "ABY_Mote_RiftBladeDashShard";
        public string soundDef = "ABY_RiftBladeDash";

        public float entryMoteScale = 0.84f;
        public float exitMoteScale = 0.96f;
        public float trailMoteScale = 0.68f;
        public float sparkMoteScale = 0.46f;
        public float shardMoteScale = 0.54f;

        public int trailSteps = 4;
        public int trailParticleBurst = 2;
        public int endpointParticleBurst = 5;
        public float particleJitter = 0.18f;

        public bool requireLineOfSight = true;
    }
}
