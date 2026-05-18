using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class AbyssalStyledWidgets
    {
        private const string EnhancedThemeRoot = "UI/AbyssalCommon/Themes/Enhanced/";
        private const string ClassicRoot = "UI/AbyssalCommon/";

        private static readonly Texture2D ButtonNormalTex = LoadThemed("Buttons/ABY_Button_Normal");
        private static readonly Texture2D ButtonHoverTex = LoadThemed("Buttons/ABY_Button_Hover");
        private static readonly Texture2D ButtonPressedTex = LoadThemed("Buttons/ABY_Button_Pressed");
        private static readonly Texture2D ButtonDisabledTex = LoadThemed("Buttons/ABY_Button_Disabled");
        private static readonly Texture2D ButtonActiveTex = LoadThemed("Buttons/ABY_Button_Active");

        private static readonly Texture2D TabNormalTex = LoadThemed("Buttons/ABY_Tab_Normal");
        private static readonly Texture2D TabHoverTex = LoadThemed("Buttons/ABY_Tab_Hover");
        private static readonly Texture2D TabPressedTex = LoadThemed("Buttons/ABY_Tab_Pressed");
        private static readonly Texture2D TabDisabledTex = LoadThemed("Buttons/ABY_Tab_Disabled");
        private static readonly Texture2D TabActiveTex = LoadThemed("Buttons/ABY_Tab_Active");

        private static readonly Texture2D IconFrameNormalTex = LoadThemed("Buttons/ABY_IconFrame_Normal");
        private static readonly Texture2D IconFrameHoverTex = LoadThemed("Buttons/ABY_IconFrame_Hover");
        private static readonly Texture2D IconFramePressedTex = LoadThemed("Buttons/ABY_IconFrame_Pressed");
        private static readonly Texture2D IconFrameDisabledTex = LoadThemed("Buttons/ABY_IconFrame_Disabled");
        private static readonly Texture2D IconFrameActiveTex = LoadThemed("Buttons/ABY_IconFrame_Active");

        private static readonly Texture2D PanelMainTex = LoadThemed("Panels/ABY_Panel_Main", false);
        private static readonly Texture2D PanelCardTex = LoadThemed("Panels/ABY_Panel_Card", false);
        private static readonly Texture2D PanelCardDarkTex = LoadThemed("Panels/ABY_Panel_Card_Dark", false);
        private static readonly Texture2D PanelRequirementTex = LoadThemed("Panels/ABY_Panel_Requirement", false);
        private static readonly Texture2D PanelRewardTex = LoadThemed("Panels/ABY_Panel_Reward", false);
        private static readonly Texture2D PanelWarningTex = LoadThemed("Panels/ABY_Panel_Warning", false);
        private static readonly Texture2D PanelLockedTex = LoadThemed("Panels/ABY_Panel_Locked", false);
        private static readonly Texture2D PanelSelectedTex = LoadThemed("Panels/ABY_Panel_Selected", false);
        private static readonly Texture2D PanelTooltipTex = LoadThemed("Panels/ABY_Panel_Tooltip", false);
        private static readonly Texture2D HeaderStripTex = LoadThemed("Panels/ABY_HeaderStrip", false);
        private static readonly Texture2D FooterStripTex = LoadThemed("Panels/ABY_FooterStrip", false);

        private static readonly Texture2D DividerHorizontalTex = LoadThemed("Accents/ABY_Divider_Horizontal", false);
        private static readonly Texture2D DividerVerticalTex = LoadThemed("Accents/ABY_Divider_Vertical", false);
        private static readonly Texture2D CornerTopLeftTex = LoadThemed("Accents/ABY_Corner_TopLeft", false);
        private static readonly Texture2D CornerTopRightTex = LoadThemed("Accents/ABY_Corner_TopRight", false);
        private static readonly Texture2D CornerBottomLeftTex = LoadThemed("Accents/ABY_Corner_BottomLeft", false);
        private static readonly Texture2D CornerBottomRightTex = LoadThemed("Accents/ABY_Corner_BottomRight", false);
        private static readonly Texture2D SocketNormalTex = LoadThemed("Accents/ABY_Socket_Normal", false);
        private static readonly Texture2D SocketActiveTex = LoadThemed("Accents/ABY_Socket_Active", false);
        private static readonly Texture2D StatusStripTex = LoadThemed("Accents/ABY_StatusStrip", false);
        private static readonly Texture2D WarningStripTex = LoadThemed("Accents/ABY_WarningStrip", false);

        private static readonly Texture2D IconSummoningTex = LoadThemed("Icons/ABY_Icon_Summoning", false);
        private static readonly Texture2D IconForgeTex = LoadThemed("Icons/ABY_Icon_Forge", false);
        private static readonly Texture2D IconWeaponTex = LoadThemed("Icons/ABY_Icon_Weapon", false);
        private static readonly Texture2D IconArmorTex = LoadThemed("Icons/ABY_Icon_Armor", false);
        private static readonly Texture2D IconImplantTex = LoadThemed("Icons/ABY_Icon_Implant", false);
        private static readonly Texture2D IconRitualMaterialTex = LoadThemed("Icons/ABY_Icon_RitualMaterial", false);
        private static readonly Texture2D IconSigilTex = LoadThemed("Icons/ABY_Icon_Sigil", false);
        private static readonly Texture2D IconCrownTex = LoadThemed("Icons/ABY_Icon_Crown", false);
        private static readonly Texture2D IconResidueTex = LoadThemed("Icons/ABY_Icon_Residue", false);
        private static readonly Texture2D IconAbyssalCoreTex = LoadThemed("Icons/ABY_Icon_AbyssalCore", false);
        private static readonly Texture2D IconCapacitorTex = LoadThemed("Icons/ABY_Icon_Capacitor", false);
        private static readonly Texture2D IconInstabilityTex = LoadThemed("Icons/ABY_Icon_Instability", false);
        private static readonly Texture2D BadgeLockedTex = LoadThemed("Icons/ABY_Badge_Locked", false);
        private static readonly Texture2D BadgeUnlockedTex = LoadThemed("Icons/ABY_Badge_Unlocked", false);
        private static readonly Texture2D BadgeReadyTex = LoadThemed("Icons/ABY_Badge_Ready", false);
        private static readonly Texture2D BadgeForbiddenTex = LoadThemed("Icons/ABY_Badge_Forbidden", false);

        private static readonly Texture2D[] EmberScanlineFrames = LoadAnimation("Accents/ABY_EmberScanline", 8);
        private static readonly Texture2D[] RitualSocketPulseFrames = LoadAnimation("Accents/ABY_RitualSocketPulse", 8);
        private static readonly Texture2D[] EdgeGlowFrames = LoadAnimation("Accents/ABY_EdgeGlow", 8);

        private static readonly Color DefaultTextColor = new Color(0.95f, 0.91f, 0.85f, 1f);
        private static readonly Color HoverTextColor = Color.white;
        private static readonly Color ActiveTextColor = new Color(1f, 0.86f, 0.72f, 1f);
        private static readonly Color DisabledTextColor = new Color(0.58f, 0.56f, 0.54f, 1f);
        private static readonly Color IconTint = new Color(0.98f, 0.86f, 0.74f, 0.98f);
        private static readonly Color FallbackPanelColor = new Color(0.11f, 0.075f, 0.065f, 0.94f);
        private static readonly Color FallbackPanelOutlineColor = new Color(0.62f, 0.28f, 0.12f, 0.62f);

        public static bool UseEnhancedTheme
        {
            get
            {
                return AbyssalProtocolMod.Settings.uiStyle == ABY_UIStyle.Enhanced;
            }
        }

        public static bool ReduceAbyssalUIAnimation
        {
            get
            {
                return AbyssalProtocolMod.Settings.reduceAbyssalUIAnimation;
            }
        }

        public enum AbyssalPanelStyle
        {
            Main,
            Card,
            CardDark,
            Requirement,
            Reward,
            Warning,
            Locked,
            Selected,
            Tooltip,
            Header,
            Footer
        }

        public enum AbyssalCategoryIcon
        {
            Summoning,
            Forge,
            Weapon,
            Armor,
            Implant,
            RitualMaterial,
            Sigil,
            Crown,
            Residue,
            AbyssalCore,
            Capacitor,
            Instability,
            Locked,
            Unlocked,
            Ready,
            Forbidden
        }

        public enum AbyssalAccentAnimation
        {
            EmberScanline,
            RitualSocketPulse,
            EdgeGlow
        }

        public static bool TextButton(Rect rect, string label, bool enabled = true, bool active = false, Texture2D icon = null, string tooltip = null)
        {
            return ButtonInternal(rect, label, enabled, active, icon, tooltip, false, false);
        }

        public static bool TabButton(Rect rect, string label, Texture2D icon, bool active, bool enabled = true, string tooltip = null)
        {
            return ButtonInternal(rect, label, enabled, active, icon, tooltip, true, false);
        }

        public static bool IconButton(Rect rect, Texture2D icon, bool enabled = true, bool active = false, string tooltip = null)
        {
            return ButtonInternal(rect, null, enabled, active, icon, tooltip, false, true);
        }

        public static void DrawPanel(Rect rect, AbyssalPanelStyle style = AbyssalPanelStyle.Card, float alpha = 1f)
        {
            Texture2D texture = GetPanelTexture(style);
            DrawTextureWithFallback(rect, texture, alpha, FallbackPanelColor, true);
        }

        public static void DrawCornerBrackets(Rect rect, float cornerSize = 42f, float alpha = 1f)
        {
            if (cornerSize <= 0f)
            {
                return;
            }

            DrawTextureWithFallback(new Rect(rect.x, rect.y, cornerSize, cornerSize), CornerTopLeftTex, alpha, Color.clear, false);
            DrawTextureWithFallback(new Rect(rect.xMax - cornerSize, rect.y, cornerSize, cornerSize), CornerTopRightTex, alpha, Color.clear, false);
            DrawTextureWithFallback(new Rect(rect.x, rect.yMax - cornerSize, cornerSize, cornerSize), CornerBottomLeftTex, alpha, Color.clear, false);
            DrawTextureWithFallback(new Rect(rect.xMax - cornerSize, rect.yMax - cornerSize, cornerSize, cornerSize), CornerBottomRightTex, alpha, Color.clear, false);
        }

        public static void DrawFramedPanel(Rect rect, AbyssalPanelStyle style = AbyssalPanelStyle.Card, float cornerSize = 38f, float alpha = 1f)
        {
            DrawPanel(rect, style, alpha);
            DrawCornerBrackets(rect, cornerSize, alpha);
        }

        public static void DrawDividerHorizontal(Rect rect, float alpha = 1f)
        {
            DrawTextureWithFallback(rect, DividerHorizontalTex, alpha, FallbackPanelOutlineColor, false);
        }

        public static void DrawDividerVertical(Rect rect, float alpha = 1f)
        {
            DrawTextureWithFallback(rect, DividerVerticalTex, alpha, FallbackPanelOutlineColor, false);
        }

        public static void DrawStatusSocket(Rect rect, bool active, float alpha = 1f)
        {
            DrawTextureWithFallback(rect, active ? SocketActiveTex : SocketNormalTex, alpha, FallbackPanelOutlineColor, false);
        }

        public static void DrawStatusStrip(Rect rect, bool warning = false, float alpha = 1f)
        {
            DrawTextureWithFallback(rect, warning ? WarningStripTex : StatusStripTex, alpha, warning ? new Color(0.65f, 0.12f, 0.06f, 0.72f) : FallbackPanelOutlineColor, false);
        }

        public static Texture2D GetCategoryIcon(AbyssalCategoryIcon icon)
        {
            switch (icon)
            {
                case AbyssalCategoryIcon.Summoning:
                    return IconSummoningTex;
                case AbyssalCategoryIcon.Forge:
                    return IconForgeTex;
                case AbyssalCategoryIcon.Weapon:
                    return IconWeaponTex;
                case AbyssalCategoryIcon.Armor:
                    return IconArmorTex;
                case AbyssalCategoryIcon.Implant:
                    return IconImplantTex;
                case AbyssalCategoryIcon.RitualMaterial:
                    return IconRitualMaterialTex;
                case AbyssalCategoryIcon.Sigil:
                    return IconSigilTex;
                case AbyssalCategoryIcon.Crown:
                    return IconCrownTex;
                case AbyssalCategoryIcon.Residue:
                    return IconResidueTex;
                case AbyssalCategoryIcon.AbyssalCore:
                    return IconAbyssalCoreTex;
                case AbyssalCategoryIcon.Capacitor:
                    return IconCapacitorTex;
                case AbyssalCategoryIcon.Instability:
                    return IconInstabilityTex;
                case AbyssalCategoryIcon.Locked:
                    return BadgeLockedTex;
                case AbyssalCategoryIcon.Unlocked:
                    return BadgeUnlockedTex;
                case AbyssalCategoryIcon.Ready:
                    return BadgeReadyTex;
                case AbyssalCategoryIcon.Forbidden:
                    return BadgeForbiddenTex;
                default:
                    return null;
            }
        }

        public static void DrawCategoryIcon(Rect rect, AbyssalCategoryIcon icon, Color? tint = null, float alpha = 1f)
        {
            Texture2D texture = GetCategoryIcon(icon);
            if (texture == null)
            {
                return;
            }

            Color oldColor = GUI.color;
            Color resolvedTint = tint ?? Color.white;
            resolvedTint.a *= alpha;
            GUI.color = resolvedTint;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            GUI.color = oldColor;
        }

        public static void DrawAccentAnimation(Rect rect, AbyssalAccentAnimation animation, float ticksPerFrame = 6f, float alpha = 1f)
        {
            Texture2D[] frames = GetAnimationFrames(animation);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (ReduceAbyssalUIAnimation)
            {
                DrawTextureWithFallback(rect, frames[0], alpha, Color.clear, false);
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : Mathf.FloorToInt(Time.realtimeSinceStartup * 60f);
            int frameIndex = Mathf.Abs(Mathf.FloorToInt(ticks / Mathf.Max(1f, ticksPerFrame))) % frames.Length;
            DrawTextureWithFallback(rect, frames[frameIndex], alpha, Color.clear, false);
        }

        public static void BeginAbyssalScrollView(Rect outRect, ref Vector2 scrollPosition, Rect viewRect, bool showVanillaScrollbar = true)
        {
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, showVanillaScrollbar);
        }

        public static void EndAbyssalScrollView(Rect outRect, ref Vector2 scrollPosition, Rect viewRect, bool drawVerticalScrollbar = true)
        {
            Widgets.EndScrollView();
            if (drawVerticalScrollbar)
            {
                DrawAbyssalVerticalScrollbar(outRect, ref scrollPosition, viewRect);
            }
        }

        public static void DrawAbyssalVerticalScrollbar(Rect outRect, ref Vector2 scrollPosition, Rect viewRect)
        {
            if (outRect.width <= 0f || outRect.height <= 0f || viewRect.height <= outRect.height + 1f)
            {
                return;
            }

            const float coverWidth = 16f;
            const float trackWidth = 8f;
            const float trackInset = 4f;
            const float minThumbHeight = 28f;

            Rect coverRect = new Rect(outRect.xMax - coverWidth, outRect.y, coverWidth, outRect.height);
            Rect trackRect = new Rect(
                coverRect.x + (coverRect.width - trackWidth) * 0.5f,
                coverRect.y + trackInset,
                trackWidth,
                Mathf.Max(1f, coverRect.height - trackInset * 2f));

            if (trackRect.height <= 1f)
            {
                return;
            }

            float maxScroll = Mathf.Max(1f, viewRect.height - outRect.height);
            scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0f, maxScroll);
            float normalized = Mathf.Clamp01(scrollPosition.y / maxScroll);
            float thumbHeight = Mathf.Clamp(trackRect.height * Mathf.Clamp01(outRect.height / Mathf.Max(outRect.height, viewRect.height)), minThumbHeight, trackRect.height);
            float travel = Mathf.Max(0f, trackRect.height - thumbHeight);
            Rect thumbRect = new Rect(trackRect.x, trackRect.y + travel * normalized, trackRect.width, thumbHeight);

            int controlId = GUIUtility.GetControlID(FocusType.Passive, coverRect);
            Event currentEvent = Event.current;
            bool hovered = Mouse.IsOver(coverRect);
            bool active = GUIUtility.hotControl == controlId;

            if (currentEvent != null)
            {
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && coverRect.Contains(currentEvent.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                    active = true;
                    SetAbyssalScrollbarPositionFromMouse(trackRect, thumbHeight, maxScroll, ref scrollPosition, currentEvent.mousePosition.y);
                    currentEvent.Use();
                }
                else if (active && currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
                {
                    SetAbyssalScrollbarPositionFromMouse(trackRect, thumbHeight, maxScroll, ref scrollPosition, currentEvent.mousePosition.y);
                    currentEvent.Use();
                }
                else if (active && currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
                {
                    GUIUtility.hotControl = 0;
                    active = false;
                    currentEvent.Use();
                }
            }

            DrawAbyssalScrollbarRect(coverRect, new Color(0.035f, 0.027f, 0.024f, hovered || active ? 0.94f : 0.82f));
            DrawAbyssalScrollbarRect(trackRect, new Color(0.075f, 0.055f, 0.048f, hovered || active ? 0.92f : 0.72f));
            DrawAbyssalScrollbarOutline(trackRect, new Color(0.68f, 0.24f, 0.10f, hovered || active ? 0.42f : 0.24f));

            Color thumbFill = active
                ? new Color(0.42f, 0.18f, 0.08f, 0.98f)
                : hovered
                    ? new Color(0.34f, 0.14f, 0.065f, 0.96f)
                    : new Color(0.22f, 0.105f, 0.055f, 0.92f);
            Color thumbOutline = active
                ? new Color(1f, 0.58f, 0.22f, 0.92f)
                : hovered
                    ? new Color(1f, 0.43f, 0.16f, 0.74f)
                    : new Color(0.86f, 0.30f, 0.11f, 0.54f);

            Rect thumbInset = thumbRect.ContractedBy(1f);
            DrawAbyssalScrollbarRect(thumbInset, thumbFill);
            DrawAbyssalScrollbarOutline(thumbInset, thumbOutline);
            DrawAbyssalScrollbarRect(new Rect(thumbInset.x + 1f, thumbInset.y + 2f, Mathf.Max(1f, thumbInset.width - 2f), 1f), new Color(1f, 0.54f, 0.20f, active ? 0.60f : hovered ? 0.44f : 0.26f));
            DrawAbyssalScrollbarRect(new Rect(thumbInset.x + 1f, thumbInset.yMax - 3f, Mathf.Max(1f, thumbInset.width - 2f), 1f), new Color(0.70f, 0.18f, 0.08f, active ? 0.58f : hovered ? 0.38f : 0.22f));
        }

        private static void SetAbyssalScrollbarPositionFromMouse(Rect trackRect, float thumbHeight, float maxScroll, ref Vector2 scrollPosition, float mouseY)
        {
            float travel = Mathf.Max(1f, trackRect.height - thumbHeight);
            float normalized = Mathf.Clamp01((mouseY - trackRect.y - thumbHeight * 0.5f) / travel);
            scrollPosition.y = Mathf.Clamp(normalized * maxScroll, 0f, maxScroll);
        }

        private static void DrawAbyssalScrollbarRect(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = oldColor;
        }

        private static void DrawAbyssalScrollbarOutline(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            Widgets.DrawBox(rect, 1);
            GUI.color = oldColor;
        }

        private static bool ButtonInternal(Rect rect, string label, bool enabled, bool active, Texture2D icon, string tooltip, bool useTabStyle, bool iconOnly)
        {
            bool hovered = Mouse.IsOver(rect);
            Event currentEvent = Event.current;
            bool pressed = enabled && hovered && currentEvent != null && currentEvent.button == 0 && (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag);

            if (!UseEnhancedTheme)
            {
                DrawClassicButtonBackground(rect, enabled, active, hovered, pressed, useTabStyle, iconOnly);
            }
            else
            {
                Texture2D background = iconOnly
                    ? GetIconFrameTexture(enabled, active, hovered, pressed)
                    : GetTexture(useTabStyle, enabled, active, hovered, pressed);
                DrawTexture(rect, background);
            }

            if (!iconOnly && !useTabStyle && hovered && enabled)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 0.86f, 0.68f, 0.08f);
                GUI.DrawTexture(rect.ContractedBy(2f), BaseContent.WhiteTex);
                GUI.color = oldColor;
            }

            if (icon != null)
            {
                DrawIcon(rect, icon, iconOnly, enabled, active, useTabStyle);
            }

            if (!iconOnly && !label.NullOrEmpty())
            {
                DrawLabel(rect, label, enabled, active, hovered, icon, useTabStyle);
            }

            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            return enabled && Widgets.ButtonInvisible(rect);
        }

        private static Texture2D GetTexture(bool useTabStyle, bool enabled, bool active, bool hovered, bool pressed)
        {
            if (useTabStyle)
            {
                if (!enabled)
                {
                    return TabDisabledTex;
                }

                // Generated tab pressed skins can visually contract inside their transparent canvas.
                // Keep tabs layout-stable while the mouse is down so category labels/icons never appear to spill outside the frame.
                if (pressed)
                {
                    return active ? TabActiveTex : TabHoverTex;
                }

                if (active)
                {
                    return TabActiveTex;
                }

                if (hovered)
                {
                    return TabHoverTex;
                }

                return TabNormalTex;
            }

            if (!enabled)
            {
                return ButtonDisabledTex;
            }

            if (pressed)
            {
                return ButtonPressedTex;
            }

            if (active)
            {
                return ButtonActiveTex;
            }

            if (hovered)
            {
                return ButtonHoverTex;
            }

            return ButtonNormalTex;
        }

        private static Texture2D GetIconFrameTexture(bool enabled, bool active, bool hovered, bool pressed)
        {
            if (!enabled)
            {
                return IconFrameDisabledTex;
            }

            if (pressed && IconFramePressedTex != null)
            {
                return IconFramePressedTex;
            }

            if (active && IconFrameActiveTex != null)
            {
                return IconFrameActiveTex;
            }

            if (hovered)
            {
                return IconFrameHoverTex;
            }

            return IconFrameNormalTex;
        }

        private static Texture2D GetPanelTexture(AbyssalPanelStyle style)
        {
            switch (style)
            {
                case AbyssalPanelStyle.Main:
                    return PanelMainTex;
                case AbyssalPanelStyle.Card:
                    return PanelCardTex;
                case AbyssalPanelStyle.CardDark:
                    return PanelCardDarkTex;
                case AbyssalPanelStyle.Requirement:
                    return PanelRequirementTex;
                case AbyssalPanelStyle.Reward:
                    return PanelRewardTex;
                case AbyssalPanelStyle.Warning:
                    return PanelWarningTex;
                case AbyssalPanelStyle.Locked:
                    return PanelLockedTex;
                case AbyssalPanelStyle.Selected:
                    return PanelSelectedTex;
                case AbyssalPanelStyle.Tooltip:
                    return PanelTooltipTex;
                case AbyssalPanelStyle.Header:
                    return HeaderStripTex;
                case AbyssalPanelStyle.Footer:
                    return FooterStripTex;
                default:
                    return PanelCardTex;
            }
        }

        private static Texture2D[] GetAnimationFrames(AbyssalAccentAnimation animation)
        {
            switch (animation)
            {
                case AbyssalAccentAnimation.EmberScanline:
                    return EmberScanlineFrames;
                case AbyssalAccentAnimation.RitualSocketPulse:
                    return RitualSocketPulseFrames;
                case AbyssalAccentAnimation.EdgeGlow:
                    return EdgeGlowFrames;
                default:
                    return null;
            }
        }

        private static void DrawClassicButtonBackground(Rect rect, bool enabled, bool active, bool hovered, bool pressed, bool useTabStyle, bool iconOnly)
        {
            Color fill;
            Color outline;
            if (!enabled)
            {
                fill = new Color(0.12f, 0.11f, 0.105f, iconOnly ? 0.72f : 0.88f);
                outline = new Color(0.34f, 0.30f, 0.27f, 0.70f);
            }
            else if (pressed)
            {
                fill = new Color(0.22f, 0.10f, 0.065f, 0.98f);
                outline = new Color(1f, 0.42f, 0.16f, 0.92f);
            }
            else if (active)
            {
                fill = new Color(0.27f, 0.08f, 0.035f, 0.98f);
                outline = new Color(1f, 0.50f, 0.20f, 0.95f);
            }
            else if (hovered)
            {
                fill = new Color(0.17f, 0.095f, 0.065f, 0.96f);
                outline = new Color(0.95f, 0.38f, 0.14f, 0.86f);
            }
            else
            {
                fill = new Color(0.10f, 0.075f, 0.065f, iconOnly ? 0.76f : 0.94f);
                outline = new Color(0.56f, 0.23f, 0.11f, 0.74f);
            }

            Color oldColor = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = outline;
            Widgets.DrawBox(rect, 1);
            if ((active || hovered) && enabled)
            {
                GUI.color = new Color(1f, 0.52f, 0.20f, active ? 0.22f : 0.12f);
                GUI.DrawTexture(new Rect(rect.x + 4f, rect.yMax - 3f, rect.width - 8f, 1f), BaseContent.WhiteTex);
            }
            GUI.color = oldColor;
        }

        private static void DrawTexture(Rect rect, Texture2D texture)
        {
            if (texture != null)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = new Color(0.18f, 0.11f, 0.09f, 0.96f);
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = oldColor;
        }

        private static void DrawTextureWithFallback(Rect rect, Texture2D texture, float alpha, Color fallbackColor, bool drawFallbackOutline)
        {
            Color oldColor = GUI.color;
            if (texture != null)
            {
                GUI.color = new Color(oldColor.r, oldColor.g, oldColor.b, oldColor.a * Mathf.Clamp01(alpha));
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
                GUI.color = oldColor;
                return;
            }

            if (fallbackColor.a > 0f)
            {
                Color color = fallbackColor;
                color.a *= Mathf.Clamp01(alpha);
                GUI.color = color;
                GUI.DrawTexture(rect, BaseContent.WhiteTex);
                if (drawFallbackOutline)
                {
                    GUI.color = FallbackPanelOutlineColor;
                    Widgets.DrawBox(rect, 1);
                }
            }

            GUI.color = oldColor;
        }

        private static void DrawIcon(Rect rect, Texture2D icon, bool iconOnly, bool enabled, bool active, bool useTabStyle)
        {
            Rect iconRect;
            if (iconOnly)
            {
                float size = Mathf.Min(rect.width, rect.height) - 10f;
                iconRect = new Rect(rect.center.x - size / 2f, rect.center.y - size / 2f, size, size);
            }
            else if (useTabStyle)
            {
                float size = Mathf.Min(rect.height - 10f, 18f);
                iconRect = new Rect(rect.x + 10f, rect.center.y - size / 2f, size, size);
            }
            else
            {
                float size = Mathf.Min(rect.height - 10f, 18f);
                iconRect = new Rect(rect.x + 10f, rect.center.y - size / 2f, size, size);
            }

            Color oldColor = GUI.color;
            if (!enabled)
            {
                GUI.color = new Color(0.58f, 0.56f, 0.54f, 0.9f);
            }
            else if (active)
            {
                GUI.color = ActiveTextColor;
            }
            else
            {
                GUI.color = IconTint;
            }

            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
            GUI.color = oldColor;
        }

        private static void DrawLabel(Rect rect, string label, bool enabled, bool active, bool hovered, Texture2D icon, bool useTabStyle)
        {
            float horizontalInset = rect.height <= 30f ? 8f : 10f;
            float verticalInset = rect.height <= 24f ? 1f : 2f;
            Rect labelRect = new Rect(
                rect.x + horizontalInset,
                rect.y + verticalInset,
                Mathf.Max(1f, rect.width - horizontalInset * 2f),
                Mathf.Max(1f, rect.height - verticalInset * 2f));

            bool tabWithIcon = useTabStyle && icon != null;
            if (icon != null)
            {
                if (tabWithIcon)
                {
                    labelRect.xMin = rect.x + Mathf.Min(36f, rect.width * 0.30f);
                    labelRect.width = Mathf.Max(1f, rect.xMax - 8f - labelRect.xMin);
                }
                else
                {
                    labelRect.xMin += 26f;
                }
            }

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            Text.Anchor = tabWithIcon ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            Text.Font = rect.height <= 24f ? GameFont.Tiny : GameFont.Small;
            if (Text.CalcSize(label).x > labelRect.width - 4f)
            {
                Text.Font = GameFont.Tiny;
            }

            if (!enabled)
            {
                GUI.color = DisabledTextColor;
            }
            else if (active)
            {
                GUI.color = ActiveTextColor;
            }
            else if (hovered)
            {
                GUI.color = HoverTextColor;
            }
            else
            {
                GUI.color = DefaultTextColor;
            }

            ABY_UIPolishUtility.SafeLabel(labelRect, label, 0f, rect.height <= 24f ? 10f : 8f);

            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }

        private static Texture2D LoadThemed(string relativePath, bool allowClassicFallback = true)
        {
            Texture2D themed = ContentFinder<Texture2D>.Get(EnhancedThemeRoot + relativePath, false);
            if (themed != null)
            {
                return themed;
            }

            if (allowClassicFallback)
            {
                return ContentFinder<Texture2D>.Get(ClassicRoot + relativePath, false);
            }

            return null;
        }

        private static Texture2D[] LoadAnimation(string prefix, int frameCount)
        {
            Texture2D[] frames = new Texture2D[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                frames[i] = LoadThemed(prefix + "_" + i.ToString("00"), false);
            }

            return frames;
        }
    }
}
