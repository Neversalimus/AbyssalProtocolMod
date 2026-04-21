using UnityEngine;

namespace AbyssalProtocol
{
    public static class ABY_BestiaryRewardUtility
    {
        public const int StudiedEntriesPerRewardStage = 3;
        public const int MaxRewardStages = 5;
        public const float RewardStagePercent = 0.01f;

        public static int GetRewardStage()
        {
            return GetRewardStageForStudiedCount(ABY_BestiaryUtility.GetStudiedEntryCount());
        }

        public static int GetRewardStageForStudiedCount(int studiedCount)
        {
            if (studiedCount <= 0)
            {
                return 0;
            }

            return Mathf.Clamp(studiedCount / StudiedEntriesPerRewardStage, 0, MaxRewardStages);
        }

        public static int GetExtractionBonusPercent()
        {
            return Mathf.RoundToInt(GetRewardStage() * RewardStagePercent * 100f);
        }

        public static float GetExtractionBonusMultiplier()
        {
            return 1f + GetRewardStage() * RewardStagePercent;
        }

        public static int ApplyResidueBonus(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(amount * GetExtractionBonusMultiplier()));
        }

        public static int ApplyCacheYieldBonus(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.RoundToInt(amount * GetExtractionBonusMultiplier()));
        }

        public static float ApplyBonusRollChance(float baseChance)
        {
            if (baseChance <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(baseChance * GetExtractionBonusMultiplier());
        }

        public static int GetNextRewardThreshold()
        {
            int stage = GetRewardStage();
            return stage >= MaxRewardStages ? -1 : (stage + 1) * StudiedEntriesPerRewardStage;
        }

        public static string GetStatusSummaryText()
        {
            int bonusPercent = GetExtractionBonusPercent();
            int studied = ABY_BestiaryUtility.GetStudiedEntryCount();
            int nextThreshold = GetNextRewardThreshold();
            if (nextThreshold < 0)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_BestiaryRewardStatus_Maxed",
                    "Archive extraction bonus: +{0}% (maximum).",
                    bonusPercent);
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_BestiaryRewardStatus_Progress",
                "Archive extraction bonus: +{0}% • next increase at {1}/{2} studied entries.",
                bonusPercent,
                studied,
                nextThreshold);
        }
    }
}
