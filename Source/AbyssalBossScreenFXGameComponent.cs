using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class AbyssalBossScreenFXGameComponent : GameComponent
    {
        private Pawn activeBoss;
        private ABY_BossBarProfileDef activeBossBarProfile;
        private string activeBossDisplayLabelOverride;
        private Map effectMap;
        private float currentStrength;
        private int effectStartTick;

        private Map ritualPulseMap;
        private float ritualPulseStrength;

        private float nextBossMusicRealtime = -1f;
        private float bossSongExpectedEndRealtime = -1f;
        private float nextBossSongProbeRealtime = -1f;
        private int missingBossSongChecks;
        private bool vanillaSongRestoreQueued;
        private string activeBossSongDefName;
        private float activeBossSongLengthSeconds;
        private float activeBossSongStartDelaySeconds = 0.05f;
        private float activeBossSongEndLingerSeconds = 1.35f;
        private float bossMusicRestoreEarliestRealtime = -1f;
        private bool bossMusicOutroActive;

        private const string FallbackBossSongDefName = "ABY_ArchonBossBattleTheme";
        private const float FallbackBossSongLengthSeconds = 90.0f;
        private const float FallbackBossSongStartDelaySeconds = 0.05f;
        private const float FallbackBossSongEndLingerSeconds = 1.35f;
        private const float BossSongRestartLeadSeconds = 0.12f;
        private const float BossSongProbeDelaySeconds = 2.2f;
        private const float BossSongProbeIntervalSeconds = 0.65f;
        private const float BossSongRetryDelaySeconds = 1.0f;

        public AbyssalBossScreenFXGameComponent(Game game)
        {
        }

        public Pawn ActiveBoss => activeBoss;
        public ABY_BossBarProfileDef ActiveBossBarProfile => activeBossBarProfile;
        public string ActiveBossDisplayLabelOverride => activeBossDisplayLabelOverride;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref activeBoss, "activeBoss");
            Scribe_Defs.Look(ref activeBossBarProfile, "activeBossBarProfile");
            Scribe_Values.Look(ref activeBossDisplayLabelOverride, "activeBossDisplayLabelOverride");
            Scribe_References.Look(ref effectMap, "effectMap");
            Scribe_Values.Look(ref currentStrength, "currentStrength", 0f);
            Scribe_Values.Look(ref effectStartTick, "effectStartTick", 0);
            Scribe_References.Look(ref ritualPulseMap, "ritualPulseMap");
            Scribe_Values.Look(ref ritualPulseStrength, "ritualPulseStrength", 0f);
            Scribe_Values.Look(ref vanillaSongRestoreQueued, "vanillaSongRestoreQueued", false);
            Scribe_Values.Look(ref activeBossSongDefName, "activeBossSongDefName");
            Scribe_Values.Look(ref activeBossSongLengthSeconds, "activeBossSongLengthSeconds", 0f);
            Scribe_Values.Look(ref activeBossSongStartDelaySeconds, "activeBossSongStartDelaySeconds", FallbackBossSongStartDelaySeconds);
            Scribe_Values.Look(ref activeBossSongEndLingerSeconds, "activeBossSongEndLingerSeconds", FallbackBossSongEndLingerSeconds);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RefreshActiveBossBarProfile();
                RefreshActiveBossSongProfile();
            }
        }

        public void RegisterBoss(Pawn boss, string displayLabelOverride = null)
        {
            if (boss == null)
            {
                return;
            }

            activeBoss = boss;
            activeBossDisplayLabelOverride = displayLabelOverride;
            activeBossBarProfile = AbyssalBossBarUtility.ResolveProfileFor(boss);
            AbyssalBossBarRenderer.ResetVisualState();
            effectMap = boss.MapHeld;
            effectStartTick = Find.TickManager.TicksGame;
            currentStrength = Mathf.Max(currentStrength, 0.55f);
            RegisterRitualPulse(effectMap, 0.35f);
            RefreshActiveBossSongProfile();
            ResetBossMusicRuntimeState(clearSongProfile: false);
            ScheduleBossSongStart(activeBossSongStartDelaySeconds);
        }

        public void ClearBoss(Pawn boss = null)
        {
            if (boss != null && activeBoss != boss)
            {
                return;
            }

            activeBoss = null;
            activeBossBarProfile = null;
            activeBossDisplayLabelOverride = null;
            effectMap = null;
            currentStrength = 0f;
            AbyssalBossBarRenderer.ResetVisualState();
            QueueVanillaMusicRestore();
        }

        public bool TryGetActiveBossBarState(out ABY_BossBarState state)
        {
            state = null;
            if (!BossAlive())
            {
                return false;
            }

            RefreshActiveBossBarProfile();
            return activeBossBarProfile != null &&
                   AbyssalBossBarUtility.TryBuildState(activeBoss, activeBossBarProfile, activeBossDisplayLabelOverride, out state);
        }

        private void RefreshActiveBossBarProfile()
        {
            if (activeBoss == null)
            {
                activeBossBarProfile = null;
                return;
            }

            if (activeBossBarProfile != null && activeBossBarProfile.Matches(activeBoss))
            {
                return;
            }

            activeBossBarProfile = AbyssalBossBarUtility.ResolveProfileFor(activeBoss);
            RefreshActiveBossSongProfile();
        }

        private void RefreshActiveBossSongProfile()
        {
            string resolvedSongDefName = activeBossBarProfile?.bossSongDefName;
            float resolvedSongLengthSeconds = activeBossBarProfile?.bossSongLengthSeconds ?? 0f;
            float resolvedSongStartDelaySeconds = activeBossBarProfile?.bossSongStartDelaySeconds ?? FallbackBossSongStartDelaySeconds;
            float resolvedSongEndLingerSeconds = activeBossBarProfile?.bossSongEndLingerSeconds ?? FallbackBossSongEndLingerSeconds;

            if (resolvedSongDefName.NullOrEmpty())
            {
                resolvedSongDefName = FallbackBossSongDefName;
            }

            if (resolvedSongLengthSeconds <= 0.01f)
            {
                resolvedSongLengthSeconds = FallbackBossSongLengthSeconds;
            }

            if (resolvedSongStartDelaySeconds < 0f)
            {
                resolvedSongStartDelaySeconds = FallbackBossSongStartDelaySeconds;
            }

            if (resolvedSongEndLingerSeconds < 0f)
            {
                resolvedSongEndLingerSeconds = FallbackBossSongEndLingerSeconds;
            }

            activeBossSongDefName = resolvedSongDefName;
            activeBossSongLengthSeconds = resolvedSongLengthSeconds;
            activeBossSongStartDelaySeconds = Mathf.Max(0f, resolvedSongStartDelaySeconds);
            activeBossSongEndLingerSeconds = Mathf.Max(0f, resolvedSongEndLingerSeconds);
        }

        public void RegisterRitualPulse(Map map, float strength)
        {
            if (map == null || strength <= 0f)
            {
                return;
            }

            ritualPulseMap = map;
            ritualPulseStrength = Mathf.Max(ritualPulseStrength, Mathf.Clamp01(strength));
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            bool bossAlive = BossAlive();
            float targetStrength = bossAlive ? 1f : 0f;
            float step = bossAlive ? 0.012f : 0.022f;
            currentStrength = Mathf.MoveTowards(currentStrength, targetStrength, step);

            ritualPulseStrength = Mathf.MoveTowards(ritualPulseStrength, 0f, 0.01f);
            if (ritualPulseStrength <= 0.001f)
            {
                ritualPulseMap = null;
            }

            if (!bossAlive)
            {
                QueueVanillaMusicRestore();
                TryRestoreVanillaMusicIfNeeded();
            }

            if (!bossAlive && currentStrength <= 0.001f)
            {
                activeBoss = null;
                activeBossBarProfile = null;
                activeBossDisplayLabelOverride = null;
                effectMap = null;
                AbyssalBossBarRenderer.ResetVisualState();

                if (!vanillaSongRestoreQueued)
                {
                    ClearBossSongProfile();
                    ResetBossMusicRuntimeState(clearSongProfile: false);
                }
            }
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            HandleBossMusicRealtime();
            DrawOverlay();
            if (TryGetActiveBossBarState(out ABY_BossBarState state))
            {
                AbyssalBossBarRenderer.Draw(state);
            }
        }

        private void HandleBossMusicRealtime()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            if (!BossAlive() || effectMap == null)
            {
                return;
            }

            if (Find.CurrentMap != effectMap)
            {
                return;
            }

            SongDef song = activeBossSongDefName.NullOrEmpty() ? null : DefDatabase<SongDef>.GetNamedSilentFail(activeBossSongDefName);
            MusicManagerPlay music = Find.MusicManagerPlay;
            if (song == null || music == null || activeBossSongLengthSeconds <= 0.01f)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (nextBossMusicRealtime < 0f)
            {
                ScheduleBossSongStart(activeBossSongStartDelaySeconds);
            }

            if (now >= nextBossMusicRealtime)
            {
                if (TryStartBossSong(music, song, now))
                {
                    return;
                }

                nextBossMusicRealtime = now + BossSongRetryDelaySeconds;
                nextBossSongProbeRealtime = now + BossSongRetryDelaySeconds;
                return;
            }

            if (bossSongExpectedEndRealtime > 0f && now >= bossSongExpectedEndRealtime - BossSongRestartLeadSeconds)
            {
                TryStartBossSong(music, song, now);
                return;
            }

            if (nextBossSongProbeRealtime > 0f && now >= nextBossSongProbeRealtime)
            {
                if (IsSongAlreadyPlaying(music, song))
                {
                    missingBossSongChecks = 0;
                    nextBossSongProbeRealtime = now + BossSongProbeIntervalSeconds;
                    return;
                }

                missingBossSongChecks++;
                if (missingBossSongChecks >= 2)
                {
                    TryStartBossSong(music, song, now);
                    return;
                }

                nextBossSongProbeRealtime = now + 0.35f;
            }
        }

        private void ScheduleBossSongStart(float delaySeconds)
        {
            float now = Time.realtimeSinceStartup;
            nextBossMusicRealtime = now + Mathf.Max(0f, delaySeconds);
            bossSongExpectedEndRealtime = -1f;
            nextBossSongProbeRealtime = now + BossSongProbeDelaySeconds;
            missingBossSongChecks = 0;
            bossMusicRestoreEarliestRealtime = -1f;
            bossMusicOutroActive = false;
        }

        private void QueueVanillaMusicRestore()
        {
            if (!vanillaSongRestoreQueued || bossMusicOutroActive)
            {
                if (!vanillaSongRestoreQueued)
                {
                    return;
                }

                if (bossMusicOutroActive)
                {
                    return;
                }
            }

            float now = Time.realtimeSinceStartup;
            bossMusicRestoreEarliestRealtime = now + Mathf.Max(0f, activeBossSongEndLingerSeconds);
            bossMusicOutroActive = true;
            nextBossMusicRealtime = -1f;
            bossSongExpectedEndRealtime = -1f;
            nextBossSongProbeRealtime = -1f;
            missingBossSongChecks = 0;
        }

        private void ResetBossMusicRuntimeState(bool clearSongProfile)
        {
            nextBossMusicRealtime = -1f;
            bossSongExpectedEndRealtime = -1f;
            nextBossSongProbeRealtime = -1f;
            missingBossSongChecks = 0;
            vanillaSongRestoreQueued = false;
            bossMusicRestoreEarliestRealtime = -1f;
            bossMusicOutroActive = false;

            if (clearSongProfile)
            {
                ClearBossSongProfile();
            }
        }

        private void ClearBossSongProfile()
        {
            activeBossSongDefName = null;
            activeBossSongLengthSeconds = 0f;
            activeBossSongStartDelaySeconds = FallbackBossSongStartDelaySeconds;
            activeBossSongEndLingerSeconds = FallbackBossSongEndLingerSeconds;
        }

        private bool TryStartBossSong(MusicManagerPlay music, SongDef song, float now)
        {
            if (music == null || song == null)
            {
                return false;
            }

            bool started = TryInvokeSongMethod(music, "ForceStartSong", song, false)
                || TryInvokeSongMethod(music, "ForcePlaySong", song, false)
                || TryInvokeSongMethod(music, "StartNewSong", song, false);

            if (!started)
            {
                return false;
            }

            vanillaSongRestoreQueued = true;
            bossMusicOutroActive = false;
            bossMusicRestoreEarliestRealtime = -1f;
            nextBossMusicRealtime = now + activeBossSongLengthSeconds - BossSongRestartLeadSeconds;
            bossSongExpectedEndRealtime = now + activeBossSongLengthSeconds;
            nextBossSongProbeRealtime = now + BossSongProbeDelaySeconds;
            missingBossSongChecks = 0;
            return true;
        }

        private void TryRestoreVanillaMusicIfNeeded()
        {
            if (!vanillaSongRestoreQueued)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (bossMusicRestoreEarliestRealtime > 0f && now < bossMusicRestoreEarliestRealtime)
            {
                return;
            }

            MusicManagerPlay music = Find.MusicManagerPlay;
            if (music == null)
            {
                return;
            }

            SongDef bossSong = activeBossSongDefName.NullOrEmpty() ? null : DefDatabase<SongDef>.GetNamedSilentFail(activeBossSongDefName);
            if (bossSong == null)
            {
                ResetBossMusicRuntimeState(clearSongProfile: false);
                if (!BossAlive() && currentStrength <= 0.001f)
                {
                    ClearBossSongProfile();
                }
                return;
            }

            if (!IsSongAlreadyPlaying(music, bossSong))
            {
                ResetBossMusicRuntimeState(clearSongProfile: false);
                if (!BossAlive() && currentStrength <= 0.001f)
                {
                    ClearBossSongProfile();
                }
                return;
            }

            bool started = TryInvokeNoArgSongMethod(music, "StartNewSong")
                || TryInvokeNoArgSongMethod(music, "ChooseNextSong");

            if (started)
            {
                ResetBossMusicRuntimeState(clearSongProfile: false);
                if (!BossAlive() && currentStrength <= 0.001f)
                {
                    ClearBossSongProfile();
                }
            }
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

            if (value is SongDef directSong)
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

        private void DrawOverlay()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            Map currentMap = Find.CurrentMap;
            if (currentMap == null)
            {
                return;
            }

            float bossStrength = currentMap == effectMap ? currentStrength : 0f;
            float pulseStrength = currentMap == ritualPulseMap ? ritualPulseStrength : 0f;
            float totalStrength = Mathf.Clamp01(bossStrength + pulseStrength);
            if (totalStrength <= 0.001f)
            {
                return;
            }

            float t = effectStartTick > 0 ? (Find.TickManager.TicksGame - effectStartTick) / 60f : 0f;
            float pulse = 0.42f + 0.26f * Mathf.Sin(t * 3.6f);
            float fade = Mathf.SmoothStep(0f, 1f, totalStrength);
            float vignetteAlpha = fade * (0.12f + pulse * 0.08f);
            float bloomAlpha = fade * (0.05f + pulse * 0.06f);
            Color vignetteColor = new Color(0.65f, 0.06f, 0.06f, vignetteAlpha);
            Color bloomColor = new Color(0.90f, 0.12f, 0.10f, bloomAlpha);

            Rect fullRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
            Widgets.DrawBoxSolid(fullRect, vignetteColor);

            float glowWidth = UI.screenWidth * (0.82f + pulse * 0.06f);
            float glowHeight = UI.screenHeight * (0.62f + pulse * 0.05f);
            Rect glowRect = new Rect(
                (UI.screenWidth - glowWidth) * 0.5f,
                (UI.screenHeight - glowHeight) * 0.28f,
                glowWidth,
                glowHeight);
            Widgets.DrawBoxSolid(glowRect, bloomColor);
        }

        private bool BossAlive()
        {
            return activeBoss != null && !activeBoss.Destroyed && !activeBoss.Dead && activeBoss.Spawned && activeBoss.MapHeld != null;
        }
    }
}
