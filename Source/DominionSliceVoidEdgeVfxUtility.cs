using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSliceVoidEdgeVfxUtility
    {
        private const string VoidRimMoteDefName = "ABY_Mote_DominionSliceVoidRim";
        private const string VoidCrackMoteDefName = "ABY_Mote_DominionSliceVoidCrack";
        private const string BoundaryRiftMoteDefName = "ABY_Mote_DominionSliceBoundaryRift";
        private const string EdgeShardMoteDefName = "ABY_Mote_DominionSliceEdgeShard";

        private static ThingDef voidRimMoteDef;
        private static ThingDef voidCrackMoteDef;
        private static ThingDef boundaryRiftMoteDef;
        private static ThingDef edgeShardMoteDef;

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

        public static void SpawnVoidRim(Vector3 position, Map map, float scale)
        {
            Spawn(position, map, VoidRimMoteDef, Mathf.Clamp(scale, 0.75f, 3.10f));
        }

        public static void SpawnVoidCrack(Vector3 position, Map map, float scale)
        {
            Spawn(position, map, VoidCrackMoteDef, Mathf.Clamp(scale, 0.55f, 2.35f));
            if (map != null && Rand.Chance(0.16f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnBoundaryRift(Vector3 position, Map map, float scale)
        {
            Spawn(position, map, BoundaryRiftMoteDef, Mathf.Clamp(scale, 0.80f, 2.90f));
            if (map != null && Rand.Chance(0.34f))
            {
                FleckMaker.ThrowLightningGlow(position, map, Mathf.Clamp(scale * 0.72f, 0.65f, 1.85f));
            }
        }

        public static void SpawnEdgeShard(Vector3 position, Map map, float scale)
        {
            Spawn(position, map, EdgeShardMoteDef, Mathf.Clamp(scale, 0.45f, 1.95f));
            if (map != null && Rand.Chance(0.24f))
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
