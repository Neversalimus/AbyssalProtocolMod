using System;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_UIPolishUtility
    {
        public static Rect TextRect(Rect rect, float horizontalPadding = 2f, float verticalPadding = 2f)
        {
            // RimWorld/Unity IMGUI text is very easy to clip when custom textured panels use
            // tight 14-22px rows. Expand symmetrically so MiddleCenter labels keep their
            // visual center while UpperLeft labels get enough descender room for g/y/p/q.
            float extraY = Mathf.Max(3f, verticalPadding + 2f);
            rect.x = Mathf.Floor(rect.x) + horizontalPadding;
            rect.y = Mathf.Floor(rect.y) - extraY;
            rect.width = Mathf.Max(1f, Mathf.Ceil(rect.width) - horizontalPadding * 2f);
            rect.height = Mathf.Max(1f, Mathf.Ceil(rect.height) + extraY * 2f);
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
