using System;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_PerformanceSettingsUtility
    {
        public static ABY_VisualIntensity CurrentIntensity => AbyssalProtocolMod.Settings?.visualIntensity ?? ABY_VisualIntensity.Full;

        public static bool IsReducedOrLower => CurrentIntensity != ABY_VisualIntensity.Full || (AbyssalProtocolMod.Settings?.reducedMotion ?? false);

        public static bool IsMinimal => CurrentIntensity == ABY_VisualIntensity.Minimal;

        public static string ResolveLabel(ABY_VisualIntensity intensity)
        {
            switch (intensity)
            {
                case ABY_VisualIntensity.Minimal:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_Minimal", "Minimal");
                case ABY_VisualIntensity.Reduced:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_Reduced", "Reduced");
                default:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_Full", "Full");
            }
        }

        public static string ResolveDescription(ABY_VisualIntensity intensity)
        {
            switch (intensity)
            {
                case ABY_VisualIntensity.Minimal:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_MinimalDesc", "Lowest optional visual load: reduced motion, no decorative Dominion ambient VFX, no boss title cards, reduced weather intensity, and UI accents minimized.");
                case ABY_VisualIntensity.Reduced:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_ReducedDesc", "Lower optional visual load: reduced motion, lighter Dominion weather, reduced UI animation, and slower ambient VFX intervals.");
                default:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_FullDesc", "Full Abyssal presentation. Recommended when performance and VRAM are comfortable.");
            }
        }

        public static void ApplyPreset(AbyssalProtocolModSettings settings, ABY_VisualIntensity intensity)
        {
            if (settings == null)
            {
                return;
            }

            settings.visualIntensity = intensity;
            switch (intensity)
            {
                case ABY_VisualIntensity.Minimal:
                    settings.reducedMotion = true;
                    settings.reduceAbyssalUIAnimation = true;
                    settings.enableDominionAmbientVfx = false;
                    settings.enableBossMapPresentationEffects = false;
                    settings.enableBossPresentationTitleCards = false;
                    settings.dominionWeatherIntensity = Mathf.Min(settings.dominionWeatherIntensity, 0.45f);
                    break;
                case ABY_VisualIntensity.Reduced:
                    settings.reducedMotion = true;
                    settings.reduceAbyssalUIAnimation = true;
                    settings.enableDominionAmbientVfx = true;
                    settings.enableBossMapPresentationEffects = true;
                    settings.enableBossPresentationTitleCards = true;
                    settings.dominionWeatherIntensity = Mathf.Min(settings.dominionWeatherIntensity, 0.65f);
                    break;
                default:
                    settings.reducedMotion = false;
                    settings.reduceAbyssalUIAnimation = false;
                    settings.enableDominionAmbientVfx = true;
                    settings.enableBossMapPresentationEffects = true;
                    settings.enableBossPresentationTitleCards = true;
                    settings.dominionWeatherIntensity = Mathf.Max(settings.dominionWeatherIntensity, 0.85f);
                    break;
            }

            settings.ClampValues();
        }

        public static bool ShouldRunDominionAmbientVfx()
        {
            return ShouldRunDominionAmbientVfx(AbyssalProtocolMod.Settings);
        }

        public static bool ShouldRunDominionAmbientVfx(AbyssalProtocolModSettings settings)
        {
            if (settings == null)
            {
                return true;
            }

            if (!settings.enableDominionAmbientVfx || !settings.enableBossMapPresentationEffects)
            {
                return false;
            }

            return settings.visualIntensity != ABY_VisualIntensity.Minimal;
        }

        public static float ResolveVfxIntensityScale()
        {
            return ResolveVfxIntensityScale(AbyssalProtocolMod.Settings);
        }

        public static float ResolveVfxIntensityScale(AbyssalProtocolModSettings settings)
        {
            if (settings == null)
            {
                return 1f;
            }

            float scale;
            switch (settings.visualIntensity)
            {
                case ABY_VisualIntensity.Minimal:
                    scale = 0.38f;
                    break;
                case ABY_VisualIntensity.Reduced:
                    scale = 0.66f;
                    break;
                default:
                    scale = 1f;
                    break;
            }

            if (settings.reducedMotion)
            {
                scale *= 0.82f;
            }

            return Mathf.Clamp(scale, 0.20f, 1.25f);
        }

        public static float ResolveWeatherIntensityScale(AbyssalProtocolModSettings settings)
        {
            if (settings == null)
            {
                return 1f;
            }

            switch (settings.visualIntensity)
            {
                case ABY_VisualIntensity.Minimal:
                    return 0.45f;
                case ABY_VisualIntensity.Reduced:
                    return 0.70f;
                default:
                    return 1f;
            }
        }

        public static int ScaleVfxInterval(int ticks)
        {
            return ScaleVfxInterval(ticks, AbyssalProtocolMod.Settings);
        }

        public static int ScaleVfxInterval(int ticks, AbyssalProtocolModSettings settings)
        {
            if (ticks <= 0 || settings == null)
            {
                return Math.Max(1, ticks);
            }

            float multiplier = 1f;
            switch (settings.visualIntensity)
            {
                case ABY_VisualIntensity.Minimal:
                    multiplier = 3.0f;
                    break;
                case ABY_VisualIntensity.Reduced:
                    multiplier = 1.65f;
                    break;
            }

            if (settings.reducedMotion)
            {
                multiplier = Math.Max(multiplier, 1.35f);
            }

            return Math.Max(1, Mathf.RoundToInt(ticks * multiplier));
        }
    }
}
