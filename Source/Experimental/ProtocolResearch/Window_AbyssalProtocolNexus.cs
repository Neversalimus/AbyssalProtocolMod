using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string RingArcLockedPath = "UI/ABY/ProtocolResearch/Segments/ABY_RingArc_Locked";
        private const string RingArcAvailablePath = "UI/ABY/ProtocolResearch/Segments/ABY_RingArc_Available";
        private const string RingArcActivePath = "UI/ABY/ProtocolResearch/Segments/ABY_RingArc_Active";
        private const string RingArcCompletedPath = "UI/ABY/ProtocolResearch/Segments/ABY_RingArc_Completed";

        private static readonly Texture2D BackgroundTex = ContentFinder<Texture2D>.Get(BackgroundPath, false);
        private static readonly Texture2D SmallRingTex = ContentFinder<Texture2D>.Get(SmallRingPath, false);
        private static readonly Texture2D LargeRingTex = ContentFinder<Texture2D>.Get(LargeRingPath, false);
        private static readonly Texture2D SegmentLockedTex = ContentFinder<Texture2D>.Get(SegmentLockedPath, false);
        private static readonly Texture2D SegmentAvailableTex = ContentFinder<Texture2D>.Get(SegmentAvailablePath, false);
        private static readonly Texture2D SegmentActiveTex = ContentFinder<Texture2D>.Get(SegmentActivePath, false);
        private static readonly Texture2D SegmentCompletedTex = ContentFinder<Texture2D>.Get(SegmentCompletedPath, false);
        private static readonly Texture2D RingArcLockedTex = ContentFinder<Texture2D>.Get(RingArcLockedPath, false);
        private static readonly Texture2D RingArcAvailableTex = ContentFinder<Texture2D>.Get(RingArcAvailablePath, false);
        private static readonly Texture2D RingArcActiveTex = ContentFinder<Texture2D>.Get(RingArcActivePath, false);
        private static readonly Texture2D RingArcCompletedTex = ContentFinder<Texture2D>.Get(RingArcCompletedPath, false);

        private readonly Building_ABY_ProtocolNexus nexus;
        private ABY_ProtocolResearchCategoryDef selectedCategory;
        private ABY_ProtocolResearchDef selectedProject;
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
            }

            List<ABY_ProtocolResearchDef> selectedProjects = ABY_ProtocolResearchUtility.ProjectsFor(selectedCategory);
            if (selectedProject == null || !selectedProjects.Contains(selectedProject))
            {
                selectedProject = selectedProjects.FirstOrDefault();
            }

            DrawBackground(inRect);

            Rect headerRect = new Rect(inRect.x + 14f, inRect.y + 12f, inRect.width - 28f, 66f);
            Rect categoryRect = new Rect(inRect.x + 22f, headerRect.yMax + 12f, 760f, 112f);
            Rect ringRect = new Rect(inRect.x + 20f, categoryRect.yMax + 12f, 762f, inRect.height - categoryRect.yMax - 22f);
            Rect rightRect = new Rect(ringRect.xMax + 16f, headerRect.yMax + 12f, inRect.width - ringRect.xMax - 36f, inRect.height - headerRect.yMax - 22f);

            DrawHeader(headerRect, categories);
            DrawCategoryRings(categoryRect, categories);
            DrawCategoryDetailRing(ringRect, selectedProjects);
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

        private void DrawCategoryDetailRing(Rect rect, List<ABY_ProtocolResearchDef> projects)
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

            // Keep all rotated segment overlays clipped to the ring canvas.
            // The previous pass drew rotated plates in absolute window space; on some UI scales they visibly drifted into the category strip.
            GUI.BeginGroup(ringArea);
            if (LargeRingTex != null)
            {
                GUI.DrawTexture(localRingArea, LargeRingTex, ScaleMode.ScaleToFit, true);
            }
            else
            {
                Widgets.DrawBox(localRingArea);
            }

            DrawProjectSegments(localRingArea, projects);
            GUI.EndGroup();

            Rect listRect = new Rect(ringArea.xMax + 16f, rect.y + 70f, rect.width - ringArea.width - 62f, rect.height - 86f);
            DrawProjectList(listRect, projects);
        }

        private void DrawProjectSegments(Rect ringArea, List<ABY_ProtocolResearchDef> projects)
        {
            if (projects == null || projects.Count == 0)
            {
                return;
            }

            Vector2 center = ringArea.center;
            float hitRadius = ringArea.width * 0.365f;
            float hitSize = 72f;
            float angleStep = 360f / Mathf.Max(1, projects.Count);

            for (int i = 0; i < projects.Count; i++)
            {
                ABY_ProtocolResearchDef project = projects[i];
                float angle = -90f + i * angleStep;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 hitPos = center + new Vector2(Mathf.Cos(rad) * hitRadius, Mathf.Sin(rad) * hitRadius);
                Rect hitRect = new Rect(hitPos.x - hitSize * 0.5f, hitPos.y - hitSize * 0.5f, hitSize, hitSize);
                bool selected = project == selectedProject;
                bool hover = Mouse.IsOver(hitRect);

                // Draw a true arc overlay derived for the static research ring instead of rotating
                // rectangular list plates.  The earlier rectangular overlays were visually unstable
                // because they could never match the circular metal band.
                DrawRingProjectArc(ringArea, project, selected, hover, angle + 90f);

                if (Widgets.ButtonInvisible(hitRect))
                {
                    selectedProject = project;
                    detailsScroll = Vector2.zero;
                }

                if (hover)
                {
                    TooltipHandler.TipRegion(hitRect, project.LabelCap + "\n" + project.description + "\n\n" + ABY_ProtocolResearchUtility.GetStateLabel(ABY_ProtocolResearchUtility.GetState(project)));
                }
            }
        }

        private void DrawRingProjectArc(Rect ringArea, ABY_ProtocolResearchDef project, bool selected, bool hover, float rotationDegrees)
        {
            Texture2D tex = RingArcTexture(ABY_ProtocolResearchUtility.GetState(project));
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;

            GUIUtility.RotateAroundPivot(rotationDegrees, ringArea.center);

            if (tex != null)
            {
                float alpha = selected ? 1f : hover ? 0.86f : 0.62f;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(ringArea, tex, ScaleMode.StretchToFill, true);

                if (selected || hover)
                {
                    GUI.color = new Color(1f, 1f, 1f, selected ? 0.38f : 0.22f);
                    GUI.DrawTexture(ringArea, tex, ScaleMode.StretchToFill, true);
                }
            }

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawProjectList(Rect rect, List<ABY_ProtocolResearchDef> projects)
        {
            DrawPanel(rect, true);
            Rect titleRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 20f);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1f, 0.72f, 0.46f, 1f);
            Widgets.Label(titleRect, "ABY_ProtocolResearch_ProjectListHeader".Translate());
            GUI.color = Color.white;

            Rect outRect = new Rect(rect.x + 8f, titleRect.yMax + 6f, rect.width - 16f, rect.height - 42f);
            float rowHeight = 48f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, projects.Count * rowHeight + 8f));
            Widgets.BeginScrollView(outRect, ref projectListScroll, viewRect);
            for (int i = 0; i < projects.Count; i++)
            {
                ABY_ProtocolResearchDef project = projects[i];
                Rect row = new Rect(0f, 4f + i * rowHeight, viewRect.width, rowHeight - 6f);
                DrawProjectListRow(row, project);
                if (Widgets.ButtonInvisible(row))
                {
                    selectedProject = project;
                    detailsScroll = Vector2.zero;
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

        private static Texture2D RingArcTexture(ABY_ProtocolResearchState state)
        {
            switch (state)
            {
                case ABY_ProtocolResearchState.Completed:
                    return RingArcCompletedTex;
                case ABY_ProtocolResearchState.Active:
                    return RingArcActiveTex;
                case ABY_ProtocolResearchState.Available:
                    return RingArcAvailableTex;
                default:
                    return RingArcLockedTex;
            }
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
