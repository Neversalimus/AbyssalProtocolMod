using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class HediffCompProperties_ArchonCoreController : HediffCompProperties
    {
        public float phase2HealthPct = 0.70f;
        public float phase3HealthPct = 0.35f;

        public float phase1AuraRadius = 6f;
        public float phase2AuraRadius = 7f;
        public float phase3AuraRadius = 8f;

        public float phase1HeatstrokeSeverity = 0.0035f;
        public float phase2HeatstrokeSeverity = 0.0060f;
        public float phase3HeatstrokeSeverity = 0.0090f;

        public int auraIntervalTicks = 60;

        public HediffCompProperties_ArchonCoreController()
        {
            compClass = typeof(HediffComp_ArchonCoreController);
        }
    }

    public class HediffComp_ArchonCoreController : HediffComp
    {
        private int currentPhase = 1;

        public HediffCompProperties_ArchonCoreController Props =>
            (HediffCompProperties_ArchonCoreController)props;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref currentPhase, "currentPhase", 1);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.MapHeld == null)
                return;

            UpdatePhase();

            if (pawn.IsHashIntervalTick(Props.auraIntervalTicks))
            {
                ApplyHeatAura();
            }
        }

        private Pawn Pawn => parent?.pawn;

        private void UpdatePhase()
        {
            Pawn pawn = Pawn;
            if (pawn == null)
                return;

            float hpPct = pawn.health.summaryHealth.SummaryHealthPercent;

            int newPhase;
            if (hpPct <= Props.phase3HealthPct)
            {
                newPhase = 3;
            }
            else if (hpPct <= Props.phase2HealthPct)
            {
                newPhase = 2;
            }
            else
            {
                newPhase = 1;
            }

            currentPhase = newPhase;

            switch (currentPhase)
            {
                case 1:
                    parent.Severity = 0.10f;
                    break;
                case 2:
                    parent.Severity = 1.10f;
                    break;
                default:
                    parent.Severity = 2.10f;
                    break;
            }
        }

        private void ApplyHeatAura()
        {
            Pawn source = Pawn;
            if (source == null || source.MapHeld == null)
                return;

            float radius;
            float severityPerPulse;

            switch (currentPhase)
            {
                case 1:
                    radius = Props.phase1AuraRadius;
                    severityPerPulse = Props.phase1HeatstrokeSeverity;
                    break;
                case 2:
                    radius = Props.phase2AuraRadius;
                    severityPerPulse = Props.phase2HeatstrokeSeverity;
                    break;
                default:
                    radius = Props.phase3AuraRadius;
                    severityPerPulse = Props.phase3HeatstrokeSeverity;
                    break;
            }

            Map map = source.MapHeld;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(source.Position, radius, true))
            {
                if (!cell.InBounds(map))
                    continue;

                foreach (Thing thing in cell.GetThingList(map))
                {
                    Pawn target = thing as Pawn;
                    if (!IsValidAuraTarget(source, target))
                        continue;

                    ApplyHeatstroke(target, severityPerPulse);
                }
            }
        }

        private static bool IsValidAuraTarget(Pawn source, Pawn target)
        {
            if (source == null || target == null)
                return false;

            if (target == source)
                return false;

            if (target.Dead || !target.Spawned)
                return false;

            if (!target.RaceProps.IsFlesh)
                return false;

            if (!target.HostileTo(source))
                return false;

            return true;
        }

        private static void ApplyHeatstroke(Pawn target, float severityAmount)
        {
            if (severityAmount <= 0f)
                return;

            Hediff heatstroke = target.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Heatstroke);
            if (heatstroke == null)
            {
                heatstroke = HediffMaker.MakeHediff(HediffDefOf.Heatstroke, target);
                heatstroke.Severity = 0f;
                target.health.AddHediff(heatstroke);
            }

            heatstroke.Severity += severityAmount;
        }
    }
}
