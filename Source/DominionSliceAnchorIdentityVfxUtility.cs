using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Restrained Dominion Slice anchor presentation.
    ///
    /// The anchor platform texture is the lower base. This utility draws a compact role-colored
    /// machine pylon/core above it so anchors remain visible without the previous broad ritual
    /// circles, floor glyphs or halo zones.
    /// </summary>
    public static class DominionSliceAnchorIdentityVfxUtility
    {
        private const string CoreFlarePath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCoreFlare";
        private const string ExposedCorePath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartExposedCore";

        private static Graphic sealCoreGraphic;
        private static Graphic choirCoreGraphic;
        private static Graphic lawCoreGraphic;
        private static Graphic sealFlareGraphic;
        private static Graphic choirFlareGraphic;
        private static Graphic lawFlareGraphic;

        public static void DrawAnchorIdentityZone(Vector3 anchorPos, Map map, DominionSliceAnchorRole role, bool activeEncounter, bool anchorfallActive, int seed)
        {
            if (map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float pulse = activeEncounter ? Mathf.Sin((ticks + seed) * 0.060f) : 0f;
            float coreScale = anchorfallActive ? 1.58f : activeEncounter ? 1.38f : 1.16f;
            coreScale += pulse * 0.07f;
            float flareScale = anchorfallActive ? 0.96f : activeEncounter ? 0.84f : 0.72f;
            flareScale += pulse * 0.04f;

            DrawAnchorCore(anchorPos, role, coreScale, flareScale, 0.318f);
        }

        public static void DrawAnchorIdentityZone(Vector3 anchorPos, Map map, DominionSliceAnchorRole role, int seed, MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            DrawAnchorIdentityZone(anchorPos, map, role, true, phase == MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall, seed);
        }

        public static void SpawnAnchorPulse(Vector3 drawLoc, Map map, DominionSliceAnchorRole role)
        {
            if (map == null)
            {
                return;
            }

            float glowScale;
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    glowScale = 0.92f;
                    break;
                case DominionSliceAnchorRole.Law:
                    glowScale = 1.02f;
                    break;
                default:
                    glowScale = 0.86f;
                    break;
            }

            FleckMaker.ThrowLightningGlow(drawLoc, map, glowScale);
            FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(Rand.Range(-0.18f, 0.18f), 0f, Rand.Range(-0.18f, 0.18f)), map);
        }

        private static void DrawAnchorCore(Vector3 anchorPos, DominionSliceAnchorRole role, float coreScale, float flareScale, float altitudeOffset)
        {
            Graphic coreGraphic;
            Graphic flareGraphic;
            Color coreColor;
            Color flareColor;
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    coreColor = new Color(0.72f, 0.16f, 1f, 0.98f);
                    flareColor = new Color(0.92f, 0.70f, 1f, 0.88f);
                    coreGraphic = GetGraphic(ref choirCoreGraphic, ExposedCorePath, coreColor);
                    flareGraphic = GetGraphic(ref choirFlareGraphic, CoreFlarePath, flareColor);
                    break;
                case DominionSliceAnchorRole.Law:
                    coreColor = new Color(0.30f, 0.44f, 1f, 0.96f);
                    flareColor = new Color(0.72f, 0.82f, 1f, 0.82f);
                    coreGraphic = GetGraphic(ref lawCoreGraphic, ExposedCorePath, coreColor);
                    flareGraphic = GetGraphic(ref lawFlareGraphic, CoreFlarePath, flareColor);
                    break;
                default:
                    coreColor = new Color(1f, 0.10f, 0.04f, 0.99f);
                    flareColor = new Color(1f, 0.62f, 0.48f, 0.86f);
                    coreGraphic = GetGraphic(ref sealCoreGraphic, ExposedCorePath, coreColor);
                    flareGraphic = GetGraphic(ref sealFlareGraphic, CoreFlarePath, flareColor);
                    break;
            }

            DrawGraphic(coreGraphic, anchorPos, coreScale, altitudeOffset);
            DrawGraphic(flareGraphic, anchorPos, flareScale, altitudeOffset + 0.010f);
        }

        private static Graphic GetGraphic(ref Graphic graphic, string path, Color color)
        {
            if (graphic == null)
            {
                graphic = GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Transparent, Vector2.one, color);
            }

            return graphic;
        }

        private static void DrawGraphic(Graphic graphic, Vector3 anchorPos, float scale, float altitudeOffset)
        {
            if (graphic == null || graphic.MatSingle == null)
            {
                return;
            }

            Vector3 loc = anchorPos;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + altitudeOffset;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }
    }
}
