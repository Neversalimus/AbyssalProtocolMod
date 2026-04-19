using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDifficultyUtility
    {
        private const string BossEscalationHediffDefName = "ABY_DifficultyBossEscalation";
        private const string EliteEscalationHediffDefName = "ABY_DifficultyEliteEscalation";
        private const string SupportEscalationHediffDefName = "ABY_DifficultySupportEscalation";
        private const string AssaultEscalationHediffDefName = "ABY_DifficultyAssaultEscalation";

        public sealed class DifficultyProfile
        {
            public ABY_DifficultyPreset Preset;
            public float EncounterBudgetMultiplier;
            public float TrashCountMultiplier;
            public float AssaultCountMultiplier;
            public float EliteCountMultiplier;
            public float SupportCountMultiplier;
            public float BossCountMultiplier;
            public float InstabilityMultiplier;
            public float RitualRiskMultiplier;
            public float RewardMultiplier;
            public int DominionStageBonus;
            public int ThreatTierBonus;
            public float BossHediffSeverity;
            public float EliteHediffSeverity;
            public float SupportHediffSeverity;
            public float AssaultHediffSeverity;
            public string LabelKey;
            public string LabelFallback;
            public string DescKey;
            public string DescFallback;
        }

        private static readonly DifficultyProfile[] Profiles =
        {
            new DifficultyProfile
            {
                Preset = ABY_DifficultyPreset.Normal,
                EncounterBudgetMultiplier = 1.00f,
                TrashCountMultiplier = 1.00f,
                AssaultCountMultiplier = 1.00f,
                EliteCountMultiplier = 1.00f,
                SupportCountMultiplier = 1.00f,
                BossCountMultiplier = 1.00f,
                InstabilityMultiplier = 1.00f,
                RitualRiskMultiplier = 1.00f,
                RewardMultiplier = 1.00f,
                DominionStageBonus = 0,
                ThreatTierBonus = 0,
                BossHediffSeverity = 0f,
                EliteHediffSeverity = 0f,
                SupportHediffSeverity = 0f,
                AssaultHediffSeverity = 0f,
                LabelKey = "ABY_Difficulty_Normal",
                LabelFallback = "Normal",
                DescKey = "ABY_Difficulty_Normal_Desc",
                DescFallback = "Baseline calibrated summon pressure."
            },
            new DifficultyProfile
            {
                Preset = ABY_DifficultyPreset.Severe,
                EncounterBudgetMultiplier = 1.12f,
                TrashCountMultiplier = 1.05f,
                AssaultCountMultiplier = 1.10f,
                EliteCountMultiplier = 1.15f,
                SupportCountMultiplier = 1.15f,
                BossCountMultiplier = 1.00f,
                InstabilityMultiplier = 1.12f,
                RitualRiskMultiplier = 1.05f,
                RewardMultiplier = 1.05f,
                DominionStageBonus = 0,
                ThreatTierBonus = 0,
                BossHediffSeverity = 1f,
                EliteHediffSeverity = 1f,
                SupportHediffSeverity = 1f,
                AssaultHediffSeverity = 1f,
                LabelKey = "ABY_Difficulty_Severe",
                LabelFallback = "Severe",
                DescKey = "ABY_Difficulty_Severe_Desc",
                DescFallback = "Leaner budgets harden into cleaner assault compositions and slightly harsher ritual backlash."
            },
            new DifficultyProfile
            {
                Preset = ABY_DifficultyPreset.Rupture,
                EncounterBudgetMultiplier = 1.25f,
                TrashCountMultiplier = 1.10f,
                AssaultCountMultiplier = 1.18f,
                EliteCountMultiplier = 1.30f,
                SupportCountMultiplier = 1.30f,
                BossCountMultiplier = 1.00f,
                InstabilityMultiplier = 1.25f,
                RitualRiskMultiplier = 1.10f,
                RewardMultiplier = 1.10f,
                DominionStageBonus = 0,
                ThreatTierBonus = 0,
                BossHediffSeverity = 2f,
                EliteHediffSeverity = 2f,
                SupportHediffSeverity = 2f,
                AssaultHediffSeverity = 1f,
                LabelKey = "ABY_Difficulty_Rupture",
                LabelFallback = "Rupture",
                DescKey = "ABY_Difficulty_Rupture_Desc",
                DescFallback = "Escalates elite density, tempo and breach aftermath without unlocking content early."
            },
            new DifficultyProfile
            {
                Preset = ABY_DifficultyPreset.Dominion,
                EncounterBudgetMultiplier = 1.40f,
                TrashCountMultiplier = 1.14f,
                AssaultCountMultiplier = 1.24f,
                EliteCountMultiplier = 1.45f,
                SupportCountMultiplier = 1.45f,
                BossCountMultiplier = 1.00f,
                InstabilityMultiplier = 1.40f,
                RitualRiskMultiplier = 1.16f,
                RewardMultiplier = 1.15f,
                DominionStageBonus = 1,
                ThreatTierBonus = 1,
                BossHediffSeverity = 3f,
                EliteHediffSeverity = 3f,
                SupportHediffSeverity = 3f,
                AssaultHediffSeverity = 2f,
                LabelKey = "ABY_Difficulty_Dominion",
                LabelFallback = "Dominion",
                DescKey = "ABY_Difficulty_Dominion_Desc",
                DescFallback = "Endgame pressure profile. Dominion incidents run one calibration step hotter and support cadres scale harder."
            },
            new DifficultyProfile
            {
                Preset = ABY_DifficultyPreset.FinalGate,
                EncounterBudgetMultiplier = 1.58f,
                TrashCountMultiplier = 1.18f,
                AssaultCountMultiplier = 1.30f,
                EliteCountMultiplier = 1.60f,
                SupportCountMultiplier = 1.60f,
                BossCountMultiplier = 1.00f,
                InstabilityMultiplier = 1.60f,
                RitualRiskMultiplier = 1.22f,
                RewardMultiplier = 1.20f,
                DominionStageBonus = 1,
                ThreatTierBonus = 1,
                BossHediffSeverity = 4f,
                EliteHediffSeverity = 4f,
                SupportHediffSeverity = 4f,
                AssaultHediffSeverity = 3f,
                LabelKey = "ABY_Difficulty_FinalGate",
                LabelFallback = "Final Gate",
                DescKey = "ABY_Difficulty_FinalGate_Desc",
                DescFallback = "Apex crisis calibration. Highest composition pressure, harshest instability, no early progression skips."
            }
        };

        public static ABY_DifficultyPreset CurrentPreset => AbyssalProtocolMod.Settings?.difficultyPreset ?? ABY_DifficultyPreset.Normal;

        public static DifficultyProfile CurrentProfile => GetProfile(CurrentPreset);

        public static DifficultyProfile GetProfile(ABY_DifficultyPreset preset)
        {
            int index = Mathf.Clamp((int)preset, 0, Profiles.Length - 1);
            return Profiles[index];
        }

        public static string GetPresetLabel(ABY_DifficultyPreset preset)
        {
            DifficultyProfile profile = GetProfile(preset);
            return AbyssalSummoningConsoleUtility.TranslateOrFallback(profile.LabelKey, profile.LabelFallback);
        }

        public static string GetCurrentPresetLabel()
        {
            return GetPresetLabel(CurrentPreset);
        }

        public static string GetPresetDescription(ABY_DifficultyPreset preset)
        {
            DifficultyProfile profile = GetProfile(preset);
            return AbyssalSummoningConsoleUtility.TranslateOrFallback(profile.DescKey, profile.DescFallback);
        }

        public static string GetCurrentPresetDescription()
        {
            return GetPresetDescription(CurrentPreset);
        }

        public static string GetTelemetrySummary()
        {
            DifficultyProfile profile = CurrentProfile;
            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_Difficulty_TelemetrySummary",
                "Protocol: {0}  •  Encounter x{1:F2}  •  Instability x{2:F2}  •  Rewards x{3:F2}",
                GetPresetLabel(profile.Preset),
                profile.EncounterBudgetMultiplier,
                profile.InstabilityMultiplier,
                profile.RewardMultiplier);
        }

        public static int ScaleEncounterBudget(int value)
        {
            if (value <= 0)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(value * CurrentProfile.EncounterBudgetMultiplier));
        }

        public static int ScaleThreatTier(int tier)
        {
            return Mathf.Clamp(tier + CurrentProfile.ThreatTierBonus, 0, 6);
        }

        public static int ScaleDominionStageTier(int tier)
        {
            return Mathf.Clamp(tier + CurrentProfile.DominionStageBonus, 0, 6);
        }

        public static float ScaleInstability(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value * CurrentProfile.InstabilityMultiplier;
        }

        public static float ScaleRisk(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(value * CurrentProfile.RitualRiskMultiplier);
        }

        public static int ScaleRewardCount(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(count * CurrentProfile.RewardMultiplier));
        }

        public static int ScaleRewardRoll(int min, int max)
        {
            if (max <= 0)
            {
                return 0;
            }

            int rolled = Rand.RangeInclusive(Mathf.Max(0, min), Mathf.Max(min, max));
            return Mathf.Clamp(ScaleRewardCount(rolled), 0, max * 3);
        }

        public static int ScaleCountByRole(int count, string role, int min = 0, int? max = null)
        {
            if (count <= 0)
            {
                return 0;
            }

            float multiplier = GetRoleCountMultiplier(role);
            int scaled = Mathf.Max(min, Mathf.RoundToInt(count * multiplier));
            if (max.HasValue)
            {
                scaled = Mathf.Clamp(scaled, min, max.Value);
            }

            return scaled;
        }

        public static float GetRoleCountMultiplier(string role)
        {
            DifficultyProfile profile = CurrentProfile;
            switch (NormalizeRole(role))
            {
                case "trash":
                    return profile.TrashCountMultiplier;
                case "support":
                    return profile.SupportCountMultiplier;
                case "elite":
                    return profile.EliteCountMultiplier;
                case "boss":
                case "crisis":
                    return profile.BossCountMultiplier;
                default:
                    return profile.AssaultCountMultiplier;
            }
        }

        public static bool CanSpawnKindNow(PawnKindDef kindDef)
        {
            DefModExtension_AbyssalDifficultyScaling ext = kindDef?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
            if (ext == null)
            {
                return true;
            }

            return CurrentPreset >= ext.difficultyFloor;
        }

        public static void ApplyPawnDifficulty(Pawn pawn, PawnKindDef explicitKindDef = null)
        {
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            PawnKindDef kindDef = explicitKindDef ?? pawn.kindDef;
            DefModExtension_AbyssalDifficultyScaling ext = kindDef?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
            if (ext == null || !ext.applyRoleStatScaling)
            {
                return;
            }

            string role = NormalizeRole(ext.role);
            float severity = GetSeverityForRole(role);
            if (severity <= 0.001f)
            {
                RemoveScalingHediffIfPresent(pawn, BossEscalationHediffDefName);
                RemoveScalingHediffIfPresent(pawn, EliteEscalationHediffDefName);
                RemoveScalingHediffIfPresent(pawn, SupportEscalationHediffDefName);
                RemoveScalingHediffIfPresent(pawn, AssaultEscalationHediffDefName);
                return;
            }

            switch (role)
            {
                case "boss":
                case "crisis":
                    ApplyScalingHediff(pawn, BossEscalationHediffDefName, severity);
                    break;
                case "elite":
                    ApplyScalingHediff(pawn, EliteEscalationHediffDefName, severity);
                    break;
                case "support":
                    ApplyScalingHediff(pawn, SupportEscalationHediffDefName, severity);
                    break;
                default:
                    ApplyScalingHediff(pawn, AssaultEscalationHediffDefName, severity);
                    break;
            }
        }

        private static float GetSeverityForRole(string role)
        {
            DifficultyProfile profile = CurrentProfile;
            switch (NormalizeRole(role))
            {
                case "boss":
                case "crisis":
                    return profile.BossHediffSeverity;
                case "elite":
                    return profile.EliteHediffSeverity;
                case "support":
                    return profile.SupportHediffSeverity;
                default:
                    return profile.AssaultHediffSeverity;
            }
        }

        private static void ApplyScalingHediff(Pawn pawn, string hediffDefName, float severity)
        {
            if (pawn?.health == null)
            {
                return;
            }

            RemoveScalingHediffIfPresent(pawn, BossEscalationHediffDefName, hediffDefName);
            RemoveScalingHediffIfPresent(pawn, EliteEscalationHediffDefName, hediffDefName);
            RemoveScalingHediffIfPresent(pawn, SupportEscalationHediffDefName, hediffDefName);
            RemoveScalingHediffIfPresent(pawn, AssaultEscalationHediffDefName, hediffDefName);

            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (def == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet?.GetFirstHediffOfDef(def);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(def, pawn);
                if (hediff == null)
                {
                    return;
                }

                pawn.health.AddHediff(hediff);
            }

            hediff.Severity = severity;
        }

        private static void RemoveScalingHediffIfPresent(Pawn pawn, string hediffDefName, string exceptDefName = null)
        {
            if (pawn?.health?.hediffSet == null || hediffDefName.NullOrEmpty() || hediffDefName == exceptDefName)
            {
                return;
            }

            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            Hediff existing = def != null ? pawn.health.hediffSet.GetFirstHediffOfDef(def) : null;
            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        private static string NormalizeRole(string role)
        {
            return role.NullOrEmpty() ? "assault" : role.Trim().ToLowerInvariant();
        }
    }
}
