using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_EncounterTemplateRoleCount
    {
        public string role = "assault";
        public int count = 0;
    }

    public sealed class ABY_EncounterTemplateRoleWeight
    {
        public string role = "assault";
        public float multiplier = 1f;
    }

    public sealed class ABY_EncounterTemplateDef : Def
    {
        public string poolId = string.Empty;
        public string difficultyFloorDefName = AbyssalDifficultyUtility.NormalProfileDefName;
        public int minBaseContentTier = 0;
        public int maxBaseContentTier = 99;
        public float selectionWeight = 1f;
        public float budgetMultiplier = 1f;
        public int extraContentTier = 0;
        public int maxSameKindCount = 999;
        public int recentTemplateLookback = 2;
        public float recentTemplatePenalty = 0.55f;
        public int recentKindLookback = 2;
        public float recentKindPenalty = 0.75f;
        public bool reduceStackedSniperPressure = true;
        public bool reduceStackedSupportPressure = true;
        public bool reduceStackedLargeWavePressure = true;
        public List<ABY_EncounterTemplateRoleCount> minimumRoleCounts = new List<ABY_EncounterTemplateRoleCount>();
        public List<ABY_EncounterTemplateRoleCount> maximumRoleCounts = new List<ABY_EncounterTemplateRoleCount>();
        public List<ABY_EncounterTemplateRoleWeight> roleWeightMultipliers = new List<ABY_EncounterTemplateRoleWeight>();

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
