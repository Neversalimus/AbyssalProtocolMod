using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_BossDifficultyProfileDef : Def
    {
        public List<string> ritualIds = new List<string>();
        public List<string> bossPawnKindDefNames = new List<string>();
        public int minProgressionStage = 0;
        public string escortPoolId = string.Empty;
        public int escortBaseContentTier = 1;
        public float fallbackEscortBudget = 0f;
        public float escortBudgetMultiplier = 1f;
        public List<string> preferredDoctrineDefNames = new List<string>();
        public List<string> secondaryDoctrineDefNames = new List<string>();
        public float preferredDoctrineWeightMultiplier = 1.55f;
        public float secondaryDoctrineWeightMultiplier = 1.18f;
        public int bonusCompanionPortalsAtDominion = 0;
        public int bonusCompanionPortalsAtFinalGate = 0;

        public bool MatchesRitualId(string ritualId)
        {
            return ContainsIgnoreCase(ritualIds, ritualId);
        }

        public bool MatchesBossKindDefName(string bossKindDefName)
        {
            return ContainsIgnoreCase(bossPawnKindDefNames, bossKindDefName);
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
