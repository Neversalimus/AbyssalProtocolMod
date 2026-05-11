using System;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_UIPolishUtility
    {
        public static Rect TextRect(Rect rect, float horizontalPadding = 2f, float verticalPadding = 2f)
        {
            rect.x = Mathf.Floor(rect.x) + horizontalPadding;
            rect.y = Mathf.Floor(rect.y) - 1f;
            rect.width = Mathf.Ceil(rect.width) - horizontalPadding * 2f;
            rect.height = Mathf.Ceil(rect.height) + verticalPadding * 2f + 2f;
            return rect;
        }

        public static Rect SafeTextRect(Rect rect, float horizontalPadding = 0f, float extraVertical = 8f)
        {
            float extra = Mathf.Max(0f, extraVertical);
            rect.x = Mathf.Floor(rect.x) + horizontalPadding;
            rect.y = Mathf.Floor(rect.y - extra * 0.5f);
            rect.width = Mathf.Max(1f, Mathf.Ceil(rect.width) - horizontalPadding * 2f);
            rect.height = Mathf.Max(1f, Mathf.Ceil(rect.height) + extra);
            return rect;
        }

        public static void SafeLabel(Rect rect, string text, float horizontalPadding = 0f, float extraVertical = 8f)
        {
            Widgets.Label(SafeTextRect(rect, horizontalPadding, extraVertical), text ?? string.Empty);
        }

        public static void SafeLabel(Rect rect, string text, TextAnchor anchor, GameFont font, float horizontalPadding = 0f, float extraVertical = 8f)
        {
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            try
            {
                Text.Anchor = anchor;
                Text.Font = font;
                Widgets.Label(SafeTextRect(rect, horizontalPadding, extraVertical), text ?? string.Empty);
            }
            catch (Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("safe label", ex);
            }
            finally
            {
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
            }
        }

        public static void SafeLabel(Rect rect, string text, TextAnchor anchor, GameFont font, Color color, float horizontalPadding = 0f, float extraVertical = 8f)
        {
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Anchor = anchor;
                Text.Font = font;
                GUI.color = color;
                Widgets.Label(SafeTextRect(rect, horizontalPadding, extraVertical), text ?? string.Empty);
            }
            catch (Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("safe colored label", ex);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
            }
        }

        public static void Label(Rect rect, string text, TextAnchor anchor, GameFont font)
        {
            SafeLabel(rect, text, anchor, font);
        }

        public static float WrappedHeight(string text, float width, GameFont font, float minHeight = 22f, float extra = 4f)
        {
            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = font;
                return Mathf.Max(minHeight, Text.CalcHeight(text ?? string.Empty, Mathf.Max(10f, width)) + extra);
            }
            catch
            {
                return minHeight;
            }
            finally
            {
                Text.Font = oldFont;
            }
        }
    }
}
