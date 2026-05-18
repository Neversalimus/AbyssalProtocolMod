using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_ABY_AntiAnimalWorkflowV3 : MapComponent
    {
        private const int GuardIntervalTicks = 30;
        private int nextGuardTick;

        public MapComponent_ABY_AntiAnimalWorkflowV3(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextGuardTick, "abyAntiAnimalWorkflowV3_nextGuardTick", 0);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            nextGuardTick = 0;
            ABY_LargeModpackHotfixBUtility.EnforceAntiAnimalWorkflow(map, true);
        }

        public override void MapGenerated()
        {
            base.MapGenerated();
            ABY_LargeModpackHotfixBUtility.EnforceAntiAnimalWorkflow(map, true);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null || Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (now < nextGuardTick)
            {
                return;
            }

            nextGuardTick = now + GuardIntervalTicks;
            ABY_LargeModpackHotfixBUtility.EnforceAntiAnimalWorkflow(map, true);
        }
    }
}
