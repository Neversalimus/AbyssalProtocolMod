using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public static class ABY_WeaponChargeSoundUtility
    {
        private static bool cacheBuilt;
        private static readonly Dictionary<string, string> OriginalVerbAimSounds = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> OriginalCompAimSounds = new Dictionary<string, string>();
        private static readonly Dictionary<string, bool> OriginalSoundSustain = new Dictionary<string, bool>();
        private static readonly Dictionary<string, float> OriginalSoundSustainFadeout = new Dictionary<string, float>();
        private static FieldInfo verbsField;

        public static void ApplyCurrentSettings()
        {
            EnsureCache();
            bool enabled = AbyssalProtocolMod.Settings?.enableWeaponChargeSounds ?? false;

            foreach (KeyValuePair<string, string> entry in OriginalVerbAimSounds)
            {
                string[] parts = entry.Key.Split('|');
                if (parts.Length != 2)
                {
                    continue;
                }

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(parts[0]);
                if (def == null)
                {
                    continue;
                }

                List<VerbProperties> verbs = GetVerbList(def);
                if (verbs == null)
                {
                    continue;
                }

                if (!int.TryParse(parts[1], out int verbIndex) || verbIndex < 0 || verbIndex >= verbs.Count)
                {
                    continue;
                }

                verbs[verbIndex].soundAiming = enabled && !entry.Value.NullOrEmpty()
                    ? DefDatabase<SoundDef>.GetNamedSilentFail(entry.Value)
                    : null;
            }

            foreach (KeyValuePair<string, string> entry in OriginalCompAimSounds)
            {
                string[] parts = entry.Key.Split('|');
                if (parts.Length != 2)
                {
                    continue;
                }

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(parts[0]);
                if (def == null || def.comps == null)
                {
                    continue;
                }

                if (!int.TryParse(parts[1], out int compIndex) || compIndex < 0 || compIndex >= def.comps.Count)
                {
                    continue;
                }

                CompProperties compProps = def.comps[compIndex];
                FieldInfo aimField = compProps?.GetType().GetField("aimSoundDefName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (aimField == null || aimField.FieldType != typeof(string))
                {
                    continue;
                }

                aimField.SetValue(compProps, enabled ? entry.Value : null);
            }

            NormalizeChargeSounds();
        }

        private static void EnsureCache()
        {
            if (cacheBuilt)
            {
                return;
            }

            verbsField = typeof(ThingDef).GetField("verbs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def == null || def.defName.NullOrEmpty() || !def.defName.StartsWith("ABY_"))
                {
                    continue;
                }

                List<VerbProperties> verbs = GetVerbList(def);
                if (verbs != null)
                {
                    for (int i = 0; i < verbs.Count; i++)
                    {
                        string soundName = verbs[i]?.soundAiming?.defName;
                        if (!soundName.NullOrEmpty())
                        {
                            OriginalVerbAimSounds[def.defName + "|" + i] = soundName;
                            CacheOriginalSound(soundName);
                        }
                    }
                }

                if (def.comps == null)
                {
                    continue;
                }

                for (int i = 0; i < def.comps.Count; i++)
                {
                    CompProperties compProps = def.comps[i];
                    FieldInfo aimField = compProps?.GetType().GetField("aimSoundDefName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (aimField == null || aimField.FieldType != typeof(string))
                    {
                        continue;
                    }

                    string soundName = aimField.GetValue(compProps) as string;
                    if (soundName.NullOrEmpty())
                    {
                        continue;
                    }

                    OriginalCompAimSounds[def.defName + "|" + i] = soundName;
                    CacheOriginalSound(soundName);
                }
            }

            cacheBuilt = true;
        }

        private static void CacheOriginalSound(string soundDefName)
        {
            if (soundDefName.NullOrEmpty() || OriginalSoundSustain.ContainsKey(soundDefName))
            {
                return;
            }

            SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail(soundDefName);
            if (soundDef == null)
            {
                return;
            }

            OriginalSoundSustain[soundDefName] = soundDef.sustain;
            OriginalSoundSustainFadeout[soundDefName] = soundDef.sustainFadeoutTime;
        }

        private static void NormalizeChargeSounds()
        {
            foreach (string soundDefName in OriginalSoundSustain.Keys)
            {
                SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail(soundDefName);
                if (soundDef == null)
                {
                    continue;
                }

                soundDef.sustain = false;
                soundDef.sustainFadeoutTime = 0f;
            }
        }

        private static List<VerbProperties> GetVerbList(ThingDef def)
        {
            return verbsField?.GetValue(def) as List<VerbProperties>;
        }
    }
}
