using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_ThreatDoctrineDef : Def
    {
        public List<string> poolIds = new List<string>();
        public List<string> allowedBossProfileDefNames = new List<string>();
        public string difficultyFloorDefName = AbyssalDifficultyUtility.NormalProfileDefName;
        public int minProgressionStage = 0;
        public int maxProgressionStage = 99;
        public float selectionWeight = 1f;
        public float budgetMultiplier = 1f;
        public int extraContentTier = 0;
        public int recentDoctrineLookback = 2;
        public float recentDoctrinePenalty = 0.60f;
        public bool reduceStackedSniperPressure = true;
        public bool reduceStackedSupportPressure = true;
        public bool reduceStackedLargeWavePressure = true;
        public List<ABY_EncounterTemplateRoleCount> minimumRoleCounts = new List<ABY_EncounterTemplateRoleCount>();
        public List<ABY_EncounterTemplateRoleCount> maximumRoleCounts = new List<ABY_EncounterTemplateRoleCount>();
        public List<ABY_EncounterTemplateRoleWeight> roleWeightMultipliers = new List<ABY_EncounterTemplateRoleWeight>();

        public bool MatchesPool(string poolId)
        {
            if (poolIds == null || poolIds.Count == 0)
            {
                return false;
            }

            string safePoolId = (poolId ?? string.Empty).ToLowerInvariant();
            for (int i = 0; i < poolIds.Count; i++)
            {
                string entry = poolIds[i];
                if (!entry.NullOrEmpty() && entry.ToLowerInvariant() == safePoolId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool AllowsBossProfile(string bossProfileDefName)
        {
            if (allowedBossProfileDefNames == null || allowedBossProfileDefNames.Count == 0)
            {
                return true;
            }

            string safeProfile = (bossProfileDefName ?? string.Empty).ToLowerInvariant();
            for (int i = 0; i < allowedBossProfileDefNames.Count; i++)
            {
                string entry = allowedBossProfileDefNames[i];
                if (!entry.NullOrEmpty() && entry.ToLowerInvariant() == safeProfile)
                {
                    return true;
                }
            }

            return false;
        }

        public float GetRoleWeightMultiplier(string role)
        {
            if (roleWeightMultipliers != null)
            {
                string safeRole = (role ?? string.Empty).ToLowerInvariant();
                for (int i = 0; i < roleWeightMultipliers.Count; i++)
                {
                    ABY_EncounterTemplateRoleWeight entry = roleWeightMultipliers[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    if ((entry.role ?? string.Empty).ToLowerInvariant() == safeRole)
                    {
                        return entry.multiplier <= 0f ? 0.01f : entry.multiplier;
                    }
                }
            }

            return 1f;
        }
    }
}
