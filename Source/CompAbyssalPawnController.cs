using Verse;

namespace AbyssalProtocol
{
    public class CompAbyssalPawnController : ThingComp
    {
        private int spawnTick = -1;
        private int lastLordEnsureTick = -99999;

        public CompProperties_AbyssalPawnController Props => (CompProperties_AbyssalPawnController)props;

        private Pawn PawnParent => parent as Pawn;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
            Scribe_Values.Look(ref lastLordEnsureTick, "lastLordEnsureTick", -99999);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (spawnTick < 0)
            {
                spawnTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            }

            TryUpdateController();
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            TryUpdateController();
        }

        private void TryUpdateController()
        {
            Pawn pawn = PawnParent;
            if (pawn == null || pawn.MapHeld == null || pawn.Dead)
            {
                return;
            }

            if (Props.autoPrepare)
            {
                AbyssalThreatPawnUtility.PrepareThreatPawn(pawn, Props);
            }

            if (Props.ensureHostileFaction)
            {
                AbyssalThreatPawnUtility.EnsureHostilityAndLord(
                    pawn,
                    Props.ensureAssaultLord,
                    Props.sappers,
                    spawnTick,
                    ref lastLordEnsureTick,
                    Props.spawnGraceTicks,
                    Props.lordRetryTicks);
            }
        }

        public static CompAbyssalPawnController GetFor(Pawn pawn)
        {
            return pawn?.GetComp<CompAbyssalPawnController>();
        }
    }
}
