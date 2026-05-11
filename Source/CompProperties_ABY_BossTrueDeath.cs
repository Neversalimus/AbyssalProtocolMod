using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_BossTrueDeath : CompProperties
    {
        public float maxBossHitPoints = 0f;
        public float damageTakenFactor = 1f;
        public float fallbackKillDamage = 35f;
        public float stabilizeInjuryHealAmount = 9999f;
        public float bloodLossClamp = 0.10f;
        public float heatstrokeClamp = 0.12f;
        public float toxicBuildupClamp = 0.10f;
        public bool restoreMissingParts = true;
        public bool removeLethalBadHediffs = true;
        public bool stabilizeOnEveryDamage = true;
        public bool forceLordReengage = true;
        public bool debugLogging = false;

        public CompProperties_ABY_BossTrueDeath()
        {
            compClass = typeof(CompABY_BossTrueDeath);
        }
    }
}
