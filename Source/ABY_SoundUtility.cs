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
