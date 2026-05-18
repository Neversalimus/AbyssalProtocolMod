using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_BossEscalationPackageDef : Def
    {
        public List<string> allowedBossProfileDefNames = new List<string>();
        public string difficultyFloorDefName = AbyssalDifficultyUtility.NormalProfileDefName;
        public string difficultyCeilingDefName = string.Empty;
        public int minProgressionStage = 0;
        public int maxProgressionStage = 99;
        public float selectionWeight = 1f;
        public int recentPackageLookback = 2;
        public float recentPackagePenalty = 0.65f;

        public string escortPoolIdOverride = string.Empty;
        public float escortBudgetMultiplier = 1f;
        public int escortExtraContentTier = 0;

        public List<ABY_EncounterTemplateRoleCount> minimumRoleCounts = new List<ABY_EncounterTemplateRoleCount>();
        public List<ABY_EncounterTemplateRoleCount> maximumRoleCounts = new List<ABY_EncounterTemplateRoleCount>();

        public List<string> preferredDoctrineDefNames = new List<string>();
        public List<string> secondaryDoctrineDefNames = new List<string>();
        public float preferredDoctrineWeightMultiplier = 1.35f;
        public float secondaryDoctrineWeightMultiplier = 1.12f;

        public int extraCompanionPortals = 0;
        public int extraCompanionPortalsAtDominion = 0;
        public int extraCompanionPortalsAtFinalGate = 0;

        public bool spawnEscortNearBossRelease = false;
        public bool scheduleDelayedReinforcement = false;
        public int reinforcementDelayTicks = 210;
        public int reinforcementDelayJitterTicks = 30;
        public float reinforcementBudgetMultiplier = 0.45f;
        public string reinforcementPoolIdOverride = string.Empty;
        public int reinforcementExtraContentTier = 0;

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

        public bool IsAllowedForCurrentState(int currentDifficultyOrder, int progressionStage)
        {
            if (currentDifficultyOrder < AbyssalDifficultyUtility.GetProfileOrder(difficultyFloorDefName))
            {
                return false;
            }

            if (!difficultyCeilingDefName.NullOrEmpty() && currentDifficultyOrder > AbyssalDifficultyUtility.GetProfileOrder(difficultyCeilingDefName))
            {
                return false;
            }

            return progressionStage >= minProgressionStage && progressionStage <= maxProgressionStage;
        }

        public bool IsPreferredDoctrine(string doctrineDefName)
        {
            return ContainsIgnoreCase(preferredDoctrineDefNames, doctrineDefName);
        }

        public bool IsSecondaryDoctrine(string doctrineDefName)
        {
            return ContainsIgnoreCase(secondaryDoctrineDefNames, doctrineDefName);
        }

        private static bool ContainsIgnoreCase(List<string> entries, string value)
        {
            if (entries == null || entries.Count == 0 || value.NullOrEmpty())
            {
                return false;
            }

            string safeValue = value.ToLowerInvariant();
            for (int i = 0; i < entries.Count; i++)
            {
                string entry = entries[i];
                if (!entry.NullOrEmpty() && entry.ToLowerInvariant() == safeValue)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
