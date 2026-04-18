using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class HediffComp_ABY_HaloJudgmentMark : HediffComp
    {
        public HediffCompProperties_ABY_HaloJudgmentMark Props => (HediffCompProperties_ABY_HaloJudgmentMark)props;

        private Pawn PawnParent => parent?.pawn;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = PawnParent;
            if (pawn == null || !pawn.Spawned || pawn.MapHeld == null || pawn.Dead || !pawn.IsHashIntervalTick(System.Math.Max(15, Props.visualPulseIntervalTicks)))
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.MapHeld, Props.visualScale);
            FleckMaker.Static(pawn.PositionHeld, pawn.MapHeld, FleckDefOf.ExplosionFlash, Props.visualScale * 0.20f);
        }
    }
}
