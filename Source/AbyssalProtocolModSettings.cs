using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class AbyssalProtocolModSettings : ModSettings
    {
        public bool enableBossBars = true;
        public ABY_BossBarAnchorPreset anchorPreset = ABY_BossBarAnchorPreset.TopCenter;
        public float width = 640f;
        public float height = 42f;
        public float iconSize = 78f;
        public float gap = 12f;
        public float globalScale = 1f;
        public float offsetX = 0f;
        public float offsetY = 44f;
        public float safeMargin = 18f;
        public bool showHealthNumbers = true;
        public bool showPhaseMarkers = true;
        public bool showPhaseLabel = true;
        public bool showSecondaryBars = true;
        public bool showCalibrationButton = true;
        public bool reducedMotion = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableBossBars, "enableBossBars", true);
            Scribe_Values.Look(ref anchorPreset, "anchorPreset", ABY_BossBarAnchorPreset.TopCenter);
            Scribe_Values.Look(ref width, "width", 640f);
            Scribe_Values.Look(ref height, "height", 42f);
            Scribe_Values.Look(ref iconSize, "iconSize", 78f);
            Scribe_Values.Look(ref gap, "gap", 12f);
            Scribe_Values.Look(ref globalScale, "globalScale", 1f);
            Scribe_Values.Look(ref offsetX, "offsetX", 0f);
            Scribe_Values.Look(ref offsetY, "offsetY", 44f);
            Scribe_Values.Look(ref safeMargin, "safeMargin", 18f);
            Scribe_Values.Look(ref showHealthNumbers, "showHealthNumbers", true);
            Scribe_Values.Look(ref showPhaseMarkers, "showPhaseMarkers", true);
            Scribe_Values.Look(ref showPhaseLabel, "showPhaseLabel", true);
            Scribe_Values.Look(ref showSecondaryBars, "showSecondaryBars", true);
            Scribe_Values.Look(ref showCalibrationButton, "showCalibrationButton", true);
            Scribe_Values.Look(ref reducedMotion, "reducedMotion", false);
            ClampValues();
        }

        public void ClampValues()
        {
            width = Mathf.Clamp(width, 320f, 1080f);
            height = Mathf.Clamp(height, 22f, 84f);
            iconSize = Mathf.Clamp(iconSize, 40f, 156f);
            gap = Mathf.Clamp(gap, 0f, 48f);
            globalScale = Mathf.Clamp(globalScale, 0.70f, 1.80f);
            offsetX = Mathf.Clamp(offsetX, -1200f, 1200f);
            offsetY = Mathf.Clamp(offsetY, -700f, 700f);
            safeMargin = Mathf.Clamp(safeMargin, 0f, 120f);
        }

        public void ResetToDefaults()
        {
            enableBossBars = true;
            anchorPreset = ABY_BossBarAnchorPreset.TopCenter;
            width = 640f;
            height = 42f;
            iconSize = 78f;
            gap = 12f;
            globalScale = 1f;
            offsetX = 0f;
            offsetY = 44f;
            safeMargin = 18f;
            showHealthNumbers = true;
            showPhaseMarkers = true;
            showPhaseLabel = true;
            showSecondaryBars = true;
            showCalibrationButton = true;
            reducedMotion = false;
        }

        public Vector2 ResolveTopLeft(Rect screenRect, Vector2 totalSize)
        {
            ClampValues();
            float x;
            float y;
            switch (anchorPreset)
            {
                case ABY_BossBarAnchorPreset.BottomCenter:
                    x = screenRect.center.x - totalSize.x * 0.5f;
                    y = screenRect.yMax - safeMargin - totalSize.y;
                    break;
                case ABY_BossBarAnchorPreset.TopLeft:
                    x = screenRect.x + safeMargin;
                    y = screenRect.y + safeMargin;
                    break;
                case ABY_BossBarAnchorPreset.TopRight:
                    x = screenRect.xMax - safeMargin - totalSize.x;
                    y = screenRect.y + safeMargin;
                    break;
                case ABY_BossBarAnchorPreset.BottomLeft:
                    x = screenRect.x + safeMargin;
                    y = screenRect.yMax - safeMargin - totalSize.y;
                    break;
                case ABY_BossBarAnchorPreset.BottomRight:
                    x = screenRect.xMax - safeMargin - totalSize.x;
                    y = screenRect.yMax - safeMargin - totalSize.y;
                    break;
                default:
                    x = screenRect.center.x - totalSize.x * 0.5f;
                    y = screenRect.y + safeMargin;
                    break;
            }

            x += offsetX;
            y += offsetY;
            x = Mathf.Clamp(x, screenRect.x, screenRect.xMax - totalSize.x);
            y = Mathf.Clamp(y, screenRect.y, screenRect.yMax - totalSize.y);
            return new Vector2(x, y);
        }
    }
}
