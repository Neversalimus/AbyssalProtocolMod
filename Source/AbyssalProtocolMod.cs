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

            Rect viewRect = new Rect(0f, 0f, inRect.width - 18f, 620f);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);
            Listing_Standard list = new Listing_Standard();
            list.Begin(viewRect);

            list.Gap(4f);
            list.CheckboxLabeled("ABY_BossBar_Enable".Translate(), ref s.enableBossBars, "ABY_BossBar_EnableDesc".Translate());
            list.CheckboxLabeled("ABY_BossBar_ShowHealthNumbers".Translate(), ref s.showHealthNumbers, "ABY_BossBar_ShowHealthNumbersDesc".Translate());
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
            Widgets.EndScrollView();
            s.ClampValues();
        }

        public override void WriteSettings()
        {
            Settings.ClampValues();
            base.WriteSettings();
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
