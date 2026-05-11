using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_WeaponChargeSoundUtility
    {
        private static readonly HashSet<string> ExplicitChargeSoundNames = new HashSet<string>
        {
            "ABY_ReactorSaintCharge",
            "ABY_RiftCarbineCharge",
            "ABY_UltraPlasmaCharge",
            "ABY_SpecterLashCharge",
            "ABY_CrownspikeRailCharge",
            "ABY_CanticleDriverAim",
            "ABY_CrownshardStormcasterAim",
            "ABY_VesperLanceAim",
            "ABY_LitanyGrinderAim",
            "ABY_PhalanxDriverAim"
        };

        public static void ApplyCurrentSettings()
        {
            // Kept for the mod settings UI and startup hooks. The old implementation rewrote
            // VerbProperties.soundAiming and custom comp fields by reflection. Charge sound
            // enable/disable is now handled at the sound playback boundary by Harmony so defs
            // remain intact and can be toggled without mutating ThingDefs.
        }

        public static bool IsTrackedChargeSoundName(string soundDefName)
        {
            if (soundDefName.NullOrEmpty())
            {
                return false;
            }

            if (ExplicitChargeSoundNames.Contains(soundDefName))
            {
                return true;
            }

            if (!soundDefName.StartsWith("ABY_"))
            {
                return false;
            }

            return soundDefName.Contains("Charge")
                || soundDefName.Contains("Aim")
                || soundDefName.Contains("Aiming");
        }

        public static bool ShouldSuppressChargeSound(SoundDef soundDef)
        {
            if (soundDef == null)
            {
                return false;
            }

            bool enabled = AbyssalProtocolMod.Settings?.enableWeaponChargeSounds ?? false;
            return !enabled && IsTrackedChargeSoundName(soundDef.defName);
        }
    }
}
