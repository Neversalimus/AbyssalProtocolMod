using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Restrained Dominion Slice heart presentation.
    ///
    /// The platform art is the lower industrial base. This utility draws a compact machine-heart
    /// core above it so the interactable heart remains readable without bringing back the old
    /// magical floor rings, halo stack, crown seal or broad shield glyphs.
    /// </summary>
    public static class DominionSliceHeartSetpieceVfxUtility
    {
        private const string CoreCoronaPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCoreCorona";
        private const string CoreFlarePath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCoreFlare";
        private const string ExposedCorePath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartExposedCore";

        private static Graphic coreCoronaGraphic;
        private static Graphic coreFlareGraphic;
        private static Graphic exposedCoreGraphic;

        public static void DrawHeartSetpiece(Vector3 heartPos, Map map, MapComponent_DominionSliceEncounter encounter, int seed)
        {
            if (map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            bool exposed = encounter != null && encounter.IsHeartExposed;
            bool active = encounter != null && encounter.IsActiveEncounter;
            float phase = Mathf.Sin((ticks + seed) * 0.052f);

            // Always visible: this is the actual heart/readability layer, not a magic-circle layer.
            float coreScale = exposed ? 2.35f + phase * 0.12f : active ? 2.05f + phase * 0.08f : 1.78f;
            float flareScale = exposed ? 1.35f + phase * 0.07f : active ? 1.16f + phase * 0.05f : 1.02f;
            float coronaScale = exposed ? 1.68f + phase * 0.08f : active ? 1.36f + phase * 0.05f : 1.12f;

            DrawOverlay(ExposedCorePath, ref exposedCoreGraphic, heartPos, coreScale, exposed ? HeartExposedColor : HeartShieldedColor, 0.325f);
            DrawOverlay(CoreCoronaPath, ref coreCoronaGraphic, heartPos, coronaScale, exposed ? HeartExposedCoronaColor : HeartShieldedCoronaColor, 0.332f);
            DrawOverlay(CoreFlarePath, ref coreFlareGraphic, heartPos, flareScale, Color.white, 0.340f);
        }

        public static void SpawnHeartbeatPulse(Vector3 heartPos, Map map, bool exposed)
        {
            if (map == null)
            {
                return;
            }

            // Small feedback only: no floor rings, crown rings, exposed-core circles, or shield halos.
            float glowScale = exposed ? 1.25f : 0.78f;
            FleckMaker.ThrowLightningGlow(heartPos, map, glowScale);

            if (exposed)
            {
                FleckMaker.ThrowMicroSparks(heartPos + new Vector3(0.18f, 0f, 0.10f), map);
                FleckMaker.ThrowMicroSparks(heartPos + new Vector3(-0.16f, 0f, -0.12f), map);
            }
        }

        private static Color HeartShieldedColor
        {
            get { return new Color(1f, 0.16f, 0.08f, 0.96f); }
        }

        private static Color HeartExposedColor
        {
            get { return new Color(1f, 0.05f, 0.025f, 1f); }
        }

        private static Color HeartShieldedCoronaColor
        {
            get { return new Color(1f, 0.10f, 0.055f, 0.46f); }
        }

        private static Color HeartExposedCoronaColor
        {
            get { return new Color(1f, 0.04f, 0.02f, 0.62f); }
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
