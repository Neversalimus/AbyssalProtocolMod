using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_ReactorOverheatField : CompProperties
    {
        public string hediffDefName = "ABY_ReactorSaturation";
        public float baseRadius = 9.4f;
        public int tickIntervalTicks = 50;
        public float severityPerPulse = 0.18f;
        public float maxSeverity = 1.2f;
        public bool requireLineOfSight;

        public CompProperties_ABY_ReactorOverheatField()
        {
            compClass = typeof(CompABY_ReactorOverheatField);
        }
    }
}
