using Verse;

namespace AbyssalProtocol
{
    public enum AbyssalCircleModuleEdge
    {
        North,
        East,
        South,
        West
    }

    public sealed class AbyssalCircleModuleSlot : IExposable
    {
        public AbyssalCircleModuleEdge Edge;
        public string InstalledThingDefName;

        public AbyssalCircleModuleSlot()
        {
        }

        public AbyssalCircleModuleSlot(AbyssalCircleModuleEdge edge)
        {
            Edge = edge;
        }

        public ThingDef InstalledThingDef =>
            InstalledThingDefName.NullOrEmpty()
                ? null
                : DefDatabase<ThingDef>.GetNamedSilentFail(InstalledThingDefName);

        public bool Occupied => InstalledThingDef != null;

        public void SetInstalledThingDef(ThingDef def)
        {
            InstalledThingDefName = def?.defName;
        }

        public void Clear()
        {
            InstalledThingDefName = null;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Edge, "edge", AbyssalCircleModuleEdge.North);
            Scribe_Values.Look(ref InstalledThingDefName, "installedThingDefName");
        }
    }
}
