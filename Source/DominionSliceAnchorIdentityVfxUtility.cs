using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class DominionSliceAnchorIdentityVfxUtility
    {
        private const string SealZoneTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_AnchorZone_Seal";
        private const string ChoirZoneTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_AnchorZone_Choir";
        private const string LawZoneTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_AnchorZone_Law";
        private const string CoreGlyphTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_AnchorZone_CoreGlyph";

        private static readonly Material SealZoneMaterial = MaterialPool.MatFrom(SealZoneTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material ChoirZoneMaterial = MaterialPool.MatFrom(ChoirZoneTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material LawZoneMaterial = MaterialPool.MatFrom(LawZoneTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material CoreGlyphMaterial = MaterialPool.MatFrom(CoreGlyphTexPath, ShaderDatabase.MoteGlow);

        public static void DrawAnchorIdentityZone(Vector3 drawLoc, Map map, DominionSliceAnchorRole role, bool activeEncounter, bool anchorfallActive, int seed)
        {
            if (map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float anchorfallBoost = anchorfallActive ? 1f : 0.82f;
            float pulse = 1f + Mathf.Sin((ticks + seed) * 0.046f) * 0.055f;
            float secondaryPulse = 1f + Mathf.Sin((ticks + seed) * 0.029f + 1.3f) * 0.035f;

            float baseScale;
            float glyphScale;
            float alpha;
            Material zoneMaterial = GetZoneMaterial(role, out baseScale, out glyphScale, out alpha);
            if (zoneMaterial == null)
            {
                return;
            }

            if (!activeEncounter)
            {
                alpha *= 0.78f;
            }

            Vector3 floorLoc = drawLoc;
            floorLoc.y = AltitudeLayer.MoteLow.AltitudeFor() + 0.012f;
            float rotation = (ticks + seed) * GetRotationSpeed(role);
            DrawLayer(zoneMaterial, floorLoc, rotation, baseScale * pulse * anchorfallBoost, alpha);

            Vector3 glyphLoc = drawLoc;
            glyphLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.031f;
            float glyphRotation = -(ticks + seed) * (GetRotationSpeed(role) * 0.72f);
            DrawLayer(CoreGlyphMaterial, glyphLoc, glyphRotation, glyphScale * secondaryPulse * anchorfallBoost, alpha * 0.92f);
        }

        public static void SpawnAnchorPulse(Vector3 drawLoc, Map map, DominionSliceAnchorRole role)
        {
            if (map == null)
            {
                return;
            }

            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    FleckMaker.ThrowLightningGlow(drawLoc, map, 1.9f);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(0.30f, 0f, 0f), map);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(-0.30f, 0f, 0f), map);
                    break;
                case DominionSliceAnchorRole.Law:
                    FleckMaker.ThrowLightningGlow(drawLoc, map, 2.1f);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(0.22f, 0f, 0.22f), map);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(-0.22f, 0f, -0.22f), map);
                    break;
                default:
                    FleckMaker.ThrowLightningGlow(drawLoc, map, 1.75f);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(0f, 0f, 0.30f), map);
                    break;
            }
        }

        private static Material GetZoneMaterial(DominionSliceAnchorRole role, out float baseScale, out float glyphScale, out float alpha)
        {
            baseScale = 10.8f;
            glyphScale = 4.35f;
            alpha = 0.88f;

            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    baseScale = 10.9f;
                    glyphScale = 4.55f;
                    alpha = 0.86f;
                    return ChoirZoneMaterial;
                case DominionSliceAnchorRole.Law:
                    baseScale = 11.2f;
                    glyphScale = 4.45f;
                    alpha = 0.90f;
                    return LawZoneMaterial;
                default:
                    baseScale = 10.6f;
                    glyphScale = 4.25f;
                    alpha = 0.88f;
                    return SealZoneMaterial;
            }
        }

        private static float GetRotationSpeed(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.045f;
                case DominionSliceAnchorRole.Law:
                    return 0.024f;
                default:
                    return -0.018f;
            }
        }

        private static void DrawLayer(Material material, Vector3 loc, float rotation, float scale, float alpha)
        {
            if (material == null)
            {
                return;
            }

            Color originalColor = material.color;
            material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.AngleAxis(rotation, Vector3.up), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            material.color = originalColor;
        }
    }
}
