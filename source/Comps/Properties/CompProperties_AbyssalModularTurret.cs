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
        public int targetScanIntervalTicks = 45;
        public float basePowerDraw = 650f;

        // Socket anchors are chassis-local map offsets where weapon-module pivots are locked.
        // This keeps rotating overlays mounted to the visible socket instead of drifting around the building center.
        public float mainWeaponSocketSideOffset;
        public float mainWeaponSocketForwardOffset;
        public float auxiliarySocketSideOffset;
        public float auxiliarySocketForwardOffset;

        public List<string> allowedModuleDefNames;

        public CompProperties_AbyssalModularTurret()
        {
            compClass = typeof(CompAbyssalModularTurret);
        }
    }
}
