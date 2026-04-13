using Verse;

namespace AbyssalProtocol
{
    public class Thing_HeraldAnalysisPacket : ThingWithComps
    {
        private bool pendingResolve = true;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            pendingResolve = !respawningAfterLoad;
        }

        protected override void Tick()
        {
            base.Tick();

            if (!pendingResolve || MapHeld == null || Destroyed)
            {
                return;
            }

            pendingResolve = false;
            ABY_HeraldFragmentAnalysisUtility.ResolveAnalysisPacket(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pendingResolve, "pendingResolve", true);
        }
    }
}
