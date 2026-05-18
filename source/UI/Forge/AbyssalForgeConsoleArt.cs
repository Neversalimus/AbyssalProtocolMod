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
        private const string IconArmorPath = "UI/AbyssalForge/ABY_Category_Armor";
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
        private static readonly Texture2D IconArmorTex = ContentFinder<Texture2D>.Get(IconArmorPath, false);
        private static readonly Texture2D IconImplantsTex = ContentFinder<Texture2D>.Get(IconImplantsPath, false);
        private static readonly Texture2D IconRitualTex = ContentFinder<Texture2D>.Get(IconRitualPath, false);
        private static readonly Texture2D IconHeraldTex = ContentFinder<Texture2D>.Get(IconHeraldPath, false);

        public static bool ReducedEffects { get; set; }

        private static float AnimTime => Time.realtimeSinceStartup;
        private static bool EnhancedUI => AbyssalStyledWidgets.UseEnhancedTheme;
        private static bool ReduceUIAnimation => ReducedEffects || AbyssalStyledWidgets.ReduceAbyssalUIAnimation;

        public static void DrawBackground(Rect rect)
        {
            if (EnhancedUI)
            {
                Fill(rect, BackColor);
                DrawOverlay(rect, OverlayTex, new Color(1f, 0.36f, 0.14f, ReduceUIAnimation ? 0.035f : 0.07f));
                DrawOutline(rect, new Color(1f, 0.40f, 0.14f, 0.58f));
                if (!ReduceUIAnimation)
                {
                    AbyssalStyledWidgets.DrawAccentAnimation(new Rect(rect.x + 8f, rect.y + rect.height - 20f, rect.width - 16f, 18f), AbyssalStyledWidgets.AbyssalAccentAnimation.EdgeGlow, 8f, 0.20f);
                }
                return;
            }

            float pulse = Pulse(1.05f, 0.35f);
            Fill(rect, BackColor);
            DrawOverlay(rect, OverlayTex, new Color(1f, 0.48f, 0.22f, (ReducedEffects ? 0.07f : 0.12f) + pulse * (ReducedEffects ? 0.02f : 0.06f)));
            DrawOutline(rect, Color.Lerp(AccentSoftColor, AccentColor, pulse * 0.35f));

            if (!ReduceUIAnimation)
            {
                float scanY = rect.y + Mathf.Repeat(AnimTime * 24f, Mathf.Max(1f, rect.height - 4f));
                Fill(new Rect(rect.x + 2f, scanY, rect.width - 4f, 1f), new Color(1f, 0.55f, 0.24f, 0.06f));
            }
        }

        public static void DrawHeader(Rect rect, string title, string subtitle, bool alert)
        {
            if (EnhancedUI)
            {
                float pulseEnhanced = Pulse(alert ? 2.0f : 1.4f, 0.12f);
                Fill(rect, new Color(0.078f, 0.045f, 0.036f, 0.985f));
                DrawOverlay(rect, OverlayTex, new Color(1f, 0.34f, 0.12f, alert ? 0.095f + pulseEnhanced * 0.035f : 0.060f));
                DrawOutline(rect, new Color(1f, 0.38f, 0.14f, alert ? 0.78f : 0.54f));

                Rect leftRail = new Rect(rect.x + 9f, rect.y + 8f, 4f, rect.height - 16f);
                Fill(leftRail, new Color(1f, 0.38f, 0.14f, alert ? 0.86f : 0.62f));
                Fill(new Rect(rect.x + 18f, rect.y + 7f, rect.width - 64f, 1f), new Color(1f, 0.48f, 0.18f, alert ? 0.46f : 0.28f));
                Fill(new Rect(rect.x + 18f, rect.yMax - 8f, rect.width - 52f, 1f), new Color(1f, 0.44f, 0.16f, alert ? 0.82f : 0.58f));

                Rect titleBack = new Rect(rect.x + 20f, rect.y + 8f, Mathf.Min(rect.width - 82f, 620f), 25f);
                Fill(titleBack, new Color(0.020f, 0.016f, 0.014f, 0.30f));
                Fill(new Rect(titleBack.x, titleBack.yMax - 1f, titleBack.width, 1f), new Color(1f, 0.44f, 0.16f, 0.20f + pulseEnhanced * 0.10f));

                if (!ReduceUIAnimation)
                {
                    AbyssalStyledWidgets.DrawAccentAnimation(new Rect(rect.x + 24f, rect.yMax - 20f, Mathf.Min(rect.width * 0.48f, 540f), 12f), AbyssalStyledWidgets.AbyssalAccentAnimation.EmberScanline, alert ? 4f : 7f, alert ? 0.20f : 0.12f);
                }

                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Medium;
                Rect titleRect = new Rect(rect.x + 26f, rect.y + 7f, rect.width - 92f, 30f);
                GUI.color = new Color(0f, 0f, 0f, 0.72f);
                ABY_UIPolishUtility.SafeLabel(new Rect(titleRect.x + 1.5f, titleRect.y + 1.5f, titleRect.width, titleRect.height), title);
                GUI.color = Color.white;
                ABY_UIPolishUtility.SafeLabel(titleRect, title);

                Text.Font = GameFont.Small;
                Rect subtitleRect = new Rect(rect.x + 26f, rect.y + 36f, rect.width - 102f, Mathf.Max(18f, rect.height - 40f));
                GUI.color = new Color(0.95f, 0.80f, 0.68f, 0.98f);
                ABY_UIPolishUtility.SafeLabel(subtitleRect, subtitle);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                DrawSignalGlyph(new Rect(rect.xMax - 34f, rect.y + 9f, 18f, 18f), pulseEnhanced);
                return;
            }

            float pulse = Pulse(alert ? 2.0f : 1.4f, 0.12f);
            Fill(rect, new Color(0.082f, 0.058f, 0.052f, 1f));
            DrawOverlay(rect, HeaderTex, new Color(1f, 0.42f, 0.18f, 0.46f + pulse * (alert ? 0.10f : 0.05f)));
            DrawOutline(rect, Color.Lerp(AccentSoftColor, AccentColor, alert ? 0.62f + pulse * 0.26f : 0.46f + pulse * 0.18f));

            Fill(new Rect(rect.x + 9f, rect.y + 8f, 4f, rect.height - 16f), new Color(1f, 0.42f, 0.16f, alert ? 0.88f : 0.62f));
            Fill(new Rect(rect.x + 20f, rect.y + rect.height - 7f, rect.width - 48f, 1f), new Color(1f, 0.42f, 0.16f, 0.70f));

            if (!ReduceUIAnimation)
            {
                float sweepX = rect.x - 90f + Mathf.Repeat(AnimTime * 105f, rect.width + 180f);
                Fill(new Rect(sweepX, rect.y + rect.height - 8f, 88f, 2f), new Color(1f, 0.76f, 0.54f, alert ? 0.30f : 0.18f));
            }

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Rect classicTitleRect = new Rect(rect.x + 26f, rect.y + 7f, rect.width - 86f, 31f);
            GUI.color = new Color(0f, 0f, 0f, 0.70f);
            ABY_UIPolishUtility.SafeLabel(new Rect(classicTitleRect.x + 1.5f, classicTitleRect.y + 1.5f, classicTitleRect.width, classicTitleRect.height), title);
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(classicTitleRect, title);

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.92f, 0.78f, 0.68f, 0.98f);
            float subtitleHeight = Text.CalcHeight(subtitle, rect.width - 98f);
            float maxSubtitleHeight = Mathf.Max(18f, rect.height - 42f);
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 26f, rect.y + 37f, rect.width - 98f, Mathf.Min(subtitleHeight, maxSubtitleHeight)), subtitle);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            DrawSignalGlyph(new Rect(rect.xMax - 34f, rect.y + 9f, 18f, 18f), pulse);
        }

        public static void DrawPanel(Rect rect, bool highlighted)
        {
            if (EnhancedUI)
            {
                float pulseEnhanced = Pulse(highlighted ? 2.0f : 1.15f, rect.x * 0.013f + rect.y * 0.009f);
                Color fill = highlighted
                    ? new Color(0.145f, 0.082f, 0.060f, 0.965f)
                    : new Color(0.080f, 0.074f, 0.078f, 0.965f);
                Color outline = highlighted
                    ? Color.Lerp(new Color(0.78f, 0.28f, 0.10f, 0.74f), new Color(1f, 0.55f, 0.22f, 0.92f), pulseEnhanced * 0.38f)
                    : new Color(0.76f, 0.26f, 0.10f, 0.50f);

                Fill(rect, fill);
                DrawOverlay(rect, OverlayTex, new Color(1f, 0.34f, 0.12f, highlighted ? 0.070f : 0.045f));
                DrawOutline(rect, outline);
                Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 1f), new Color(1f, 0.44f, 0.16f, highlighted ? 0.42f : 0.24f));
                Fill(new Rect(rect.x + 1f, rect.yMax - 2f, rect.width - 2f, 1f), new Color(0.92f, 0.28f, 0.10f, highlighted ? 0.34f : 0.18f));

                if (highlighted && !ReduceUIAnimation)
                {
                    AbyssalStyledWidgets.DrawAccentAnimation(new Rect(rect.x + 8f, rect.y + 1f, rect.width - 16f, 10f), AbyssalStyledWidgets.AbyssalAccentAnimation.EdgeGlow, 8f, 0.13f);
                }
                return;
            }

            float pulse = Pulse(highlighted ? 2.1f : 1.25f, highlighted ? 0.35f : 0.12f);
            Fill(rect, highlighted ? PanelAltColor : PanelColor);
            DrawOverlay(rect, OverlayTex, new Color(1f, 0.48f, 0.22f, highlighted ? 0.10f + pulse * (ReducedEffects ? 0.02f : 0.05f) : 0.05f + pulse * (ReducedEffects ? 0.02f : 0.03f)));
            DrawOutline(rect, highlighted ? Color.Lerp(AccentSoftColor, AccentColor, 0.35f + pulse * 0.25f) : Color.Lerp(AccentSoftColor, AccentColor, 0.10f + pulse * 0.12f));
            Fill(new Rect(rect.x, rect.y, rect.width, 2f), highlighted ? Color.Lerp(AccentSoftColor, AccentColor, 0.40f + pulse * 0.25f) : AccentSoftColor);

            if (!ReduceUIAnimation)
            {
                float sweep = rect.x - 70f + Mathf.Repeat(AnimTime * (highlighted ? 72f : 44f), rect.width + 140f);
                Fill(new Rect(sweep, rect.y + 1f, 68f, 1f), new Color(1f, 0.76f, 0.54f, highlighted ? 0.20f : 0.10f));
            }
        }

        public static void DrawSectionTitle(Rect rect, string title)
        {
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(rect, title);
            GUI.color = Color.white;
        }

        public static void DrawMetric(Rect rect, string label, string value)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = TextDimColor;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x, rect.y, rect.width, 14f), label);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x, rect.y + 14f, rect.width, rect.height - 14f), value);
            GUI.color = Color.white;
        }

        public static void DrawProgressBar(Rect rect, float fillPercent, string label, bool alert)
        {
            if (EnhancedUI)
            {
                Fill(rect, new Color(0.035f, 0.032f, 0.034f, 0.92f));
                DrawOutline(rect, alert ? new Color(1f, 0.44f, 0.16f, 0.92f) : new Color(0.95f, 0.36f, 0.13f, 0.70f));
                Rect enhancedFillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * Mathf.Clamp01(fillPercent), rect.height - 4f);
                if (enhancedFillRect.width > 1f)
                {
                    Color enhancedFillColor = alert ? new Color(1f, 0.54f, 0.22f, 0.92f) : new Color(0.92f, 0.40f, 0.16f, 0.86f);
                    Fill(enhancedFillRect, enhancedFillColor);
                    if (!ReduceUIAnimation)
                    {
                        AbyssalStyledWidgets.DrawAccentAnimation(new Rect(enhancedFillRect.x, enhancedFillRect.y, enhancedFillRect.width, enhancedFillRect.height), AbyssalStyledWidgets.AbyssalAccentAnimation.EmberScanline, alert ? 4f : 8f, alert ? 0.30f : 0.18f);
                    }
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.white;
                ABY_UIPolishUtility.SafeLabel(rect, label);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Fill(rect, new Color(0.04f, 0.04f, 0.045f, 1f));
            Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * Mathf.Clamp01(fillPercent), rect.height - 4f);
            Color fillColor = alert ? new Color(1f, 0.54f, 0.22f, 1f) : new Color(0.92f, 0.42f, 0.18f, 1f);
            Fill(fillRect, fillColor);
            DrawOverlay(fillRect, HeaderTex, new Color(1f, 0.72f, 0.42f, 0.20f + Pulse(alert ? 3.0f : 2.2f, 0.55f) * (alert ? 0.18f : 0.08f)));

            if (fillRect.width > 20f && !ReduceUIAnimation)
            {
                float sheenWidth = Mathf.Min(80f, fillRect.width);
                float sheenX = fillRect.x - sheenWidth + Mathf.Repeat(AnimTime * (alert ? 94f : 68f), fillRect.width + sheenWidth);
                Fill(new Rect(sheenX, fillRect.y, sheenWidth, fillRect.height), new Color(1f, 0.92f, 0.78f, alert ? 0.20f : 0.12f));
            }

            DrawOutline(rect, alert ? AccentColor : AccentSoftColor);

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(rect, label);
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

            if (EnhancedUI)
            {
                if (freshlyUnlocked || !ReduceUIAnimation)
                {
                    AbyssalStyledWidgets.DrawAccentAnimation(new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 12f), AbyssalStyledWidgets.AbyssalAccentAnimation.EdgeGlow, freshlyUnlocked ? 4f : 8f, freshlyUnlocked ? 0.30f : 0.16f);
                }
                return;
            }

            float pulse = Pulse(freshlyUnlocked ? 3.2f : 2.35f, rect.x * 0.01f + rect.y * 0.01f);
            Fill(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, freshlyUnlocked ? 3f : 2f), new Color(1f, 0.72f, 0.50f, freshlyUnlocked ? 0.16f + pulse * 0.12f : 0.08f + pulse * 0.08f));
            if (!ReduceUIAnimation)
            {
                float sweepX = rect.x - 36f + Mathf.Repeat(AnimTime * (freshlyUnlocked ? 92f : 58f) + rect.y * 0.4f, rect.width + 72f);
                Fill(new Rect(sweepX, rect.y + rect.height - 22f, 34f, 1f), new Color(1f, 0.82f, 0.66f, freshlyUnlocked ? 0.28f : 0.18f));
            }
        }

        public static void DrawTag(Rect rect, string label, bool alert)
        {
            if (EnhancedUI)
            {
                AbyssalStyledWidgets.DrawStatusStrip(rect, alert, alert ? 0.95f : 0.72f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                GUI.color = Color.white;
                ABY_UIPolishUtility.SafeLabel(rect.ContractedBy(2f), label);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Fill(rect, alert ? new Color(0.85f, 0.26f, 0.08f, 0.92f) : new Color(0.35f, 0.18f, 0.10f, 0.92f));
            DrawOutline(rect, alert ? new Color(1f, 0.72f, 0.44f, 0.9f) : AccentSoftColor);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            ABY_UIPolishUtility.SafeLabel(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static Texture2D GetCategoryIcon(string category)
        {
            if (EnhancedUI)
            {
                Texture2D enhancedIcon = GetEnhancedCategoryIcon(category);
                if (enhancedIcon != null)
                {
                    return enhancedIcon;
                }
            }

            if (category == AbyssalForgeProgressUtility.CoreCategory)
            {
                return IconCoreTex;
            }

            if (category == AbyssalForgeProgressUtility.WeaponsCategory)
            {
                return IconWeaponsTex;
            }

            if (category == AbyssalForgeProgressUtility.ArmorCategory)
            {
                return IconArmorTex;
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

            if (category == AbyssalForgeProgressUtility.TurretSystemsCategory)
            {
                return IconWeaponsTex ?? IconCoreTex;
            }

            return IconAllTex;
        }

        private static Texture2D GetEnhancedCategoryIcon(string category)
        {
            if (category == AbyssalForgeProgressUtility.AllCategory)
            {
                return AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.Forge);
            }

            if (category == AbyssalForgeProgressUtility.CoreCategory)
            {
                return AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.AbyssalCore) ?? AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.Forge);
            }

            if (category == AbyssalForgeProgressUtility.WeaponsCategory)
            {
                return AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.Weapon);
            }

            if (category == AbyssalForgeProgressUtility.ArmorCategory)
            {
                return AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.Armor);
            }

            if (category == AbyssalForgeProgressUtility.ImplantsCategory)
            {
                return AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.Implant);
            }

            if (category == AbyssalForgeProgressUtility.RitualCategory)
            {
                return AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.RitualMaterial);
            }

            if (category == AbyssalForgeProgressUtility.HeraldCategory)
            {
                return AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.Crown);
            }

            if (category == AbyssalForgeProgressUtility.TurretSystemsCategory)
            {
                return AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.Capacitor) ?? AbyssalStyledWidgets.GetCategoryIcon(AbyssalStyledWidgets.AbyssalCategoryIcon.Weapon);
            }

            return null;
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

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = selected ? Color.Lerp(new Color(1f, 0.72f, 0.52f, 1f), Color.white, Pulse(2.2f, rect.y * 0.01f) * 0.45f) : Color.white;
            ABY_UIPolishUtility.SafeLabel(new Rect(rect.x + 36f, rect.y, rect.width - 42f, rect.height), AbyssalForgeProgressUtility.GetCategoryLabel(category));
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
            float value = (Mathf.Sin(AnimTime * speed * (ReduceUIAnimation ? 0.45f : 1f) + offset) + 1f) * 0.5f;
            if (!ReduceUIAnimation)
            {
                return value;
            }

            return Mathf.Lerp(0.35f, 0.65f, value);
        }
    }
}
