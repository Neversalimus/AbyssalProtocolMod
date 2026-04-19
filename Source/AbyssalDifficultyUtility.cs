using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDifficultyUtility
    {
        public const string NormalProfileDefName = "ABY_Difficulty_Normal";
        private static readonly string[] RoleHediffDefs =
        {
            "ABY_DifficultyScaling_Assault",
            "ABY_DifficultyScaling_Support",
            "ABY_DifficultyScaling_Elite",
            "ABY_DifficultyScaling_Boss"
        };

        public static ABY_DifficultyProfileDef GetCurrentProfile()
        {
            string defName = AbyssalProtocolMod.Settings?.difficultyProfileDefName;
            ABY_DifficultyProfileDef profile = !defName.NullOrEmpty()
                ? DefDatabase<ABY_DifficultyProfileDef>.GetNamedSilentFail(defName)
                : null;

            if (profile != null)
            {
                return profile;
            }

            profile = DefDatabase<ABY_DifficultyProfileDef>.GetNamedSilentFail(NormalProfileDefName);
            if (profile != null)
            {
                return profile;
            }

            List<ABY_DifficultyProfileDef> allDefs = DefDatabase<ABY_DifficultyProfileDef>.AllDefsListForReading;
            return allDefs != null && allDefs.Count > 0 ? allDefs[0] : null;
        }

        public static IEnumerable<ABY_DifficultyProfileDef> GetOrderedProfiles()
        {
            List<ABY_DifficultyProfileDef> defs = new List<ABY_DifficultyProfileDef>(DefDatabase<ABY_DifficultyProfileDef>.AllDefsListForReading);
            defs.Sort(delegate (ABY_DifficultyProfileDef a, ABY_DifficultyProfileDef b)
            {
                if (a == null && b == null)
                {
                    return 0;
                }

                if (a == null)
                {
                    return 1;
                }

                if (b == null)
                {
                    return -1;
                }

                int orderCompare = a.order.CompareTo(b.order);
                return orderCompare != 0 ? orderCompare : string.Compare(a.defName, b.defName, StringComparison.Ordinal);
            });
            return defs;
        }

        public static int GetCurrentProfileOrder()
        {
            return GetProfileOrder(GetCurrentProfile());
        }

        public static int GetProfileOrder(ABY_DifficultyProfileDef profile)
        {
            return profile != null ? profile.order : 0;
        }

        public static bool IsProfileAllowedForCurrentSave(ABY_DifficultyProfileDef profile)
        {
            if (profile == null)
            {
                return false;
            }

            if (!AbyssalProtocolMod.Settings.lockDifficultyAfterFirstBoss)
            {
                return true;
            }

            if (!HasRecordedFirstBossKill())
            {
                return true;
            }

            ABY_DifficultyProfileDef current = GetCurrentProfile();
            if (current == null)
            {
                return true;
            }

            return string.Equals(current.defName, profile.defName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool HasRecordedFirstBossKill()
        {
            return Current.Game != null && Current.Game.GetComponent<ABY_FirstBossProgressionGameComponent>()?.FirstBossKillRecorded == true;
        }

        public static bool CanUseByDifficulty(DefModExtension_AbyssalDifficultyScaling extension, ABY_DifficultyProfileDef profile)
        {
            if (extension == null)
            {
                return false;
            }

            return GetProfileOrder(profile ?? GetCurrentProfile()) >= GetProfileOrder(extension.difficultyFloorDefName);
        }

        public static int GetProfileOrder(string defName)
        {
            if (defName.NullOrEmpty())
            {
                return 0;
            }

            ABY_DifficultyProfileDef profile = DefDatabase<ABY_DifficultyProfileDef>.GetNamedSilentFail(defName);
            return profile != null ? profile.order : 0;
        }

        public static float GetEncounterBudgetMultiplier()
        {
            return Math.Max(0.25f, GetCurrentProfile()?.encounterBudgetMultiplier ?? 1f);
        }

        public static float GetInstabilityMultiplier()
        {
            return Math.Max(0.25f, GetCurrentProfile()?.instabilityMultiplier ?? 1f);
        }

        public static float GetResidueRewardMultiplier()
        {
            return Math.Max(0.10f, GetCurrentProfile()?.residueRewardMultiplier ?? 1f);
        }

        public static float GetBonusLootMultiplier()
        {
            return Math.Max(0.10f, GetCurrentProfile()?.bonusLootMultiplier ?? 1f);
        }

        public static float GetDominionHostileBudgetMultiplier()
        {
            return Math.Max(0.25f, GetCurrentProfile()?.dominionHostileBudgetMultiplier ?? 1f);
        }

        public static float GetDominionPortalBudgetMultiplier()
        {
            return Math.Max(0.25f, GetCurrentProfile()?.dominionPortalBudgetMultiplier ?? 1f);
        }

        public static float GetRoleWeightMultiplier(string role)
        {
            ABY_DifficultyProfileDef profile = GetCurrentProfile();
            switch ((role ?? string.Empty).ToLowerInvariant())
            {
                case "support":
                    return Math.Max(0.10f, profile?.supportRoleWeightMultiplier ?? 1f);
                case "elite":
                    return Math.Max(0.10f, profile?.eliteRoleWeightMultiplier ?? 1f);
                case "boss":
                    return Math.Max(0.10f, profile?.bossRoleWeightMultiplier ?? 1f);
                default:
                    return 1f;
            }
        }

        public static int GetAllowedContentTier(int baseTier)
        {
            ABY_DifficultyProfileDef profile = GetCurrentProfile();
            return Math.Max(1, baseTier + Math.Max(0, profile != null ? profile.extraContentTier : 0));
        }

        public static string GetCurrentDifficultyLabel()
        {
            ABY_DifficultyProfileDef profile = GetCurrentProfile();
            return profile != null ? profile.ResolveLabel() : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultyLabel_Default", "Normal");
        }

        public static string GetCurrentDifficultyDescription()
        {
            ABY_DifficultyProfileDef profile = GetCurrentProfile();
            return profile != null ? profile.ResolveDescription() : string.Empty;
        }

        public static List<string> GetDiagnosticsLines()
        {
            ABY_DifficultyProfileDef profile = GetCurrentProfile();
            List<string> lines = new List<string>();
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultyDiagnostics_Profile", "Protocol: {0}", GetCurrentDifficultyLabel()));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultyDiagnostics_Budget", "Encounter budget: x{0}", GetEncounterBudgetMultiplier().ToString("F2")));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultyDiagnostics_Instability", "Instability pressure: x{0}", GetInstabilityMultiplier().ToString("F2")));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultyDiagnostics_Reward", "Residue payout: x{0}", GetResidueRewardMultiplier().ToString("F2")));
            if (profile != null && profile.extraContentTier > 0)
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultyDiagnostics_TierLift", "Escalation lift: +{0} content tier", profile.extraContentTier));
            }

            if (AbyssalProtocolMod.Settings.lockDifficultyAfterFirstBoss)
            {
                lines.Add(HasRecordedFirstBossKill()
                    ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultyDiagnostics_Locked", "Protocol changes are locked after the first boss kill on this save.")
                    : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultyDiagnostics_Armed", "Protocol lock will engage after the first boss kill on this save."));
            }

            return lines;
        }

        public static void ApplyDifficultyScaling(Pawn pawn)
        {
            if (pawn == null || pawn.health == null)
            {
                return;
            }

            DefModExtension_AbyssalDifficultyScaling extension = pawn.kindDef?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
            if (extension == null)
            {
                return;
            }

            ABY_DifficultyProfileDef profile = GetCurrentProfile();
            if (!CanUseByDifficulty(extension, profile))
            {
                return;
            }

            string hediffDefName = GetRoleHediffDefName(extension.role);
            if (hediffDefName.NullOrEmpty())
            {
                return;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (hediffDef == null)
            {
                return;
            }

            for (int i = 0; i < RoleHediffDefs.Length; i++)
            {
                HediffDef existingDef = DefDatabase<HediffDef>.GetNamedSilentFail(RoleHediffDefs[i]);
                if (existingDef == null || existingDef == hediffDef)
                {
                    continue;
                }

                Hediff existingOther = pawn.health.hediffSet.GetFirstHediffOfDef(existingDef);
                if (existingOther != null)
                {
                    pawn.health.RemoveHediff(existingOther);
                }
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(existing);
            }

            existing.Severity = Math.Max(0, GetProfileOrder(profile));
        }

        private static string GetRoleHediffDefName(string role)
        {
            switch ((role ?? string.Empty).ToLowerInvariant())
            {
                case "support":
                    return "ABY_DifficultyScaling_Support";
                case "elite":
                    return "ABY_DifficultyScaling_Elite";
                case "boss":
                    return "ABY_DifficultyScaling_Boss";
                default:
                    return "ABY_DifficultyScaling_Assault";
            }
        }
    }
}
