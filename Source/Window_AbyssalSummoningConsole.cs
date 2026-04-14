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
        private string selectedRitualId = "archon_beast";
        private Vector2 ritualScrollPosition = Vector2.zero;

        public Window_AbyssalSummoningConsole(Building_AbyssalSummoningCircle circle)
        {
            this.circle = circle;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseX = true;
            draggable = true;
            forcePause = false;
            preventCameraMotion = false;
            onlyOneOfTypeAllowed = false;
            resizeable = false;
        }

        public override Vector2 InitialSize => new Vector2(1180f, 760f);

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
            Rect ritualsRect = new Rect(inRect.x, stripRect.yMax + 10f, 468f, 430f);
            Rect controlRect = new Rect(ritualsRect.xMax + 10f, stripRect.yMax + 10f, inRect.width - ritualsRect.width - 10f, 236f);
            Rect statusRect = new Rect(ritualsRect.xMax + 10f, controlRect.yMax + 10f, inRect.width - ritualsRect.width - 10f, 184f);
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
                ? "ABY_CircleConsoleSubtitleActive".Translate(circle.GetCurrentPhaseTranslated())
                : "ABY_CircleConsoleSubtitle".Translate();
            AbyssalSummoningConsoleArt.DrawHeader(rect, "ABY_CircleConsoleTitle".Translate(), subtitle, circle.RitualActive);
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
            float cardHeight = 156f;
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
            Rect selectRect = new Rect(rect.xMax - 104f, rect.y + rect.height - 30f, 92f, 24f);

            Widgets.Label(titleRect, ritual.LabelKey.Translate());
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(subRect, ritual.SubtitleKey.Translate());
            GUI.color = Color.white;
            Widgets.Label(descRect, ritual.DescriptionKey.Translate());
            GUI.color = new Color(1f, 0.76f, 0.58f, 1f);
            Widgets.Label(metaRect, "ABY_CircleRitualMeta".Translate(AbyssalSummoningConsoleUtility.CountSigilsOnMap(circle.Map, ritual), ritual.SpawnPoints));
            GUI.color = Color.white;

            if (AbyssalStyledWidgets.TextButton(selectRect, selected ? "ABY_CircleSelected".Translate() : "ABY_CircleSelect".Translate(), !selected, selected))
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
            Rect reducedRect = new Rect(inner.x, inner.y + 112f, inner.width, 24f);
            if (AbyssalStyledWidgets.TextButton(reducedRect, reduced ? "ABY_ForgeReducedEffectsOn".Translate() : "ABY_ForgeReducedEffectsOff".Translate(), true, reduced, null, "ABY_CircleReducedEffectsDesc".Translate()))
            {
                circle.SetReducedConsoleEffects(!reduced);
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }

            Rect openRect = new Rect(inner.x, inner.y + 150f, inner.width, 30f);
            Rect invokeRect = new Rect(inner.x, inner.y + 188f, inner.width, 34f);
            if (AbyssalStyledWidgets.TextButton(openRect, "ABY_CircleCommand_JumpToSigil".Translate()))
            {
                JumpToSigil(ritual);
            }

            if (AbyssalStyledWidgets.TextButton(invokeRect, "ABY_CircleCommand_AssignSigil".Translate(), !circle.RitualActive, true))
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
            Rect inner = rect.ContractedBy(12f);

            Rect leftRect = new Rect(inner.x, inner.y, inner.width * 0.38f, inner.height);
            Rect midRect = new Rect(leftRect.xMax + 12f, inner.y, inner.width * 0.28f, inner.height);
            Rect rightRect = new Rect(midRect.xMax + 12f, inner.y, inner.width - leftRect.width - midRect.width - 24f, inner.height);

            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(leftRect.x, leftRect.y, leftRect.width, 22f), "ABY_CirclePreviewHeader".Translate());
            Widgets.Label(new Rect(leftRect.x, leftRect.y + 28f, leftRect.width, 22f), "ABY_CirclePreviewHost".Translate(ritual.BossLabel));
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(leftRect.x, leftRect.y + 52f, leftRect.width, 18f), ritual.SubtitleKey.Translate());
            GUI.color = Color.white;
            Widgets.Label(new Rect(leftRect.x, leftRect.y + 76f, leftRect.width, leftRect.height - 76f), ritual.DescriptionKey.Translate());

            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(midRect.x, midRect.y, midRect.width, 22f), "ABY_CircleRewardsHeader".Translate());
            Widgets.Label(new Rect(midRect.x, midRect.y + 28f, midRect.width, midRect.height - 28f), ritual.RewardHintKey.Translate());

            AbyssalSummoningConsoleArt.DrawSectionTitle(new Rect(rightRect.x, rightRect.y, rightRect.width, 22f), "ABY_CircleConsequencesHeader".Translate());
            Widgets.Label(new Rect(rightRect.x, rightRect.y + 28f, rightRect.width, rightRect.height - 28f), ritual.SideEffectHintKey.Translate());
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
            string confirmText = "ABY_CircleConfirmInvocation".Translate(ritual.LabelKey.Translate());
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(confirmText, delegate
            {
                if (AbyssalSummoningConsoleUtility.TryAssignInvocation(circle, ritual, out string failReason))
                {
                    Messages.Message("ABY_CircleAssignStarted".Translate(), MessageTypeDefOf.PositiveEvent, false);
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
