using Verse;

namespace AbyssalProtocol
{
    public sealed class DefModExtension_AbyssalDifficultyScaling : DefModExtension
    {
        public int contentTier = 1;
        public string role = "assault";
        public ABY_DifficultyPreset difficultyFloor = ABY_DifficultyPreset.Normal;
        public bool applyRoleStatScaling = true;
        public bool allowFutureAutoEscalation = true;
    }
}
