using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class AbyssalProtocolMod : Mod
    {
        private static AbyssalProtocolModSettings settings;
        private static AbyssalProtocolMod instance;
        private Vector2 settingsScroll;

        public static AbyssalProtocolModSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = LoadedModManager.GetMod<AbyssalProtocolMod>()?.GetSettings<AbyssalProtocolModSettings>() ?? new AbyssalProtocolModSettings();
                    settings.ClampValues();
                }

                return settings;
            }
        }

        public AbyssalProtocolMod(ModContentPack content) : base(content)
        {
            instance = this;
            settings = GetSettings<AbyssalProtocolModSettings>();
            settings.ClampValues();
            LongEventHandler.ExecuteWhenFinished(ABY_WeaponChargeSoundUtility.ApplyCurrentSettings);
            LongEventHandler.ExecuteWhenFinished(ABY_EncounterValidationUtility.LogStartupValidationIfEnabled);
        }

        public static void SaveNow()
        {
            instance?.WriteSettings();
        }

        public override string SettingsCategory()
        {
            return "ABY_ModSettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            AbyssalProtocolModSettings s = Settings;
            s.ClampValues();

            Rect viewRect = new Rect(0f, 0f, inRect.width - 18f, 1620f);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
            try
            {
                Listing_Standard list = new Listing_Standard();
                list.Begin(viewRect);

            list.Gap(4f);
            DrawDifficultySection(list, s);
            list.GapLine();
            DrawUIStyleSection(list, s);
            list.GapLine();
            DrawPerformanceSection(list, s);
            list.GapLine();

            bool previousChargeSounds = s.enableWeaponChargeSounds;
            list.CheckboxLabeled("ABY_WeaponChargeSounds_Enable".Translate(), ref s.enableWeaponChargeSounds, "ABY_WeaponChargeSounds_EnableDesc".Translate());
            if (previousChargeSounds != s.enableWeaponChargeSounds)
            {
                ABY_WeaponChargeSoundUtility.ApplyCurrentSettings();
            }

            list.GapLine();
            DrawDiagnosticsSection(list, s);
            list.GapLine();
            DrawExperimentalSystemsSection(list, s);
            list.GapLine();

            list.CheckboxLabeled("ABY_BossBar_Enable".Translate(), ref s.enableBossBars, "ABY_BossBar_EnableDesc".Translate());
            list.CheckboxLabeled("ABY_BossBar_ShowHealthNumbers".Translate(), ref s.showHealthNumbers, "ABY_BossBar_ShowHealthNumbersDesc".Translate());
            list.CheckboxLabeled("ABY_MiniBossHealthBar_Enable".Translate(), ref s.enableMiniBossHealthBars, "ABY_MiniBossHealthBar_EnableDesc".Translate());
            list.CheckboxLabeled("ABY_BossBar_ShowPhaseMarkers".Translate(), ref s.showPhaseMarkers, "ABY_BossBar_ShowPhaseMarkersDesc".Translate());
            list.CheckboxLabeled("ABY_BossBar_ShowPhaseLabel".Translate(), ref s.showPhaseLabel, "ABY_BossBar_ShowPhaseLabelDesc".Translate());
            list.CheckboxLabeled("ABY_BossBar_ShowSecondaryBars".Translate(), ref s.showSecondaryBars, "ABY_BossBar_ShowSecondaryBarsDesc".Translate());
            list.CheckboxLabeled("ABY_BossBar_ShowCalibrationButton".Translate(), ref s.showCalibrationButton, "ABY_BossBar_ShowCalibrationButtonDesc".Translate());
            list.CheckboxLabeled("ABY_BossBar_ReducedMotion".Translate(), ref s.reducedMotion, "ABY_BossBar_ReducedMotionDesc".Translate());
            list.GapLine();

            DrawAnchorSelector(list, s);
            DrawSlider(list, "ABY_BossBar_Width".Translate(s.width.ToString("F0")), ref s.width, 320f, 1080f);
            DrawSlider(list, "ABY_BossBar_Height".Translate(s.height.ToString("F0")), ref s.height, 22f, 84f);
            DrawSlider(list, "ABY_BossBar_IconSize".Translate(s.iconSize.ToString("F0")), ref s.iconSize, 40f, 156f);
            DrawSlider(list, "ABY_BossBar_Gap".Translate(s.gap.ToString("F0")), ref s.gap, 0f, 48f);
            DrawSlider(list, "ABY_BossBar_Scale".Translate(s.globalScale.ToString("F2")), ref s.globalScale, 0.70f, 1.80f);
            DrawSlider(list, "ABY_BossBar_OffsetX".Translate(s.offsetX.ToString("F0")), ref s.offsetX, -1200f, 1200f);
            DrawSlider(list, "ABY_BossBar_OffsetY".Translate(s.offsetY.ToString("F0")), ref s.offsetY, -700f, 700f);
            DrawSlider(list, "ABY_BossBar_SafeMargin".Translate(s.safeMargin.ToString("F0")), ref s.safeMargin, 0f, 120f);
            list.Gap(10f);

            Rect buttonRow = list.GetRect(32f);
            Rect calibrateRect = new Rect(buttonRow.x, buttonRow.y, (buttonRow.width - 10f) * 0.58f, 32f);
            Rect resetRect = new Rect(calibrateRect.xMax + 10f, buttonRow.y, buttonRow.width - calibrateRect.width - 10f, 32f);
            if (AbyssalStyledWidgets.TextButton(calibrateRect, "ABY_BossBar_OpenCalibration".Translate()))
            {
                Window_ABY_BossBarCalibration.OpenWindow();
            }

            if (AbyssalStyledWidgets.TextButton(resetRect, "ABY_BossBar_ResetDefaults".Translate()))
            {
                s.ResetToDefaults();
            }

            list.Gap(8f);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.84f, 0.78f, 0.72f, 1f);
            Widgets.Label(list.GetRect(42f), "ABY_BossBar_SettingsHint".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

                list.End();
            }
            finally
            {
                Widgets.EndScrollView();
            }
            s.ClampValues();
        }

        public override void WriteSettings()
        {
            Settings.ClampValues();
            base.WriteSettings();
            ABY_WeaponChargeSoundUtility.ApplyCurrentSettings();
        }


        private static void DrawUIStyleSection(Listing_Standard list, AbyssalProtocolModSettings settingsData)
        {
            Widgets.Label(list.GetRect(24f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_UIStyleSettingsHeader", "Abyssal UI style"));
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.84f, 0.78f, 0.72f, 1f);
            Widgets.Label(ABY_UIPolishUtility.TextRect(list.GetRect(40f)), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_UIStyleSettingsDesc", "Choose the shared skin used by Forge, Summoning and other Abyssal custom consoles. Classic keeps the restrained procedural interface; Enhanced uses the new sliced UI kit."));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect row = list.GetRect(34f);
            float gap = 8f;
            float cellWidth = (row.width - gap) * 0.5f;
            DrawUIStyleButton(new Rect(row.x, row.y, cellWidth, 34f), settingsData, ABY_UIStyle.Classic, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_UIStyle_Classic", "Classic"));
            DrawUIStyleButton(new Rect(row.x + cellWidth + gap, row.y, cellWidth, 34f), settingsData, ABY_UIStyle.Enhanced, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_UIStyle_Enhanced", "Enhanced"));

            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_UIStyle_ReduceAnimation", "Reduce Abyssal UI animation"), ref settingsData.reduceAbyssalUIAnimation, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_UIStyle_ReduceAnimationDesc", "Disables optional scanlines, socket pulses and enhanced UI accent sweeps. This is separate from boss bar reduced motion."));
            list.Gap(4f);
        }

        private static void DrawUIStyleButton(Rect rect, AbyssalProtocolModSettings settingsData, ABY_UIStyle targetStyle, string label)
        {
            bool active = settingsData.uiStyle == targetStyle;
            if (AbyssalStyledWidgets.TextButton(rect, label, true, active))
            {
                settingsData.uiStyle = targetStyle;
            }
        }


        private static void DrawPerformanceSection(Listing_Standard list, AbyssalProtocolModSettings settingsData)
        {
            Widgets.Label(list.GetRect(24f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_PerformanceSettingsHeader", "Abyssal performance / visual intensity"));
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.84f, 0.78f, 0.72f, 1f);
            Widgets.Label(ABY_UIPolishUtility.TextRect(list.GetRect(44f)), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_PerformanceSettingsDesc", "Performance presets reduce optional VFX density and visual motion without changing gameplay, rewards, AI, or boss logic. Use Minimal on low-end laptops or heavy modpacks."));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect row = list.GetRect(34f);
            float gap = 8f;
            float cellWidth = (row.width - gap * 2f) / 3f;
            DrawPerformancePresetButton(new Rect(row.x, row.y, cellWidth, 34f), settingsData, ABY_VisualIntensity.Full);
            DrawPerformancePresetButton(new Rect(row.x + cellWidth + gap, row.y, cellWidth, 34f), settingsData, ABY_VisualIntensity.Reduced);
            DrawPerformancePresetButton(new Rect(row.x + (cellWidth + gap) * 2f, row.y, cellWidth, 34f), settingsData, ABY_VisualIntensity.Minimal);

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.88f, 0.82f, 0.78f, 1f);
            Widgets.Label(ABY_UIPolishUtility.TextRect(list.GetRect(42f)), ABY_PerformanceSettingsUtility.ResolveDescription(settingsData.visualIntensity));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_DominionAmbient", "Enable Dominion ambient VFX"), ref settingsData.enableDominionAmbientVfx, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_DominionAmbientDesc", "Controls optional Dominion Slice ambient pulses, cohesion accents, edge effects, and decorative map atmosphere. Combat-critical mechanics remain active."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_DevAudit", "Enable performance audit window button"), ref settingsData.enableDevPerformanceAuditWindow, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_DevAuditDesc", "Shows a development-only audit button in these settings for checking active Abyssal VFX, pawns, map state, and performance toggles."));

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.78f, 0.92f, 1f, 1f);
            Widgets.Label(list.GetRect(24f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_CurrentScale", "Current VFX scale: {0}; sample interval 120 -> {1} ticks", ABY_PerformanceSettingsUtility.ResolveVfxIntensityScale(settingsData).ToString("F2"), ABY_PerformanceSettingsUtility.ScaleVfxInterval(120, settingsData).ToString()));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            list.Gap(4f);
        }

        private static void DrawPerformancePresetButton(Rect rect, AbyssalProtocolModSettings settingsData, ABY_VisualIntensity intensity)
        {
            bool active = settingsData.visualIntensity == intensity;
            if (AbyssalStyledWidgets.TextButton(rect, ABY_PerformanceSettingsUtility.ResolveLabel(intensity), true, active))
            {
                ABY_PerformanceSettingsUtility.ApplyPreset(settingsData, intensity);
            }
        }


        private static void DrawDiagnosticsSection(Listing_Standard list, AbyssalProtocolModSettings settingsData)
        {
            Widgets.Label(list.GetRect(24f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DiagnosticsSettingsHeader", "Stability / diagnostics / UI polish"));
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.84f, 0.78f, 0.72f, 1f);
            Widgets.Label(ABY_UIPolishUtility.TextRect(list.GetRect(42f)), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DiagnosticsSettingsDesc", "Optional tools for checking Harmony hooks, boss true HP, monster AI recovery, and safer Abyssal UI text layout. Keep verbose diagnostics off unless testing."));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_Enable", "Enable periodic diagnostics"), ref settingsData.enableStabilityDiagnostics, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_EnableDesc", "Logs a throttled health snapshot while a game is loaded. Off by default."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_HarmonyReport", "Log Harmony patch report on load"), ref settingsData.showHarmonyPatchReportOnLoad, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_HarmonyReportDesc", "Writes a compact count of Abyssal-owned Harmony patches after startup."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_DebugInspect", "Show debug inspect strings"), ref settingsData.showDebugInspectStrings, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_DebugInspectDesc", "Shows extra inspect text such as boss true HP. Intended for testing only."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_Verbose", "Verbose diagnostics"), ref settingsData.verboseDiagnostics, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_VerboseDesc", "Adds throttled messages for AI recovery and periodic snapshots. Use only while debugging."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_EncounterValidation_Enable", "Validate encounter data on startup"), ref settingsData.enableEncounterDataValidation, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_EncounterValidation_EnableDesc", "Checks encounter templates, doctrines, pawn pools, roles, and budgets at startup. It logs warnings only and never changes spawns."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_EncounterShadow_Enable", "Enable encounter shadow planning logs"), ref settingsData.enableEncounterShadowPlanning, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_EncounterShadow_EnableDesc", "Diagnostic-only mode: compares selected legacy packs against the directed encounter planner without changing the real spawned wave."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_UIPolish", "Enable Abyssal UI text polish"), ref settingsData.enableUIPolish, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_UIPolishDesc", "Adds small padding and clipping guards to Abyssal custom labels/buttons."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_ThrottleWarnings", "Throttle repeated Abyssal warnings"), ref settingsData.suppressRepeatedWarnings, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_ThrottleWarningsDesc", "Prevents repeated compatibility warnings from spamming the log."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossPresentation_ScreenEffects", "Enable boss screen presentation effects"), ref settingsData.enableBossScreenEffects, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossPresentation_ScreenEffectsDesc", "Draws smooth boss-specific fullscreen vignette, bloom, and instability noise without rectangular overlay blocks."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossPresentation_MapEffects", "Enable boss map presentation effects"), ref settingsData.enableBossMapPresentationEffects, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossPresentation_MapEffectsDesc", "Adds restrained boss-specific map pulses around active bosses and summon moments."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWeather_Enable", "Enable Dominion hell weather"), ref settingsData.enableDominionWeather, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWeather_EnableDesc", "Adds a restrained hell-only ashfall / static veil / furnace drift layer inside Dominion pocket maps. Uses existing Abyssal VFX assets and honors reduced motion."));
            if (settingsData.enableDominionWeather)
            {
                DrawSlider(list, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionWeather_Intensity", "Dominion weather intensity: {0}", settingsData.dominionWeatherIntensity.ToString("F2")), ref settingsData.dominionWeatherIntensity, 0.20f, 1.50f);
            }
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionPocketMusic_Enable", "Enable Dominion pocket music"), ref settingsData.enableDominionPocketMusic, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionPocketMusic_EnableDesc", "Plays the dedicated looped hell track while the player is inside an active Dominion pocket dimension. A guard automatically restores normal music if the pocket closes unexpectedly."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossPresentation_Timeline", "Enable boss intro / phase / outro timeline"), ref settingsData.enableBossPresentationTimeline, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossPresentation_TimelineDesc", "Adds timed title cards and burst events for boss arrival, phase changes, and collapse."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossPresentation_TitleCards", "Show boss presentation title cards"), ref settingsData.enableBossPresentationTitleCards, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossPresentation_TitleCardsDesc", "Shows short cinematic name / phase / collapse cards during supported boss encounters."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossExpandedSelection_Enable", "Enable expanded boss selection"), ref settingsData.enableBossExpandedSelection, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BossExpandedSelection_EnableDesc", "Allows large Abyssal bosses to be selected by clicking their visual body, not only the pawn's center cell."));
            bool previousHarrowerAnimation = settingsData.enableAorticHarrowerBodyAnimation;
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HarrowerAnimation_Enable", "Enable Aortic Chain Harrower body animation"), ref settingsData.enableAorticHarrowerBodyAnimation, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HarrowerAnimation_EnableDesc", "Experimental overlay animation for the Dominion Slice heart guardians. If it throws a render exception, it auto-disables and can be re-enabled here after testing."));
            if (!previousHarrowerAnimation && settingsData.enableAorticHarrowerBodyAnimation)
            {
                ABY_AnimatedPawnBodyRenderer.ResetRuntimeDisableForDevTest();
            }
            if (ABY_AnimatedPawnBodyRenderer.IsRuntimeDisabled)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(1f, 0.64f, 0.58f, 1f);
                Widgets.Label(list.GetRect(32f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HarrowerAnimation_RuntimeDisabled", "Aortic Harrower animation was auto-disabled this session: {0}", ABY_AnimatedPawnBodyRenderer.RuntimeDisableReason ?? "unknown"));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            Rect row = list.GetRect(32f);
            float buttonGap = 8f;
            float buttonWidth = (row.width - buttonGap * 3f) / 4f;
            Rect openRect = new Rect(row.x, row.y, buttonWidth, 32f);
            Rect logRect = new Rect(openRect.xMax + buttonGap, row.y, buttonWidth, 32f);
            Rect validateRect = new Rect(logRect.xMax + buttonGap, row.y, buttonWidth, 32f);
            Rect perfRect = new Rect(validateRect.xMax + buttonGap, row.y, buttonWidth, 32f);
            if (AbyssalStyledWidgets.TextButton(openRect, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_OpenWindow", "Open diagnostics")))
            {
                Window_ABY_Diagnostics.OpenWindow();
            }
            if (AbyssalStyledWidgets.TextButton(logRect, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Diagnostics_LogNow", "Log snapshot now")))
            {
                ABY_StabilityDiagnosticsUtility.LogSnapshot(true);
            }
            if (AbyssalStyledWidgets.TextButton(validateRect, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_EncounterValidation_LogNow", "Validate encounters")))
            {
                ABY_EncounterValidationUtility.LogValidationSnapshot(true);
            }
            if (settingsData.enableDevPerformanceAuditWindow && AbyssalStyledWidgets.TextButton(perfRect, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Performance_OpenAudit", "Performance audit")))
            {
                Window_ABY_PerformanceAudit.OpenWindow();
            }

            list.Gap(4f);
        }

        private static void DrawExperimentalSystemsSection(Listing_Standard list, AbyssalProtocolModSettings settingsData)
        {
            Widgets.Label(list.GetRect(24f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_ExperimentalSystemsHeader", "Experimental systems / kill switches"));
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.84f, 0.78f, 0.72f, 1f);
            Widgets.Label(ABY_UIPolishUtility.TextRect(list.GetRect(42f)), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_ExperimentalSystemsDesc", "Prototype systems live behind explicit kill switches. Disabling a system should stop runtime behavior and hide its forge exposure without deleting save data."));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_ModularTurrets_Enable", "Enable modular turret prototype"), ref settingsData.enableModularTurrets, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_ModularTurrets_EnableDesc", "Master switch for Package 0 modular turrets. When disabled, modular turret comps stop targeting/firing, placement is blocked, and turret-module forge recipes are hidden. Installed modules remain saved for safe re-enable."));
            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_ProtocolNexusGating_Enable", "Enable Protocol Nexus forge gating"), ref settingsData.enableProtocolNexusGating, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_ProtocolNexusGating_EnableDesc", "Optional experimental overlay. When disabled, all Forge patterns are treated as decoded and the Nexus becomes a codex/progression map instead of a hard gate."));
        }

        private static void DrawDifficultySection(Listing_Standard list, AbyssalProtocolModSettings settingsData)
        {
            Widgets.Label(list.GetRect(24f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultySettingsHeader", "Abyssal difficulty protocol"));
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.84f, 0.78f, 0.72f, 1f);
            Widgets.Label(list.GetRect(34f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultySettingsDesc", "Global threat protocol for summon pressure, instability, reward routing, and future encounter composition."));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Widgets.Label(list.GetRect(22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultySettingsCurrent", "Current protocol: {0}", AbyssalDifficultyUtility.GetCurrentDifficultyLabel()));

            float gap = 6f;
            Rect row = list.GetRect(34f);
            var profiles = new List<ABY_DifficultyProfileDef>(AbyssalDifficultyUtility.GetOrderedProfiles());
            float cellWidth = (row.width - gap * (profiles.Count - 1)) / Mathf.Max(1, profiles.Count);
            for (int i = 0; i < profiles.Count; i++)
            {
                ABY_DifficultyProfileDef profile = profiles[i];
                Rect buttonRect = new Rect(row.x + (cellWidth + gap) * i, row.y, cellWidth, 34f);
                bool active = string.Equals(settingsData.difficultyProfileDefName, profile.defName);
                bool canUse = !AbyssalProtocolMod.Settings.lockDifficultyAfterFirstBoss || !AbyssalDifficultyUtility.HasRecordedFirstBossKill() || active;
                if (AbyssalStyledWidgets.TextButton(buttonRect, profile.ResolveLabel(), canUse && !active, active))
                {
                    settingsData.difficultyProfileDefName = profile.defName;
                }
            }

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.88f, 0.82f, 0.78f, 1f);
            Widgets.Label(list.GetRect(44f), AbyssalDifficultyUtility.GetCurrentDifficultyDescription());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            list.CheckboxLabeled(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultySettingsLock", "Lock protocol after first boss kill"), ref settingsData.lockDifficultyAfterFirstBoss, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultySettingsLockDesc", "Optional anti-abuse toggle. Disabled by default in v3, but can be re-enabled for the old per-save lock behavior."));

            if (!settingsData.lockDifficultyAfterFirstBoss)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.80f, 0.92f, 1f, 1f);
                Widgets.Label(list.GetRect(26f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultySettingsUnlockedNote", "Save-lock is currently disabled by default. Re-enable it here to restore the old per-save protocol lock."));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
            else if (AbyssalDifficultyUtility.HasRecordedFirstBossKill())
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(1f, 0.64f, 0.58f, 1f);
                Widgets.Label(list.GetRect(30f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DifficultySettingsLockedNow", "Protocol changes are currently locked on this save because the first boss kill has already been recorded."));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            List<string> lines = AbyssalDifficultyUtility.GetDiagnosticsLines();
            for (int i = 0; i < lines.Count; i++)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.78f, 0.92f, 1f, 1f);
                Widgets.Label(list.GetRect(20f), lines[i]);
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            list.Gap(4f);
        }

        private static void DrawAnchorSelector(Listing_Standard list, AbyssalProtocolModSettings settingsData)
        {
            Rect labelRect = list.GetRect(24f);
            Widgets.Label(labelRect, "ABY_BossBar_Anchor".Translate() + ": " + ResolveAnchorLabel(settingsData.anchorPreset));

            Rect rowRect = list.GetRect(32f);
            float gap = 6f;
            float cellWidth = (rowRect.width - gap * 2f) / 3f;
            DrawAnchorButton(new Rect(rowRect.x, rowRect.y, cellWidth, 32f), ref settingsData.anchorPreset, ABY_BossBarAnchorPreset.TopLeft, "ABY_BossBar_Anchor_TopLeft".Translate());
            DrawAnchorButton(new Rect(rowRect.x + cellWidth + gap, rowRect.y, cellWidth, 32f), ref settingsData.anchorPreset, ABY_BossBarAnchorPreset.TopCenter, "ABY_BossBar_Anchor_TopCenter".Translate());
            DrawAnchorButton(new Rect(rowRect.x + (cellWidth + gap) * 2f, rowRect.y, cellWidth, 32f), ref settingsData.anchorPreset, ABY_BossBarAnchorPreset.TopRight, "ABY_BossBar_Anchor_TopRight".Translate());

            rowRect = list.GetRect(32f);
            DrawAnchorButton(new Rect(rowRect.x, rowRect.y, cellWidth, 32f), ref settingsData.anchorPreset, ABY_BossBarAnchorPreset.BottomLeft, "ABY_BossBar_Anchor_BottomLeft".Translate());
            DrawAnchorButton(new Rect(rowRect.x + cellWidth + gap, rowRect.y, cellWidth, 32f), ref settingsData.anchorPreset, ABY_BossBarAnchorPreset.BottomCenter, "ABY_BossBar_Anchor_BottomCenter".Translate());
            DrawAnchorButton(new Rect(rowRect.x + (cellWidth + gap) * 2f, rowRect.y, cellWidth, 32f), ref settingsData.anchorPreset, ABY_BossBarAnchorPreset.BottomRight, "ABY_BossBar_Anchor_BottomRight".Translate());
            list.Gap(4f);
        }

        private static void DrawAnchorButton(Rect rect, ref ABY_BossBarAnchorPreset current, ABY_BossBarAnchorPreset target, string label)
        {
            if (AbyssalStyledWidgets.TextButton(rect, label, true, current == target))
            {
                current = target;
            }
        }

        private static void DrawSlider(Listing_Standard list, string label, ref float value, float min, float max)
        {
            Rect labelRect = list.GetRect(22f);
            Widgets.Label(labelRect, label);
            Rect sliderRect = list.GetRect(24f);
            value = Widgets.HorizontalSlider(sliderRect, value, min, max, true);
            list.Gap(2f);
        }

        private static string ResolveAnchorLabel(ABY_BossBarAnchorPreset anchorPreset)
        {
            switch (anchorPreset)
            {
                case ABY_BossBarAnchorPreset.BottomCenter:
                    return "ABY_BossBar_Anchor_BottomCenter".Translate();
                case ABY_BossBarAnchorPreset.TopLeft:
                    return "ABY_BossBar_Anchor_TopLeft".Translate();
                case ABY_BossBarAnchorPreset.TopRight:
                    return "ABY_BossBar_Anchor_TopRight".Translate();
                case ABY_BossBarAnchorPreset.BottomLeft:
                    return "ABY_BossBar_Anchor_BottomLeft".Translate();
                case ABY_BossBarAnchorPreset.BottomRight:
                    return "ABY_BossBar_Anchor_BottomRight".Translate();
                default:
                    return "ABY_BossBar_Anchor_TopCenter".Translate();
            }
        }
    }
}
