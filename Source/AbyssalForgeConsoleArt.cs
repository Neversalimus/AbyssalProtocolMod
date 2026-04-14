using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class AbyssalForgeConsoleArt
    {
        private const string OverlayPath = "UI/AbyssalForge/ABY_ConsoleOverlay";
        private const string HeaderPath = "UI/AbyssalForge/ABY_HeaderStrip";
        private const string IconAllPath = "UI/AbyssalForge/ABY_Category_All";
        private const string IconCorePath = "UI/AbyssalForge/ABY_Category_Core";
        private const string IconWeaponsPath = "UI/AbyssalForge/ABY_Category_Weapons";
        private const string IconImplantsPath = "UI/AbyssalForge/ABY_Category_Implants";
        private const string IconRitualPath = "UI/AbyssalForge/ABY_Category_Ritual";
        private const string IconHeraldPath = "UI/AbyssalForge/ABY_Category_Herald";

        public static readonly Color BackColor = new Color(0.07f, 0.07f, 0.08f, 1f);
        public static readonly Color PanelColor = new Color(0.11f, 0.105f, 0.115f, 0.98f);
        public static readonly Color PanelAltColor = new Color(0.125f, 0.10f, 0.09f, 0.98f);
        public static readonly Color AccentColor = new Color(0.95f, 0.43f, 0.18f, 1f);
        public static readonly Color AccentSoftColor = new Color(0.56f, 0.23f, 0.11f, 1f);
        public static readonly Color TextDimColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        public static readonly Color TextSoftColor = new Color(0.86f, 0.78f, 0.72f, 1f);
        public static readonly Color LockedColor = new Color(0.45f, 0.22f, 0.16f, 1f);
        public static readonly Color UnlockedColor = new Color(0.20f, 0.12f, 0.10f, 1f);

        private static readonly Texture2D OverlayTex = ContentFinder<Texture2D>.Get(OverlayPath, false);
        private static readonly Texture2D HeaderTex = ContentFinder<Texture2D>.Get(HeaderPath, false);
        private static readonly Texture2D IconAllTex = ContentFinder<Texture2D>.Get(IconAllPath, false);
        private static readonly Texture2D IconCoreTex = ContentFinder<Texture2D>.Get(IconCorePath, false);
        private static readonly Texture2D IconWeaponsTex = ContentFinder<Texture2D>.Get(IconWeaponsPath, false);
        private static readonly Texture2D IconImplantsTex = ContentFinder<Texture2D>.Get(IconImplantsPath, false);
        private static readonly Texture2D IconRitualTex = ContentFinder<Texture2D>.Get(IconRitualPath, false);
        private static readonly Texture2D IconHeraldTex = ContentFinder<Texture2D>.Get(IconHeraldPath, false);

        public static bool ReducedEffects { get; set; }

        private static float AnimTime => Time.realtimeSinceStartup;

        public static void DrawBackground(Rect rect)
        {
            float pulse = Pulse(1.05f, 0.35f);
            Fill(rect, BackColor);
            DrawOverlay(rect, OverlayTex, new Color(1f, 0.48f, 0.22f, (ReducedEffects ? 0.07f : 0.12f) + pulse * (ReducedEffects ? 0.02f : 0.06f)));
            DrawOutline(rect, Color.Lerp(AccentSoftColor, AccentColor, pulse * 0.35f));

            if (!ReducedEffects)
            {
                float scanY = rect.y + Mathf.Repeat(AnimTime * 24f, Mathf.Max(1f, rect.height - 4f));
                Fill(new Rect(rect.x + 2f, scanY, rect.width - 4f, 1f), new Color(1f, 0.55f, 0.24f, 0.06f));
            }
        }

        public static void DrawHeader(Rect rect, string title, string subtitle, bool alert)
        {
            float pulse = Pulse(alert ? 2.0f : 1.4f, 0.12f);
            Fill(rect, new Color(0.09f, 0.075f, 0.07f, 1f));
            DrawOverlay(rect, HeaderTex, new Color(1f, 0.5f, 0.22f, 0.58f + pulse * (alert ? 0.14f : 0.08f)));
            DrawOutline(rect, Color.Lerp(AccentSoftColor, AccentColor, alert ? 0.58f + pulse * 0.30f : 0.45f + pulse * 0.20f));

            if (!ReducedEffects)
            {
                float sweepX = rect.x - 90f + Mathf.Repeat(AnimTime * 120f, rect.width + 180f);
                Fill(new Rect(sweepX, rect.y + rect.height - 8f, 88f, 2f), new Color(1f, 0.76f, 0.54f, alert ? 0.34f : 0.22f));
            }

            Fill(new Rect(rect.x + 16f, rect.y + rect.height - 4f, rect.width - 32f, 1f), new Color(1f, 0.42f, 0.16f, 0.72f));

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 16f, rect.y + 6f, rect.width - 76f, 34f), title);

            Text.Font = GameFont.Small;
            GUI.color = TextSoftColor;
            float subtitleHeight = Text.CalcHeight(subtitle, rect.width - 88f);
            float maxSubtitleHeight = Mathf.Max(18f, rect.height - 40f);
            Widgets.Label(new Rect(rect.x + 18f, rect.y + 36f, rect.width - 88f, Mathf.Min(subtitleHeight, maxSubtitleHeight)), subtitle);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            DrawSignalGlyph(new Rect(rect.xMax - 34f, rect.y + 9f, 18f, 18f), pulse);
        }

        public static void DrawPanel(Rect rect, bool highlighted)
        {
            float pulse = Pulse(highlighted ? 2.1f : 1.25f, highlighted ? 0.35f : 0.12f);
            Fill(rect, highlighted ? PanelAltColor : PanelColor);
            DrawOverlay(rect, OverlayTex, new Color(1f, 0.48f, 0.22f, highlighted ? 0.10f + pulse * (ReducedEffects ? 0.02f : 0.05f) : 0.05f + pulse * (ReducedEffects ? 0.02f : 0.03f)));
            DrawOutline(rect, highlighted ? Color.Lerp(AccentSoftColor, AccentColor, 0.35f + pulse * 0.25f) : Color.Lerp(AccentSoftColor, AccentColor, 0.10f + pulse * 0.12f));
            Fill(new Rect(rect.x, rect.y, rect.width, 2f), highlighted ? Color.Lerp(AccentSoftColor, AccentColor, 0.40f + pulse * 0.25f) : AccentSoftColor);

            if (!ReducedEffects)
            {
                float sweep = rect.x - 70f + Mathf.Repeat(AnimTime * (highlighted ? 72f : 44f), rect.width + 140f);
                Fill(new Rect(sweep, rect.y + 1f, 68f, 1f), new Color(1f, 0.76f, 0.54f, highlighted ? 0.20f : 0.10f));
            }
        }

        public static void DrawSectionTitle(Rect rect, string title)
        {
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(rect, title);
            GUI.color = Color.white;
        }

        public static void DrawMetric(Rect rect, string label, string value)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = TextDimColor;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 14f), label);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x, rect.y + 14f, rect.width, rect.height - 14f), value);
            GUI.color = Color.white;
        }

        public static void DrawProgressBar(Rect rect, float fillPercent, string label, bool alert)
        {
            Fill(rect, new Color(0.04f, 0.04f, 0.045f, 1f));
            Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * Mathf.Clamp01(fillPercent), rect.height - 4f);
            Color fillColor = alert ? new Color(1f, 0.54f, 0.22f, 1f) : new Color(0.92f, 0.42f, 0.18f, 1f);
            Fill(fillRect, fillColor);
            DrawOverlay(fillRect, HeaderTex, new Color(1f, 0.72f, 0.42f, 0.20f + Pulse(alert ? 3.0f : 2.2f, 0.55f) * (alert ? 0.18f : 0.08f)));

            if (fillRect.width > 20f && !ReducedEffects)
            {
                float sheenWidth = Mathf.Min(80f, fillRect.width);
                float sheenX = fillRect.x - sheenWidth + Mathf.Repeat(AnimTime * (alert ? 94f : 68f), fillRect.width + sheenWidth);
                Fill(new Rect(sheenX, fillRect.y, sheenWidth, fillRect.height), new Color(1f, 0.92f, 0.78f, alert ? 0.20f : 0.12f));
            }

            DrawOutline(rect, alert ? AccentColor : AccentSoftColor);

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DrawActionButtonFrame(Rect rect, bool emphasis)
        {
            DrawPanel(rect, emphasis);
            float pulse = Pulse(emphasis ? 1.9f : 1.15f, 0.24f);
            Fill(new Rect(rect.x + 8f, rect.y + rect.height - 8f, rect.width - 16f, 2f), new Color(1f, 0.68f, 0.42f, emphasis ? 0.16f + pulse * 0.12f : 0.08f));
        }

        public static void DrawPatternCardPulse(Rect rect, bool unlocked, bool freshlyUnlocked)
        {
            if (!unlocked)
            {
                return;
            }

            float pulse = Pulse(freshlyUnlocked ? 3.2f : 2.35f, rect.x * 0.01f + rect.y * 0.01f);
            Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, freshlyUnlocked ? 3f : 2f), new Color(1f, 0.72f, 0.50f, freshlyUnlocked ? 0.16f + pulse * 0.12f : 0.08f + pulse * 0.08f));
            if (!ReducedEffects)
            {
                float sweepX = rect.x - 36f + Mathf.Repeat(AnimTime * (freshlyUnlocked ? 92f : 58f) + rect.y * 0.4f, rect.width + 72f);
                Fill(new Rect(sweepX, rect.y + rect.height - 22f, 34f, 1f), new Color(1f, 0.82f, 0.66f, freshlyUnlocked ? 0.28f : 0.18f));
            }
        }

        public static void DrawTag(Rect rect, string label, bool alert)
        {
            Fill(rect, alert ? new Color(0.85f, 0.26f, 0.08f, 0.92f) : new Color(0.35f, 0.18f, 0.10f, 0.92f));
            DrawOutline(rect, alert ? new Color(1f, 0.72f, 0.44f, 0.9f) : AccentSoftColor);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static Texture2D GetCategoryIcon(string category)
        {
            if (category == AbyssalForgeProgressUtility.CoreCategory)
            {
                return IconCoreTex;
            }

            if (category == AbyssalForgeProgressUtility.WeaponsCategory)
            {
                return IconWeaponsTex;
            }

            if (category == AbyssalForgeProgressUtility.ImplantsCategory)
            {
                return IconImplantsTex;
            }

            if (category == AbyssalForgeProgressUtility.RitualCategory)
            {
                return IconRitualTex;
            }

            if (category == AbyssalForgeProgressUtility.HeraldCategory)
            {
                return IconHeraldTex;
            }

            return IconAllTex;
        }

        public static void DrawCategoryButton(Rect rect, string category, bool selected)
        {
            DrawPanel(rect, selected);

            Texture2D icon = GetCategoryIcon(category);
            if (icon != null)
            {
                Color oldColor = GUI.color;
                float pulse = Pulse(2.1f, rect.x * 0.015f);
                GUI.color = selected
                    ? Color.Lerp(new Color(1f, 0.68f, 0.45f, 1f), new Color(1f, 0.86f, 0.72f, 1f), pulse)
                    : new Color(0.92f, 0.92f, 0.92f, 0.9f);
                GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 6f, 24f, 24f), icon);
                GUI.color = oldColor;
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = selected ? Color.Lerp(new Color(1f, 0.72f, 0.52f, 1f), Color.white, Pulse(2.2f, rect.y * 0.01f) * 0.45f) : Color.white;
            Widgets.Label(new Rect(rect.x + 18f, rect.y, rect.width - 22f, rect.height), AbyssalForgeProgressUtility.GetCategoryLabel(category));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
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

        private static void DrawSignalGlyph(Rect rect, float pulse)
        {
            Color glyphColor = Color.Lerp(new Color(0.85f, 0.28f, 0.08f, 0.85f), new Color(1f, 0.56f, 0.20f, 1f), pulse);
            DrawOutline(rect, glyphColor);
            Fill(rect.ContractedBy(4f), new Color(glyphColor.r, glyphColor.g, glyphColor.b, 0.08f + pulse * 0.10f));
            Fill(new Rect(rect.x + 4f, rect.center.y, rect.width - 8f, 1f), new Color(1f, 0.62f, 0.26f, 0.55f));
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
