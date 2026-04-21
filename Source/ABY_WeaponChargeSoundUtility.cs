using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public static class ABY_WeaponChargeSoundUtility
    {
        private const string AbyssalPackageId = "neversalimus.abyssalprotocol";

        private readonly struct DefIndexKey
        {
            public DefIndexKey(string defName, int index)
            {
                DefName = defName;
                Index = index;
            }

            public string DefName { get; }
            public int Index { get; }
        }

        private static bool cacheBuilt;
        private static readonly Dictionary<DefIndexKey, string> OriginalVerbAimSounds = new Dictionary<DefIndexKey, string>();
        private static readonly Dictionary<DefIndexKey, string> OriginalCompAimSounds = new Dictionary<DefIndexKey, string>();
        private static readonly Dictionary<string, bool> OriginalSoundSustain = new Dictionary<string, bool>();
        private static readonly Dictionary<string, float> OriginalSoundSustainFadeout = new Dictionary<string, float>();
        private static readonly HashSet<string> TrackedChargeSoundNames = new HashSet<string>();
        private static FieldInfo verbsField;

        public static void ApplyCurrentSettings()
        {
            EnsureCache();
            bool enabled = AbyssalProtocolMod.Settings?.enableWeaponChargeSounds ?? false;

            foreach (KeyValuePair<DefIndexKey, string> entry in OriginalVerbAimSounds)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(entry.Key.DefName);
                if (def == null)
                {
                    continue;
                }

                List<VerbProperties> verbs = GetVerbList(def);
                if (verbs == null)
                {
                    continue;
                }

                int verbIndex = entry.Key.Index;
                if (verbIndex < 0 || verbIndex >= verbs.Count)
                {
                    continue;
                }

                verbs[verbIndex].soundAiming = enabled && !entry.Value.NullOrEmpty()
                    ? DefDatabase<SoundDef>.GetNamedSilentFail(entry.Value)
                    : null;
            }

            foreach (KeyValuePair<DefIndexKey, string> entry in OriginalCompAimSounds)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(entry.Key.DefName);
                if (def == null || def.comps == null)
                {
                    continue;
                }

                int compIndex = entry.Key.Index;
                if (compIndex < 0 || compIndex >= def.comps.Count)
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

            ApplyChargeSoundSustain(enabled);
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
                if (def == null || def.defName.NullOrEmpty())
                {
                    continue;
                }

                List<VerbProperties> verbs = GetVerbList(def);
                if (verbs != null)
                {
                    for (int i = 0; i < verbs.Count; i++)
                    {
                        string soundName = verbs[i]?.soundAiming?.defName;
                        if (!ShouldTrackChargeSound(def, soundName))
                        {
                            continue;
                        }

                        OriginalVerbAimSounds[new DefIndexKey(def.defName, i)] = soundName;
                        RegisterTrackedChargeSound(soundName);
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
                    if (!ShouldTrackChargeSound(def, soundName))
                    {
                        continue;
                    }

                    OriginalCompAimSounds[new DefIndexKey(def.defName, i)] = soundName;
                    RegisterTrackedChargeSound(soundName);
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

        private static void RegisterTrackedChargeSound(string soundDefName)
        {
            if (soundDefName.NullOrEmpty())
            {
                return;
            }

            TrackedChargeSoundNames.Add(soundDefName);
            CacheOriginalSound(soundDefName);
        }

        public static bool IsTrackedChargeSoundName(string soundDefName)
        {
            if (soundDefName.NullOrEmpty())
            {
                return false;
            }

            EnsureCache();
            return TrackedChargeSoundNames.Contains(soundDefName);
        }

        private static bool ShouldTrackChargeSound(ThingDef ownerDef, string soundDefName)
        {
            if (soundDefName.NullOrEmpty())
            {
                return false;
            }

            if (IsAbyssalOwnedDef(ownerDef))
            {
                return true;
            }

            SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail(soundDefName);
            return IsAbyssalOwnedDef(soundDef);
        }

        private static bool IsAbyssalOwnedDef(Def def)
        {
            ModContentPack mod = def?.modContentPack;
            if (mod == null)
            {
                return false;
            }

            string packageId = mod.PackageId;
            return !packageId.NullOrEmpty()
                && packageId.Equals(AbyssalPackageId, System.StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyChargeSoundSustain(bool enabled)
        {
            foreach (string soundDefName in OriginalSoundSustain.Keys)
            {
                SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail(soundDefName);
                if (soundDef == null)
                {
                    continue;
                }

                if (!enabled)
                {
                    if (OriginalSoundSustain.TryGetValue(soundDefName, out bool originalSustain))
                    {
                        soundDef.sustain = originalSustain;
                    }

                    if (OriginalSoundSustainFadeout.TryGetValue(soundDefName, out float originalFadeout))
                    {
                        soundDef.sustainFadeoutTime = originalFadeout;
                    }

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
