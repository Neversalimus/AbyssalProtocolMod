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

        public static void DrawBackground(Rect rect)
        {
            Fill(rect, BackColor);
            DrawOverlay(rect, OverlayTex, new Color(1f, 0.48f, 0.22f, 0.18f));
            DrawOutline(rect, AccentSoftColor);
        }

        public static void DrawHeader(Rect rect, string title, string subtitle)
        {
            Fill(rect, new Color(0.09f, 0.075f, 0.07f, 1f));
            DrawOverlay(rect, HeaderTex, new Color(1f, 0.5f, 0.22f, 0.72f));
            DrawOutline(rect, AccentColor);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 16f, rect.y + 8f, rect.width - 32f, 34f), title);
            Text.Font = GameFont.Small;
            GUI.color = TextDimColor;
            Widgets.Label(new Rect(rect.x + 18f, rect.y + 40f, rect.width - 36f, 22f), subtitle);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DrawPanel(Rect rect, bool highlighted)
        {
            Fill(rect, highlighted ? PanelAltColor : PanelColor);
            DrawOverlay(rect, OverlayTex, new Color(1f, 0.48f, 0.22f, highlighted ? 0.16f : 0.09f));
            DrawOutline(rect, highlighted ? AccentColor : AccentSoftColor);
            Fill(new Rect(rect.x, rect.y, rect.width, 2f), highlighted ? AccentColor : AccentSoftColor);
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
            Text.Font = GameFont.Small;
            GUI.color = TextDimColor;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 18f), label);
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x, rect.y + 18f, rect.width, rect.height - 18f), value);
            GUI.color = Color.white;
        }

        public static void DrawProgressBar(Rect rect, float fillPercent, string label)
        {
            Fill(rect, new Color(0.04f, 0.04f, 0.045f, 1f));
            Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * Mathf.Clamp01(fillPercent), rect.height - 4f);
            Fill(fillRect, new Color(0.92f, 0.42f, 0.18f, 1f));
            DrawOverlay(fillRect, HeaderTex, new Color(1f, 0.72f, 0.42f, 0.28f));
            DrawOutline(rect, AccentSoftColor);

            Text.Anchor = TextAnchor.MiddleCenter;
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
                GUI.color = selected ? new Color(1f, 0.78f, 0.58f, 1f) : new Color(0.92f, 0.92f, 0.92f, 0.9f);
                GUI.DrawTexture(new Rect(rect.x + 8f, rect.y + 6f, 24f, 24f), icon);
                GUI.color = oldColor;
            }

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = selected ? new Color(1f, 0.72f, 0.52f, 1f) : Color.white;
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
    }
}
