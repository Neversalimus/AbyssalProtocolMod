using HarmonyLib;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    [HarmonyPatch(typeof(SoundStarter))]
    public static class HarmonyPatch_WeaponChargeSoundGate
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(SoundStarter.PlayOneShot))]
        public static bool PlayOneShotPrefix(SoundDef soundDef, SoundInfo info)
        {
            return !ABY_WeaponChargeSoundUtility.ShouldSuppressChargeSound(soundDef);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(SoundStarter.TrySpawnSustainer))]
        public static bool TrySpawnSustainerPrefix(SoundDef soundDef, SoundInfo info, ref Sustainer __result)
        {
            if (!ABY_WeaponChargeSoundUtility.ShouldSuppressChargeSound(soundDef))
            {
                return true;
            }

            __result = null;
            return false;
        }
    }
}
