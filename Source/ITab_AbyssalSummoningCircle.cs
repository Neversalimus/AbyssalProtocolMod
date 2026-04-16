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
            size = new Vector2(470f, 332f);
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
            MapComponent_DominionCrisis dominionCrisis = circle.Map?.GetComponent<MapComponent_DominionCrisis>();
            bool dominionActive = dominionCrisis != null && dominionCrisis.IsActive;
            string subtitle = dominionActive
                ? AbyssalSummoningConsoleUtility.GetConsoleSubtitleDominionActive(dominionCrisis.GetPhaseLabel())
                : AbyssalSummoningConsoleUtility.GetCompactSubtitle();
            AbyssalSummoningConsoleArt.DrawHeader(headerRect, AbyssalSummoningConsoleUtility.GetConsoleTitle(), subtitle, circle.RitualActive || dominionActive);

            AbyssalSummoningConsoleUtility.RitualDefinition ritual = AbyssalSummoningConsoleUtility.GetSuggestedRitual(circle);
            Rect leftRect = new Rect(rect.x, headerRect.yMax + 10f, rect.width * 0.54f, 184f);
            Rect rightRect = new Rect(leftRect.xMax + 8f, headerRect.yMax + 10f, rect.width - leftRect.width - 8f, 184f);
            Rect bottomRect = new Rect(rect.x, leftRect.yMax + 8f, rect.width, 42f);

            AbyssalSummoningConsoleArt.DrawPanel(leftRect, false);
            Rect leftInner = leftRect.ContractedBy(10f);
            Widgets.Label(new Rect(leftInner.x, leftInner.y, leftInner.width, 36f), AbyssalSummoningConsoleUtility.GetCompactStatusLine(circle));
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(leftInner.x, leftInner.y + 40f, leftInner.width, 18f), AbyssalSummoningConsoleUtility.GetInspectSigilsText(AbyssalSummoningConsoleUtility.CountAvailableSigils(circle, ritual)));
            Widgets.Label(new Rect(leftInner.x, leftInner.y + 60f, leftInner.width, 18f), AbyssalSummoningConsoleUtility.GetInspectReadinessText(AbyssalSummoningConsoleUtility.GetShortRequirementSummary(circle, ritual)));
            Widgets.Label(new Rect(leftInner.x, leftInner.y + 80f, leftInner.width, 18f), AbyssalSummoningConsoleUtility.GetInspectRiskText(AbyssalSummoningConsoleUtility.GetRiskLabel(AbyssalSummoningConsoleUtility.GetRiskTier(circle, ritual))));
            if (dominionActive)
            {
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(leftInner.x, leftInner.y + 100f, leftInner.width, 58f), AbyssalSummoningConsoleUtility.GetDominionOpsSummary(circle));
                Text.Font = GameFont.Small;
            }
            GUI.color = Color.white;
            AbyssalCircleCapacitorRitualUtility.CapacitorReadinessReport capacitorReport = AbyssalCircleCapacitorRitualUtility.CreateReadinessReport(circle, ritual);
            if (!dominionActive)
            {
                Widgets.Label(new Rect(leftInner.x, leftInner.y + 104f, leftInner.width, 18f), AbyssalCircleCapacitorUtility.GetInstalledSummary(circle));
                Widgets.Label(new Rect(leftInner.x, leftInner.y + 124f, leftInner.width, 18f), "ABY_CapacitorPanel_State".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetSupportStateLabel(capacitorReport));
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                Widgets.Label(new Rect(leftInner.x, leftInner.y + 144f, leftInner.width, 18f), "ABY_CapacitorPanel_Lattice".Translate() + ": " + AbyssalCircleCapacitorUtility.GetLatticeProfileLabel(circle));
                Widgets.Label(new Rect(leftInner.x, leftInner.y + 164f, leftInner.width, 18f), "ABY_CapacitorPanel_Flow".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetChargeFlowReadout(capacitorReport));
            }
            else
            {
                GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
                Widgets.Label(new Rect(leftInner.x, leftInner.y + 144f, leftInner.width, 18f), "ABY_CapacitorPanel_State".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetSupportStateLabel(capacitorReport));
            }
            GUI.color = Color.white;

            AbyssalSummoningConsoleArt.DrawPanel(rightRect, true);
            Rect rightInner = rightRect.ContractedBy(10f);
            AbyssalSummoningConsoleUtility.CircleRiskTier riskTier = AbyssalSummoningConsoleUtility.GetRiskTier(circle, ritual);
            AbyssalSummoningConsoleArt.DrawRiskBar(new Rect(rightInner.x, rightInner.y + 8f, rightInner.width, 24f), AbyssalSummoningConsoleUtility.GetRiskFill(circle, ritual), AbyssalSummoningConsoleUtility.GetRiskLabel(riskTier), AbyssalSummoningConsoleUtility.GetRiskColor(riskTier), circle.RitualActive);
            GUI.color = Color.white;
            Widgets.Label(new Rect(rightInner.x, rightInner.y + 126f, rightInner.width, 18f), "ABY_CircleInspect_Stabilizers".Translate(circle.InstalledStabilizerCount, circle.ModuleSlots.Count));
            GUI.color = AbyssalSummoningConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rightInner.x, rightInner.y + 146f, rightInner.width, 36f), dominionActive ? AbyssalSummoningConsoleUtility.GetCompactHint(circle) : AbyssalSummoningConsoleUtility.GetStabilizerMiniSummary(circle));
            GUI.color = Color.white;

            if (AbyssalStyledWidgets.TextButton(new Rect(rightInner.x, rightInner.y + 46f, rightInner.width, 32f), AbyssalSummoningConsoleUtility.GetOpenConsoleLabel(), true, true))
            {
                Find.WindowStack.Add(new Window_AbyssalSummoningConsole(circle));
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }

            bool canInvoke = !circle.RitualActive;
            if (dominionActive)
            {
                if (AbyssalStyledWidgets.TextButton(new Rect(rightInner.x, rightInner.y + 86f, rightInner.width, 32f), AbyssalSummoningConsoleUtility.GetDominionObjectiveButtonLabel(circle), true, true))
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
            }
            else if (AbyssalStyledWidgets.TextButton(new Rect(rightInner.x, rightInner.y + 86f, rightInner.width, 32f), AbyssalSummoningConsoleUtility.GetAssignSigilLabel(), canInvoke, true))
            {
                TryAssign(circle, ritual);
            }

            AbyssalSummoningConsoleArt.DrawPanel(bottomRect, false);
            Widgets.Label(bottomRect.ContractedBy(10f), AbyssalSummoningConsoleUtility.GetCompactFooter(circle));
        }

        private static void TryAssign(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
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
        }
    }
}
