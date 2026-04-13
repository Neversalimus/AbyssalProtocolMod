using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class ITab_AbyssalForgeBills : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(640f, 260f);

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

            Rect canvas = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Rect headerRect = new Rect(canvas.x, canvas.y, canvas.width, 56f);
            Rect statusRect = new Rect(canvas.x, headerRect.yMax + 8f, 365f, 126f);
            Rect offerRect = new Rect(statusRect.xMax + 8f, headerRect.yMax + 8f, canvas.width - statusRect.width - 8f, 126f);
            Rect openRect = new Rect(canvas.x, statusRect.yMax + 10f, canvas.width, 46f);

            MapComponent_AbyssalForgeProgress progress = SelForge.ProgressComponent;
            if (progress == null)
            {
                return;
            }

            AbyssalForgeConsoleArt.DrawBackground(canvas);
            AbyssalForgeConsoleArt.DrawHeader(headerRect, "ABY_ForgePanelHeader".Translate(), "ABY_ForgeOverviewSubtitle".Translate());
            DrawStatusPanel(statusRect, progress);
            DrawOfferPanel(offerRect, progress);

            AbyssalForgeConsoleArt.DrawPanel(openRect, true);
            if (Widgets.ButtonText(openRect.ContractedBy(10f), "ABY_ForgeOpenConsole".Translate()))
            {
                Find.WindowStack.Add(new Window_AbyssalForgeConsole(SelForge));
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
        }

        private void DrawStatusPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, false);
            Rect inner = rect.ContractedBy(12f);
            int nextUnlock = progress.GetNextUnlockResidue();
            RecipeDef nextRecipe = progress.GetNextUnlockRecipe();
            string nextLine = nextUnlock > 0
                ? "ABY_ForgeNextPattern".Translate(nextUnlock, nextRecipe != null ? AbyssalForgeProgressUtility.GetRecipeDisplayLabel(nextRecipe) : "?")
                : "ABY_ForgeNextPatternDone".Translate();

            AbyssalForgeConsoleArt.DrawMetric(new Rect(inner.x, inner.y, inner.width * 0.48f, 42f), "ABY_ForgeMetricResidue".Translate(), progress.TotalResidueOffered.ToString());
            AbyssalForgeConsoleArt.DrawMetric(new Rect(inner.x + inner.width * 0.52f, inner.y, inner.width * 0.48f, 42f), "ABY_ForgeMetricAvailable".Translate(), progress.CountAvailableResidue().ToString());
            AbyssalForgeConsoleArt.DrawMetric(new Rect(inner.x, inner.y + 46f, inner.width * 0.48f, 42f), "ABY_ForgeMetricAttunement".Translate(), ("ABY_AttunementTier_" + progress.GetCurrentAttunementTier(false)).Translate());
            AbyssalForgeConsoleArt.DrawMetric(new Rect(inner.x + inner.width * 0.52f, inner.y + 46f, inner.width * 0.48f, 42f), "ABY_ForgeMetricPower".Translate(), SelForge.IsPowerActive ? "ABY_ForgePowerOnlineShort".Translate() : "ABY_ForgePowerOfflineShort".Translate());

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 92f, inner.width, 24f), nextLine);
            GUI.color = Color.white;
        }

        private void DrawOfferPanel(Rect rect, MapComponent_AbyssalForgeProgress progress)
        {
            AbyssalForgeConsoleArt.DrawPanel(rect, true);
            Rect inner = rect.ContractedBy(12f);
            int availableResidue = progress.CountAvailableResidue();

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 22f), "ABY_ForgeOfferHeader".Translate());
            GUI.color = Color.white;

            bool enabled = availableResidue > 0;
            bool oldEnabled = GUI.enabled;
            GUI.enabled = enabled;

            float buttonWidth = (inner.width - 8f) / 2f;
            if (Widgets.ButtonText(new Rect(inner.x, inner.y + 26f, buttonWidth, 28f), "ABY_ForgeOfferAmount".Translate(10)))
            {
                TryOfferResidue(10);
            }

            if (Widgets.ButtonText(new Rect(inner.x + buttonWidth + 8f, inner.y + 26f, buttonWidth, 28f), "ABY_ForgeOfferAmount".Translate(50)))
            {
                TryOfferResidue(50);
            }

            if (Widgets.ButtonText(new Rect(inner.x, inner.y + 60f, inner.width, 30f), "ABY_ForgeOfferAll".Translate(availableResidue)))
            {
                TryOfferResidue(availableResidue);
            }

            GUI.enabled = oldEnabled;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, inner.y + 94f, inner.width, 22f), enabled ? "ABY_ForgeOverviewHint".Translate() : "ABY_ForgeOfferNoneAvailable".Translate());
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
