using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class HarmonyPatch_BossMusicIsolation_AppropriateNow
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodBase method = AccessTools.Method(typeof(MusicManagerPlay), "AppropriateNow", new[] { typeof(SongDef) });
            return ResolveSingleTarget("boss-music-target-appropriatenow", "MusicManagerPlay.AppropriateNow(SongDef)", method);
        }

        private static void Postfix(SongDef song, ref bool __result)
        {
            if (__result && ABY_BossMusicUtility.ShouldBlockVanillaSelection(song))
            {
                __result = false;
            }
        }

        private static IEnumerable<MethodBase> ResolveSingleTarget(string key, string label, MethodBase method)
        {
            if (method != null)
            {
                return new[] { method };
            }

            ABY_LogThrottleUtility.Warning(key, "[Abyssal Protocol] Boss music Harmony target not found: " + label + "; related isolation guard disabled for this runtime.", 999999);
            return Array.Empty<MethodBase>();
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossMusicIsolation_ChooseNextSong
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodBase method = AccessTools.Method(typeof(MusicManagerPlay), "ChooseNextSong");
            return ResolveSingleTarget("boss-music-target-choosenextsong", "MusicManagerPlay.ChooseNextSong()", method);
        }

        private static void Postfix(ref SongDef __result)
        {
            if (ABY_BossMusicUtility.ShouldBlockVanillaSelection(__result))
            {
                __result = null;
            }
        }

        private static IEnumerable<MethodBase> ResolveSingleTarget(string key, string label, MethodBase method)
        {
            if (method != null)
            {
                return new[] { method };
            }

            ABY_LogThrottleUtility.Warning(key, "[Abyssal Protocol] Boss music Harmony target not found: " + label + "; related isolation guard disabled for this runtime.", 999999);
            return Array.Empty<MethodBase>();
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossMusicIsolation_PlaySong
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodBase method = AccessTools.Method(typeof(MusicManagerPlay), "PlaySong", new[] { typeof(SongDef), typeof(bool), typeof(bool) });
            return ResolveSingleTarget("boss-music-target-playsong", "MusicManagerPlay.PlaySong(SongDef, bool, bool)", method);
        }

        private static bool Prefix(SongDef song)
        {
            return ABY_BossMusicUtility.ShouldAllowExplicitPlay(song);
        }

        private static IEnumerable<MethodBase> ResolveSingleTarget(string key, string label, MethodBase method)
        {
            if (method != null)
            {
                return new[] { method };
            }

            ABY_LogThrottleUtility.Warning(key, "[Abyssal Protocol] Boss music Harmony target not found: " + label + "; related isolation guard disabled for this runtime.", 999999);
            return Array.Empty<MethodBase>();
        }
    }

    [HarmonyPatch]
    public static class HarmonyPatch_BossMusicIsolation_ForcePlaySong
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodBase method = AccessTools.Method(typeof(MusicManagerPlay), "ForcePlaySong", new[] { typeof(SongDef), typeof(bool) });
            return ResolveSingleTarget("boss-music-target-forceplaysong", "MusicManagerPlay.ForcePlaySong(SongDef, bool)", method);
        }

        private static bool Prefix(SongDef song)
        {
            return ABY_BossMusicUtility.ShouldAllowExplicitPlay(song);
        }

        private static IEnumerable<MethodBase> ResolveSingleTarget(string key, string label, MethodBase method)
        {
            if (method != null)
            {
                return new[] { method };
            }

            ABY_LogThrottleUtility.Warning(key, "[Abyssal Protocol] Boss music Harmony target not found: " + label + "; related isolation guard disabled for this runtime.", 999999);
            return Array.Empty<MethodBase>();
        }
    }
}
