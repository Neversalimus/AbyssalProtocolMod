using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_DominionPocketMusicGameComponent : GameComponent
    {
        public const string HellPocketSongDefName = "ABY_DominionHellPocketTheme";

        private const float HellPocketSongLengthSeconds = 180.0f;
        private const float RestartLeadSeconds = 0.35f;
        private const float StartRetryDelaySeconds = 1.0f;
        private const float ProbeDelaySeconds = 1.5f;
        private const float ProbeIntervalSeconds = 0.75f;
        private const float RestoreRetryDelaySeconds = 1.0f;
        private const float PostLoadStartDelaySeconds = 3.0f;
        private const float PostLoadWarningGraceSeconds = 8.0f;
        private const int StartFailureWarningAttempts = 5;
        private const int WarningThrottleTicks = 900;

        private string activeSessionId;
        private int activePocketMapId = -1;
        private bool hellSongRestoreQueued;
        private bool forceRestoreRequested;
        private float nextStartRealtime = -1f;
        private float expectedEndRealtime = -1f;
        private float nextProbeRealtime = -1f;
        private float nextRestoreRetryRealtime = -1f;
        private float suppressStartWarningsUntilRealtime = -1f;
        private int missingSongChecks;
        private int startFailureCount;
        private int lastWarnTick = -999999;

        public ABY_DominionPocketMusicGameComponent(Game game)
        {
        }

        public static ABY_DominionPocketMusicGameComponent Get()
        {
            return Current.Game?.GetComponent<ABY_DominionPocketMusicGameComponent>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref activeSessionId, "activeSessionId");
            Scribe_Values.Look(ref activePocketMapId, "activePocketMapId", -1);
            Scribe_Values.Look(ref hellSongRestoreQueued, "hellSongRestoreQueued", false);
            Scribe_Values.Look(ref forceRestoreRequested, "forceRestoreRequested", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ResetRealtimeState(keepRestoreQueued: hellSongRestoreQueued || forceRestoreRequested);
                SuppressStartWarningsAfterLoad();
            }
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();

            if (Current.ProgramState != ProgramState.Playing || Find.MusicManagerPlay == null)
            {
                return;
            }

            try
            {
                UpdatePocketMusicRealtime();
            }
            catch (Exception ex)
            {
                WarnThrottled("[Abyssal Protocol] Dominion pocket music guard failed safely: " + ex.Message);
                QueueRestore();
            }
        }

        public static void NotifyPocketOpened(ABY_DominionPocketSession session)
        {
            if (session == null)
            {
                return;
            }

            ABY_DominionPocketMusicGameComponent component = Get();
            if (component != null)
            {
                component.activeSessionId = session.sessionId;
                component.activePocketMapId = session.pocketMapId;
                component.ScheduleStartSoon();
            }
        }

        public static void NotifyPocketClosed(ABY_DominionPocketSession session)
        {
            ABY_DominionPocketMusicGameComponent component = Get();
            if (component != null)
            {
                component.NotifyClosedInternal(session);
            }
        }

        public static bool IsSongManagedByActivePocketMusic(SongDef song)
        {
            if (song == null || song.defName != HellPocketSongDefName)
            {
                return false;
            }

            ABY_DominionPocketMusicGameComponent component = Get();
            return component != null && component.ShouldKeepHellMusicActiveNow();
        }

        private void UpdatePocketMusicRealtime()
        {
            SongDef song = ABY_DefCache.SongDefNamed(HellPocketSongDefName);
            MusicManagerPlay music = Find.MusicManagerPlay;
            if (song == null || music == null)
            {
                if (hellSongRestoreQueued || forceRestoreRequested)
                {
                    ResetRuntimeState(clearSession: false);
                }
                return;
            }

            bool shouldPlay = ShouldKeepHellMusicActiveNow();
            if (!AbyssalProtocolMod.Settings.enableDominionPocketMusic)
            {
                shouldPlay = false;
            }

            if (shouldPlay)
            {
                forceRestoreRequested = false;
                EnsureHellSongPlaying(music, song);
                return;
            }

            if (hellSongRestoreQueued || forceRestoreRequested || IsSongAlreadyPlaying(music, song))
            {
                TryRestoreVanillaMusic(music, song);
            }
            else
            {
                ResetRuntimeState(clearSession: true);
            }
        }

        private bool ShouldKeepHellMusicActiveNow()
        {
            if (Current.Game == null || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null)
            {
                return false;
            }

            ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
            if (runtime == null)
            {
                return false;
            }

            ABY_DominionPocketSession session;
            if (!runtime.TryGetSessionByPocketMap(Find.CurrentMap, out session) || session == null || !session.active)
            {
                return false;
            }

            activeSessionId = session.sessionId;
            activePocketMapId = session.pocketMapId;
            return true;
        }

        private void EnsureHellSongPlaying(MusicManagerPlay music, SongDef song)
        {
            float now = Time.realtimeSinceStartup;
            if (nextStartRealtime < 0f)
            {
                nextStartRealtime = now;
                expectedEndRealtime = -1f;
                nextProbeRealtime = now + ProbeDelaySeconds;
                missingSongChecks = 0;
            }

            if (IsSongAlreadyPlaying(music, song))
            {
                hellSongRestoreQueued = true;
                forceRestoreRequested = false;
                if (expectedEndRealtime <= 0f)
                {
                    expectedEndRealtime = now + HellPocketSongLengthSeconds;
                }
                if (nextProbeRealtime <= 0f)
                {
                    nextProbeRealtime = now + ProbeIntervalSeconds;
                }
                if (expectedEndRealtime > 0f && now >= expectedEndRealtime - RestartLeadSeconds)
                {
                    TryStartHellSong(music, song, now);
                }
                return;
            }

            if (now >= nextStartRealtime)
            {
                if (TryStartHellSong(music, song, now))
                {
                    return;
                }

                nextStartRealtime = now + StartRetryDelaySeconds;
                nextProbeRealtime = now + StartRetryDelaySeconds;
                return;
            }

            if (nextProbeRealtime > 0f && now >= nextProbeRealtime)
            {
                missingSongChecks++;
                if (missingSongChecks >= 2)
                {
                    nextStartRealtime = now;
                    return;
                }

                nextProbeRealtime = now + 0.35f;
            }
        }

        private bool TryStartHellSong(MusicManagerPlay music, SongDef song, float now)
        {
            if (music == null || song == null)
            {
                return false;
            }

            bool started;
            using (ABY_BossMusicUtility.AuthorizeBossSongStart(song))
            {
                started = TryInvokeSongMethod(music, "ForceStartSong", song, false)
                    || TryInvokeSongMethod(music, "ForcePlaySong", song, false)
                    || TryInvokeSongMethod(music, "StartNewSong", song, false)
                    || TryInvokeSongMethod(music, "PlaySong", song, false);
            }

            if (!started)
            {
                RegisterStartFailure(now);
                return false;
            }

            startFailureCount = 0;
            hellSongRestoreQueued = true;
            forceRestoreRequested = false;
            nextStartRealtime = now + HellPocketSongLengthSeconds - RestartLeadSeconds;
            expectedEndRealtime = now + HellPocketSongLengthSeconds;
            nextProbeRealtime = now + ProbeDelaySeconds;
            missingSongChecks = 0;
            return true;
        }

        private void NotifyClosedInternal(ABY_DominionPocketSession session)
        {
            if (session == null || session.sessionId.NullOrEmpty() || activeSessionId.NullOrEmpty() || session.sessionId == activeSessionId || session.pocketMapId == activePocketMapId)
            {
                QueueRestore();
            }
        }

        private void QueueRestore()
        {
            forceRestoreRequested = true;
            nextStartRealtime = -1f;
            expectedEndRealtime = -1f;
            nextProbeRealtime = -1f;
            missingSongChecks = 0;
            startFailureCount = 0;
        }

        private void TryRestoreVanillaMusic(MusicManagerPlay music, SongDef hellSong)
        {
            if (music == null)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (nextRestoreRetryRealtime > 0f && now < nextRestoreRetryRealtime)
            {
                return;
            }

            if (hellSong == null || !IsSongAlreadyPlaying(music, hellSong))
            {
                ResetRuntimeState(clearSession: true);
                return;
            }

            bool restored = TryInvokeNoArgSongMethod(music, "StartNewSong")
                || TryInvokeNoArgSongMethod(music, "ChooseNextSong");

            if (restored)
            {
                ResetRuntimeState(clearSession: true);
                return;
            }

            nextRestoreRetryRealtime = now + RestoreRetryDelaySeconds;
            WarnThrottled("[Abyssal Protocol] Dominion pocket music was still active after pocket closure; restore will retry.");
        }

        private void ResetRealtimeState(bool keepRestoreQueued)
        {
            nextStartRealtime = -1f;
            expectedEndRealtime = -1f;
            nextProbeRealtime = -1f;
            nextRestoreRetryRealtime = -1f;
            missingSongChecks = 0;
            startFailureCount = 0;
            if (!keepRestoreQueued)
            {
                hellSongRestoreQueued = false;
                forceRestoreRequested = false;
            }
        }

        private void ScheduleStartSoon()
        {
            if (!AbyssalProtocolMod.Settings.enableDominionPocketMusic)
            {
                return;
            }

            nextStartRealtime = Time.realtimeSinceStartup + 0.05f;
            expectedEndRealtime = -1f;
            nextProbeRealtime = Time.realtimeSinceStartup + ProbeDelaySeconds;
            suppressStartWarningsUntilRealtime = Time.realtimeSinceStartup + ProbeDelaySeconds;
            missingSongChecks = 0;
            startFailureCount = 0;
        }

        private void ResetRuntimeState(bool clearSession)
        {
            hellSongRestoreQueued = false;
            forceRestoreRequested = false;
            ResetRealtimeState(keepRestoreQueued: false);
            if (clearSession)
            {
                activeSessionId = null;
                activePocketMapId = -1;
            }
        }


        private void SuppressStartWarningsAfterLoad()
        {
            float now = Time.realtimeSinceStartup;
            suppressStartWarningsUntilRealtime = now + PostLoadWarningGraceSeconds;
            if (nextStartRealtime < 0f)
            {
                nextStartRealtime = now + PostLoadStartDelaySeconds;
            }

            if (nextProbeRealtime < 0f)
            {
                nextProbeRealtime = now + PostLoadStartDelaySeconds + ProbeDelaySeconds;
            }

            startFailureCount = 0;
        }

        private void RegisterStartFailure(float now)
        {
            startFailureCount++;
            if (now < suppressStartWarningsUntilRealtime || startFailureCount < StartFailureWarningAttempts)
            {
                return;
            }

            WarnThrottled("[Abyssal Protocol] Dominion pocket music has not accepted a start request after " + startFailureCount + " attempts; retrying quietly.");
        }

        private void WarnThrottled(string message)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now - lastWarnTick < WarningThrottleTicks)
            {
                return;
            }

            lastWarnTick = now;
            Log.Warning(message);
        }

        private static bool TryInvokeNoArgSongMethod(MusicManagerPlay music, string methodName)
        {
            if (music == null || string.IsNullOrEmpty(methodName))
            {
                return false;
            }

            MethodInfo[] methods = music.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 0)
                {
                    continue;
                }

                try
                {
                    method.Invoke(music, null);
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool TryInvokeSongMethod(MusicManagerPlay music, string methodName, SongDef song, bool interrupting)
        {
            if (music == null || song == null || string.IsNullOrEmpty(methodName))
            {
                return false;
            }

            MethodInfo[] methods = music.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method == null || method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                try
                {
                    if (parameters.Length == 1 && typeof(SongDef).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        method.Invoke(music, new object[] { song });
                        return true;
                    }

                    if (parameters.Length == 2 &&
                        typeof(SongDef).IsAssignableFrom(parameters[0].ParameterType) &&
                        parameters[1].ParameterType == typeof(bool))
                    {
                        method.Invoke(music, new object[] { song, interrupting });
                        return true;
                    }

                    if (parameters.Length == 3 &&
                        typeof(SongDef).IsAssignableFrom(parameters[0].ParameterType) &&
                        parameters[1].ParameterType == typeof(bool) &&
                        parameters[2].ParameterType == typeof(bool))
                    {
                        method.Invoke(music, new object[] { song, interrupting, false });
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool IsSongAlreadyPlaying(MusicManagerPlay music, SongDef targetSong)
        {
            if (music == null || targetSong == null)
            {
                return false;
            }

            SongDef currentSong = TryGetCurrentSong(music);
            if (SongsMatch(currentSong, targetSong))
            {
                return true;
            }

            try
            {
                List<object> visited = new List<object>();
                return ValueMatchesSongRecursive(music, targetSong, 0, visited);
            }
            catch
            {
                return false;
            }
        }

        private static SongDef TryGetCurrentSong(MusicManagerPlay music)
        {
            if (music == null)
            {
                return null;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            try
            {
                FieldInfo field = typeof(MusicManagerPlay).GetField("currentSong", flags);
                if (field != null)
                {
                    SongDef song = field.GetValue(music) as SongDef;
                    if (song != null)
                    {
                        return song;
                    }
                }
            }
            catch
            {
            }

            try
            {
                PropertyInfo property = typeof(MusicManagerPlay).GetProperty("CurrentSong", flags);
                if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(music, null) as SongDef;
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool SongsMatch(SongDef candidate, SongDef targetSong)
        {
            return candidate != null && targetSong != null &&
                (candidate == targetSong || candidate.defName == targetSong.defName);
        }

        private static bool ValueMatchesSongRecursive(object value, SongDef targetSong, int depth, List<object> visited)
        {
            if (value == null || targetSong == null || depth > 4)
            {
                return false;
            }

            if (ReferenceEquals(value, targetSong))
            {
                return true;
            }

            SongDef directSong = value as SongDef;
            if (SongsMatch(directSong, targetSong))
            {
                return true;
            }

            Type valueType = value.GetType();
            if (valueType.IsPrimitive || valueType.IsEnum || value is string || IsUnsafeReflectionTarget(valueType))
            {
                return false;
            }

            if (!valueType.IsValueType)
            {
                for (int i = 0; i < visited.Count; i++)
                {
                    if (ReferenceEquals(visited[i], value))
                    {
                        return false;
                    }
                }

                visited.Add(value);
            }

            System.Collections.IEnumerable enumerable = value as System.Collections.IEnumerable;
            if (enumerable != null)
            {
                int inspected = 0;
                foreach (object item in enumerable)
                {
                    if (ValueMatchesSongRecursive(item, targetSong, depth + 1, visited))
                    {
                        return true;
                    }

                    inspected++;
                    if (inspected >= 64)
                    {
                        break;
                    }
                }
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = valueType.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field == null || field.IsStatic || IsUnsafeReflectionTarget(field.FieldType))
                {
                    continue;
                }

                object fieldValue;
                try
                {
                    fieldValue = field.GetValue(value);
                }
                catch
                {
                    continue;
                }

                if (ValueMatchesSongRecursive(fieldValue, targetSong, depth + 1, visited))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUnsafeReflectionTarget(Type type)
        {
            if (type == null)
            {
                return true;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return true;
            }

            string ns = type.Namespace;
            if (ns == null)
            {
                return false;
            }

            return ns.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                ns.StartsWith("System.Reflection", StringComparison.Ordinal) ||
                ns.StartsWith("System.Runtime", StringComparison.Ordinal);
        }

    }
}
