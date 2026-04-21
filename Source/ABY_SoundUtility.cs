using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public static class ABY_SoundUtility
    {
        public static void PlayAt(string soundDefName, IntVec3 cell, Map map)
        {
            if (soundDefName.NullOrEmpty() || map == null || !cell.IsValid)
            {
                return;
            }

            if (!AbyssalProtocolMod.Settings.enableWeaponChargeSounds && ABY_WeaponChargeSoundUtility.IsTrackedChargeSoundName(soundDefName))
            {
                return;
            }

            SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail(soundDefName);
            if (soundDef == null)
            {
                return;
            }

            if (soundDef.sustain)
            {
                return;
            }

            soundDef.PlayOneShot(
                SoundInfo.InMap(
                    new TargetInfo(cell, map, false),
                    MaintenanceType.None));
        }

        public static bool IsAbyssalChargeSoundName(string soundDefName)
        {
            if (soundDefName.NullOrEmpty() || !soundDefName.StartsWith("ABY_", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return soundDefName.IndexOf("Charge", System.StringComparison.OrdinalIgnoreCase) >= 0
                || soundDefName.IndexOf("Aim", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
