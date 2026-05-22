using Verse;

namespace AbyssalProtocol
{
    public class CompABY_BossNoDowned : ThingComp
    {
        private const int DownedRetryTicks = 6;
        private const int StableRetryTicks = 30;

        public CompProperties_ABY_BossNoDowned Props => (CompProperties_ABY_BossNoDowned)props;

        private int lastNoDownedRecoveryTick = -999999;
        private int nextNoDownedRecoveryTick = -999999;

        private Pawn PawnParent => parent as Pawn;

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Dead || !pawn.Spawned || !pawn.Downed)
            {
                return;
            }

            TryRunNoDownedRecovery(pawn, urgent: false);
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.Dead || !pawn.Downed)
            {
                return;
            }

            TryRunNoDownedRecovery(pawn, urgent: true);
        }

        private void TryRunNoDownedRecovery(Pawn pawn, bool urgent)
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (lastNoDownedRecoveryTick == ticksGame)
            {
                return;
            }

            if (!urgent && ticksGame < nextNoDownedRecoveryTick)
            {
                return;
            }

            lastNoDownedRecoveryTick = ticksGame;
            AbyssalBossNoDownedUtility.TryPreventDowned(
                pawn,
                Props.bloodLossClamp,
                Props.heatstrokeClamp,
                Props.healWorstInjuryAmount,
                Props.maxHealPasses,
                Props.forceLordReengage);

            nextNoDownedRecoveryTick = pawn.Downed
                ? ticksGame + DownedRetryTicks
                : ticksGame + StableRetryTicks;
        }
    }
}
