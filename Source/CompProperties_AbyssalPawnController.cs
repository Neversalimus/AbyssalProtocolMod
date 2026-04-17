using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_AbyssalPawnController : CompProperties
    {
        public AbyssalPawnArchetype archetype = AbyssalPawnArchetype.None;
        public string forcedPrimaryDefName;
        public int minMeleeSkill = -1;
        public int minShootingSkill = -1;
        public bool useSapperAssaultLord = true;
        public int spawnGraceTicks = 90;
        public int lordRetryTicks = 120;
        public float preferredMinRange = -1f;
        public int retreatSearchRadius = -1;
        public bool preferRangedTargets;
        public bool preferFarthestTargets;
        public bool holdPositionWhenTargeting;
        public float targetMinRange = -1f;
        public float targetMaxRange = -1f;

        public CompProperties_AbyssalPawnController()
        {
            compClass = typeof(CompAbyssalPawnController);
        }
    }
}
