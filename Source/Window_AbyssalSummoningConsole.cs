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

        public override Vector2 InitialSize => new Vector2(1210f, 800f);

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
            Widgets.Label(metaRect, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleRitualMeta", "Sigils on map: {0}   •   Threat budget: {1}", AbyssalSummoningConsoleUtility.CountSigilsOnMap(circle.Map, ritual), ritual.SpawnPoints));
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
            Widgets.Label(new Rect(inner.x, inner.y + 84f, inner.width, 20f), circle.GetCurrentStatusLine());

            float colWidth = inner.width / 4f;
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x + colWidth * 0f, inner.y + 108f, colWidth - 4f, 18f), "ABY_CircleTelemetry_Heat".Translate());
            Widgets.Label(new Rect(inner.x + colWidth * 1f, inner.y + 108f, colWidth - 4f, 18f), "ABY_CircleTelemetry_Containment".Translate());
            Widgets.Label(new Rect(inner.x + colWidth * 2f, inner.y + 108f, colWidth - 4f, 18f), "ABY_CircleTelemetry_Contamination".Translate());
            Widgets.Label(new Rect(inner.x + colWidth * 3f, inner.y + 108f, colWidth - 4f, 18f), "ABY_CircleTelemetry_Delta".Translate());
            GUI.color = Color.white;
            Widgets.Label(new Rect(inner.x + colWidth * 0f, inner.y + 126f, colWidth - 4f, 20f), AbyssalSummoningConsoleUtility.GetHeatDisplay(circle));
            Widgets.Label(new Rect(inner.x + colWidth * 1f, inner.y + 126f, colWidth - 4f, 20f), AbyssalSummoningConsoleUtility.GetContainmentDisplay(circle));
            Widgets.Label(new Rect(inner.x + colWidth * 2f, inner.y + 126f, colWidth - 4f, 20f), AbyssalSummoningConsoleUtility.GetContaminationDisplay(circle));
            Widgets.Label(new Rect(inner.x + colWidth * 3f, inner.y + 126f, colWidth - 4f, 20f), AbyssalSummoningConsoleUtility.GetProjectedDeltaDisplay(circle, ritual));

            bool reduced = circle.ReducedConsoleEffects;
            bool newReduced = reduced;
            Rect reducedRect = new Rect(inner.x, inner.y + 146f, 180f, 24f);
            Widgets.CheckboxLabeled(reducedRect, "ABY_CircleReducedEffects".Translate(), ref newReduced, false, null, null, false);
            if (newReduced != reduced)
            {
                circle.SetReducedConsoleEffects(newReduced);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
            TooltipHandler.TipRegion(reducedRect, AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleReducedEffectsDesc", "Softens header sweeps, seal rotation, and other animated accents inside the summoning console."));

            Rect openRect = new Rect(inner.x, inner.y + 174f, inner.width, 28f);
            Rect invokeRect = new Rect(inner.x, inner.y + 206f, inner.width, 30f);
            if (AbyssalStyledWidgets.TextButton(openRect, AbyssalSummoningConsoleUtility.GetJumpToSigilLabel()))
            {
                JumpToSigil(ritual);
            }

            if (AbyssalStyledWidgets.TextButton(invokeRect, AbyssalSummoningConsoleUtility.GetAssignSigilLabel(), !circle.RitualActive, true))
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
                Rect lineRect = new Rect(inner.x, inner.y + 28f + i * 18f, inner.width, 18f);
                Rect valueRect = new Rect(lineRect.x + inner.width * 0.44f, lineRect.y, inner.width * 0.56f, lineRect.height);
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                Widgets.Label(new Rect(lineRect.x, lineRect.y, inner.width * 0.42f, lineRect.height), entries[i].Label);
                GUI.color = entries[i].Satisfied ? new Color(0.72f, 1f, 0.74f, 1f) : new Color(1f, 0.60f, 0.54f, 1f);
                Widgets.Label(valueRect, entries[i].Value);
                GUI.color = Color.white;
            }

            float buttonsY = inner.y + 140f;
            Rect purgeRect = new Rect(inner.x, buttonsY, inner.width * 0.48f, 24f);
            Rect ventRect = new Rect(inner.x + inner.width * 0.52f, buttonsY, inner.width * 0.48f, 24f);

            bool canPurge = circle.CanPurgeHeat(out string purgeReason);
            string purgeTooltip = canPurge
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CirclePurgeTooltip", "Bleed local circle heat. Faster recovery, but some residue is dumped into the map layer.")
                : purgeReason;
            if (AbyssalStyledWidgets.TextButton(purgeRect, "ABY_CircleCommand_PurgeHeat".Translate(), canPurge, false, null, purgeTooltip))
            {
                if (circle.TryPurgeHeat(out string failReason))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                else if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }
            }

            bool canVent = circle.CanVentContamination(out string ventReason);
            string ventTooltip = canVent
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleVentTooltip", "Vent residue pressure from the map layer. Safer future breaches, but it kicks heat back into the circle.")
                : ventReason;
            if (AbyssalStyledWidgets.TextButton(ventRect, "ABY_CircleCommand_VentResidue".Translate(), canVent, false, null, ventTooltip))
            {
                if (circle.TryVentContamination(out string failReason))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                }
                else if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }
            }
        }

        private void DrawPreviewPanel(Rect rect, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            AbyssalSummoningConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(12f);

            Rect leftRect = new Rect(inner.x, inner.y, inner.width * 0.38f, inner.height);
            Rect midRect = new Rect(leftRect.xMax + 12f, inner.y, inner.width * 0.28f, inner.height);
            Rect rightRect = new Rect(midRect.xMax + 12f, inner.y, inner.width - leftRect.width - midRect.width - 24f, inner.height);

            DrawModulePanel(leftRect);

            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(midRect.x, midRect.y, midRect.width, 22f), "ABY_CircleRewardsHeader".Translate());
            Widgets.Label(new Rect(midRect.x, midRect.y + 28f, midRect.width, midRect.height - 28f), AbyssalSummoningConsoleUtility.GetRitualRewardHint(ritual));

            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rightRect.x, rightRect.y, rightRect.width, 22f), "ABY_CircleConsequencesHeader".Translate());
            Widgets.Label(new Rect(rightRect.x, rightRect.y + 28f, rightRect.width, rightRect.height - 28f), AbyssalSummoningConsoleUtility.GetProjectedInstabilityBlock(circle, ritual) + "\n\n" + AbyssalSummoningConsoleUtility.GetRitualSideEffectHint(ritual));
        }

        private void DrawModulePanel(Rect rect)
        {
            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rect.x, rect.y, rect.width, 22f), "ABY_CircleModulesHeader".Translate());
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, rect.y + 24f, rect.width, 18f), "ABY_CircleInspect_Stabilizers".Translate(circle.InstalledStabilizerCount, circle.ModuleSlots.Count));
            GUI.color = Color.white;

            float rowsY = rect.y + 42f;
            float rowHeight = 22f;
            int rowIndex = 0;
            foreach (AbyssalCircleModuleEdge edge in AbyssalCircleModuleUtility.GetOrderedEdges())
            {
                Rect rowRect = new Rect(rect.x, rowsY + rowIndex * (rowHeight + 2f), rect.width, rowHeight);
                DrawModuleSlotRow(rowRect, edge);
                rowIndex++;
            }

            float summaryY = rowsY + rowIndex * (rowHeight + 2f) + 2f;
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, summaryY, rect.width, 16f), AbyssalSummoningConsoleUtility.GetStabilizerPatternSummary(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 14f, rect.width, 30f), AbyssalSummoningConsoleUtility.GetStabilizerPatternDetail(circle));
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x, summaryY + 40f, rect.width * 0.5f, 16f), "ABY_CircleModulesContainment".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerContainmentBonusDisplay(circle));
            Widgets.Label(new Rect(rect.x + rect.width * 0.52f, summaryY + 40f, rect.width * 0.48f, 16f), "ABY_CircleModulesHeatDamping".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerHeatDampingDisplay(circle));
            Widgets.Label(new Rect(rect.x, summaryY + 56f, rect.width * 0.5f, 16f), "ABY_CircleModulesResidue".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerResidueSuppressionDisplay(circle));
            Widgets.Label(new Rect(rect.x + rect.width * 0.52f, summaryY + 56f, rect.width * 0.48f, 16f), "ABY_CircleModulesAnomaly".Translate() + ": " + AbyssalSummoningConsoleUtility.GetStabilizerAnomalyShieldingDisplay(circle));
        }

        private void DrawModuleSlotRow(Rect rect, AbyssalCircleModuleEdge edge)
        {
            AbyssalCircleModuleSlot slot = circle.GetModuleSlot(edge);
            ThingDef installedDef = slot?.InstalledThingDef;
            string edgeLabel = AbyssalCircleModuleUtility.GetEdgeLabel(edge);
            string slotLabel = installedDef == null
                ? "ABY_CircleModuleSlotEmpty".Translate(edgeLabel)
                : "ABY_CircleModuleSlotInstalled".Translate(edgeLabel, installedDef.label.CapitalizeFirst(), AbyssalCircleModuleUtility.GetTierLabel(installedDef));

            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 138f, rect.height);
            GUI.color = installedDef == null ? AbyssalSummoningConsoleArt.TextDimColor : Color.white;
            Widgets.Label(labelRect, slotLabel);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(rect, AbyssalSummoningConsoleUtility.GetModuleSlotTooltip(circle, edge));

            if (installedDef == null)
            {
                Rect installRect = new Rect(rect.xMax - 82f, rect.y, 82f, rect.height);
                if (AbyssalStyledWidgets.TextButton(installRect, "ABY_CircleModuleCommand_Install".Translate(), !circle.RitualActive, false))
                {
                    OpenInstallMenu(edge);
                }
            }
            else
            {
                Rect removeRect = new Rect(rect.xMax - 82f, rect.y, 82f, rect.height);
                if (AbyssalStyledWidgets.TextButton(removeRect, "ABY_CircleModuleCommand_Remove".Translate(), !circle.RitualActive, false))
                {
                    TryAssignRemove(edge);
                }
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

        private void JumpToSigil(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            Thing sigil = AbyssalSummoningConsoleUtility.FindBestSigil(circle, ritual, out string failReason);
            if (sigil != null)
            {
                CameraJumper.TryJumpAndSelect(sigil);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
            else if (!failReason.NullOrEmpty())
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
            }
        }

        private void ConfirmAndAssign(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            string confirmText = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleConfirmInvocation", "Assign a prepared sigil and order a colonist to begin {0}? This starts a hostile breach sequence.", AbyssalSummoningConsoleUtility.GetRitualLabel(ritual));
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
