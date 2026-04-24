using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class DominionSliceHeartSetpieceVfxUtility
    {
        private const string OuterHaloTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartHaloOuter";
        private const string InnerHaloTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartHaloInner";
        private const string ExposedCoreTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartExposedCore";
        private const string FloorPulseTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartFloorPulse";
        private const string ShieldLatticeTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartShieldLattice";
        private const string CrownRingTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCrownRing";
        private const string CoreCoronaTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCoreCorona";

        private const string HeartBeatMoteDefName = "ABY_Mote_DominionSliceHeartBeat";
        private const string HeartBeatExposedMoteDefName = "ABY_Mote_DominionSliceHeartBeatExposed";
        private const string HeartCoreFlareMoteDefName = "ABY_Mote_DominionSliceHeartCoreFlare";

        private static readonly Material OuterHaloMaterial = MaterialPool.MatFrom(OuterHaloTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material InnerHaloMaterial = MaterialPool.MatFrom(InnerHaloTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material ExposedCoreMaterial = MaterialPool.MatFrom(ExposedCoreTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material FloorPulseMaterial = MaterialPool.MatFrom(FloorPulseTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material ShieldLatticeMaterial = MaterialPool.MatFrom(ShieldLatticeTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material CrownRingMaterial = MaterialPool.MatFrom(CrownRingTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material CoreCoronaMaterial = MaterialPool.MatFrom(CoreCoronaTexPath, ShaderDatabase.MoteGlow);

        private static ThingDef heartBeatMoteDef;
        private static ThingDef heartBeatExposedMoteDef;
        private static ThingDef heartCoreFlareMoteDef;

        private static ThingDef HeartBeatMoteDef => heartBeatMoteDef ?? (heartBeatMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(HeartBeatMoteDefName));
        private static ThingDef HeartBeatExposedMoteDef => heartBeatExposedMoteDef ?? (heartBeatExposedMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(HeartBeatExposedMoteDefName));
        private static ThingDef HeartCoreFlareMoteDef => heartCoreFlareMoteDef ?? (heartCoreFlareMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(HeartCoreFlareMoteDefName));

        public static void DrawHeartSetpiece(Vector3 heartPos, Map map, MapComponent_DominionSliceEncounter encounter, int seed)
        {
            if (map == null || encounter == null || !encounter.IsActiveEncounter)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float breathe = 1f + Mathf.Sin((ticks + seed) * 0.029f) * 0.045f;
            float shimmer = 1f + Mathf.Sin((ticks + seed) * 0.051f + 0.8f) * 0.035f;
            float phasePulse = 1f + Mathf.Sin((ticks + seed) * 0.083f + 0.45f) * 0.05f;
            float yBase = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.004f;
            bool exposed = encounter.IsHeartExposed;
            int liveAnchors = encounter.LiveAnchorCount;

            Vector3 floorLoc = heartPos;
            floorLoc.y = AltitudeLayer.MoteLow.AltitudeFor() + 0.01f;
            float floorScale = exposed ? 7.25f : 5.95f + Mathf.Min(liveAnchors, 3) * 0.38f;
            DrawLayer(FloorPulseMaterial, floorLoc, (ticks + seed) * (exposed ? -0.055f : 0.030f), floorScale * breathe, exposed ? 0.87f : 0.82f);

            Vector3 haloLoc = heartPos;
            haloLoc.y = yBase + 0.012f;

            if (!exposed)
            {
                float outerScale = (7.95f + liveAnchors * 0.34f) * breathe;
                float innerScale = (5.35f + liveAnchors * 0.26f) * shimmer;
                float latticeScale = (4.8f + liveAnchors * 0.22f) * (1f + (breathe - 1f) * 0.55f);
                float crownScale = (8.65f + liveAnchors * 0.36f) * phasePulse;

                DrawLayer(CrownRingMaterial, haloLoc + new Vector3(0f, 0.001f, 0f), -(ticks + seed) * 0.020f, crownScale, 0.58f);
                DrawLayer(OuterHaloMaterial, haloLoc, (ticks + seed) * 0.060f, outerScale, 0.84f);
                DrawLayer(InnerHaloMaterial, haloLoc + new Vector3(0f, 0.002f, 0f), -(ticks + seed) * 0.047f, innerScale, 0.92f);
                DrawLayer(ShieldLatticeMaterial, haloLoc + new Vector3(0f, 0.004f, 0f), (ticks + seed) * 0.028f, latticeScale, 0.90f);
            }
            else
            {
                float outerScale = 7.45f * breathe;
                float innerScale = 5.6f * shimmer;
                float crownScale = 9.1f * phasePulse;
                float coronaScale = 4.35f * (1f + Mathf.Sin((ticks + seed) * 0.063f) * 0.05f);
                float coreScale = 2.55f * (1f + Mathf.Sin((ticks + seed) * 0.085f) * 0.06f);

                DrawLayer(CrownRingMaterial, haloLoc + new Vector3(0f, 0.001f, 0f), -(ticks + seed) * 0.030f, crownScale, 0.64f);
                DrawLayer(OuterHaloMaterial, haloLoc, (ticks + seed) * 0.090f, outerScale, 0.92f);
                DrawLayer(InnerHaloMaterial, haloLoc + new Vector3(0f, 0.002f, 0f), -(ticks + seed) * 0.075f, innerScale, 0.98f);
                DrawLayer(CoreCoronaMaterial, haloLoc + new Vector3(0f, 0.005f, 0f), (ticks + seed) * 0.115f, coronaScale, 0.86f);
                DrawLayer(ExposedCoreMaterial, haloLoc + new Vector3(0f, 0.006f, 0f), (ticks + seed) * 0.120f, coreScale, 1f);
            }
        }

        public static void SpawnHeartbeatPulse(Vector3 heartPos, Map map, bool exposed)
        {
            if (map == null)
            {
                return;
            }

            ThingDef beatDef = exposed ? HeartBeatExposedMoteDef : HeartBeatMoteDef;
            if (beatDef != null)
            {
                MoteMaker.MakeStaticMote(heartPos, map, beatDef, exposed ? 2.7f : 1.95f);
            }

            if (exposed)
            {
                ThingDef coreFlareDef = HeartCoreFlareMoteDef;
                if (coreFlareDef != null)
                {
                    MoteMaker.MakeStaticMote(heartPos + new Vector3(0f, 0.005f, 0f), map, coreFlareDef, 1.45f);
                    MoteMaker.MakeStaticMote(heartPos + new Vector3(0f, 0.005f, 0f), map, coreFlareDef, 0.98f);
                }

                FleckMaker.ThrowLightningGlow(heartPos, map, 2.6f);
                FleckMaker.ThrowMicroSparks(heartPos, map);
            }
            else
            {
                FleckMaker.ThrowLightningGlow(heartPos, map, 1.75f);
            }
        }

        private static void DrawLayer(Material material, Vector3 loc, float rotation, float scale, float alpha)
        {
            if (material == null)
            {
                return;
            }

            Color color = material.color;
            material.color = new Color(color.r, color.g, color.b, alpha);
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.AngleAxis(rotation, Vector3.up), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            material.color = color;
        }
    }
}
