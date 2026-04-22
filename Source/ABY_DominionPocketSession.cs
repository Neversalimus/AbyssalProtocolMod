using System.Collections.Generic;
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
        public IntVec3 extractionCell = IntVec3.Invalid;
        public IntVec3 heartCell = IntVec3.Invalid;
        public List<IntVec3> anchorCells = new List<IntVec3>();
        public int createdTick;
        public int initialStrikeTeamCount;
        public int lastKnownPocketPawnCount;
        public bool victoryAchieved;
        public bool rewardsGranted;
        public int collapseAtTick;
        public string rewardSummary;
        public string lastOutcomeReason;
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
            Scribe_Values.Look(ref extractionCell, "extractionCell", IntVec3.Invalid);
            Scribe_Values.Look(ref heartCell, "heartCell", IntVec3.Invalid);
            Scribe_Collections.Look(ref anchorCells, "anchorCells", LookMode.Value);
            Scribe_Values.Look(ref createdTick, "createdTick", 0);
            Scribe_Values.Look(ref initialStrikeTeamCount, "initialStrikeTeamCount", 0);
            Scribe_Values.Look(ref lastKnownPocketPawnCount, "lastKnownPocketPawnCount", 0);
            Scribe_Values.Look(ref victoryAchieved, "victoryAchieved", false);
            Scribe_Values.Look(ref rewardsGranted, "rewardsGranted", false);
            Scribe_Values.Look(ref collapseAtTick, "collapseAtTick", 0);
            Scribe_Values.Look(ref rewardSummary, "rewardSummary");
            Scribe_Values.Look(ref lastOutcomeReason, "lastOutcomeReason");
            Scribe_Values.Look(ref active, "active", true);
            Scribe_Values.Look(ref cleanupQueued, "cleanupQueued", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && anchorCells == null)
            {
                anchorCells = new List<IntVec3>();
            }
        }
    }
}
