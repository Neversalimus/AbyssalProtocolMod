using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_RiftSapperShooter : CompProperties
    {
        public string projectileDefName = "ABY_RiftSapperSpike";
        public string aimSoundDefName = "ABY_RiftCarbineCharge";
        public string castSoundDefName = "ABY_AshenScattergunFire";
        public float range = 19.9f;
        public int warmupTicks = 52;
        public int cooldownTicks = 122;
        public int scanIntervalTicks = 13;
        public float preferredMinRange = 6.8f;
        public float targetMinRange = 4.2f;
        public int retreatSearchRadius = 8;
        public float panicMeleeRange = 2.2f;
        public int panicMeleeJobExpiryTicks = 150;
        public bool preferRangedTargets = true;
        public bool preferBuildingTargets = true;
        public bool prioritizeTurrets = true;
        public bool prioritizeDoors = true;
        public bool prioritizeCover = true;
        public bool holdPositionWhenTargeting;

        public CompProperties_ABY_RiftSapperShooter()
        {
            compClass = typeof(CompABY_RiftSapperShooter);
        }
    }
}
