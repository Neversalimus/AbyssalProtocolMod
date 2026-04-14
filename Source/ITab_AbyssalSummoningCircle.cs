using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    public class ITab_AbyssalSummoningCircle : ITab
    {
        private Building_AbyssalSummoningCircle SelCircle => SelThing as Building_AbyssalSummoningCircle;

        public ITab_AbyssalSummoningCircle()
        {
            size = new Vector2(420f, 288f);
            labelKey = "ABY_CircleTab_Label";
        }

        protected override void FillTab()
        {
            Building_AbyssalSummoningCircle circle = SelCircle;
            if (circle == null || circle.Destroyed || circle.Map == null)
            {
                return;
            }

            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(8f);
            AbyssalSummoningConsoleArt.ReducedEffects = circle.ReducedConsoleEffects;
            AbyssalSummoningConsoleArt.DrawBackground(rect);

            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 52f);
            AbyssalSummoningConsoleArt.DrawHeader(headerRect, "ABY_CircleConsoleTitle".Translate(), "ABY_CircleTab_Subtitle".Translate(), circle.RitualActive);

            AbyssalSummoningConsoleUtility.RitualDefinition ritual = AbyssalSummoningConsoleUtility.GetDefaultRitual();
            Rect leftRect = new Rect(rect.x, headerRect.yMax + 10f, rect.width * 0.52f, 150f);
            Rect rightRect = new Rect(leftRect.xMax + 8f, headerRect.yMax + 10f, rect.width - leftRect.width - 8f, 150f);
            Rect bottomRect = new Rect(rect.x, leftRect.yMax + 10f, rect.width, 48f);

            AbyssalSummoningConsoleArt.DrawPanel(leftRect, false);
            Rect leftInner = leftRect.ContractedBy(10f);
            Widgets.Label(new Rect(leftInner.x, leftInner.y, leftInner.width, 22f), circle.GetCurrentStatusLine());
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(leftInner.x, leftInner.y + 26f, leftInner.width, 18f), "ABY_CircleInspect_Sigils".Translate(AbyssalSummoningConsoleUtility.CountSigilsOnMap(circle.Map, ritual)));
            Widgets.Label(new Rect(leftInner.x, leftInner.y + 46f, leftInner.width, 18f), "ABY_CircleInspect_Readiness".Translate(AbyssalSummoningConsoleUtility.GetShortRequirementSummary(circle, ritual)));
            Widgets.Label(new Rect(leftInner.x, leftInner.y + 66f, leftInner.width, 18f), "ABY_CircleInspect_Risk".Translate(AbyssalSummoningConsoleUtility.GetRiskLabel(AbyssalSummoningConsoleUtility.GetRiskTier(circle, ritual))));
            GUI.color = Color.white;
            Widgets.Label(new Rect(leftInner.x, leftInner.y + 92f, leftInner.width, 50f), "ABY_CircleTab_Hint".Translate());

            AbyssalSummoningConsoleArt.DrawPanel(rightRect, true);
            Rect rightInner = rightRect.ContractedBy(10f);
            AbyssalSummoningConsoleUtility.CircleRiskTier riskTier = AbyssalSummoningConsoleUtility.GetRiskTier(circle, ritual);
            AbyssalSummoningConsoleArt.DrawRiskBar(new Rect(rightInner.x, rightInner.y + 10f, rightInner.width, 24f), AbyssalSummoningConsoleUtility.GetRiskFill(circle, ritual), AbyssalSummoningConsoleUtility.GetRiskLabel(riskTier), AbyssalSummoningConsoleUtility.GetRiskColor(riskTier), circle.RitualActive);

            if (AbyssalStyledWidgets.TextButton(new Rect(rightInner.x, rightInner.y + 50f, rightInner.width, 30f), "ABY_CircleCommand_OpenConsole".Translate(), true, true))
            {
                Find.WindowStack.Add(new Window_AbyssalSummoningConsole(circle));
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }

            bool canInvoke = !circle.RitualActive;
            if (AbyssalStyledWidgets.TextButton(new Rect(rightInner.x, rightInner.y + 88f, rightInner.width, 30f), "ABY_CircleCommand_AssignSigil".Translate(), canInvoke, true))
            {
                TryAssign(circle, ritual);
            }

            AbyssalSummoningConsoleArt.DrawPanel(bottomRect, false);
            Widgets.Label(bottomRect.ContractedBy(10f), "ABY_CircleTab_Footer".Translate());
        }

        private static void TryAssign(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
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
        }
    }
}
