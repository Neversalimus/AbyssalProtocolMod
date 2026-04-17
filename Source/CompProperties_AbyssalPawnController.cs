using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_AbyssalPawnController : CompProperties
    {
        public string roleId = "generic";
        public string forcedWeaponDefName;
        public int minimumShootingSkill = -1;
        public int minimumMeleeSkill = -1;
        public float maintainDistanceBelow = -1f;
        public int retreatSearchRadius = 9;
        public bool preferRangedTargets;
        public bool preferLowHealthTargets;
        public bool preferFarthestTargets;
        public bool holdPositionWhenTargeting;
        public bool useSapperAssaultLord = true;
        public int refreshIntervalTicks = 250;

        public CompProperties_AbyssalPawnController()
        {
            compClass = typeof(CompAbyssalPawnController);
        }
    }
}
