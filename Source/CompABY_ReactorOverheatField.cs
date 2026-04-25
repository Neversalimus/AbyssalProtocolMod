using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_ReactorOverheatField : ThingComp
    {
        private float tunedRadius = -1f;
        private float tunedSeverityPerPulse = -1f;
        private int tunedIntervalTicks = -1;
        private int collapseWindowUntilTick = -1;

        public CompProperties_ABY_ReactorOverheatField Props => (CompProperties_ABY_ReactorOverheatField)props;

        private Pawn PawnParent => parent as Pawn;
        private float ActiveRadius => tunedRadius > 0f ? tunedRadius : Props.baseRadius;
        private float ActiveSeverityPerPulse => tunedSeverityPerPulse > 0f ? tunedSeverityPerPulse : Props.severityPerPulse;
        private int ActiveIntervalTicks => tunedIntervalTicks > 0 ? tunedIntervalTicks : Props.tickIntervalTicks;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref tunedRadius, "tunedRadius", -1f);
            Scribe_Values.Look(ref tunedSeverityPerPulse, "tunedSeverityPerPulse", -1f);
            Scribe_Values.Look(ref tunedIntervalTicks, "tunedIntervalTicks", -1);
            Scribe_Values.Look(ref collapseWindowUntilTick, "collapseWindowUntilTick", -1);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || CollapseWindowActive || !parent.IsHashIntervalTick(Mathf.Max(15, ActiveIntervalTicks)))
            {
                return;
            }

            HediffDef hediffDef = ABY_DefCache.HediffDefNamed(Props.hediffDefName);
            if (hediffDef == null)
            {
                return;
            }

            var pawns = pawn.MapHeld.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            float activeRadius = ActiveRadius;
            float activeRadiusSq = activeRadius * activeRadius;
            IntVec3 origin = pawn.PositionHeld;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn other = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, other))
                {
                    continue;
                }

                if ((other.PositionHeld - origin).LengthHorizontalSquared > activeRadiusSq)
                {
                    continue;
                }

                if (Props.requireLineOfSight && !GenSight.LineOfSight(origin, other.PositionHeld, pawn.MapHeld))
                {
                    continue;
                }

                ApplyOrIncreaseHediff(other, hediffDef, ActiveSeverityPerPulse, Props.maxSeverity);
            }
        }

        public void ApplyPhaseTuning(float radius, float severityPerPulse, int intervalTicks)
        {
            tunedRadius = Mathf.Max(1f, radius);
            tunedSeverityPerPulse = Mathf.Max(0.01f, severityPerPulse);
            tunedIntervalTicks = Mathf.Max(15, intervalTicks);
        }

        public void NotifyAegisCollapsed(int durationTicks)
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            collapseWindowUntilTick = Math.Max(collapseWindowUntilTick, ticksGame + Math.Max(60, durationTicks));
        }

        private bool CollapseWindowActive
        {
            get
            {
                int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                return ticksGame < collapseWindowUntilTick;
            }
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed;
        }

        private static void ApplyOrIncreaseHediff(Pawn target, HediffDef hediffDef, float amount, float maxSeverity)
        {
            if (target?.health == null || hediffDef == null)
            {
                return;
            }

            Hediff existing = target.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (existing != null)
            {
                existing.Severity = Mathf.Min(maxSeverity, existing.Severity + amount);
                return;
            }

            Hediff added = HediffMaker.MakeHediff(hediffDef, target);
            if (added == null)
            {
                return;
            }

            added.Severity = Mathf.Clamp(amount, 0.01f, maxSeverity);
            target.health.AddHediff(added);
        }
    }
}
