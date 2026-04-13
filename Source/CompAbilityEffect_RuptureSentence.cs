using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_AbilityEffect_RuptureSentence : CompProperties_AbilityEffect
    {
        public string markHediffDef = RuptureCrownUtility.MarkHediffDefName;
        public int markTicks = 4320;
        public int fallbackCooldownTicks = GenDate.TicksPerDay;

        public CompProperties_AbilityEffect_RuptureSentence()
        {
            compClass = typeof(CompAbilityEffect_RuptureSentence);
        }
    }

    public class CompAbilityEffect_RuptureSentence : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_RuptureSentence Props => (CompProperties_AbilityEffect_RuptureSentence)props;

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (!base.CanApplyOn(target, dest))
            {
                return false;
            }

            Pawn caster = parent?.pawn;
            Pawn targetPawn = ResolveTargetPawn(caster, target);
            if (caster == null || targetPawn == null)
            {
                return false;
            }

            if (caster == targetPawn || caster.Dead || targetPawn.Dead)
            {
                return false;
            }

            if (!caster.Spawned || !targetPawn.Spawned || caster.MapHeld == null || targetPawn.MapHeld != caster.MapHeld)
            {
                return false;
            }

            if (!targetPawn.HostileTo(caster))
            {
                return false;
            }

            return GenSight.LineOfSight(caster.PositionHeld, targetPawn.PositionHeld, caster.MapHeld);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent?.pawn;
            Pawn targetPawn = ResolveTargetPawn(caster, target);
            if (caster == null || targetPawn == null || targetPawn.health == null)
            {
                return;
            }

            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.markHediffDef);
            if (markDef == null)
            {
                return;
            }

            base.Apply(target, dest);

            Hediff mark = targetPawn.health.hediffSet.GetFirstHediffOfDef(markDef);
            if (mark == null)
            {
                mark = HediffMaker.MakeHediff(markDef, targetPawn);
                targetPawn.health.AddHediff(mark);
            }

            mark.Severity = Mathf.Max(mark.Severity, 1f);
            HediffComp_Disappears disappears = mark.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = Props.markTicks;
            }

            targetPawn.health.hediffSet.DirtyCache();

            CompRuptureCrown crownComp = RuptureCrownUtility.GetWornCrownComp(caster);
            if (crownComp != null)
            {
                crownComp.NotifyUsed();
                parent.StartCooldown(crownComp.Props.rechargeTicks);
            }
            else
            {
                parent.StartCooldown(Props.fallbackCooldownTicks);
            }

            if (caster.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureVerdict", caster.PositionHeld, caster.MapHeld);
            }

            if (targetPawn.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureImpact", targetPawn.PositionHeld, targetPawn.MapHeld);
            }

            if (caster.Faction == Faction.OfPlayer)
            {
                Messages.Message("Rupture Sentence discharged.", caster, MessageTypeDefOf.NeutralEvent, false);
            }
        }

        private static Pawn ResolveTargetPawn(Pawn caster, LocalTargetInfo target)
        {
            if (caster?.MapHeld == null || !target.IsValid)
            {
                return null;
            }

            Pawn directPawn = target.Thing as Pawn;
            if (directPawn != null)
            {
                return directPawn;
            }

            if (!target.Cell.IsValid)
            {
                return null;
            }

            var thingList = target.Cell.GetThingList(caster.MapHeld);
            for (int i = 0; i < thingList.Count; i++)
            {
                Pawn pawn = thingList[i] as Pawn;
                if (pawn != null)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}
