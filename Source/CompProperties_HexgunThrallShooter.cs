using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_HexgunThrallShooter : CompProperties
    {
        public string projectileDefName = "ABY_HexgunBolt";
        public string aimSoundDefName = "ABY_RiftCarbineCharge";
        public string castSoundDefName = "ABY_RiftCarbineFire";
        public float range = 27.9f;
        public int warmupTicks = 57;
        public int cooldownTicks = 99;
        public int burstShotCount = 3;
        public int ticksBetweenBurstShots = 10;
        public int scanIntervalTicks = 15;

        public CompProperties_HexgunThrallShooter()
        {
            compClass = typeof(CompHexgunThrallShooter);
        }
    }
}
