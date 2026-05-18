using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_ReactorSaintShooter : CompProperties
    {
        public string directProjectileDefName = "ABY_ReactorSaintBolt";
        public string barrageProjectileDefName = "ABY_ReactorSaintBarrageShell";
        public string aimSoundDefName = "ABY_ReactorSaintCharge";
        public string primaryFireSoundDefName = "ABY_ReactorSaintVolleyFire";
        public string barrageFireSoundDefName = "ABY_ReactorSaintBarrageFire";
        public float range = 34.9f;
        public int scanIntervalTicks = 15;
        public int primaryWarmupTicks = 60;
        public int primaryCooldownTicks = 165;
        public int primaryBurstShotCount = 3;
        public int ticksBetweenPrimaryShots = 9;
        public int barrageWarmupTicks = 78;
        public int barrageCooldownTicks = 300;
        public int barrageShotCount = 4;
        public int ticksBetweenBarrageShots = 14;
        public float barrageScatterRadius = 2.6f;
        public float barrageTargetClusterRadius = 2.85f;
        public int barrageClusterThreshold = 2;
        public float barrageRandomChance = 0.24f;
        public float preferredMinRange = 10.5f;
        public int retreatSearchRadius = 11;
        public bool preferFarthestTargets = true;
        public bool preferRangedTargets = true;
        public bool holdPositionWhenTargeting = true;

        public CompProperties_ABY_ReactorSaintShooter()
        {
            compClass = typeof(CompABY_ReactorSaintShooter);
        }
    }
}
