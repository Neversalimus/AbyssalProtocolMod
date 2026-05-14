using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class CompProperties_AbyssalModularTurret : CompProperties
    {
        public string chassisTag = "Medium";
        public int mainWeaponSlots = 1;
        public int auxiliarySlots = 1;
        public int passiveSlots = 2;
        public float baseRange = 24f;
        public int baseCooldownTicks = 210;
        public int targetScanIntervalTicks = 30;
        public float basePowerDraw = 650f;
        public List<string> allowedModuleDefNames;

        public CompProperties_AbyssalModularTurret()
        {
            compClass = typeof(CompAbyssalModularTurret);
        }
    }
}
