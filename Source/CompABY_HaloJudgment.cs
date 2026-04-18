using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_HaloJudgment : ThingComp
    {
        private int nextMarkTick = -1;

        public CompProperties_ABY_HaloJudgment Props => (CompProperties_ABY_HaloJudgment)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextMarkTick, "nextMarkTick", -1);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                ScheduleNextMark(initial: true);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || !parent.IsHashIntervalTick(Mathf.Max(12, Props.scanIntervalTicks)))
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (nextMarkTick < 0)
            {
                ScheduleNextMark(initial: true);
            }

            if (ticksGame < nextMarkTick)
            {
                return;
            }

            Pawn target = FindTarget(pawn);
            if (target != null)
            {
                ApplyJudgmentMark(target);
            }

            ScheduleNextMark(initial: false);
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed && pawn.Faction != null;
        }

        private void ScheduleNextMark(bool initial)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int baseDelay = initial ? Props.initialWarmupTicks : Props.markCooldownTicks;
            int variance = initial ? 0 : Mathf.Max(0, Props.markCooldownVariance);
            int offset = variance > 0 ? Rand.RangeInclusive(-variance, variance) : 0;
            nextMarkTick = now + Mathf.Max(120, baseDelay + offset);
        }

        private Pawn FindTarget(Pawn pawn)
        {
            Pawn target = AbyssalThreatPawnUtility.FindBestTarget(
                pawn,
                Props.minTargetRange,
                Props.maxTargetRange,
                preferFarthestTargets: false,
                preferRangedTargets: Props.preferRangedTargets,
                requireRangedTargets: false,
                rangedTargetBias: 2.6f,
                healthWeight: 0f);

            if (target == null)
            {
                return null;
            }

            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.markHediffDefName);
            if (markDef == null)
            {
                return null;
            }

            Hediff existing = target.health?.hediffSet?.GetFirstHediffOfDef(markDef);
            HediffComp_Disappears disappears = existing?.TryGetComp<HediffComp_Disappears>();
            if (disappears != null && disappears.ticksToDisappear > Mathf.Max(0, Props.minRefreshTicksRemaining))
            {
                return null;
            }

            return target;
        }

        private void ApplyJudgmentMark(Pawn target)
        {
            if (target?.health == null)
            {
                return;
            }

            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.markHediffDefName);
            if (markDef == null)
            {
                return;
            }

            Hediff mark = target.health.hediffSet.GetFirstHediffOfDef(markDef);
            if (mark == null)
            {
                mark = HediffMaker.MakeHediff(markDef, target);
                target.health.AddHediff(mark);
            }

            mark.Severity = Mathf.Max(mark.Severity, 1f);
            HediffComp_Disappears disappears = mark.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = Mathf.Max(60, Props.markDurationTicks);
            }

            target.health.hediffSet.DirtyCache();

            if (!Props.soundDefName.NullOrEmpty() && target.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt(Props.soundDefName, target.PositionHeld, target.MapHeld);
            }

            if (target.MapHeld != null)
            {
                FleckMaker.ThrowLightningGlow(target.DrawPos, target.MapHeld, Props.applicationVisualScale);
                FleckMaker.Static(target.PositionHeld, target.MapHeld, FleckDefOf.ExplosionFlash, Props.applicationVisualScale * 0.45f);
            }
        }
    }
}
