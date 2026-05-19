using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class Window_ABY_PerformanceAudit : Window
    {
        private Vector2 scroll;
        private List<string> cachedLines;

        public override Vector2 InitialSize => new Vector2(780f, 680f);

        public Window_ABY_PerformanceAudit()
        {
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = false;
            cachedLines = ABY_PerformanceAuditUtility.BuildStatusLines();
        }

        public static void OpenWindow()
        {
            Find.WindowStack?.Add(new Window_ABY_PerformanceAudit());
        }

        public override void DoWindowContents(Rect inRect)
        {
            ABY_UISafetyUtility.TryDo("Abyssal performance audit window", delegate
            {
                Text.Font = GameFont.Medium;
                ABY_UIPolishUtility.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "Abyssal Protocol performance audit", TextAnchor.MiddleLeft, GameFont.Medium);

                Rect buttonRow = new Rect(inRect.x, inRect.y + 42f, inRect.width, 34f);
                float buttonWidth = (buttonRow.width - 16f) / 3f;
                if (AbyssalStyledWidgets.TextButton(new Rect(buttonRow.x, buttonRow.y, buttonWidth, 34f), "Refresh"))
                {
                    cachedLines = ABY_PerformanceAuditUtility.BuildStatusLines();
                }

                if (AbyssalStyledWidgets.TextButton(new Rect(buttonRow.x + buttonWidth + 8f, buttonRow.y, buttonWidth, 34f), "Log snapshot"))
                {
                    ABY_PerformanceAuditUtility.LogSnapshot();
                }

                if (AbyssalStyledWidgets.TextButton(new Rect(buttonRow.x + (buttonWidth + 8f) * 2f, buttonRow.y, buttonWidth, 34f), "Copy report"))
                {
                    GUIUtility.systemCopyBuffer = ABY_PerformanceAuditUtility.BuildPlainTextReport();
                    Messages.Message("Abyssal performance audit copied to clipboard.", MessageTypeDefOf.NeutralEvent, false);
                }

                Rect outRect = new Rect(inRect.x, inRect.y + 88f, inRect.width, inRect.height - 96f);
                Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, Mathf.Max(outRect.height, ResolveViewHeight(outRect.width - 18f)));
                Widgets.BeginScrollView(outRect, ref scroll, viewRect);
                float y = 0f;
                if (cachedLines == null)
                {
                    cachedLines = ABY_PerformanceAuditUtility.BuildStatusLines();
                }

                for (int i = 0; i < cachedLines.Count; i++)
                {
                    string line = cachedLines[i] ?? string.Empty;
                    float height = Mathf.Max(24f, Text.CalcHeight(line, viewRect.width - 8f) + 6f);
                    ABY_UIPolishUtility.Label(new Rect(4f, y, viewRect.width - 8f, height), line, TextAnchor.UpperLeft, GameFont.Small);
                    y += height;
                }

                Widgets.EndScrollView();
            });
        }

        private float ResolveViewHeight(float width)
        {
            float total = 8f;
            List<string> lines = cachedLines ?? ABY_PerformanceAuditUtility.BuildStatusLines();
            for (int i = 0; i < lines.Count; i++)
            {
                total += Mathf.Max(24f, Text.CalcHeight(lines[i] ?? string.Empty, width - 8f) + 6f);
            }

            return total + 18f;
        }
    }
}
