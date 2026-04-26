using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ForgeCrucibleInfrastructureCard
    {
        private const string CrucibleIconPath = "Things/Building/ABY_ResidueSinteringCrucible";
        private static Texture2D cachedCrucibleIcon;

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

            Rect stateRect = new Rect(titleRect.x, titleRect.yMax + 4f, titleRect.width, 24f);
            Text.Font = GameFont.Small;
            GUI.color = GetStateColor(status);
            Widgets.Label(stateRect, status.StateKey.Translate());
            GUI.color = Color.white;

            Rect metricsRect = new Rect(inner.x, iconRect.yMax + 10f, inner.width, 38f);
            float metricWidth = (metricsRect.width - 18f) / 4f;
            AbyssalForgeConsoleArt.DrawMetric(new Rect(metricsRect.x, metricsRect.y, metricWidth, metricsRect.height), "ABY_CrucibleMetricUnits".Translate(), status.OnlineCrucibleCount + "/" + status.CrucibleCount);
            AbyssalForgeConsoleArt.DrawMetric(new Rect(metricsRect.x + metricWidth + 6f, metricsRect.y, metricWidth, metricsRect.height), "ABY_CrucibleMetricCorpses".Translate(), status.SinterableCorpseCount.ToString());
            AbyssalForgeConsoleArt.DrawMetric(new Rect(metricsRect.x + (metricWidth + 6f) * 2f, metricsRect.y, metricWidth, metricsRect.height), "ABY_CrucibleMetricYield".Translate(), ABY_ResidueSinteringConsoleUtility.BuildEstimatedYieldLabel(status));
            AbyssalForgeConsoleArt.DrawMetric(new Rect(metricsRect.x + (metricWidth + 6f) * 3f, metricsRect.y, metricWidth, metricsRect.height), "ABY_CrucibleMetricQueued".Translate(), status.QueuedSinterBills.ToString());

            float detailY = metricsRect.yMax + 8f;
            Text.Font = GameFont.Tiny;
            GUI.color = status.BestCorpseResidue > 0 ? Color.white : AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, detailY, inner.width, 20f), ABY_ResidueSinteringConsoleUtility.BuildBestCandidateLabel(status));

            GUI.color = AbyssalForgeConsoleArt.TextDimColor;
            Widgets.Label(new Rect(inner.x, detailY + 21f, inner.width, 20f), ABY_ResidueSinteringConsoleUtility.BuildTierBreakdownLabel(status));
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            TooltipHandler.TipRegion(rect, ABY_ResidueSinteringConsoleUtility.BuildInfrastructureTooltip(status));
        }

        private static void DrawCrucibleIcon(Rect rect, ABY_ResidueSinteringConsoleUtility.StatusSnapshot status)
        {
            Texture2D icon = cachedCrucibleIcon;
            if (icon == null)
            {
                icon = ContentFinder<Texture2D>.Get(CrucibleIconPath, false);
                cachedCrucibleIcon = icon;
            }

            Widgets.DrawBoxSolid(rect, new Color(0.05f, 0.035f, 0.032f, 0.78f));
            Widgets.DrawBox(rect, 1);

            if (icon != null)
            {
                GUI.color = status.CrucibleDefAvailable ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                GUI.DrawTexture(rect.ContractedBy(3f), icon, ScaleMode.ScaleToFit, true);
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
