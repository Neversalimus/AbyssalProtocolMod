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
        private float introSurgeStrength;
        private int nextBossMapEffectTick;
        private int lastKnownPhase = -1;
        private float phaseSurgeStrength;
        private float outroSurgeStrength;
        private bool outroTriggered;
        private int titleCardStartTick = -999999;
        private int titleCardDurationTicks;
        private string titleCardTitle;
        private string titleCardSubtitle;
        private string titleCardKind;

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

        private const int BossBarStateRefreshIntervalTicks = 6;
        private int cachedBossBarStateTick = -1;
        private Pawn cachedBossBarStateBoss;
        private ABY_BossBarProfileDef cachedBossBarStateProfile;
        private string cachedBossBarStateLabelOverride;
        private bool cachedBossBarStateAvailable;
        private ABY_BossBarState cachedBossBarState;

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
            Scribe_Values.Look(ref introSurgeStrength, "introSurgeStrength", 0f);
            Scribe_Values.Look(ref nextBossMapEffectTick, "nextBossMapEffectTick", 0);
            Scribe_Values.Look(ref lastKnownPhase, "lastKnownPhase", -1);
            Scribe_Values.Look(ref phaseSurgeStrength, "phaseSurgeStrength", 0f);
            Scribe_Values.Look(ref outroSurgeStrength, "outroSurgeStrength", 0f);
            Scribe_Values.Look(ref outroTriggered, "outroTriggered", false);
            Scribe_Values.Look(ref titleCardStartTick, "titleCardStartTick", -999999);
            Scribe_Values.Look(ref titleCardDurationTicks, "titleCardDurationTicks", 0);
            Scribe_Values.Look(ref titleCardTitle, "titleCardTitle");
            Scribe_Values.Look(ref titleCardSubtitle, "titleCardSubtitle");
            Scribe_Values.Look(ref titleCardKind, "titleCardKind");
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
            introSurgeStrength = 1f;
            phaseSurgeStrength = 0f;
            outroSurgeStrength = 0f;
            outroTriggered = false;
            lastKnownPhase = ResolveCurrentPresentationPhase();
            nextBossMapEffectTick = effectStartTick + 8;
            RegisterRitualPulse(effectMap, 0.35f);
            ABY_BossPresentationUtility.SpawnIntroBurst(boss, activeBossBarProfile);
            StartTitleCard(
                ABY_BossPresentationDirector.ResolveIntroTitle(boss, activeBossBarProfile),
                ABY_BossPresentationDirector.ResolveIntroSubtitle(boss, activeBossBarProfile),
                ABY_BossPresentationDirector.IntroTitleDurationTicks,
                "intro");
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

            if (activeBoss != null && AbyssalProtocolMod.Settings.enableBossPresentationTimeline && !outroTriggered)
            {
                TriggerOutroPresentation();
                QueueVanillaMusicRestore();
                return;
            }

            activeBoss = null;
            activeBossBarProfile = null;
            activeBossDisplayLabelOverride = null;
            effectMap = null;
            currentStrength = 0f;
            introSurgeStrength = 0f;
            phaseSurgeStrength = 0f;
            outroSurgeStrength = 0f;
            lastKnownPhase = -1;
            ClearTitleCard();
            AbyssalBossBarRenderer.ResetVisualState();
            QueueVanillaMusicRestore();
        }

        public bool TryGetActiveBossBarState(out ABY_BossBarState state)
        {
            state = null;
            if (!BossAlive())
            {
                CacheBossBarState(false, null, -1);
                return false;
            }

            RefreshActiveBossBarProfile();
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : -1;
            if (cachedBossBarStateTick >= ticksGame
                && cachedBossBarStateBoss == activeBoss
                && cachedBossBarStateProfile == activeBossBarProfile
                && cachedBossBarStateLabelOverride == activeBossDisplayLabelOverride)
            {
                state = cachedBossBarState;
                return cachedBossBarStateAvailable;
            }

            bool available = activeBossBarProfile != null &&
                             AbyssalBossBarUtility.TryBuildState(activeBoss, activeBossBarProfile, activeBossDisplayLabelOverride, out state);
            int cacheTick = ticksGame >= 0 ? ticksGame + BossBarStateRefreshIntervalTicks : ticksGame;
            CacheBossBarState(available, state, cacheTick);
            return available;
        }

        private void CacheBossBarState(bool available, ABY_BossBarState state, int ticksGame)
        {
            cachedBossBarStateTick = ticksGame;
            cachedBossBarStateBoss = activeBoss;
            cachedBossBarStateProfile = activeBossBarProfile;
            cachedBossBarStateLabelOverride = activeBossDisplayLabelOverride;
            cachedBossBarStateAvailable = available;
            cachedBossBarState = state;
        }

        private void InvalidateBossBarStateCache()
        {
            cachedBossBarStateTick = -1;
            cachedBossBarStateBoss = null;
            cachedBossBarStateProfile = null;
            cachedBossBarStateLabelOverride = null;
            cachedBossBarStateAvailable = false;
            cachedBossBarState = null;
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
                resolvedSongDefName = ABY_SigilEncounterMusicUtility.ResolveSongDefNameForPawnKindDefName(activeBoss?.kindDef?.defName);
                float sigilSongLengthSeconds = ABY_SigilEncounterMusicUtility.ResolveSongLengthSeconds(resolvedSongDefName);
                if (sigilSongLengthSeconds > 0.01f)
                {
                    resolvedSongLengthSeconds = sigilSongLengthSeconds;
                }
            }

            if (resolvedSongDefName.NullOrEmpty())
            {
                resolvedSongDefName = FallbackBossSongDefName;
            }

            if (resolvedSongLengthSeconds <= 0.01f)
            {
                resolvedSongLengthSeconds = ABY_SigilEncounterMusicUtility.ResolveSongLengthSeconds(resolvedSongDefName);
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
            introSurgeStrength = Mathf.MoveTowards(introSurgeStrength, 0f, 0.016f);
            phaseSurgeStrength = Mathf.MoveTowards(phaseSurgeStrength, 0f, 0.014f);
            outroSurgeStrength = Mathf.MoveTowards(outroSurgeStrength, 0f, 0.012f);
            if (ritualPulseStrength <= 0.001f)
            {
                ritualPulseMap = null;
            }

            DetectPhaseTransition(bossAlive);
            TickBossPresentationMapEffects(bossAlive);

            if (!bossAlive)
            {
                if (activeBoss != null && !outroTriggered && AbyssalProtocolMod.Settings.enableBossPresentationTimeline)
                {
                    TriggerOutroPresentation();
                }

                QueueVanillaMusicRestore();
                TryRestoreVanillaMusicIfNeeded();
            }

            if (!bossAlive && currentStrength <= 0.001f && outroSurgeStrength <= 0.001f && !TitleCardActive())
            {
                activeBoss = null;
                activeBossBarProfile = null;
                activeBossDisplayLabelOverride = null;
                effectMap = null;
                lastKnownPhase = -1;
                outroTriggered = false;
                ClearTitleCard();
                AbyssalBossBarRenderer.ResetVisualState();

                if (!vanillaSongRestoreQueued)
                {
                    ClearBossSongProfile();
                    ResetBossMusicRuntimeState(clearSongProfile: false);
                }
            }
        }

        private void DetectPhaseTransition(bool bossAlive)
        {
            if (!bossAlive || activeBoss == null || !AbyssalProtocolMod.Settings.enableBossPresentationTimeline)
            {
                return;
            }

            int phase = ResolveCurrentPresentationPhase();
            if (phase <= 0)
            {
                return;
            }

            if (lastKnownPhase <= 0)
            {
                lastKnownPhase = phase;
                return;
            }

            if (phase <= lastKnownPhase)
            {
                return;
            }

            lastKnownPhase = phase;
            phaseSurgeStrength = 1f;
            introSurgeStrength = Mathf.Max(introSurgeStrength, 0.35f);
            ABY_BossPresentationUtility.SpawnPhaseTransitionBurst(activeBoss, activeBossBarProfile, phase);
            StartTitleCard(
                ABY_BossPresentationDirector.ResolvePhaseTitle(activeBoss, activeBossBarProfile, phase),
                ABY_BossPresentationDirector.ResolvePhaseSubtitle(activeBoss, activeBossBarProfile, phase),
                ABY_BossPresentationDirector.PhaseTitleDurationTicks,
                "phase");
        }

        private int ResolveCurrentPresentationPhase()
        {
            if (activeBoss == null || activeBossBarProfile == null)
            {
                return -1;
            }

            if (AbyssalBossBarUtility.TryBuildState(activeBoss, activeBossBarProfile, activeBossDisplayLabelOverride, out ABY_BossBarState state) && state != null)
            {
                return state.currentPhase;
            }

            return -1;
        }

        private void TriggerOutroPresentation()
        {
            if (activeBoss == null || outroTriggered)
            {
                return;
            }

            outroTriggered = true;
            outroSurgeStrength = 1f;
            currentStrength = Mathf.Max(currentStrength, 0.62f);
            ABY_BossPresentationUtility.SpawnOutroBurst(activeBoss, activeBossBarProfile);
            StartTitleCard(
                ABY_BossPresentationDirector.ResolveOutroTitle(activeBoss, activeBossBarProfile),
                ABY_BossPresentationDirector.ResolveOutroSubtitle(activeBoss, activeBossBarProfile),
                ABY_BossPresentationDirector.OutroTitleDurationTicks,
                "outro");
        }

        private void StartTitleCard(string title, string subtitle, int durationTicks, string kind)
        {
            if (!AbyssalProtocolMod.Settings.enableBossPresentationTimeline || !AbyssalProtocolMod.Settings.enableBossPresentationTitleCards)
            {
                return;
            }

            titleCardTitle = title;
            titleCardSubtitle = subtitle;
            titleCardDurationTicks = Mathf.Max(1, durationTicks);
            titleCardKind = kind;
            titleCardStartTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        private bool TitleCardActive()
        {
            if (titleCardTitle.NullOrEmpty() || titleCardDurationTicks <= 0 || Find.TickManager == null)
            {
                return false;
            }

            int age = Find.TickManager.TicksGame - titleCardStartTick;
            return age >= 0 && age <= titleCardDurationTicks;
        }

        private void ClearTitleCard()
        {
            titleCardTitle = null;
            titleCardSubtitle = null;
            titleCardKind = null;
            titleCardStartTick = -999999;
            titleCardDurationTicks = 0;
        }

        private void TickBossPresentationMapEffects(bool bossAlive)
        {
            if (!bossAlive || activeBoss == null || activeBoss.MapHeld == null || Find.TickManager == null)
            {
                return;
            }

            if (!AbyssalProtocolMod.Settings.enableBossMapPresentationEffects || AbyssalProtocolMod.Settings.reducedMotion)
            {
                return;
            }

            if (Find.CurrentMap != activeBoss.MapHeld)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            if (nextBossMapEffectTick <= 0)
            {
                nextBossMapEffectTick = tick + ABY_BossPresentationUtility.ResolveMapEffectIntervalTicks(activeBoss, activeBossBarProfile);
                return;
            }

            if (tick < nextBossMapEffectTick)
            {
                return;
            }

            float strength = Mathf.Clamp01(currentStrength + introSurgeStrength * 0.55f);
            ABY_BossPresentationUtility.SpawnAmbientMapEffects(activeBoss, activeBossBarProfile, strength);
            nextBossMapEffectTick = tick + ABY_BossPresentationUtility.ResolveMapEffectIntervalTicks(activeBoss, activeBossBarProfile);
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();

            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            // Keep boss UI work repaint-bound. The previous generic OnGUI path rebuilt boss-bar
            // state on every GUI event while the game tick advanced, which could produce severe
            // main-thread drops during active boss fights but look normal on pause.
            if (currentEvent.type == EventType.MouseDown)
            {
                AbyssalBossBarRenderer.HandleInput(currentEvent);
                return;
            }

            if (currentEvent.type != EventType.Repaint)
            {
                return;
            }

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

            SongDef song = ABY_DefCache.SongDefNamed(activeBossSongDefName);
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

            bool started;
            using (ABY_BossMusicUtility.AuthorizeBossSongStart(song))
            {
                started = TryInvokeSongMethod(music, "ForceStartSong", song, false)
                    || TryInvokeSongMethod(music, "ForcePlaySong", song, false)
                    || TryInvokeSongMethod(music, "StartNewSong", song, false);
            }

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

            SongDef bossSong = ABY_DefCache.SongDefNamed(activeBossSongDefName);
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
            float totalStrength = Mathf.Clamp01(bossStrength + pulseStrength + introSurgeStrength * 0.5f + phaseSurgeStrength * 0.45f + outroSurgeStrength * 0.55f);
            if (totalStrength <= 0.001f)
            {
                return;
            }

            ABY_BossPresentationUtility.DrawBossScreenOverlay(
                activeBoss,
                activeBossBarProfile,
                bossStrength,
                pulseStrength,
                Mathf.Max(introSurgeStrength, phaseSurgeStrength, outroSurgeStrength),
                effectStartTick);

            ABY_BossPresentationUtility.DrawTitleCard(
                activeBoss,
                activeBossBarProfile,
                titleCardTitle,
                titleCardSubtitle,
                titleCardStartTick,
                titleCardDurationTicks,
                titleCardKind);
        }

        private bool BossAlive()
        {
            return activeBoss != null && !activeBoss.Destroyed && !activeBoss.Dead && activeBoss.Spawned && activeBoss.MapHeld != null;
        }
    }
}
