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

            if (Find.CurrentMap == map && HasOnCameraSubSound(soundDef))
            {
                SoundStarter.PlayOneShotOnCamera(soundDef, map);
                return;
            }

            soundDef.PlayOneShot(
                SoundInfo.InMap(
                    new TargetInfo(cell, map, false),
                    MaintenanceType.None));
        }

        private static bool HasOnCameraSubSound(SoundDef soundDef)
        {
            if (soundDef == null || soundDef.subSounds == null)
            {
                return false;
            }

            for (int i = 0; i < soundDef.subSounds.Count; i++)
            {
                SubSoundDef subSound = soundDef.subSounds[i];
                if (subSound != null && subSound.onCamera)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
