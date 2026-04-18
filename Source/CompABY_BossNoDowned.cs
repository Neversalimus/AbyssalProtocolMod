using Verse;

namespace AbyssalProtocol
{
    public class CompABY_BossNoDowned : ThingComp
    {
        public CompProperties_ABY_BossNoDowned Props => (CompProperties_ABY_BossNoDowned)props;

        private Pawn PawnParent => parent as Pawn;

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Dead || !pawn.Spawned)
            {
                return;
            }

            if (pawn.Downed)
            {
                AbyssalBossNoDownedUtility.TryPreventDowned(
                    pawn,
                    Props.bloodLossClamp,
                    Props.heatstrokeClamp,
                    Props.healWorstInjuryAmount,
                    Props.maxHealPasses,
                    Props.forceLordReengage);
            }
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Dead || !pawn.Downed)
            {
                return;
            }

            AbyssalBossNoDownedUtility.TryPreventDowned(
                pawn,
                Props.bloodLossClamp,
                Props.heatstrokeClamp,
                Props.healWorstInjuryAmount,
                Props.maxHealPasses,
                Props.forceLordReengage);
        }
    }
}
