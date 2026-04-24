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

        public static void DrawAnchorIdentityZone(Vector3 anchorPos, Map map, DominionSliceAnchorRole role, int seed, MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            if (map == null || phase == MapComponent_DominionSliceEncounter.SlicePhase.Dormant || phase == MapComponent_DominionSliceEncounter.SlicePhase.Failed)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float intensity = GetPhaseIntensity(phase);
            if (intensity <= 0.01f)
            {
                return;
            }

            float roleOffset = GetRoleOffset(role);
            float slowPulse = 1f + Mathf.Sin((ticks + seed) * 0.020f + roleOffset) * GetRoleSlowPulse(role);
            float fastPulse = 1f + Mathf.Sin((ticks + seed) * GetRoleFastRate(role) + roleOffset) * GetRoleFastPulse(role);
            float phaseScale = Mathf.Lerp(0.92f, 1.16f, intensity);
            float scale = GetRoleBaseScale(role) * slowPulse * phaseScale;

            Vector3 loc = anchorPos;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() - 0.047f;

            Material zoneMaterial = GetZoneMaterial(role);
            if (zoneMaterial != null)
            {
                float rotation = GetRoleRotation(role, ticks, seed);
                float alpha = GetRoleZoneAlpha(role) * Mathf.Lerp(0.74f, 1f, intensity);
                DrawLayer(zoneMaterial, loc, rotation, scale, alpha);

                // A counter-rotating ghost layer gives each anchor a more authored identity without adding extra defs.
                float echoScale = scale * GetRoleEchoScale(role) * (1f + (fastPulse - 1f) * 0.45f);
                float echoAlpha = alpha * GetRoleEchoAlpha(role);
                DrawLayer(zoneMaterial, loc + new Vector3(0f, 0.0015f, 0f), -rotation * GetRoleEchoRotationFactor(role), echoScale, echoAlpha);
            }

            if (CoreGlyphMaterial != null)
            {
                float coreScale = scale * GetRoleCoreScale(role) * fastPulse;
                float coreRotation = -GetRoleRotation(role, ticks, seed) * GetRoleCoreRotationFactor(role);
                Vector3 coreLoc = loc + new Vector3(0f, 0.004f, 0f);
                DrawLayer(CoreGlyphMaterial, coreLoc, coreRotation, coreScale, Mathf.Lerp(0.72f, 0.98f, intensity));
            }
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
                    FleckMaker.ThrowLightningGlow(drawLoc, map, 2.05f);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(0.34f, 0f, 0f), map);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(-0.34f, 0f, 0f), map);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(0f, 0f, 0.34f), map);
                    break;
                case DominionSliceAnchorRole.Law:
                    FleckMaker.ThrowLightningGlow(drawLoc, map, 2.25f);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(0.28f, 0f, 0.28f), map);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(-0.28f, 0f, -0.28f), map);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(0.28f, 0f, -0.28f), map);
                    break;
                default:
                    FleckMaker.ThrowLightningGlow(drawLoc, map, 1.95f);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(0f, 0f, 0.34f), map);
                    FleckMaker.ThrowMicroSparks(drawLoc + new Vector3(0.28f, 0f, -0.16f), map);
                    break;
            }
        }

        private static Material GetZoneMaterial(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return ChoirZoneMaterial;
                case DominionSliceAnchorRole.Law:
                    return LawZoneMaterial;
                default:
                    return SealZoneMaterial;
            }
        }

        private static float GetRoleBaseScale(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 12.15f;
                case DominionSliceAnchorRole.Law:
                    return 12.45f;
                default:
                    return 11.85f;
            }
        }

        private static float GetRoleCoreScale(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.405f;
                case DominionSliceAnchorRole.Law:
                    return 0.385f;
                default:
                    return 0.395f;
            }
        }

        private static float GetRoleEchoScale(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.78f;
                case DominionSliceAnchorRole.Law:
                    return 0.86f;
                default:
                    return 0.82f;
            }
        }

        private static float GetRoleEchoAlpha(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.42f;
                case DominionSliceAnchorRole.Law:
                    return 0.36f;
                default:
                    return 0.40f;
            }
        }

        private static float GetRoleEchoRotationFactor(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.55f;
                case DominionSliceAnchorRole.Law:
                    return 0.35f;
                default:
                    return 0.45f;
            }
        }

        private static float GetRoleCoreRotationFactor(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.95f;
                case DominionSliceAnchorRole.Law:
                    return 0.52f;
                default:
                    return 0.72f;
            }
        }

        private static float GetRoleZoneAlpha(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.92f;
                case DominionSliceAnchorRole.Law:
                    return 0.96f;
                default:
                    return 0.94f;
            }
        }

        private static float GetRoleFastRate(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.071f;
                case DominionSliceAnchorRole.Law:
                    return 0.044f;
                default:
                    return 0.052f;
            }
        }

        private static float GetRoleSlowPulse(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.072f;
                case DominionSliceAnchorRole.Law:
                    return 0.040f;
                default:
                    return 0.052f;
            }
        }

        private static float GetRoleFastPulse(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.082f;
                case DominionSliceAnchorRole.Law:
                    return 0.040f;
                default:
                    return 0.058f;
            }
        }

        private static float GetRoleRotation(DominionSliceAnchorRole role, int ticks, int seed)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return (ticks + seed) * 0.074f + Mathf.Sin((ticks + seed) * 0.031f) * 7.0f;
                case DominionSliceAnchorRole.Law:
                    return (ticks + seed) * 0.022f;
                default:
                    return -(ticks + seed) * 0.032f;
            }
        }

        private static float GetRoleOffset(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 1.71f;
                case DominionSliceAnchorRole.Law:
                    return 3.38f;
                default:
                    return 0.36f;
            }
        }

        private static float GetPhaseIntensity(MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            switch (phase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    return 0.45f;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return 1f;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return 0.88f;
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return 0.72f;
                default:
                    return 0f;
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
