using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class AbyssalCircleCapacitorSlot : IExposable
    {
        public string slotId;
        public string labelKey;
        public string installedThingDefName;

        public bool IsEmpty => installedThingDefName.NullOrEmpty();

        public ThingDef InstalledThingDef
        {
            get
            {
                if (installedThingDefName.NullOrEmpty())
                {
                    return null;
                }

                return DefDatabase<ThingDef>.GetNamedSilentFail(installedThingDefName);
            }
        }

        public DefModExtension_AbyssalCircleCapacitor InstalledExtension => InstalledThingDef?.GetModExtension<DefModExtension_AbyssalCircleCapacitor>();

        public void SetInstalledThingDef(ThingDef def)
        {
            installedThingDefName = def?.defName;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref slotId, "slotId");
            Scribe_Values.Look(ref labelKey, "labelKey");
            Scribe_Values.Look(ref installedThingDefName, "installedThingDefName");
        }
    }
}
