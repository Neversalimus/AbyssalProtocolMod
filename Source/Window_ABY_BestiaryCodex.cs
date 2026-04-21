using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class Window_ABY_BestiaryCodex : Window
    {
        private readonly Building_AbyssalSummoningCircle sourceCircle;
        private Vector2 entryScrollPosition = Vector2.zero;
        private ABY_BestiaryCategory selectedCategory = ABY_BestiaryCategory.All;
        private string selectedEntryId = string.Empty;

        public Window_ABY_BestiaryCodex(Building_AbyssalSummoningCircle sourceCircle)
        {
            this.sourceCircle = sourceCircle;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
            draggable = true;
            forcePause = false;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = false;
            resizeable = false;
            selectedEntryId = ABY_BestiaryUtility.GetFirstAvailableEntryId(selectedCategory);
        }

        public override Vector2 InitialSize => new Vector2(1220f, 860f);

        public override void DoWindowContents(Rect inRect)
        {
            AbyssalSummoningConsoleArt.ReducedEffects = sourceCircle != null && sourceCircle.ReducedConsoleEffects;
            AbyssalSummoningConsoleArt.DrawBackground(inRect);

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 74f);
            Rect stripRect = new Rect(inRect.x, headerRect.yMax + 10f, inRect.width, 64f);
            Rect categoryRect = new Rect(inRect.x, stripRect.yMax + 10f, inRect.width, 34f);
            Rect browserRect = new Rect(inRect.x, categoryRect.yMax + 10f, 468f, inRect.height - categoryRect.yMax - 10f);
            Rect detailRect = new Rect(browserRect.xMax + 10f, categoryRect.yMax + 10f, inRect.width - browserRect.width - 10f, inRect.height - categoryRect.yMax - 10f);

            DrawHeader(headerRect);
            DrawSummaryStrip(stripRect);
            DrawCategoryTabs(categoryRect);
            DrawEntryBrowser(browserRect);
            DrawDetailPanel(detailRect);
        }

        private void DrawHeader(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawHeader(
                rect,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryTitle", "abyssal threat codex"),
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiarySubtitle", "Kill-confirmed hostile telemetry archived from summon encounters, breach cleanups, and boss remains."),
                false);
        }

        private void DrawSummaryStrip(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(6f);
            string[] labels =
            {
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiarySummary_Discovered", "Discovered"),
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiarySummary_Studied", "Studied"),
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiarySummary_Kills", "Confirmed kills"),
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiarySummary_Selected", "Selected entry")
            };
            string[] values =
            {
                ABY_BestiaryUtility.GetUnlockedEntryCount() + " / " + ABY_BestiaryUtility.GetTrackedEntryCount(),
                ABY_BestiaryUtility.GetStudiedEntryCount() + " / " + ABY_BestiaryUtility.GetTrackedEntryCount(),
                ABY_BestiaryUtility.GetTotalTrackedKills().ToString(),
                GetSelectedSummaryValue()
            };
            bool[] good =
            {
                ABY_BestiaryUtility.GetUnlockedEntryCount() > 0,
                ABY_BestiaryUtility.GetStudiedEntryCount() > 0,
                ABY_BestiaryUtility.GetTotalTrackedKills() > 0,
                !values[3].NullOrEmpty()
            };

            float width = inner.width / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                Rect cellRect = new Rect(inner.x + width * i, inner.y, width - 4f, inner.height);
                AbyssalSummoningConsoleArt.DrawStripCell(cellRect, labels[i], values[i], good[i], i * 0.19f);
            }
        }

        private void DrawCategoryTabs(Rect rect)
        {
            ABY_BestiaryCategory[] categories =
            {
                ABY_BestiaryCategory.All,
                ABY_BestiaryCategory.Assault,
                ABY_BestiaryCategory.Support,
                ABY_BestiaryCategory.Elite,
                ABY_BestiaryCategory.Boss
            };

            float gap = 6f;
            float width = (rect.width - gap * (categories.Length - 1)) / categories.Length;
            for (int i = 0; i < categories.Length; i++)
            {
                ABY_BestiaryCategory category = categories[i];
                Rect tabRect = new Rect(rect.x + i * (width + gap), rect.y, width, rect.height);
                string label = ABY_BestiaryUtility.GetCategoryLabel(category) + " (" + ABY_BestiaryUtility.GetCategoryEntryCount(category) + ")";
                if (AbyssalStyledWidgets.TabButton(tabRect, label, null, selectedCategory == category, true))
                {
                    selectedCategory = category;
                    EnsureValidSelection();
                }
            }
        }

        private void DrawEntryBrowser(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryBrowserHeader", "Archived hostile patterns"));

            List<ABY_BestiaryEntryDefinition> definitions = GetFilteredEntries();
            EnsureValidSelection(definitions);

            Rect outRect = new Rect(inner.x, inner.y + 30f, inner.width, inner.height - 30f);
            float cardHeight = 104f;
            float viewHeight = Mathf.Max(outRect.height, definitions.Count * (cardHeight + 8f));
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, outRect.width - 16f), viewHeight);

            Widgets.BeginScrollView(outRect, ref entryScrollPosition, viewRect, true);
            for (int i = 0; i < definitions.Count; i++)
            {
                Rect cardRect = new Rect(0f, i * (cardHeight + 8f), viewRect.width, cardHeight);
                DrawEntryCard(cardRect, definitions[i], definitions[i].EntryId == selectedEntryId);
            }
            Widgets.EndScrollView();
        }

        private void DrawEntryCard(Rect rect, ABY_BestiaryEntryDefinition definition, bool selected)
        {
            bool unlocked = ABY_BestiaryUtility.IsUnlocked(definition.EntryId);
            bool tacticalUnlocked = ABY_BestiaryUtility.IsTacticalNoteUnlocked(definition.EntryId);
            bool studied = ABY_BestiaryUtility.IsStudied(definition.EntryId);
            int kills = ABY_BestiaryUtility.GetKillCount(definition.EntryId);

            AbyssalSummoningConsoleArt.DrawPanel(rect, selected || unlocked);
            AbyssalSummoningConsoleArt.DrawRitualCardPulse(rect, selected, studied);

            Rect portraitRect = new Rect(rect.x + 10f, rect.y + 10f, 62f, 62f);
            DrawPortrait(portraitRect, definition.EntryId, unlocked);

            string title = unlocked
                ? ABY_BestiaryUtility.GetDisplayLabel(definition.EntryId)
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryUnknownName", "Unknown hostile pattern");
            string status = ABY_BestiaryUtility.GetArchiveStateLabel(definition.EntryId);
            string meta = unlocked
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCardMeta", "Kills: {0}   •   {1}", kills, ABY_BestiaryUtility.GetCategoryLabel(definition.Category))
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCardLockedMeta", "First confirmed kill required.");
            string tags = unlocked
                ? ABY_BestiaryUtility.GetTagLine(definition.EntryId)
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCardTagLocked", "Telemetry band unknown.");
            string progress = studied
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCardProgress_Studied", "Deep record unlocked.")
                : tacticalUnlocked
                    ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCardProgress_StudiedPending", "Deep record at {0}/{1} kills.", kills, ABY_BestiaryUtility.StudiedKillThreshold)
                    : unlocked
                        ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCardProgress_TacticalPending", "Tactical note at {0}/{1} kills.", kills, ABY_BestiaryUtility.TacticalKillThreshold)
                        : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCardProgress_Locked", "First confirmed kill required.");

            Rect titleRect = new Rect(rect.x + 84f, rect.y + 8f, rect.width - 96f, 22f);
            Rect statusRect = new Rect(rect.x + 84f, rect.y + 30f, rect.width - 96f, 14f);
            Rect metaRect = new Rect(rect.x + 84f, rect.y + 46f, rect.width - 96f, 14f);
            Rect tagRect = new Rect(rect.x + 84f, rect.y + 62f, rect.width - 96f, 14f);
            Rect progressRect = new Rect(rect.x + 84f, rect.y + 78f, rect.width - 96f, 14f);

            Widgets.Label(titleRect, title);

            Text.Font = GameFont.Tiny;
            GUI.color = studied
                ? new Color(0.86f, 1f, 0.76f, 1f)
                : unlocked
                    ? AbyssalSummoningConsoleArt.TextDimColor
                    : new Color(0.70f, 0.70f, 0.70f, 0.92f);
            Widgets.Label(statusRect, status);
            GUI.color = unlocked ? new Color(1f, 0.76f, 0.58f, 1f) : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(metaRect, meta);
            GUI.color = unlocked ? new Color(0.78f, 0.90f, 1f, 1f) : new Color(0.58f, 0.58f, 0.58f, 0.92f);
            Widgets.Label(tagRect, tags);
            GUI.color = studied
                ? new Color(0.80f, 1f, 0.80f, 1f)
                : tacticalUnlocked
                    ? new Color(1f, 0.86f, 0.60f, 1f)
                    : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(progressRect, progress);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(rect))
            {
                selectedEntryId = definition.EntryId;
            }
        }

        private void DrawDetailPanel(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(12f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryDetailHeader", "Pattern telemetry"));

            ABY_BestiaryEntryDefinition definition = ABY_BestiaryUtility.GetDefinition(selectedEntryId);
            if (definition == null)
            {
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                Widgets.Label(new Rect(inner.x, inner.y + 32f, inner.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryNoSelection", "No hostile pattern is currently selected."));
                GUI.color = Color.white;
                return;
            }

            bool unlocked = ABY_BestiaryUtility.IsUnlocked(definition.EntryId);
            bool tacticalUnlocked = ABY_BestiaryUtility.IsTacticalNoteUnlocked(definition.EntryId);
            bool studied = ABY_BestiaryUtility.IsStudied(definition.EntryId);
            int kills = ABY_BestiaryUtility.GetKillCount(definition.EntryId);
            ABY_BestiaryEntryProgress progress = ABY_BestiaryUtility.GetProgress(definition.EntryId);

            Rect portraitRect = new Rect(inner.x, inner.y + 36f, 228f, 228f);
            Rect titleRect = new Rect(portraitRect.xMax + 16f, inner.y + 36f, inner.width - portraitRect.width - 16f, 28f);
            Rect tagRect = new Rect(portraitRect.xMax + 16f, inner.y + 66f, inner.width - portraitRect.width - 16f, 18f);
            Rect summaryHeaderRect = new Rect(portraitRect.xMax + 16f, inner.y + 94f, inner.width - portraitRect.width - 16f, 18f);
            Rect summaryBodyRect = new Rect(portraitRect.xMax + 16f, inner.y + 114f, inner.width - portraitRect.width - 16f, 80f);
            Rect tacticalHeaderRect = new Rect(portraitRect.xMax + 16f, inner.y + 198f, inner.width - portraitRect.width - 16f, 18f);
            Rect tacticalBodyRect = new Rect(portraitRect.xMax + 16f, inner.y + 218f, inner.width - portraitRect.width - 16f, 46f);
            Rect progressRect = new Rect(inner.x, portraitRect.yMax + 20f, inner.width, 136f);
            Rect footerRect = new Rect(inner.x, progressRect.yMax + 12f, inner.width, inner.height - (progressRect.yMax + 12f - inner.y));

            DrawPortrait(portraitRect, definition.EntryId, unlocked, true);

            string title = unlocked
                ? ABY_BestiaryUtility.GetDisplayLabel(definition.EntryId)
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryUnknownName", "Unknown hostile pattern");
            Widgets.Label(titleRect, title);

            Text.Font = GameFont.Tiny;
            GUI.color = unlocked ? new Color(0.82f, 0.92f, 1f, 1f) : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(tagRect, unlocked ? ABY_BestiaryUtility.GetTagLine(definition.EntryId) : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryCardTagLocked", "Telemetry band unknown."));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            DrawBodySection(summaryHeaderRect, summaryBodyRect,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiarySection_Summary", "Pattern summary"),
                unlocked
                    ? ABY_BestiaryUtility.GetSummaryText(definition.EntryId)
                    : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiarySummaryLocked", "This hostile pattern is indexed by the codex, but no reliable corpse telemetry has been recovered on the current save yet."),
                unlocked);

            DrawBodySection(tacticalHeaderRect, tacticalBodyRect,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiarySection_Tactical", "Tactical note"),
                tacticalUnlocked
                    ? ABY_BestiaryUtility.GetTacticalText(definition.EntryId)
                    : unlocked
                        ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryTacticalLocked", "Additional tactical annotation unlocks at {0} confirmed kills. Current progress: {1} / {0}.", ABY_BestiaryUtility.TacticalKillThreshold, kills)
                        : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryTacticalLockedNoReveal", "Recover the first confirmed kill to begin tactical annotation for this entry."),
                tacticalUnlocked);

            DrawProgressPanel(progressRect, kills, unlocked, tacticalUnlocked, studied);
            DrawFooterPanel(footerRect, definition, unlocked, tacticalUnlocked, studied, kills, progress);
        }

        private void DrawBodySection(Rect headerRect, Rect bodyRect, string header, string body, bool highlighted)
        {
            AbyssalSummoningConsoleArt.DrawSectionTitle(headerRect, header);
            Text.Font = GameFont.Tiny;
            GUI.color = highlighted ? Color.white : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(bodyRect, body);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawProgressPanel(Rect rect, int kills, bool unlocked, bool tacticalUnlocked, bool studied)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 20f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryProgressHeader", "Archive milestones"));

            string discoveryLine = unlocked
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryProgressDiscoveryDone", "Discovery confirmed — first kill archived.")
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryProgressDiscoveryPending", "Discovery pending — first confirmed kill required.");
            string tacticalLine = tacticalUnlocked
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryProgressTacticalDone", "Tactical note unlocked — field annotation restored.")
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryProgressTacticalPending", "Tactical note threshold: {0} / {1} confirmed kills.", kills, ABY_BestiaryUtility.TacticalKillThreshold);
            string studyLine = studied
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryProgressStudyDone", "Study threshold reached — telemetry stabilized.")
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryProgressStudyPending", "Study threshold: {0} / {1} confirmed kills.", kills, ABY_BestiaryUtility.StudiedKillThreshold);

            Text.Font = GameFont.Tiny;
            GUI.color = unlocked ? new Color(0.78f, 1f, 0.78f, 1f) : new Color(1f, 0.64f, 0.60f, 1f);
            Widgets.Label(new Rect(inner.x, inner.y + 24f, inner.width, 16f), discoveryLine);
            GUI.color = tacticalUnlocked ? new Color(1f, 0.86f, 0.60f, 1f) : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 42f, inner.width, 16f), tacticalLine);
            GUI.color = studied ? new Color(0.78f, 1f, 0.78f, 1f) : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 60f, inner.width, 16f), studyLine);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            DrawKillProgressBar(new Rect(inner.x, inner.y + 88f, inner.width, 24f), kills, ABY_BestiaryUtility.StudiedKillThreshold);
        }

        private void DrawKillProgressBar(Rect rect, int value, int threshold)
        {
            AbyssalSummoningConsoleArt.Fill(rect, new Color(0.05f, 0.04f, 0.045f, 1f));
            float fillPercent = threshold <= 0 ? 1f : Mathf.Clamp01((float)value / threshold);
            Rect fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * fillPercent, rect.height - 4f);
            AbyssalSummoningConsoleArt.Fill(fillRect, new Color(1f, 0.34f, 0.16f, 0.88f));
            AbyssalSummoningConsoleArt.DrawOutline(rect, new Color(1f, 0.40f, 0.20f, 0.76f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, value + " / " + threshold);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawFooterPanel(Rect rect, ABY_BestiaryEntryDefinition definition, bool unlocked, bool tacticalUnlocked, bool studied, int kills, ABY_BestiaryEntryProgress progress)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 20f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiarySection_DeepRecord", "Deep codex record"));

            string firstSeen = progress != null && progress.firstUnlockTick >= 0
                ? progress.firstUnlockTick.ToStringTicksToPeriod()
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryTelemetry_Unknown", "unknown");
            string lastSeen = progress != null && progress.lastKillTick >= 0
                ? progress.lastKillTick.ToStringTicksToPeriod()
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryTelemetry_Unknown", "unknown");
            string note = studied
                ? ABY_BestiaryUtility.GetStudiedText(definition.EntryId)
                : tacticalUnlocked
                    ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryStudiedLocked", "Deep record unlocks at {0} confirmed kills. Current progress: {1} / {0}.", ABY_BestiaryUtility.StudiedKillThreshold, kills)
                    : unlocked
                        ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryStudiedLockedEarly", "Stabilize the archive first. Tactical notes unlock at {0} kills and the deep record unlocks at {1}.", ABY_BestiaryUtility.TacticalKillThreshold, ABY_BestiaryUtility.StudiedKillThreshold)
                        : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryFooterLocked", "No reliable telemetry is available yet. Recover one confirmed kill to reveal the pattern and start codex tracking.");

            string footerText = AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_BestiaryFooterStatusBlock",
                "Archive state: {0}\nConfirmed kills: {1}\nFirst archive event: {2}\nMost recent archive event: {3}\n\n{4}",
                ABY_BestiaryUtility.GetArchiveStateLabel(definition.EntryId),
                kills,
                firstSeen,
                lastSeen,
                note);

            Text.Font = GameFont.Tiny;
            GUI.color = studied ? Color.white : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 24f, inner.width, rect.height - 28f), footerText);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private string GetSelectedSummaryValue()
        {
            ABY_BestiaryEntryDefinition definition = ABY_BestiaryUtility.GetDefinition(selectedEntryId);
            if (definition == null)
            {
                return string.Empty;
            }

            string label = ABY_BestiaryUtility.IsUnlocked(definition.EntryId)
                ? ABY_BestiaryUtility.GetDisplayLabel(definition.EntryId)
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_BestiaryUnknownName", "Unknown hostile pattern");
            return Shorten(label + " • " + ABY_BestiaryUtility.GetArchiveStateLabel(definition.EntryId), 40);
        }

        private static string Shorten(string text, int maxLength)
        {
            if (text.NullOrEmpty() || text.Length <= maxLength)
            {
                return text ?? string.Empty;
            }

            return text.Substring(0, Mathf.Max(0, maxLength - 1)).TrimEnd() + "…";
        }

        private List<ABY_BestiaryEntryDefinition> GetFilteredEntries()
        {
            return ABY_BestiaryUtility.GetEntriesForCategory(selectedCategory).Where(definition => definition != null).ToList();
        }

        private void EnsureValidSelection()
        {
            EnsureValidSelection(GetFilteredEntries());
        }

        private void EnsureValidSelection(List<ABY_BestiaryEntryDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0)
            {
                selectedEntryId = string.Empty;
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] != null && definitions[i].EntryId == selectedEntryId)
                {
                    return;
                }
            }

            selectedEntryId = definitions[0].EntryId;
        }

        private void DrawPortrait(Rect rect, string entryId, bool unlocked, bool large = false)
        {
            Texture2D portrait = ABY_BestiaryUtility.GetPortrait(entryId);
            AbyssalSummoningConsoleArt.Fill(rect, new Color(0.07f, 0.05f, 0.055f, 1f));
            AbyssalSummoningConsoleArt.DrawOutline(rect, new Color(1f, 0.34f, 0.14f, 0.34f));
            if (portrait != null)
            {
                Color oldColor = GUI.color;
                GUI.color = unlocked ? Color.white : new Color(0.42f, 0.42f, 0.42f, 0.96f);
                GUI.DrawTexture(rect.ContractedBy(large ? 12f : 6f), portrait, ScaleMode.ScaleToFit, true);
                GUI.color = oldColor;
            }

            if (!unlocked)
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.34f);
                GUI.DrawTexture(rect, BaseContent.WhiteTex);
                GUI.color = oldColor;
            }
        }
    }
}
