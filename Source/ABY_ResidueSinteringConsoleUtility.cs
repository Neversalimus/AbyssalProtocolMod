using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ResidueSinteringConsoleUtility
    {
        private const string CrucibleDefName = "ABY_ResidueSinteringCrucible";
        private const string SinterRecipeDefName = "ABY_SinterAbyssalRemains";
        private const string SignalResearchDefName = "ABY_AbyssalSignalTheory";

        public struct StatusSnapshot
        {
            public bool CrucibleDefAvailable;
            public bool SinterRecipeAvailable;
            public bool ResearchSatisfied;
            public int CrucibleCount;
            public int OnlineCrucibleCount;
            public int SinterableCorpseCount;
            public int QueuedSinterBills;

            public bool HasAnyCrucible => CrucibleCount > 0;
            public bool HasOnlineCrucible => OnlineCrucibleCount > 0;
            public bool IsReady => CrucibleDefAvailable && SinterRecipeAvailable && ResearchSatisfied && HasOnlineCrucible && SinterableCorpseCount > 0;

            public string StateKey
            {
                get
                {
                    if (!CrucibleDefAvailable || !SinterRecipeAvailable)
                    {
                        return "ABY_CrucibleStateUnavailable";
                    }

                    if (!ResearchSatisfied)
                    {
                        return "ABY_CrucibleStateResearchLocked";
                    }

                    if (!HasAnyCrucible)
                    {
                        return "ABY_CrucibleStateNotBuilt";
                    }

                    if (!HasOnlineCrucible)
                    {
                        return "ABY_CrucibleStateOffline";
                    }

                    if (SinterableCorpseCount <= 0)
                    {
                        return "ABY_CrucibleStateWaitingCorpses";
                    }

                    return "ABY_CrucibleStateReady";
                }
            }
        }

        public static StatusSnapshot BuildStatus(Map map)
        {
            StatusSnapshot status = new StatusSnapshot();

            ThingDef crucibleDef = DefDatabase<ThingDef>.GetNamedSilentFail(CrucibleDefName);
            RecipeDef sinterRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail(SinterRecipeDefName);
            ResearchProjectDef signalResearch = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(SignalResearchDefName);

            status.CrucibleDefAvailable = crucibleDef != null;
            status.SinterRecipeAvailable = sinterRecipe != null;
            status.ResearchSatisfied = signalResearch == null || signalResearch.IsFinished;

            if (map == null)
            {
                return status;
            }

            status.SinterableCorpseCount = ABY_ResidueSinteringUtility.CountSinterableCorpses(map);

            if (crucibleDef == null)
            {
                return status;
            }

            List<Thing> crucibles = map.listerThings?.ThingsOfDef(crucibleDef);
            if (crucibles == null)
            {
                return status;
            }

            for (int i = 0; i < crucibles.Count; i++)
            {
                Thing thing = crucibles[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                status.CrucibleCount++;

                CompPowerTrader power = thing.TryGetComp<CompPowerTrader>();
                if (power == null || power.PowerOn)
                {
                    status.OnlineCrucibleCount++;
                }

                if (sinterRecipe != null && thing is Building_WorkTable workTable && workTable.BillStack != null)
                {
                    List<Bill> bills = workTable.BillStack.Bills;
                    if (bills == null)
                    {
                        continue;
                    }

                    for (int billIndex = 0; billIndex < bills.Count; billIndex++)
                    {
                        Bill bill = bills[billIndex];
                        if (bill?.recipe == sinterRecipe)
                        {
                            status.QueuedSinterBills++;
                        }
                    }
                }
            }

            return status;
        }

        public static string BuildInfrastructureTooltip(StatusSnapshot status)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ABY_CrucibleInfrastructureTooltipTitle".Translate());
            sb.AppendLine(status.StateKey.Translate());
            sb.AppendLine();
            sb.AppendLine("ABY_CrucibleInfrastructureTooltipRequirements".Translate());
            AppendRequirement(sb, status.ResearchSatisfied, "ABY_CrucibleRequirementSignal".Translate());
            AppendRequirement(sb, status.HasAnyCrucible, "ABY_CrucibleRequirementBuilt".Translate());
            AppendRequirement(sb, status.HasOnlineCrucible, "ABY_CrucibleRequirementPowered".Translate());
            AppendRequirement(sb, status.SinterableCorpseCount > 0, "ABY_CrucibleRequirementCorpses".Translate());
            sb.AppendLine();
            sb.AppendLine("ABY_CrucibleInfrastructureTooltipCorpseRules".Translate());
            sb.AppendLine("ABY_CrucibleInfrastructureTooltipResidueScale".Translate());
            sb.AppendLine();
            sb.AppendLine("ABY_CrucibleInfrastructureTooltipHint".Translate());
            return sb.ToString().TrimEnd();
        }

        private static void AppendRequirement(StringBuilder sb, bool satisfied, string label)
        {
            sb.Append(satisfied ? "✓ " : "– ");
            sb.AppendLine(label);
        }
    }
}
