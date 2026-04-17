using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_AbyssalPawnController : CompProperties
    {
        public AbyssalPawnArchetype archetype = AbyssalPawnArchetype.None;
        public bool autoPrepare = true;
        public bool ensureHostileFaction = true;
        public bool ensureAssaultLord = true;
        public bool sappers = true;
        public int spawnGraceTicks = 90;
        public int lordRetryTicks = 120;
        public string forcedPrimaryDefName;
        public int minimumShootingSkill = -1;
        public int minimumMeleeSkill = -1;

        public CompProperties_AbyssalPawnController()
        {
            compClass = typeof(CompAbyssalPawnController);
        }
    }
}
