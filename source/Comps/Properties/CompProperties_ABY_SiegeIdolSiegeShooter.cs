using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_SiegeIdolSiegeShooter : CompProperties
    {
        public string projectileDefName = "ABY_SiegeIdolPlasmaBolt";
        public string aimSoundDefName = "ABY_UltraPlasmaCharge";
        public string castSoundDefName = "ABY_UltraPlasmaFire";
        public string breachProjectileDefName = "ABY_SiegeIdolBreachShell";
        public string breachAimSoundDefName = "ABY_UltraPlasmaCharge";
        public string breachCastSoundDefName = "ABY_UltraPlasmaFire";
        public float range = 31.9f;
        public int warmupTicks = 92;
        public int cooldownTicks = 180;
        public int scanIntervalTicks = 18;
        public int deployTicks = 42;
        public int telegraphIntervalTicks = 12;
        public int anchoredIdleReleaseTicks = 150;
        public float preferredMinRange = 10.8f;
        public float targetMinRange = 8.6f;
        public float anchorMinRange = 12.4f;
        public int retreatSearchRadius = 12;
        public float panicMeleeRange = 4.2f;
        public bool preferRangedTargets = true;
        public bool preferBuildingTargets = true;
        public bool prioritizeTurrets = true;
        public bool prioritizeDoors = true;
        public bool prioritizeCover = true;

        public int breachWarmupTicks = 138;
        public int breachCooldownTicks = 560;
        public int breachTelegraphIntervalTicks = 9;
        public float breachMinRange = 15.2f;
        public bool breachRequiresBuildingTarget = true;

        public CompProperties_ABY_SiegeIdolSiegeShooter()
        {
            compClass = typeof(CompABY_SiegeIdolSiegeShooter);
        }
    }
}
