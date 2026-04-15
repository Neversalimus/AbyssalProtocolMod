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
            Rect ritualsRect = new Rect(inRect.x, stripRect.yMax + 10f, 500f, 452f);
            Rect controlRect = new Rect(ritualsRect.xMax + 10f, stripRect.yMax + 10f, inRect.width - ritualsRect.width - 10f, 248f);
            Rect statusRect = new Rect(ritualsRect.xMax + 10f, controlRect.yMax + 10f, inRect.width - ritualsRect.width - 10f, 194f);
            Rect previewRect = new Rect(inRect.x, ritualsRect.yMax + 10f, inRect.width, inRect.height - ritualsRect.yMax - 10f);

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
            Widgets.Label(metaRect, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleRitualMeta", "Sigils on map: {0}   •   Threat budget: {1}", AbyssalSummoningConsoleUtility.CountAvailableSigils(circle, ritual), ritual.SpawnPoints));
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
            Widgets.Label(new Rect(inner.x, inner.y + 64f, inner.width, 20f), "ABY_CircleControlState".Translate());
            GUI.color = Color.white;
            Widgets.Label(new Rect(inner.x, inner.y + 84f, inner.width, 22f), circle.GetCurrentStatusLine());

            bool reduced = circle.ReducedConsoleEffects;
            bool newReduced = reduced;
            Rect reducedRect = new Rect(inner.x, inner.y + 112f, 180f, 24f);
            Widgets.CheckboxLabeled(reducedRect, "ABY_CircleReducedEffects".Translate(), ref newReduced, false, null, null, false);
            if (newReduced != reduced)
            {
                circle.SetReducedConsoleEffects(newReduced);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
            TooltipHandler.TipRegion(reducedRect, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleReducedEffectsDesc", "Softens header sweeps, seal rotation, and other animated accents inside the summoning console."));

            bool overchannel = circle.CapacitorOverchannelEnabled;
            bool newOverchannel = overchannel;
            Rect overchannelRect = new Rect(inner.x, inner.y + 128f, inner.width, 24f);
            Widgets.CheckboxLabeled(overchannelRect, "ABY_CapacitorControl_Overchannel".Translate(), ref newOverchannel, false, null, null, false);
            if (newOverchannel != overchannel)
            {
                circle.SetCapacitorOverchannelEnabled(newOverchannel);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
            TooltipHandler.TipRegion(overchannelRect, "ABY_CapacitorControl_OverchannelDesc".Translate());

            bool dump = circle.CapacitorEmergencyDumpEnabled;
            bool newDump = dump;
            Rect dumpRect = new Rect(inner.x, inner.y + 148f, inner.width, 24f);
            Widgets.CheckboxLabeled(dumpRect, "ABY_CapacitorControl_Dump".Translate(), ref newDump, false, null, null, false);
            if (newDump != dump)
            {
                circle.SetCapacitorEmergencyDumpEnabled(newDump);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
            TooltipHandler.TipRegion(dumpRect, "ABY_CapacitorControl_DumpDesc".Translate());

            Rect openRect = new Rect(inner.x, inner.y + 174f, inner.width, 22f);
            Rect invokeRect = new Rect(inner.x, inner.y + 200f, inner.width, 24f);
            if (AbyssalStyledWidgets.TextButton(openRect, AbyssalSummoningConsoleUtility.GetJumpToSigilLabel()))
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
            float leftWidth = Mathf.Round(inner.width * 0.38f);
            float midWidth = Mathf.Round(inner.width * 0.26f);
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
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, summaryY, rect.width, 16f), "ABY_CapacitorPanel_State".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetSupportStateLabel(report));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, summaryY + 16f, rect.width, 34f), AbyssalCircleCapacitorRitualUtility.GetSupportDetailText(report));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x, summaryY + 52f, rect.width, 16f), AbyssalCircleCapacitorUtility.GetChargeReadout(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 68f, rect.width, 16f), "ABY_CapacitorPanel_Lattice".Translate() + ": " + AbyssalCircleCapacitorUtility.GetLatticeProfileLabel(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 84f, rect.width, 16f), "ABY_CapacitorPanel_Startup".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetStartupReadout(report));
            Widgets.Label(new Rect(rect.x, summaryY + 100f, rect.width, 16f), "ABY_CapacitorPanel_Reserve".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetReserveReadout(report));
            Widgets.Label(new Rect(rect.x, summaryY + 116f, rect.width, 16f), "ABY_CapacitorPanel_Feed".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetThroughputRequirementReadout(report));
            Widgets.Label(new Rect(rect.x, summaryY + 132f, rect.width, 16f), "ABY_CapacitorPanel_Grid".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetGridSmoothingReadout(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 148f, rect.width, 16f), "ABY_CapacitorPanel_Leakage".Translate() + ": " + AbyssalCircleCapacitorUtility.GetLeakageValueReadout(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 164f, rect.width, 16f), "ABY_CapacitorPanel_Flow".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetChargeFlowReadout(report));
            Widgets.Label(new Rect(rect.x, summaryY + 180f, rect.width, 16f), "ABY_CapacitorPanel_Mode".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetOperationalModeSummary(circle, ritual));
            Widgets.Label(new Rect(rect.x, summaryY + 196f, rect.width, 16f), "ABY_CapacitorPanel_Dump".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetEmergencyDumpStatusLabel(circle));
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, summaryY + 216f, rect.width, 36f), AbyssalCircleCapacitorRitualUtility.GetRitualDemandSummary(ritual));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
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
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, summaryY, rect.width, 16f), AbyssalSummoningConsoleUtility.GetStabilizerPatternSummary(circle));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, summaryY + 14f, rect.width, 36f), AbyssalSummoningConsoleUtility.GetStabilizerPatternDetail(circle));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x, summaryY + 54f, rect.width, 16f), "ABY_CircleModulesContainment".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerContainmentBonusDisplay(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 70f, rect.width, 16f), "ABY_CircleModulesHeatDamping".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerHeatDampingDisplay(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 86f, rect.width, 16f), "ABY_CircleModulesResidue".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerResidueSuppressionDisplay(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 102f, rect.width, 16f), "ABY_CircleModulesAnomaly".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerAnomalyShieldingDisplay(circle));
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
            Widgets.Label(new Rect(rect.x, rect.y + 52f, rect.width, 18f), AbyssalSummoningConsoleUtility.GetRitualSubtitle(ritual));
            GUI.color = Color.white;

            Text.Font = GameFont.Tiny;
            float descriptionHeight = Text.CalcHeight(AbyssalSummoningConsoleUtility.GetRitualDescription(ritual), rect.width);
            Widgets.Label(new Rect(rect.x, rect.y + 76f, rect.width, descriptionHeight), AbyssalSummoningConsoleUtility.GetRitualDescription(ritual));

            float consequencesY = rect.y + 84f + descriptionHeight;
            Text.Font = GameFont.Small;
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, consequencesY, rect.width, 22f), "ABY_CircleConsequencesHeader".Translate());
            Text.Font = GameFont.Tiny;
            string sideEffectText = AbyssalSummoningConsoleUtility.GetRitualSideEffectHint(ritual);
            float sideEffectHeight = Text.CalcHeight(sideEffectText, rect.width);
            Widgets.Label(new Rect(rect.x, consequencesY + 28f, rect.width, sideEffectHeight), sideEffectText);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }

        private float GetCapacitorContentHeight(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            int bayCount = AbyssalCircleCapacitorUtility.GetOrderedBays().Count();
            float rowsHeight = bayCount * 28f;
            return 48f + rowsHeight + 260f;
        }

        private float GetModuleContentHeight()
        {
            int edgeCount = AbyssalCircleModuleUtility.GetOrderedEdges().Count();
            float rowsHeight = edgeCount * 27f;
            return 48f + rowsHeight + 140f;
        }

        private float GetRitualPreviewContentHeight(AbyssalSummoningConsoleUtility.RitualDefinition ritual, float width)
        {
            Text.Font = GameFont.Tiny;
            float descriptionHeight = Text.CalcHeight(AbyssalSummoningConsoleUtility.GetRitualDescription(ritual), width);
            float sideEffectHeight = Text.CalcHeight(AbyssalSummoningConsoleUtility.GetRitualSideEffectHint(ritual), width);
            Text.Font = GameFont.Small;
            return 120f + descriptionHeight + sideEffectHeight;
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
