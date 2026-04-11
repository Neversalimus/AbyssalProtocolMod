using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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

        public int dashPhase = 3;
        public int dashCooldownTicks = 780;
        public int dashSearchIntervalTicks = 30;
        public float dashMinRange = 7f;
        public float dashMaxRange = 24f;
        public float dashLandingRadius = 1.9f;

        public HediffCompProperties_ArchonCoreController()
        {
            compClass = typeof(HediffComp_ArchonCoreController);
        }
    }

    public class HediffComp_ArchonCoreController : HediffComp
    {
        private int currentPhase = 1;
        private int lastDashTick = -999999;

        public HediffCompProperties_ArchonCoreController Props =>
            (HediffCompProperties_ArchonCoreController)props;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref currentPhase, "currentPhase", 1);
            Scribe_Values.Look(ref lastDashTick, "lastDashTick", -999999);
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

            if (currentPhase >= Props.dashPhase && pawn.IsHashIntervalTick(Props.dashSearchIntervalTicks))
            {
                TryDash();
            }
        }

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

        private void TryDash()
        {
            Pawn source = Pawn;
            if (source == null || source.MapHeld == null || !source.Spawned)
                return;

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame - lastDashTick < Props.dashCooldownTicks)
                return;

            Pawn target = FindDashTarget(source);
            if (target == null)
                return;

            if (!TryFindDashCellNearTarget(source, target, out IntVec3 dashCell))
                return;

            lastDashTick = ticksGame;
            DoDash(source, target, dashCell);
        }

        private Pawn FindDashTarget(Pawn source)
        {
            Pawn bestTarget = null;
            float bestScore = float.MinValue;
            Map map = source.MapHeld;

            foreach (Pawn candidate in map.mapPawns.AllPawnsSpawned)
            {
                if (!IsValidDashTarget(source, candidate))
                    continue;

                float distance = source.Position.DistanceTo(candidate.Position);
                if (distance < Props.dashMinRange || distance > Props.dashMaxRange)
                    continue;

                float score = distance;

                if (candidate.equipment?.Primary != null && candidate.equipment.Primary.def.IsRangedWeapon)
                {
                    score += 12f;
                }

                if (candidate.health != null && candidate.health.summaryHealth.SummaryHealthPercent < 0.50f)
                {
                    score += 2f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private bool TryFindDashCellNearTarget(Pawn source, Pawn target, out IntVec3 dashCell)
        {
            Map map = source.MapHeld;
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Position, Props.dashLandingRadius, true))
            {
                if (!cell.InBounds(map))
                    continue;

                if (!cell.Standable(map))
                    continue;

                Pawn occupant = cell.GetFirstPawn(map);
                if (occupant != null && occupant != source)
                    continue;

                float distFromSource = source.Position.DistanceTo(cell);
                if (distFromSource < 4f)
                    continue;

                float distToTarget = cell.DistanceTo(target.Position);
                float score = -distToTarget;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            dashCell = bestCell;
            return bestCell.IsValid;
        }

        private void DoDash(Pawn source, Pawn target, IntVec3 dashCell)
        {
            Map map = source.MapHeld;
            if (map == null || !dashCell.IsValid)
                return;

            if (source.Spawned)
            {
                source.DeSpawn();
            }

            GenSpawn.Spawn(source, dashCell, map, source.Rotation);

            if (source.pather != null && target != null && target.Spawned && !target.Dead)
            {
                source.pather.StartPath(target, PathEndMode.Touch);
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

        private static bool IsValidDashTarget(Pawn source, Pawn target)
        {
            if (source == null || target == null)
                return false;

            if (target == source)
                return false;

            if (target.Dead || !target.Spawned)
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
