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
            // Disabled by the Dominion Sepulcher redesign: the shield state should be communicated
            // mechanically and through small impact feedback, not by a large magic circle around the heart.
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

            FleckMaker.ThrowLightningGlow(position, map, 1.15f);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position + new Vector3(0.16f, 0f, -0.12f), map);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", position.ToIntVec3(), map);
        }

        public static void SpawnHeartShieldBlockFlare(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, 0.62f);
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
            // Disabled: old link-entry bloom drew another circular halo on the heart platform.
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
