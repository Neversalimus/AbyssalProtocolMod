using System;
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
        private const int CacheIntervalTicks = 90;

        private static readonly Dictionary<Map, CachedStatus> CachedStatuses = new Dictionary<Map, CachedStatus>();

        private struct CachedStatus
        {
            public int Tick;
            public StatusSnapshot Status;
        }

        public struct StatusSnapshot
        {
            public bool CrucibleDefAvailable;
            public bool SinterRecipeAvailable;
            public bool ResearchSatisfied;
            public int CrucibleCount;
            public int OnlineCrucibleCount;
            public int SinterableCorpseCount;
            public int QueuedSinterBills;
            public int LowTierCorpseCount;
            public int MidTierCorpseCount;
            public int EliteTierCorpseCount;
            public int EstimatedResidueYield;
            public string BestCorpseLabel;
            public int BestCorpseResidue;

            public bool HasAnyCrucible => CrucibleCount > 0;
            public bool HasOnlineCrucible => OnlineCrucibleCount > 0;
            public bool HasSinterableCorpse => SinterableCorpseCount > 0;
            public bool IsReady => CrucibleDefAvailable && SinterRecipeAvailable && ResearchSatisfied && HasOnlineCrucible && HasSinterableCorpse;

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

                    if (!HasSinterableCorpse)
                    {
                        return "ABY_CrucibleStateWaitingCorpses";
                    }

                    if (QueuedSinterBills <= 0)
                    {
                        return "ABY_CrucibleStateReadyNoBill";
                    }

                    return "ABY_CrucibleStateReady";
                }
            }
        }

        public static StatusSnapshot BuildStatus(Map map)
        {
            if (map == null)
            {
                return BuildStatusUncached(null);
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            if (CachedStatuses.TryGetValue(map, out CachedStatus cached) && ticksGame - cached.Tick < CacheIntervalTicks)
            {
                return cached.Status;
            }

            StatusSnapshot status = BuildStatusUncached(map);
            CachedStatuses[map] = new CachedStatus
            {
                Tick = ticksGame,
                Status = status
            };
            return status;
        }

        public static StatusSnapshot BuildStatusUncached(Map map)
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

            ScanSinterableCorpses(map, ref status);
            ScanCrucibles(map, crucibleDef, sinterRecipe, ref status);
            return status;
        }

        public static string BuildTierBreakdownLabel(StatusSnapshot status)
        {
            return "ABY_CrucibleTierBreakdown".Translate(status.LowTierCorpseCount, status.MidTierCorpseCount, status.EliteTierCorpseCount);
        }

        public static string BuildBestCandidateLabel(StatusSnapshot status)
        {
            if (status.BestCorpseResidue <= 0 || status.BestCorpseLabel.NullOrEmpty())
            {
                return "ABY_CrucibleBestCandidateNone".Translate();
            }

            return "ABY_CrucibleBestCandidate".Translate(status.BestCorpseLabel, status.BestCorpseResidue);
        }

        public static string BuildEstimatedYieldLabel(StatusSnapshot status)
        {
            if (status.EstimatedResidueYield <= 0)
            {
                return "—";
            }

            return "~" + status.EstimatedResidueYield;
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
            AppendRequirement(sb, status.HasSinterableCorpse, "ABY_CrucibleRequirementCorpses".Translate());

            sb.AppendLine();
            sb.AppendLine("ABY_CrucibleInfrastructureTooltipOutput".Translate());
            sb.AppendLine("ABY_CrucibleTooltipYield".Translate(BuildEstimatedYieldLabel(status)));
            sb.AppendLine(BuildBestCandidateLabel(status));
            sb.AppendLine(BuildTierBreakdownLabel(status));

            sb.AppendLine();
            sb.AppendLine("ABY_CrucibleInfrastructureTooltipCorpseRules".Translate());
            sb.AppendLine("ABY_CrucibleInfrastructureTooltipResidueScale".Translate());
            sb.AppendLine();
            sb.AppendLine("ABY_CrucibleInfrastructureTooltipHint".Translate());
            return sb.ToString().TrimEnd();
        }

        private static void ScanSinterableCorpses(Map map, ref StatusSnapshot status)
        {
            List<Thing> corpses = map.listerThings?.ThingsInGroup(ThingRequestGroup.Corpse);
            if (corpses == null)
            {
                return;
            }

            for (int i = 0; i < corpses.Count; i++)
            {
                Thing corpse = corpses[i];
                if (!ABY_ResidueSinteringUtility.TryGetResidueAmount(corpse, out int residueAmount))
                {
                    continue;
                }

                status.SinterableCorpseCount++;
                status.EstimatedResidueYield += residueAmount;

                if (residueAmount <= 8)
                {
                    status.LowTierCorpseCount++;
                }
                else if (residueAmount <= 18)
                {
                    status.MidTierCorpseCount++;
                }
                else
                {
                    status.EliteTierCorpseCount++;
                }

                if (residueAmount > status.BestCorpseResidue)
                {
                    status.BestCorpseResidue = residueAmount;
                    status.BestCorpseLabel = GetCorpseDisplayLabel(corpse);
                }
            }
        }

        private static void ScanCrucibles(Map map, ThingDef crucibleDef, RecipeDef sinterRecipe, ref StatusSnapshot status)
        {
            if (crucibleDef == null)
            {
                return;
            }

            List<Thing> crucibles = map.listerThings?.ThingsOfDef(crucibleDef);
            if (crucibles == null)
            {
                return;
            }

            for (int i = 0; i < crucibles.Count; i++)
            {
                Thing thing = crucibles[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                status.CrucibleCount++;

                CompPowerTrader power = thing is ThingWithComps thingWithComps ? thingWithComps.GetComp<CompPowerTrader>() : null;
                if (power == null || power.PowerOn)
                {
                    status.OnlineCrucibleCount++;
                }

                if (sinterRecipe == null || !(thing is Building_WorkTable workTable) || workTable.BillStack == null)
                {
                    continue;
                }

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

        private static string GetCorpseDisplayLabel(Thing corpse)
        {
            if (corpse is Corpse innerCorpse && innerCorpse.InnerPawn != null)
            {
                string pawnLabel = innerCorpse.InnerPawn.LabelShortCap;
                if (!pawnLabel.NullOrEmpty())
                {
                    return pawnLabel;
                }

                string kindLabel = innerCorpse.InnerPawn.kindDef?.label;
                if (!kindLabel.NullOrEmpty())
                {
                    return kindLabel.CapitalizeFirst();
                }
            }

            return corpse.LabelCap.ToString();
        }

        private static void AppendRequirement(StringBuilder sb, bool satisfied, string label)
        {
            sb.Append(satisfied ? "✓ " : "– ");
            sb.AppendLine(label);
        }
    }
}
