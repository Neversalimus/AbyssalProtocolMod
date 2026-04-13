using System.Collections.Generic;
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
            return CanApplyInternal(target);
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            ApplyInternal(target);
        }

        private bool CanApplyInternal(LocalTargetInfo target)
        {
            Pawn caster = parent?.pawn;
            Pawn targetPawn = ResolveTargetPawn(caster, target);
            return IsValidPawnTarget(caster, targetPawn);
        }

        private void ApplyInternal(LocalTargetInfo target)
        {
            Pawn caster = parent?.pawn;
            Pawn targetPawn = ResolveTargetPawn(caster, target);

            if (caster == null || targetPawn == null || targetPawn.health == null)
            {
                if (caster != null && caster.Faction == Faction.OfPlayer)
                {
                    Messages.Message("Rupture Sentence failed: no valid hostile pawn in the selected target.", caster, MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.markHediffDef);
            if (markDef == null)
            {
                if (caster.Faction == Faction.OfPlayer)
                {
                    Messages.Message("Rupture Sentence failed: mark hediff is missing.", caster, MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            if (!IsValidPawnTarget(caster, targetPawn))
            {
                if (caster.Faction == Faction.OfPlayer)
                {
                    Messages.Message("Rupture Sentence failed: target is invalid.", caster, MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            base.Apply(new LocalTargetInfo(targetPawn), LocalTargetInfo.Invalid);

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
                FleckMaker.ThrowLightningGlow(targetPawn.DrawPos, targetPawn.MapHeld, 1.8f);
            }

            if (caster.Faction == Faction.OfPlayer)
            {
                Messages.Message("Rupture Sentence discharged.", new LookTargets(targetPawn), MessageTypeDefOf.NeutralEvent, false);
            }
        }

        private static bool IsValidPawnTarget(Pawn caster, Pawn targetPawn)
        {
            if (caster == null || caster.Dead || !caster.Spawned || caster.MapHeld == null)
            {
                return false;
            }

            if (targetPawn == null || targetPawn == caster || targetPawn.Dead || !targetPawn.Spawned || targetPawn.MapHeld != caster.MapHeld)
            {
                return false;
            }

            if (!targetPawn.HostileTo(caster))
            {
                return false;
            }

            return GenSight.LineOfSight(caster.PositionHeld, targetPawn.PositionHeld, caster.MapHeld);
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

            List<Thing> things = target.Cell.GetThingList(caster.MapHeld);
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
    }
}
