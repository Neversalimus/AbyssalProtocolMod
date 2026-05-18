using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_ForgeCrucibleInfrastructureCard
    {
        private const string CrucibleIconPath = "Things/Building/ABY_ResidueSinteringCrucible";
        private static readonly Texture2D cachedCrucibleIcon = ContentFinder<Texture2D>.Get(CrucibleIconPath, false);

        public static void Draw(Rect rect, Building_AbyssalForge forge)
        {
            ABY_ResidueSinteringConsoleUtility.StatusSnapshot status = ABY_ResidueSinteringConsoleUtility.BuildStatus(forge?.Map);
            bool highlighted = status.IsReady || (status.HasOnlineCrucible && status.EstimatedResidueYield > 0);

            AbyssalForgeConsoleArt.DrawPanel(rect, highlighted);
            Rect inner = rect.ContractedBy(10f);

            Rect iconRect = new Rect(inner.x, inner.y + 2f, 46f, 46f);
            DrawCrucibleIcon(iconRect, status);

            Rect titleRect = new Rect(iconRect.xMax + 8f, inner.y, inner.width - iconRect.width - 8f, 22f);
            AbyssalForgeConsoleArt.DrawSectionTitle(titleRect, "ABY_CrucibleInfrastructureHeader".Translate());

            Rect stateRect = new Rect(titleRect.x, titleRect.yMax + 4f, titleRect.width, 38f);
            Text.Font = GameFont.Small;
            GUI.color = GetStateColor(status);
            Widgets.Label(stateRect, status.StateKey.Translate());
            GUI.color = Color.white;

            Rect buttonRect = new Rect(inner.xMax - 132f, iconRect.yMax + 8f, 132f, 28f);
            DrawSelectCrucibleButton(buttonRect, status);

            Rect metricsRect = new Rect(inner.x, buttonRect.yMax + 8f, inner.width, 44f);
            float metricWidth = (metricsRect.width - 18f) / 4f;
            DrawMetricSafe(new Rect(metricsRect.x, metricsRect.y, metricWidth, metricsRect.height), "ABY_CrucibleMetricUnits".Translate(), status.OnlineCrucibleCount + "/" + status.CrucibleCount);
            DrawMetricSafe(new Rect(metricsRect.x + metricWidth + 6f, metricsRect.y, metricWidth, metricsRect.height), "ABY_CrucibleMetricCorpses".Translate(), status.SinterableCorpseCount.ToString());
            DrawMetricSafe(new Rect(metricsRect.x + (metricWidth + 6f) * 2f, metricsRect.y, metricWidth, metricsRect.height), "ABY_CrucibleMetricYield".Translate(), ABY_ResidueSinteringConsoleUtility.BuildEstimatedYieldLabel(status));
            DrawMetricSafe(new Rect(metricsRect.x + (metricWidth + 6f) * 3f, metricsRect.y, metricWidth, metricsRect.height), "ABY_CrucibleMetricQueued".Translate(), status.QueuedSinterBills.ToString());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            TooltipHandler.TipRegion(rect, ABY_ResidueSinteringConsoleUtility.BuildInfrastructureTooltip(status));
        }

        private static void DrawMetricSafe(Rect rect, string label, string value)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Tiny;
            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 18f), label);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x, rect.y + 20f, rect.width, rect.height - 20f), value);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DrawSelectCrucibleButton(Rect rect, ABY_ResidueSinteringConsoleUtility.StatusSnapshot status)
        {
            bool enabled = status.FocusCrucible != null && !status.FocusCrucible.Destroyed;
            string label = "ABY_CrucibleSelectButton".Translate();
            string tooltip = enabled ? "ABY_CrucibleSelectButtonTip".Translate() : "ABY_CrucibleSelectButtonDisabledTip".Translate();

            bool clicked = AbyssalStyledWidgets.TextButton(
                rect,
                label,
                enabled,
                false,
                null,
                tooltip);

            if (clicked && enabled)
            {
                SelectCrucible(status.FocusCrucible);
            }
        }

        private static void SelectCrucible(Thing crucible)
        {
            if (crucible == null || crucible.Destroyed)
            {
                return;
            }

            Find.Selector.ClearSelection();
            Find.Selector.Select(crucible);

        }

        private static void DrawCrucibleIcon(Rect rect, ABY_ResidueSinteringConsoleUtility.StatusSnapshot status)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.05f, 0.035f, 0.032f, 0.78f));
            Widgets.DrawBox(rect, 1);

            if (cachedCrucibleIcon != null)
            {
                GUI.color = status.CrucibleDefAvailable ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                GUI.DrawTexture(rect.ContractedBy(3f), cachedCrucibleIcon, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }
            else
            {
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = AbyssalForgeConsoleArt.TextDimColor;
                Widgets.Label(rect, "ABY_CrucibleIconUnavailable".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }
        }

        private static Color GetStateColor(ABY_ResidueSinteringConsoleUtility.StatusSnapshot status)
        {
            if (status.IsReady && status.QueuedSinterBills > 0)
            {
                return new Color(0.72f, 1f, 0.74f, 1f);
            }

            if (status.HasAnyCrucible || status.ResearchSatisfied)
            {
                return new Color(1f, 0.78f, 0.48f, 1f);
            }

            return new Color(1f, 0.58f, 0.52f, 1f);
        }
    }
}
