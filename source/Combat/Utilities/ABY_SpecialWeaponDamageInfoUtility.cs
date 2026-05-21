using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_SpecialWeaponDamageInfoUtility
    {
        private const string SpecterLashDefName = "ABY_SpecterLashProjector";
        private const string CrownshardStormcasterDefName = "ABY_CrownshardStormcaster";
        private const string OblivionChoirDefName = "ABY_OblivionChoir";

        public static bool HasProfile(ThingDef def)
        {
            if (def == null || def.defName.NullOrEmpty())
            {
                return false;
            }

            return def.defName == SpecterLashDefName
                || def.defName == CrownshardStormcasterDefName
                || def.defName == OblivionChoirDefName;
        }

        public static List<StatDrawEntry> BuildStatEntries(ThingDef def)
        {
            List<StatDrawEntry> result = new List<StatDrawEntry>();
            try
            {
                DamageProfile profile = ResolveProfile(def);
                if (profile == null)
                {
                    return result;
                }

                StatCategoryDef category = ResolveStatCategory();
                if (category == null)
                {
                    return result;
                }

                int order = 7900;
                AddEntry(result, category, ref order,
                    "ABY_SpecialWeaponDamage_Profile", "Abyssal damage profile",
                    profile.profileValue,
                    "ABY_SpecialWeaponDamage_ProfileDesc", "Custom damage layers that are not represented by the vanilla projectile damage line alone.");

                AddEntry(result, category, ref order,
                    "ABY_SpecialWeaponDamage_Impact", "Base impact",
                    profile.impactValue,
                    "ABY_SpecialWeaponDamage_ImpactDesc", "The ordinary projectile hit shown by the vanilla weapon card.");

                AddEntry(result, category, ref order,
                    "ABY_SpecialWeaponDamage_Special", "Special damage layer",
                    profile.specialValue,
                    "ABY_SpecialWeaponDamage_SpecialDesc", "Additional C#-driven damage applied by this weapon when its special conditions are met.");

                if (!profile.packageValue.NullOrEmpty())
                {
                    AddEntry(result, category, ref order,
                        "ABY_SpecialWeaponDamage_Package", "Full package",
                        profile.packageValue,
                        "ABY_SpecialWeaponDamage_PackageDesc", "Approximate maximum damage package before armor. Conditional effects may end early or miss targets.");
                }

                if (!profile.limitsValue.NullOrEmpty())
                {
                    AddEntry(result, category, ref order,
                        "ABY_SpecialWeaponDamage_Limits", "Limits",
                        profile.limitsValue,
                        "ABY_SpecialWeaponDamage_LimitsDesc", "Conditions that can prevent the full damage package from being delivered.");
                }
            }
            catch (Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("special weapon damage info card", ex);
            }

            return result;
        }

        public static string GetForgeDetails(ThingDef def)
        {
            try
            {
                DamageProfile profile = ResolveProfile(def);
                return profile?.forgeDetails ?? string.Empty;
            }
            catch (Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("special weapon Forge damage details", ex);
                return string.Empty;
            }
        }

        private static DamageProfile ResolveProfile(ThingDef def)
        {
            if (def == null || def.defName.NullOrEmpty())
            {
                return null;
            }

            switch (def.defName)
            {
                case SpecterLashDefName:
                    return BuildSpecterLashProfile();
                case CrownshardStormcasterDefName:
                    return BuildCrownshardStormcasterProfile();
                case OblivionChoirDefName:
                    return BuildOblivionChoirProfile();
                default:
                    return null;
            }
        }

        private static DamageProfile BuildSpecterLashProfile()
        {
            int maximumPulseCount = SpecterLashStreamGameComponent.MaximumPawnPulseCount;
            float maximumRawPackage = 26f + maximumPulseCount * SpecterLashStreamGameComponent.PulseDamage;

            return new DamageProfile
            {
                profileValue = TranslateOrFallback("ABY_SpecialWeaponDamage_Specter_ProfileValue", "Tether lock / single-target burn"),
                impactValue = TranslateOrFallback("ABY_SpecialWeaponDamage_Specter_ImpactValue", "26 Burn, 90% AP"),
                specialValue = FormatTranslated("ABY_SpecialWeaponDamage_Specter_SpecialValue", "up to {0} x {1} Burn, {2}% AP",
                    maximumPulseCount,
                    FormatNumber(SpecterLashStreamGameComponent.PulseDamage),
                    FormatPercent(SpecterLashStreamGameComponent.PulseArmorPenetration)),
                packageValue = FormatTranslated("ABY_SpecialWeaponDamage_Specter_PackageValue", "up to {0} raw Burn before armor", FormatNumber(maximumRawPackage)),
                limitsValue = TranslateOrFallback("ABY_SpecialWeaponDamage_Specter_LimitsValue", "requires maintained range and line of sight; the stream ends if the shooter is interrupted or the target breaks lock"),
                forgeDetails = TranslateOrFallback("ABY_SpecialWeaponDamage_Specter_ForgeDetails", "Combat profile: 26 Burn impact, then a maintained tether can add up to 10 x 16 Burn at 24% AP. Full held lock: up to 186 raw Burn before armor. Requires line of sight and range to hold.")
            };
        }

        private static DamageProfile BuildCrownshardStormcasterProfile()
        {
            int fullPulseCount = Thing_CrownshardStormNode.DefaultPulseCount;
            int shieldedPulseCount = Thing_CrownshardStormNode.ShieldDampenedPulseCount;
            float singleTargetRaw = 12f + fullPulseCount * Thing_CrownshardStormNode.PulseDamage;
            float denseTargetRaw = 12f + fullPulseCount * Thing_CrownshardStormNode.PulseDamage * Thing_CrownshardStormNode.MechOrBuildingDamageMultiplier;

            return new DamageProfile
            {
                profileValue = TranslateOrFallback("ABY_SpecialWeaponDamage_Stormcaster_ProfileValue", "Storm field / area denial"),
                impactValue = TranslateOrFallback("ABY_SpecialWeaponDamage_Stormcaster_ImpactValue", "12 Burn, 45% AP"),
                specialValue = FormatTranslated("ABY_SpecialWeaponDamage_Stormcaster_SpecialValue", "{0} Cut, {1}% AP every {2} ticks, up to {3} targets",
                    FormatNumber(Thing_CrownshardStormNode.PulseDamage),
                    FormatPercent(Thing_CrownshardStormNode.PulseArmorPenetration),
                    Thing_CrownshardStormNode.PulseIntervalTicks,
                    Thing_CrownshardStormNode.MaxTargetsPerPulse),
                packageValue = FormatTranslated("ABY_SpecialWeaponDamage_Stormcaster_PackageValue", "up to {0} raw damage per target; {1} vs mechanoids/buildings",
                    FormatNumber(singleTargetRaw),
                    FormatNumber(denseTargetRaw)),
                limitsValue = FormatTranslated("ABY_SpecialWeaponDamage_Stormcaster_LimitsValue", "{0} pulses normally or {1} after shield damping; radius {2}; ignores downed pawns",
                    fullPulseCount,
                    shieldedPulseCount,
                    FormatNumber(Thing_CrownshardStormNode.Radius)),
                forgeDetails = TranslateOrFallback("ABY_SpecialWeaponDamage_Stormcaster_ForgeDetails", "Combat profile: 12 Burn seed impact, then a storm node pulses 7 Cut at 48% AP every 20 ticks in radius 3.35, up to 3 targets per pulse. Full field: up to 12 pulses, or 8 after shield damping. Dense targets take x1.3 pulse damage.")
            };
        }

        private static DamageProfile BuildOblivionChoirProfile()
        {
            return new DamageProfile
            {
                profileValue = TranslateOrFallback("ABY_SpecialWeaponDamage_Oblivion_ProfileValue", "Branching resonance / collapse weapon"),
                impactValue = TranslateOrFallback("ABY_SpecialWeaponDamage_Oblivion_ImpactValue", "34 Burn, 96% AP + 60 Burn explosion, 122% AP"),
                specialValue = FormatTranslated("ABY_SpecialWeaponDamage_Oblivion_SpecialValue", "{0} Burn, {1}% AP every {2} ticks, up to {3} branch targets",
                    FormatNumber(Projectile_OblivionChoirCore.ArcDamage),
                    FormatPercent(Projectile_OblivionChoirCore.ArcArmorPenetration),
                    Projectile_OblivionChoirCore.ArcIntervalTicks,
                    Projectile_OblivionChoirCore.MaxArcTargetsPerPulse),
                packageValue = FormatTranslated("ABY_SpecialWeaponDamage_Oblivion_PackageValue", "resonance marks detonate for 5-18 Burn at 54-86% AP in radius {0}",
                    FormatNumber(Projectile_OblivionChoirCore.ResonanceImpactRadius)),
                limitsValue = TranslateOrFallback("ABY_SpecialWeaponDamage_Oblivion_LimitsValue", "branch count depends on travel path, hostile targets near the core, and accumulated resonance severity"),
                forgeDetails = TranslateOrFallback("ABY_SpecialWeaponDamage_Oblivion_ForgeDetails", "Combat profile: 34 Burn core impact plus 60 Burn collapse explosion at 122% AP. While travelling, the core branches every 3 ticks for 3.75 Burn at 36% AP, up to 5 targets per branch pulse. Branch hits build resonance; marked targets within 7.2 cells detonate for 5-18 Burn at 54-86% AP.")
            };
        }

        private static StatCategoryDef ResolveStatCategory()
        {
            return DefDatabase<StatCategoryDef>.GetNamedSilentFail("Weapon_Ranged")
                ?? DefDatabase<StatCategoryDef>.GetNamedSilentFail("Weapon")
                ?? DefDatabase<StatCategoryDef>.GetNamedSilentFail("BasicsImportant")
                ?? DefDatabase<StatCategoryDef>.GetNamedSilentFail("Basics");
        }

        private static void AddEntry(List<StatDrawEntry> result, StatCategoryDef category, ref int order, string labelKey, string fallbackLabel, string value, string descKey, string fallbackDesc)
        {
            result.Add(new StatDrawEntry(
                category,
                TranslateOrFallback(labelKey, fallbackLabel),
                value ?? string.Empty,
                TranslateOrFallback(descKey, fallbackDesc),
                order++));
        }

        private static string TranslateOrFallback(string key, string fallback)
        {
            try
            {
                string translated = key.Translate();
                return translated.NullOrEmpty() || translated == key ? fallback : translated;
            }
            catch
            {
                return fallback;
            }
        }


        private static string FormatTranslated(string key, string fallbackFormat, params object[] args)
        {
            string format = TranslateOrFallback(key, fallbackFormat);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return string.Format(fallbackFormat, args);
            }
        }

        private static string FormatPercent(float fraction)
        {
            return Math.Round(fraction * 100f).ToString("0");
        }

        private static string FormatNumber(float value)
        {
            return Math.Abs(value - (float)Math.Round(value)) < 0.01f
                ? Math.Round(value).ToString("0")
                : value.ToString("0.#");
        }

        private sealed class DamageProfile
        {
            public string profileValue;
            public string impactValue;
            public string specialValue;
            public string packageValue;
            public string limitsValue;
            public string forgeDetails;
        }
    }
}
