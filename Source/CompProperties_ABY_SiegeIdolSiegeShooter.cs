using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_SiegeIdolSiegeShooter : CompProperties
    {
        public string projectileDefName = "ABY_SiegeIdolPlasmaBolt";
        public string aimSoundDefName = "ABY_UltraPlasmaCharge";
        public string castSoundDefName = "ABY_UltraPlasmaFire";
        public float range = 31.9f;
        public int warmupTicks = 92;
        public int cooldownTicks = 176;
        public int scanIntervalTicks = 20;
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

        public CompProperties_ABY_SiegeIdolSiegeShooter()
        {
            compClass = typeof(CompABY_SiegeIdolSiegeShooter);
        }
    }
}
