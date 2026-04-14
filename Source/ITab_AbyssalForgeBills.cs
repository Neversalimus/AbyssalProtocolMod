using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class ITab_AbyssalForgeBills : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(648f, 284f);

        protected Building_AbyssalForge SelForge => (Building_AbyssalForge)SelThing;

        public ITab_AbyssalForgeBills()
        {
            size = WinSize;
            labelKey = "ABY_ForgeTabLabel";
            tutorTag = "Bills";
        }

        protected override void FillTab()
        {
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);

            Rect canvas = new Rect(0f, 0f, size.x, size.y).ContractedBy(8f);
            Rect headerRect = new Rect(canvas.x, canvas.y, canvas.width, 64f);
            Rect statusRect = new Rect(canvas.x, headerRect.yMax + 8f, 372f, 148f);
            Rect offerRect = new Rect(statusRect.xMax + 8f, headerRect.yMax + 8f, canvas.width - statusRect.width - 8f, 148f);
            Rect openRect = new Rect(canvas.x, statusRect.yMax + 10f, canvas.width, 46f);

            MapComponent_AbyssalForgeProgress progress = SelForge.ProgressComponent;
            if (progress == null)
            {
                return;
            }

            AbyssalForgeConsoleArt.ReducedEffects = progress.ReducedVisualEffects;
            AbyssalForgeConsoleArt.DrawBackground(canvas);
            AbyssalForgeConsoleArt.DrawHeader(headerRect, "ABY_ForgePanelHeader".Translate(), "ABY_ForgeOverviewSubtitleShort".Translate(), progress.HasRecentUnlocks);
            DrawStatusPanel(statusRect, progress);
            DrawOfferPanel(offerRect, progress);

            if (AbyssalStyledWidgets.TextButton(openRect.ContractedBy(8f), "ABY_ForgeOpenConsole".Translate(), true, true))
            {
                Find.WindowStack.Add(new Window_AbyssalForgeConsole(SelForge));
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }

            if (progress.HasRecentUnlocks)
            {
                Rect tagRect = new Rect(openRect.xMax - 82f, openRect.y - 9f, 70f, 18f);
                AbyssalForgeConsoleArt.DrawTag(tagRect, "ABY_ForgePatternNew".Translate(), true);
                TooltipHandler.TipRegion(tagRect, "ABY_ForgeOpenConsoleDesc".Translate());
            }
        }

        private void DrawStatusPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(12f);
            float metricWidth = (inner.width - 16f) / 2f;
            int nextUnlock = progress.GetNextUnlockResidue();
            RecipeDef nextRecipe = progress.GetNextUnlockRecipe();
            string nextLine = nextUnlock > 0
                ? "ABY_ForgeNextPattern".Translate(nextUnlock, nextRecipe != null ? AbyssalForgeProgressUtility.GetRecipeDisplayLabel(nextRecipe) : "?")
                : "ABY_ForgeNextPatternDone".Translate();

            Rect residueRect = new Rect(inner.x, inner.y, metricWidth, 42f);
            Rect availableRect = new Rect(inner.x + metricWidth + 16f, inner.y, metricWidth, 42f);
            Rect attunementRect = new Rect(inner.x, inner.y + 48f, metricWidth, 42f);
            Rect powerRect = new Rect(inner.x + metricWidth + 16f, inner.y + 48f, metricWidth, 42f);

            AbyssalForgeConsoleArt.DrawMetric(residueRect, "ABY_ForgeMetricResidue".Translate(), progress.TotalResidueOffered.ToString());
            AbyssalForgeConsoleArt.DrawMetric(availableRect, "ABY_ForgeMetricAvailable".Translate(), progress.CountAvailableResidue().ToString());
            AbyssalForgeConsoleArt.DrawMetric(attunementRect, "ABY_ForgeMetricAttunement".Translate(), ("ABY_AttunementTier_" + progress.GetCurrentAttunementTier(false)).Translate());
            AbyssalForgeConsoleArt.DrawMetric(powerRect, "ABY_ForgeMetricPower".Translate(), SelForge.IsPowerActive ? "ABY_ForgePowerOnlineShort".Translate() : "ABY_ForgePowerOfflineShort".Translate());

            TooltipHandler.TipRegion(attunementRect, AbyssalForgeProgressUtility.GetAttunementTooltip(progress.GetCurrentAttunementTier(false), progress.HasPoweredForge()));

            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
            Rect nextRect = new Rect(inner.x, inner.y + 96f, inner.width, inner.height - 96f);
            Widgets.Label(nextRect, nextLine);
            GUI.color = Color.white;
        }

        private void DrawOfferPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(12f);
            int availableResidue = progress.CountAvailableResidue();

            GUI.color = Color.white;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeOfferHeader".Translate());
            GUI.color = Color.white;

            bool enabled = availableResidue > 0;

            float buttonWidth = (inner.width - 8f) / 2f;
            if (AbyssalStyledWidgets.TextButton(new Rect(inner.x, inner.y + 30f, buttonWidth, 30f), "ABY_ForgeOfferAmount".Translate(10), enabled))
            {
                TryOfferResidue(10);
            }

            if (AbyssalStyledWidgets.TextButton(new Rect(inner.x + buttonWidth + 8f, inner.y + 30f, buttonWidth, 30f), "ABY_ForgeOfferAmount".Translate(50), enabled))
            {
                TryOfferResidue(50);
            }

            if (AbyssalStyledWidgets.TextButton(new Rect(inner.x, inner.y + 66f, inner.width, 32f), "ABY_ForgeOfferAll".Translate(availableResidue), enabled))
            {
                TryOfferResidue(availableResidue);
            }

            GUI.color = AbyssalForgeConsoleArt.TextSoftColor;
            Widgets.Label(new Rect(inner.x, inner.y + 104f, inner.width, inner.height - 104f), enabled ? "ABY_ForgeOverviewHintCompact".Translate() : "ABY_ForgeOfferNoneAvailable".Translate());
            GUI.color = Color.white;
        }

        private void TryOfferResidue(int requestedAmount)
        {
            int consumed = SelForge.OfferResidue(requestedAmount);
            if (consumed > 0)
            {
                SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
            }
            else
            {
                Messages.Message("ABY_ForgeOfferNoneAvailable".Translate(), SelForge, MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
