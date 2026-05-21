using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_MiniBossHealthBarRenderer
    {
        private const float BaseWidth = 138f;
        private const float BaseHeight = 11f;
        private const float LabelHeight = 16f;
        private const float BorderThickness = 1f;

        private static readonly Color BackColor = new Color(0.035f, 0.022f, 0.018f, 0.88f);
        private static readonly Color BorderColor = new Color(0.62f, 0.32f, 0.16f, 0.92f);
        private static readonly Color TrailColor = new Color(0.75f, 0.28f, 0.16f, 0.48f);
        private static readonly Color FillColor = new Color(0.92f, 0.18f, 0.11f, 0.95f);
        private static readonly Color CriticalFillColor = new Color(1f, 0.58f, 0.18f, 0.98f);
        private static readonly Color TextColor = new Color(0.92f, 0.84f, 0.76f, 0.96f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.74f);

        public static void Draw(List<Pawn> miniBosses, AbyssalProtocolModSettings settings)
        {
            if (miniBosses == null || miniBosses.Count == 0 || settings == null)
            {
                return;
            }

            Camera camera = Find.Camera;
            if (camera == null)
            {
                return;
            }

            Color oldColor = GUI.color;
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            bool oldWordWrap = Text.WordWrap;

            try
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.WordWrap = false;

                for (int i = 0; i < miniBosses.Count; i++)
                {
                    DrawOne(miniBosses[i], camera, settings);
                }
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
                Text.Anchor = oldAnchor;
                Text.WordWrap = oldWordWrap;
            }
        }

        private static void DrawOne(Pawn pawn, Camera camera, AbyssalProtocolModSettings settings)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.MapHeld != Find.CurrentMap)
            {
                return;
            }

            float current;
            float max;
            float pct;
            if (!ABY_BossTrueDeathUtility.TryGetBossHp(pawn, out current, out max, out pct) || max <= 0.001f)
            {
                return;
            }

            pct = Mathf.Clamp01(pct);
            current = Mathf.Clamp(current, 0f, max);

            Rect barRect;
            if (!TryResolveBarRect(pawn, camera, out barRect))
            {
                return;
            }

            Rect labelRect = new Rect(barRect.x - 8f, barRect.y - LabelHeight + 1f, barRect.width + 16f, LabelHeight);
            Rect backingRect = new Rect(barRect.x - 3f, barRect.y - 1f, barRect.width + 6f, barRect.height + 3f);

            GUI.color = ShadowColor;
            GUI.DrawTexture(labelRect, BaseContent.WhiteTex);
            GUI.DrawTexture(backingRect, BaseContent.WhiteTex);

            string label = ResolveDisplayLabel(pawn, settings, current, max);
            GUI.color = TextColor;
            Widgets.Label(labelRect, label);

            GUI.color = BackColor;
            GUI.DrawTexture(barRect, BaseContent.WhiteTex);

            Rect trailRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(ABY_BossTrueDeathUtility.ResolveBossHealthPercentForPhase(pawn)), barRect.height);
            GUI.color = TrailColor;
            GUI.DrawTexture(trailRect, BaseContent.WhiteTex);

            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * pct, barRect.height);
            GUI.color = pct <= 0.18f ? CriticalFillColor : FillColor;
            GUI.DrawTexture(fillRect, BaseContent.WhiteTex);

            GUI.color = BorderColor;
            GUI.DrawTexture(new Rect(barRect.x, barRect.y, barRect.width, BorderThickness), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(barRect.x, barRect.yMax - BorderThickness, barRect.width, BorderThickness), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(barRect.x, barRect.y, BorderThickness, barRect.height), BaseContent.WhiteTex);
            GUI.DrawTexture(new Rect(barRect.xMax - BorderThickness, barRect.y, BorderThickness, barRect.height), BaseContent.WhiteTex);

            TooltipHandler.TipRegion(backingRect.ExpandedBy(3f), ResolveTooltip(pawn, current, max, pct));
            GUI.color = Color.white;
        }

        private static bool TryResolveBarRect(Pawn pawn, Camera camera, out Rect rect)
        {
            rect = Rect.zero;
            if (pawn == null || camera == null)
            {
                return false;
            }

            float drawSizeY = 1f;
            try
            {
                if (pawn.def?.graphicData != null)
                {
                    drawSizeY = Mathf.Max(1f, pawn.def.graphicData.drawSize.y);
                }
            }
            catch
            {
                drawSizeY = Mathf.Max(1f, pawn.BodySize);
            }

            float yOffsetCells = Mathf.Clamp(drawSizeY * 0.48f + 0.24f, 0.85f, 5.25f);
            Vector3 worldPos = pawn.DrawPos + new Vector3(0f, 0f, yOffsetCells);
            Vector3 screen = camera.WorldToScreenPoint(worldPos);
            if (screen.z < 0f)
            {
                return false;
            }

            float screenHeight = UI.screenHeight > 0 ? UI.screenHeight : Screen.height;
            float screenWidth = UI.screenWidth > 0 ? UI.screenWidth : Screen.width;
            Vector2 guiPoint = new Vector2(screen.x, screenHeight - screen.y);

            float zoomScale = ResolveZoomScale(camera);
            float width = Mathf.Clamp(BaseWidth * zoomScale, 92f, 164f);
            float height = Mathf.Clamp(BaseHeight * zoomScale, 8f, 13f);
            rect = new Rect(guiPoint.x - width * 0.5f, guiPoint.y - height * 0.5f, width, height);

            return rect.xMax >= -32f && rect.x <= screenWidth + 32f && rect.yMax >= -32f && rect.y <= screenHeight + 32f;
        }

        private static float ResolveZoomScale(Camera camera)
        {
            if (camera == null || !camera.orthographic)
            {
                return 1f;
            }

            // Keep the bar readable at distance without letting it become huge when zoomed in.
            return Mathf.Clamp(12.5f / Mathf.Max(8f, camera.orthographicSize), 0.78f, 1.10f);
        }

        private static string ResolveDisplayLabel(Pawn pawn, AbyssalProtocolModSettings settings, float current, float max)
        {
            string name = pawn?.kindDef?.LabelCap ?? pawn?.LabelCap.ToString() ?? "Miniboss";
            if (settings != null && settings.showHealthNumbers)
            {
                return name + "  " + Mathf.CeilToInt(current).ToString() + "/" + Mathf.CeilToInt(max).ToString();
            }

            return name;
        }

        private static string ResolveTooltip(Pawn pawn, float current, float max, float pct)
        {
            string name = pawn?.kindDef?.LabelCap ?? pawn?.LabelCap.ToString() ?? "Miniboss";
            return name + "\n" + Mathf.CeilToInt(current).ToString() + " / " + Mathf.CeilToInt(max).ToString() + " custom abyssal HP (" + Mathf.RoundToInt(pct * 100f).ToString() + "%).";
        }
    }
}
