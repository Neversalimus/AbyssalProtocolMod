using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalCircleInstabilityUtility
    {
        public const int HeatTickInterval = 60;
        public const int ContainmentRefreshInterval = 180;

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
            return Mathf.Clamp(poweredContribution + healthContribution + attunementContribution, 0f, 0.32f);
        }

        public static float GetProjectedHeatGain(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            float baseGain = ritual?.InstabilityGain ?? 0.12f;
            float existingPressure = (circle?.InstabilityHeat ?? 0f) * 0.30f;
            float containmentMitigation = (circle?.ContainmentRating ?? 0f) * 0.55f;
            float gain = baseGain + existingPressure - containmentMitigation;
            return Mathf.Clamp(gain, 0.04f, 0.55f);
        }

        public static float GetProjectedPostInvokeHeat(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            if (circle == null)
            {
                return 0.05f;
            }

            return Mathf.Clamp01(circle.InstabilityHeat + GetProjectedHeatGain(circle, ritual));
        }

        public static float GetIdleDecayPerTick(Building_AbyssalSummoningCircle circle)
        {
            float containment = circle?.ContainmentRating ?? 0f;
            return 0.0015f + containment * 0.02f;
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
                    return 0.08f;
                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Surge:
                    return 0.16f;
                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Breach:
                    return 0.28f;
                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Cooldown:
                    return 0.04f;
                default:
                    return 0f;
            }
        }

        public static float GetRiskValue(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            if (circle == null)
            {
                return 0.08f;
            }

            float readinessPenalty = circle.IsReadyForSigil(out _) ? 0f : 0.04f;
            if (circle.RitualActive)
            {
                return Mathf.Clamp01(circle.InstabilityHeat + GetActivePhasePressure(circle.CurrentRitualPhase) + readinessPenalty);
            }

            return Mathf.Clamp(GetProjectedPostInvokeHeat(circle, ritual) + readinessPenalty, 0.05f, 1f);
        }
    }
}
