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
            float slowPulse = 1f + Mathf.Sin((ticks + seed) * 0.022f + roleOffset) * 0.045f;
            float fastPulse = 1f + Mathf.Sin((ticks + seed) * 0.055f + roleOffset) * 0.055f;
            float scale = GetRoleBaseScale(role) * slowPulse * Mathf.Lerp(0.88f, 1.08f, intensity);

            Vector3 loc = anchorPos;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() - 0.045f;

            Material zoneMaterial = GetZoneMaterial(role);
            if (zoneMaterial != null)
            {
                float rotation = GetRoleRotation(role, ticks, seed);
                Matrix4x4 zoneMatrix = Matrix4x4.TRS(loc, Quaternion.AngleAxis(rotation, Vector3.up), new Vector3(scale, 1f, scale));
                Graphics.DrawMesh(MeshPool.plane10, zoneMatrix, zoneMaterial, 0);
            }

            if (CoreGlyphMaterial != null)
            {
                float coreScale = scale * (0.43f + intensity * 0.035f) * fastPulse;
                float coreRotation = -GetRoleRotation(role, ticks, seed) * 0.72f;
                Vector3 coreLoc = loc + new Vector3(0f, 0.002f, 0f);
                Matrix4x4 coreMatrix = Matrix4x4.TRS(coreLoc, Quaternion.AngleAxis(coreRotation, Vector3.up), new Vector3(coreScale, 1f, coreScale));
                Graphics.DrawMesh(MeshPool.plane10, coreMatrix, CoreGlyphMaterial, 0);
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
                    return 5.25f;
                case DominionSliceAnchorRole.Law:
                    return 5.75f;
                default:
                    return 5.55f;
            }
        }

        private static float GetRoleRotation(DominionSliceAnchorRole role, int ticks, int seed)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return Mathf.Sin((ticks + seed) * 0.018f) * 5.5f;
                case DominionSliceAnchorRole.Law:
                    return ((ticks + seed) * 0.035f) % 360f;
                default:
                    return Mathf.Sin((ticks + seed) * 0.012f) * 2.2f;
            }
        }

        private static float GetRoleOffset(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 1.7f;
                case DominionSliceAnchorRole.Law:
                    return 3.4f;
                default:
                    return 0.25f;
            }
        }

        private static float GetPhaseIntensity(MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            switch (phase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    return 0.42f;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return 1f;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return 0.72f;
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return 1.15f;
                default:
                    return 0f;
            }
        }
    }
}
