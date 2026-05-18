using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSlicePhaseFloorVfxUtility
    {
        private const string ConduitPulseDefName = "ABY_Mote_DominionSlicePhaseConduitPulse";
        private const string HeartCrackDefName = "ABY_Mote_DominionSliceHeartFloorCrack";

        private static ThingDef conduitPulseDef;
        private static ThingDef heartCrackDef;

        private static ThingDef ConduitPulseDef
        {
            get { return conduitPulseDef ?? (conduitPulseDef = DefDatabase<ThingDef>.GetNamedSilentFail(ConduitPulseDefName)); }
        }

        private static ThingDef HeartCrackDef
        {
            get { return heartCrackDef ?? (heartCrackDef = DefDatabase<ThingDef>.GetNamedSilentFail(HeartCrackDefName)); }
        }

        public static void SpawnPhaseRing(IntVec3 cell, Map map, float scale)
        {
            // Disabled by the Dominion Sepulcher redesign: phase state should no longer draw large
            // magic circles around the heart.
        }

        public static void SpawnConduitPulse(IntVec3 cell, Map map, float scale)
        {
            ThingDef def = ConduitPulseDef;
            if (def == null || map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            MoteMaker.MakeStaticMote(CellToFloorPos(cell, 0.020f), map, def, Mathf.Clamp(scale, 0.30f, 1.20f));
        }

        public static void SpawnAnchorGlyph(IntVec3 cell, Map map, float scale)
        {
            // Disabled by the Dominion Sepulcher redesign: no large glyph/circle under pylons.
        }

        public static void SpawnHeartCrack(IntVec3 cell, Map map, float scale)
        {
            ThingDef def = HeartCrackDef;
            if (def == null || map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            MoteMaker.MakeStaticMote(CellToFloorPos(cell, 0.022f), map, def, Mathf.Clamp(scale, 0.35f, 1.45f));
        }

        private static Vector3 CellToFloorPos(IntVec3 cell, float altitudeOffset)
        {
            return new Vector3(cell.x + 0.5f, AltitudeLayer.MoteOverhead.AltitudeFor() + altitudeOffset, cell.z + 0.5f);
        }
    }
}
