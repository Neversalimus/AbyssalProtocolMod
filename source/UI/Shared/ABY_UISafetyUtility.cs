using System;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_UISafetyUtility
    {
        public static bool TryDo(string context, Action action)
        {
            if (action == null)
            {
                return false;
            }

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                LogUIException(context, ex);
                return false;
            }
        }

        public static void LogUIException(string context, Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            string safeContext = SafeString(context, "Abyssal UI");
            Log.WarningOnce("[Abyssal Protocol] UI safety guard caught an exception in " + safeContext + ": " + ex, StableHash(safeContext) ^ 0x4A8B19);
        }

        public static void DrawWindowFallback(Rect inRect, string title, Exception ex)
        {
            LogUIException(title, ex);

            GUI.color = Color.white;
            Widgets.DrawMenuSection(inRect);

            Rect inner = inRect.ContractedBy(18f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 32f), SafeString(title, "Abyssal interface"));

            Text.Font = GameFont.Small;
            GUI.color = new Color(1f, 0.76f, 0.62f, 1f);
            Widgets.Label(
                new Rect(inner.x, inner.y + 42f, inner.width, 96f),
                "Abyssal Protocol recovered this UI panel from a null/compatibility error. Close and reopen the panel; gameplay state was not changed by this fallback.");
            GUI.color = Color.white;
        }

        public static string SafeString(string value, string fallback)
        {
            return value.NullOrEmpty() ? (fallback ?? string.Empty) : value;
        }

        public static string SafeDefLabel(Def def, string fallback = "unknown")
        {
            try
            {
                if (def == null)
                {
                    return fallback ?? "unknown";
                }

                string label = def.LabelCap;
                if (!label.NullOrEmpty())
                {
                    return label;
                }

                return SafeString(def.defName, fallback ?? "unknown");
            }
            catch
            {
                return fallback ?? "unknown";
            }
        }

        public static string SafeLowerLabel(Def def, string fallback = "unknown")
        {
            try
            {
                if (def == null)
                {
                    return fallback ?? "unknown";
                }

                if (!def.label.NullOrEmpty())
                {
                    return def.label;
                }

                return SafeString(def.defName, fallback ?? "unknown");
            }
            catch
            {
                return fallback ?? "unknown";
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                string text = value ?? string.Empty;
                for (int i = 0; i < text.Length; i++)
                {
                    hash = hash * 31 + text[i];
                }

                return hash;
            }
        }
    }
}
