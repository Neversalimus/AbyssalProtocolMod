using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Window_AbyssalProtocolNexus : Window
    {
        private const string BackgroundPath = "UI/ABY/ProtocolResearch/ABY_NexusWindowBackground";
        private const string SmallRingPath = "UI/ABY/ProtocolResearch/ABY_SmallCategoryRingFrame";
        private const string LargeRingPath = "UI/ABY/ProtocolResearch/ABY_LargeResearchRing";
        private const string SegmentLockedPath = "UI/ABY/ProtocolResearch/Segments/ABY_Segment_Locked";
        private const string SegmentAvailablePath = "UI/ABY/ProtocolResearch/Segments/ABY_Segment_Available";
        private const string SegmentActivePath = "UI/ABY/ProtocolResearch/Segments/ABY_Segment_Active";
        private const string SegmentCompletedPath = "UI/ABY/ProtocolResearch/Segments/ABY_Segment_Completed";

        private static readonly Texture2D BackgroundTex = ContentFinder<Texture2D>.Get(BackgroundPath, false);
        private static readonly Texture2D SmallRingTex = ContentFinder<Texture2D>.Get(SmallRingPath, false);
        private static readonly Texture2D LargeRingTex = ContentFinder<Texture2D>.Get(LargeRingPath, false);
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
            if (selectedCategory == null || !categories.Contains(selectedCategory))
            {
                selectedCategory = categories.FirstOrDefault();
                selectedProject = null;
                selectedLayerKey = null;
            }

            List<ABY_ProtocolResearchDef> selectedProjects = ABY_ProtocolResearchUtility.ProjectsFor(selectedCategory);
            if (selectedProject == null || !selectedProjects.Contains(selectedProject))
            {
                selectedProject = selectedProjects.FirstOrDefault();
                selectedLayerKey = selectedProject == null ? null : LayerKeyFor(selectedProject);
            }

            List<ResearchLayerView> selectedLayers = BuildLayerViews(selectedProjects);
            EnsureSelectedLayer(selectedLayers);
            ResearchLayerView activeLayer = SelectedLayer(selectedLayers);
            List<ABY_ProtocolResearchDef> layerProjects = ProjectsForSelectedLayer(selectedLayers, selectedProjects);
            List<ABY_ProtocolResearchDef> displayedProjects = FilterProjects(layerProjects, selectedFilter);
            if (displayedProjects.Count > 0 && (selectedProject == null || !displayedProjects.Contains(selectedProject)))
            {
                selectedProject = displayedProjects.FirstOrDefault();
            }
            else if (displayedProjects.Count == 0 && (selectedProject == null || !layerProjects.Contains(selectedProject)))
            {
                selectedProject = layerProjects.FirstOrDefault() ?? selectedProjects.FirstOrDefault();
            }

            DrawBackground(inRect);

            Rect headerRect = new Rect(inRect.x + 14f, inRect.y + 12f, inRect.width - 28f, 66f);
            Rect categoryRect = new Rect(inRect.x + 22f, headerRect.yMax + 12f, 760f, 112f);
            Rect ringRect = new Rect(inRect.x + 20f, categoryRect.yMax + 12f, 762f, inRect.height - categoryRect.yMax - 22f);
            Rect rightRect = new Rect(ringRect.xMax + 16f, headerRect.yMax + 12f, inRect.width - ringRect.xMax - 36f, inRect.height - headerRect.yMax - 22f);

            DrawHeader(headerRect, categories);
            DrawCategoryRings(categoryRect, categories);
            DrawCategoryDetailRing(ringRect, selectedProjects, selectedLayers, activeLayer, layerProjects, displayedProjects);
            DrawProjectDetails(rightRect, selectedProject);
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
            int total = DefDatabase<ABY_ProtocolResearchDef>.AllDefsListForReading.Count;
            int available = categories.Sum(ABY_ProtocolResearchUtility.CountAvailable);
            int completed = categories.Sum(ABY_ProtocolResearchUtility.CountVisibleCompleted);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 16f, rect.y + 8f, rect.width - 260f, 30f), "ABY_ProtocolResearch_Title".Translate());

            Text.Font = GameFont.Small;
            GUI.color = new Color(0.88f, 0.78f, 0.69f, 1f);
            Widgets.Label(new Rect(rect.x + 18f, rect.y + 38f, rect.width - 36f, 22f), "ABY_ProtocolResearch_Subtitle".Translate());

            Text.Anchor = TextAnchor.MiddleRight;
            GUI.color = new Color(1f, 0.78f, 0.54f, 1f);
            Widgets.Label(new Rect(rect.xMax - 270f, rect.y + 9f, 240f, 24f), "ABY_ProtocolResearch_HeaderProgress".Translate(available, completed, total));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawCategoryRings(Rect rect, List<ABY_ProtocolResearchCategoryDef> categories)
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

            float itemSize = 82f;
            float gap = 9f;
            float startX = rect.x + 12f;
            float y = rect.y + 25f;
            for (int i = 0; i < categories.Count; i++)
            {
                ABY_ProtocolResearchCategoryDef category = categories[i];
                Rect itemRect = new Rect(startX + i * (itemSize + gap), y, itemSize, itemSize);
                bool selected = category == selectedCategory;
                DrawCategoryRing(itemRect, category, selected);
                if (Widgets.ButtonInvisible(itemRect))
                {
                    selectedCategory = category;
                    selectedProject = ABY_ProtocolResearchUtility.ProjectsFor(category).FirstOrDefault();
                    selectedLayerKey = selectedProject == null ? null : LayerKeyFor(selectedProject);
                    selectedFilter = ProtocolProjectFilter.All;
                    projectListScroll = Vector2.zero;
                    detailsScroll = Vector2.zero;
                }
            }
        }

        private void DrawCategoryRing(Rect rect, ABY_ProtocolResearchCategoryDef category, bool selected)
        {
            int available = ABY_ProtocolResearchUtility.CountAvailable(category);
            int completed = ABY_ProtocolResearchUtility.CountVisibleCompleted(category);
            int total = ABY_ProtocolResearchUtility.ProjectsFor(category).Count;
            bool hover = Mouse.IsOver(rect);

            Color oldColor = GUI.color;
            GUI.color = selected ? new Color(1f, 0.86f, 0.68f, 1f) : hover ? new Color(1f, 0.72f, 0.50f, 0.94f) : Color.white;
            if (SmallRingTex != null)
            {
                GUI.DrawTexture(rect, SmallRingTex, ScaleMode.ScaleToFit, true);
            }
            else
            {
                Widgets.DrawBox(rect);
            }

            Texture2D icon = category.iconPath.NullOrEmpty() ? null : ContentFinder<Texture2D>.Get(category.iconPath, false);
            if (icon != null)
            {
                GUI.color = selected ? Color.white : new Color(0.86f, 0.82f, 0.78f, 0.92f);
                GUI.DrawTexture(rect.ContractedBy(18f), icon, ScaleMode.ScaleToFit, true);
            }
            GUI.color = oldColor;

            Rect countRect = new Rect(rect.x + 8f, rect.yMax - 18f, rect.width - 16f, 18f);
            DrawSolid(countRect, new Color(0f, 0f, 0f, 0.62f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = available > 0 ? new Color(1f, 0.72f, 0.42f, 1f) : new Color(0.72f, 0.68f, 0.62f, 1f);
            Widgets.Label(countRect, completed + "/" + total);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            if (hover)
            {
                string tooltip = category.LabelCap + "\n" + category.description + "\n\n" + "ABY_ProtocolResearch_CategoryTooltip".Translate(available, completed, total);
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private void DrawCategoryDetailRing(Rect rect, List<ABY_ProtocolResearchDef> projects, List<ResearchLayerView> layers, ResearchLayerView activeLayer, List<ABY_ProtocolResearchDef> layerProjects, List<ABY_ProtocolResearchDef> displayedProjects)
        {
            DrawPanel(rect, false);
            if (selectedCategory == null)
            {
                return;
            }

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 24f), selectedCategory.LabelCap);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.76f, 0.72f, 0.68f, 1f);
            Widgets.Label(new Rect(rect.x + 14f, rect.y + 32f, rect.width - 28f, 34f), selectedCategory.description);
            GUI.color = Color.white;

            float ringSize = Mathf.Min(430f, Mathf.Max(360f, rect.height - 128f));
            Rect ringArea = new Rect(rect.x + 30f, rect.y + 70f, ringSize, ringSize);
            Rect localRingArea = new Rect(0f, 0f, ringArea.width, ringArea.height);

            // Keep the large ring and its minimal markers inside one local canvas.
            // Navigation remains driven by the stable project list to the right.
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

            DrawRingAmbientGlow(ringArea, projects);
            DrawInnerProtocolMotion(ringArea, activeLayer);
            DrawRingProgressTicks(ringArea, projects);
            DrawStaticProtocolAnchors(ringArea, layers, activeLayer);
            DrawSelectedLayerConduit(ringArea, layers, activeLayer);
            DrawLayerNodes(ringArea, layers);
            DrawRingCenterDashboard(ringArea, projects, activeLayer, layerProjects);
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


        private static void DrawRingAmbientGlow(Rect ringArea, List<ABY_ProtocolResearchDef> projects)
        {
            if (projects == null || projects.Count == 0 || LargeRingTex == null)
            {
                return;
            }

            int ready = CountProjects(projects, ProtocolProjectFilter.Ready);
            int completed = CountProjects(projects, ProtocolProjectFilter.Completed);
            float completion = projects.Count == 0 ? 0f : Mathf.Clamp01((float)completed / projects.Count);
            float pulse = 0.5f + Mathf.Sin(Time.realtimeSinceStartup * 1.18f) * 0.5f;

            Color glow = ready > 0
                ? new Color(1f, 0.46f, 0.14f, 0.055f + pulse * 0.035f)
                : new Color(0.70f, 0.24f, 0.10f, 0.028f + pulse * 0.020f);

            if (completion > 0.55f)
            {
                glow = Color.Lerp(glow, new Color(1f, 0.76f, 0.34f, 0.070f + pulse * 0.032f), Mathf.Clamp01(completion));
            }

            Color oldColor = GUI.color;
            GUI.color = glow;
            GUI.DrawTexture(ringArea.ExpandedBy(4f), LargeRingTex, ScaleMode.ScaleToFit, true);
            GUI.color = new Color(glow.r, glow.g, glow.b, glow.a * 0.55f);
            GUI.DrawTexture(ringArea.ExpandedBy(9f), LargeRingTex, ScaleMode.ScaleToFit, true);
            GUI.color = oldColor;
        }

        private static void DrawInnerProtocolMotion(Rect ringArea, ResearchLayerView activeLayer)
        {
            Vector2 center = ringArea.center;
            float radius = ringArea.width * 0.235f;
            float counterRadius = ringArea.width * 0.182f;
            float time = Time.realtimeSinceStartup;
            Color baseColor = activeLayer == null ? new Color(1f, 0.43f, 0.18f, 1f) : StateColor(activeLayer.State);

            for (int i = 0; i < 16; i++)
            {
                float angle = -90f + i * 22.5f + time * 6.2f;
                float rad = angle * Mathf.Deg2Rad;
                float pulse = 0.5f + Mathf.Sin(time * 1.6f + i * 0.71f) * 0.5f;
                Vector2 pos = center + new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
                float size = 1.8f + pulse * 1.5f;
                DrawSolid(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size), new Color(baseColor.r, baseColor.g, baseColor.b, 0.040f + pulse * 0.050f));
            }

            for (int i = 0; i < 10; i++)
            {
                float angle = -90f + i * 36f - time * 4.4f;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 pos = center + new Vector2(Mathf.Cos(rad) * counterRadius, Mathf.Sin(rad) * counterRadius);
                DrawSolid(new Rect(pos.x - 1.25f, pos.y - 1.25f, 2.5f, 2.5f), new Color(1f, 0.55f, 0.20f, 0.045f));
            }
        }


        private static void DrawStaticProtocolAnchors(Rect ringArea, List<ResearchLayerView> layers, ResearchLayerView activeLayer)
        {
            Vector2 center = ringArea.center;
            float outerRadius = ringArea.width * 0.438f;
            float innerRadius = ringArea.width * 0.338f;
            float time = Time.realtimeSinceStartup;
            Color stateColor = activeLayer == null ? new Color(1f, 0.42f, 0.14f, 1f) : StateColor(activeLayer.State);
            int layerCount = layers?.Count ?? 0;
            int availableLayers = layers == null ? 0 : layers.Count(layer => layer.AvailableCount > 0 || layer.ActiveCount > 0);
            float readiness = layerCount == 0 ? 0f : Mathf.Clamp01((float)availableLayers / layerCount);

            for (int i = 0; i < 8; i++)
            {
                float angle = -90f + i * 45f;
                float rad = angle * Mathf.Deg2Rad;
                float pulse = 0.5f + Mathf.Sin(time * 1.05f + i * 0.92f) * 0.5f;
                float alpha = 0.085f + pulse * 0.055f + readiness * 0.045f;
                Vector2 outer = center + new Vector2(Mathf.Cos(rad) * outerRadius, Mathf.Sin(rad) * outerRadius);
                Vector2 inner = center + new Vector2(Mathf.Cos(rad) * innerRadius, Mathf.Sin(rad) * innerRadius);

                float primary = (i % 2 == 0) ? 13f : 9f;
                Rect outerRect = new Rect(outer.x - primary * 0.5f, outer.y - primary * 0.5f, primary, primary);
                Color anchorColor = new Color(stateColor.r, stateColor.g, stateColor.b, alpha);
                DrawOutline(outerRect, anchorColor);
                DrawSolid(new Rect(outer.x - 2f, outer.y - 2f, 4f, 4f), new Color(1f, 0.58f, 0.22f, alpha * 1.45f));

                if (i % 2 == 0)
                {
                    DrawOutline(outerRect.ExpandedBy(5f), new Color(stateColor.r, stateColor.g, stateColor.b, alpha * 0.38f));
                }

                float innerSize = (i % 2 == 0) ? 5f : 3.6f;
                DrawSolid(new Rect(inner.x - innerSize * 0.5f, inner.y - innerSize * 0.5f, innerSize, innerSize), new Color(1f, 0.39f, 0.15f, alpha * 0.72f));
            }
        }

        private static void DrawSelectedLayerConduit(Rect ringArea, List<ResearchLayerView> layers, ResearchLayerView activeLayer)
        {
            if (layers == null || activeLayer == null || layers.Count == 0)
            {
                return;
            }

            int index = layers.IndexOf(activeLayer);
            if (index < 0)
            {
                return;
            }

            Vector2 center = ringArea.center;
            int count = Mathf.Max(1, layers.Count);
            float angle = -90f + (360f * index / count);
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Color stateColor = StateColor(activeLayer.State);
            float pulse = 0.5f + Mathf.Sin(Time.realtimeSinceStartup * 2.15f) * 0.5f;
            float baseAlpha = 0.13f + pulse * 0.075f;

            float startRadius = ringArea.width * 0.225f;
            float endRadius = ringArea.width * 0.345f;
            for (int i = 0; i < 11; i++)
            {
                float t = i / 10f;
                float radius = Mathf.Lerp(startRadius, endRadius, t);
                Vector2 pos = center + dir * radius;
                float size = Mathf.Lerp(2.2f, 4.1f, t);
                float alpha = baseAlpha * Mathf.Lerp(0.45f, 1f, t);
                DrawSolid(new Rect(pos.x - size * 0.5f, pos.y - size * 0.5f, size, size), new Color(stateColor.r, stateColor.g, stateColor.b, alpha));
            }

            Vector2 innerAnchor = center + dir * startRadius;
            Vector2 outerAnchor = center + dir * endRadius;
            DrawOutline(new Rect(innerAnchor.x - 8f, innerAnchor.y - 8f, 16f, 16f), new Color(1f, 0.45f, 0.18f, 0.16f + pulse * 0.10f));
            DrawOutline(new Rect(outerAnchor.x - 12f, outerAnchor.y - 12f, 24f, 24f), new Color(1f, 0.62f, 0.24f, 0.20f + pulse * 0.14f));
        }

        private void DrawLayerNodes(Rect ringArea, List<ResearchLayerView> layers)
        {
            if (layers == null || layers.Count == 0)
            {
                return;
            }

            Vector2 center = ringArea.center;
            float radius = ringArea.width * 0.382f;
            int count = Mathf.Max(1, layers.Count);

            for (int i = 0; i < layers.Count; i++)
            {
                ResearchLayerView layer = layers[i];
                float angle = -90f + (360f * i / count);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 pos = center + new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);

                bool selected = layer.Key == selectedLayerKey;
                Rect hitRect = new Rect(pos.x - 26f, pos.y - 26f, 52f, 52f);
                bool hover = Mouse.IsOver(hitRect);
                float nodeSize = selected ? 36f : hover ? 32f : 28f;
                Rect nodeRect = new Rect(pos.x - nodeSize * 0.5f, pos.y - nodeSize * 0.5f, nodeSize, nodeSize);

                DrawLayerNode(nodeRect, layer, i, selected, hover);

                if (Widgets.ButtonInvisible(hitRect))
                {
                    selectedLayerKey = layer.Key;
                    selectedProject = layer.Projects.FirstOrDefault();
                    projectListScroll = Vector2.zero;
                    detailsScroll = Vector2.zero;
                }

                if (hover)
                {
                    TooltipHandler.TipRegion(hitRect, layer.Label + "\n" + LayerProgressText(layer) + "\n\n" + "Click to filter the project list to this protocol layer.");
                }
            }
        }

        private static void DrawLayerNode(Rect rect, ResearchLayerView layer, int index, bool selected, bool hover)
        {
            Color stateColor = StateColor(layer.State);
            float selectedPulse = 0.5f + Mathf.Sin(Time.realtimeSinceStartup * 3.6f) * 0.5f;
            float idlePulse = 0.5f + Mathf.Sin(Time.realtimeSinceStartup * 1.35f + index * 1.71f) * 0.5f;
            Rect backingRect = rect.ExpandedBy(selected ? 8f : 5f);

            DrawSolid(backingRect, new Color(0f, 0f, 0f, selected ? 0.80f : 0.62f));

            if (!selected)
            {
                DrawOutline(backingRect.ExpandedBy(2f), new Color(stateColor.r, stateColor.g, stateColor.b, 0.045f + idlePulse * 0.075f));
            }

            Color fill = stateColor;
            fill.a = selected ? 0.92f : hover ? 0.80f : 0.56f + idlePulse * 0.08f;
            DrawSolid(rect, fill);

            Color outline = selected
                ? new Color(1f, 0.70f, 0.34f, 0.98f)
                : hover
                    ? new Color(1f, 0.48f, 0.20f, 0.82f)
                    : new Color(0.72f, 0.28f, 0.14f, 0.42f + idlePulse * 0.12f);
            DrawOutline(backingRect, outline);

            if (selected)
            {
                Color halo = new Color(1f, 0.43f, 0.15f, 0.30f + selectedPulse * 0.24f);
                DrawOutline(backingRect.ExpandedBy(5f), halo);
                DrawOutline(backingRect.ExpandedBy(10f), new Color(1f, 0.32f, 0.10f, 0.12f + selectedPulse * 0.13f));
                DrawFocusBrackets(backingRect.ExpandedBy(13f), new Color(1f, 0.62f, 0.24f, 0.36f + selectedPulse * 0.24f));
            }
            else if (hover)
            {
                DrawFocusBrackets(backingRect.ExpandedBy(8f), new Color(1f, 0.42f, 0.16f, 0.22f));
            }

            float coreSize = selected ? 5.4f : 3.4f;
            DrawSolid(new Rect(rect.center.x - coreSize * 0.5f, rect.y + 4f, coreSize, coreSize), new Color(1f, 0.76f, 0.34f, selected ? 0.46f + selectedPulse * 0.25f : 0.18f + idlePulse * 0.12f));

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            GUI.color = selected ? Color.white : new Color(1f, 0.86f, 0.68f, 0.92f);
            Widgets.Label(rect, LayerGlyph(layer, index));

            Text.Anchor = oldAnchor;
            Text.Font = oldFont;
            GUI.color = oldColor;
        }

        private static void DrawFocusBrackets(Rect rect, Color color)
        {
            float length = Mathf.Min(14f, rect.width * 0.28f);
            const float thickness = 2f;

            DrawSolid(new Rect(rect.x, rect.y, length, thickness), color);
            DrawSolid(new Rect(rect.x, rect.y, thickness, length), color);

            DrawSolid(new Rect(rect.xMax - length, rect.y, length, thickness), color);
            DrawSolid(new Rect(rect.xMax - thickness, rect.y, thickness, length), color);

            DrawSolid(new Rect(rect.x, rect.yMax - thickness, length, thickness), color);
            DrawSolid(new Rect(rect.x, rect.yMax - length, thickness, length), color);

            DrawSolid(new Rect(rect.xMax - length, rect.yMax - thickness, length, thickness), color);
            DrawSolid(new Rect(rect.xMax - thickness, rect.yMax - length, thickness, length), color);
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

            Rect summaryRect = new Rect(ringArea.center.x - 96f, ringArea.center.y - 43f, 192f, 86f);
            DrawSolid(summaryRect, new Color(0.012f, 0.010f, 0.009f, 0.66f));
            DrawOutline(summaryRect, new Color(0.78f, 0.30f, 0.12f, 0.34f));

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(summaryRect.x + 8f, summaryRect.y + 8f, summaryRect.width - 16f, 22f), Shorten(selectedCategory?.LabelCap ?? string.Empty, 24));

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.88f, 0.78f, 0.66f, 1f);
            Widgets.Label(new Rect(summaryRect.x + 8f, summaryRect.y + 32f, summaryRect.width - 16f, 18f), completed + "/" + total + " decoded  •  " + ready + " ready");

            GUI.color = selectedLayer == null ? new Color(0.70f, 0.66f, 0.60f, 1f) : StateColor(selectedLayer.State);
            Widgets.Label(new Rect(summaryRect.x + 8f, summaryRect.y + 52f, summaryRect.width - 16f, 18f), Shorten(selectedLayer?.Label ?? "No protocol layer", 30));

            GUI.color = new Color(0.66f, 0.58f, 0.50f, 0.92f);
            Widgets.Label(new Rect(summaryRect.x + 8f, summaryRect.y + 68f, summaryRect.width - 16f, 14f), "Layer matrix / list filters");

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

            Rect layerRect = new Rect(rect.x + 10f, titleRect.yMax + 2f, rect.width - 20f, 18f);
            Text.Font = GameFont.Tiny;
            GUI.color = selectedLayer == null ? new Color(0.72f, 0.68f, 0.62f, 1f) : StateColor(selectedLayer.State);
            Widgets.Label(layerRect, selectedLayer == null ? "No protocol layer" : Shorten(selectedLayer.Label + "  —  " + LayerProgressText(selectedLayer), 58));
            GUI.color = Color.white;

            Rect filterRect = new Rect(rect.x + 10f, layerRect.yMax + 5f, rect.width - 20f, 24f);
            DrawFilterStrip(filterRect, layerProjects);

            Rect outRect = new Rect(rect.x + 8f, filterRect.yMax + 6f, rect.width - 16f, rect.yMax - filterRect.yMax - 14f);
            float rowHeight = 48f;
            int projectCount = projects?.Count ?? 0;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, projectCount * rowHeight + 8f));
            Widgets.BeginScrollView(outRect, ref projectListScroll, viewRect);
            if (projectCount == 0)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.72f, 0.68f, 0.62f, 1f);
                string emptyMessage = selectedFilter == ProtocolProjectFilter.All
                    ? "No projects are assigned to this protocol layer."
                    : "No projects match this list filter. Use All or another state tab.";
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
            Widgets.EndScrollView();
        }

        private void DrawProjectListRow(Rect rect, ABY_ProtocolResearchDef project)
        {
            bool selected = project == selectedProject;
            DrawSolid(rect, selected ? new Color(0.28f, 0.12f, 0.07f, 0.88f) : new Color(0.05f, 0.045f, 0.045f, 0.76f));
            DrawOutline(rect, selected ? new Color(1f, 0.48f, 0.20f, 0.75f) : new Color(0.52f, 0.20f, 0.12f, 0.35f));

            ABY_ProtocolResearchState state = ABY_ProtocolResearchUtility.GetState(project);
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

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;

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
            Widgets.BeginScrollView(outRect, ref detailsScroll, viewRect);
            float y = 0f;

            y = DrawParagraph(viewRect, y, "ABY_ProtocolResearch_DescriptionHeader".Translate(), project.description);
            y = DrawListSection(viewRect, y, "Protocol Diagnostic", BuildDiagnosticLines(project));
            y = DrawListSection(viewRect, y, "ABY_ProtocolResearch_RequirementsHeader".Translate(), BuildRequirementLines(project));
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
            Widgets.EndScrollView();
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
