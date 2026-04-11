using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_UseEffectSummonBoss : CompProperties_UseEffect
    {
        public string pawnKindDefName = "SpaceSoldier";
        public string bossLabel = "Archon of Rupture";
        public int spawnPoints = 650;

        public CompProperties_UseEffectSummonBoss()
        {
            compClass = typeof(CompUseEffect_SummonBoss);
        }
    }
}
