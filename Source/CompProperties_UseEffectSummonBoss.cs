using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_UseEffectSummonBoss : CompProperties_UseEffect
    {
        public string pawnKindDefName = "ABY_ArchonBeast";
        public string bossLabel = "Archon Beast";
        public int spawnPoints = 900;

        public CompProperties_UseEffectSummonBoss()
        {
            compClass = typeof(CompUseEffect_SummonBoss);
        }
    }
}
