using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Restrained Dominion Slice heart presentation.
    ///
    /// The old implementation drew broad rotating halo/ring layers around the heart. The Dominion
    /// Sepulcher pass keeps those magic circles disabled, but still draws a small machine-core
    /// overlay above the industrial platform so the actual heart remains readable in combat.
    /// </summary>
    public static class DominionSliceHeartSetpieceVfxUtility
    {
        private const string CoreCoronaPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCoreCorona";
        private const string CoreFlarePath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCoreFlare";

        private static Graphic coreCoronaGraphic;
        private static Graphic coreFlareGraphic;

        public static void DrawHeartSetpiece(Vector3 heartPos, Map map, MapComponent_DominionSliceEncounter encounter, int seed)
        {
            if (map == null || encounter == null)
            {
                return;
            }

            // Small over-platform core only. This is intentionally not a floor ring, shield halo,
            // crown seal, or rotating glyph stack.
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            bool exposed = encounter.IsHeartExposed;
            float phase = Mathf.Sin((ticks + seed) * 0.047f);
            float coronaScale = exposed ? 2.35f + phase * 0.12f : 1.72f + phase * 0.08f;
            float flareScale = exposed ? 1.22f + phase * 0.06f : 0.92f + phase * 0.04f;

            DrawOverlay(CoreCoronaPath, ref coreCoronaGraphic, heartPos, coronaScale, exposed ? HeartExposedColor : HeartShieldedColor, 0.118f);
            DrawOverlay(CoreFlarePath, ref coreFlareGraphic, heartPos, flareScale, Color.white, 0.124f);
        }

        public static void SpawnHeartbeatPulse(Vector3 heartPos, Map map, bool exposed)
        {
            if (map == null)
            {
                return;
            }

            // Small feedback only: no floor rings, crown rings, exposed-core circles, or shield halos.
            float glowScale = exposed ? 0.9f : 0.48f;
            FleckMaker.ThrowLightningGlow(heartPos, map, glowScale);

            if (exposed)
            {
                FleckMaker.ThrowMicroSparks(heartPos + new Vector3(0.18f, 0f, 0.10f), map);
                FleckMaker.ThrowMicroSparks(heartPos + new Vector3(-0.16f, 0f, -0.12f), map);
            }
        }

        private static Color HeartShieldedColor
        {
            get { return new Color(0.95f, 0.18f, 0.10f, 0.86f); }
        }

        private static Color HeartExposedColor
        {
            get { return new Color(1f, 0.08f, 0.04f, 0.96f); }
        }

        private static void DrawOverlay(string path, ref Graphic graphic, Vector3 drawPos, float scale, Color color, float altitudeOffset)
        {
            if (graphic == null)
            {
                graphic = GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Transparent, Vector2.one, color);
            }

            if (graphic == null || graphic.MatSingle == null)
            {
                return;
            }

            Vector3 loc = drawPos;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + altitudeOffset;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }
    }
}
