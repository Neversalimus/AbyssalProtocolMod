using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class DominionSliceVfxUtility
    {
        private static readonly HashSet<int> DrawnAnchorLinkSeeds = new HashSet<int>();
        private static int drawnAnchorLinkFrame = -1;

        private const string LinkBeamTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_LinkBeam";
        private const string LinkCoreTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_LinkCore";
        private const string LinkEntryBloomTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_LinkEntryBloom";
        private const string HeartShieldTexPath = "Things/VFX/DominionSlice/ABY_DominionSlice_HeartShield";

        private const string TetherGlowTexPath = "Things/VFX/DominionSlice/Tether/ABY_DominionTether_Glow";
        private const string TetherCoreTexPath = "Things/VFX/DominionSlice/Tether/ABY_DominionTether_Core";
        private const string TetherChainSparseTexPath = "Things/VFX/DominionSlice/Tether/ABY_DominionTether_ChainSparse";
        private const string TetherChainHeavyTexPath = "Things/VFX/DominionSlice/Tether/ABY_DominionTether_ChainHeavy";
        private const string TetherSnapAnchorTexPath = "Things/VFX/DominionSlice/Tether/ABY_DominionTether_SnapAnchor";
        private const string TetherSnapHeartTexPath = "Things/VFX/DominionSlice/Tether/ABY_DominionTether_SnapHeart";

        private const string AnchorBreakMoteDefName = "ABY_Mote_DominionSliceAnchorBreak";
        private const string HeartExposeMoteDefName = "ABY_Mote_DominionSliceHeartExpose";
        private const string ShieldBlockMoteDefName = "ABY_Mote_DominionSliceShieldBlock";
        private const int LinkSeverBurstDurationTicks = 62;

        private static readonly Material LinkBeamMaterial = MaterialPool.MatFrom(LinkBeamTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material LinkCoreMaterial = MaterialPool.MatFrom(LinkCoreTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material LinkEntryBloomMaterial = MaterialPool.MatFrom(LinkEntryBloomTexPath, ShaderDatabase.MoteGlow);
        private static readonly Material HeartShieldMaterial = MaterialPool.MatFrom(HeartShieldTexPath, ShaderDatabase.MoteGlow);

        private static readonly Material TetherGlowMaterial = MaterialPool.MatFrom(TetherGlowTexPath, ShaderDatabase.MoteGlow, new Color(1f, 1f, 1f, 0.62f));
        private static readonly Material TetherCoreMaterial = MaterialPool.MatFrom(TetherCoreTexPath, ShaderDatabase.MoteGlow, new Color(1f, 1f, 1f, 0.92f));
        private static readonly Material TetherChainSparseMaterial = MaterialPool.MatFrom(TetherChainSparseTexPath, ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, 0.88f));
        private static readonly Material TetherChainHeavyMaterial = MaterialPool.MatFrom(TetherChainHeavyTexPath, ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, 0.34f));
        private static readonly Material TetherSnapAnchorMaterial = MaterialPool.MatFrom(TetherSnapAnchorTexPath, ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, 0.94f));
        private static readonly Material TetherSnapHeartMaterial = MaterialPool.MatFrom(TetherSnapHeartTexPath, ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, 0.94f));

        private static ThingDef anchorBreakMoteDef;
        private static ThingDef heartExposeMoteDef;
        private static ThingDef shieldBlockMoteDef;

        public static int SeverBurstDurationTicks
        {
            get { return LinkSeverBurstDurationTicks; }
        }

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

            if (!TryMarkAnchorLinkDrawn(seed))
            {
                return;
            }

            float tetherAltitude = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.086f;
            anchorPos.y = tetherAltitude;
            heartPos.y = tetherAltitude;

            Vector3 flatDelta = heartPos - anchorPos;
            flatDelta.y = 0f;
            float length = flatDelta.magnitude;
            if (length <= 0.80f)
            {
                return;
            }

            Vector3 direction = flatDelta / length;
            Vector3 start = anchorPos + direction * Mathf.Min(0.65f, length * 0.14f);
            Vector3 end = heartPos - direction * Mathf.Min(1.05f, length * 0.20f);
            start.y = anchorPos.y;
            end.y = heartPos.y;

            float renderLength = (end - start).MagnitudeHorizontal();
            if (renderLength <= 0.25f)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float roleOffset = GetRolePhaseOffset(role);
            float pulse = 1f + Mathf.Sin((ticks + seed) * 0.030f + roleOffset) * 0.032f;
            float breath = 1f + Mathf.Sin((ticks + seed) * 0.014f + roleOffset) * 0.030f;
            float surge = 1f + Mathf.Sin((ticks + seed) * 0.052f + roleOffset) * 0.025f;
            float width = GetRoleWidth(role) * pulse;

            // Persistent layers stay continuous; movement is carried by flowing energy packets.
            // Drawing is de-duplicated per Unity frame, so the same link may be requested from the
            // map component, heart, or anchor without double-brightening when both endpoints are visible.
            DrawBeam(start, end, width * 8.10f, renderLength, TetherGlowMaterial, breath);
            DrawBeam(start, end, width * 1.62f, renderLength, TetherCoreMaterial, 1f + (surge - 1f) * 0.75f);
            DrawBeam(start, end, width * 2.36f, renderLength, TetherChainSparseMaterial, 1f + (pulse - 1f) * 0.35f);
            DrawBeam(start, end, width * 1.62f, renderLength, TetherChainHeavyMaterial, 1f + (breath - 1f) * 0.50f);
            DrawBeam(start, end, width * 0.92f, renderLength, TetherCoreMaterial, 1f + (pulse - 1f) * 0.55f);
            DrawFlowPackets(start, end, width, renderLength, seed, ticks, roleOffset);
        }

        public static void DrawAnchorLinkSeverBurst(Vector3 anchorPos, Vector3 heartPos, Map map, DominionSliceAnchorRole role, int seed, int ageTicks)
        {
            if (map == null || ageTicks < 0 || ageTicks > LinkSeverBurstDurationTicks)
            {
                return;
            }

            float tetherAltitude = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.092f;
            anchorPos.y = tetherAltitude;
            heartPos.y = tetherAltitude;

            Vector3 flatDelta = heartPos - anchorPos;
            flatDelta.y = 0f;
            float length = flatDelta.magnitude;
            if (length <= 0.80f)
            {
                return;
            }

            Vector3 direction = flatDelta / length;
            float progress = Mathf.Clamp01(ageTicks / (float)LinkSeverBurstDurationTicks);
            float segment = Mathf.Clamp(length * (0.27f + progress * 0.04f), 2.60f, 6.40f);
            float baseWidth = GetRoleWidth(role);
            float burstWidth = baseWidth * Mathf.Lerp(7.25f, 4.75f, progress);
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float jitter = 1f + Mathf.Sin((ticks + seed) * 0.115f + GetRolePhaseOffset(role)) * 0.035f;

            Vector3 anchorFrom = anchorPos + direction * 0.15f;
            Vector3 anchorTo = anchorPos + direction * segment;
            Vector3 heartFrom = heartPos - direction * segment;
            Vector3 heartTo = heartPos - direction * 0.20f;
            anchorFrom.y = anchorTo.y = heartFrom.y = heartTo.y = anchorPos.y;

            DrawBeam(anchorFrom, anchorTo, burstWidth * jitter, (anchorTo - anchorFrom).MagnitudeHorizontal(), TetherSnapAnchorMaterial, 1f);
            DrawBeam(heartFrom, heartTo, burstWidth * (0.92f + (1f - progress) * 0.06f), (heartTo - heartFrom).MagnitudeHorizontal(), TetherSnapHeartMaterial, 1f);
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

        public static void SpawnAnchorLinkSever(Vector3 anchorPos, Vector3 heartPos, Map map, DominionSliceAnchorRole role)
        {
            if (map == null)
            {
                return;
            }

            SpawnAnchorBreakFlare(anchorPos, map, role);

            Vector3 flatDelta = heartPos - anchorPos;
            flatDelta.y = 0f;
            float distance = flatDelta.magnitude;
            if (distance <= 0.10f)
            {
                FleckMaker.ThrowLightningGlow(heartPos, map, 1.45f);
                FleckMaker.ThrowMicroSparks(heartPos, map);
                return;
            }

            Vector3 direction = flatDelta / distance;
            Vector3 midpoint = anchorPos + direction * (distance * 0.45f);
            midpoint.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.006f;

            Vector3 heartSocket = heartPos - direction * Mathf.Clamp(distance * 0.16f, 1.05f, 2.15f);
            heartSocket.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.006f;

            ThingDef moteDef = AnchorBreakMoteDef;
            if (moteDef != null)
            {
                MoteMaker.MakeStaticMote(midpoint, map, moteDef, role == DominionSliceAnchorRole.Law ? 1.28f : 1.10f);
            }

            FleckMaker.ThrowLightningGlow(midpoint, map, 1.28f);
            FleckMaker.ThrowLightningGlow(heartSocket, map, 1.44f);
            FleckMaker.ThrowMicroSparks(midpoint, map);
            FleckMaker.ThrowMicroSparks(heartSocket, map);
            FleckMaker.ThrowMicroSparks(heartSocket + new Vector3(direction.z, 0f, -direction.x) * 0.18f, map);
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

        private static bool TryMarkAnchorLinkDrawn(int seed)
        {
            int frame = Time.frameCount;
            if (frame != drawnAnchorLinkFrame)
            {
                drawnAnchorLinkFrame = frame;
                DrawnAnchorLinkSeeds.Clear();
            }

            return DrawnAnchorLinkSeeds.Add(seed);
        }

        private static void DrawFlowPackets(Vector3 start, Vector3 end, float baseWidth, float renderLength, int seed, int ticks, float roleOffset)
        {
            Vector3 delta = end - start;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length <= 1.20f)
            {
                return;
            }

            Vector3 direction = delta / length;
            float packetLength = Mathf.Clamp(renderLength * 0.115f, 1.45f, 3.35f);
            float speed = 0.0105f;
            for (int i = 0; i < 3; i++)
            {
                float t = Mathf.Repeat(ticks * speed + seed * 0.0073f + roleOffset * 0.071f + i * 0.333f, 1f);
                float fade = Mathf.Sin(t * Mathf.PI);
                if (fade <= 0.05f)
                {
                    continue;
                }

                Vector3 center = Vector3.Lerp(start, end, t);
                Vector3 from = center - direction * (packetLength * 0.50f);
                Vector3 to = center + direction * (packetLength * 0.50f);
                from.y = start.y + 0.004f;
                to.y = start.y + 0.004f;
                DrawBeam(from, to, baseWidth * Mathf.Lerp(1.55f, 2.25f, fade), (to - from).MagnitudeHorizontal(), TetherCoreMaterial, 1f);
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
            // Disabled: old link-entry bloom drew another circular halo on the heart platform.
        }

        private static float GetRoleWidth(DominionSliceAnchorRole role)
        {
            switch (role)
            {
                case DominionSliceAnchorRole.Choir:
                    return 0.178f;
                case DominionSliceAnchorRole.Law:
                    return 0.214f;
                default:
                    return 0.196f;
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
