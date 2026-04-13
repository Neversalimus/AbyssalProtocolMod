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

            if (!GenSight.LineOfSight(caster.PositionHeld, targetPawn.PositionHeld, caster.MapHeld))
            {
                return false;
            }

            CompRuptureCrown crownComp = RuptureCrownUtility.GetWornCrownComp(caster);
            if (crownComp != null && !crownComp.IsReady)
            {
                return false;
            }

            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent?.pawn;
            Pawn targetPawn = ResolveTargetPawn(caster, target);
            if (caster == null || targetPawn == null || targetPawn.health == null)
            {
                NotifyPlayerFailure(caster, "Rupture Sentence failed: no valid hostile pawn was resolved from the selected target.");
                return;
            }

            if (!CanApplyOn(target, dest))
            {
                NotifyPlayerFailure(caster, "Rupture Sentence failed: target became invalid before cast resolution.");
                return;
            }

            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.markHediffDef);
            if (markDef == null)
            {
                NotifyPlayerFailure(caster, "Rupture Sentence failed: mark hediff def is missing.");
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

            var things = target.Cell.GetThingList(caster.MapHeld);
            for (int i = 0; i < things.Count; i++)
            {
                Pawn pawn = things[i] as Pawn;
                if (pawn != null)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static void NotifyPlayerFailure(Pawn caster, string text)
        {
            if (caster != null && caster.Faction == Faction.OfPlayer)
            {
                Messages.Message(text, caster, MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
