using Verse;

namespace AbyssalProtocol
{
    public class DefModExtension_AbyssalCircleModule : DefModExtension
    {
        public const string StabilizerFamily = "Stabilizer";

        public string moduleFamily = StabilizerFamily;
        public int tier = 1;
        public float containmentBonus = 0f;
        public float ritualHeatMultiplier = 1f;
        public float contaminationMultiplier = 1f;
        public string mountedTexPath;
        public float mountedDrawScale = 1.18f;
    }
}
