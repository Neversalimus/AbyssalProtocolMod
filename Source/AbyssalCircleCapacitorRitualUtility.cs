using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalCircleCapacitorRitualUtility
    {
        public sealed class RitualProfile
        {
            public string RitualId;
            public float StartupChargeRequired;
            public float TotalChargeRequired;
            public float ThroughputRequired;
            public float MaxRiskReduction;
        }

        public sealed class CapacitorReadinessReport
        {
            public RitualProfile Profile;
            public float AvailableCharge;
            public float Capacity;
            public float Throughput;
            public float ChargeRate;
            public float GridSmoothing;
            public float EffectiveStartupRequired;
            public float EffectiveTotalRequired;
            public float EffectiveThroughputRequired;

            public bool HasLattice => Capacity > 0.01f;
            public bool StartupSatisfied => HasLattice && AvailableCharge + 0.001f >= EffectiveStartupRequired;
            public bool ReserveSatisfied => HasLattice && AvailableCharge + 0.001f >= EffectiveTotalRequired;
            public bool ThroughputSatisfied => HasLattice && Throughput + 0.001f >= EffectiveThroughputRequired;
            public bool FullySatisfied => StartupSatisfied && ReserveSatisfied && ThroughputSatisfied;
        }

        private static readonly RitualProfile UnstableBreachProfile = new RitualProfile
        {
            RitualId = "unstable_breach",
            StartupChargeRequired = 18f,
            TotalChargeRequired = 34f,
            ThroughputRequired = 16f,
            MaxRiskReduction = 0.05f
        };

        private static readonly RitualProfile ArchonBeastProfile = new RitualProfile
        {
            RitualId = "archon_beast",
            StartupChargeRequired = 42f,
            TotalChargeRequired = 88f,
            ThroughputRequired = 30f,
            MaxRiskReduction = 0.12f
        };

        private static string TranslateOrFallback(string key, string fallback)
        {
            string value = key.Translate();
            return value == key ? fallback : value;
        }

        private static string TranslateOrFallback(string key, string fallbackFormat, params object[] args)
        {
            string template = key.Translate();
            if (template == key)
            {
                return string.Format(fallbackFormat, args);
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        public static RitualProfile GetProfile(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            return ritual == null ? null : GetProfile(ritual.Id);
        }

        public static RitualProfile GetProfile(CompProperties_UseEffectSummonBoss summonProps)
        {
            return summonProps == null ? null : GetProfile(summonProps.ritualId);
        }

        public static RitualProfile GetProfile(string ritualId)
        {
            if (ritualId.NullOrEmpty())
            {
                return null;
            }

            if (string.Equals(ritualId, UnstableBreachProfile.RitualId, StringComparison.OrdinalIgnoreCase))
            {
                return UnstableBreachProfile;
            }

            if (string.Equals(ritualId, ArchonBeastProfile.RitualId, StringComparison.OrdinalIgnoreCase))
            {
                return ArchonBeastProfile;
            }

            return null;
        }

        public static CapacitorReadinessReport CreateReadinessReport(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            return CreateReadinessReport(circle, GetProfile(ritual));
        }

        public static CapacitorReadinessReport CreateReadinessReport(Building_AbyssalSummoningCircle circle, CompProperties_UseEffectSummonBoss summonProps)
        {
            return CreateReadinessReport(circle, GetProfile(summonProps));
        }

        public static CapacitorReadinessReport CreateReadinessReport(Building_AbyssalSummoningCircle circle, RitualProfile profile)
        {
            CapacitorReadinessReport report = new CapacitorReadinessReport
            {
                Profile = profile,
                AvailableCharge = circle?.StoredCapacitorCharge ?? 0f,
                Capacity = circle?.GetCapacitorCapacity() ?? 0f,
                Throughput = circle?.GetCapacitorThroughput() ?? 0f,
                ChargeRate = circle?.GetCapacitorChargeRatePerSecond() ?? 0f,
                GridSmoothing = GetGridSmoothing(circle)
            };

            if (profile == null)
            {
                report.EffectiveStartupRequired = 0f;
                report.EffectiveTotalRequired = 0f;
                report.EffectiveThroughputRequired = 0f;
                return report;
            }

            float smoothing = report.GridSmoothing;
            report.EffectiveStartupRequired = profile.StartupChargeRequired * (1f - smoothing * 0.12f);
            report.EffectiveTotalRequired = Mathf.Max(report.EffectiveStartupRequired, profile.TotalChargeRequired * (1f - smoothing * 0.10f));
            report.EffectiveThroughputRequired = profile.ThroughputRequired * (1f - smoothing * 0.18f);
            return report;
        }

        public static bool CanSupportRitual(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual, out string failReason)
        {
            return CanSupportRitual(circle, GetProfile(ritual), out failReason);
        }

        public static bool CanSupportRitual(Building_AbyssalSummoningCircle circle, CompProperties_UseEffectSummonBoss summonProps, out string failReason)
        {
            return CanSupportRitual(circle, GetProfile(summonProps), out failReason);
        }

        public static bool CanSupportRitual(Building_AbyssalSummoningCircle circle, RitualProfile profile, out string failReason)
        {
            failReason = null;
            if (profile == null)
            {
                return true;
            }

            CapacitorReadinessReport report = CreateReadinessReport(circle, profile);
            if (!report.HasLattice)
            {
                failReason = TranslateOrFallback("ABY_CapacitorFail_NoLattice", "This ritual requires an installed capacitor lattice.");
                return false;
            }

            if (!report.StartupSatisfied)
            {
                failReason = TranslateOrFallback("ABY_CapacitorFail_Startup", "Insufficient startup buffer: {0} / {1}", Mathf.RoundToInt(report.AvailableCharge), Mathf.RoundToInt(report.EffectiveStartupRequired));
                return false;
            }

            if (!report.ReserveSatisfied)
            {
                failReason = TranslateOrFallback("ABY_CapacitorFail_Reserve", "Insufficient charge reserve: {0} / {1}", Mathf.RoundToInt(report.AvailableCharge), Mathf.RoundToInt(report.EffectiveTotalRequired));
                return false;
            }

            if (!report.ThroughputSatisfied)
            {
                failReason = TranslateOrFallback("ABY_CapacitorFail_Throughput", "Insufficient capacitor throughput: {0} / {1}", Mathf.RoundToInt(report.Throughput), Mathf.RoundToInt(report.EffectiveThroughputRequired));
                return false;
            }

            return true;
        }

        public static float GetGridSmoothing(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return 0f;
            }

            float totalTolerance = 0f;
            foreach (AbyssalCircleCapacitorSlot slot in circle.GetCapacitorSlots())
            {
                totalTolerance += slot?.InstalledExtension?.surgeTolerance ?? 0f;
            }

            return Mathf.Clamp01(totalTolerance * 0.5f);
        }

        public static float GetRiskReduction(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            CapacitorReadinessReport report = CreateReadinessReport(circle, ritual);
            if (report.Profile == null || !report.FullySatisfied)
            {
                return 0f;
            }

            float fill = report.Capacity <= 0.01f ? 0f : Mathf.Clamp01(report.AvailableCharge / report.Capacity);
            float throughputHeadroom = report.EffectiveThroughputRequired <= 0.01f
                ? 0f
                : Mathf.Clamp01((report.Throughput - report.EffectiveThroughputRequired) / report.EffectiveThroughputRequired);
            float reduction = report.GridSmoothing * 0.06f + fill * 0.03f + throughputHeadroom * 0.03f;
            return Mathf.Min(report.Profile.MaxRiskReduction, reduction);
        }

        public static float GetSustainDrainPerSecond(CapacitorReadinessReport report)
        {
            if (report?.Profile == null)
            {
                return 0f;
            }

            return Mathf.Max(0f, report.EffectiveTotalRequired - report.EffectiveStartupRequired) / 6f;
        }

        public static string GetSupportStateLabel(CapacitorReadinessReport report)
        {
            if (report == null || report.Profile == null)
            {
                return TranslateOrFallback("ABY_CapacitorSupport_NotRequired", "not required");
            }

            if (!report.HasLattice)
            {
                return TranslateOrFallback("ABY_CapacitorSupport_NoLattice", "no lattice");
            }

            if (!report.StartupSatisfied || !report.ReserveSatisfied)
            {
                return TranslateOrFallback("ABY_CapacitorSupport_Undercharged", "undercharged");
            }

            if (!report.ThroughputSatisfied)
            {
                return TranslateOrFallback("ABY_CapacitorSupport_ThroughputLimited", "throughput-limited");
            }

            return TranslateOrFallback("ABY_CapacitorSupport_Ready", "ready");
        }

        public static string GetReserveReadout(CapacitorReadinessReport report)
        {
            if (report?.Profile == null)
            {
                return TranslateOrFallback("ABY_CapacitorReadout_NotRequired", "not required");
            }

            return TranslateOrFallback(
                "ABY_CapacitorReadout_Reserve",
                "{0} / {1}",
                Mathf.RoundToInt(report.AvailableCharge),
                Mathf.RoundToInt(report.EffectiveTotalRequired));
        }

        public static string GetStartupReadout(CapacitorReadinessReport report)
        {
            if (report?.Profile == null)
            {
                return TranslateOrFallback("ABY_CapacitorReadout_NotRequired", "not required");
            }

            return TranslateOrFallback(
                "ABY_CapacitorReadout_Startup",
                "{0} / {1}",
                Mathf.RoundToInt(report.AvailableCharge),
                Mathf.RoundToInt(report.EffectiveStartupRequired));
        }

        public static string GetThroughputRequirementReadout(CapacitorReadinessReport report)
        {
            if (report?.Profile == null)
            {
                return TranslateOrFallback("ABY_CapacitorReadout_NotRequired", "not required");
            }

            return TranslateOrFallback(
                "ABY_CapacitorReadout_Throughput",
                "{0} / {1}",
                Mathf.RoundToInt(report.Throughput),
                Mathf.RoundToInt(report.EffectiveThroughputRequired));
        }

        public static string GetGridSmoothingReadout(Building_AbyssalSummoningCircle circle)
        {
            return TranslateOrFallback(
                "ABY_CapacitorReadout_GridSmoothing",
                "{0}%",
                Mathf.RoundToInt(GetGridSmoothing(circle) * 100f));
        }

        public static string GetRitualDemandSummary(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            RitualProfile profile = GetProfile(ritual);
            if (profile == null)
            {
                return TranslateOrFallback("ABY_CapacitorDemandSummary_None", "No capacitor gate is defined for this ritual.");
            }

            return TranslateOrFallback(
                "ABY_CapacitorDemandSummary",
                "Startup {0} • reserve {1} • throughput {2}",
                Mathf.RoundToInt(profile.StartupChargeRequired),
                Mathf.RoundToInt(profile.TotalChargeRequired),
                Mathf.RoundToInt(profile.ThroughputRequired));
        }
    }
}
