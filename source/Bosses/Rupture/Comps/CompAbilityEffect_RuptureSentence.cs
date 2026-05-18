using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_AbilityEffect_RuptureSentence : CompProperties_AbilityEffect
    {
        public int markTicks = RuptureCrownUtility.DefaultMarkTicks;
        public int fallbackCooldownTicks = GenDate.TicksPerDay;
        public float effectRadius = RuptureCrownUtility.DefaultVerdictRadius;

        public CompProperties_AbilityEffect_RuptureSentence()
        {
            compClass = typeof(CompAbilityEffect_RuptureSentence);
        }
    }

    public class CompAbilityEffect_RuptureSentence : CompAbilityEffect
    {
        public new CompProperties_AbilityEffect_RuptureSentence Props =>
            (CompProperties_AbilityEffect_RuptureSentence)props;

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent?.pawn;
            return caster != null && RuptureCrownUtility.CountEligibleTargets(caster, Props.effectRadius) > 0;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn caster = parent?.pawn;
            if (caster == null)
            {
                return;
            }

            CompRuptureCrown crownComp = RuptureCrownUtility.GetWornCrownComp(caster);
            if (crownComp != null)
            {
                crownComp.TryDischargeVerdict(caster);
                return;
            }

            int affectedCount = RuptureCrownUtility.ApplyVerdictWave(caster, Props.effectRadius, Props.markTicks);
            if (affectedCount <= 0)
            {
                if (caster.Faction == Faction.OfPlayer)
                {
                    Messages.Message(
                        "No hostile or neutral non-colony pawns are within rupture radius.",
                        caster,
                        MessageTypeDefOf.RejectInput,
                        false);
                }

                return;
            }

            parent.StartCooldown(Props.fallbackCooldownTicks);

            if (caster.MapHeld != null)
            {
                ABY_SoundUtility.PlayAt("ABY_RuptureVerdict", caster.PositionHeld, caster.MapHeld);
            }

            if (caster.Faction == Faction.OfPlayer)
            {
                Messages.Message(
                    "Rupture Verdict collapsed " + affectedCount + " target(s).",
                    new LookTargets(caster),
                    MessageTypeDefOf.NeutralEvent,
                    false);
            }
        }
    }
}
