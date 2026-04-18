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

        private const string BossSongDefName = "ABY_ArchonBossBattleTheme";
        private const float BossSongLengthSeconds = 115.9f;
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

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RefreshActiveBossBarProfile();
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
            effectMap = boss.MapHeld;
            effectStartTick = Find.TickManager.TicksGame;
            currentStrength = Mathf.Max(currentStrength, 0.55f);
            RegisterRitualPulse(effectMap, 0.35f);
            ScheduleBossSongStart(0.05f);
            vanillaSongRestoreQueued = false;
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
            ResetBossMusicState();
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
                TryRestoreVanillaMusicIfNeeded();
            }

            if (!bossAlive && currentStrength <= 0.001f)
            {
                activeBoss = null;
                activeBossBarProfile = null;
                activeBossDisplayLabelOverride = null;
                effectMap = null;
                ResetBossMusicState();
            }
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            HandleBossMusicRealtime();
            DrawOverlay();
        }

        private void HandleBossMusicRealtime()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            if (!BossAlive() || effectMap == null)
            {
                ResetBossMusicState();
                return;
            }

            if (Find.CurrentMap != effectMap)
            {
                return;
            }

            SongDef song = DefDatabase<SongDef>.GetNamedSilentFail(BossSongDefName);
            MusicManagerPlay music = Find.MusicManagerPlay;
            if (song == null || music == null)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (nextBossMusicRealtime < 0f)
            {
                ScheduleBossSongStart(0.05f);
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
        }

        private void ResetBossMusicState()
        {
            nextBossMusicRealtime = -1f;
            bossSongExpectedEndRealtime = -1f;
            nextBossSongProbeRealtime = -1f;
            missingBossSongChecks = 0;
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
            nextBossMusicRealtime = now + BossSongLengthSeconds - BossSongRestartLeadSeconds;
            bossSongExpectedEndRealtime = now + BossSongLengthSeconds;
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

            MusicManagerPlay music = Find.MusicManagerPlay;
            if (music == null)
            {
                return;
            }

            SongDef bossSong = DefDatabase<SongDef>.GetNamedSilentFail(BossSongDefName);
            if (bossSong == null)
            {
                vanillaSongRestoreQueued = false;
                return;
            }

            if (!IsSongAlreadyPlaying(music, bossSong))
            {
                vanillaSongRestoreQueued = false;
                return;
            }

            bool started = TryInvokeNoArgSongMethod(music, "StartNewSong")
                || TryInvokeNoArgSongMethod(music, "ChooseNextSong");

            if (started)
            {
                vanillaSongRestoreQueued = false;
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

            float time = Find.TickManager.TicksGame * 0.045f;
            float pulseA = 0.5f + 0.5f * Mathf.Sin(time);
            float pulseB = 0.5f + 0.5f * Mathf.Sin(time * 1.7f + 1.2f);
            float pulseC = 0.5f + 0.5f * Mathf.Sin(time * 2.6f + 0.4f);

            float screenW = UI.screenWidth;
            float screenH = UI.screenHeight;
            Rect full = new Rect(0f, 0f, screenW, screenH);

            Color darkHeat = new Color(0.33f, 0.04f, 0.01f, totalStrength * (0.10f + pulseA * 0.05f));
            Color fireGlow = new Color(1f, 0.28f, 0.03f, totalStrength * (0.05f + pulseB * 0.05f));
            Color innerHeat = new Color(1f, 0.78f, 0.14f, totalStrength * (0.02f + pulseC * 0.04f));

            Widgets.DrawBoxSolid(full, darkHeat);
            Widgets.DrawBoxSolid(full, fireGlow);
            Widgets.DrawBoxSolid(full, innerHeat);

            float outerX = screenW * Mathf.Lerp(0.06f, 0.10f, totalStrength);
            float outerY = screenH * Mathf.Lerp(0.06f, 0.10f, totalStrength);
            float innerX = screenW * Mathf.Lerp(0.03f, 0.05f, totalStrength);
            float innerY = screenH * Mathf.Lerp(0.03f, 0.05f, totalStrength);

            Color edgeDark = new Color(0.45f, 0.06f, 0.01f, totalStrength * (0.14f + pulseA * 0.08f));
            Color edgeHot = new Color(1f, 0.45f, 0.08f, totalStrength * (0.05f + pulseB * 0.05f));

            DrawSoftEdgeFrame(outerX, outerY, edgeDark, screenW, screenH, 6);
            DrawSoftEdgeFrame(innerX, innerY, edgeHot, screenW, screenH, 5);
        }

        private static void DrawSoftEdgeFrame(float thicknessX, float thicknessY, Color color, float screenW, float screenH, int layers)
        {
            if (thicknessX <= 0f || thicknessY <= 0f || layers <= 0)
            {
                return;
            }

            for (int i = 0; i < layers; i++)
            {
                float t = (float)(i + 1) / layers;
                float layerThicknessX = Mathf.Lerp(thicknessX, 2f, t);
                float layerThicknessY = Mathf.Lerp(thicknessY, 2f, t);
                Color layerColor = color;
                layerColor.a *= (1f - t) * 0.85f;

                if (layerColor.a <= 0.001f)
                {
                    continue;
                }

                Widgets.DrawBoxSolid(new Rect(0f, 0f, screenW, layerThicknessY), layerColor);
                Widgets.DrawBoxSolid(new Rect(0f, screenH - layerThicknessY, screenW, layerThicknessY), layerColor);
                Widgets.DrawBoxSolid(new Rect(0f, layerThicknessY, layerThicknessX, screenH - layerThicknessY * 2f), layerColor);
                Widgets.DrawBoxSolid(new Rect(screenW - layerThicknessX, layerThicknessY, layerThicknessX, screenH - layerThicknessY * 2f), layerColor);
            }
        }

        private bool BossAlive()
        {
            return activeBoss != null &&
                   !activeBoss.Destroyed &&
                   !activeBoss.Dead &&
                   activeBoss.Spawned &&
                   activeBoss.MapHeld != null;
        }
    }
}
