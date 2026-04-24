using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSliceVoidEdgeVfxUtility
    {
        private const string VoidVeilMoteDefName = "ABY_Mote_DominionSliceVoidVeil";
        private const string VoidRimMoteDefName = "ABY_Mote_DominionSliceVoidRim";
        private const string VoidCrackMoteDefName = "ABY_Mote_DominionSliceVoidCrack";
        private const string BoundaryRiftMoteDefName = "ABY_Mote_DominionSliceBoundaryRift";
        private const string EdgeShardMoteDefName = "ABY_Mote_DominionSliceEdgeShard";

        private static ThingDef voidVeilMoteDef;
        private static ThingDef voidRimMoteDef;
        private static ThingDef voidCrackMoteDef;
        private static ThingDef boundaryRiftMoteDef;
        private static ThingDef edgeShardMoteDef;

        private static ThingDef VoidVeilMoteDef
        {
            get { return voidVeilMoteDef ?? (voidVeilMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(VoidVeilMoteDefName)); }
        }

        private static ThingDef VoidRimMoteDef
        {
            get { return voidRimMoteDef ?? (voidRimMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(VoidRimMoteDefName)); }
        }

        private static ThingDef VoidCrackMoteDef
        {
            get { return voidCrackMoteDef ?? (voidCrackMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(VoidCrackMoteDefName)); }
        }

        private static ThingDef BoundaryRiftMoteDef
        {
            get { return boundaryRiftMoteDef ?? (boundaryRiftMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(BoundaryRiftMoteDefName)); }
        }

        private static ThingDef EdgeShardMoteDef
        {
            get { return edgeShardMoteDef ?? (edgeShardMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(EdgeShardMoteDefName)); }
        }

        public static void SpawnVoidVeil(Vector3 position, Map map, float scale)
        {
            Spawn(position, map, VoidVeilMoteDef, Mathf.Clamp(scale, 1.40f, 5.40f));
        }

        public static void SpawnVoidRim(Vector3 position, Map map, float scale)
        {
            Spawn(position, map, VoidRimMoteDef, Mathf.Clamp(scale, 1.10f, 4.25f));
        }

        public static void SpawnVoidCrack(Vector3 position, Map map, float scale)
        {
            Spawn(position, map, VoidCrackMoteDef, Mathf.Clamp(scale, 0.75f, 3.10f));
            if (map != null && Rand.Chance(0.24f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnBoundaryRift(Vector3 position, Map map, float scale)
        {
            Spawn(position, map, BoundaryRiftMoteDef, Mathf.Clamp(scale, 1.10f, 4.10f));
            if (map != null && Rand.Chance(0.46f))
            {
                FleckMaker.ThrowLightningGlow(position, map, Mathf.Clamp(scale * 0.82f, 0.85f, 2.45f));
            }

            if (map != null && Rand.Chance(0.22f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnEdgeShard(Vector3 position, Map map, float scale)
        {
            Spawn(position, map, EdgeShardMoteDef, Mathf.Clamp(scale, 0.55f, 2.30f));
            if (map != null && Rand.Chance(0.18f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        private static void Spawn(Vector3 position, Map map, ThingDef moteDef, float scale)
        {
            if (map == null || moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(position, map, moteDef, scale);
        }
    }
}
