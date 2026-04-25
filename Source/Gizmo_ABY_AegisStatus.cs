using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Read-only styled status gizmo for armor-mounted Abyssal aegis shields.
    /// Package B polish adds explicit theme tag, dedicated icon support and a
    /// compact detail line for recharge/suppression readability.
    /// </summary>
    public class Gizmo_ABY_AegisStatus : Gizmo
    {
        private const float Width = 224f;
        private const float Height = 86f;

        private readonly string label;
        private readonly string subtitle;
        private readonly string state;
        private readonly string points;
        private readonly string detail;
        private readonly string headerTag;
        private readonly string tooltip;
        private readonly string theme;
        private readonly float current;
        private readonly float max;
        private readonly bool suppressed;
        private readonly bool collapsed;
        private readonly Texture2D icon;

        public Gizmo_ABY_AegisStatus(string label, string subtitle, string state, string points, string detail, string headerTag, string tooltip, string theme, float current, float max, bool suppressed, bool collapsed, Texture2D icon)
        {
            this.label = label.NullOrEmpty() ? "Aegis" : label;
            this.subtitle = subtitle ?? string.Empty;
            this.state = state ?? string.Empty;
            this.points = points ?? string.Empty;
            this.detail = detail ?? string.Empty;
            this.headerTag = headerTag ?? string.Empty;
            this.tooltip = tooltip ?? string.Empty;
            this.theme = theme ?? string.Empty;
            this.current = current;
            this.max = max;
            this.suppressed = suppressed;
            this.collapsed = collapsed;
            this.icon = icon;
            Order = -91f;
        }

        public override float GetWidth(float maxWidth)
        {
            return Width;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), Height);
            Palette palette = ResolvePalette(theme, suppressed, collapsed);
            bool hovered = Mouse.IsOver(rect);

            DrawBackground(rect, palette, hovered);
            DrawIcon(rect, palette);
            DrawHeader(rect, palette);
            DrawText(rect, palette);
            DrawChargeBar(rect, palette);
            DrawPulseOverlay(rect, palette, hovered);

            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            return new GizmoResult(GizmoState.Clear);
        }

        private void DrawBackground(Rect rect, Palette palette, bool hovered)
        {
            Widgets.DrawBoxSolid(rect, palette.back);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 24f), palette.header);

            if (hovered)
            {
                Widgets.DrawBoxSolid(rect.ContractedBy(2f), new Color(palette.fill.r, palette.fill.g, palette.fill.b, 0.06f));
            }

            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 1f), palette.border);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), palette.borderDark);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 1f, rect.height), palette.borderDark);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), palette.border);

            Widgets.DrawBoxSolid(new Rect(rect.x + 8f, rect.y + 25f, rect.width - 16f, 1f), new Color(palette.border.r, palette.border.g, palette.border.b, 0.35f));
            Widgets.DrawBoxSolid(new Rect(rect.x + 48f, rect.y + 74f, rect.width - 58f, 1f), new Color(palette.border.r, palette.border.g, palette.border.b, 0.22f));
        }

        private void DrawHeader(Rect rect, Palette palette)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;
            GUI.color = palette.title;
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 3f, rect.width - 84f, 18f), label.ToUpperInvariant());

            if (!headerTag.NullOrEmpty())
            {
                float tagWidth = Mathf.Min(72f, Mathf.Max(42f, headerTag.Length * 6.2f + 12f));
                Rect tagRect = new Rect(rect.xMax - tagWidth - 8f, rect.y + 4f, tagWidth, 16f);
                Widgets.DrawBoxSolid(tagRect, palette.tagBack);
                Widgets.DrawBoxSolid(new Rect(tagRect.x, tagRect.yMax - 1f, tagRect.width, 1f), palette.fillTop);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = palette.tagText;
                Widgets.Label(tagRect, headerTag);
            }

            GUI.color = oldColor;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }

        private void DrawIcon(Rect rect, Palette palette)
        {
            Rect iconFrame = new Rect(rect.x + 8f, rect.y + 31f, 34f, 34f);
            Widgets.DrawBoxSolid(iconFrame, palette.iconBack);
            Widgets.DrawBox(iconFrame, 1);

            if (icon != null)
            {
                Color oldColor = GUI.color;
                GUI.color = suppressed ? new Color(0.62f, 0.60f, 0.58f, 0.76f) : Color.white;
                GUI.DrawTexture(iconFrame.ContractedBy(4f), icon, ScaleMode.ScaleToFit, true);
                GUI.color = oldColor;
            }
            else
            {
                Widgets.DrawBoxSolid(iconFrame.ContractedBy(9f), new Color(palette.fill.r, palette.fill.g, palette.fill.b, suppressed ? 0.28f : 0.62f));
            }
        }

        private void DrawText(Rect rect, Palette palette)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;
            GUI.color = palette.dim;
            Widgets.Label(new Rect(rect.x + 48f, rect.y + 29f, rect.width - 56f, 16f), subtitle.NullOrEmpty() ? ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_GizmoIntegrity", "Shield integrity") : subtitle);

            GUI.color = palette.text;
            Widgets.Label(new Rect(rect.x + 48f, rect.y + 44f, 88f, 18f), points);

            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = palette.stateText;
            Widgets.Label(new Rect(rect.x + 132f, rect.y + 44f, rect.width - 142f, 18f), state);

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = palette.detailText;
            Widgets.Label(new Rect(rect.x + 48f, rect.y + 75f, rect.width - 56f, 10f), detail);

            GUI.color = oldColor;
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
        }

        private void DrawChargeBar(Rect rect, Palette palette)
        {
            Rect barBack = new Rect(rect.x + 48f, rect.y + 63f, rect.width - 58f, 7f);
            Widgets.DrawBoxSolid(barBack, new Color(0.025f, 0.025f, 0.03f, 0.96f));
            Widgets.DrawBox(barBack, 1);

            if (suppressed)
            {
                Widgets.DrawBoxSolid(barBack.ContractedBy(1f), new Color(0.28f, 0.27f, 0.25f, 0.38f));
                return;
            }

            float pct = max <= 0.01f ? 0f : Mathf.Clamp01(current / max);
            if (pct <= 0.001f)
            {
                return;
            }

            Rect fillRect = barBack.ContractedBy(1f);
            fillRect.width *= pct;
            Widgets.DrawBoxSolid(fillRect, palette.fill);
            Widgets.DrawBoxSolid(new Rect(fillRect.x, fillRect.y, fillRect.width, 2f), palette.fillTop);

            if (fillRect.width > 18f)
            {
                float shimmerWidth = Mathf.Min(42f, Mathf.Max(12f, fillRect.width * 0.24f));
                float shimmerX = fillRect.x - shimmerWidth + Mathf.Repeat(Time.realtimeSinceStartup * 48f, fillRect.width + shimmerWidth);
                Rect shimmerRect = new Rect(shimmerX, fillRect.y, shimmerWidth, fillRect.height);
                Widgets.DrawBoxSolid(shimmerRect, new Color(1f, 1f, 1f, collapsed ? 0.08f : 0.13f));
            }
        }

        private void DrawPulseOverlay(Rect rect, Palette palette, bool hovered)
        {
            if (suppressed)
            {
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * (collapsed ? 7.2f : 3.1f));
            float pct = max <= 0.01f ? 0f : Mathf.Clamp01(current / max);
            bool full = pct >= 0.995f;

            if (collapsed)
            {
                Color color = new Color(palette.border.r, palette.border.g, palette.border.b, 0.12f + pulse * 0.18f);
                Widgets.DrawBoxSolid(rect.ExpandedBy(2f), color);
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, 2f), palette.fillTop);
                Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), palette.fillTop);
                return;
            }

            if (full || hovered)
            {
                float alpha = full ? 0.055f + pulse * 0.045f : 0.035f;
                Widgets.DrawBoxSolid(rect.ContractedBy(3f), new Color(palette.fillTop.r, palette.fillTop.g, palette.fillTop.b, alpha));
            }
        }

        private static Palette ResolvePalette(string theme, bool suppressed, bool collapsed)
        {
            if (suppressed)
            {
                return new Palette(
                    new Color(0.035f, 0.034f, 0.035f, 0.96f),
                    new Color(0.070f, 0.064f, 0.062f, 0.98f),
                    new Color(0.39f, 0.36f, 0.32f, 0.96f),
                    new Color(0.14f, 0.13f, 0.12f, 0.96f),
                    new Color(0.32f, 0.30f, 0.27f, 0.60f),
                    new Color(0.55f, 0.52f, 0.48f, 0.65f),
                    new Color(0.92f, 0.83f, 0.72f, 0.70f),
                    new Color(0.68f, 0.63f, 0.56f, 0.72f),
                    new Color(0.64f, 0.58f, 0.52f, 0.70f),
                    new Color(0.10f, 0.09f, 0.08f, 0.94f),
                    new Color(0.12f, 0.11f, 0.10f, 0.98f),
                    new Color(0.86f, 0.82f, 0.76f, 0.84f),
                    new Color(0.62f, 0.60f, 0.56f, 0.80f));
            }

            if (collapsed)
            {
                return new Palette(
                    new Color(0.050f, 0.018f, 0.018f, 0.97f),
                    new Color(0.105f, 0.025f, 0.020f, 0.99f),
                    new Color(0.95f, 0.22f, 0.16f, 0.98f),
                    new Color(0.26f, 0.055f, 0.045f, 0.96f),
                    new Color(0.92f, 0.09f, 0.055f, 0.72f),
                    new Color(1.00f, 0.46f, 0.34f, 0.96f),
                    new Color(1.00f, 0.85f, 0.72f, 0.98f),
                    new Color(0.95f, 0.58f, 0.48f, 0.82f),
                    new Color(1.00f, 0.34f, 0.22f, 0.95f),
                    new Color(0.13f, 0.035f, 0.030f, 0.96f),
                    new Color(0.28f, 0.06f, 0.05f, 0.98f),
                    new Color(1.00f, 0.84f, 0.72f, 0.96f),
                    new Color(0.98f, 0.64f, 0.58f, 0.84f));
            }

            string lower = (theme ?? string.Empty).ToLowerInvariant();
            if (lower.Contains("crown"))
            {
                return new Palette(
                    new Color(0.025f, 0.020f, 0.034f, 0.97f),
                    new Color(0.060f, 0.040f, 0.080f, 0.99f),
                    new Color(1.00f, 0.74f, 0.28f, 0.98f),
                    new Color(0.20f, 0.085f, 0.30f, 0.96f),
                    new Color(0.82f, 0.50f, 1.00f, 0.80f),
                    new Color(1.00f, 0.88f, 0.56f, 0.98f),
                    new Color(1.00f, 0.92f, 0.76f, 0.98f),
                    new Color(0.92f, 0.72f, 1.00f, 0.82f),
                    new Color(0.98f, 0.82f, 0.42f, 0.96f),
                    new Color(0.055f, 0.035f, 0.070f, 0.95f),
                    new Color(0.15f, 0.06f, 0.22f, 0.98f),
                    new Color(1.00f, 0.90f, 0.62f, 0.98f),
                    new Color(0.94f, 0.78f, 1.00f, 0.84f));
            }

            return new Palette(
                new Color(0.030f, 0.024f, 0.022f, 0.97f),
                new Color(0.082f, 0.036f, 0.028f, 0.99f),
                new Color(0.95f, 0.36f, 0.16f, 0.98f),
                new Color(0.18f, 0.070f, 0.040f, 0.96f),
                new Color(1.00f, 0.22f, 0.12f, 0.72f),
                new Color(1.00f, 0.62f, 0.38f, 0.96f),
                new Color(1.00f, 0.88f, 0.76f, 0.98f),
                new Color(0.92f, 0.66f, 0.52f, 0.82f),
                new Color(1.00f, 0.48f, 0.26f, 0.96f),
                new Color(0.075f, 0.040f, 0.030f, 0.95f),
                new Color(0.19f, 0.08f, 0.05f, 0.98f),
                new Color(1.00f, 0.88f, 0.72f, 0.98f),
                new Color(0.94f, 0.70f, 0.58f, 0.84f));
        }

        private readonly struct Palette
        {
            public readonly Color back;
            public readonly Color header;
            public readonly Color border;
            public readonly Color borderDark;
            public readonly Color fill;
            public readonly Color fillTop;
            public readonly Color title;
            public readonly Color dim;
            public readonly Color text;
            public readonly Color iconBack;
            public readonly Color tagBack;
            public readonly Color tagText;
            public readonly Color detailText;

            public Color stateText => text;

            public Palette(Color back, Color header, Color border, Color borderDark, Color fill, Color fillTop, Color title, Color dim, Color text, Color iconBack, Color tagBack, Color tagText, Color detailText)
            {
                this.back = back;
                this.header = header;
                this.border = border;
                this.borderDark = borderDark;
                this.fill = fill;
                this.fillTop = fillTop;
                this.title = title;
                this.dim = dim;
                this.text = text;
                this.iconBack = iconBack;
                this.tagBack = tagBack;
                this.tagText = tagText;
                this.detailText = detailText;
            }
        }
    }
}
