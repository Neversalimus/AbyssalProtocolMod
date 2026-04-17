using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_UseEffectSummonBoss : CompProperties_UseEffect
    {
        public string ritualId = "archon_beast";
        public string summonMode = "Boss";
        public string pawnKindDefName = "ABY_ArchonBeast";
        public string bossLabel = "Archon Beast";
        public int spawnPoints = 900;
        public int ritualWarmupTicks = 180;

        public int impCount = 3;
        public int impPortalWarmupTicks = 120;
        public int impSpawnIntervalTicks = 120;
        public int impPortalLingerTicks = 2400;
        public int supportImpCount;
        public int supportThrallCount;
        public int supportZealotCount;
        public string completionLetterLabelKey;
        public string completionLetterDescKey;
        public string arrivalManifestationDefName;
        public int arrivalManifestationWarmupTicks;
        public string arrivalSoundDefName = "ABY_ArchonBossArrive";

        public CompProperties_UseEffectSummonBoss()
        {
            compClass = typeof(CompUseEffect_SummonBoss);
        }
    }
}
