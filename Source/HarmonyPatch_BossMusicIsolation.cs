using HarmonyLib;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class HarmonyPatch_BossMusicIsolation_AppropriateNow
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(MusicManagerPlay), "AppropriateNow", new[] { typeof(SongDef) });
        }

        private static void Postfix(SongDef song, ref bool __result)
        {
            if (__result && ABY_BossMusicUtility.ShouldBlockVanillaSelection(song))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossMusicIsolation_ChooseNextSong
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(MusicManagerPlay), "ChooseNextSong");
        }

        private static void Postfix(ref SongDef __result)
        {
            if (ABY_BossMusicUtility.ShouldBlockVanillaSelection(__result))
            {
                __result = null;
            }
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossMusicIsolation_PlaySong
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(MusicManagerPlay), "PlaySong", new[] { typeof(SongDef), typeof(bool), typeof(bool) });
        }

        private static bool Prefix(SongDef song)
        {
            if (ABY_BossMusicUtility.ShouldAllowExplicitPlay(song))
            {
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossMusicIsolation_ForcePlaySong
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(MusicManagerPlay), "ForcePlaySong", new[] { typeof(SongDef), typeof(bool) });
        }

        private static bool Prefix(SongDef song)
        {
            if (ABY_BossMusicUtility.ShouldAllowExplicitPlay(song))
            {
                return true;
            }

            return false;
        }
    }
}
