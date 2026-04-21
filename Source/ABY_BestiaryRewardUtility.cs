using UnityEngine;

namespace AbyssalProtocol
{
    public static class ABY_BestiaryRewardUtility
    {
        public const int StudiedEntriesPerBonusStep = 3;
        public const int MaxExtractionBonusPercent = 5;

        public static int GetStudiedEntryCount()
        {
            return ABY_BestiaryUtility.GetStudiedEntryCount();
        }

        public static int GetExtractionBonusPercent()
        {
            int steps = Mathf.FloorToInt(GetStudiedEntryCount() / (float)StudiedEntriesPerBonusStep);
            return Mathf.Clamp(steps, 0, MaxExtractionBonusPercent);
        }

        public static float GetExtractionBonusMultiplier()
        {
            return 1f + GetExtractionBonusPercent() / 100f;
        }

        public static int ApplyExtractionBonus(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(count * GetExtractionBonusMultiplier()));
        }

        public static float ApplyExtractionBonus(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value * GetExtractionBonusMultiplier();
        }

        public static int GetEntriesUntilNextBonusStep()
        {
            int studied = GetStudiedEntryCount();
            int currentBonus = GetExtractionBonusPercent();
            if (currentBonus >= MaxExtractionBonusPercent)
            {
                return 0;
            }

            int nextTarget = (currentBonus + 1) * StudiedEntriesPerBonusStep;
            return Mathf.Max(0, nextTarget - studied);
        }

        public static string GetSummaryLine()
        {
            int bonus = GetExtractionBonusPercent();
            int remaining = GetEntriesUntilNextBonusStep();
            if (bonus >= MaxExtractionBonusPercent)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_ExtractionSummary_Max", "Archive extraction bonus: +{0}% (maxed)", bonus);
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_ExtractionSummary_Next", "Archive extraction bonus: +{0}% ({1} studied entries until next step)", bonus, remaining);
        }
    }
}
