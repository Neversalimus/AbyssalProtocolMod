using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_DifficultyProfileDef : Def
    {
        public string labelKey;
        public string descriptionKey;
        public int order;
        public float encounterBudgetMultiplier = 1f;
        public float instabilityMultiplier = 1f;
        public float residueRewardMultiplier = 1f;
        public float bonusLootMultiplier = 1f;
        public float dominionHostileBudgetMultiplier = 1f;
        public float dominionPortalBudgetMultiplier = 1f;
        public float eliteRoleWeightMultiplier = 1f;
        public float supportRoleWeightMultiplier = 1f;
        public float bossRoleWeightMultiplier = 1f;
        public int extraContentTier;
        public bool lockChangesAfterFirstBoss = true;

        public string ResolveLabel()
        {
            if (!labelKey.NullOrEmpty())
            {
                string translated = labelKey.Translate();
                if (translated != labelKey)
                {
                    return translated;
                }
            }

            if (!label.NullOrEmpty())
            {
                return label;
            }

            return defName;
        }

        public string ResolveDescription()
        {
            if (!descriptionKey.NullOrEmpty())
            {
                string translated = descriptionKey.Translate();
                if (translated != descriptionKey)
                {
                    return translated;
                }
            }

            return description ?? string.Empty;
        }
    }
}
