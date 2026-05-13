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
        private const int WarningThrottleTicks = 900;

        private string activeSessionId;
        private int activePocketMapId = -1;
        private bool hellSongRestoreQueued;
        private bool forceRestoreRequested;
        private float nextStartRealtime = -1f;
        private float expectedEndRealtime = -1f;
        private float nextProbeRealtime = -1f;
        private float nextRestoreRetryRealtime = -1f;
        private int missingSongChecks;
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
                WarnThrottled("[Abyssal Protocol] Could not start Dominion pocket music; will retry.");
                return false;
            }

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
            missingSongChecks = 0;
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

            List<object> visited = new List<object>();
            return ValueMatchesSongRecursive(music, targetSong, 0, visited);
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
            if (directSong != null)
            {
                return directSong == targetSong || directSong.defName == targetSong.defName;
            }

            Type valueType = value.GetType();
            if (valueType.IsPrimitive || valueType.IsEnum || value is string)
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

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = valueType.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field == null)
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

            PropertyInfo[] properties = valueType.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                object propertyValue;
                try
                {
                    propertyValue = property.GetValue(value, null);
                }
                catch
                {
                    continue;
                }

                if (ValueMatchesSongRecursive(propertyValue, targetSong, depth + 1, visited))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
