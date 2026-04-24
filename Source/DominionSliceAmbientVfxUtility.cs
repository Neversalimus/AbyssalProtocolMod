using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSliceAmbientVfxUtility
    {
        private const string AmbientEmberDefName = "ABY_Mote_DominionSliceAmbientEmber";
        private const string PressurePulseDefName = "ABY_Mote_DominionSliceAmbientPressurePulse";
        private const string EdgeSparkDefName = "ABY_Mote_DominionSliceAmbientEdgeSpark";

        private static ThingDef ambientEmberDef;
        private static ThingDef pressurePulseDef;
        private static ThingDef edgeSparkDef;

        private static ThingDef AmbientEmberDef
        {
            get { return ambientEmberDef ?? (ambientEmberDef = DefDatabase<ThingDef>.GetNamedSilentFail(AmbientEmberDefName)); }
        }

        private static ThingDef PressurePulseDef
        {
            get { return pressurePulseDef ?? (pressurePulseDef = DefDatabase<ThingDef>.GetNamedSilentFail(PressurePulseDefName)); }
        }

        private static ThingDef EdgeSparkDef
        {
            get { return edgeSparkDef ?? (edgeSparkDef = DefDatabase<ThingDef>.GetNamedSilentFail(EdgeSparkDefName)); }
        }

        public static void SpawnAmbientEmber(Vector3 position, Map map, float scale)
        {
            if (map == null)
            {
                return;
            }

            ThingDef def = AmbientEmberDef;
            if (def != null)
            {
                MoteMaker.MakeStaticMote(position, map, def, Mathf.Clamp(scale, 0.25f, 1.65f));
            }
        }

        public static void SpawnPressurePulse(Vector3 position, Map map, float scale)
        {
            if (map == null)
            {
                return;
            }

            ThingDef def = PressurePulseDef;
            if (def != null)
            {
                MoteMaker.MakeStaticMote(position, map, def, Mathf.Clamp(scale, 2.5f, 10.5f));
            }
        }

        public static void SpawnEdgeSpark(Vector3 position, Map map, float scale)
        {
            if (map == null)
            {
                return;
            }

            ThingDef def = EdgeSparkDef;
            if (def != null)
            {
                MoteMaker.MakeStaticMote(position, map, def, Mathf.Clamp(scale, 0.55f, 2.1f));
            }

            if (Rand.Chance(0.38f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }
    }
}
