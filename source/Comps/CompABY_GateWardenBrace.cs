using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_GateWardenBrace : ThingComp
    {
        public CompProperties_ABY_GateWardenBrace Props => (CompProperties_ABY_GateWardenBrace)props;

        private Pawn PawnParent => parent as Pawn;

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || !parent.IsHashIntervalTick(Mathf.Max(10, Props.scanIntervalTicks)))
            {
                return;
            }

            bool threatened = AbyssalThreatPawnUtility.FindClosestThreatWithin(pawn, Props.triggerEnemyRange) != null;
            if (!threatened)
            {
                CompABY_GateWardenEscort escort = pawn.TryGetComp<CompABY_GateWardenEscort>();
                threatened = escort != null && escort.HasAnchorThreatNow;
            }

            if (!threatened)
            {
                Thing currentJobTarget = pawn.jobs?.curJob?.targetA.Thing;
                if (currentJobTarget is Pawn hostilePawn
                    && AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, hostilePawn)
                    && pawn.PositionHeld.DistanceTo(hostilePawn.PositionHeld) <= Props.triggerEnemyRange + 1.4f)
                {
                    threatened = true;
                }
            }

            if (!threatened)
            {
                return;
            }

            float severity = Props.braceSeverity;
            if (pawn.health != null && pawn.health.summaryHealth != null)
            {
                float healthFraction = pawn.health.summaryHealth.SummaryHealthPercent;
                if (healthFraction <= Props.woundedHealthThreshold)
                {
                    severity = Mathf.Max(severity, Props.woundedBraceSeverity);
                }
            }

            AbyssalThreatPawnUtility.ApplyOrRefreshHediff(pawn, Props.braceHediffDefName, severity);
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed;
        }
    }
}
