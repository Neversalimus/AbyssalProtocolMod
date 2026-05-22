using System;
using System.Reflection;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_SigilEncounterMusicUtility
    {
        public const string StandardSigilSongDefName = "ABY_AbyssSigilDrum";
        public const string MiniBossSigilSongDefName = "ABY_BellmetalRookery";

        private const string UnstableBreachRitualId = "unstable_breach";
        private const string EmberHuntRitualId = "ember_hunt";
        private const string WardenOfAshRitualId = "warden_of_ash";
        private const string ChoirEngineRitualId = "choir_engine";
        private const string RiftButcherRitualId = "rift_butcher";
        private const string WardenOfAshPawnKindDefName = "ABY_WardenOfAsh";
        private const string ChoirEnginePawnKindDefName = "ABY_ChoirEngine";
        private const string RiftButcherPawnKindDefName = "ABY_RiftButcher";

        public static bool IsReservedSigilSongDefName(string defName)
        {
            return string.Equals(defName, StandardSigilSongDefName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, MiniBossSigilSongDefName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryStartForRitual(string ritualId, Map map = null)
        {
            string songDefName = ResolveSongDefNameForRitual(ritualId);
            if (songDefName.NullOrEmpty())
            {
                return false;
            }

            if (Current.Game == null || Find.MusicManagerPlay == null)
            {
                return false;
            }

            if (IsBossMusicCurrentlyActive())
            {
                return false;
            }

            SongDef song = ABY_DefCache.SongDefNamed(songDefName);
            if (song == null)
            {
                ABY_LogThrottleUtility.Warning(
                    "sigil-music-missing-" + songDefName,
                    "[Abyssal Protocol] Sigil encounter music SongDef is missing: " + songDefName,
                    999999);
                return false;
            }

            bool started;
            using (ABY_BossMusicUtility.AuthorizeBossSongStart(song))
            {
                MusicManagerPlay music = Find.MusicManagerPlay;
                started = TryInvokeSongMethod(music, "ForceStartSong", song, false)
                    || TryInvokeSongMethod(music, "ForcePlaySong", song, false)
                    || TryInvokeSongMethod(music, "StartNewSong", song, false)
                    || TryInvokeSongMethod(music, "PlaySong", song, false);
            }

            if (!started)
            {
                ABY_LogThrottleUtility.Warning(
                    "sigil-music-start-failed-" + songDefName,
                    "[Abyssal Protocol] Could not start sigil encounter music: " + songDefName,
                    999999);
            }

            return started;
        }

        public static string ResolveSongDefNameForPawnKindDefName(string pawnKindDefName)
        {
            if (pawnKindDefName.NullOrEmpty())
            {
                return null;
            }

            if (string.Equals(pawnKindDefName, WardenOfAshPawnKindDefName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(pawnKindDefName, ChoirEnginePawnKindDefName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(pawnKindDefName, RiftButcherPawnKindDefName, StringComparison.OrdinalIgnoreCase))
            {
                return MiniBossSigilSongDefName;
            }

            return null;
        }

        public static float ResolveSongLengthSeconds(string songDefName)
        {
            if (string.Equals(songDefName, StandardSigilSongDefName, StringComparison.OrdinalIgnoreCase))
            {
                return 197.48f;
            }

            if (string.Equals(songDefName, MiniBossSigilSongDefName, StringComparison.OrdinalIgnoreCase))
            {
                return 253.32f;
            }

            return 0f;
        }

        public static string ResolveSongDefNameForRitual(string ritualId)
        {
            if (ritualId.NullOrEmpty())
            {
                return null;
            }

            if (string.Equals(ritualId, UnstableBreachRitualId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ritualId, EmberHuntRitualId, StringComparison.OrdinalIgnoreCase))
            {
                return StandardSigilSongDefName;
            }

            if (string.Equals(ritualId, WardenOfAshRitualId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ritualId, ChoirEngineRitualId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ritualId, RiftButcherRitualId, StringComparison.OrdinalIgnoreCase))
            {
                return MiniBossSigilSongDefName;
            }

            return null;
        }

        private static bool IsBossMusicCurrentlyActive()
        {
            try
            {
                AbyssalBossScreenFXGameComponent bossFx = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();
                Pawn boss = bossFx?.ActiveBoss;
                return boss != null && !boss.Destroyed && !boss.Dead;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryInvokeSongMethod(MusicManagerPlay music, string methodName, SongDef song, bool interrupting)
        {
            if (music == null || song == null || methodName.NullOrEmpty())
            {
                return false;
            }

            try
            {
                MethodInfo[] methods = music.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null || method.Name != methodName)
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 1 && typeof(SongDef).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        method.Invoke(music, new object[] { song });
                        return true;
                    }

                    if (parameters.Length == 2
                        && typeof(SongDef).IsAssignableFrom(parameters[0].ParameterType)
                        && parameters[1].ParameterType == typeof(bool))
                    {
                        method.Invoke(music, new object[] { song, interrupting });
                        return true;
                    }

                    if (parameters.Length == 3
                        && typeof(SongDef).IsAssignableFrom(parameters[0].ParameterType)
                        && parameters[1].ParameterType == typeof(bool)
                        && parameters[2].ParameterType == typeof(bool))
                    {
                        method.Invoke(music, new object[] { song, interrupting, false });
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "sigil-music-invoke-failed-" + methodName,
                    "[Abyssal Protocol] Sigil encounter music method failed safely: " + methodName + " (" + ex.GetType().Name + ")",
                    999999);
            }

            return false;
        }
    }
}
