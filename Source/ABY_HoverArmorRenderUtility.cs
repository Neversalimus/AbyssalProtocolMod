using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class ABY_HoverArmorRenderUtility
    {
        private const string RingTexPath = "Effects/ABY_HoverGravRing";
        private const string SparkTexPath = "Effects/ABY_HoverSpark";
        private const string ShadowTexPath = "Effects/ABY_HoverShadow";
        private const string HaloTexPath = "Effects/ABY_HoverHalo";
        private const string FlightRigTexPrefix = "Effects/FlightRig/ABY_FlightRig_";
        private const int FlightRigFrameCount = 8;
        private const int FlightRigDeployFrameCount = 3;
        private const int FlightRigIdleFirstFrame = 3;

        private static readonly Material RingMaterial = MaterialPool.MatFrom(RingTexPath, ShaderDatabase.MoteGlow, Color.white);
        private static readonly Material SparkMaterial = MaterialPool.MatFrom(SparkTexPath, ShaderDatabase.MoteGlow, Color.white);
        private static readonly Material ShadowMaterial = MaterialPool.MatFrom(ShadowTexPath, ShaderDatabase.Transparent, Color.white);
        private static readonly Material HaloMaterial = MaterialPool.MatFrom(HaloTexPath, ShaderDatabase.MoteGlow, Color.white);
        private static readonly Material[] FlightRigMaterials = BuildFlightRigMaterials(ShaderDatabase.Transparent);
        private static readonly Material[] FlightRigGlowMaterials = BuildFlightRigMaterials(ShaderDatabase.MoteGlow);

        private static readonly Dictionary<int, int> FlightRigStartTicksByPawn = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> FlightRigLastSeenTicksByPawn = new Dictionary<int, int>();
        private static int lastFlightRigCleanupTick = -1;

        public static void DrawBackFlightRigFx(Pawn pawn, Vector3 pawnDrawLoc, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null || !extension.enableFlightRigFx)
            {
                return;
            }

            int frame = ResolveFlightRigFrame(pawn, extension, out int ticks);
            Material rigMaterial = MaterialAt(FlightRigMaterials, frame);
            if (rigMaterial == null)
            {
                return;
            }

            float periodPhase = (ticks % 112) / 112f;
            float pulse = (float)Math.Sin(periodPhase * Math.PI * 2.0);
            float scale = Math.Max(0.35f, extension.flightRigScale + pulse * extension.flightRigPulseScale);
            float alpha = Mathf.Clamp01(extension.flightRigAlpha);

            Vector3 loc = pawnDrawLoc;
            loc.x += extension.flightRigOffsetX;
            loc.z += extension.flightRigOffsetZ;
            loc.y = AltitudeLayer.Pawn.AltitudeFor() + extension.flightRigAltitudeOffset;

            // Draw the solid rig first, behind the pawn. This changes the drafted silhouette without
            // covering the pawn's face/body once the vanilla pawn renderer draws over it.
            DrawPlane(loc, new Vector3(scale, 1f, scale), rigMaterial, Quaternion.identity, Alpha(alpha));

            Material glowMaterial = MaterialAt(FlightRigGlowMaterials, frame);
            if (glowMaterial != null && extension.flightRigGlowAlpha > 0.001f)
            {
                // Low-alpha glow pass preserves the energetic thrusters without turning the rig into a
                // full-screen glare. It uses the same frame, slightly larger and barely above the solid pass.
                DrawPlane(loc + new Vector3(0f, 0.004f, 0f), new Vector3(scale * 1.035f, 1f, scale * 1.035f), glowMaterial, Quaternion.identity, Alpha(extension.flightRigGlowAlpha));
            }
        }

        public static void DrawUnderfootFx(Pawn pawn, Vector3 groundDrawLoc, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null || !extension.enableUnderfootFx)
            {
                return;
            }

            int ticks = ABY_HoverArmorUtility.SafeTicksGame() + pawn.thingIDNumber * 11;
            float phase = (ticks % 96) / 96f;
            float pulse = (float)Math.Sin(phase * Math.PI * 2.0);
            float ringScale = Math.Max(0.15f, extension.ringScale + pulse * extension.ringPulseScale);

            Vector3 ground = groundDrawLoc;
            ground.y = AltitudeLayer.MoteLow.AltitudeFor() + 0.030f;

            DrawPlane(ground, new Vector3(extension.shadowScale, 1f, extension.shadowScale * 0.62f), ShadowMaterial, Quaternion.identity, Alpha(extension.ringAlpha * 0.32f));
            DrawPlane(ground + new Vector3(0f, 0.010f, 0f), new Vector3(ringScale, 1f, ringScale), RingMaterial, Quaternion.identity, Alpha(extension.ringAlpha * 0.34f));

            // Keep these subtle; the animated back rig is now the main drafted-flight readability cue.
            DrawIdleSparkSet(pawn, groundDrawLoc, extension, ticks, phase);
            if (pawn.pather != null && pawn.pather.MovingNow)
            {
                DrawTrailSparkSet(pawn, groundDrawLoc, extension, ticks, phase);
            }
        }

        public static void DrawHaloFx(Pawn pawn, Vector3 pawnDrawLoc, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null || !extension.enableHaloFx)
            {
                return;
            }

            int ticks = ABY_HoverArmorUtility.SafeTicksGame() + pawn.thingIDNumber * 19;
            int period = 132;
            float phase = (ticks % period) / (float)period;
            float pulse = (float)Math.Sin(phase * Math.PI * 2.0);
            float scale = Math.Max(0.45f, extension.haloScale + pulse * extension.haloPulseScale);
            float alpha = Mathf.Clamp01(extension.haloAlpha + pulse * 0.055f);

            Vector3 loc = pawnDrawLoc;
            loc.z += extension.haloOffsetZ;
            loc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + extension.haloAltitudeOffset;

            // Legacy stable upper grav-halo/backplate. No spin.
            DrawPlane(loc, new Vector3(scale, 1f, scale * 0.52f), HaloMaterial, Quaternion.identity, Alpha(alpha));
            DrawPlane(loc + new Vector3(0f, 0.006f, 0f), new Vector3(scale * 0.72f, 1f, scale * 0.34f), HaloMaterial, Quaternion.identity, Alpha(alpha * 0.42f));
        }

        private static int ResolveFlightRigFrame(Pawn pawn, ABY_HoverArmorExtension extension, out int ticks)
        {
            ticks = ABY_HoverArmorUtility.SafeTicksGame();
            int id = pawn != null ? pawn.thingIDNumber : 0;
            int frameTicks = Math.Max(3, extension?.flightRigFrameTicks ?? 8);

            if (!FlightRigLastSeenTicksByPawn.TryGetValue(id, out int lastSeen) || ticks - lastSeen > 30)
            {
                FlightRigStartTicksByPawn[id] = ticks;
            }

            FlightRigLastSeenTicksByPawn[id] = ticks;
            CleanupFlightRigStateIfNeeded(ticks);

            int startTick = FlightRigStartTicksByPawn.TryGetValue(id, out int value) ? value : ticks;
            int age = Math.Max(0, ticks - startTick);
            int deployTicks = FlightRigDeployFrameCount * frameTicks;
            if (age < deployTicks)
            {
                return Math.Min(FlightRigDeployFrameCount - 1, age / frameTicks);
            }

            int idleFrameCount = FlightRigFrameCount - FlightRigIdleFirstFrame;
            int idleFrame = ((age - deployTicks) / frameTicks) % Math.Max(1, idleFrameCount);
            return FlightRigIdleFirstFrame + idleFrame;
        }

        private static void CleanupFlightRigStateIfNeeded(int ticks)
        {
            if (lastFlightRigCleanupTick >= 0 && ticks - lastFlightRigCleanupTick < 360)
            {
                return;
            }

            lastFlightRigCleanupTick = ticks;
            List<int> staleIds = null;
            foreach (KeyValuePair<int, int> pair in FlightRigLastSeenTicksByPawn)
            {
                if (ticks - pair.Value > 720)
                {
                    if (staleIds == null)
                    {
                        staleIds = new List<int>();
                    }
                    staleIds.Add(pair.Key);
                }
            }

            if (staleIds == null)
            {
                return;
            }

            for (int i = 0; i < staleIds.Count; i++)
            {
                FlightRigLastSeenTicksByPawn.Remove(staleIds[i]);
                FlightRigStartTicksByPawn.Remove(staleIds[i]);
            }
        }

        private static MaterialPropertyBlock Alpha(float alpha)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            return block;
        }

        private static void DrawIdleSparkSet(Pawn pawn, Vector3 groundDrawLoc, ABY_HoverArmorExtension extension, int ticks, float phase)
        {
            Vector3 baseLoc = groundDrawLoc;
            baseLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.020f;

            for (int i = 0; i < 3; i++)
            {
                float angle = (phase * 360f + i * 120f + pawn.thingIDNumber * 7) * Mathf.Deg2Rad;
                float radius = 0.19f + 0.025f * (float)Math.Sin((phase + i * 0.17f) * Math.PI * 2.0);
                Vector3 offset = new Vector3((float)Math.Cos(angle) * radius, 0f, (float)Math.Sin(angle) * radius * 0.62f);
                float scale = Math.Max(0.030f, extension.sparkScale * 0.46f);
                DrawPlane(baseLoc + offset + new Vector3(0f, i * 0.002f, 0f), new Vector3(scale, 1f, scale), SparkMaterial, Quaternion.identity, Alpha(extension.ringAlpha * 0.38f));
            }
        }

        private static void DrawTrailSparkSet(Pawn pawn, Vector3 groundDrawLoc, ABY_HoverArmorExtension extension, int ticks, float phase)
        {
            Vector3 trailDir = ResolveTrailDirection(pawn);
            Vector3 right = new Vector3(trailDir.z, 0f, -trailDir.x);
            Vector3 baseLoc = groundDrawLoc - trailDir * 0.34f;
            baseLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.030f;

            for (int i = 0; i < 4; i++)
            {
                float side = (i - 1.5f) * 0.095f;
                float back = 0.075f * i;
                float wobble = (float)Math.Sin((phase + i * 0.31f) * Math.PI * 2.0) * 0.040f;
                Vector3 loc = baseLoc - trailDir * back + right * (side + wobble);
                float scale = Math.Max(0.030f, extension.sparkScale * (0.74f - i * 0.10f));
                DrawPlane(loc + new Vector3(0f, i * 0.002f, 0f), new Vector3(scale, 1f, scale), SparkMaterial, Quaternion.identity, Alpha(extension.ringAlpha * 0.58f));
            }
        }

        private static Vector3 ResolveTrailDirection(Pawn pawn)
        {
            if (pawn?.pather != null && pawn.pather.MovingNow)
            {
                IntVec3 next = pawn.pather.nextCell;
                IntVec3 cur = pawn.Position;
                Vector3 dir = new Vector3(next.x - cur.x, 0f, next.z - cur.z);
                if (dir.sqrMagnitude > 0.001f)
                {
                    return dir.normalized;
                }
            }

            if (pawn != null)
            {
                if (pawn.Rotation == Rot4.North) return new Vector3(0f, 0f, 1f);
                if (pawn.Rotation == Rot4.South) return new Vector3(0f, 0f, -1f);
                if (pawn.Rotation == Rot4.East) return new Vector3(1f, 0f, 0f);
                if (pawn.Rotation == Rot4.West) return new Vector3(-1f, 0f, 0f);
            }

            return new Vector3(0f, 0f, -1f);
        }

        private static Material[] BuildFlightRigMaterials(Shader shader)
        {
            Material[] result = new Material[FlightRigFrameCount];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = MaterialPool.MatFrom(FlightRigTexPrefix + i.ToString("00"), shader, Color.white);
            }
            return result;
        }

        private static Material MaterialAt(Material[] materials, int index)
        {
            if (materials == null || materials.Length == 0)
            {
                return null;
            }

            if (index < 0)
            {
                index = 0;
            }
            else if (index >= materials.Length)
            {
                index = materials.Length - 1;
            }

            return materials[index];
        }

        private static void DrawPlane(Vector3 loc, Vector3 scale, Material material, Quaternion rotation, MaterialPropertyBlock propertyBlock)
        {
            if (material == null)
            {
                return;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(loc, rotation, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0, null, 0, propertyBlock);
        }
    }
}
