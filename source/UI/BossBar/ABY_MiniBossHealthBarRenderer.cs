using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_MiniBossHealthBarRenderer
    {
        private const int CacheRefreshIntervalTicks = 12;
        private const float BaseWidth = 164f;
        private const float BaseHeight = 13f;
        private const float LabelHeight = 17f;
        private const float BorderThickness = 1f;

        private static readonly List<Pawn> CachedMiniBosses = new List<Pawn>();
        private static Map cachedMap;
        private static int nextCacheRefreshTick = -1;

        private static readonly Color BackColor = new Color(0.030f, 0.018f, 0.014f, 0.94f);
        private static readonly Color BorderColor = new Color(0.78f, 0.36f, 0.15f, 0.98f);
        private static readonly Color TrailColor = new Color(0.72f, 0.24f, 0.12f, 0.62f);
        private static readonly Color FillColor = new Color(0.96f, 0.20f, 0.10f, 0.98f);
        private static readonly Color CriticalFillColor = new Color(1f, 0.58f, 0.16f, 0.98f);
        private static readonly Color TextColor = new Color(0.96f, 0.88f, 0.78f, 0.98f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.82f);
        private static readonly Color OuterGlowColor = new Color(0.72f, 0.18f, 0.06f, 0.28f);

        public static void DrawForCurrentMap(AbyssalProtocolModSettings settings)
        {
            if (!ShouldDraw(settings))
            {
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                ClearCache();
                return;
            }

            EnsureCache(map);
            Draw(CachedMiniBosses, settings);
        }

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

        private static bool ShouldDraw(AbyssalProtocolModSettings settings)
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return false;
            }

            if (settings == null || !settings.enableBossBars || !settings.enableMiniBossHealthBars)
            {
                return false;
            }

            Event currentEvent = Event.current;
            return currentEvent != null && currentEvent.type == EventType.Repaint;
        }

        private static void EnsureCache(Map map)
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (map == cachedMap && ticksGame < nextCacheRefreshTick)
            {
                return;
            }

            cachedMap = map;
            nextCacheRefreshTick = ticksGame + CacheRefreshIntervalTicks;
            CachedMiniBosses.Clear();

            if (map?.mapPawns == null)
            {
                return;
            }

            IEnumerable<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                if (ShouldTrackMiniBoss(pawn, map))
                {
                    CachedMiniBosses.Add(pawn);
                }
            }
        }

        private static bool ShouldTrackMiniBoss(Pawn pawn, Map map)
        {
            if (pawn == null || map == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.MapHeld != map)
            {
                return false;
            }

            if (!ABY_AbyssalPawnClassificationUtility.IsMiniBoss(pawn))
            {
                return false;
            }

            if (ABY_AbyssalPawnClassificationUtility.IsMajorBoss(pawn))
            {
                return false;
            }

            float current;
            float max;
            float pct;
            if (!ABY_BossTrueDeathUtility.TryGetBossHp(pawn, out current, out max, out pct))
            {
                return false;
            }

            if (max <= 0.001f || current <= 0f)
            {
                return false;
            }

            try
            {
                if (pawn.PositionHeld.Fogged(map))
                {
                    return false;
                }
            }
            catch
            {
            }

            return true;
        }

        private static void ClearCache()
        {
            CachedMiniBosses.Clear();
            cachedMap = null;
            nextCacheRefreshTick = -1;
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

            Rect labelRect = new Rect(barRect.x - 10f, barRect.y - LabelHeight + 1f, barRect.width + 20f, LabelHeight);
            Rect backingRect = new Rect(barRect.x - 4f, barRect.y - 2f, barRect.width + 8f, barRect.height + 4f);
            Rect glowRect = backingRect.ExpandedBy(3f);

            GUI.color = OuterGlowColor;
            GUI.DrawTexture(glowRect, BaseContent.WhiteTex);

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

            TooltipHandler.TipRegion(backingRect.ExpandedBy(5f), ResolveTooltip(pawn, current, max, pct));
            GUI.color = Color.white;
        }

        private static bool TryResolveBarRect(Pawn pawn, Camera camera, out Rect rect)
        {
            rect = Rect.zero;
            if (pawn == null || camera == null)
            {
                return false;
            }

            float drawSizeY = ResolvePawnDrawSizeY(pawn);
            float yOffsetCells = Mathf.Clamp(drawSizeY * 0.46f + 0.34f, 1.05f, 5.40f);

            // Use RimWorld's own map-label projection instead of Camera.WorldToScreenPoint.
            // The raw Unity screen projection is pixel-based and does not follow RimWorld's
            // scaled IMGUI/map-interface coordinate space reliably, especially while the camera
            // pans or when UI scale is not exactly 1.0. LabelDrawPosFor is the same projection
            // family vanilla uses for overhead map labels, so the bar stays attached to the pawn
            // instead of drifting toward a fixed screen/map point.
            Vector2 guiPoint;
            try
            {
                guiPoint = GenMapUI.LabelDrawPosFor(pawn, yOffsetCells);
            }
            catch
            {
                return false;
            }

            float screenHeight = UI.screenHeight > 0 ? UI.screenHeight : Screen.height;
            float screenWidth = UI.screenWidth > 0 ? UI.screenWidth : Screen.width;
            if (float.IsNaN(guiPoint.x) || float.IsNaN(guiPoint.y) || float.IsInfinity(guiPoint.x) || float.IsInfinity(guiPoint.y))
            {
                return false;
            }

            float zoomScale = ResolveZoomScale(camera);
            float width = Mathf.Clamp(BaseWidth * zoomScale, 118f, 190f);
            float height = Mathf.Clamp(BaseHeight * zoomScale, 10f, 15f);
            rect = new Rect(guiPoint.x - width * 0.5f, guiPoint.y - height * 0.5f, width, height);

            return rect.xMax >= -48f && rect.x <= screenWidth + 48f && rect.yMax >= -48f && rect.y <= screenHeight + 48f;
        }

        private static float ResolvePawnDrawSizeY(Pawn pawn)
        {
            float result = 1f;
            try
            {
                result = Mathf.Max(result, pawn != null ? pawn.BodySize : 1f);
            }
            catch
            {
            }

            try
            {
                if (pawn?.def?.graphicData != null)
                {
                    result = Mathf.Max(result, pawn.def.graphicData.drawSize.y);
                }
            }
            catch
            {
            }

            try
            {
                List<PawnKindLifeStage> stages = pawn?.kindDef?.lifeStages;
                if (stages != null)
                {
                    for (int i = 0; i < stages.Count; i++)
                    {
                        GraphicData data = stages[i]?.bodyGraphicData;
                        if (data != null)
                        {
                            result = Mathf.Max(result, data.drawSize.y);
                        }
                    }
                }
            }
            catch
            {
            }

            return Mathf.Clamp(result, 1f, 12f);
        }

        private static float ResolveZoomScale(Camera camera)
        {
            if (camera == null || !camera.orthographic)
            {
                return 1f;
            }

            // Keep the bar readable at combat zoom without letting it become a full boss HUD.
            return Mathf.Clamp(13.0f / Mathf.Max(8f, camera.orthographicSize), 0.84f, 1.16f);
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
