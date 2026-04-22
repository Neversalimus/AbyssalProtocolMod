using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class Window_AbyssalSummoningConsole : Window
    {
        private readonly Building_AbyssalSummoningCircle circle;
        private string selectedRitualId;
        private Vector2 ritualScrollPosition = Vector2.zero;
        private Vector2 capacitorScrollPosition = Vector2.zero;
        private Vector2 stabilizerScrollPosition = Vector2.zero;
        private Vector2 ritualPreviewScrollPosition = Vector2.zero;
        private Vector2 statusScrollPosition = Vector2.zero;

        public Window_AbyssalSummoningConsole(Building_AbyssalSummoningCircle circle)
        {
            this.circle = circle;
            selectedRitualId = AbyssalSummoningConsoleUtility.GetSuggestedRitual(circle)?.Id;
            MapComponent_DominionCrisis dominionCrisis = circle?.Map?.GetComponent<MapComponent_DominionCrisis>();
            if (dominionCrisis != null && !dominionCrisis.IsActive && (dominionCrisis.CompletionCount > 0 || dominionCrisis.FailureCount > 0 || dominionCrisis.CancelledCount > 0 || dominionCrisis.CooldownTicksRemaining > 0))
            {
                AbyssalSummoningConsoleUtility.RitualDefinition dominionRitual = AbyssalSummoningConsoleUtility.GetRitualsForCircle(circle).FirstOrDefault(r => AbyssalSummoningConsoleUtility.IsDominionRitual(r));
                if (dominionRitual != null)
                {
                    selectedRitualId = dominionRitual.Id;
                }
            }
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
            draggable = true;
            forcePause = false;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = false;
            resizeable = false;
        }

        public override Vector2 InitialSize => new Vector2(1360f, 910f);

        public override void DoWindowContents(Rect inRect)
        {
            if (circle == null || circle.Destroyed || circle.Map == null)
            {
                Close();
                return;
            }

            AbyssalSummoningConsoleArt.ReducedEffects = circle.ReducedConsoleEffects;
            AbyssalSummoningConsoleArt.DrawBackground(inRect);

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 74f);
            Rect stripRect = new Rect(inRect.x, headerRect.yMax + 10f, inRect.width, 64f);
            Rect ritualsRect = new Rect(inRect.x, stripRect.yMax + 10f, 556f, 420f);
            Rect overviewRect = new Rect(ritualsRect.xMax + 10f, stripRect.yMax + 10f, inRect.width - ritualsRect.width - 10f, 420f);
            Rect systemsRect = new Rect(inRect.x, ritualsRect.yMax + 10f, inRect.width, inRect.height - ritualsRect.yMax - 10f);

            AbyssalSummoningConsoleUtility.RitualDefinition ritual = GetSelectedRitual();

            float controlWidth = Mathf.Min(316f, overviewRect.width * 0.36f);
            Rect controlRect = new Rect(overviewRect.x, overviewRect.y, controlWidth, overviewRect.height);
            Rect previewRect = new Rect(controlRect.xMax + 10f, overviewRect.y, overviewRect.width - controlWidth - 10f, overviewRect.height);

            DrawHeader(headerRect, ritual);
            DrawReadinessStrip(stripRect, ritual);
            DrawRitualBrowser(ritualsRect, ritual);
            DrawControlPanel(controlRect, ritual);
            DrawScrollableRitualPreviewPanel(previewRect, ritual);
            DrawSystemsPanel(systemsRect, ritual);
        }

        private AbyssalSummoningConsoleUtility.RitualDefinition GetSelectedRitual()
        {
            AbyssalSummoningConsoleUtility.RitualDefinition ritual = AbyssalSummoningConsoleUtility.GetRitualsForCircle(circle).FirstOrDefault(r => r.Id == selectedRitualId);
            if (ritual == null)
            {
                ritual = AbyssalSummoningConsoleUtility.GetDefaultRitual();
                selectedRitualId = ritual.Id;
            }
            return ritual;
        }

        private void DrawHeader(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            MapComponent_DominionCrisis dominionCrisis = circle.Map?.GetComponent<MapComponent_DominionCrisis>();
            bool dominionActive = dominionCrisis != null && dominionCrisis.IsActive;
            string subtitle = circle.RitualActive
                ? AbyssalSummoningConsoleUtility.GetConsoleSubtitleActive(circle.GetCurrentPhaseTranslated())
                : dominionActive
                    ? AbyssalSummoningConsoleUtility.GetConsoleSubtitleDominionActive(dominionCrisis.GetPhaseLabel())
                    : AbyssalSummoningConsoleUtility.GetConsoleSubtitle();
            AbyssalSummoningConsoleArt.DrawHeader(rect, AbyssalSummoningConsoleUtility.GetConsoleTitle(), subtitle, circle.RitualActive || dominionActive);
        }

        private void DrawReadinessStrip(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(6f);
            List<AbyssalSummoningConsoleUtility.StatusEntry> entries = AbyssalSummoningConsoleUtility.GetStatusEntries(circle, ritual);
            float width = inner.width / entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                Rect cellRect = new Rect(inner.x + width * i, inner.y, width - 4f, inner.height);
                AbyssalSummoningConsoleArt.DrawStripCell(cellRect, entries[i].Label, entries[i].Value, entries[i].Satisfied, i * 0.17f);
            }
        }

        private void DrawRitualBrowser(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition selected)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_CirclePatternsHeader".Translate());

            List<AbyssalSummoningConsoleUtility.RitualDefinition> rituals = AbyssalSummoningConsoleUtility.GetRitualsForCircle(circle).ToList();
            Rect outRect = new Rect(inner.x, inner.y + 30f, inner.width, inner.height - 30f);
            float cardHeight = 110f;
            float viewHeight = Mathf.Max(outRect.height, rituals.Count * (cardHeight + 8f));
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(outRect, ref ritualScrollPosition, viewRect, true);
            for (int i = 0; i < rituals.Count; i++)
            {
                AbyssalSummoningConsoleUtility.RitualDefinition ritual = rituals[i];
                Rect cardRect = new Rect(0f, i * (cardHeight + 8f), viewRect.width, cardHeight);
                DrawRitualCard(cardRect, ritual, ritual.Id == selected.Id);
            }
            Widgets.EndScrollView();
        }


        private void DrawRitualCard(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual, bool selected)
        {
            bool ready = circle.IsReadyForSigil(out _);
            AbyssalSummoningConsoleArt.DrawPanel(rect, selected || ready);
            AbyssalSummoningConsoleArt.DrawRitualCardPulse(rect, selected, circle.RitualActive);

            Rect iconRect = new Rect(rect.x + 12f, rect.y + 14f, 42f, 42f);
            ThingDef sigilDef = AbyssalSummoningConsoleUtility.GetSigilDef(ritual);
            if (sigilDef != null && sigilDef.uiIcon != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(iconRect, sigilDef.uiIcon, ScaleMode.ScaleToFit, true);
            }

            float contentX = rect.x + 66f;
            float buttonWidth = 104f;
            float rightInset = 12f;
            float textWidth = Mathf.Max(120f, rect.width - (contentX - rect.x) - buttonWidth - rightInset - 10f);

            Rect titleRect = new Rect(contentX, rect.y + 10f, textWidth, 22f);
            Rect tagRect = new Rect(contentX, rect.y + 32f, textWidth, 16f);
            Rect metaRect = new Rect(contentX, rect.y + 54f, textWidth, 34f);
            Rect selectRect = new Rect(rect.xMax - buttonWidth - 12f, rect.y + rect.height - 38f, buttonWidth, 30f);

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Widgets.Label(titleRect, AbyssalSummoningConsoleUtility.GetRitualLabel(ritual));

            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Text.Font = GameFont.Tiny;
            Widgets.Label(tagRect, AbyssalSummoningConsoleUtility.GetRoleTagLine(ritual));
            GUI.color = new Color(1f, 0.76f, 0.58f, 1f);
            Widgets.Label(metaRect, FormatRitualMetaForCard(AbyssalSummoningConsoleUtility.GetRitualMetaText(circle, ritual)));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (AbyssalStyledWidgets.TextButton(selectRect, selected ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleSelected", "Selected") : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleSelect", "Select"), !selected, selected))
            {
                selectedRitualId = ritual.Id;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
        }

        private string FormatRitualMetaForCard(string meta)
        {
            if (meta.NullOrEmpty())
            {
                return string.Empty;
            }

            string[] separators = new[] { "•" };
            List<string> parts = meta.Split(separators, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !part.NullOrEmpty())
                .ToList();

            if (parts.Count <= 2)
            {
                return string.Join("   •   ", parts.ToArray());
            }

            int firstLineCount = Mathf.CeilToInt(parts.Count / 2f);
            string firstLine = string.Join("   •   ", parts.Take(firstLineCount).ToArray());
            string secondLine = string.Join("   •   ", parts.Skip(firstLineCount).ToArray());
            return secondLine.NullOrEmpty() ? firstLine : firstLine + "\n" + secondLine;
        }

        private void DrawControlPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(12f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_CircleControlHeader".Translate());

            AbyssalSummoningConsoleUtility.CircleRiskTier riskTier = AbyssalSummoningConsoleUtility.GetRiskTier(circle, ritual);
            AbyssalSummoningConsoleArt.DrawRiskBar(new Rect(inner.x, inner.y + 28f, inner.width, 28f), AbyssalSummoningConsoleUtility.GetRiskFill(circle, ritual), AbyssalSummoningConsoleUtility.GetRiskLabel(riskTier), AbyssalSummoningConsoleUtility.GetRiskColor(riskTier), circle.RitualActive);

            MapComponent_DominionCrisis dominionCrisis = circle.Map?.GetComponent<MapComponent_DominionCrisis>();
            bool dominionUiMode = AbyssalSummoningConsoleUtility.IsDominionUiMode(circle, ritual);

            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 64f, inner.width, 18f), "ABY_CircleControlState".Translate());
            GUI.color = Color.white;
            string statusLine = dominionCrisis != null && dominionCrisis.IsActive
                ? dominionCrisis.GetStatusLine()
                : circle.GetCurrentStatusLine();
            Widgets.Label(new Rect(inner.x, inner.y + 82f, inner.width, 26f), statusLine);

            string blockerLine = AbyssalSummoningConsoleUtility.IsInvocationPathClear(circle, ritual)
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleCommandReady", "Invocation path clear. Prepared sigil, operator, and circle state are valid.")
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleCommandBlocked", "Current blocker: {0}", AbyssalSummoningConsoleUtility.GetPrimaryInvocationBlocker(circle, ritual));

            GUI.color = AbyssalSummoningConsoleUtility.IsInvocationPathClear(circle, ritual)
                ? new Color(0.72f, 1f, 0.74f, 1f)
                : new Color(1f, 0.60f, 0.54f, 1f);
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(inner.x, inner.y + 108f, inner.width, 30f), blockerLine);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            float rowY = inner.y + 140f;
            DrawBooleanControlRow(new Rect(inner.x, rowY, inner.width, 30f),
                "ABY_CircleReducedEffects".Translate(),
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleReducedEffectsDesc", "Softens header sweeps, seal rotation, and other animated accents inside the summoning console."),
                circle.ReducedConsoleEffects,
                delegate(bool value)
                {
                    circle.SetReducedConsoleEffects(value);
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                });

            rowY += 34f;
            DrawBooleanControlRow(new Rect(inner.x, rowY, inner.width, 30f),
                "ABY_CapacitorControl_Overchannel".Translate(),
                "ABY_CapacitorControl_OverchannelDesc".Translate(),
                circle.CapacitorOverchannelEnabled,
                delegate(bool value)
                {
                    circle.SetCapacitorOverchannelEnabled(value);
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                });

            rowY += 34f;
            DrawBooleanControlRow(new Rect(inner.x, rowY, inner.width, 30f),
                "ABY_CapacitorControl_Dump".Translate(),
                "ABY_CapacitorControl_DumpDesc".Translate(),
                circle.CapacitorEmergencyDumpEnabled,
                delegate(bool value)
                {
                    circle.SetCapacitorEmergencyDumpEnabled(value);
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                });

            Rect invokeRect = new Rect(inner.x, inner.yMax - 40f, inner.width, 34f);
            if (dominionUiMode)
            {
                bool pocketControls = AbyssalSummoningConsoleUtility.HasDominionPocketControls(circle);
                if (pocketControls)
                {
                    float thirdWidth = (inner.width - 12f) / 3f;
                    Rect codexRect = new Rect(inner.x, inner.yMax - 78f, thirdWidth, 30f);
                    Rect objectiveRect = new Rect(codexRect.xMax + 6f, inner.yMax - 78f, thirdWidth, 30f);
                    Rect gateRect = new Rect(objectiveRect.xMax + 6f, inner.yMax - 78f, thirdWidth, 30f);
                    if (AbyssalStyledWidgets.TextButton(codexRect, "ABY_Bestiary_OpenCodex".Translate()))
                    {
                        OpenThreatCodex();
                    }

                    if (AbyssalStyledWidgets.TextButton(objectiveRect, AbyssalSummoningConsoleUtility.GetDominionObjectiveButtonLabel(circle), dominionCrisis != null, false))
                    {
                        JumpToDominionObjective();
                    }

                    if (AbyssalStyledWidgets.TextButton(gateRect, AbyssalSummoningConsoleUtility.GetDominionPocketPrimaryLabel(circle), true, true))
                    {
                        if (AbyssalSummoningConsoleUtility.TryExecuteDominionPocketPrimary(circle, out string failReason))
                        {
                            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                        }
                        else if (!failReason.NullOrEmpty())
                        {
                            Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                }
                else
                {
                    float halfWidth = (inner.width - 6f) * 0.5f;
                    Rect codexRect = new Rect(inner.x, inner.yMax - 78f, halfWidth, 30f);
                    Rect objectiveRect = new Rect(codexRect.xMax + 6f, inner.yMax - 78f, halfWidth, 30f);
                    if (AbyssalStyledWidgets.TextButton(codexRect, "ABY_Bestiary_OpenCodex".Translate()))
                    {
                        OpenThreatCodex();
                    }

                    if (AbyssalStyledWidgets.TextButton(objectiveRect, AbyssalSummoningConsoleUtility.GetDominionObjectiveButtonLabel(circle), dominionCrisis != null, false))
                    {
                        JumpToDominionObjective();
                    }
                }
            }
            else
            {
                Rect openRect = new Rect(inner.x, inner.yMax - 78f, inner.width, 30f);
                if (AbyssalStyledWidgets.TextButton(openRect, "ABY_Bestiary_OpenCodex".Translate()))
                {
                    OpenThreatCodex();
                }
            }

            bool dominionAbortMode = AbyssalSummoningConsoleUtility.IsDominionRitual(ritual) && dominionCrisis != null && dominionCrisis.IsActive;
            string invokeLabel = dominionAbortMode
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionCrisisAbortCommand", "Abort dominion staging")
                : circle.CapacitorOverchannelEnabled && AbyssalCircleCapacitorRitualUtility.WouldForceStart(circle, ritual)
                    ? "ABY_CapacitorCommand_ForceInvoke".Translate()
                    : AbyssalSummoningConsoleUtility.GetAssignSigilLabel();
            bool invokeEnabled = dominionAbortMode || !circle.RitualActive;
            if (AbyssalStyledWidgets.TextButton(invokeRect, invokeLabel, invokeEnabled, true))
            {
                ConfirmAndAssign(ritual);
            }
        }

        private void DrawBooleanControlRow(Rect rect, string label, string tooltip, bool state, System.Action<bool> setter)
        {
            Rect labelRect = new Rect(rect.x, rect.y + 4f, rect.width - 42f, rect.height - 8f);
            Rect checkboxRect = new Rect(rect.xMax - 24f, rect.y + Mathf.Max(0f, (rect.height - 24f) * 0.5f), 24f, 24f);

            GUI.color = state ? Color.white : AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(labelRect, label);
            GUI.color = Color.white;

            bool newState = state;
            Widgets.CheckboxLabeled(new Rect(checkboxRect.x, checkboxRect.y, 24f, 24f), string.Empty, ref newState, false, null, null, false);
            if (newState != state)
            {
                setter(newState);
            }

            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        private void DrawStatusPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(12f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_CircleStatusHeaderLong".Translate());

            List<AbyssalSummoningConsoleUtility.StatusEntry> entries = AbyssalSummoningConsoleUtility.GetStatusEntries(circle, ritual);
            List<string> difficultyLines = AbyssalDifficultyUtility.GetDiagnosticsLines();
            bool dominionUiMode = AbyssalSummoningConsoleUtility.IsDominionUiMode(circle, ritual);
            float summaryHeight = dominionUiMode ? 54f : 0f;
            float difficultyHeight = difficultyLines.Count * 18f + (difficultyLines.Count > 0 ? 10f : 0f);
            float contentHeight = 8f + summaryHeight + difficultyHeight + entries.Count * 22f;
            Rect outRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, outRect.width - 16f), contentHeight);

            Widgets.BeginScrollView(outRect, ref statusScrollPosition, viewRect, true);
            float y = 0f;
            if (dominionUiMode)
            {
                GUI.color = new Color(0.19f, 0.08f, 0.07f, 0.78f);
                GUI.DrawTexture(new Rect(0f, 0f, viewRect.width, 50f), BaseContent.WhiteTex);
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                Widgets.Label(new Rect(8f, 4f, viewRect.width - 16f, 16f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionStatusHeader", "Crisis telemetry"));
                GUI.color = Color.white;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(8f, 20f, viewRect.width - 16f, 28f), AbyssalSummoningConsoleUtility.GetDominionOpsSummary(circle));
                Text.Font = GameFont.Small;
                y += 58f;
            }

            if (difficultyLines.Count > 0)
            {
                GUI.color = new Color(0.17f, 0.09f, 0.12f, 0.72f);
                GUI.DrawTexture(new Rect(0f, y, viewRect.width, difficultyLines.Count * 18f + 6f), BaseContent.WhiteTex);
                GUI.color = Color.white;
                Text.Font = GameFont.Tiny;
                for (int i = 0; i < difficultyLines.Count; i++)
                {
                    GUI.color = new Color(0.78f, 0.92f, 1f, 1f);
                    Widgets.Label(new Rect(8f, y + 4f + i * 18f, viewRect.width - 16f, 18f), difficultyLines[i]);
                }

                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                y += difficultyLines.Count * 18f + 10f;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                Rect lineRect = new Rect(0f, y + i * 22f, viewRect.width, 20f);
                Rect valueRect = new Rect(lineRect.x + viewRect.width * 0.44f, lineRect.y, viewRect.width * 0.56f, lineRect.height);
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                Widgets.Label(new Rect(lineRect.x, lineRect.y, viewRect.width * 0.42f, lineRect.height), entries[i].Label);
                GUI.color = entries[i].Satisfied ? new Color(0.72f, 1f, 0.74f, 1f) : new Color(1f, 0.60f, 0.54f, 1f);
                Widgets.Label(valueRect, entries[i].Value);
                GUI.color = Color.white;
            }

            Widgets.EndScrollView();
        }

        private void DrawSystemsPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);

            float gap = 10f;
            float leftWidth = (inner.width - gap) * 0.5f;
            float rightWidth = inner.width - leftWidth - gap;

            Rect leftRect = new Rect(inner.x, inner.y, leftWidth, inner.height);
            Rect rightRect = new Rect(leftRect.xMax + gap, inner.y, rightWidth, inner.height);

            DrawScrollableCapacitorPanel(leftRect, ritual);
            DrawScrollableModulePanel(rightRect);
        }

        private void DrawScrollableCapacitorPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            Rect outRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            float contentHeight = GetCapacitorContentHeight(ritual);
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, outRect.width - 16f), contentHeight);

            Widgets.BeginScrollView(outRect, ref capacitorScrollPosition, viewRect, true);
            DrawCapacitorPanel(new Rect(0f, 0f, viewRect.width, contentHeight), ritual);
            Widgets.EndScrollView();
        }

        private void DrawCapacitorPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rect.y, rect.width, 22f), "ABY_CapacitorPanel_Header".Translate());
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, rect.y + 24f, rect.width, 18f), AbyssalCircleCapacitorUtility.GetInstalledSummary(circle));
            GUI.color = Color.white;

            float rowsY = rect.y + 48f;
            float rowHeight = 28f;
            int rowIndex = 0;
            foreach (AbyssalCircleCapacitorBay bay in AbyssalCircleCapacitorUtility.GetOrderedBays())
            {
                Rect rowRect = new Rect(rect.x, rowsY + rowIndex * (rowHeight + 6f), rect.width, rowHeight);
                DrawCapacitorSlotRow(rowRect, bay);
                rowIndex++;
            }

            AbyssalCircleCapacitorRitualUtility.CapacitorReadinessReport report = AbyssalCircleCapacitorRitualUtility.CreateReadinessReport(circle, ritual);
            float summaryY = rowsY + rowIndex * (rowHeight + 6f) + 10f;
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, summaryY, rect.width, 16f), "ABY_CapacitorPanel_State".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetSupportStateLabel(report));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, summaryY + 16f, rect.width, 34f), AbyssalCircleCapacitorRitualUtility.GetSupportDetailText(report));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, summaryY + 52f, rect.width, 14f), AbyssalCircleCapacitorUtility.GetChargeReadout(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 66f, rect.width, 14f), "ABY_CapacitorPanel_Lattice".Translate() + ": " + AbyssalCircleCapacitorUtility.GetLatticeProfileLabel(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 80f, rect.width, 14f), "ABY_CapacitorPanel_Startup".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetStartupReadout(report));
            Widgets.Label(new Rect(rect.x, summaryY + 94f, rect.width, 14f), "ABY_CapacitorPanel_Reserve".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetReserveReadout(report));
            Widgets.Label(new Rect(rect.x, summaryY + 108f, rect.width, 14f), "ABY_CapacitorPanel_Feed".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetThroughputRequirementReadout(report));
            Widgets.Label(new Rect(rect.x, summaryY + 122f, rect.width, 14f), "ABY_CapacitorPanel_Grid".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetGridSmoothingReadout(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 136f, rect.width, 14f), "ABY_CapacitorPanel_Leakage".Translate() + ": " + AbyssalCircleCapacitorUtility.GetLeakageValueReadout(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 150f, rect.width, 14f), "ABY_CapacitorPanel_Flow".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetChargeFlowReadout(report));
            Widgets.Label(new Rect(rect.x, summaryY + 164f, rect.width, 14f), "ABY_CapacitorPanel_Mode".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetOperationalModeSummary(circle, ritual));
            Widgets.Label(new Rect(rect.x, summaryY + 178f, rect.width, 14f), "ABY_CapacitorPanel_Dump".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetEmergencyDumpStatusLabel(circle));
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, summaryY + 196f, rect.width, 36f), AbyssalCircleCapacitorRitualUtility.GetRitualDemandSummary(ritual));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private void DrawCapacitorSlotRow(Rect rect, AbyssalCircleCapacitorBay bay)
        {
            AbyssalCircleCapacitorSlot slot = circle.GetCapacitorSlot(bay);
            ThingDef installedDef = slot?.InstalledThingDef;
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 118f, rect.height);
            GUI.color = installedDef == null ? AbyssalSummoningConsoleArt.TextDimColor : Color.white;
            Widgets.Label(labelRect, AbyssalCircleCapacitorUtility.GetSlotRowText(slot, bay));
            GUI.color = Color.white;
            TooltipHandler.TipRegion(labelRect, AbyssalCircleCapacitorUtility.GetBayTooltip(circle, bay));

            Rect actionRect = new Rect(rect.xMax - 112f, rect.y - 1f, 112f, rect.height + 2f);
            if (installedDef == null)
            {
                if (AbyssalStyledWidgets.TextButton(actionRect, "ABY_CapacitorCommand_Install".Translate(), !circle.RitualActive, false))
                {
                    OpenCapacitorInstallMenu(bay);
                }
            }
            else if (AbyssalStyledWidgets.TextButton(actionRect, "ABY_CapacitorCommand_Remove".Translate(), !circle.RitualActive, false))
            {
                TryAssignCapacitorRemove(bay);
            }
        }


        private void DrawScrollableModulePanel(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            Rect outRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            float contentHeight = GetModuleContentHeight();
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, outRect.width - 16f), contentHeight);

            Widgets.BeginScrollView(outRect, ref stabilizerScrollPosition, viewRect, true);
            DrawModulePanel(new Rect(0f, 0f, viewRect.width, contentHeight));
            Widgets.EndScrollView();
        }

        private void DrawModulePanel(Rect rect)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rect.y, rect.width, 22f), "ABY_CircleModulesHeader".Translate());
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, rect.y + 24f, rect.width, 18f), "ABY_CircleInspect_Stabilizers".Translate(circle.InstalledStabilizerCount, circle.ModuleSlots.Count));
            GUI.color = Color.white;

            float rowsY = rect.y + 48f;
            float rowHeight = 28f;
            int rowIndex = 0;
            foreach (AbyssalCircleModuleEdge edge in AbyssalCircleModuleUtility.GetOrderedEdges())
            {
                Rect rowRect = new Rect(rect.x, rowsY + rowIndex * (rowHeight + 5f), rect.width, rowHeight);
                DrawModuleSlotRow(rowRect, edge);
                rowIndex++;
            }

            float summaryY = rowsY + rowIndex * (rowHeight + 5f) + 10f;
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, summaryY, rect.width, 16f), AbyssalSummoningConsoleUtility.GetStabilizerPatternSummary(circle));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, summaryY + 14f, rect.width, 36f), AbyssalSummoningConsoleUtility.GetStabilizerPatternDetail(circle));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, summaryY + 54f, rect.width, 14f), "ABY_CircleModulesContainment".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerContainmentBonusDisplay(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 68f, rect.width, 14f), "ABY_CircleModulesHeatDamping".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerHeatDampingDisplay(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 82f, rect.width, 14f), "ABY_CircleModulesResidue".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerResidueSuppressionDisplay(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 96f, rect.width, 14f), "ABY_CircleModulesAnomaly".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerAnomalyShieldingDisplay(circle));
            Text.Font = GameFont.Small;
        }

        private void DrawScrollableRitualPreviewPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);
            Rect outRect = new Rect(inner.x, inner.y + 28f, inner.width, inner.height - 28f);
            float contentHeight = GetRitualPreviewContentHeight(ritual, Mathf.Max(0f, outRect.width - 16f));
            Rect viewRect = new Rect(0f, 0f, Mathf.Max(0f, outRect.width - 16f), contentHeight);

            Widgets.BeginScrollView(outRect, ref ritualPreviewScrollPosition, viewRect, true);
            DrawRitualPreviewPanel(new Rect(0f, 0f, viewRect.width, contentHeight), ritual);
            Widgets.EndScrollView();
        }

        private void DrawRitualPreviewPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rect.y, rect.width, 22f), "ABY_CirclePreviewHeader".Translate());

            Widgets.Label(new Rect(rect.x, rect.y + 28f, rect.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CirclePreviewHost", "Likely host: {0}", ritual.BossLabel));
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, rect.y + 52f, rect.width, 16f), AbyssalSummoningConsoleUtility.GetRoleTagLine(ritual));
            GUI.color = Color.white;

            Text.Font = GameFont.Tiny;
            string ritualDescription = AbyssalSummoningConsoleUtility.GetRitualDescription(ritual);
            float descriptionHeight = Text.CalcHeight(ritualDescription, rect.width);
            Widgets.Label(new Rect(rect.x, rect.y + 72f, rect.width, descriptionHeight), ritualDescription);

            float rewardY = rect.y + 80f + descriptionHeight;
            if (AbyssalHordeSigilUtility.IsSupportedRitual(ritual?.Id))
            {
                AbyssalHordeSigilUtility.HordePlan hordePlan = AbyssalHordeSigilUtility.GetHordePlan(circle?.Map);
                AbyssalHordeRewardUtility.RewardSnapshot rewardSnapshot = AbyssalHordeRewardUtility.BuildSnapshot(hordePlan);
                string doctrineLine = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrinePreview_Line", "Forecast doctrine: {0}", AbyssalHordeSigilUtility.GetDoctrineLabel(hordePlan));
                string doctrineSummary = AbyssalHordeSigilUtility.GetDoctrineSummary(hordePlan);
                string operationBulletin = AbyssalHordeSigilUtility.GetOperationBulletin(hordePlan);
                string doctrineWarning = AbyssalHordeSigilUtility.GetDoctrineWarning(hordePlan);

                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rewardY, rect.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_Header", "Combat bulletin"));
                Text.Font = GameFont.Tiny;

                float bulletinTop = rewardY + 28f;
                float gap = 6f;
                float cellWidth = (rect.width - gap * 2f) / 3f;
                AbyssalSummoningConsoleArt.DrawStripCell(new Rect(rect.x, bulletinTop, cellWidth, 40f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_CellDoctrine", "Doctrine"), AbyssalHordeSigilUtility.GetDoctrineLabel(hordePlan), true, 0.03f);
                AbyssalSummoningConsoleArt.DrawStripCell(new Rect(rect.x + cellWidth + gap, bulletinTop, cellWidth, 40f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_CellFronts", "Fronts"), AbyssalHordeSigilUtility.GetFrontsBulletin(hordePlan), true, 0.21f);
                AbyssalSummoningConsoleArt.DrawStripCell(new Rect(rect.x + (cellWidth + gap) * 2f, bulletinTop, cellWidth, 40f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_CellPhases", "Phases"), AbyssalHordeSigilUtility.GetPhasesBulletin(hordePlan), true, 0.39f);

                float bulletinRow2 = bulletinTop + 46f;
                AbyssalSummoningConsoleArt.DrawStripCell(new Rect(rect.x, bulletinRow2, cellWidth, 40f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_CellCommand", "Command"), AbyssalHordeSigilUtility.GetCommandBulletin(hordePlan), hordePlan != null && hordePlan.UsesCommandGate, 0.12f);
                AbyssalSummoningConsoleArt.DrawStripCell(new Rect(rect.x + cellWidth + gap, bulletinRow2, cellWidth, 40f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_CellSiege", "Siege"), AbyssalHordeSigilUtility.GetSiegeBulletin(hordePlan), true, 0.30f);
                AbyssalSummoningConsoleArt.DrawStripCell(new Rect(rect.x + (cellWidth + gap) * 2f, bulletinRow2, cellWidth, 40f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_CellClosure", "Closure"), AbyssalHordeRewardUtility.GetClosureBulletin(rewardSnapshot), true, 0.48f);

                float bulletinSummaryY = bulletinRow2 + 46f;
                float operationBulletinHeight = Text.CalcHeight(operationBulletin, rect.width);
                Widgets.Label(new Rect(rect.x, bulletinSummaryY, rect.width, operationBulletinHeight), operationBulletin);
                GUI.color = new Color(1f, 0.72f, 0.56f, 1f);
                float doctrineWarningHeight = Text.CalcHeight(doctrineWarning, rect.width);
                Widgets.Label(new Rect(rect.x, bulletinSummaryY + operationBulletinHeight + 4f, rect.width, doctrineWarningHeight), doctrineWarning);
                GUI.color = Color.white;

                rewardY = bulletinSummaryY + operationBulletinHeight + doctrineWarningHeight + 14f;

                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rewardY, rect.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrinePreview_Header", "Offensive pattern"));
                Text.Font = GameFont.Tiny;

                float doctrineLineHeight = Text.CalcHeight(doctrineLine, rect.width);
                Widgets.Label(new Rect(rect.x, rewardY + 28f, rect.width, doctrineLineHeight), doctrineLine);

                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                float doctrineSummaryHeight = Text.CalcHeight(doctrineSummary, rect.width);
                Widgets.Label(new Rect(rect.x, rewardY + 32f + doctrineLineHeight, rect.width, doctrineSummaryHeight), doctrineSummary);
                GUI.color = Color.white;

                rewardY += 42f + doctrineLineHeight + doctrineSummaryHeight + 10f;

                string phaseSummary = AbyssalHordeSigilUtility.GetPhaseFlowSummary(hordePlan);
                List<string> phaseLines = AbyssalHordeSigilUtility.GetPhaseLines(hordePlan);

                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rewardY, rect.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhasesPreview_Header", "Phase flow"));
                Text.Font = GameFont.Tiny;

                float phaseSummaryHeight = Text.CalcHeight(phaseSummary, rect.width);
                Widgets.Label(new Rect(rect.x, rewardY + 28f, rect.width, phaseSummaryHeight), phaseSummary);

                float phaseLineY = rewardY + 32f + phaseSummaryHeight;
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                for (int i = 0; i < phaseLines.Count; i++)
                {
                    float lineHeight = Text.CalcHeight(phaseLines[i], rect.width);
                    Widgets.Label(new Rect(rect.x, phaseLineY, rect.width, lineHeight), phaseLines[i]);
                    phaseLineY += lineHeight + 4f;
                }
                GUI.color = Color.white;

                rewardY = phaseLineY + 10f;

                string perimeterSummary = AbyssalHordeSigilUtility.GetPerimeterSummary(hordePlan);
                List<string> perimeterLines = AbyssalHordeSigilUtility.GetPerimeterLines(hordePlan);

                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rewardY, rect.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePerimeter_Header", "Perimeter intelligence"));
                Text.Font = GameFont.Tiny;

                float perimeterSummaryHeight = Text.CalcHeight(perimeterSummary, rect.width);
                Widgets.Label(new Rect(rect.x, rewardY + 28f, rect.width, perimeterSummaryHeight), perimeterSummary);

                float perimeterLineY = rewardY + 32f + perimeterSummaryHeight;
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                for (int i = 0; i < perimeterLines.Count; i++)
                {
                    float lineHeight = Text.CalcHeight(perimeterLines[i], rect.width);
                    Widgets.Label(new Rect(rect.x, perimeterLineY, rect.width, lineHeight), perimeterLines[i]);
                    perimeterLineY += lineHeight + 4f;
                }
                GUI.color = Color.white;

                rewardY = perimeterLineY + 10f;

                string commandGateSummary = AbyssalHordeSigilUtility.GetCommandGateSummary(hordePlan);
                List<string> commandGateLines = AbyssalHordeSigilUtility.GetCommandGateLines(hordePlan);

                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rewardY, rect.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_Header", "Command gate node"));
                Text.Font = GameFont.Tiny;

                float commandSummaryHeight = Text.CalcHeight(commandGateSummary, rect.width);
                Widgets.Label(new Rect(rect.x, rewardY + 28f, rect.width, commandSummaryHeight), commandGateSummary);

                float commandLineY = rewardY + 32f + commandSummaryHeight;
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                for (int i = 0; i < commandGateLines.Count; i++)
                {
                    float lineHeight = Text.CalcHeight(commandGateLines[i], rect.width);
                    Widgets.Label(new Rect(rect.x, commandLineY, rect.width, lineHeight), commandGateLines[i]);
                    commandLineY += lineHeight + 4f;
                }
                GUI.color = Color.white;

                rewardY = commandLineY + 10f;

                string economySummary = AbyssalHordeRewardUtility.GetForecastSummary(rewardSnapshot);
                List<string> economyLines = AbyssalHordeRewardUtility.GetForecastLines(rewardSnapshot);

                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rewardY, rect.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeEconomy_Header", "Reward routing"));
                Text.Font = GameFont.Tiny;

                float economySummaryHeight = Text.CalcHeight(economySummary, rect.width);
                Widgets.Label(new Rect(rect.x, rewardY + 28f, rect.width, economySummaryHeight), economySummary);

                float economyLineY = rewardY + 32f + economySummaryHeight;
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                for (int i = 0; i < economyLines.Count; i++)
                {
                    float lineHeight = Text.CalcHeight(economyLines[i], rect.width);
                    Widgets.Label(new Rect(rect.x, economyLineY, rect.width, lineHeight), economyLines[i]);
                    economyLineY += lineHeight + 4f;
                }
                GUI.color = Color.white;

                rewardY = economyLineY + 10f;
            }

            Text.Font = GameFont.Small;
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rewardY, rect.width, 22f), "ABY_CircleRewardsHeader".Translate());
            Text.Font = GameFont.Tiny;

            string rewardText = string.Join("\n",
                new[]
                {
                    AbyssalSummoningConsoleUtility.GetRewardVectorGuaranteed(ritual),
                    AbyssalSummoningConsoleUtility.GetRewardVectorProgression(ritual),
                    AbyssalSummoningConsoleUtility.GetRewardVectorFollowUp(ritual)
                }.Where(line => !line.NullOrEmpty()).ToArray());
            float rewardHeight = Text.CalcHeight(rewardText, rect.width);
            Widgets.Label(new Rect(rect.x, rewardY + 28f, rect.width, rewardHeight), rewardText);

            float dominionY = rewardY + 36f + rewardHeight + 10f;
            if (AbyssalSummoningConsoleUtility.IsDominionRitual(ritual))
            {
                MapComponent_DominionCrisis crisis = circle.Map?.GetComponent<MapComponent_DominionCrisis>();
                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, dominionY, rect.width, 22f), "ABY_DominionAnchorPreviewHeader".Translate());
                Text.Font = GameFont.Tiny;

                string summaryText = crisis != null && crisis.IsGatePhaseActive
                    ? "ABY_DominionAnchorPreviewSummaryGate".Translate(crisis.GetGateStatusValue(), crisis.GetGateIntegrityValue(), crisis.TicksRemaining.ToStringTicksToPeriod())
                    : crisis != null && crisis.IsActive
                        ? "ABY_DominionAnchorPreviewSummary".Translate(crisis.GetAnchorStatusValue(), crisis.GetAnchorPressureLabel(), crisis.TicksRemaining.ToStringTicksToPeriod())
                        : "ABY_DominionAnchorPreviewSummaryIdle".Translate();

                float summaryHeight = Text.CalcHeight(summaryText, rect.width);
                Widgets.Label(new Rect(rect.x, dominionY + 28f, rect.width, summaryHeight), summaryText);

                float linesY = dominionY + 32f + summaryHeight;
                List<string> anchorLines = crisis != null ? crisis.GetAnchorConsoleLines() : new List<string>();
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                for (int i = 0; i < anchorLines.Count; i++)
                {
                    float lineHeight = Text.CalcHeight(anchorLines[i], rect.width);
                    Widgets.Label(new Rect(rect.x, linesY, rect.width, lineHeight), anchorLines[i]);
                    linesY += lineHeight + 4f;
                }
                GUI.color = Color.white;

                float waveSectionY = linesY + 10f;
                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, waveSectionY, rect.width, 22f), "ABY_DominionWavePreviewHeader".Translate());
                Text.Font = GameFont.Tiny;

                string waveSummaryText = crisis != null && crisis.IsGatePhaseActive
                    ? "ABY_DominionWavePreviewSummaryGate".Translate(crisis.GetWaveStatusValue(), crisis.GetGatePulseEtaValue())
                    : crisis != null && crisis.IsAnchorPhaseActive
                        ? "ABY_DominionWavePreviewSummary".Translate(crisis.GetWaveStatusValue(), crisis.GetNextWaveEtaValue())
                        : "ABY_DominionWavePreviewSummaryIdle".Translate();

                float waveSummaryHeight = Text.CalcHeight(waveSummaryText, rect.width);
                Widgets.Label(new Rect(rect.x, waveSectionY + 28f, rect.width, waveSummaryHeight), waveSummaryText);

                float waveLinesY = waveSectionY + 32f + waveSummaryHeight;
                List<string> waveLines = crisis != null ? crisis.GetWaveConsoleLines() : new List<string>();
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                for (int i = 0; i < waveLines.Count; i++)
                {
                    float lineHeight = Text.CalcHeight(waveLines[i], rect.width);
                    Widgets.Label(new Rect(rect.x, waveLinesY, rect.width, lineHeight), waveLines[i]);
                    waveLinesY += lineHeight + 4f;
                }
                GUI.color = Color.white;

                float gateSectionY = waveLinesY + 10f;
                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, gateSectionY, rect.width, 22f), "ABY_DominionGatePreviewHeader".Translate());
                Text.Font = GameFont.Tiny;

                string gateSummaryText = crisis != null && crisis.IsGatePhaseActive
                    ? "ABY_DominionGatePreviewSummary".Translate(crisis.GetGateStatusValue(), crisis.GetGateIntegrityValue(), crisis.GetGatePulseEtaValue(), crisis.TicksRemaining.ToStringTicksToPeriod())
                    : "ABY_DominionGatePreviewSummaryIdle".Translate();

                float gateSummaryHeight = Text.CalcHeight(gateSummaryText, rect.width);
                Widgets.Label(new Rect(rect.x, gateSectionY + 28f, rect.width, gateSummaryHeight), gateSummaryText);

                float gateLinesY = gateSectionY + 32f + gateSummaryHeight;
                List<string> gateLines = crisis != null ? crisis.GetGateConsoleLines() : new List<string>();
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                for (int i = 0; i < gateLines.Count; i++)
                {
                    float lineHeight = Text.CalcHeight(gateLines[i], rect.width);
                    Widgets.Label(new Rect(rect.x, gateLinesY, rect.width, lineHeight), gateLines[i]);
                    gateLinesY += lineHeight + 4f;
                }
                GUI.color = Color.white;

                float rewardSectionY = gateLinesY + 10f;
                Text.Font = GameFont.Small;
                AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rewardSectionY, rect.width, 22f), AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionRewardPreviewHeader", "Reward / aftermath"));
                Text.Font = GameFont.Tiny;

                string rewardSummaryText = crisis != null
                    ? AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_DominionRewardPreviewSummary",
                        "Next payout forecast: {0}. Rearm window: {1}.",
                        crisis.GetRewardForecastValue(),
                        crisis.GetCooldownValue())
                    : AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_DominionRewardPreviewSummaryIdle",
                        "Dominion reward routing is dormant until the breach is attempted on this map.");

                float rewardSummaryHeight = Text.CalcHeight(rewardSummaryText, rect.width);
                Widgets.Label(new Rect(rect.x, rewardSectionY + 28f, rect.width, rewardSummaryHeight), rewardSummaryText);

                float rewardLinesY = rewardSectionY + 32f + rewardSummaryHeight;
                List<string> rewardLines = crisis != null ? crisis.GetRewardConsoleLines() : new List<string>();
                List<string> balanceLines = crisis != null ? crisis.GetBalanceConsoleLines() : new List<string>();
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                for (int i = 0; i < rewardLines.Count; i++)
                {
                    float lineHeight = Text.CalcHeight(rewardLines[i], rect.width);
                    Widgets.Label(new Rect(rect.x, rewardLinesY, rect.width, lineHeight), rewardLines[i]);
                    rewardLinesY += lineHeight + 4f;
                }

                if (balanceLines.Count > 0)
                {
                    rewardLinesY += 6f;
                    for (int i = 0; i < balanceLines.Count; i++)
                    {
                        float lineHeight = Text.CalcHeight(balanceLines[i], rect.width);
                        Widgets.Label(new Rect(rect.x, rewardLinesY, rect.width, lineHeight), balanceLines[i]);
                        rewardLinesY += lineHeight + 4f;
                    }
                }
                GUI.color = Color.white;
            }

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private float GetCapacitorContentHeight(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            int bayCount = AbyssalCircleCapacitorUtility.GetOrderedBays().Count();
            float rowsHeight = bayCount * 34f;
            return 48f + rowsHeight + 268f;
        }

        private float GetModuleContentHeight()
        {
            int edgeCount = AbyssalCircleModuleUtility.GetOrderedEdges().Count();
            float rowsHeight = edgeCount * 33f;
            return 48f + rowsHeight + 146f;
        }

        private float GetRitualPreviewContentHeight(AbyssalSummoningConsoleUtility.RitualDefinition ritual, float width)
        {
            Text.Font = GameFont.Tiny;
            float descriptionHeight = Text.CalcHeight(AbyssalSummoningConsoleUtility.GetRitualDescription(ritual), width);
            float rewardHeight = Text.CalcHeight(string.Join("\n",
                new[]
                {
                    AbyssalSummoningConsoleUtility.GetRewardVectorGuaranteed(ritual),
                    AbyssalSummoningConsoleUtility.GetRewardVectorProgression(ritual),
                    AbyssalSummoningConsoleUtility.GetRewardVectorFollowUp(ritual)
                }.Where(line => !line.NullOrEmpty()).ToArray()), width);
            float total = 126f + descriptionHeight + rewardHeight;

            if (AbyssalHordeSigilUtility.IsSupportedRitual(ritual?.Id))
            {
                AbyssalHordeSigilUtility.HordePlan hordePlan = AbyssalHordeSigilUtility.GetHordePlan(circle?.Map);
                AbyssalHordeRewardUtility.RewardSnapshot rewardSnapshot = AbyssalHordeRewardUtility.BuildSnapshot(hordePlan);
                string doctrineLine = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrinePreview_Line", "Forecast doctrine: {0}", AbyssalHordeSigilUtility.GetDoctrineLabel(hordePlan));
                string doctrineSummary = AbyssalHordeSigilUtility.GetDoctrineSummary(hordePlan);
                string phaseSummary = AbyssalHordeSigilUtility.GetPhaseFlowSummary(hordePlan);
                List<string> phaseLines = AbyssalHordeSigilUtility.GetPhaseLines(hordePlan);
                string perimeterSummary = AbyssalHordeSigilUtility.GetPerimeterSummary(hordePlan);
                List<string> perimeterLines = AbyssalHordeSigilUtility.GetPerimeterLines(hordePlan);
                string commandGateSummary = AbyssalHordeSigilUtility.GetCommandGateSummary(hordePlan);
                List<string> commandGateLines = AbyssalHordeSigilUtility.GetCommandGateLines(hordePlan);
                string economySummary = AbyssalHordeRewardUtility.GetForecastSummary(rewardSnapshot);
                List<string> economyLines = AbyssalHordeRewardUtility.GetForecastLines(rewardSnapshot);
                total += 52f + Text.CalcHeight(doctrineLine, width) + Text.CalcHeight(doctrineSummary, width);
                total += 56f + Text.CalcHeight(phaseSummary, width);
                for (int i = 0; i < phaseLines.Count; i++)
                {
                    total += Text.CalcHeight(phaseLines[i], width) + 4f;
                }
                total += 56f + Text.CalcHeight(perimeterSummary, width);
                for (int i = 0; i < perimeterLines.Count; i++)
                {
                    total += Text.CalcHeight(perimeterLines[i], width) + 4f;
                }
                total += 56f + Text.CalcHeight(commandGateSummary, width);
                for (int i = 0; i < commandGateLines.Count; i++)
                {
                    total += Text.CalcHeight(commandGateLines[i], width) + 4f;
                }
                total += 56f + Text.CalcHeight(economySummary, width);
                for (int i = 0; i < economyLines.Count; i++)
                {
                    total += Text.CalcHeight(economyLines[i], width) + 4f;
                }
            }

            if (AbyssalSummoningConsoleUtility.IsDominionRitual(ritual))
            {
                MapComponent_DominionCrisis crisis = circle.Map?.GetComponent<MapComponent_DominionCrisis>();
                string summaryText = crisis != null && crisis.IsGatePhaseActive
                    ? "ABY_DominionAnchorPreviewSummaryGate".Translate(crisis.GetGateStatusValue(), crisis.GetGateIntegrityValue(), crisis.TicksRemaining.ToStringTicksToPeriod())
                    : crisis != null && crisis.IsActive
                        ? "ABY_DominionAnchorPreviewSummary".Translate(crisis.GetAnchorStatusValue(), crisis.GetAnchorPressureLabel(), crisis.TicksRemaining.ToStringTicksToPeriod())
                        : "ABY_DominionAnchorPreviewSummaryIdle".Translate();
                total += 54f + Text.CalcHeight(summaryText, width);

                List<string> anchorLines = crisis != null ? crisis.GetAnchorConsoleLines() : new List<string>();
                for (int i = 0; i < anchorLines.Count; i++)
                {
                    total += Text.CalcHeight(anchorLines[i], width) + 4f;
                }

                string waveSummaryText = crisis != null && crisis.IsGatePhaseActive
                    ? "ABY_DominionWavePreviewSummaryGate".Translate(crisis.GetWaveStatusValue(), crisis.GetGatePulseEtaValue())
                    : crisis != null && crisis.IsAnchorPhaseActive
                        ? "ABY_DominionWavePreviewSummary".Translate(crisis.GetWaveStatusValue(), crisis.GetNextWaveEtaValue())
                        : "ABY_DominionWavePreviewSummaryIdle".Translate();
                total += 64f + Text.CalcHeight(waveSummaryText, width);

                List<string> waveLines = crisis != null ? crisis.GetWaveConsoleLines() : new List<string>();
                for (int i = 0; i < waveLines.Count; i++)
                {
                    total += Text.CalcHeight(waveLines[i], width) + 4f;
                }

                string gateSummaryText = crisis != null && crisis.IsGatePhaseActive
                    ? "ABY_DominionGatePreviewSummary".Translate(crisis.GetGateStatusValue(), crisis.GetGateIntegrityValue(), crisis.GetGatePulseEtaValue(), crisis.TicksRemaining.ToStringTicksToPeriod())
                    : "ABY_DominionGatePreviewSummaryIdle".Translate();
                total += 64f + Text.CalcHeight(gateSummaryText, width);

                List<string> gateLines = crisis != null ? crisis.GetGateConsoleLines() : new List<string>();
                for (int i = 0; i < gateLines.Count; i++)
                {
                    total += Text.CalcHeight(gateLines[i], width) + 4f;
                }

                string rewardSummaryText = crisis != null
                    ? AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_DominionRewardPreviewSummary",
                        "Next payout forecast: {0}. Rearm window: {1}.",
                        crisis.GetRewardForecastValue(),
                        crisis.GetCooldownValue())
                    : AbyssalSummoningConsoleUtility.TranslateOrFallback(
                        "ABY_DominionRewardPreviewSummaryIdle",
                        "Dominion reward routing is dormant until the breach is attempted on this map.");
                total += 64f + Text.CalcHeight(rewardSummaryText, width);

                List<string> rewardLines = crisis != null ? crisis.GetRewardConsoleLines() : new List<string>();
                for (int i = 0; i < rewardLines.Count; i++)
                {
                    total += Text.CalcHeight(rewardLines[i], width) + 4f;
                }
            }

            Text.Font = GameFont.Small;
            return total;
        }

        private void DrawModuleSlotRow(Rect rect, AbyssalCircleModuleEdge edge)
        {
            AbyssalCircleModuleSlot slot = circle.GetModuleSlot(edge);
            ThingDef installedDef = slot?.InstalledThingDef;
            string edgeLabel = AbyssalCircleModuleUtility.GetEdgeLabel(edge);
            string slotLabel = installedDef == null ? "ABY_CircleModuleSlotEmpty".Translate(edgeLabel) : "ABY_CircleModuleSlotInstalled".Translate(edgeLabel, installedDef.label.CapitalizeFirst(), AbyssalCircleModuleUtility.GetTierLabel(installedDef));
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 118f, rect.height);
            GUI.color = installedDef == null ? AbyssalSummoningConsoleArt.TextDimColor : Color.white;
            Widgets.Label(labelRect, slotLabel);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(rect, AbyssalSummoningConsoleUtility.GetModuleSlotTooltip(circle, edge));
            Rect actionRect = new Rect(rect.xMax - 112f, rect.y - 1f, 112f, rect.height + 2f);
            if (installedDef == null)
            {
                if (AbyssalStyledWidgets.TextButton(actionRect, "ABY_CircleModuleCommand_Install".Translate(), !circle.RitualActive, false))
                {
                    OpenInstallMenu(edge);
                }
            }
            else if (AbyssalStyledWidgets.TextButton(actionRect, "ABY_CircleModuleCommand_Remove".Translate(), !circle.RitualActive, false))
            {
                TryAssignRemove(edge);
            }
        }

        private void OpenInstallMenu(AbyssalCircleModuleEdge edge)
        {
            List<Thing> candidates = AbyssalCircleModuleUtility.GetBestAvailableModuleCandidates(circle, DefModExtension_AbyssalCircleModule.StabilizerFamily);
            if (candidates.Count == 0)
            {
                Messages.Message("ABY_CircleModuleFail_NoAvailableModules".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing candidate = candidates[i];
                int totalCount = AbyssalCircleModuleUtility.CountAvailableModules(circle.Map, candidate.def);
                Thing capturedThing = candidate;
                options.Add(new FloatMenuOption("ABY_CircleModuleMenuOption".Translate(candidate.LabelCap, AbyssalCircleModuleUtility.GetTierLabel(candidate.def), totalCount), delegate
                {
                    if (AbyssalSummoningConsoleUtility.TryAssignModuleInstall(circle, capturedThing, edge, out string failReason))
                    {
                        Messages.Message("ABY_CircleModuleInstallQueued".Translate(capturedThing.LabelCap, AbyssalCircleModuleUtility.GetEdgeLabel(edge)), MessageTypeDefOf.PositiveEvent, false);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    }
                    else if (!failReason.NullOrEmpty())
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    }
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void TryAssignRemove(AbyssalCircleModuleEdge edge)
        {
            if (AbyssalSummoningConsoleUtility.TryAssignModuleRemove(circle, edge, out string failReason))
            {
                Messages.Message("ABY_CircleModuleRemoveQueued".Translate(AbyssalCircleModuleUtility.GetEdgeLabel(edge)), MessageTypeDefOf.PositiveEvent, false);
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
            else if (!failReason.NullOrEmpty())
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void OpenCapacitorInstallMenu(AbyssalCircleCapacitorBay bay)
        {
            List<Thing> candidates = AbyssalCircleCapacitorUtility.GetBestAvailableCapacitorCandidates(circle, bay);
            if (candidates.Count == 0)
            {
                Messages.Message("ABY_CapacitorFail_NoAvailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Thing candidate = candidates[i];
                int totalCount = AbyssalCircleCapacitorUtility.CountAvailableCapacitors(circle.Map, candidate.def);
                Thing capturedThing = candidate;
                options.Add(new FloatMenuOption("ABY_CapacitorMenuOption".Translate(candidate.LabelCap, AbyssalCircleCapacitorUtility.GetTierLabel(candidate.def), totalCount), delegate
                {
                    if (AbyssalSummoningConsoleUtility.TryAssignCapacitorInstall(circle, capturedThing, bay, out string failReason))
                    {
                        Messages.Message("ABY_CapacitorInstallQueued".Translate(capturedThing.LabelCap, AbyssalCircleCapacitorUtility.GetBayLabel(bay)), MessageTypeDefOf.PositiveEvent, false);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    }
                    else if (!failReason.NullOrEmpty())
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    }
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void TryAssignCapacitorRemove(AbyssalCircleCapacitorBay bay)
        {
            if (AbyssalSummoningConsoleUtility.TryAssignCapacitorRemove(circle, bay, out string failReason))
            {
                Messages.Message("ABY_CapacitorRemoveQueued".Translate(AbyssalCircleCapacitorUtility.GetBayLabel(bay)), MessageTypeDefOf.PositiveEvent, false);
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
            else if (!failReason.NullOrEmpty())
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void OpenThreatCodex()
        {
            Find.WindowStack.Add(new Window_ABY_BestiaryCodex(circle));
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
        }

        private void JumpToSigil(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            Thing jumpTarget = AbyssalSummoningConsoleUtility.FindBestSigilJumpTarget(circle, ritual, out string failReason);
            if (jumpTarget != null)
            {
                CameraJumper.TryJumpAndSelect(jumpTarget);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
            else if (!failReason.NullOrEmpty())
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void JumpToDominionObjective()
        {
            if (AbyssalSummoningConsoleUtility.TryJumpToDominionObjective(circle, out string failReason))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
            else if (!failReason.NullOrEmpty())
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void ConfirmAndAssign(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            MapComponent_DominionCrisis dominionCrisis = circle.Map?.GetComponent<MapComponent_DominionCrisis>();
            bool dominionAbortMode = AbyssalSummoningConsoleUtility.IsDominionRitual(ritual) && dominionCrisis != null && dominionCrisis.IsActive;

            string confirmText = dominionAbortMode
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionCrisisConfirmAbort", "Abort the dominion crisis core on this map? This ends the current staging window and marks the run as cancelled.")
                : circle.CapacitorOverchannelEnabled && AbyssalCircleCapacitorRitualUtility.WouldForceStart(circle, ritual)
                    ? "ABY_CapacitorConfirm_ForcedInvocation".Translate(AbyssalSummoningConsoleUtility.GetRitualLabel(ritual))
                    : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleConfirmInvocation", "Assign a prepared sigil and order a colonist to begin {0}? This starts a hostile breach sequence.", AbyssalSummoningConsoleUtility.GetRitualLabel(ritual));

            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(confirmText, delegate
            {
                if (dominionAbortMode)
                {
                    if (dominionCrisis.TryAbort(circle, out string abortFailReason))
                    {
                        Messages.Message(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionCrisisAbortStarted", "Dominion staging aborted."), MessageTypeDefOf.PositiveEvent, false);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    }
                    else if (!abortFailReason.NullOrEmpty())
                    {
                        Messages.Message(abortFailReason, MessageTypeDefOf.RejectInput, false);
                    }

                    return;
                }

                if (AbyssalSummoningConsoleUtility.TryAssignInvocation(circle, ritual, out string failReason))
                {
                    Messages.Message(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleAssignStarted", "Invocation sequence assigned. A colonist is moving a sigil to the circle."), MessageTypeDefOf.PositiveEvent, false);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                else if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }
            }));
        }
    }
}
