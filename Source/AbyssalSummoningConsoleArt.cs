using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class AbyssalSummoningConsoleArt
    {
        private static readonly Texture2D OverlayTex = ContentFinder<Texture2D>.Get("UI/AbyssalSummoningCircle/ABY_SummoningConsoleOverlay", false);
        private static readonly Texture2D HeaderTex = ContentFinder<Texture2D>.Get("UI/AbyssalSummoningCircle/ABY_SummoningHeaderStrip", false);
        private static readonly Texture2D SealTex = ContentFinder<Texture2D>.Get("UI/AbyssalSummoningCircle/ABY_SummoningSeal", false);

        public static readonly Color BackgroundColor = new Color(0.045f, 0.035f, 0.04f, 0.97f);
        public static readonly Color PanelColor = new Color(0.095f, 0.072f, 0.076f, 0.96f);
        public static readonly Color PanelAltColor = new Color(0.12f, 0.078f, 0.074f, 0.97f);
        public static readonly Color AccentColor = new Color(1f, 0.36f, 0.15f, 1f);
        public static readonly Color AccentSoftColor = new Color(0.94f, 0.58f, 0.26f, 0.9f);
        public static readonly Color TextDimColor = new Color(0.94f, 0.78f, 0.72f, 0.80f);

        public static bool ReducedEffects;

        private static float AnimTime => Time.realtimeSinceStartup;

        public static void DrawBackground(Rect rect)
        {
            Fill(rect, BackgroundColor);
            if (OverlayTex != null)
            {
                DrawOverlay(rect, OverlayTex, new Color(1f, 0.28f, 0.16f, 0.12f));
            }

            if (SealTex != null)
            {
                Rect sealRect = new Rect(rect.center.x - 192f, rect.center.y - 192f, 384f, 384f);
                DrawRotatedTexture(sealRect, SealTex, -AnimTime * (ReducedEffects ? 2.4f : 6.6f), new Color(1f, 0.32f, 0.15f, ReducedEffects ? 0.04f : 0.08f));
                DrawRotatedTexture(sealRect.ContractedBy(22f), SealTex, AnimTime * (ReducedEffects ? 3.8f : 10.5f), new Color(1f, 0.70f, 0.46f, ReducedEffects ? 0.03f : 0.06f));
            }

            DrawOutline(rect, new Color(1f, 0.32f, 0.12f, 0.50f));
        }

        public static void DrawHeader(Rect rect, string title, string subtitle, bool alert)
        {
            float pulse = Pulse(alert ? 2.2f : 1.3f, rect.x * 0.01f);
            Fill(rect, new Color(0.085f, 0.060f, 0.065f, 1f));
            DrawOverlay(rect, HeaderTex, new Color(1f, 0.36f, 0.14f, alert ? 0.34f + pulse * 0.12f : 0.22f + pulse * 0.08f));
            DrawOutline(rect, Color.Lerp(AccentSoftColor, AccentColor, alert ? 0.55f + pulse * 0.24f : 0.38f + pulse * 0.14f));
            Fill(new Rect(rect.x + 16f, rect.yMax - 4f, rect.width - 32f, 1f), new Color(1f, 0.42f, 0.16f, 0.78f));

            if (!ReducedEffects)
            {
                float sweepX = rect.x - 100f + Mathf.Repeat(AnimTime * 116f, rect.width + 200f);
                Fill(new Rect(sweepX, rect.y + rect.height - 9f, 94f, 2f), new Color(1f, 0.72f, 0.48f, alert ? 0.30f : 0.18f));
            }

            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 18f, rect.y + 6f, rect.width - 80f, 30f), title);
            Text.Font = GameFont.Small;
            GUI.color = TextDimColor;
            Widgets.Label(new Rect(rect.x + 20f, rect.y + 36f, rect.width - 84f, rect.height - 36f), subtitle);
            GUI.color = Color.white;
        }

        public static void DrawPanel(Rect rect, bool highlighted)
        {
            float pulse = Pulse(highlighted ? 1.9f : 1.1f, rect.y * 0.01f);
            Fill(rect, highlighted ? PanelAltColor : PanelColor);
            DrawOverlay(rect, OverlayTex, new Color(1f, 0.24f, 0.16f, highlighted ? 0.09f + pulse * (ReducedEffects ? 0.02f : 0.05f) : 0.05f + pulse * (ReducedEffects ? 0.01f : 0.025f)));
            DrawOutline(rect, highlighted ? Color.Lerp(AccentSoftColor, AccentColor, 0.34f + pulse * 0.22f) : new Color(1f, 0.42f, 0.20f, 0.24f + pulse * 0.12f));
            Fill(new Rect(rect.x, rect.y, rect.width, 2f), highlighted ? Color.Lerp(AccentSoftColor, AccentColor, 0.34f + pulse * 0.20f) : new Color(1f, 0.34f, 0.12f, 0.28f));
        }

        public static void DrawSectionTitle(Rect rect, string title)
        {
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(rect, title);
            GUI.color = Color.white;
        }

        public static void DrawStripCell(Rect rect, string label, string value, bool good, float offset)
        {
            DrawPanel(rect, good);
            Fill(new Rect(rect.x + 6f, rect.y + 6f, 6f, rect.height - 12f), good ? new Color(0.78f, 1f, 0.76f, 0.8f) : new Color(1f, 0.46f, 0.36f, 0.84f));

            float pulse = Pulse(2.4f, offset);
            if (good && !ReducedEffects)
            {
                Fill(new Rect(rect.x + rect.width - 26f, rect.y + 6f, 18f, 1f), new Color(1f, 0.76f, 0.56f, 0.14f + pulse * 0.14f));
            }

            GUI.color = TextDimColor;
            Widgets.Label(new Rect(rect.x + 18f, rect.y + 6f, rect.width - 24f, 18f), label);
            GUI.color = good ? Color.white : new Color(1f, 0.74f, 0.72f, 1f);
            Widgets.Label(new Rect(rect.x + 18f, rect.y + 24f, rect.width - 24f, rect.height - 24f), value);
            GUI.color = Color.white;
        }

        public static void DrawRiskBar(Rect rect, float fillPercent, string label, Color fillColor, bool dangerPulse)
        {
            Fill(rect, new Color(0.05f, 0.04f, 0.045f, 1f));
            Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * Mathf.Clamp01(fillPercent), rect.height - 4f);
            Fill(fillRect, fillColor);
            DrawOverlay(fillRect, HeaderTex, new Color(1f, 0.72f, 0.44f, 0.14f + Pulse(dangerPulse ? 3.2f : 2.0f, rect.x * 0.01f) * (dangerPulse ? 0.20f : 0.08f)));

            if (fillRect.width > 20f && !ReducedEffects)
            {
                float sheenWidth = Mathf.Min(78f, fillRect.width);
                float sheenX = fillRect.x - sheenWidth + Mathf.Repeat(AnimTime * (dangerPulse ? 102f : 70f), fillRect.width + sheenWidth);
                Fill(new Rect(sheenX, fillRect.y, sheenWidth, fillRect.height), new Color(1f, 0.9f, 0.8f, dangerPulse ? 0.18f : 0.10f));
            }

            DrawOutline(rect, new Color(fillColor.r, fillColor.g, fillColor.b, 0.82f));
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DrawRitualCardPulse(Rect rect, bool selected, bool active)
        {
            float pulse = Pulse(active ? 3.1f : 2.0f, rect.x * 0.005f + rect.y * 0.01f);
            if (selected)
            {
                Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 3f), new Color(1f, 0.74f, 0.54f, 0.18f + pulse * 0.12f));
            }

            if (!ReducedEffects)
            {
                float sweepX = rect.x - 42f + Mathf.Repeat(AnimTime * (active ? 88f : 54f) + rect.y * 0.4f, rect.width + 84f);
                Fill(new Rect(sweepX, rect.y + rect.height - 24f, 38f, 1f), new Color(1f, 0.82f, 0.68f, active ? 0.22f : 0.12f));
            }
        }

        public static void DrawActionButtonFrame(Rect rect, bool emphasis)
        {
            DrawPanel(rect, emphasis);
            float pulse = Pulse(emphasis ? 1.8f : 1.2f, rect.x * 0.015f);
            Fill(new Rect(rect.x + 8f, rect.y + rect.height - 7f, rect.width - 16f, 2f), new Color(1f, 0.68f, 0.42f, emphasis ? 0.16f + pulse * 0.12f : 0.08f));
        }

        public static void Fill(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = oldColor;
        }

        public static void DrawOutline(Rect rect, Color color)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, 1f), color);
            Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            Fill(new Rect(rect.x, rect.y, 1f, rect.height), color);
            Fill(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        public static void DrawOverlay(Rect rect, Texture2D texture, Color color)
        {
            if (texture == null)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
        }

        private static void DrawRotatedTexture(Rect rect, Texture2D texture, float angle, Color color)
        {
            if (texture == null)
            {
                return;
            }

            Matrix4x4 old = GUI.matrix;
            Vector2 pivot = rect.center;
            GUIUtility.RotateAroundPivot(angle, pivot);
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
            GUI.matrix = old;
        }

        private static float Pulse(float speed, float offset)
        {
            float value = (Mathf.Sin(AnimTime * speed * (ReducedEffects ? 0.45f : 1f) + offset) + 1f) * 0.5f;
            if (!ReducedEffects)
            {
                return value;
            }

            return Mathf.Lerp(0.35f, 0.65f, value);
        }
    }
}
