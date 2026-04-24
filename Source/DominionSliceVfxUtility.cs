using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class DominionSliceVfxUtility
    {
        private const string LinkBeamTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_LinkBeam";
        private const string LinkCoreTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_LinkCore";
        private const string LinkEntryBloomTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_LinkEntryBloom";
        private const string HeartShieldTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartShield";
        private const string AnchorBreakMoteDefName = "ABY_Mote_DominionSliceAnchorBreak";
        private const string HeartExposeMoteDefName = "ABY_Mote_DominionSliceHeartExpose";
        private const string ShieldBlockMoteDefName = "ABY_Mote_DominionSliceShieldBlock";

        private static readonly Material LinkBeamMaterial = MaterialPool.MatFrom(LinkBeamTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material LinkCoreMaterial = MaterialPool.MatFrom(LinkCoreTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material LinkEntryBloomMaterial = MaterialPool.MatFrom(LinkEntryBloomTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material HeartShieldMaterial = MaterialPool.MatFrom(HeartShieldTexPath, ShaderDatabase.MoteGlow);

        private static ThingDef anchorBreakMoteDef;
        private static ThingDef heartExposeMoteDef;
        private static ThingDef shieldBlockMoteDef;

        private static ThingDef AnchorBreakMoteDef
        {
            get { return anchorBreakMoteDef ?? (anchorBreakMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(AnchorBreakMoteDefName)); }
        }

        private static ThingDef HeartExposeMoteDef
        {
            get { return heartExposeMoteDef ?? (heartExposeMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(HeartExposeMoteDefName)); }
        }

        private static ThingDef ShieldBlockMoteDef
        {
            get { return shieldBlockMoteDef ?? (shieldBlockMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ShieldBlockMoteDefName)); }
        }

        public static void DrawAnchorLink(Vector3 anchorPos, Vector3 heartPos, Map map, DominionSliceAnchorRole role, int seed)
        {
            if (map == null)
            {
                return;
            }

            anchorPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.012f;
            heartPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.012f;

            Vector3 delta = heartPos - anchorPos;
            float length = delta.MagnitudeHorizontal();
            if (length <= 0.25f)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float roleOffset = GetRolePhaseOffset(role);
            float pulse = 1f + Mathf.Sin((ticks + seed) * 0.044f + roleOffset) * 0.08f;
            float breath = 1f + Mathf.Sin((ticks + seed) * 0.017f + roleOffset) * 0.05f;
            float surge = 1f + Mathf.Sin((ticks + seed) * 0.072f + roleOffset) * 0.035f;
            float width = GetRoleWidth(role) * pulse;

            DrawBeam(anchorPos, heartPos, width * 3.05f, length, LinkBeamMaterial, breath);
            DrawBeam(anchorPos, heartPos, width * 1.35f, length, LinkBeamMaterial, 1f + (surge - 1f) * 0.8f);
            DrawBeam(anchorPos, heartPos, width * 0.58f, length, LinkCoreMaterial, 1f + (pulse - 1f) * 0.65f);
            DrawLinkEntryBloom(heartPos, role, seed, ticks, 1.18f + width * 3.6f);
        }

        public static void DrawHeartShield(Vector3 heartPos, Map map, int liveAnchors, int seed)
        {
            if (map == null || liveAnchors <= 0)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float pulse = 1f + Mathf.Sin((ticks + seed) * 0.032f) * 0.045f;
            float scale = (5.8f + liveAnchors * 0.42f) * pulse;

            Vector3 loc = heartPos;
            loc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.006f;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.AngleAxis((ticks + seed) * 0.075f, Vector3.up), new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, HeartShieldMaterial, 0);

            float innerScale = scale * 0.74f;
            Matrix4x4 innerMatrix = Matrix4x4.TRS(loc + new Vector3(0f, 0.002f, 0f), Quaternion.AngleAxis(-(ticks + seed) * 0.045f, Vector3.up), new Vector3(innerScale, 1f, innerScale));
            Graphics.DrawMesh(MeshPool.plane10, innerMatrix, HeartShieldMaterial, 0);
        }

        public static void SpawnAnchorBreakFlare(Vector3 position, Map map, DominionSliceAnchorRole role)
        {
            if (map == null)
            {
                return;
            }

            ThingDef moteDef = AnchorBreakMoteDef;
            if (moteDef != null)
            {
                float scale = role == DominionSliceAnchorRole.Law ? 2.05f : role == DominionSliceAnchorRole.Choir ? 1.85f : 1.70f;
                MoteMaker.MakeStaticMote(position, map, moteDef, scale);
            }

            FleckMaker.ThrowLightningGlow(position, map, 2.15f);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
        }

        public static void SpawnHeartExposedBurst(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            ThingDef moteDef = HeartExposeMoteDef;
            if (moteDef != null)
            {
                MoteMaker.MakeStaticMote(position, map, moteDef, 3.35f);
                MoteMaker.MakeStaticMote(position + new Vector3(0f, 0.004f, 0f), map, moteDef, 2.28f);
            }

            FleckMaker.ThrowLightningGlow(position, map, 3.65f);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", position.ToIntVec3(), map);
        }

        public static void SpawnHeartShieldBlockFlare(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            ThingDef moteDef = ShieldBlockMoteDef;
            if (moteDef != null)
            {
                MoteMaker.MakeStaticMote(position, map, moteDef, 1.35f);
            }
        }

        private static void DrawBeam(Vector3 from, Vector3 to, float width, float length, Material material, float scalePulse)
        {
            if (material == null)
            {
                return;
            }

            Vector3 delta = to - from;
            Vector3 center = (from + to) * 0.5f;
            center.y = from.y;
            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(width * scalePulse, 1f, length));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private static void DrawLinkEntryBloom(Vector3 heartPos, DominionSliceAnchorRole role, int seed, int ticks, float scale)
        {
            if (LinkEntryBloomMaterial == null)
            {
                return;
            }

            float roleOffset = GetRolePhaseOffset(role);
            float pulse = 1f + Mathf.Sin((ticks + seed) * 0.068f + roleOffset) * 0.07f;
            Vector3 loc = heartPos;
            loc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.018f;
            Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.AngleAxis((ticks + seed) * 0.030f + roleOffset * 17f, Vector3.up), new Vector3(scale * pulse, 1f, scale * pulse));
            Graphics.DrawMesh(MeshPool.plane10, matrix, LinkEntryBloomMaterial, 0);
        }

        private static float GetRoleWidth(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.108f;
                case DominionSliceAnchorRole.Law:
                    return 0.140f;
                default:
                    return 0.122f;
            }
        }

        private static float GetRolePhaseOffset(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 1.72f;
                case DominionSliceAnchorRole.Law:
                    return 3.18f;
                default:
                    return 0.35f;
            }
        }
    }
}
