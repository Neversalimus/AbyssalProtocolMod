using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ApparelAegisUtility
    {
        private const string DefaultCategoryDefName = "Apparel";

        public static string TranslateOrFallback(string key, string fallback)
        {
            if (key.NullOrEmpty())
            {
                return fallback ?? string.Empty;
            }

            try
            {
                string translated = key.Translate();
                if (!translated.NullOrEmpty() && translated != key)
                {
                    return translated;
                }
            }
            catch
            {
            }

            return fallback ?? key;
        }

        public static DefModExtension_ABY_ApparelAegis GetAegisExtension(Apparel apparel)
        {
            return apparel?.def?.GetModExtension<DefModExtension_ABY_ApparelAegis>();
        }

        public static bool HasAegisExtension(Apparel apparel)
        {
            return GetAegisExtension(apparel) != null;
        }

        public static string AegisLabel(DefModExtension_ABY_ApparelAegis ext)
        {
            return TranslateOrFallback(ext?.labelKey, "Aegis");
        }

        public static string FormatPoints(float current, float max)
        {
            return Mathf.RoundToInt(Mathf.Max(0f, current)) + " / " + Mathf.RoundToInt(Mathf.Max(1f, max));
        }

        public static string SecondsFromTicks(int ticks)
        {
            return (Mathf.Max(0, ticks) / 60f).ToString("0.0") + "s";
        }

        public static bool IsExternalShieldApparel(Apparel apparel)
        {
            if (apparel?.def == null)
            {
                return false;
            }

            if (HasAegisExtension(apparel))
            {
                return false;
            }

            string defName = apparel.def.defName ?? string.Empty;
            if (defName == "ShieldBelt")
            {
                return true;
            }

            string thingClassName = apparel.def.thingClass != null ? apparel.def.thingClass.FullName ?? apparel.def.thingClass.Name : string.Empty;
            if (ContainsShieldToken(thingClassName))
            {
                return true;
            }

            List<CompProperties> comps = apparel.def.comps;
            if (comps != null)
            {
                for (int i = 0; i < comps.Count; i++)
                {
                    Type compClass = comps[i]?.compClass;
                    if (compClass == null)
                    {
                        continue;
                    }

                    string compName = compClass.FullName ?? compClass.Name;
                    if (ContainsShieldToken(compName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsShieldToken(string value)
        {
            if (value.NullOrEmpty())
            {
                return false;
            }

            string lower = value.ToLowerInvariant();
            return lower.Contains("shieldbelt") || lower.Contains("shield_belt") || lower.Contains("compshield") || lower.Contains("shieldcomp") || lower.EndsWith("shield") || lower.Contains(".shield");
        }

        public static bool IsExplosiveDamage(DamageDef def)
        {
            if (def == null || def.defName.NullOrEmpty())
            {
                return false;
            }

            string lower = def.defName.ToLowerInvariant();
            return lower.Contains("bomb") || lower.Contains("explosion") || lower.Contains("explosive") || lower.Contains("blast") || lower.Contains("grenade") || lower.Contains("shell") || lower.Contains("rocket") || lower.Contains("mortar");
        }

        public static StatCategoryDef ResolveApparelCategory()
        {
            return DefDatabase<StatCategoryDef>.GetNamedSilentFail(DefaultCategoryDefName) ?? DefDatabase<StatCategoryDef>.GetNamedSilentFail("Basics");
        }

        public static void PlaySound(string soundDefName, IntVec3 position, Map map)
        {
            if (soundDefName.NullOrEmpty() || map == null)
            {
                return;
            }

            try
            {
                ABY_SoundUtility.PlayAt(soundDefName, position, map);
            }
            catch
            {
            }
        }
    }
}
