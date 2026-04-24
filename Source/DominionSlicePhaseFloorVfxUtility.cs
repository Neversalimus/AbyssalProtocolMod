using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSlicePhaseFloorVfxUtility
    {
        private const string PhaseRingDefName = "ABY_Mote_DominionSlicePhaseFloorRing";
        private const string ConduitPulseDefName = "ABY_Mote_DominionSlicePhaseConduitPulse";
        private const string AnchorGlyphDefName = "ABY_Mote_DominionSliceAnchorFloorGlyph";
        private const string HeartCrackDefName = "ABY_Mote_DominionSliceHeartFloorCrack";

        private static ThingDef phaseRingDef;
        private static ThingDef conduitPulseDef;
        private static ThingDef anchorGlyphDef;
        private static ThingDef heartCrackDef;

        private static ThingDef PhaseRingDef
        {
            get { return phaseRingDef ?? (phaseRingDef = DefDatabase<ThingDef>.GetNamedSilentFail(PhaseRingDefName)); }
        }

        private static ThingDef ConduitPulseDef
        {
            get { return conduitPulseDef ?? (conduitPulseDef = DefDatabase<ThingDef>.GetNamedSilentFail(ConduitPulseDefName)); }
        }

        private static ThingDef AnchorGlyphDef
        {
            get { return anchorGlyphDef ?? (anchorGlyphDef = DefDatabase<ThingDef>.GetNamedSilentFail(AnchorGlyphDefName)); }
        }

        private static ThingDef HeartCrackDef
        {
            get { return heartCrackDef ?? (heartCrackDef = DefDatabase<ThingDef>.GetNamedSilentFail(HeartCrackDefName)); }
        }

        public static void SpawnPhaseRing(IntVec3 cell, Map map, float scale)
        {
            ThingDef def = PhaseRingDef;
            if (def == null || map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            MoteMaker.MakeStaticMote(CellToFloorPos(cell, 0.015f), map, def, Mathf.Clamp(scale, 1.2f, 12.5f));
        }

        public static void SpawnConduitPulse(IntVec3 cell, Map map, float scale)
        {
            ThingDef def = ConduitPulseDef;
            if (def == null || map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            MoteMaker.MakeStaticMote(CellToFloorPos(cell, 0.020f), map, def, Mathf.Clamp(scale, 0.45f, 2.8f));
        }

        public static void SpawnAnchorGlyph(IntVec3 cell, Map map, float scale)
        {
            ThingDef def = AnchorGlyphDef;
            if (def == null || map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            MoteMaker.MakeStaticMote(CellToFloorPos(cell, 0.018f), map, def, Mathf.Clamp(scale, 2.0f, 8.0f));
        }

        public static void SpawnHeartCrack(IntVec3 cell, Map map, float scale)
        {
            ThingDef def = HeartCrackDef;
            if (def == null || map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            MoteMaker.MakeStaticMote(CellToFloorPos(cell, 0.022f), map, def, Mathf.Clamp(scale, 0.75f, 4.2f));
        }

        private static Vector3 CellToFloorPos(IntVec3 cell, float altitudeOffset)
        {
            return new Vector3(cell.x + 0.5f, AltitudeLayer.MoteOverhead.AltitudeFor() + altitudeOffset, cell.z + 0.5f);
        }
    }
}
