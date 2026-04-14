using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class AbyssalStyledWidgets
    {
        private static readonly Texture2D ButtonNormalTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Button_Normal", false);
        private static readonly Texture2D ButtonHoverTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Button_Hover", false);
        private static readonly Texture2D ButtonPressedTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Button_Pressed", false);
        private static readonly Texture2D ButtonDisabledTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Button_Disabled", false);
        private static readonly Texture2D ButtonActiveTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Button_Active", false);

        private static readonly Texture2D TabNormalTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Tab_Normal", false);
        private static readonly Texture2D TabHoverTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Tab_Hover", false);
        private static readonly Texture2D TabPressedTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Tab_Pressed", false);
        private static readonly Texture2D TabDisabledTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Tab_Disabled", false);
        private static readonly Texture2D TabActiveTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_Tab_Active", false);

        private static readonly Texture2D IconFrameNormalTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_IconFrame_Normal", false);
        private static readonly Texture2D IconFrameHoverTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_IconFrame_Hover", false);
        private static readonly Texture2D IconFrameDisabledTex = ContentFinder<Texture2D>.Get("UI/AbyssalCommon/Buttons/ABY_IconFrame_Disabled", false);

        private static readonly Color DefaultTextColor = new Color(0.95f, 0.91f, 0.85f, 1f);
        private static readonly Color HoverTextColor = Color.white;
        private static readonly Color ActiveTextColor = new Color(1f, 0.86f, 0.72f, 1f);
        private static readonly Color DisabledTextColor = new Color(0.58f, 0.56f, 0.54f, 1f);
        private static readonly Color IconTint = new Color(0.98f, 0.86f, 0.74f, 0.98f);

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

        private static bool ButtonInternal(Rect rect, string label, bool enabled, bool active, Texture2D icon, string tooltip, bool useTabStyle, bool iconOnly)
        {
            bool hovered = Mouse.IsOver(rect);
            Event currentEvent = Event.current;
            bool pressed = enabled && hovered && currentEvent != null && currentEvent.button == 0 && (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag);
            Texture2D background = iconOnly
                ? GetIconFrameTexture(enabled, hovered)
                : GetTexture(useTabStyle, enabled, active, hovered, pressed);

            DrawTexture(rect, background);

            if (!iconOnly && hovered && enabled)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 0.86f, 0.68f, useTabStyle ? 0.06f : 0.08f);
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

                if (pressed)
                {
                    return TabPressedTex;
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

        private static Texture2D GetIconFrameTexture(bool enabled, bool hovered)
        {
            if (!enabled)
            {
                return IconFrameDisabledTex;
            }

            if (hovered)
            {
                return IconFrameHoverTex;
            }

            return IconFrameNormalTex;
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
            Rect labelRect = rect.ContractedBy(6f);
            if (icon != null)
            {
                labelRect.xMin += useTabStyle ? 28f : 26f;
            }

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = rect.height <= 28f ? GameFont.Tiny : GameFont.Small;
            if (Text.CalcSize(label).x > labelRect.width - 4f)
            {
                Text.Font = GameFont.Tiny;
            }

            if (rect.height <= 30f)
            {
                labelRect.y -= 1f;
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

            Widgets.Label(labelRect, label);

            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }
    }
}
