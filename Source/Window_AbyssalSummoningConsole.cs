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

        public Window_AbyssalSummoningConsole(Building_AbyssalSummoningCircle circle)
        {
            this.circle = circle;
            selectedRitualId = AbyssalSummoningConsoleUtility.GetDefaultRitual()?.Id;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
            draggable = true;
            forcePause = false;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = false;
            resizeable = false;
        }

        public override Vector2 InitialSize => new Vector2(1210f, 820f);

        public override void DoWindowContents(Rect inRect)
        {
            if (circle == null || circle.Destroyed || circle.Map == null)
            {
                Close();
                return;
            }

            AbyssalSummoningConsoleArt.ReducedEffects = circle.ReducedConsoleEffects;
            AbyssalSummoningConsoleArt.DrawBackground(inRect);

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 72f);
            Rect stripRect = new Rect(inRect.x, headerRect.yMax + 10f, inRect.width, 62f);
            float upperY = stripRect.yMax + 10f;
            Rect ritualsRect = new Rect(inRect.x, upperY, 500f, 452f);
            Rect controlRect = new Rect(ritualsRect.xMax + 10f, upperY, inRect.width - ritualsRect.width - 10f, 248f);
            Rect statusRect = new Rect(ritualsRect.xMax + 10f, controlRect.yMax + 10f, inRect.width - ritualsRect.width - 10f, 194f);
            float upperBottom = Mathf.Max(ritualsRect.yMax, statusRect.yMax);
            Rect previewRect = new Rect(inRect.x, upperBottom + 10f, inRect.width, inRect.height - upperBottom - 10f);

            AbyssalSummoningConsoleUtility.RitualDefinition ritual = GetSelectedRitual();

            DrawHeader(headerRect, ritual);
            DrawReadinessStrip(stripRect, ritual);
            DrawRitualBrowser(ritualsRect, ritual);
            DrawControlPanel(controlRect, ritual);
            DrawStatusPanel(statusRect, ritual);
            DrawPreviewPanel(previewRect, ritual);
        }

        private AbyssalSummoningConsoleUtility.RitualDefinition GetSelectedRitual()
        {
            AbyssalSummoningConsoleUtility.RitualDefinition ritual = AbyssalSummoningConsoleUtility.GetRituals().FirstOrDefault(r => r.Id == selectedRitualId);
            if (ritual == null)
            {
                ritual = AbyssalSummoningConsoleUtility.GetDefaultRitual();
                selectedRitualId = ritual.Id;
            }
            return ritual;
        }

        private void DrawHeader(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            string subtitle = circle.RitualActive
                ? AbyssalSummoningConsoleUtility.GetConsoleSubtitleActive(circle.GetCurrentPhaseTranslated())
                : AbyssalSummoningConsoleUtility.GetConsoleSubtitle();
            AbyssalSummoningConsoleArt.DrawHeader(rect, AbyssalSummoningConsoleUtility.GetConsoleTitle(), subtitle, circle.RitualActive);
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

            List<AbyssalSummoningConsoleUtility.RitualDefinition> rituals = AbyssalSummoningConsoleUtility.GetRituals().ToList();
            Rect outRect = new Rect(inner.x, inner.y + 30f, inner.width, inner.height - 30f);
            float cardHeight = 160f;
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

            Rect iconRect = new Rect(rect.x + 12f, rect.y + 14f, 46f, 46f);
            ThingDef sigilDef = AbyssalSummoningConsoleUtility.GetSigilDef(ritual);
            if (sigilDef != null && sigilDef.uiIcon != null)
            {
                GUI.color = Color.white;
                GUI.DrawTexture(iconRect, sigilDef.uiIcon, ScaleMode.ScaleToFit, true);
            }

            Rect titleRect = new Rect(rect.x + 70f, rect.y + 12f, rect.width - 150f, 22f);
            Rect subRect = new Rect(rect.x + 70f, rect.y + 34f, rect.width - 150f, 18f);
            Rect descRect = new Rect(rect.x + 12f, rect.y + 64f, rect.width - 24f, 50f);
            Rect metaRect = new Rect(rect.x + 12f, rect.y + 116f, rect.width - 24f, 18f);
            Rect selectRect = new Rect(rect.xMax - 108f, rect.y + rect.height - 34f, 96f, 28f);

            Widgets.Label(titleRect, AbyssalSummoningConsoleUtility.GetRitualLabel(ritual));
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(subRect, AbyssalSummoningConsoleUtility.GetRitualSubtitle(ritual));
            GUI.color = Color.white;
            Widgets.Label(descRect, AbyssalSummoningConsoleUtility.GetRitualDescription(ritual));
            GUI.color = new Color(1f, 0.76f, 0.58f, 1f);
            Widgets.Label(metaRect, AbyssalSummoningConsoleUtility.GetRitualMetaText(circle, ritual));
            GUI.color = Color.white;

            if (AbyssalStyledWidgets.TextButton(selectRect, selected ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleSelected", "Selected") : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleSelect", "Select"), !selected, selected))
            {
                selectedRitualId = ritual.Id;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
        }

        private void DrawControlPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(12f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_CircleControlHeader".Translate());

            AbyssalSummoningConsoleUtility.CircleRiskTier riskTier = AbyssalSummoningConsoleUtility.GetRiskTier(circle, ritual);
            AbyssalSummoningConsoleArt.DrawRiskBar(new Rect(inner.x, inner.y + 28f, inner.width, 28f), AbyssalSummoningConsoleUtility.GetRiskFill(circle, ritual), AbyssalSummoningConsoleUtility.GetRiskLabel(riskTier), AbyssalSummoningConsoleUtility.GetRiskColor(riskTier), circle.RitualActive);

            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 64f, inner.width, 18f), "ABY_CircleControlState".Translate());
            GUI.color = Color.white;
            Widgets.Label(new Rect(inner.x, inner.y + 82f, inner.width, 24f), circle.GetCurrentStatusLine());

            float rowY = inner.y + 110f;
            float rowHeight = 30f;
            float rowGap = 6f;

            bool reduced = circle.ReducedConsoleEffects;
            if (DrawToggleSettingRow(new Rect(inner.x, rowY, inner.width, rowHeight),
                "ABY_CircleReducedEffects".Translate().ToString(),
                AbyssalSummoningConsoleUtility.GetReducedEffectsLabel(reduced),
                reduced,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleReducedEffectsDesc", "Softens header sweeps, seal rotation, and other animated accents inside the summoning console.")))
            {
                circle.SetReducedConsoleEffects(!reduced);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }

            rowY += rowHeight + rowGap;
            bool overchannel = circle.CapacitorOverchannelEnabled;
            string overchannelState = overchannel
                ? "ABY_CapacitorMode_Armed".Translate().ToString()
                : "ABY_CapacitorMode_Standard".Translate().ToString();
            if (DrawToggleSettingRow(new Rect(inner.x, rowY, inner.width, rowHeight),
                "ABY_CapacitorControl_Overchannel".Translate().ToString(),
                overchannelState,
                overchannel,
                "ABY_CapacitorControl_OverchannelDesc".Translate()))
            {
                circle.SetCapacitorOverchannelEnabled(!overchannel);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }

            rowY += rowHeight + rowGap;
            bool dump = circle.CapacitorEmergencyDumpEnabled;
            string dumpState = AbyssalCircleCapacitorRitualUtility.GetEmergencyDumpStatusLabel(circle);
            if (DrawToggleSettingRow(new Rect(inner.x, rowY, inner.width, rowHeight),
                "ABY_CapacitorControl_Dump".Translate().ToString(),
                dumpState,
                dump,
                "ABY_CapacitorControl_DumpDesc".Translate()))
            {
                circle.SetCapacitorEmergencyDumpEnabled(!dump);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }

            float buttonY = rowY + rowHeight + 10f;
            Rect openRect = new Rect(inner.x, buttonY, inner.width, 30f);
            Rect invokeRect = new Rect(inner.x, openRect.yMax + 6f, inner.width, 34f);
            if (AbyssalStyledWidgets.TextButton(openRect, AbyssalSummoningConsoleUtility.GetJumpToSigilLabel(), !circle.RitualActive, false))
            {
                JumpToSigil(ritual);
            }

            string invokeLabel = circle.CapacitorOverchannelEnabled && AbyssalCircleCapacitorRitualUtility.WouldForceStart(circle, ritual)
                ? "ABY_CapacitorCommand_ForceInvoke".Translate()
                : AbyssalSummoningConsoleUtility.GetAssignSigilLabel();
            if (AbyssalStyledWidgets.TextButton(invokeRect, invokeLabel, !circle.RitualActive, true))
            {
                ConfirmAndAssign(ritual);
            }
        }

        private bool DrawToggleSettingRow(Rect rect, string label, string stateText, bool active, string tooltip)
        {
            Color oldColor = GUI.color;
            Color fillColor = active
                ? new Color(0.28f, 0.12f, 0.08f, 0.72f)
                : new Color(0.14f, 0.08f, 0.08f, 0.62f);
            GUI.color = fillColor;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = active ? new Color(1f, 0.52f, 0.18f, 0.95f) : new Color(0.58f, 0.28f, 0.16f, 0.80f);
            Widgets.DrawBox(rect, 1);
            GUI.color = oldColor;

            Rect labelRect = new Rect(rect.x + 10f, rect.y + 4f, rect.width - 150f, rect.height - 8f);
            Rect stateRect = new Rect(rect.xMax - 132f, rect.y + 3f, 124f, rect.height - 6f);

            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldGuiColor = GUI.color;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;
            if (Text.CalcHeight(label, labelRect.width) > labelRect.height + 2f)
            {
                Text.Font = GameFont.Tiny;
            }

            GUI.color = active ? Color.white : new Color(0.94f, 0.85f, 0.78f, 0.95f);
            Widgets.Label(labelRect, label);
            GUI.color = oldGuiColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;

            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }

            return AbyssalStyledWidgets.TextButton(stateRect, stateText, true, active);
        }

        private void DrawSummaryLine(Rect rect, ref float y, string text, bool dim = false, bool tiny = true, Color? colorOverride = null)
        {
            TextAnchor oldAnchor = Text.Anchor;
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = tiny ? GameFont.Tiny : GameFont.Small;
            GUI.color = colorOverride ?? (dim ? AbyssalSummoningConsoleArt.TextDimColor : Color.white);
            float height = Text.CalcHeight(text, rect.width);
            Widgets.Label(new Rect(rect.x, y, rect.width, height + 2f), text);
            y += height + (tiny ? 4f : 5f);

            GUI.color = oldColor;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }

        private void DrawStatusPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(12f);
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_CircleStatusHeaderLong".Translate());

            List<AbyssalSummoningConsoleUtility.StatusEntry> entries = AbyssalSummoningConsoleUtility.GetStatusEntries(circle, ritual);
            for (int i = 0; i < entries.Count; i++)
            {
                Rect lineRect = new Rect(inner.x, inner.y + 28f + i * 22f, inner.width, 20f);
                Rect valueRect = new Rect(lineRect.x + inner.width * 0.44f, lineRect.y, inner.width * 0.56f, lineRect.height);
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                Widgets.Label(new Rect(lineRect.x, lineRect.y, inner.width * 0.42f, lineRect.height), entries[i].Label);
                GUI.color = entries[i].Satisfied ? new Color(0.72f, 1f, 0.74f, 1f) : new Color(1f, 0.60f, 0.54f, 1f);
                Widgets.Label(valueRect, entries[i].Value);
                GUI.color = Color.white;
            }
        }

        private void DrawPreviewPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(10f);

            float gap = 10f;
            float leftWidth = Mathf.Round(inner.width * 0.36f);
            float midWidth = Mathf.Round(inner.width * 0.24f);
            float rightWidth = inner.width - leftWidth - midWidth - gap * 2f;

            Rect leftRect = new Rect(inner.x, inner.y, leftWidth, inner.height);
            Rect midRect = new Rect(leftRect.xMax + gap, inner.y, midWidth, inner.height);
            Rect rightRect = new Rect(midRect.xMax + gap, inner.y, rightWidth, inner.height);

            DrawScrollableCapacitorPanel(leftRect, ritual);
            DrawScrollableModulePanel(midRect);
            DrawScrollableRitualPreviewPanel(rightRect, ritual);
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
            float rowHeight = 24f;
            int rowIndex = 0;
            foreach (AbyssalCircleCapacitorBay bay in AbyssalCircleCapacitorUtility.GetOrderedBays())
            {
                Rect rowRect = new Rect(rect.x, rowsY + rowIndex * (rowHeight + 4f), rect.width, rowHeight);
                DrawCapacitorSlotRow(rowRect, bay);
                rowIndex++;
            }

            AbyssalCircleCapacitorRitualUtility.CapacitorReadinessReport report = AbyssalCircleCapacitorRitualUtility.CreateReadinessReport(circle, ritual);
            float summaryY = rowsY + rowIndex * (rowHeight + 4f) + 8f;
            Rect summaryRect = new Rect(rect.x, summaryY, rect.width, rect.height - summaryY);
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_State".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetSupportStateLabel(report), true, false);
            DrawSummaryLine(summaryRect, ref summaryY, AbyssalCircleCapacitorRitualUtility.GetSupportDetailText(report), true, true);
            DrawSummaryLine(summaryRect, ref summaryY, AbyssalCircleCapacitorUtility.GetChargeReadout(circle));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_Lattice".Translate() + ": " + AbyssalCircleCapacitorUtility.GetLatticeProfileLabel(circle));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_Startup".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetStartupReadout(report));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_Reserve".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetReserveReadout(report));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_Feed".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetThroughputRequirementReadout(report));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_Grid".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetGridSmoothingReadout(circle));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_Leakage".Translate() + ": " + AbyssalCircleCapacitorUtility.GetLeakageValueReadout(circle));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_Flow".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetChargeFlowReadout(report));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_Mode".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetOperationalModeSummary(circle, ritual));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CapacitorPanel_Dump".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetEmergencyDumpStatusLabel(circle));
            DrawSummaryLine(summaryRect, ref summaryY, AbyssalCircleCapacitorRitualUtility.GetRitualDemandSummary(ritual), true, true);
        }

        private void DrawCapacitorSlotRow(Rect rect, AbyssalCircleCapacitorBay bay)
        {
            AbyssalCircleCapacitorSlot slot = circle.GetCapacitorSlot(bay);
            ThingDef installedDef = slot?.InstalledThingDef;
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 96f, rect.height);
            GUI.color = installedDef == null ? AbyssalSummoningConsoleArt.TextDimColor : Color.white;
            Widgets.Label(labelRect, AbyssalCircleCapacitorUtility.GetSlotRowText(slot, bay));
            GUI.color = Color.white;
            TooltipHandler.TipRegion(labelRect, AbyssalCircleCapacitorUtility.GetBayTooltip(circle, bay));

            Rect actionRect = new Rect(rect.xMax - 98f, rect.y - 1f, 98f, rect.height + 2f);
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
            float rowHeight = 24f;
            int rowIndex = 0;
            foreach (AbyssalCircleModuleEdge edge in AbyssalCircleModuleUtility.GetOrderedEdges())
            {
                Rect rowRect = new Rect(rect.x, rowsY + rowIndex * (rowHeight + 3f), rect.width, rowHeight);
                DrawModuleSlotRow(rowRect, edge);
                rowIndex++;
            }

            float summaryY = rowsY + rowIndex * (rowHeight + 3f) + 8f;
            Rect summaryRect = new Rect(rect.x, summaryY, rect.width, rect.height - summaryY);
            DrawSummaryLine(summaryRect, ref summaryY, AbyssalSummoningConsoleUtility.GetStabilizerPatternSummary(circle), true, false);
            DrawSummaryLine(summaryRect, ref summaryY, AbyssalSummoningConsoleUtility.GetStabilizerPatternDetail(circle), true, true);
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CircleModulesContainment".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerContainmentBonusDisplay(circle));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CircleModulesHeatDamping".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerHeatDampingDisplay(circle));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CircleModulesResidue".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerResidueSuppressionDisplay(circle));
            DrawSummaryLine(summaryRect, ref summaryY, "ABY_CircleModulesAnomaly".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerAnomalyShieldingDisplay(circle));
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

            float y = rect.y + 28f;
            DrawSummaryLine(rect, ref y, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CirclePreviewHost", "Likely host: {0}", ritual.BossLabel), false, false);
            DrawSummaryLine(rect, ref y, AbyssalSummoningConsoleUtility.GetRitualSubtitle(ritual), true, true);
            DrawSummaryLine(rect, ref y, AbyssalSummoningConsoleUtility.GetRitualDescription(ritual), false, true);

            y += 2f;
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, y, rect.width, 22f), "ABY_CircleConsequencesHeader".Translate());
            y += 28f;
            DrawSummaryLine(rect, ref y, AbyssalSummoningConsoleUtility.GetRitualSideEffectHint(ritual), true, true);
        }

        private float GetCapacitorContentHeight(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            int bayCount = AbyssalCircleCapacitorUtility.GetOrderedBays().Count();
            float rowsHeight = bayCount * 28f;
            return 48f + rowsHeight + 292f;
        }

        private float GetModuleContentHeight()
        {
            int edgeCount = AbyssalCircleModuleUtility.GetOrderedEdges().Count();
            float rowsHeight = edgeCount * 27f;
            return 48f + rowsHeight + 168f;
        }

        private float GetRitualPreviewContentHeight(AbyssalSummoningConsoleUtility.RitualDefinition ritual, float width)
        {
            Text.Font = GameFont.Tiny;
            float descriptionHeight = Text.CalcHeight(AbyssalSummoningConsoleUtility.GetRitualDescription(ritual), width);
            float sideEffectHeight = Text.CalcHeight(AbyssalSummoningConsoleUtility.GetRitualSideEffectHint(ritual), width);
            Text.Font = GameFont.Small;
            return 140f + descriptionHeight + sideEffectHeight;
        }

        private void DrawModuleSlotRow(Rect rect, AbyssalCircleModuleEdge edge)
        {
            AbyssalCircleModuleSlot slot = circle.GetModuleSlot(edge);
            ThingDef installedDef = slot?.InstalledThingDef;
            string edgeLabel = AbyssalCircleModuleUtility.GetEdgeLabel(edge);
            string slotLabel = installedDef == null ? "ABY_CircleModuleSlotEmpty".Translate(edgeLabel) : "ABY_CircleModuleSlotInstalled".Translate(edgeLabel, installedDef.label.CapitalizeFirst(), AbyssalCircleModuleUtility.GetTierLabel(installedDef));
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 98f, rect.height);
            GUI.color = installedDef == null ? AbyssalSummoningConsoleArt.TextDimColor : Color.white;
            Widgets.Label(labelRect, slotLabel);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(rect, AbyssalSummoningConsoleUtility.GetModuleSlotTooltip(circle, edge));
            Rect actionRect = new Rect(rect.xMax - 96f, rect.y - 1f, 96f, rect.height + 2f);
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

        private void ConfirmAndAssign(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            string confirmText = circle.CapacitorOverchannelEnabled && AbyssalCircleCapacitorRitualUtility.WouldForceStart(circle, ritual)
                ? "ABY_CapacitorConfirm_ForcedInvocation".Translate(AbyssalSummoningConsoleUtility.GetRitualLabel(ritual))
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleConfirmInvocation", "Assign a prepared sigil and order a colonist to begin {0}? This starts a hostile breach sequence.", AbyssalSummoningConsoleUtility.GetRitualLabel(ritual));
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(confirmText, delegate
            {
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
