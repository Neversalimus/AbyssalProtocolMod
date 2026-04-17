using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_AbyssalPawnController : CompProperties
    {
        public AbyssalPawnArchetype archetype = AbyssalPawnArchetype.None;
        public string forcePrimaryWeaponDefName;
        public int minimumShootingSkill;
        public int minimumMeleeSkill;
        public bool autoHostile = true;
        public bool assignAssaultLord = true;
        public bool useSapperLord = true;
        public bool preferRangedTargets;
        public bool preferFarthestTargets;
        public bool holdPositionWhenTargeting;
        public float preferredMinRange;
        public int retreatSearchRadius = 9;
        public int spawnGraceTicks = 90;
        public int lordRetryTicks = 120;

        public CompProperties_AbyssalPawnController()
        {
            compClass = typeof(CompAbyssalPawnController);
        }
    }
}
