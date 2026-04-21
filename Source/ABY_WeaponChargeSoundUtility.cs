using System.Collections.Generic;
using System.Reflection;
using System;
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
        private static readonly Dictionary<Type, AimSoundAccessor> AimSoundAccessorsByType = new Dictionary<Type, AimSoundAccessor>();
        private static readonly HashSet<Type> WarnedUnsupportedAimSoundTypes = new HashSet<Type>();
        private static FieldInfo verbsField;

        private sealed class AimSoundAccessor
        {
            public FieldInfo Field;
            public PropertyInfo Property;
            public bool CanWrite;
        }

        public static void ApplyCurrentSettings()
        {
            RebuildCache();
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
                if (compProps == null || !TrySetAimSoundDefName(compProps, enabled ? entry.Value : null))
                {
                    continue;
                }
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
                    if (!TryGetAimSoundDefName(compProps, out string soundName))
                    {
                        continue;
                    }

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

        private static void RebuildCache()
        {
            cacheBuilt = false;
            OriginalVerbAimSounds.Clear();
            OriginalCompAimSounds.Clear();
            OriginalSoundSustain.Clear();
            OriginalSoundSustainFadeout.Clear();
            TrackedChargeSoundNames.Clear();

            EnsureCache();
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

        private static bool TryGetAimSoundDefName(CompProperties compProps, out string soundDefName)
        {
            soundDefName = null;
            if (compProps == null)
            {
                return false;
            }

            AimSoundAccessor accessor = ResolveAimSoundAccessor(compProps.GetType());
            if (accessor == null)
            {
                return false;
            }

            object value = null;
            try
            {
                if (accessor.Field != null)
                {
                    value = accessor.Field.GetValue(compProps);
                }
                else if (accessor.Property != null)
                {
                    value = accessor.Property.GetValue(compProps, null);
                }
            }
            catch
            {
                return false;
            }

            soundDefName = value as string;
            return !soundDefName.NullOrEmpty();
        }

        private static bool TrySetAimSoundDefName(CompProperties compProps, string soundDefName)
        {
            if (compProps == null)
            {
                return false;
            }

            AimSoundAccessor accessor = ResolveAimSoundAccessor(compProps.GetType());
            if (accessor == null || !accessor.CanWrite)
            {
                return false;
            }

            try
            {
                if (accessor.Field != null)
                {
                    accessor.Field.SetValue(compProps, soundDefName);
                    return true;
                }

                accessor.Property.SetValue(compProps, soundDefName, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static AimSoundAccessor ResolveAimSoundAccessor(Type compType)
        {
            if (compType == null)
            {
                return null;
            }

            if (AimSoundAccessorsByType.TryGetValue(compType, out AimSoundAccessor cached))
            {
                return cached;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            AimSoundAccessor accessor = new AimSoundAccessor();

            FieldInfo field = compType.GetField("aimSoundDefName", flags);
            if (field != null && field.FieldType == typeof(string))
            {
                accessor.Field = field;
                accessor.CanWrite = true;
                AimSoundAccessorsByType[compType] = accessor;
                return accessor;
            }

            PropertyInfo property = compType.GetProperty("aimSoundDefName", flags);
            if (property != null && property.PropertyType == typeof(string) && property.GetIndexParameters().Length == 0)
            {
                accessor.Property = property;
                accessor.CanWrite = property.CanWrite;
                AimSoundAccessorsByType[compType] = accessor;
                return accessor;
            }

            if (!WarnedUnsupportedAimSoundTypes.Contains(compType))
            {
                WarnedUnsupportedAimSoundTypes.Add(compType);
                Log.Warning("[Abyssal Protocol] Unable to access aimSoundDefName on comp type: " + compType.FullName);
            }

            AimSoundAccessorsByType[compType] = null;
            return null;
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
