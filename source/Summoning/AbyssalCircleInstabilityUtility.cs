using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalCircleInstabilityUtility
    {
        public const int HeatTickInterval = 60;
        public const int ContainmentRefreshInterval = 180;
        public const int ContaminationDecayInterval = 600;
        public const int AmbientBleedInterval = 360;
        public const int PurgeCooldownTicks = 3000;
        public const int VentCooldownTicks = 4500;

        public static float CalculateContainment(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null || circle.Destroyed || circle.Map == null)
            {
                return 0f;
            }

            float poweredContribution = circle.IsPoweredForRitual ? 0.08f : 0f;
            float healthContribution = 0.05f;
            if (circle.MaxHitPoints > 0)
            {
                healthContribution = Mathf.Lerp(0.02f, 0.08f, Mathf.Clamp01(circle.HitPoints / (float)circle.MaxHitPoints));
            }

            float attunementContribution = AbyssalForgeProgressUtility.GetSummoningInstabilityReduction(circle.Map);
            AbyssalCircleStabilizerBonusSummary moduleSummary = circle.GetStabilizerBonusSummary();
            float containment = poweredContribution + healthContribution + attunementContribution + moduleSummary.ContainmentBonus;
            return Mathf.Clamp(containment, 0f, 0.50f);
        }

        public static float GetEffectiveContainment(Building_AbyssalSummoningCircle circle, float rawContainment)
        {
            float contamination = circle?.ResidualContamination ?? 0f;
            AbyssalCircleStabilizerBonusSummary moduleSummary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            float penalty = contamination * 0.12f * (moduleSummary.AnyInstalled ? moduleSummary.ContaminationPenaltyMultiplier : 1f);
            if (circle != null && circle.RitualActive)
            {
                penalty += 0.02f;
            }

            return Mathf.Clamp(rawContainment - penalty, 0f, 0.46f);
        }

        public static float GetProjectedHeatGain(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            float baseGain = (ritual?.InstabilityGain ?? 0.12f) * AbyssalDifficultyUtility.GetInstabilityMultiplier();
            float existingPressure = (circle?.InstabilityHeat ?? 0f) * 0.30f;
            float contaminationPressure = (circle?.ResidualContamination ?? 0f) * 0.12f;
            float containmentMitigation = (circle?.ContainmentRating ?? 0f) * 0.55f;
            AbyssalCircleStabilizerBonusSummary moduleSummary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            float gain = baseGain + existingPressure + contaminationPressure - containmentMitigation;
            if (moduleSummary.AnyInstalled)
            {
                gain *= moduleSummary.HeatMultiplier;
            }
            return Mathf.Clamp(gain, 0.04f, 0.52f);
        }

        public static float GetProjectedPostInvokeHeat(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            if (circle == null)
            {
                return 0.05f;
            }

            return Mathf.Clamp01(circle.InstabilityHeat + GetProjectedHeatGain(circle, ritual));
        }

        public static float GetProjectedContaminationGain(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            float baseGain = (ritual?.ContaminationGain ?? 0.05f) * Mathf.Lerp(1f, AbyssalDifficultyUtility.GetInstabilityMultiplier(), 0.65f);
            float heatPressure = Mathf.Max(0f, (circle?.InstabilityHeat ?? 0f) - 0.35f) * 0.10f;
            float gain = baseGain + heatPressure;
            AbyssalCircleStabilizerBonusSummary moduleSummary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            if (moduleSummary.AnyInstalled)
            {
                gain *= moduleSummary.ContaminationMultiplier;
            }
            return Mathf.Clamp(gain, 0.02f, 0.24f);
        }

        public static float GetIdleDecayPerTick(Building_AbyssalSummoningCircle circle)
        {
            float containment = circle?.ContainmentRating ?? 0f;
            float contamination = circle?.ResidualContamination ?? 0f;
            return Mathf.Max(0.0006f, 0.0012f + containment * 0.02f - contamination * 0.003f);
        }

        public static float GetCooldownDecayPerTick(Building_AbyssalSummoningCircle circle)
        {
            return GetIdleDecayPerTick(circle) + 0.002f;
        }

        public static float GetActivePhasePressure(Building_AbyssalSummoningCircle.ConsoleRitualPhase phase)
        {
            switch (phase)
            {
                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Charging:
                    return 0.08f * AbyssalDifficultyUtility.GetInstabilityMultiplier();
                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Surge:
                    return 0.16f * AbyssalDifficultyUtility.GetInstabilityMultiplier();
                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Breach:
                    return 0.28f * AbyssalDifficultyUtility.GetInstabilityMultiplier();
                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Cooldown:
                    return 0.04f * Mathf.Lerp(1f, AbyssalDifficultyUtility.GetInstabilityMultiplier(), 0.45f);
                default:
                    return 0f;
            }
        }

        public static float GetAmbientBleedAmount(Building_AbyssalSummoningCircle circle)
        {
            float heat = circle?.InstabilityHeat ?? 0f;
            if (heat < 0.42f)
            {
                return 0f;
            }

            float amount = Mathf.Clamp(0.004f + (heat - 0.42f) * 0.05f, 0.004f, 0.03f);
            AbyssalCircleStabilizerBonusSummary moduleSummary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            if (moduleSummary.AnyInstalled)
            {
                amount *= moduleSummary.ContaminationMultiplier;
            }
            return Mathf.Clamp(amount, 0.002f, 0.024f);
        }

        public static float GetPurgeRemovedHeat(Building_AbyssalSummoningCircle circle)
        {
            float containment = circle?.ContainmentRating ?? 0f;
            float removed = Mathf.Clamp(0.18f + containment * 0.35f, 0.18f, 0.32f);
            AbyssalCircleStabilizerBonusSummary moduleSummary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            if (moduleSummary.AnyInstalled)
            {
                removed *= moduleSummary.PurgeEfficiencyMultiplier;
            }
            return Mathf.Clamp(removed, 0.18f, 0.38f);
        }

        public static float GetPurgeBackwash(float removedHeat)
        {
            return Mathf.Clamp(0.02f + removedHeat * 0.10f, 0.02f, 0.06f);
        }

        public static float GetVentRemovedContamination(Building_AbyssalSummoningCircle circle)
        {
            float contamination = circle?.ResidualContamination ?? 0f;
            float removed = Mathf.Clamp(0.16f + contamination * 0.18f, 0.12f, 0.32f);
            AbyssalCircleStabilizerBonusSummary moduleSummary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            if (moduleSummary.AnyInstalled)
            {
                removed *= moduleSummary.VentEfficiencyMultiplier;
            }
            return Mathf.Clamp(removed, 0.12f, 0.38f);
        }

        public static float GetVentHeatKick(Building_AbyssalSummoningCircle circle)
        {
            float contamination = circle?.ResidualContamination ?? 0f;
            return Mathf.Clamp(0.04f + contamination * 0.08f, 0.04f, 0.12f);
        }


        public static float GetInstabilityEventChanceMultiplier(Building_AbyssalSummoningCircle circle)
        {
            AbyssalCircleStabilizerBonusSummary moduleSummary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            float baseMultiplier = moduleSummary.AnyInstalled ? moduleSummary.EventChanceMultiplier : 1f;
            return Mathf.Max(0.15f, baseMultiplier * Mathf.Lerp(1f, AbyssalDifficultyUtility.GetInstabilityMultiplier(), 0.55f));
        }

        public static float GetInstabilityEventSeverityMultiplier(Building_AbyssalSummoningCircle circle)
        {
            AbyssalCircleStabilizerBonusSummary moduleSummary = circle != null ? circle.GetStabilizerBonusSummary() : default;
            float baseMultiplier = moduleSummary.AnyInstalled ? moduleSummary.EventSeverityMultiplier : 1f;
            return Mathf.Max(0.15f, baseMultiplier * Mathf.Lerp(1f, AbyssalDifficultyUtility.GetInstabilityMultiplier(), 0.70f));
        }

        public static int GetProjectedImpSpillCount(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            float projectedHeat = GetProjectedPostInvokeHeat(circle, ritual);
            float severity = GetInstabilityEventSeverityMultiplier(circle);
            if (projectedHeat >= 0.96f && severity > 0.92f)
            {
                return 2;
            }

            if (projectedHeat >= 0.82f)
            {
                return 1;
            }

            return 0;
        }

        public static float GetRiskValue(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            if (circle == null)
            {
                return 0.08f;
            }

            float readinessPenalty = circle.IsReadyForSigil(out _) ? 0f : 0.04f;
            float contamination = circle.ResidualContamination;
            if (circle.RitualActive)
            {
                return Mathf.Clamp01(circle.InstabilityHeat + GetActivePhasePressure(circle.CurrentRitualPhase) + contamination * 0.20f + readinessPenalty);
            }

            return Mathf.Clamp(GetProjectedPostInvokeHeat(circle, ritual) + GetProjectedContaminationGain(circle, ritual) * 0.45f + contamination * 0.18f + readinessPenalty, 0.05f, 1f);
        }
    }
}
