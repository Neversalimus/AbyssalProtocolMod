using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public sealed class Window_ABY_BestiaryCodex : Window
    {
        private enum StatusFilterMode
        {
            Any,
            Locked,
            Discovered,
            Studied
        }

        private enum CategoryFilterMode
        {
            Any,
            Assault,
            Elite,
            Support,
            Boss
        }

        private enum SortMode
        {
            Threat,
            Kills,
            Name,
            Recent
        }

        private readonly Building_AbyssalSummoningCircle circle;
        private Vector2 listScrollPosition = Vector2.zero;
        private Vector2 detailScrollPosition = Vector2.zero;
        private string selectedEntryId;
        private StatusFilterMode statusFilterMode = StatusFilterMode.Any;
        private CategoryFilterMode categoryFilterMode = CategoryFilterMode.Any;
        private SortMode sortMode = SortMode.Threat;

        public Window_ABY_BestiaryCodex(Building_AbyssalSummoningCircle circle)
        {
            this.circle = circle;
            selectedEntryId = ABY_BestiaryUtility.GetTrackedEntries().FirstOrDefault()?.EntryId ?? string.Empty;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
            draggable = true;
            forcePause = false;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = false;
            resizeable = false;
        }

        public override Vector2 InitialSize => new Vector2(1420f, 920f);

        public override void DoWindowContents(Rect inRect)
        {
            if (circle != null)
            {
                AbyssalSummoningConsoleArt.ReducedEffects = circle.ReducedConsoleEffects;
            }

            AbyssalSummoningConsoleArt.DrawBackground(inRect);

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 74f);
            Rect summaryRect = new Rect(inRect.x, headerRect.yMax + 10f, inRect.width, 64f);
            Rect filtersRect = new Rect(inRect.x, summaryRect.yMax + 10f, inRect.width, 58f);
            Rect listRect = new Rect(inRect.x, filtersRect.yMax + 10f, 468f, inRect.height - filtersRect.yMax - 10f);
            Rect detailRect = new Rect(listRect.xMax + 10f, filtersRect.yMax + 10f, inRect.width - listRect.width - 10f, inRect.height - filtersRect.yMax - 10f);

            DrawHeader(headerRect);
            DrawSummary(summaryRect);
            DrawFilters(filtersRect);
            DrawBrowser(listRect);
            DrawDetails(detailRect);
        }

        private void DrawHeader(Rect rect)
        {
            string subtitle = AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_Bestiary_HeaderSubtitle",
                "Indexed hostile archive for confirmed abyssal kills, tactical notes and extraction efficiency.");
            AbyssalSummoningConsoleArt.DrawHeader(rect, "ABY_Bestiary_Header".Translate(), subtitle, false);
        }

        private void DrawSummary(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(6f);
            List<AbyssalSummoningConsoleUtility.StatusEntry> entries = new List<AbyssalSummoningConsoleUtility.StatusEntry>
            {
                new AbyssalSummoningConsoleUtility.StatusEntry
                {
                    Label = "ABY_Bestiary_Summary_Discovered".Translate(),
                    Value = ABY_BestiaryUtility.GetUnlockedEntryCount() + " / " + ABY_BestiaryUtility.GetTrackedEntryCount(),
                    Satisfied = ABY_BestiaryUtility.GetUnlockedEntryCount() > 0
                },
                new AbyssalSummoningConsoleUtility.StatusEntry
                {
                    Label = "ABY_Bestiary_Summary_Studied".Translate(),
                    Value = ABY_BestiaryUtility.GetStudiedEntryCount() + " / " + ABY_BestiaryUtility.GetTrackedEntryCount(),
                    Satisfied = ABY_BestiaryUtility.GetStudiedEntryCount() > 0
                },
                new AbyssalSummoningConsoleUtility.StatusEntry
                {
                    Label = "ABY_Bestiary_Summary_Kills".Translate(),
                    Value = ABY_BestiaryUtility.GetTotalTrackedKills().ToString(),
                    Satisfied = ABY_BestiaryUtility.GetTotalTrackedKills() > 0
                },
                new AbyssalSummoningConsoleUtility.StatusEntry
                {
                    Label = "ABY_Bestiary_Summary_Extraction".Translate(),
                    Value = "+" + ABY_BestiaryRewardUtility.GetExtractionBonusPercent() + "%",
                    Satisfied = ABY_BestiaryRewardUtility.GetExtractionBonusPercent() > 0
                }
            };

            float width = inner.width / entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                Rect cellRect = new Rect(inner.x + width * i, inner.y, width - 4f, inner.height);
                AbyssalSummoningConsoleArt.DrawStripCell(cellRect, entries[i].Label, entries[i].Value, entries[i].Satisfied, i * 0.15f);
            }
        }


        private void DrawFilters(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            float rowHeight = 28f;
            Rect titleRect = new Rect(inner.x, inner.y + 4f, 152f, 20f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(titleRect, "ABY_Bestiary_FilterHeader".Translate());

            float controlsX = titleRect.xMax + 12f;
            float gap = 8f;
            float availableWidth = inner.xMax - controlsX;
            float statusWidth = 230f;
            float categoryWidth = 280f;
            float sortWidth = 190f;
            float resetWidth = Mathf.Max(118f, availableWidth - statusWidth - categoryWidth - sortWidth - gap * 3f);

            Rect statusRect = new Rect(controlsX, inner.y, statusWidth, rowHeight);
            if (DrawReadableStyledButton(statusRect, BuildCompactFilterLabel(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_StatusHeader", "Status"), GetStatusFilterLabel(statusFilterMode)), true, statusFilterMode != StatusFilterMode.Any))
            {
                ShowStatusFilterMenu();
            }

            Rect categoryRect = new Rect(statusRect.xMax + gap, inner.y, categoryWidth, rowHeight);
            if (DrawReadableStyledButton(categoryRect, BuildCompactFilterLabel(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_CategoryHeader", "Category"), GetCategoryFilterLabel(categoryFilterMode)), true, categoryFilterMode != CategoryFilterMode.Any))
            {
                ShowCategoryFilterMenu();
            }

            Rect sortRect = new Rect(categoryRect.xMax + gap, inner.y, sortWidth, rowHeight);
            if (DrawReadableStyledButton(sortRect, BuildCompactFilterLabel("ABY_Bestiary_SortHeader".Translate(), GetSortLabel(sortMode)), true, sortMode != SortMode.Threat))
            {
                ShowSortMenu();
            }

            bool resetActive = statusFilterMode != StatusFilterMode.Any || categoryFilterMode != CategoryFilterMode.Any || sortMode != SortMode.Threat;
            Rect resetRect = new Rect(sortRect.xMax + gap, inner.y, resetWidth, rowHeight);
            if (DrawReadableStyledButton(resetRect, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_ResetFilters", "Reset filters"), true, resetActive))
            {
                statusFilterMode = StatusFilterMode.Any;
                categoryFilterMode = CategoryFilterMode.Any;
                sortMode = SortMode.Threat;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
        }

        private string BuildCompactFilterLabel(string prefix, string value)
        {
            return prefix + ": " + value + " ▼";
        }


        private void DrawBrowser(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_Bestiary_BrowserHeader".Translate());

            List<ABY_BestiaryEntryDefinition> visibleEntries = GetVisibleEntries();
            EnsureSelectedEntry(visibleEntries);

            Rect outRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            float cardHeight = 118f;
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, outRect.width - 16f), Mathf.Max(outRect.height, visibleEntries.Count * (cardHeight + 6f)));
            Widgets.BeginScrollView(outRect, ref listScrollPosition, viewRect, true);
            for (int i = 0; i < visibleEntries.Count; i++)
            {
                DrawBrowserCard(new Rect(0f, i * (cardHeight + 6f), viewRect.width, cardHeight), visibleEntries[i]);
            }
            Widgets.EndScrollView();
        }

        private void DrawBrowserCard(Rect rect, ABY_BestiaryEntryDefinition entry)
        {
            bool selected = string.Equals(selectedEntryId, entry.EntryId, StringComparison.OrdinalIgnoreCase);
            bool unlocked = ABY_BestiaryUtility.IsUnlocked(entry.EntryId);
            bool studied = ABY_BestiaryUtility.IsStudied(entry.EntryId);
            AbyssalSummoningConsoleArt.DrawPanel(rect, selected);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlightIfMouseover(rect);
            }

            Rect portraitRect = new Rect(rect.x + 10f, rect.y + 10f, 72f, 72f);
            DrawPortrait(portraitRect, entry.EntryId, unlocked);

            string title = unlocked ? ABY_BestiaryUtility.GetEntryLabel(entry.EntryId) : ABY_BestiaryUtility.GetUnknownEntryLabel();
            string status = ABY_BestiaryUtility.GetStatusLabel(entry.EntryId);
            string category = ABY_BestiaryUtility.GetCategoryLabel(entry.EntryId);
            string tag = unlocked ? ABY_BestiaryUtility.GetTagline(entry.EntryId) : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_LockedTag", "Kill this hostile once to unseal the archive stub.");
            int kills = ABY_BestiaryUtility.GetKillCount(entry.EntryId);
            float contentX = rect.x + 94f;
            float rightColumnWidth = 54f;
            float textWidth = Mathf.Max(96f, rect.width - (contentX - rect.x) - rightColumnWidth - 14f);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(contentX, rect.y + 8f, textWidth, 22f), title);

            Text.Font = GameFont.Tiny;
            GUI.color = studied ? new Color(0.74f, 1f, 0.76f, 1f) : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(contentX, rect.y + 30f, textWidth, 16f), category + "  •  " + status);

            GUI.color = Color.white;
            Widgets.Label(new Rect(contentX, rect.y + 50f, textWidth, 32f), tag);

            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = selected ? Color.white : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.xMax - rightColumnWidth - 10f, rect.y + 10f, rightColumnWidth, 16f), "×" + kills);
            Text.Anchor = TextAnchor.UpperLeft;

            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(contentX, rect.y + 86f, rect.width - (contentX - rect.x) - 14f, 22f), ABY_BestiaryUtility.GetMilestoneSummary(entry.EntryId));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonInvisible(rect))
            {
                selectedEntryId = entry.EntryId;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
        }

        private void DrawDetails(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(10f);
            ABY_BestiaryEntryDefinition entry = ABY_BestiaryUtility.GetEntry(selectedEntryId);
            if (entry == null)
            {
                return;
            }

            bool unlocked = ABY_BestiaryUtility.IsUnlocked(entry.EntryId);
            bool tacticalUnlocked = ABY_BestiaryUtility.HasTacticalData(entry.EntryId);
            bool studied = ABY_BestiaryUtility.IsStudied(entry.EntryId);
            int kills = ABY_BestiaryUtility.GetKillCount(entry.EntryId);

            Rect headerRect = new Rect(inner.x, inner.y, inner.width, 140f);
            DrawDetailHeader(headerRect, entry, unlocked, kills);

            Rect outRect = new Rect(inner.x, headerRect.yMax + 8f, inner.width, inner.height - headerRect.height - 8f);
            float contentHeight = 600f;
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, outRect.width - 16f), contentHeight);
            Widgets.BeginScrollView(outRect, ref detailScrollPosition, viewRect, true);

            float y = 0f;
            y = DrawTextSection(new Rect(0f, y, viewRect.width, 92f), "ABY_Bestiary_Section_Summary".Translate(), unlocked ? ABY_BestiaryUtility.GetSummary(entry.EntryId) : "ABY_Bestiary_LockedSummary".Translate());
            y += 8f;
            y = DrawTextSection(new Rect(0f, y, viewRect.width, 110f), "ABY_Bestiary_Section_Tactical".Translate(), tacticalUnlocked ? ABY_BestiaryUtility.GetTacticalNote(entry.EntryId) : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_LockedTactical", "Field notes remain sealed until {0} confirmed kills.", ABY_BestiaryUtility.TacticalThreshold));
            y += 8f;
            y = DrawTextSection(new Rect(0f, y, viewRect.width, 140f), "ABY_Bestiary_Section_Deep".Translate(), studied ? ABY_BestiaryUtility.GetDeepRecord(entry.EntryId) : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_LockedDeep", "Deep archive record remains sealed until studied status at {0} confirmed kills.", ABY_BestiaryUtility.StudiedThreshold));
            y += 8f;
            y = DrawMilestonesSection(new Rect(0f, y, viewRect.width, 132f), entry.EntryId, kills, unlocked, tacticalUnlocked, studied);
            y += 8f;
            DrawExtractionSection(new Rect(0f, y, viewRect.width, 86f));

            Widgets.EndScrollView();
        }

        private void DrawDetailHeader(Rect rect, ABY_BestiaryEntryDefinition entry, bool unlocked, int kills)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect portraitRect = new Rect(rect.x + 12f, rect.y + 12f, 116f, 116f);
            DrawPortrait(portraitRect, entry.EntryId, unlocked);

            string title = unlocked ? ABY_BestiaryUtility.GetEntryLabel(entry.EntryId) : ABY_BestiaryUtility.GetUnknownEntryLabel();
            string category = ABY_BestiaryUtility.GetCategoryLabel(entry.EntryId);
            string status = ABY_BestiaryUtility.GetStatusLabel(entry.EntryId);
            string tag = unlocked ? ABY_BestiaryUtility.GetTagline(entry.EntryId) : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_LockedTag", "Kill this hostile once to unseal the archive stub.");

            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + 142f, rect.y + 10f, rect.width - 150f, 28f), title);
            Text.Font = GameFont.Small;
            GUI.color = new Color(1f, 0.84f, 0.70f, 1f);
            Widgets.Label(new Rect(rect.x + 142f, rect.y + 40f, rect.width - 150f, 22f), category + "  •  " + status);
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x + 142f, rect.y + 66f, rect.width - 160f, 40f), tag);
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x + 142f, rect.y + 108f, rect.width - 160f, 16f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Detail_Kills", "Confirmed kills: {0}", kills));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private float DrawTextSection(Rect rect, string title, string body)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 20f), title);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            Widgets.Label(new Rect(inner.x, inner.y + 24f, inner.width, inner.height - 24f), body);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            return rect.yMax;
        }

        private float DrawMilestonesSection(Rect rect, string entryId, int kills, bool unlocked, bool tacticalUnlocked, bool studied)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 20f), "ABY_Bestiary_Section_Milestones".Translate());
            Text.Font = GameFont.Tiny;
            DrawMilestoneLine(new Rect(inner.x, inner.y + 24f, inner.width, 18f), unlocked, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Milestone_Discover", "1 kill — archive reveal"));
            DrawMilestoneLine(new Rect(inner.x, inner.y + 44f, inner.width, 18f), tacticalUnlocked, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Milestone_Notes", "5 kills — field notes unlocked"));
            DrawMilestoneLine(new Rect(inner.x, inner.y + 64f, inner.width, 18f), studied, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Milestone_Study", "15 kills — studied status and deep record"));
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 90f, inner.width, 28f), ABY_BestiaryUtility.GetMilestoneSummary(entryId));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            return rect.yMax;
        }

        private void DrawMilestoneLine(Rect rect, bool active, string label)
        {
            GUI.color = active ? new Color(0.74f, 1f, 0.76f, 1f) : new Color(1f, 0.62f, 0.56f, 1f);
            Widgets.Label(rect, (active ? "● " : "○ ") + label);
            GUI.color = Color.white;
        }

        private void DrawExtractionSection(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(10f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 20f), "ABY_Bestiary_Section_Extraction".Translate());
            Text.Font = GameFont.Tiny;
            GUI.color = Color.white;
            Widgets.Label(new Rect(inner.x, inner.y + 24f, inner.width, 18f), ABY_BestiaryRewardUtility.GetSummaryLine());
            Widgets.Label(new Rect(inner.x, inner.y + 42f, inner.width, 28f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_ExtractionScope", "Applies only to Abyssal Protocol reward routing: horde caches, dominion payouts and supported boss-side residue/caches."));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawPortrait(Rect rect, string entryId, bool unlocked)
        {
            GUI.color = unlocked ? Color.white : new Color(0.34f, 0.34f, 0.34f, 1f);
            GUI.DrawTexture(rect, BaseContent.BlackTex);
            Texture2D texture = ABY_BestiaryUtility.GetPortrait(entryId);
            if (texture != null)
            {
                GUI.DrawTexture(rect.ContractedBy(4f), texture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.color = unlocked ? new Color(0.78f, 0.58f, 0.44f, 0.85f) : new Color(0.32f, 0.32f, 0.32f, 0.85f);
                GUI.DrawTexture(rect.ContractedBy(10f), BaseContent.WhiteTex);
            }
            GUI.color = unlocked ? new Color(1f, 0.42f, 0.16f, 0.75f) : new Color(0.44f, 0.44f, 0.44f, 0.75f);
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;
        }

        private List<ABY_BestiaryEntryDefinition> GetVisibleEntries()
        {
            IEnumerable<ABY_BestiaryEntryDefinition> query = ABY_BestiaryUtility.GetTrackedEntries();

            switch (statusFilterMode)
            {
                case StatusFilterMode.Locked:
                    query = query.Where(entry => !ABY_BestiaryUtility.IsUnlocked(entry.EntryId));
                    break;
                case StatusFilterMode.Discovered:
                    query = query.Where(entry => ABY_BestiaryUtility.IsUnlocked(entry.EntryId));
                    break;
                case StatusFilterMode.Studied:
                    query = query.Where(entry => ABY_BestiaryUtility.IsStudied(entry.EntryId));
                    break;
            }

            switch (categoryFilterMode)
            {
                case CategoryFilterMode.Assault:
                    query = query.Where(entry => entry.Category == ABY_BestiaryCategory.Assault);
                    break;
                case CategoryFilterMode.Elite:
                    query = query.Where(entry => entry.Category == ABY_BestiaryCategory.Elite);
                    break;
                case CategoryFilterMode.Support:
                    query = query.Where(entry => entry.Category == ABY_BestiaryCategory.Support);
                    break;
                case CategoryFilterMode.Boss:
                    query = query.Where(entry => entry.Category == ABY_BestiaryCategory.Boss);
                    break;
            }

            switch (sortMode)
            {
                case SortMode.Kills:
                    query = query.OrderByDescending(entry => ABY_BestiaryUtility.GetKillCount(entry.EntryId)).ThenBy(entry => ABY_BestiaryUtility.GetEntryLabel(entry.EntryId));
                    break;
                case SortMode.Name:
                    query = query.OrderBy(entry => ABY_BestiaryUtility.IsUnlocked(entry.EntryId) ? ABY_BestiaryUtility.GetEntryLabel(entry.EntryId) : ABY_BestiaryUtility.GetUnknownEntryLabel());
                    break;
                case SortMode.Recent:
                    query = query.OrderByDescending(entry => ABY_BestiaryUtility.GetProgress(entry.EntryId)?.lastKillTick ?? -1).ThenByDescending(entry => ABY_BestiaryUtility.GetKillCount(entry.EntryId));
                    break;
                default:
                    query = query.OrderBy(entry => (int)entry.Category).ThenBy(entry => ABY_BestiaryUtility.IsUnlocked(entry.EntryId) ? 0 : 1).ThenBy(entry => ABY_BestiaryUtility.GetEntryLabel(entry.EntryId));
                    break;
            }

            return query.ToList();
        }

        private void EnsureSelectedEntry(List<ABY_BestiaryEntryDefinition> visibleEntries)
        {
            if (visibleEntries == null || visibleEntries.Count == 0)
            {
                selectedEntryId = string.Empty;
                return;
            }

            if (selectedEntryId.NullOrEmpty() || !visibleEntries.Any(entry => string.Equals(entry.EntryId, selectedEntryId, StringComparison.OrdinalIgnoreCase)))
            {
                selectedEntryId = visibleEntries[0].EntryId;
            }
        }

        private string GetStatusFilterLabel(StatusFilterMode mode)
        {
            switch (mode)
            {
                case StatusFilterMode.Locked: return "ABY_Bestiary_Filter_Locked".Translate();
                case StatusFilterMode.Discovered: return "ABY_Bestiary_Filter_Discovered".Translate();
                case StatusFilterMode.Studied: return "ABY_Bestiary_Filter_Studied".Translate();
                default: return "ABY_Bestiary_Filter_All".Translate();
            }
        }

        private string GetCategoryFilterLabel(CategoryFilterMode mode)
        {
            switch (mode)
            {
                case CategoryFilterMode.Assault: return "ABY_Bestiary_Filter_Assault".Translate();
                case CategoryFilterMode.Elite: return "ABY_Bestiary_Filter_Elite".Translate();
                case CategoryFilterMode.Support: return "ABY_Bestiary_Filter_Support".Translate();
                case CategoryFilterMode.Boss: return "ABY_Bestiary_Filter_Boss".Translate();
                default: return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_Bestiary_Filter_AllCategories", "All categories");
            }
        }

        private string GetSortLabel(SortMode mode)
        {
            switch (mode)
            {
                case SortMode.Kills: return "ABY_Bestiary_Sort_Kills".Translate();
                case SortMode.Name: return "ABY_Bestiary_Sort_Name".Translate();
                case SortMode.Recent: return "ABY_Bestiary_Sort_Recent".Translate();
                default: return "ABY_Bestiary_Sort_Threat".Translate();
            }
        }

        private void ShowStatusFilterMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            AddStatusFilterOption(options, StatusFilterMode.Any);
            AddStatusFilterOption(options, StatusFilterMode.Locked);
            AddStatusFilterOption(options, StatusFilterMode.Discovered);
            AddStatusFilterOption(options, StatusFilterMode.Studied);
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void AddStatusFilterOption(List<FloatMenuOption> options, StatusFilterMode mode)
        {
            string label = GetStatusFilterLabel(mode);
            if (statusFilterMode == mode)
            {
                label += "  ✓";
            }

            options.Add(new FloatMenuOption(label, delegate
            {
                statusFilterMode = mode;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }));
        }

        private void ShowCategoryFilterMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            AddCategoryFilterOption(options, CategoryFilterMode.Any);
            AddCategoryFilterOption(options, CategoryFilterMode.Assault);
            AddCategoryFilterOption(options, CategoryFilterMode.Elite);
            AddCategoryFilterOption(options, CategoryFilterMode.Support);
            AddCategoryFilterOption(options, CategoryFilterMode.Boss);
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void AddCategoryFilterOption(List<FloatMenuOption> options, CategoryFilterMode mode)
        {
            string label = GetCategoryFilterLabel(mode);
            if (categoryFilterMode == mode)
            {
                label += "  ✓";
            }

            options.Add(new FloatMenuOption(label, delegate
            {
                categoryFilterMode = mode;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }));
        }

        private void ShowSortMenu()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            AddSortOption(options, SortMode.Threat);
            AddSortOption(options, SortMode.Kills);
            AddSortOption(options, SortMode.Name);
            AddSortOption(options, SortMode.Recent);
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void AddSortOption(List<FloatMenuOption> options, SortMode mode)
        {
            string label = GetSortLabel(mode);
            if (sortMode == mode)
            {
                label += "  ✓";
            }

            options.Add(new FloatMenuOption(label, delegate
            {
                sortMode = mode;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }));
        }

        private bool DrawReadableStyledButton(Rect rect, string label, bool enabled, bool active)
        {
            bool clicked = AbyssalStyledWidgets.TextButton(rect, string.Empty, enabled, active);

            Color oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, active ? 0.12f : 0.18f);
            GUI.DrawTexture(rect.ContractedBy(8f), BaseContent.WhiteTex);
            GUI.color = oldColor;

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldTextColor = GUI.color;

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            if (Text.CalcSize(label).x > rect.width - 18f)
            {
                Text.Font = GameFont.Tiny;
            }

            Rect labelRect = rect.ContractedBy(8f);
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            Widgets.Label(new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height), label);
            GUI.color = !enabled
                ? new Color(0.58f, 0.56f, 0.54f, 1f)
                : (active ? new Color(1f, 0.90f, 0.80f, 1f) : Color.white);
            Widgets.Label(labelRect, label);

            GUI.color = oldTextColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
            return clicked;
        }
    }
}
