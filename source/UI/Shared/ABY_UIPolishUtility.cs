using System;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_UIPolishUtility
    {
        public static Rect TextRect(Rect rect, float horizontalPadding = 2f, float verticalPadding = 2f)
        {
            // RimWorld IMGUI clips ascenders/descenders very easily in custom 14-22px rows.
            // Do not always move the rect upward: that fixed some centered labels but broke
            // Forge/Summoning upper-left rows. Expand according to the current anchor instead.
            float extraY = Mathf.Max(3f, verticalPadding + 2f);
            TextAnchor anchor = Text.Anchor;

            rect.x = Mathf.Floor(rect.x) + horizontalPadding;
            rect.width = Mathf.Max(1f, Mathf.Ceil(rect.width) - horizontalPadding * 2f);

            if (anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.MiddleCenter || anchor == TextAnchor.MiddleRight)
            {
                rect.y = Mathf.Floor(rect.y) - extraY;
                rect.height = Mathf.Max(1f, Mathf.Ceil(rect.height) + extraY * 2f);
            }
            else if (anchor == TextAnchor.LowerLeft || anchor == TextAnchor.LowerCenter || anchor == TextAnchor.LowerRight)
            {
                rect.y = Mathf.Floor(rect.y) - extraY * 2f;
                rect.height = Mathf.Max(1f, Mathf.Ceil(rect.height) + extraY * 2f);
            }
            else
            {
                rect.y = Mathf.Floor(rect.y);
                rect.height = Mathf.Max(1f, Mathf.Ceil(rect.height) + extraY * 2f);
            }

            return rect;
        }

        public static void SafeLabel(Rect rect, string text)
        {
            try
            {
                Widgets.Label(TextRect(rect), text ?? string.Empty);
            }
            catch (Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("safe label", ex);
            }
        }


        public static void SafeLabel(Rect rect, string text, float horizontalPadding, float verticalPadding)
        {
            try
            {
                Widgets.Label(TextRect(rect, horizontalPadding, verticalPadding), text ?? string.Empty);
            }
            catch (Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("safe label", ex);
            }
        }

        public static void SafeLabel(Rect rect, TaggedString text)
        {
            SafeLabel(rect, text.ToString());
        }

        public static void Label(Rect rect, string text, TextAnchor anchor, GameFont font)
        {
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            try
            {
                Text.Anchor = anchor;
                Text.Font = font;
                Widgets.Label(TextRect(rect), text ?? string.Empty);
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
