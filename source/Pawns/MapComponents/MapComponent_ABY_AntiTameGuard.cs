using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_ABY_AntiTameGuard : MapComponent
    {
        private const int GuardIntervalTicks = 30;
        private int nextGuardTick;

        public MapComponent_ABY_AntiTameGuard(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextGuardTick, "abyAntiTame_nextGuardTick", 0);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            ABY_AntiTameUtility.NormalizeAbyssalRaceDefsOnce();
            nextGuardTick = 0;
        }

        public override void MapGenerated()
        {
            base.MapGenerated();
            ABY_AntiTameUtility.EnforceMap(map);
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
            ABY_AntiTameUtility.EnforceMap(map);
        }
    }
}
