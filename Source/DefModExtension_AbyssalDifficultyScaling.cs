using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class DefModExtension_AbyssalDifficultyScaling : DefModExtension
    {
        public int contentTier = 1;
        public string difficultyFloorDefName = "ABY_Difficulty_Normal";
        public string role = "assault";
        public float budgetCost = 100f;
        public float selectionWeight = 1f;
        public int maxPlanCount = -1;
        public bool allowFutureAutoEscalation = true;
        public bool forceStatScalingOnAssault = false;
        public List<string> encounterPools = new List<string>();
    }
}
