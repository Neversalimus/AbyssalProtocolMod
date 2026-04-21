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

            SoundDef soundDef = DefDatabase<SoundDef>.GetNamedSilentFail(soundDefName);
            if (soundDef == null)
            {
                return;
            }

            soundDef.PlayOneShot(
                SoundInfo.InMap(
                    new TargetInfo(cell, map, false),
                    MaintenanceType.None));
        }

        public static void PlayChargeAt(string soundDefName, IntVec3 cell, Map map)
        {
            // Safer than name-only suppression: charge sound muting is now explicit at true charge/aim call sites,
            // so unrelated gameplay uses of the same SoundDef are not accidentally muted.
            if (!(AbyssalProtocolMod.Settings?.enableWeaponChargeSounds ?? false))
            {
                return;
            }

            PlayOneShotAt(soundDefName, cell, map);
        }

        public static void PlayOneShotAt(string soundDefName, IntVec3 cell, Map map)
        {
            if (soundDefName.NullOrEmpty() || map == null || !cell.IsValid)
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
    }
}
