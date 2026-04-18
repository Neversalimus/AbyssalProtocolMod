using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class Window_ABY_BossBarCalibration : Window
    {
        private static Window_ABY_BossBarCalibration openWindow;
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(540f, 620f);

        public Window_ABY_BossBarCalibration()
        {
            doCloseButton = true;
            doCloseX = true;
            absorbInputAroundWindow = false;
            closeOnClickedOutside = false;
            draggable = true;
        }

        public static void OpenWindow()
        {
            if (Find.WindowStack == null || openWindow != null)
            {
                return;
            }

            openWindow = new Window_ABY_BossBarCalibration();
            Find.WindowStack.Add(openWindow);
        }

        public override void PostClose()
        {
            base.PostClose();
            if (openWindow == this)
            {
                openWindow = null;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
            settings.ClampValues();

            Rect bgRect = inRect.ContractedBy(2f);
            AbyssalForgeConsoleArt.DrawBackground(bgRect);
            Rect headerRect = new Rect(bgRect.x, bgRect.y, bgRect.width, 72f);
            AbyssalForgeConsoleArt.DrawHeader(headerRect, "ABY_BossBar_CalibrationTitle".Translate(), "ABY_BossBar_CalibrationDesc".Translate(), false);

            Rect contentRect = new Rect(bgRect.x + 10f, headerRect.yMax + 8f, bgRect.width - 20f, bgRect.height - headerRect.height - 16f);
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 18f, 760f);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            float curY = 0f;
            DrawSectionLabel(viewRect, ref curY, "ABY_BossBar_Calibration_Positioning".Translate());
            DrawAnchorButtons(viewRect, ref curY, settings);
            DrawNudgeGrid(viewRect, ref curY, settings);
            DrawSlider(viewRect, ref curY, "ABY_BossBar_OffsetX".Translate(settings.offsetX.ToString("F0")), ref settings.offsetX, -1200f, 1200f);
            DrawSlider(viewRect, ref curY, "ABY_BossBar_OffsetY".Translate(settings.offsetY.ToString("F0")), ref settings.offsetY, -700f, 700f);
            DrawSlider(viewRect, ref curY, "ABY_BossBar_SafeMargin".Translate(settings.safeMargin.ToString("F0")), ref settings.safeMargin, 0f, 120f);

            DrawSectionLabel(viewRect, ref curY, "ABY_BossBar_Calibration_Size".Translate());
            DrawSizeStepRow(viewRect, ref curY, settings);
            DrawSlider(viewRect, ref curY, "ABY_BossBar_Width".Translate(settings.width.ToString("F0")), ref settings.width, 320f, 1080f);
            DrawSlider(viewRect, ref curY, "ABY_BossBar_Height".Translate(settings.height.ToString("F0")), ref settings.height, 22f, 84f);
            DrawSlider(viewRect, ref curY, "ABY_BossBar_IconSize".Translate(settings.iconSize.ToString("F0")), ref settings.iconSize, 40f, 156f);
            DrawSlider(viewRect, ref curY, "ABY_BossBar_Gap".Translate(settings.gap.ToString("F0")), ref settings.gap, 0f, 48f);
            DrawSlider(viewRect, ref curY, "ABY_BossBar_Scale".Translate(settings.globalScale.ToString("F2")), ref settings.globalScale, 0.70f, 1.80f);

            DrawSectionLabel(viewRect, ref curY, "ABY_BossBar_Calibration_Display".Translate());
            DrawCheckbox(viewRect, ref curY, "ABY_BossBar_Enable".Translate(), ref settings.enableBossBars);
            DrawCheckbox(viewRect, ref curY, "ABY_BossBar_ShowHealthNumbers".Translate(), ref settings.showHealthNumbers);
            DrawCheckbox(viewRect, ref curY, "ABY_BossBar_ShowPhaseMarkers".Translate(), ref settings.showPhaseMarkers);
            DrawCheckbox(viewRect, ref curY, "ABY_BossBar_ShowPhaseLabel".Translate(), ref settings.showPhaseLabel);
            DrawCheckbox(viewRect, ref curY, "ABY_BossBar_ShowSecondaryBars".Translate(), ref settings.showSecondaryBars);
            DrawCheckbox(viewRect, ref curY, "ABY_BossBar_ShowCalibrationButton".Translate(), ref settings.showCalibrationButton);
            DrawCheckbox(viewRect, ref curY, "ABY_BossBar_ReducedMotion".Translate(), ref settings.reducedMotion);

            Rect resetRect = new Rect(0f, curY + 10f, viewRect.width, 34f);
            Rect resetLeft = new Rect(resetRect.x, resetRect.y, (resetRect.width - 6f) * 0.5f, resetRect.height);
            Rect resetRight = new Rect(resetLeft.xMax + 6f, resetRect.y, resetRect.width - resetLeft.width - 6f, resetRect.height);
            if (AbyssalStyledWidgets.TextButton(resetLeft, "ABY_BossBar_ResetDefaults".Translate()))
            {
                settings.ResetToDefaults();
            }

            if (AbyssalStyledWidgets.TextButton(resetRight, "ABY_BossBar_CloseCalibration".Translate()))
            {
                Close();
            }

            curY = resetRect.yMax + 6f;
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.86f, 0.79f, 0.72f, 1f);
            Widgets.Label(new Rect(0f, curY, viewRect.width, 44f), "ABY_BossBar_CalibrationHint".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Widgets.EndScrollView();
            settings.ClampValues();
            AbyssalProtocolMod.SaveNow();
        }

        private static void DrawSectionLabel(Rect viewRect, ref float curY, string label)
        {
            Rect rect = new Rect(0f, curY, viewRect.width, 24f);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            curY = rect.yMax + 4f;
        }

        private static void DrawAnchorButtons(Rect viewRect, ref float curY, AbyssalProtocolModSettings settings)
        {
            float gap = 6f;
            float cellWidth = (viewRect.width - gap * 2f) / 3f;
            Rect row = new Rect(0f, curY, viewRect.width, 32f);
            DrawAnchorButton(new Rect(row.x, row.y, cellWidth, 32f), ref settings.anchorPreset, ABY_BossBarAnchorPreset.TopLeft, "ABY_BossBar_Anchor_TopLeft".Translate());
            DrawAnchorButton(new Rect(row.x + cellWidth + gap, row.y, cellWidth, 32f), ref settings.anchorPreset, ABY_BossBarAnchorPreset.TopCenter, "ABY_BossBar_Anchor_TopCenter".Translate());
            DrawAnchorButton(new Rect(row.x + (cellWidth + gap) * 2f, row.y, cellWidth, 32f), ref settings.anchorPreset, ABY_BossBarAnchorPreset.TopRight, "ABY_BossBar_Anchor_TopRight".Translate());
            curY = row.yMax + 6f;
            row = new Rect(0f, curY, viewRect.width, 32f);
            DrawAnchorButton(new Rect(row.x, row.y, cellWidth, 32f), ref settings.anchorPreset, ABY_BossBarAnchorPreset.BottomLeft, "ABY_BossBar_Anchor_BottomLeft".Translate());
            DrawAnchorButton(new Rect(row.x + cellWidth + gap, row.y, cellWidth, 32f), ref settings.anchorPreset, ABY_BossBarAnchorPreset.BottomCenter, "ABY_BossBar_Anchor_BottomCenter".Translate());
            DrawAnchorButton(new Rect(row.x + (cellWidth + gap) * 2f, row.y, cellWidth, 32f), ref settings.anchorPreset, ABY_BossBarAnchorPreset.BottomRight, "ABY_BossBar_Anchor_BottomRight".Translate());
            curY = row.yMax + 10f;
        }

        private static void DrawAnchorButton(Rect rect, ref ABY_BossBarAnchorPreset current, ABY_BossBarAnchorPreset target, string label)
        {
            if (AbyssalStyledWidgets.TextButton(rect, label, true, current == target))
            {
                current = target;
            }
        }

        private static void DrawNudgeGrid(Rect viewRect, ref float curY, AbyssalProtocolModSettings settings)
        {
            Widgets.Label(new Rect(0f, curY, viewRect.width, 22f), "ABY_BossBar_Calibration_Nudge".Translate());
            curY += 24f;
            float buttonSize = 32f;
            float gap = 6f;
            float centerX = viewRect.width * 0.5f - buttonSize * 0.5f;

            if (AbyssalStyledWidgets.TextButton(new Rect(centerX, curY, buttonSize, buttonSize), "↑"))
            {
                settings.offsetY -= 10f;
            }

            curY += buttonSize + gap;
            if (AbyssalStyledWidgets.TextButton(new Rect(centerX - buttonSize - gap, curY, buttonSize, buttonSize), "←"))
            {
                settings.offsetX -= 10f;
            }

            if (AbyssalStyledWidgets.TextButton(new Rect(centerX, curY, buttonSize, buttonSize), "•"))
            {
                settings.offsetX = 0f;
                settings.offsetY = 44f;
            }

            if (AbyssalStyledWidgets.TextButton(new Rect(centerX + buttonSize + gap, curY, buttonSize, buttonSize), "→"))
            {
                settings.offsetX += 10f;
            }

            curY += buttonSize + gap;
            if (AbyssalStyledWidgets.TextButton(new Rect(centerX, curY, buttonSize, buttonSize), "↓"))
            {
                settings.offsetY += 10f;
            }

            curY += buttonSize + 10f;
        }

        private static void DrawSizeStepRow(Rect viewRect, ref float curY, AbyssalProtocolModSettings settings)
        {
            float gap = 6f;
            float cellWidth = (viewRect.width - gap * 3f) / 4f;
            Rect row = new Rect(0f, curY, viewRect.width, 32f);
            if (AbyssalStyledWidgets.TextButton(new Rect(row.x, row.y, cellWidth, 32f), "ABY_BossBar_WidthMinus".Translate()))
            {
                settings.width -= 20f;
            }

            if (AbyssalStyledWidgets.TextButton(new Rect(row.x + (cellWidth + gap), row.y, cellWidth, 32f), "ABY_BossBar_WidthPlus".Translate()))
            {
                settings.width += 20f;
            }

            if (AbyssalStyledWidgets.TextButton(new Rect(row.x + (cellWidth + gap) * 2f, row.y, cellWidth, 32f), "ABY_BossBar_HeightMinus".Translate()))
            {
                settings.height -= 4f;
            }

            if (AbyssalStyledWidgets.TextButton(new Rect(row.x + (cellWidth + gap) * 3f, row.y, cellWidth, 32f), "ABY_BossBar_HeightPlus".Translate()))
            {
                settings.height += 4f;
            }

            curY = row.yMax + 8f;
        }

        private static void DrawSlider(Rect viewRect, ref float curY, string label, ref float value, float min, float max)
        {
            Widgets.Label(new Rect(0f, curY, viewRect.width, 22f), label);
            curY += 22f;
            value = Widgets.HorizontalSlider(new Rect(0f, curY, viewRect.width, 24f), value, min, max, true);
            curY += 28f;
        }

        private static void DrawCheckbox(Rect viewRect, ref float curY, string label, ref bool value)
        {
            Rect rect = new Rect(0f, curY, viewRect.width, 24f);
            Widgets.CheckboxLabeled(rect, label, ref value);
            curY = rect.yMax + 2f;
        }
    }
}
