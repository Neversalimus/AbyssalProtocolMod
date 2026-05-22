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
    [StaticConstructorOnStartup]
    public static class DominionSliceHeartSetpieceVfxUtility
    {
        private const string CoreCoronaPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCoreCorona";
        private const string CoreFlarePath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCoreFlare";
        private const string ExposedCorePath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartExposedCore";
        private const string HeartOverlayPulseSheetPath = "Things/Building/DominionSlice/Heart/ABY_DominionSlice_Heart_OverlayPulse_Sheet";
        private const int HeartOverlayPulseFrameCount = 8;

        private static Graphic coreCoronaGraphic;
        private static Graphic coreFlareGraphic;
        private static Graphic exposedCoreGraphic;
        private static Mesh[] heartOverlayPulseMeshes;
        private static Material heartOverlayPulseInactiveMaterial;
        private static Material heartOverlayPulseActiveMaterial;
        private static Material heartOverlayPulseExposedMaterial;

        // The pulse sheet is authored at the same logical footprint as the approved heart graphic.
        // Keep this near the ThingDef drawSize; the frame mesh itself is unit-sized.
        // Previous build used a 10-cell mesh * 9.6 scale, which expanded the overlay across most of the map.
        private const float HeartOverlayPulseDrawSize = 9.55f;

        public static void DrawHeartSetpiece(Vector3 heartPos, Map map, MapComponent_DominionSliceEncounter encounter, int seed)
        {
            if (map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            bool exposed = encounter != null && encounter.IsHeartExposed;
            bool active = encounter != null && encounter.IsActiveEncounter;

            // Always visible: this is the actual heart/readability layer, not a magic-circle layer.
            // Keep these support overlays static; the heartbeat itself is authored in the overlay sprite sheet below.
            float coreScale = exposed ? 2.35f : active ? 2.05f : 1.78f;
            float flareScale = exposed ? 1.35f : active ? 1.16f : 1.02f;
            float coronaScale = exposed ? 1.68f : active ? 1.36f : 1.12f;

            DrawOverlay(ExposedCorePath, ref exposedCoreGraphic, heartPos, coreScale, exposed ? HeartExposedColor : HeartShieldedColor, 0.325f);
            DrawOverlay(CoreCoronaPath, ref coreCoronaGraphic, heartPos, coronaScale, exposed ? HeartExposedCoronaColor : HeartShieldedCoronaColor, 0.332f);
            DrawOverlay(CoreFlarePath, ref coreFlareGraphic, heartPos, flareScale, Color.white, 0.340f);
            DrawHeartOverlayPulseSheet(heartPos, ticks, seed, active, exposed);
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


        private static Color HeartOverlayInactiveColor
        {
            get { return new Color(1f, 0.36f, 0.16f, 0.24f); }
        }

        private static Color HeartOverlayActiveColor
        {
            get { return new Color(1f, 0.34f, 0.12f, 0.42f); }
        }

        private static Color HeartOverlayExposedColor
        {
            get { return new Color(1f, 0.26f, 0.08f, 0.54f); }
        }

        private static void DrawHeartOverlayPulseSheet(Vector3 heartPos, int ticks, int seed, bool active, bool exposed)
        {
            EnsureHeartOverlayPulseMeshes();
            if (heartOverlayPulseMeshes == null || heartOverlayPulseMeshes.Length != HeartOverlayPulseFrameCount)
            {
                return;
            }

            Material material = GetHeartOverlayPulseMaterial(active, exposed);
            if (material == null)
            {
                return;
            }

            int frameDuration = exposed ? 6 : active ? 8 : 12;
            int frame = Mathf.Abs((ticks + seed) / frameDuration) % HeartOverlayPulseFrameCount;
            Mesh mesh = heartOverlayPulseMeshes[frame];
            if (mesh == null)
            {
                return;
            }

            Vector3 loc = heartPos;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.348f;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(HeartOverlayPulseDrawSize, 1f, HeartOverlayPulseDrawSize));
            Graphics.DrawMesh(mesh, matrix, material, 0);
        }

        private static Material GetHeartOverlayPulseMaterial(bool active, bool exposed)
        {
            if (exposed)
            {
                if (heartOverlayPulseExposedMaterial == null)
                {
                    heartOverlayPulseExposedMaterial = ABY_MaterialCacheUtility.MatFrom(HeartOverlayPulseSheetPath, ShaderDatabase.TransparentPostLight, HeartOverlayExposedColor);
                }

                return heartOverlayPulseExposedMaterial;
            }

            if (active)
            {
                if (heartOverlayPulseActiveMaterial == null)
                {
                    heartOverlayPulseActiveMaterial = ABY_MaterialCacheUtility.MatFrom(HeartOverlayPulseSheetPath, ShaderDatabase.TransparentPostLight, HeartOverlayActiveColor);
                }

                return heartOverlayPulseActiveMaterial;
            }

            if (heartOverlayPulseInactiveMaterial == null)
            {
                heartOverlayPulseInactiveMaterial = ABY_MaterialCacheUtility.MatFrom(HeartOverlayPulseSheetPath, ShaderDatabase.TransparentPostLight, HeartOverlayInactiveColor);
            }

            return heartOverlayPulseInactiveMaterial;
        }

        private static void EnsureHeartOverlayPulseMeshes()
        {
            if (heartOverlayPulseMeshes != null)
            {
                return;
            }

            heartOverlayPulseMeshes = new Mesh[HeartOverlayPulseFrameCount];
            for (int i = 0; i < HeartOverlayPulseFrameCount; i++)
            {
                float uMin = i / (float)HeartOverlayPulseFrameCount;
                float uMax = (i + 1) / (float)HeartOverlayPulseFrameCount;
                Mesh mesh = new Mesh();
                mesh.name = "ABY_DominionSliceHeartOverlayPulseFrame_" + i;
                // Unit-sized custom quad. The draw size is controlled only by HeartOverlayPulseDrawSize above.
                mesh.vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, -0.5f)
                };
                mesh.uv = new[]
                {
                    new Vector2(uMin, 0f),
                    new Vector2(uMin, 1f),
                    new Vector2(uMax, 1f),
                    new Vector2(uMax, 0f)
                };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.RecalculateBounds();
                heartOverlayPulseMeshes[i] = mesh;
            }
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
