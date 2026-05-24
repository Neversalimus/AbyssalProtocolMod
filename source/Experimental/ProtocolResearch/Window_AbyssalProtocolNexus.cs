using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Window_AbyssalProtocolNexus : Window
    {
        private const string BackgroundPath = "UI/ABY/ProtocolResearch/ABY_NexusWindowBackground";
        private const string SmallRingPath = "UI/ABY/ProtocolResearch/ABY_SmallCategoryRingFrame";
        private const string LargeRingPath = "UI/ABY/ProtocolResearch/ABY_LargeResearchRing";
        private const string SelectedSocketHaloPath = "UI/ABY/ProtocolResearch/ABY_LargeResearchRing_SelectedSocketHalo";
        private const string SegmentLockedPath = "UI/ABY/ProtocolResearch/Segments/ABY_Segment_Locked";
        private const string SegmentAvailablePath = "UI/ABY/ProtocolResearch/Segments/ABY_Segment_Available";
        private const string SegmentActivePath = "UI/ABY/ProtocolResearch/Segments/ABY_Segment_Active";
        private const string SegmentCompletedPath = "UI/ABY/ProtocolResearch/Segments/ABY_Segment_Completed";

        private static readonly Texture2D BackgroundTex = ContentFinder<Texture2D>.Get(BackgroundPath, false);
        private static readonly Texture2D SmallRingTex = ContentFinder<Texture2D>.Get(SmallRingPath, false);
        private static readonly Texture2D LargeRingTex = ContentFinder<Texture2D>.Get(LargeRingPath, false);
        private static readonly Texture2D SelectedSocketHaloTex = ContentFinder<Texture2D>.Get(SelectedSocketHaloPath, false);
        private static readonly Texture2D SegmentLockedTex = ContentFinder<Texture2D>.Get(SegmentLockedPath, false);
        private static readonly Texture2D SegmentAvailableTex = ContentFinder<Texture2D>.Get(SegmentAvailablePath, false);
        private static readonly Texture2D SegmentActiveTex = ContentFinder<Texture2D>.Get(SegmentActivePath, false);
        private static readonly Texture2D SegmentCompletedTex = ContentFinder<Texture2D>.Get(SegmentCompletedPath, false);

        private readonly Building_ABY_ProtocolNexus nexus;
        private ABY_ProtocolResearchCategoryDef selectedCategory;
        private ABY_ProtocolResearchDef selectedProject;
        private string selectedLayerKey;
        private ProtocolProjectFilter selectedFilter = ProtocolProjectFilter.All;
        private Vector2 projectListScroll = Vector2.zero;
        private Vector2 detailsScroll = Vector2.zero;
        private float detailViewHeight = 600f;
        private int headerCacheTick = -999999;
        private int headerCacheTotal;
        private int headerCacheAvailable;
        private int headerCacheCompleted;

        private const int HeaderSummaryCacheRefreshTicks = 90;
        private const int OuterTierSlotCount = 8;
        private const int ApotheosisTierSlot = 8;

        public Window_AbyssalProtocolNexus(Building_ABY_ProtocolNexus nexus)
        {
            this.nexus = nexus;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
            draggable = true;
            forcePause = false;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = false;
            resizeable = false;
        }

        public override Vector2 InitialSize => new Vector2(1228f, 812f);

        public override void DoWindowContents(Rect inRect)
        {
            try
            {
                DoWindowContentsSafe(inRect);
            }
            catch (Exception ex)
            {
                ABY_UISafetyUtility.DrawWindowFallback(inRect, "Abyssal Protocol Nexus", ex);
            }
        }

        private void DoWindowContentsSafe(Rect inRect)
        {
            if (nexus == null || nexus.Destroyed || nexus.Map == null)
            {
                Close();
                return;
            }

            List<ABY_ProtocolResearchCategoryDef> categories = ABY_ProtocolResearchUtility.AllCategories();
            if (selectedCategory != null && !categories.Contains(selectedCategory))
            {
                selectedCategory = null;
                selectedProject = null;
            }

            List<ABY_ProtocolResearchDef> allProjects = AllProtocolProjects();
            List<ResearchLayerView> selectedLayers = BuildLayerViews(allProjects);
            EnsureSelectedLayer(selectedLayers);
            ResearchLayerView activeLayer = SelectedLayer(selectedLayers);
            List<ABY_ProtocolResearchDef> layerProjects = ProjectsForSelectedLayer(selectedLayers, allProjects);
            List<ABY_ProtocolResearchDef> categoryLayerProjects = FilterByCategory(layerProjects, selectedCategory);
            List<ABY_ProtocolResearchDef> displayedProjects = FilterProjects(categoryLayerProjects, selectedFilter);

            if (displayedProjects.Count > 0 && (selectedProject == null || !displayedProjects.Contains(selectedProject)))
            {
                selectedProject = displayedProjects.FirstOrDefault();
            }
            else if (displayedProjects.Count == 0 && (selectedProject == null || !categoryLayerProjects.Contains(selectedProject)))
            {
                selectedProject = categoryLayerProjects.FirstOrDefault() ?? layerProjects.FirstOrDefault() ?? allProjects.FirstOrDefault();
            }

            DrawBackground(inRect);

            Rect headerRect = new Rect(inRect.x + 14f, inRect.y + 12f, inRect.width - 28f, 66f);
            Rect categoryRect = new Rect(inRect.x + 22f, headerRect.yMax + 12f, 760f, 112f);
            Rect ringRect = new Rect(inRect.x + 20f, categoryRect.yMax + 12f, 762f, inRect.height - categoryRect.yMax - 22f);
            Rect rightRect = new Rect(ringRect.xMax + 16f, headerRect.yMax + 12f, inRect.width - ringRect.xMax - 36f, inRect.height - headerRect.yMax - 22f);

            DrawHeader(headerRect, categories);
            DrawCategoryRings(categoryRect, categories, activeLayer, layerProjects);
            DrawCategoryDetailRing(ringRect, allProjects, selectedLayers, activeLayer, categoryLayerProjects, displayedProjects);
            DrawProjectDetails(rightRect, selectedProject);
        }

        private void ResolveHeaderSummary(List<ABY_ProtocolResearchCategoryDef> categories, out int total, out int available, out int completed)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : Environment.TickCount;
            if (now - headerCacheTick < HeaderSummaryCacheRefreshTicks)
            {
                total = headerCacheTotal;
                available = headerCacheAvailable;
                completed = headerCacheCompleted;
                return;
            }

            total = ABY_ProtocolResearchUtility.AllProjects().Count;
            available = 0;
            completed = 0;
            if (categories != null)
            {
                for (int i = 0; i < categories.Count; i++)
                {
                    ABY_ProtocolResearchCategoryDef category = categories[i];
                    available += ABY_ProtocolResearchUtility.CountAvailable(category);
                    completed += ABY_ProtocolResearchUtility.CountVisibleCompleted(category);
                }
            }

            headerCacheTick = now;
            headerCacheTotal = total;
            headerCacheAvailable = available;
            headerCacheCompleted = completed;
        }

        private static void DrawBackground(Rect rect)
        {
            if (BackgroundTex != null)
            {
                GUI.DrawTexture(rect, BackgroundTex, ScaleMode.StretchToFill, true);
            }
            else
            {
                DrawSolid(rect, new Color(0.035f, 0.032f, 0.034f, 1f));
            }

            DrawSolid(rect, new Color(0f, 0f, 0f, 0.18f));
            DrawOutline(rect, new Color(0.85f, 0.34f, 0.14f, 0.55f));
        }

        private void DrawHeader(Rect rect, List<ABY_ProtocolResearchCategoryDef> categories)
        {
            DrawPanel(rect, false);
            ResolveHeaderSummary(categories, out int total, out int available, out int completed);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 16f, rect.y + 8f, rect.width - 380f, 30f), "ABY_ProtocolResearch_Title".Translate());

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.88f, 0.78f, 0.69f, 1f);
            Widgets.Label(new Rect(rect.x + 18f, rect.y + 38f, rect.width - 36f, 22f), "ABY_ProtocolResearch_Subtitle".Translate());

            Rect progressRect = new Rect(rect.xMax - 352f, rect.y + 9f, 322f, 24f);
            DrawSolid(progressRect, new Color(0.07f, 0.030f, 0.020f, 0.74f));
            DrawOutline(progressRect, new Color(1f, 0.42f, 0.16f, 0.36f));

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1f, 0.78f, 0.54f, 1f);
            Widgets.Label(progressRect.ContractedBy(4f, 1f), Shorten("ABY_ProtocolResearch_HeaderProgress".Translate(available, completed, total), 54));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawCategoryRings(Rect rect, List<ABY_ProtocolResearchCategoryDef> categories, ResearchLayerView activeLayer, List<ABY_ProtocolResearchDef> layerProjects)
        {
            DrawPanel(rect, false);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.95f, 0.71f, 0.48f, 1f);
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 6f, rect.width - 20f, 18f), "ABY_ProtocolResearch_CategoryHeader".Translate());
            GUI.color = Color.white;

            if (categories.Count == 0)
            {
                Widgets.Label(rect.ContractedBy(12f), "ABY_ProtocolResearch_NoCategories".Translate());
                return;
            }

            float itemSize = 76f;
            float gap = 7f;
            float startX = rect.x + 10f;
            float y = rect.y + 25f;
            DrawCategoryFilterRing(new Rect(startX, y, itemSize, itemSize), null, selectedCategory == null, activeLayer, layerProjects);

            for (int i = 0; i < categories.Count; i++)
            {
                ABY_ProtocolResearchCategoryDef category = categories[i];
                Rect itemRect = new Rect(startX + (i + 1) * (itemSize + gap), y, itemSize, itemSize);
                bool selected = category == selectedCategory;
                DrawCategoryFilterRing(itemRect, category, selected, activeLayer, layerProjects);
            }
        }

        private void DrawCategoryFilterRing(Rect rect, ABY_ProtocolResearchCategoryDef category, bool selected, ResearchLayerView activeLayer, List<ABY_ProtocolResearchDef> layerProjects)
        {
            List<ABY_ProtocolResearchDef> filtered = FilterByCategory(layerProjects, category);
            int available = CountProjects(filtered, ProtocolProjectFilter.Ready);
            int completed = CountProjects(filtered, ProtocolProjectFilter.Completed);
            int total = filtered.Count;
            bool hover = Mouse.IsOver(rect);
            bool empty = total == 0;

            Color oldColor = GUI.color;
            GUI.color = selected
                ? new Color(1f, 0.86f, 0.68f, 1f)
                : hover
                    ? new Color(1f, 0.72f, 0.50f, empty ? 0.64f : 0.94f)
                    : new Color(1f, 1f, 1f, empty ? 0.46f : 1f);
            if (SmallRingTex != null)
            {
                GUI.DrawTexture(rect, SmallRingTex, ScaleMode.ScaleToFit, true);
            }
            else
            {
                Widgets.DrawBox(rect);
            }

            if (category == null)
            {
                TextAnchor oldAnchor = Text.Anchor;
                GameFont oldFont = Text.Font;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Small;
                GUI.color = selected ? Color.white : new Color(0.88f, 0.80f, 0.70f, empty ? 0.58f : 0.92f);
                Widgets.Label(rect.ContractedBy(18f), "ABY_ProtocolResearch_AllFilterLabel".Translate());
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
            }
            else
            {
                Texture2D icon = category.iconPath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(category.iconPath, false);
                if (icon != null)
                {
                    GUI.color = selected ? Color.white : new Color(0.86f, 0.82f, 0.78f, empty ? 0.45f : 0.92f);
                    GUI.DrawTexture(rect.ContractedBy(18f), icon, ScaleMode.ScaleToFit, true);
                }
            }
            GUI.color = oldColor;

            Rect countRect = new Rect(rect.x + 8f, rect.yMax - 18f, rect.width - 16f, 18f);
            DrawSolid(countRect, new Color(0f, 0f, 0f, empty ? 0.44f : 0.62f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = available > 0 ? new Color(1f, 0.72f, 0.42f, 1f) : empty ? new Color(0.48f, 0.40f, 0.34f, 1f) : new Color(0.72f, 0.68f, 0.62f, 1f);
            Widgets.Label(countRect, completed + "/" + total);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(rect))
            {
                selectedCategory = category;
                List<ABY_ProtocolResearchDef> categoryProjects = FilterByCategory(layerProjects, selectedCategory);
                selectedProject = categoryProjects.FirstOrDefault() ?? layerProjects?.FirstOrDefault();
                selectedFilter = ProtocolProjectFilter.All;
                projectListScroll = Vector2.zero;
                detailsScroll = Vector2.zero;
            }

            if (hover)
            {
                string label = category == null ? "ABY_ProtocolResearch_AllFilterName".Translate() : category.LabelCap;
                string description = category == null ? "ABY_ProtocolResearch_AllFilterTooltip".Translate() : category.description;
                string layerLabel = activeLayer == null ? "ABY_ProtocolResearch_NoLayerFilter".Translate() : activeLayer.Label;
                string tooltip = label + "\n" + description + "\n\n" + "ABY_ProtocolResearch_CategoryFilterTooltip".Translate(available, completed, total, layerLabel);
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private void DrawCategoryDetailRing(Rect rect, List<ABY_ProtocolResearchDef> projects, List<ResearchLayerView> layers, ResearchLayerView activeLayer, List<ABY_ProtocolResearchDef> layerProjects, List<ABY_ProtocolResearchDef> displayedProjects)
        {
            DrawPanel(rect, false);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            string title = activeLayer == null ? "ABY_ProtocolResearch_TierMatrixTitle".Translate() : activeLayer.Label;
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 24f), title);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.76f, 0.72f, 0.68f, 1f);
            string filterLabel = selectedCategory == null ? "ABY_ProtocolResearch_AllFilterName".Translate() : selectedCategory.LabelCap;
            string tierDescription = "ABY_ProtocolResearch_TierMatrixDesc".Translate(filterLabel);
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 32f, rect.width - 28f, 34f), tierDescription);
            GUI.color = Color.white;

            float ringSize = Mathf.Min(430f, Mathf.Max(360f, rect.height - 128f));
            Rect ringArea = new Rect(rect.x + 30f, rect.y + 70f, ringSize, ringSize);
            Rect localRingArea = new Rect(0f, 0f, ringArea.width, ringArea.height);

            GUI.BeginGroup(ringArea);
            if (LargeRingTex != null)
            {
                GUI.DrawTexture(localRingArea, LargeRingTex, ScaleMode.ScaleToFit, true);
            }
            else
            {
                Widgets.DrawBox(localRingArea);
            }

            DrawLayerMatrix(localRingArea, projects, layers, activeLayer, layerProjects);
            GUI.EndGroup();

            Rect listRect = new Rect(ringArea.xMax + 16f, rect.y + 70f, rect.width - ringArea.width - 62f, rect.height - 86f);
            DrawProjectList(listRect, activeLayer, layerProjects, displayedProjects);
        }

        private void DrawLayerMatrix(Rect ringArea, List<ABY_ProtocolResearchDef> projects, List<ResearchLayerView> layers, ResearchLayerView activeLayer, List<ABY_ProtocolResearchDef> layerProjects)
        {
            if (projects == null || projects.Count == 0)
            {
                return;
            }

            DrawRingProgressTicks(ringArea, projects);
            DrawSelectedLayerFocus(ringArea, layers, activeLayer);
            DrawLayerNodes(ringArea, layers);
            if (!IsApotheosisLayer(activeLayer))
            {
                DrawRingCenterDashboard(ringArea, projects, activeLayer, layerProjects);
            }
            else
            {
                DrawApotheosisCenterNode(ringArea, activeLayer);
            }
        }

        private void DrawRingProgressTicks(Rect ringArea, List<ABY_ProtocolResearchDef> projects)
        {
            if (projects == null || projects.Count == 0)
            {
                return;
            }

            const int MaxTicks = 48;
            int tickCount = Mathf.Min(MaxTicks, Mathf.Max(1, projects.Count));
            Vector2 center = ringArea.center;
            float radius = ringArea.width * 0.438f;

            for (int i = 0; i < tickCount; i++)
            {
                int startIndex = Mathf.FloorToInt((float)i * projects.Count / tickCount);
                int endIndex = Mathf.FloorToInt((float)(i + 1) * projects.Count / tickCount);
                endIndex = Mathf.Max(startIndex + 1, endIndex);
                endIndex = Mathf.Min(projects.Count, endIndex);

                ABY_ProtocolResearchState state = AggregateState(projects.GetRange(startIndex, endIndex - startIndex));
                float angle = -90f + (360f * i / tickCount);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 pos = center + new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
                float size = state == ABY_ProtocolResearchState.Completed ? 4.6f : 3.6f;
                Rect tickRect = new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size);

                Color color = StateColor(state);
                color.a = state == ABY_ProtocolResearchState.Locked ? 0.34f : 0.58f;
                DrawSolid(tickRect, color);
            }
        }

        private void DrawLayerNodes(Rect ringArea, List<ResearchLayerView> layers)
        {
            Vector2 center = ringArea.center;
            float radius = ringArea.width * 0.382f;

            for (int slot = 0; slot < OuterTierSlotCount; slot++)
            {
                ResearchLayerView layer = LayerForTierSlot(layers, slot);
                Vector2 pos = TierSlotPosition(center, radius, slot);

                bool selected = layer != null && layer.Key == selectedLayerKey;
                Rect hitRect = new Rect(pos.x - 27f, pos.y - 27f, 54f, 54f);
                bool hover = Mouse.IsOver(hitRect);
                float nodeSize = layer == null ? (hover ? 30f : 26f) : selected ? 36f : hover ? 32f : 28f;
                Rect nodeRect = new Rect(pos.x - nodeSize * 0.5f, pos.y - nodeSize * 0.5f, nodeSize, nodeSize);

                DrawLayerNode(nodeRect, layer, slot, selected, hover);

                if (layer != null)
                {
                    if (Widgets.ButtonInvisible(hitRect))
                    {
                        selectedLayerKey = layer.Key;
                        List<ABY_ProtocolResearchDef> categoryProjects = FilterByCategory(layer.Projects, selectedCategory);
                        selectedProject = categoryProjects.FirstOrDefault() ?? layer.Projects.FirstOrDefault();
                        selectedFilter = ProtocolProjectFilter.All;
                        projectListScroll = Vector2.zero;
                        detailsScroll = Vector2.zero;
                    }

                    if (hover)
                    {
                        TooltipHandler.TipRegion(hitRect, layer.Label + "\n" + LayerProgressText(layer) + "\n\n" + "ABY_ProtocolResearch_TierNodeTooltip".Translate());
                    }
                }
                else if (hover)
                {
                    TooltipHandler.TipRegion(hitRect, "ABY_ProtocolResearch_SealedTierTooltip".Translate(TierGlyphForSlot(slot)));
                }
            }
        }

        private void DrawSelectedLayerFocus(Rect ringArea, List<ResearchLayerView> layers, ResearchLayerView activeLayer)
        {
            if (layers == null || layers.Count == 0 || activeLayer == null || IsApotheosisLayer(activeLayer))
            {
                return;
            }

            int slot = TierSlotIndexFor(activeLayer.Label);
            if (slot < 0 || slot >= OuterTierSlotCount)
            {
                return;
            }

            Vector2 center = ringArea.center;
            float angle = TierSlotAngle(slot);
            float rad = angle * Mathf.Deg2Rad;
            float nodeRadius = ringArea.width * 0.382f;
            Vector2 nodePos = center + new Vector2(Mathf.Cos(rad) * nodeRadius, Mathf.Sin(rad) * nodeRadius);
            float pulse = 0.5f + Mathf.Sin(Time.realtimeSinceStartup * 3.2f) * 0.5f;

            // Premium focus feedback is deliberately local-space only: no GUI.matrix rotation,
            // no long sweep lines, and no elements that can escape the ring group.
            float focusRadiusOuter = ringArea.width * 0.425f;
            float focusRadiusInner = ringArea.width * 0.397f;
            const int markers = 17;
            float arcHalfSpan = 24f;
            for (int i = 0; i < markers; i++)
            {
                float t = markers == 1 ? 0.5f : i / (float)(markers - 1);
                float centerWeight = Mathf.Clamp01(1f - Mathf.Abs(t - 0.5f) * 1.8f);
                float markerAngle = angle - arcHalfSpan + (arcHalfSpan * 2f * t);
                float markerRad = markerAngle * Mathf.Deg2Rad;

                Vector2 outerPos = center + new Vector2(Mathf.Cos(markerRad) * focusRadiusOuter, Mathf.Sin(markerRad) * focusRadiusOuter);
                float outerSize = Mathf.Lerp(2.4f, 6.2f, centerWeight);
                Rect outerRect = new Rect(outerPos.x - outerSize * 0.5f, outerPos.y - outerSize * 0.5f, outerSize, outerSize);
                DrawSolid(outerRect, new Color(1f, 0.52f, 0.20f, (0.08f + pulse * 0.09f) * Mathf.Lerp(0.45f, 1f, centerWeight)));

                if (i % 2 == 0)
                {
                    Vector2 innerPos = center + new Vector2(Mathf.Cos(markerRad) * focusRadiusInner, Mathf.Sin(markerRad) * focusRadiusInner);
                    float innerSize = Mathf.Lerp(1.7f, 3.2f, centerWeight);
                    Rect innerRect = new Rect(innerPos.x - innerSize * 0.5f, innerPos.y - innerSize * 0.5f, innerSize, innerSize);
                    DrawSolid(innerRect, new Color(1f, 0.78f, 0.42f, (0.05f + pulse * 0.07f) * Mathf.Lerp(0.40f, 0.90f, centerWeight)));
                }
            }

            Rect nodeFocusRect = new Rect(nodePos.x - 44f, nodePos.y - 44f, 88f, 88f);
            DrawCornerBrackets(nodeFocusRect, new Color(1f, 0.50f, 0.18f, 0.18f + pulse * 0.18f), 14f, 2f);
            DrawCornerBrackets(nodeFocusRect.ContractedBy(6f), new Color(1f, 0.76f, 0.38f, 0.10f + pulse * 0.14f), 10f, 1f);
        }

        private void DrawApotheosisCenterNode(Rect ringArea, ResearchLayerView activeLayer)
        {
            if (activeLayer == null)
            {
                return;
            }

            Vector2 center = ringArea.center;
            float pulse = 0.5f + Mathf.Sin(Time.realtimeSinceStartup * 3.0f) * 0.5f;
            Rect rect = new Rect(center.x - 44f, center.y - 44f, 88f, 88f);

            DrawSolid(rect, new Color(0.012f, 0.010f, 0.009f, 0.76f));
            DrawCornerBrackets(rect, new Color(1f, 0.48f, 0.18f, 0.30f + pulse * 0.22f), 18f, 2f);
            DrawCornerBrackets(rect.ContractedBy(8f), new Color(1f, 0.76f, 0.38f, 0.14f + pulse * 0.16f), 12f, 1f);

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 20f, rect.width - 8f, 22f), "IX");
            Text.Font = GameFont.Tiny;
            GUI.color = StateColor(activeLayer.State);
            Widgets.Label(new Rect(rect.x + 4f, rect.y + 44f, rect.width - 8f, 18f), "APOTHEOSIS");

            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
        }

        private static void DrawLayerNode(Rect rect, ResearchLayerView layer, int slot, bool selected, bool hover)
        {
            bool plannedOnly = layer == null;
            ABY_ProtocolResearchState state = plannedOnly ? ABY_ProtocolResearchState.Locked : layer.State;
            Color stateColor = plannedOnly ? new Color(0.28f, 0.15f, 0.11f, 1f) : StateColor(state);
            float pulse = 0.5f + Mathf.Sin(Time.realtimeSinceStartup * 4f) * 0.5f;
            Rect backingRect = rect.ExpandedBy(selected ? 2f : plannedOnly ? 4f : 5f);

            if (selected && SelectedSocketHaloTex != null)
            {
                float haloSize = Mathf.Max(rect.width, rect.height) + 48f;
                Rect haloRect = new Rect(rect.center.x - haloSize * 0.5f, rect.center.y - haloSize * 0.5f, haloSize, haloSize);
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 0.94f, 0.86f, 0.82f + pulse * 0.14f);
                GUI.DrawTexture(haloRect, SelectedSocketHaloTex, ScaleMode.ScaleToFit, true);
                GUI.color = oldColor;
            }

            DrawSolid(backingRect, new Color(0f, 0f, 0f, selected ? 0.56f : plannedOnly ? 0.48f : 0.62f));
            if (selected)
            {
                DrawSolid(backingRect.ContractedBy(2f), new Color(0.18f, 0.055f, 0.022f, 0.14f + pulse * 0.08f));
            }

            Color fill = stateColor;
            fill.a = selected ? 0.76f : hover ? 0.68f : plannedOnly ? 0.34f : 0.58f;
            DrawSolid(rect, fill);

            Color outline = selected
                ? new Color(1f, 0.76f, 0.38f, 0.78f)
                : hover
                    ? new Color(1f, 0.48f, 0.20f, plannedOnly ? 0.42f : 0.78f)
                    : new Color(0.72f, 0.28f, 0.14f, plannedOnly ? 0.26f : 0.45f);
            DrawOutline(backingRect, outline);

            if (selected && SelectedSocketHaloTex == null)
            {
                DrawOutline(backingRect.ExpandedBy(5f), new Color(1f, 0.42f, 0.16f, 0.30f + pulse * 0.22f));
                DrawOutline(backingRect.ExpandedBy(10f), new Color(1f, 0.32f, 0.10f, 0.12f + pulse * 0.12f));
            }
            else if (selected)
            {
                Color contactColor = new Color(1f, 0.64f, 0.28f, 0.34f + pulse * 0.24f);
                DrawSolid(new Rect(rect.center.x - 3f, rect.y - 7f, 6f, 4f), contactColor);
                DrawSolid(new Rect(rect.center.x - 3f, rect.yMax + 3f, 6f, 4f), contactColor);
                DrawSolid(new Rect(rect.x - 7f, rect.center.y - 3f, 4f, 6f), contactColor);
                DrawSolid(new Rect(rect.xMax + 3f, rect.center.y - 3f, 4f, 6f), contactColor);
                DrawCornerBrackets(backingRect.ExpandedBy(5f), new Color(1f, 0.54f, 0.20f, 0.24f + pulse * 0.18f), 8f, 1f);
            }

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor2 = GUI.color;

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = plannedOnly ? new Color(0.64f, 0.46f, 0.36f, 0.76f) : selected ? Color.white : new Color(1f, 0.86f, 0.68f, 0.92f);
            Widgets.Label(rect, layer == null ? TierGlyphForSlot(slot) : LayerGlyph(layer, slot));

            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor2;
        }

        private void DrawFilterStrip(Rect rect, List<ABY_ProtocolResearchDef> layerProjects)
        {
            List<FilterButtonView> filters = BuildFilterButtons(layerProjects);
            if (filters.Count == 0)
            {
                return;
            }

            float gap = 4f;
            float buttonWidth = Mathf.Max(42f, (rect.width - gap * (filters.Count - 1)) / filters.Count);
            float x = rect.x;

            for (int i = 0; i < filters.Count; i++)
            {
                FilterButtonView filter = filters[i];
                Rect buttonRect = new Rect(x, rect.y, buttonWidth, rect.height);
                x += buttonWidth + gap;

                bool selected = selectedFilter == filter.Filter;
                bool hover = Mouse.IsOver(buttonRect);
                bool enabled = filter.Count > 0 || filter.Filter == ProtocolProjectFilter.All;

                Color backColor = selected
                    ? new Color(0.30f, 0.105f, 0.045f, 0.88f)
                    : hover
                        ? new Color(0.16f, 0.060f, 0.035f, 0.82f)
                        : new Color(0.035f, 0.030f, 0.028f, 0.76f);
                DrawSolid(buttonRect, backColor);
                DrawOutline(buttonRect, selected ? new Color(1f, 0.55f, 0.22f, 0.88f) : hover ? new Color(1f, 0.36f, 0.15f, 0.62f) : new Color(0.52f, 0.20f, 0.11f, 0.38f));

                TextAnchor oldAnchor = Text.Anchor;
                GameFont oldFont = Text.Font;
                Color oldColor = GUI.color;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                GUI.color = enabled ? (selected ? Color.white : new Color(0.90f, 0.76f, 0.60f, 1f)) : new Color(0.46f, 0.40f, 0.34f, 1f);
                Widgets.Label(buttonRect, filter.ShortLabel + " " + filter.Count);
                Text.Anchor = oldAnchor;
                Text.Font = oldFont;
                GUI.color = oldColor;

                if (Widgets.ButtonInvisible(buttonRect))
                {
                    selectedFilter = filter.Filter;
                    List<ABY_ProtocolResearchDef> filtered = FilterProjects(layerProjects, selectedFilter);
                    selectedProject = filtered.FirstOrDefault() ?? layerProjects?.FirstOrDefault();
                    projectListScroll = Vector2.zero;
                    detailsScroll = Vector2.zero;
                }

                if (hover)
                {
                    TooltipHandler.TipRegion(buttonRect, filter.Tooltip);
                }
            }
        }

        private void DrawRingCenterDashboard(Rect ringArea, List<ABY_ProtocolResearchDef> projects, ResearchLayerView selectedLayer, List<ABY_ProtocolResearchDef> layerProjects)
        {
            int total = projects?.Count ?? 0;
            int completed = CountProjects(projects, ProtocolProjectFilter.Completed);
            int ready = CountProjects(projects, ProtocolProjectFilter.Ready);
            int selectedCompleted = selectedLayer?.CompletedCount ?? 0;
            int selectedReady = selectedLayer?.AvailableCount ?? 0;
            int selectedLocked = selectedLayer?.LockedCount ?? 0;
            int selectedTotal = selectedLayer?.Projects?.Count ?? 0;

            Rect summaryRect = new Rect(ringArea.center.x - 100f, ringArea.center.y - 39f, 200f, 78f);
            DrawSolid(summaryRect, new Color(0.010f, 0.008f, 0.007f, 0.66f));
            DrawCornerBrackets(summaryRect, new Color(1f, 0.40f, 0.14f, 0.42f), 18f, 1f);

            Rect topLine = new Rect(summaryRect.x + 18f, summaryRect.y + 8f, summaryRect.width - 36f, 2f);
            Rect bottomLine = new Rect(summaryRect.x + 24f, summaryRect.yMax - 10f, summaryRect.width - 48f, 1f);
            DrawSolid(topLine, selectedLayer == null ? new Color(0.52f, 0.22f, 0.12f, 0.44f) : StateColor(selectedLayer.State));
            DrawSolid(bottomLine, new Color(1f, 0.52f, 0.20f, 0.22f));

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(summaryRect.x + 8f, summaryRect.y + 14f, summaryRect.width - 16f, 22f), Shorten(selectedLayer?.Label ?? "No tier selected", 28));

            Text.Font = GameFont.Tiny;
            GUI.color = selectedLayer == null ? new Color(0.72f, 0.68f, 0.62f, 1f) : StateColor(selectedLayer.State);
            string centerFilterLabel = selectedCategory == null ? "ABY_ProtocolResearch_AllFilterName".Translate() : selectedCategory.LabelCap;
            Widgets.Label(new Rect(summaryRect.x + 8f, summaryRect.y + 37f, summaryRect.width - 16f, 16f), Shorten(centerFilterLabel, 24));

            GUI.color = new Color(0.92f, 0.80f, 0.68f, 1f);
            string selectedCounts = selectedLayer == null
                ? completed + "/" + total + " decoded  •  R " + ready
                : selectedCompleted + "/" + selectedTotal + " decoded  •  R " + selectedReady + "  •  L " + selectedLocked;
            Widgets.Label(new Rect(summaryRect.x + 8f, summaryRect.y + 55f, summaryRect.width - 16f, 16f), Shorten(selectedCounts, 34));

            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
        }

        private void DrawProjectList(Rect rect, ResearchLayerView selectedLayer, List<ABY_ProtocolResearchDef> layerProjects, List<ABY_ProtocolResearchDef> projects)
        {
            DrawPanel(rect, true);
            Rect titleRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 20f);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1f, 0.72f, 0.46f, 1f);
            Widgets.Label(titleRect, "ABY_ProtocolResearch_ProjectListHeader".Translate());
            GUI.color = Color.white;

            Rect layerRect = new Rect(rect.x + 10f, titleRect.yMax + 3f, rect.width - 20f, 22f);
            DrawSolid(layerRect, new Color(0.12f, 0.038f, 0.020f, 0.82f));
            DrawOutline(layerRect, selectedLayer == null ? new Color(0.52f, 0.20f, 0.12f, 0.34f) : new Color(1f, 0.52f, 0.22f, 0.58f));
            DrawSolid(new Rect(layerRect.x + 1f, layerRect.y + 2f, 3f, layerRect.height - 4f), selectedLayer == null ? new Color(0.52f, 0.20f, 0.12f, 0.38f) : StateColor(selectedLayer.State));
            Text.Font = GameFont.Tiny;
            GUI.color = selectedLayer == null ? new Color(0.72f, 0.68f, 0.62f, 1f) : new Color(1f, 0.78f, 0.52f, 1f);
            string categoryLabel = selectedCategory == null ? "ABY_ProtocolResearch_AllFilterName".Translate() : selectedCategory.LabelCap;
            string tierFocus = selectedLayer == null ? "ABY_ProtocolResearch_TierFocusNone".Translate() : "ABY_ProtocolResearch_TierFocusLabel".Translate(selectedLayer.Label, categoryLabel);
            Widgets.Label(new Rect(layerRect.x + 9f, layerRect.y + 3f, layerRect.width - 18f, 16f), Shorten(tierFocus, 48));
            GUI.color = Color.white;

            Rect filterRect = new Rect(rect.x + 10f, layerRect.yMax + 5f, rect.width - 20f, 24f);
            DrawFilterStrip(filterRect, layerProjects);

            Rect outRect = new Rect(rect.x + 8f, filterRect.yMax + 6f, rect.width - 16f, rect.yMax - filterRect.yMax - 14f);
            float rowHeight = 48f;
            int projectCount = projects?.Count ?? 0;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, projectCount * rowHeight + 8f));
            AbyssalStyledWidgets.BeginAbyssalScrollView(outRect, ref projectListScroll, viewRect);
            try
            {
            if (projectCount == 0)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.72f, 0.68f, 0.62f, 1f);
                string emptyMessage = selectedFilter == ProtocolProjectFilter.All
                    ? "ABY_ProtocolResearch_NoProjectsForCategory".Translate()
                    : "ABY_ProtocolResearch_NoProjectsForFilter".Translate();
                Widgets.Label(new Rect(4f, 6f, viewRect.width - 8f, 54f), emptyMessage);
                GUI.color = Color.white;
            }
            else
            {
                for (int i = 0; i < projects.Count; i++)
                {
                    ABY_ProtocolResearchDef project = projects[i];
                    Rect row = new Rect(0f, 4f + i * rowHeight, viewRect.width, rowHeight - 6f);
                    DrawProjectListRow(row, project);
                    if (Widgets.ButtonInvisible(row))
                    {
                        selectedProject = project;
                        selectedLayerKey = LayerKeyFor(project);
                        detailsScroll = Vector2.zero;
                    }
                }
            }
            }
            finally
            {
            AbyssalStyledWidgets.EndAbyssalScrollView(outRect, ref projectListScroll, viewRect);
            }
        }

        private void DrawProjectListRow(Rect rect, ABY_ProtocolResearchDef project)
        {
            bool selected = project == selectedProject;
            ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(project);
            DrawSolid(rect, selected ? new Color(0.24f, 0.095f, 0.050f, 0.90f) : new Color(0.05f, 0.045f, 0.045f, 0.76f));
            DrawOutline(rect, selected ? new Color(1f, 0.50f, 0.20f, 0.72f) : new Color(0.52f, 0.20f, 0.12f, 0.35f));
            if (selected)
            {
                DrawSolid(new Rect(rect.x + 1f, rect.y + 3f, 3f, rect.height - 6f), StateColor(state));
                DrawSolid(new Rect(rect.x + 5f, rect.y + 2f, rect.width - 10f, 1f), new Color(1f, 0.58f, 0.24f, 0.22f));
            }

            Rect stateRect = new Rect(rect.x + 6f, rect.y + 7f, 64f, 28f);
            DrawSegment(stateRect, project, false);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 76f, rect.y + 5f, rect.width - 84f, 18f), project.LabelCap);
            GUI.color = StateColor(state);
            Widgets.Label(new Rect(rect.x + 76f, rect.y + 24f, rect.width - 84f, 16f), ABY_ProtocolResearchUtility.GetStateLabel(state));
            GUI.color = Color.white;
        }

        private void DrawSegment(Rect rect, ABY_ProtocolResearchDef project, bool selected, float rotationDegrees = 0f)
        {
            ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(project);
            Texture2D tex = SegmentTexture(state);
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;

            try
            {
                if (Mathf.Abs(rotationDegrees) > 0.01f)
                {
                    GUIUtility.RotateAroundPivot(rotationDegrees, rect.center);
                }

                GUI.color = selected ? new Color(1f, 0.88f, 0.68f, 1f) : Color.white;
                if (tex != null)
                {
                    GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, true);
                }
                else
                {
                    DrawSolid(rect, StateColor(state) * 0.55f);
                    Widgets.DrawBox(rect);
                }

                if (selected)
                {
                    DrawOutline(rect.ExpandedBy(2f), new Color(1f, 0.72f, 0.36f, 0.95f));
                }
            }
            finally
            {
                GUI.matrix = oldMatrix;
                GUI.color = oldColor;
            }

            if (Mouse.IsOver(rect.ExpandedBy(6f)))
            {
                TooltipHandler.TipRegion(rect.ExpandedBy(6f), project.LabelCap + "\n" + project.description + "\n\n" + ABY_ProtocolResearchUtility.GetStateLabel(state));
            }
        }

        private void DrawProjectDetails(Rect rect, ABY_ProtocolResearchDef project)
        {
            DrawPanel(rect, true);
            if (project == null)
            {
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                Widgets.Label(rect.ContractedBy(14f), "ABY_ProtocolResearch_NoProjectSelected".Translate());
                return;
            }

            ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(project);
            Rect headerRect = new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 74f);
            DrawSolid(headerRect, new Color(0.02f, 0.018f, 0.018f, 0.72f));
            DrawOutline(headerRect, StateColor(state));

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(headerRect.x + 10f, headerRect.y + 7f, headerRect.width - 20f, 24f), project.LabelCap);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.78f, 0.72f, 0.67f, 1f);
            Widgets.Label(new Rect(headerRect.x + 10f, headerRect.y + 32f, headerRect.width - 20f, 18f), project.tierLabel ?? string.Empty);
            GUI.color = StateColor(state);
            Widgets.Label(new Rect(headerRect.x + 10f, headerRect.y + 50f, headerRect.width - 20f, 18f), ABY_ProtocolResearchUtility.GetStateLabel(state));
            GUI.color = Color.white;

            Rect outRect = new Rect(rect.x + 12f, headerRect.yMax + 10f, rect.width - 24f, rect.height - headerRect.height - 34f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, detailViewHeight);
            AbyssalStyledWidgets.BeginAbyssalScrollView(outRect, ref detailsScroll, viewRect);
            try
            {
            float y = 0f;

            y = DrawDecodeControls(viewRect, y, project);
            y = DrawListSection(viewRect, y, "ABY_ProtocolResearch_RevealsHeader".Translate(), BuildRevealLines(project));
            y = DrawParagraph(viewRect, y, "ABY_ProtocolResearch_DescriptionHeader".Translate(), project.description);
            if (!project.loreRecord.NullOrEmpty())
            {
                y = DrawParagraph(viewRect, y, "ABY_ProtocolResearch_LoreHeader".Translate(), project.loreRecord);
            }
            y = DrawListSection(viewRect, y, "ABY_ProtocolResearch_RequirementsHeader".Translate(), BuildRequirementLines(project));
            y = DrawListSection(viewRect, y, "Protocol Status", BuildDiagnosticLines(project));
            y = DrawListSection(viewRect, y, "ABY_ProtocolResearch_UnlocksHeader".Translate(), project.unlocks);
            y = DrawListSection(viewRect, y, "ABY_ProtocolResearch_NotesHeader".Translate(), project.notes);

            y += 12f;
            Rect warningRect = new Rect(0f, y, viewRect.width, 72f);
            DrawSolid(warningRect, new Color(0.28f, 0.08f, 0.04f, 0.72f));
            DrawOutline(warningRect, new Color(1f, 0.42f, 0.16f, 0.55f));
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1f, 0.76f, 0.58f, 1f);
            Widgets.Label(warningRect.ContractedBy(8f), "ABY_ProtocolResearch_IsolatedWarning".Translate());
            GUI.color = Color.white;
            y = warningRect.yMax + 12f;

            detailViewHeight = Mathf.Max(outRect.height + 1f, y);
            }
            finally
            {
            AbyssalStyledWidgets.EndAbyssalScrollView(outRect, ref detailsScroll, viewRect);
            }
        }


        private float DrawDecodeControls(Rect viewRect, float y, ABY_ProtocolResearchDef project)
        {
            Rect rect = new Rect(0f, y, viewRect.width, 74f);
            DrawSolid(rect, new Color(0.035f, 0.026f, 0.020f, 0.78f));
            DrawOutline(rect, new Color(1f, 0.42f, 0.16f, 0.46f));

            bool decoded = ABY_ProtocolResearchUtility.IsDecoded(project) || project.autoDecodeWhenPrerequisitesMet;
            bool activeHere = nexus != null && nexus.ActiveDecodeProjectDefName == project.defName;
            bool canStart = ABY_ProtocolResearchUtility.CanStartDecode(project, out string reason);
            bool anotherActive = nexus != null && nexus.HasActiveDecode && !activeHere;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = new Color(1f, 0.70f, 0.44f, 1f);
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 150f, 18f), "ABY_ProtocolResearch_DecodeHeader".Translate());

            GUI.color = decoded ? new Color(0.72f, 1f, 0.74f, 1f) : activeHere ? new Color(1f, 0.78f, 0.44f, 1f) : new Color(0.84f, 0.78f, 0.72f, 1f);
            string status = decoded
                ? "ABY_ProtocolResearch_DecodeDecoded".Translate()
                : activeHere
                    ? "ABY_ProtocolResearch_DecodeActive".Translate((nexus?.ActiveDecodeProgress ?? 0f).ToStringPercent())
                    : canStart && !anotherActive
                        ? "ABY_ProtocolResearch_DecodeReady".Translate(ABY_ProtocolResearchUtility.ResolveDecodeWorkTicks(project).ToStringTicksToPeriod())
                        : (anotherActive ? "ABY_ProtocolResearch_DecodeAnotherActive".Translate(ABY_ProtocolResearchGateUtility.GetProtocolProjectLabel(nexus.ActiveDecodeProjectDefName)) : reason);
            Widgets.Label(new Rect(rect.x + 10f, rect.y + 29f, rect.width - 150f, 36f), status);
            GUI.color = Color.white;

            Rect buttonRect = new Rect(rect.xMax - 132f, rect.y + 22f, 120f, 30f);
            if (!decoded && !activeHere && canStart && !anotherActive && nexus != null && nexus.IsPowerActive)
            {
                if (AbyssalStyledWidgets.TextButton(buttonRect, "ABY_ProtocolResearch_DecodeStart".Translate()))
                {
                    if (nexus.BeginDecode(project))
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    }
                }
            }
            else if (activeHere)
            {
                if (AbyssalStyledWidgets.TextButton(buttonRect, "ABY_ProtocolResearch_DecodeCancel".Translate()))
                {
                    nexus.CancelActiveDecode();
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                }
            }
            else
            {
                AbyssalStyledWidgets.TextButton(buttonRect, decoded ? "ABY_ProtocolResearch_DecodeDone".Translate() : "ABY_ProtocolResearch_DecodeBlocked".Translate(), false);
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            return rect.yMax + 10f;
        }

        private enum ProtocolProjectFilter
        {
            All,
            Ready,
            Locked,
            Completed,
            Gated
        }

        private sealed class FilterButtonView
        {
            public ProtocolProjectFilter Filter;
            public string ShortLabel;
            public int Count;
            public string Tooltip;
        }

        private sealed class ResearchLayerView
        {
            public string Key;
            public string Label;
            public readonly List<ABY_ProtocolResearchDef> Projects = new List<ABY_ProtocolResearchDef>();
            public int DisplayOrder;
            public int CompletedCount;
            public int ActiveCount;
            public int AvailableCount;
            public int LockedCount;
            public ABY_ProtocolResearchState State;
        }


        private static List<ABY_ProtocolResearchDef> AllProtocolProjects()
        {
            return ABY_ProtocolResearchUtility.AllProjects();
        }

        private static List<ABY_ProtocolResearchDef> FilterByCategory(List<ABY_ProtocolResearchDef> projects, ABY_ProtocolResearchCategoryDef category)
        {
            if (projects == null || projects.Count == 0)
            {
                return new List<ABY_ProtocolResearchDef>();
            }

            if (category == null)
            {
                return projects;
            }

            return projects.Where(project => project != null && project.category == category).ToList();
        }

        private static ResearchLayerView LayerForTierSlot(List<ResearchLayerView> layers, int slot)
        {
            if (layers == null)
            {
                return null;
            }

            return layers.FirstOrDefault(layer => TierSlotIndexFor(layer.Label) == slot);
        }

        private static bool IsApotheosisLayer(ResearchLayerView layer)
        {
            return layer != null && TierSlotIndexFor(layer.Label) == ApotheosisTierSlot;
        }

        private static Vector2 TierSlotPosition(Vector2 center, float radius, int slot)
        {
            float angle = TierSlotAngle(slot);
            float rad = angle * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
        }

        private static float TierSlotAngle(int slot)
        {
            return -90f + (360f * slot / OuterTierSlotCount);
        }

        private static int TierSlotIndexFor(string label)
        {
            if (label.NullOrEmpty())
            {
                return -1;
            }

            string normalized = label.Trim().ToLowerInvariant();
            if (normalized.Contains("apotheosis"))
            {
                return ApotheosisTierSlot;
            }

            string token = ExtractTierToken(normalized);
            switch (token)
            {
                case "1":
                case "i":
                    return 0;
                case "2":
                case "ii":
                    return 1;
                case "3":
                case "iii":
                    return 2;
                case "4":
                case "iv":
                    return 3;
                case "5":
                case "v":
                    return 4;
                case "6":
                case "vi":
                    return 5;
                case "7":
                case "vii":
                    return 6;
                case "8":
                case "viii":
                    return 7;
                case "9":
                case "ix":
                    return ApotheosisTierSlot;
                default:
                    return -1;
            }
        }

        private static string ExtractTierToken(string normalizedLabel)
        {
            if (normalizedLabel.NullOrEmpty())
            {
                return string.Empty;
            }

            int tierIndex = normalizedLabel.IndexOf("tier ", StringComparison.Ordinal);
            if (tierIndex < 0)
            {
                return string.Empty;
            }

            int tokenStart = tierIndex + 5;
            while (tokenStart < normalizedLabel.Length && char.IsWhiteSpace(normalizedLabel[tokenStart]))
            {
                tokenStart++;
            }

            int tokenEnd = tokenStart;
            while (tokenEnd < normalizedLabel.Length)
            {
                char c = normalizedLabel[tokenEnd];
                if (!char.IsLetterOrDigit(c))
                {
                    break;
                }

                tokenEnd++;
            }

            if (tokenEnd <= tokenStart)
            {
                return string.Empty;
            }

            return normalizedLabel.Substring(tokenStart, tokenEnd - tokenStart);
        }

        private static string TierGlyphForSlot(int slot)
        {
            switch (slot)
            {
                case 0: return "I";
                case 1: return "II";
                case 2: return "III";
                case 3: return "IV";
                case 4: return "V";
                case 5: return "VI";
                case 6: return "VII";
                case 7: return "VIII";
                default: return "IX";
            }
        }

        private static List<ResearchLayerView> BuildLayerViews(List<ABY_ProtocolResearchDef> projects)
        {
            List<ResearchLayerView> layers = new List<ResearchLayerView>();
            if (projects == null || projects.Count == 0)
            {
                return layers;
            }

            Dictionary<string, ResearchLayerView> byKey = new Dictionary<string, ResearchLayerView>();
            for (int i = 0; i < projects.Count; i++)
            {
                ABY_ProtocolResearchDef project = projects[i];
                string key = LayerKeyFor(project);
                if (!byKey.TryGetValue(key, out ResearchLayerView layer))
                {
                    layer = new ResearchLayerView
                    {
                        Key = key,
                        Label = LayerLabelFor(project),
                        DisplayOrder = project?.displayOrder ?? i
                    };
                    byKey.Add(key, layer);
                    layers.Add(layer);
                }

                layer.Projects.Add(project);
                if (project != null && project.displayOrder < layer.DisplayOrder)
                {
                    layer.DisplayOrder = project.displayOrder;
                }
            }

            for (int i = 0; i < layers.Count; i++)
            {
                CalculateLayerState(layers[i]);
            }

            return layers
                .OrderBy(layer => layer.DisplayOrder)
                .ThenBy(layer => layer.Label)
                .ToList();
        }

        private void EnsureSelectedLayer(List<ResearchLayerView> layers)
        {
            if (layers == null || layers.Count == 0)
            {
                selectedLayerKey = null;
                return;
            }

            if (!selectedLayerKey.NullOrEmpty() && layers.Any(layer => layer.Key == selectedLayerKey))
            {
                return;
            }

            if (selectedProject != null)
            {
                string projectLayerKey = LayerKeyFor(selectedProject);
                if (layers.Any(layer => layer.Key == projectLayerKey))
                {
                    selectedLayerKey = projectLayerKey;
                    return;
                }
            }

            selectedLayerKey = layers[0].Key;
        }

        private ResearchLayerView SelectedLayer(List<ResearchLayerView> layers)
        {
            if (layers == null || layers.Count == 0)
            {
                return null;
            }

            return layers.FirstOrDefault(layer => layer.Key == selectedLayerKey) ?? layers[0];
        }

        private List<ABY_ProtocolResearchDef> ProjectsForSelectedLayer(List<ResearchLayerView> layers, List<ABY_ProtocolResearchDef> fallbackProjects)
        {
            ResearchLayerView layer = SelectedLayer(layers);
            if (layer != null)
            {
                return layer.Projects;
            }

            return fallbackProjects ?? new List<ABY_ProtocolResearchDef>();
        }
        private static List<ABY_ProtocolResearchDef> FilterProjects(List<ABY_ProtocolResearchDef> projects, ProtocolProjectFilter filter)
        {
            if (projects == null || projects.Count == 0)
            {
                return new List<ABY_ProtocolResearchDef>();
            }

            if (filter == ProtocolProjectFilter.All)
            {
                return projects;
            }

            List<ABY_ProtocolResearchDef> result = new List<ABY_ProtocolResearchDef>();
            for (int i = 0; i < projects.Count; i++)
            {
                ABY_ProtocolResearchDef project = projects[i];
                if (MatchesFilter(project, filter))
                {
                    result.Add(project);
                }
            }

            return result;
        }

        private static bool MatchesFilter(ABY_ProtocolResearchDef project, ProtocolProjectFilter filter)
        {
            ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(project);
            switch (filter)
            {
                case ProtocolProjectFilter.Ready:
                    return state == ABY_ProtocolResearchState.Available || state == ABY_ProtocolResearchState.Active;
                case ProtocolProjectFilter.Locked:
                    return state == ABY_ProtocolResearchState.Locked;
                case ProtocolProjectFilter.Completed:
                    return state == ABY_ProtocolResearchState.Completed;
                case ProtocolProjectFilter.Gated:
                    return state == ABY_ProtocolResearchState.Locked && IsExplicitlyGated(project);
                default:
                    return true;
            }
        }

        private static int CountProjects(List<ABY_ProtocolResearchDef> projects, ProtocolProjectFilter filter)
        {
            if (projects == null || projects.Count == 0)
            {
                return 0;
            }

            if (filter == ProtocolProjectFilter.All)
            {
                return projects.Count;
            }

            int count = 0;
            for (int i = 0; i < projects.Count; i++)
            {
                if (MatchesFilter(projects[i], filter))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<FilterButtonView> BuildFilterButtons(List<ABY_ProtocolResearchDef> projects)
        {
            List<FilterButtonView> buttons = new List<FilterButtonView>();
            buttons.Add(new FilterButtonView { Filter = ProtocolProjectFilter.All, ShortLabel = "All", Count = CountProjects(projects, ProtocolProjectFilter.All), Tooltip = "Show every project in the selected protocol layer." });
            buttons.Add(new FilterButtonView { Filter = ProtocolProjectFilter.Ready, ShortLabel = "Ready", Count = CountProjects(projects, ProtocolProjectFilter.Ready), Tooltip = "Show projects that are available or currently active." });
            buttons.Add(new FilterButtonView { Filter = ProtocolProjectFilter.Locked, ShortLabel = "Lock", Count = CountProjects(projects, ProtocolProjectFilter.Locked), Tooltip = "Show locked projects in this protocol layer." });
            buttons.Add(new FilterButtonView { Filter = ProtocolProjectFilter.Completed, ShortLabel = "Done", Count = CountProjects(projects, ProtocolProjectFilter.Completed), Tooltip = "Show completed / decoded projects." });
            buttons.Add(new FilterButtonView { Filter = ProtocolProjectFilter.Gated, ShortLabel = "Gate", Count = CountProjects(projects, ProtocolProjectFilter.Gated), Tooltip = "Show locked projects with explicit boss, sigil, material, forge, or external protocol gates." });
            return buttons;
        }

        private static string FilterLabel(ProtocolProjectFilter filter)
        {
            switch (filter)
            {
                case ProtocolProjectFilter.Ready:
                    return "Ready / Active";
                case ProtocolProjectFilter.Locked:
                    return "Locked";
                case ProtocolProjectFilter.Completed:
                    return "Completed";
                case ProtocolProjectFilter.Gated:
                    return "Explicit Gates";
                default:
                    return "All Projects";
            }
        }

        private static ABY_ProtocolResearchDef NextActionProject(List<ABY_ProtocolResearchDef> projects)
        {
            if (projects == null || projects.Count == 0)
            {
                return null;
            }

            ABY_ProtocolResearchDef available = null;
            ABY_ProtocolResearchDef gated = null;
            ABY_ProtocolResearchDef locked = null;
            for (int i = 0; i < projects.Count; i++)
            {
                ABY_ProtocolResearchDef project = projects[i];
                ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(project);
                if (state == ABY_ProtocolResearchState.Active)
                {
                    return project;
                }

                if (state == ABY_ProtocolResearchState.Available && available == null)
                {
                    available = project;
                }
                else if (state == ABY_ProtocolResearchState.Locked && IsExplicitlyGated(project) && gated == null)
                {
                    gated = project;
                }
                else if (state == ABY_ProtocolResearchState.Locked && locked == null)
                {
                    locked = project;
                }
            }

            return available ?? gated ?? locked;
        }

        private static bool IsExplicitlyGated(ABY_ProtocolResearchDef project)
        {
            if (project == null)
            {
                return false;
            }

            if (MissingResearchLabels(project).Count > 0)
            {
                return true;
            }

            if (ContainsGateKeyword(project.LabelCap) || ContainsGateKeyword(project.description) || ContainsGateKeyword(project.tierLabel) || ContainsGateKeyword(project.previewState))
            {
                return true;
            }

            if (ContainsGateKeyword(project.requirements) || ContainsGateKeyword(project.notes) || ContainsGateKeyword(project.unlocks))
            {
                return true;
            }

            return ABY_ProtocolResearchUtility.GetState(project) == ABY_ProtocolResearchState.Locked && ABY_ProtocolResearchUtility.PrerequisitesMet(project);
        }

        private static bool ContainsGateKeyword(List<string> lines)
        {
            if (lines == null)
            {
                return false;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (ContainsGateKeyword(lines[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsGateKeyword(string text)
        {
            if (text.NullOrEmpty())
            {
                return false;
            }

            string[] gateTerms =
            {
                "boss", "sigil", "shard", "core", "reactor", "saint", "archon",
                "crown", "herald", "dominion", "forge", "residue", "material",
                "gate", "locked", "requires", "requirement", "prerequisite"
            };

            for (int i = 0; i < gateTerms.Length; i++)
            {
                if (text.IndexOf(gateTerms[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> MissingResearchLabels(ABY_ProtocolResearchDef project)
        {
            List<string> missing = new List<string>();
            if (project?.requiredResearchProjects == null)
            {
                return missing;
            }

            for (int i = 0; i < project.requiredResearchProjects.Count; i++)
            {
                ResearchProjectDef prerequisite = project.requiredResearchProjects[i];
                if (prerequisite != null && !prerequisite.IsFinished)
                {
                    missing.Add(prerequisite.LabelCap);
                }
            }

            return missing;
        }

        private static string BlockingReasonText(ABY_ProtocolResearchDef project)
        {
            if (project == null)
            {
                return "No active protocol path.";
            }

            ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(project);
            if (state == ABY_ProtocolResearchState.Completed)
            {
                return "Decoded.";
            }

            if (state == ABY_ProtocolResearchState.Active)
            {
                return "Currently active.";
            }

            if (state == ABY_ProtocolResearchState.Available)
            {
                return "Ready now.";
            }

            List<string> missingResearch = MissingResearchLabels(project);
            if (missingResearch.Count > 0)
            {
                return "Requires research: " + Shorten(string.Join(", ", missingResearch.ToArray()), 42);
            }

            if (project.requirements != null && project.requirements.Count > 0)
            {
                return "Requires: " + Shorten(project.requirements[0], 48);
            }

            return "External protocol gate / future unlock.";
        }

        private static List<string> BuildDiagnosticLines(ABY_ProtocolResearchDef project)
        {
            List<string> lines = new List<string>();
            if (project == null)
            {
                lines.Add("No project selected.");
                return lines;
            }

            ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(project);
            lines.Add("State — " + ABY_ProtocolResearchUtility.GetStateLabel(state));
            lines.Add("Layer — " + LayerLabelFor(project));
            lines.Add("Filter bucket — " + DiagnosticBucketLabel(project));
            lines.Add("Blocking reason — " + BlockingReasonText(project));
            return lines;
        }

        private static string DiagnosticBucketLabel(ABY_ProtocolResearchDef project)
        {
            ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(project);
            if (state == ABY_ProtocolResearchState.Completed)
            {
                return FilterLabel(ProtocolProjectFilter.Completed);
            }

            if (state == ABY_ProtocolResearchState.Available || state == ABY_ProtocolResearchState.Active)
            {
                return FilterLabel(ProtocolProjectFilter.Ready);
            }

            return IsExplicitlyGated(project) ? FilterLabel(ProtocolProjectFilter.Gated) : FilterLabel(ProtocolProjectFilter.Locked);
        }

        private static string Shorten(string text, int maxChars)
        {
            if (text.NullOrEmpty())
            {
                return string.Empty;
            }

            if (maxChars <= 4 || text.Length <= maxChars)
            {
                return text;
            }

            return text.Substring(0, maxChars - 1).TrimEnd() + "…";
        }


        private static void CalculateLayerState(ResearchLayerView layer)
        {
            if (layer == null)
            {
                return;
            }

            layer.CompletedCount = 0;
            layer.ActiveCount = 0;
            layer.AvailableCount = 0;
            layer.LockedCount = 0;

            for (int i = 0; i < layer.Projects.Count; i++)
            {
                ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(layer.Projects[i]);
                switch (state)
                {
                    case ABY_ProtocolResearchState.Completed:
                        layer.CompletedCount++;
                        break;
                    case ABY_ProtocolResearchState.Active:
                        layer.ActiveCount++;
                        break;
                    case ABY_ProtocolResearchState.Available:
                        layer.AvailableCount++;
                        break;
                    default:
                        layer.LockedCount++;
                        break;
                }
            }

            layer.State = AggregateState(layer.Projects);
        }

        private static ABY_ProtocolResearchState AggregateState(List<ABY_ProtocolResearchDef> projects)
        {
            if (projects == null || projects.Count == 0)
            {
                return ABY_ProtocolResearchState.Locked;
            }

            bool anyActive = false;
            bool anyAvailable = false;
            bool allCompleted = true;

            for (int i = 0; i < projects.Count; i++)
            {
                ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(projects[i]);
                if (state != ABY_ProtocolResearchState.Completed)
                {
                    allCompleted = false;
                }

                if (state == ABY_ProtocolResearchState.Active)
                {
                    anyActive = true;
                }
                else if (state == ABY_ProtocolResearchState.Available)
                {
                    anyAvailable = true;
                }
            }

            if (allCompleted)
            {
                return ABY_ProtocolResearchState.Completed;
            }

            if (anyActive)
            {
                return ABY_ProtocolResearchState.Active;
            }

            if (anyAvailable)
            {
                return ABY_ProtocolResearchState.Available;
            }

            return ABY_ProtocolResearchState.Locked;
        }

        private static string LayerKeyFor(ABY_ProtocolResearchDef project)
        {
            string raw = project?.tierLabel;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "Unclassified";
            }

            return raw.Trim();
        }

        private static string LayerLabelFor(ABY_ProtocolResearchDef project)
        {
            string raw = project?.tierLabel;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "Unclassified protocol";
            }

            return raw.Trim();
        }

        private static string LayerProgressText(ResearchLayerView layer)
        {
            if (layer == null)
            {
                return string.Empty;
            }

            int total = layer.Projects.Count;
            return layer.CompletedCount + "/" + total + " decoded, " + (layer.AvailableCount + layer.ActiveCount) + " available";
        }

        private static string LayerGlyph(ResearchLayerView layer, int index)
        {
            if (layer != null && !layer.Label.NullOrEmpty())
            {
                const string prefix = "Tier ";
                if (layer.Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string rest = layer.Label.Substring(prefix.Length).Trim();
                    int dash = rest.IndexOf('—');
                    if (dash < 0)
                    {
                        dash = rest.IndexOf('-');
                    }

                    string glyph = dash >= 0 ? rest.Substring(0, dash).Trim() : rest;
                    if (!glyph.NullOrEmpty() && glyph.Length <= 5)
                    {
                        return glyph;
                    }
                }
            }

            return (index + 1).ToString();
        }


        private static List<string> BuildRevealLines(ABY_ProtocolResearchDef project)
        {
            List<string> lines = new List<string>();
            if (project?.reveals != null)
            {
                lines.AddRange(project.reveals);
            }

            if (lines.Count == 0 && project?.unlocks != null)
            {
                lines.AddRange(project.unlocks);
            }

            if (lines.Count == 0)
            {
                lines.Add("ABY_ProtocolResearch_NoExplicitReveals".Translate());
            }

            return lines;
        }

        private static List<string> BuildRequirementLines(ABY_ProtocolResearchDef project)
        {
            List<string> lines = new List<string>();
            if (project.requiredResearchProjects != null)
            {
                for (int i = 0; i < project.requiredResearchProjects.Count; i++)
                {
                    if (project.requiredResearchProjects[i] != null)
                    {
                        string state = project.requiredResearchProjects[i].IsFinished
                            ? "ABY_ProtocolResearch_RequirementMet".Translate()
                            : "ABY_ProtocolResearch_RequirementMissing".Translate();
                        lines.Add(project.requiredResearchProjects[i].LabelCap + " — " + state);
                    }
                }
            }

            if (project.requirements != null)
            {
                lines.AddRange(project.requirements);
            }

            if (lines.Count == 0)
            {
                lines.Add("ABY_ProtocolResearch_NoExplicitRequirements".Translate());
            }

            return lines;
        }

        private static float DrawParagraph(Rect viewRect, float y, string header, string body)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1f, 0.72f, 0.46f, 1f);
            Widgets.Label(new Rect(0f, y, viewRect.width, 20f), header);
            y += 22f;

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.88f, 0.84f, 0.78f, 1f);
            float height = Text.CalcHeight(body ?? string.Empty, viewRect.width);
            Widgets.Label(new Rect(0f, y, viewRect.width, height), body ?? string.Empty);
            GUI.color = Color.white;
            return y + height + 14f;
        }

        private static float DrawListSection(Rect viewRect, float y, string header, List<string> entries)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1f, 0.72f, 0.46f, 1f);
            Widgets.Label(new Rect(0f, y, viewRect.width, 20f), header);
            y += 22f;

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.84f, 0.80f, 0.74f, 1f);
            if (entries == null || entries.Count == 0)
            {
                Widgets.Label(new Rect(0f, y, viewRect.width, 18f), "—");
                y += 22f;
            }
            else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    string line = "• " + entries[i];
                    float height = Text.CalcHeight(line, viewRect.width);
                    Widgets.Label(new Rect(0f, y, viewRect.width, height), line);
                    y += height + 4f;
                }
            }

            GUI.color = Color.white;
            return y + 10f;
        }

        private static Texture2D SegmentTexture(ABY_ProtocolResearchState state)
        {
            switch (state)
            {
                case ABY_ProtocolResearchState.Completed:
                    return SegmentCompletedTex;
                case ABY_ProtocolResearchState.Active:
                    return SegmentActiveTex;
                case ABY_ProtocolResearchState.Available:
                    return SegmentAvailableTex;
                default:
                    return SegmentLockedTex;
            }
        }

        private static Color StateColor(ABY_ProtocolResearchState state)
        {
            switch (state)
            {
                case ABY_ProtocolResearchState.Completed:
                    return new Color(1f, 0.72f, 0.28f, 1f);
                case ABY_ProtocolResearchState.Active:
                    return new Color(1f, 0.28f, 0.12f, 1f);
                case ABY_ProtocolResearchState.Available:
                    return new Color(1f, 0.48f, 0.18f, 1f);
                default:
                    return new Color(0.48f, 0.23f, 0.16f, 1f);
            }
        }

        private static void DrawCornerBrackets(Rect rect, Color color, float length, float width)
        {
            DrawSolid(new Rect(rect.x, rect.y, length, width), color);
            DrawSolid(new Rect(rect.x, rect.y, width, length), color);
            DrawSolid(new Rect(rect.xMax - length, rect.y, length, width), color);
            DrawSolid(new Rect(rect.xMax - width, rect.y, width, length), color);
            DrawSolid(new Rect(rect.x, rect.yMax - width, length, width), color);
            DrawSolid(new Rect(rect.x, rect.yMax - length, width, length), color);
            DrawSolid(new Rect(rect.xMax - length, rect.yMax - width, length, width), color);
            DrawSolid(new Rect(rect.xMax - width, rect.yMax - length, width, length), color);
        }

        private static void DrawPanel(Rect rect, bool highlighted)
        {
            DrawSolid(rect, highlighted ? new Color(0.055f, 0.045f, 0.042f, 0.88f) : new Color(0.035f, 0.032f, 0.032f, 0.78f));
            DrawOutline(rect, highlighted ? new Color(0.88f, 0.32f, 0.12f, 0.58f) : new Color(0.55f, 0.22f, 0.13f, 0.36f));
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = oldColor;
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            Widgets.DrawBox(rect, 1);
            GUI.color = oldColor;
        }
    }
}
