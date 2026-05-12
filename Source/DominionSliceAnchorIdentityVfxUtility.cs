using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Restrained Dominion Slice anchor presentation.
    ///
    /// Broad animated ritual circles are disabled. Anchors now get only a compact, on-top machine
    /// core overlay so the industrial platform art does not swallow the actual interactable anchor.
    /// </summary>
    public static class DominionSliceAnchorIdentityVfxUtility
    {
        private const string CoreFlarePath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartCoreFlare";
        private static Graphic sealCoreGraphic;
        private static Graphic choirCoreGraphic;
        private static Graphic lawCoreGraphic;

        public static void DrawAnchorIdentityZone(Vector3 anchorPos, Map map, DominionSliceAnchorRole role, bool activeEncounter, bool anchorfallActive, int seed)
        {
            if (map == null)
            {
                return;
            }

            // No floor glyphs or halo zones. This draw is intentionally compact and above the
            // platform texture, acting as the visible machine core of the anchor.
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float pulse = activeEncounter ? Mathf.Sin((ticks + seed) * 0.055f) : 0f;
            float baseScale = anchorfallActive ? 0.92f : 0.72f;
            float scale = baseScale + pulse * 0.045f;
            DrawAnchorCore(anchorPos, role, scale, 0.108f);
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
                    glowScale = 0.72f;
                    break;
                case DominionSliceAnchorRole.Law:
                    glowScale = 0.80f;
                    break;
                default:
                    glowScale = 0.64f;
                    break;
            }

            FleckMaker.ThrowLightningGlow(drawLoc, map, glowScale);
            FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(Rand.Range(-0.18f, 0.18f), 0f, Rand.Range(-0.18f, 0.18f)), map);
        }

        private static void DrawAnchorCore(Vector3 anchorPos, DominionSliceAnchorRole role, float scale, float altitudeOffset)
        {
            Graphic graphic;
            Color color;
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    graphic = GetGraphic(ref choirCoreGraphic, new Color(0.74f, 0.18f, 1f, 0.88f));
                    color = new Color(0.74f, 0.18f, 1f, 0.88f);
                    break;
                case DominionSliceAnchorRole.Law:
                    graphic = GetGraphic(ref lawCoreGraphic, new Color(0.18f, 0.34f, 1f, 0.86f));
                    color = new Color(0.18f, 0.34f, 1f, 0.86f);
                    break;
                default:
                    graphic = GetGraphic(ref sealCoreGraphic, new Color(1f, 0.12f, 0.06f, 0.90f));
                    color = new Color(1f, 0.12f, 0.06f, 0.90f);
                    break;
            }

            if (graphic == null || graphic.MatSingle == null)
            {
                return;
            }

            Vector3 loc = anchorPos;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + altitudeOffset;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }

        private static Graphic GetGraphic(ref Graphic graphic, Color color)
        {
            if (graphic == null)
            {
                graphic = GraphicDatabase.Get<Graphic_Single>(CoreFlarePath, ShaderDatabase.Transparent, Vector2.one, color);
            }

            return graphic;
        }
    }
}
