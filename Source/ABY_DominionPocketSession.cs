using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_DominionPocketSession : IExposable
    {
        public string sessionId;
        public int sourceMapId = -1;
        public int pocketMapId = -1;
        public int sourceGateThingId = -1;
        public int pocketExitThingId = -1;
        public IntVec3 sourceReturnCell = IntVec3.Invalid;
        public IntVec3 pocketEntryCell = IntVec3.Invalid;
        public int createdTick;
        public bool active = true;
        public bool cleanupQueued;

        public void ExposeData()
        {
            Scribe_Values.Look(ref sessionId, "sessionId");
            Scribe_Values.Look(ref sourceMapId, "sourceMapId", -1);
            Scribe_Values.Look(ref pocketMapId, "pocketMapId", -1);
            Scribe_Values.Look(ref sourceGateThingId, "sourceGateThingId", -1);
            Scribe_Values.Look(ref pocketExitThingId, "pocketExitThingId", -1);
            Scribe_Values.Look(ref sourceReturnCell, "sourceReturnCell", IntVec3.Invalid);
            Scribe_Values.Look(ref pocketEntryCell, "pocketEntryCell", IntVec3.Invalid);
            Scribe_Values.Look(ref createdTick, "createdTick", 0);
            Scribe_Values.Look(ref active, "active", true);
            Scribe_Values.Look(ref cleanupQueued, "cleanupQueued", false);
        }
    }
}
