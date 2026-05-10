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
        private const string HaloFallbackTexPath = "Effects/ABY_HoverHalo";
        private const string HaloFrameTexPrefix = "Effects/HoverHalo/ABY_HoverHalo_";
        private const int HaloFrameCount = 8;
        private const int HaloAppearFrameCount = 3;
        private const int HaloIdleStartFrame = 3;

        private static readonly Material RingMaterial = MaterialPool.MatFrom(RingTexPath, ShaderDatabase.MoteGlow, Color.white);
        private static readonly Material SparkMaterial = MaterialPool.MatFrom(SparkTexPath, ShaderDatabase.MoteGlow, Color.white);
        private static readonly Material ShadowMaterial = MaterialPool.MatFrom(ShadowTexPath, ShaderDatabase.Transparent, Color.white);
        private static readonly Material HaloFallbackMaterial = MaterialPool.MatFrom(HaloFallbackTexPath, ShaderDatabase.MoteGlow, Color.white);
        private static readonly Material[] HaloFrameMaterials = BuildHaloFrameMaterials();

        private static readonly Dictionary<int, int> HoverStartTicksByPawnId = new Dictionary<int, int>();
        private static int lastCleanupTick = -1;

        public static void NotifyHoverInactive(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            HoverStartTicksByPawnId.Remove(pawn.thingIDNumber);
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
            DrawPlane(ground + new Vector3(0f, 0.010f, 0f), new Vector3(ringScale, 1f, ringScale), RingMaterial, Quaternion.identity, Alpha(extension.ringAlpha * 0.32f));

            // Ground sparks are intentionally subtle now; animated halo is the readable drafted cue.
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

            int ticks = ABY_HoverArmorUtility.SafeTicksGame();
            CleanupOccasionally(ticks);

            int activeTicks = ActiveTicksFor(pawn, ticks);
            int frameIndex = ResolveHaloFrameIndex(activeTicks, ticks, extension);
            Material material = ResolveHaloMaterial(frameIndex);

            int visualTicks = ticks + pawn.thingIDNumber * 19;
            float pulse = (float)Math.Sin((visualTicks % 132) / 132f * Math.PI * 2.0);
            float scale = Math.Max(0.18f, extension.haloScale + pulse * extension.haloPulseScale);
            float appearAlpha = Mathf.Clamp01(activeTicks / Math.Max(1f, extension.haloAppearTicks * 0.70f));
            float alpha = Mathf.Clamp01((extension.haloAlpha + pulse * 0.030f) * appearAlpha);

            Vector3 loc = pawnDrawLoc;
            loc.z += extension.haloOffsetZ;

            // This is deliberately drawn as a backplate before the pawn and slightly below pawn altitude.
            // It should frame the head/shoulders, not sit on the face like a second helmet.
            loc.y = pawnDrawLoc.y + extension.haloAltitudeOffset;

            DrawPlane(loc, new Vector3(scale, 1f, scale), material, Quaternion.identity, Alpha(alpha));
        }

        private static Material[] BuildHaloFrameMaterials()
        {
            Material[] result = new Material[HaloFrameCount];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = MaterialPool.MatFrom(HaloFrameTexPrefix + i.ToString("00"), ShaderDatabase.MoteGlow, Color.white);
            }

            return result;
        }

        private static int ActiveTicksFor(Pawn pawn, int ticks)
        {
            int key = pawn.thingIDNumber;
            if (!HoverStartTicksByPawnId.TryGetValue(key, out int startTick))
            {
                startTick = ticks;
                HoverStartTicksByPawnId[key] = startTick;
            }

            return Math.Max(0, ticks - startTick);
        }

        private static int ResolveHaloFrameIndex(int activeTicks, int ticks, ABY_HoverArmorExtension extension)
        {
            int appearTicks = Math.Max(1, extension.haloAppearTicks);
            if (activeTicks < appearTicks)
            {
                float t = Mathf.Clamp01(activeTicks / (float)appearTicks);
                return Mathf.Clamp(Mathf.FloorToInt(t * HaloAppearFrameCount), 0, HaloAppearFrameCount - 1);
            }

            int loopTicks = Math.Max(2, extension.haloLoopFrameTicks);
            int idleFrameCount = HaloFrameCount - HaloIdleStartFrame;
            int loopFrame = (ticks / loopTicks) % idleFrameCount;
            return HaloIdleStartFrame + loopFrame;
        }

        private static Material ResolveHaloMaterial(int frameIndex)
        {
            if (frameIndex >= 0 && frameIndex < HaloFrameMaterials.Length && HaloFrameMaterials[frameIndex] != null)
            {
                return HaloFrameMaterials[frameIndex];
            }

            return HaloFallbackMaterial;
        }

        private static void CleanupOccasionally(int ticks)
        {
            if (lastCleanupTick >= 0 && ticks - lastCleanupTick < 900)
            {
                return;
            }

            lastCleanupTick = ticks;
            if (HoverStartTicksByPawnId.Count > 256)
            {
                HoverStartTicksByPawnId.Clear();
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

            for (int i = 0; i < 2; i++)
            {
                float angle = (phase * 360f + i * 180f + pawn.thingIDNumber * 7) * Mathf.Deg2Rad;
                float radius = 0.16f + 0.020f * (float)Math.Sin((phase + i * 0.17f) * Math.PI * 2.0);
                Vector3 offset = new Vector3((float)Math.Cos(angle) * radius, 0f, (float)Math.Sin(angle) * radius * 0.62f);
                float scale = Math.Max(0.026f, extension.sparkScale * 0.45f);
                DrawPlane(baseLoc + offset + new Vector3(0f, i * 0.002f, 0f), new Vector3(scale, 1f, scale), SparkMaterial, Quaternion.identity, Alpha(extension.ringAlpha * 0.38f));
            }
        }

        private static void DrawTrailSparkSet(Pawn pawn, Vector3 groundDrawLoc, ABY_HoverArmorExtension extension, int ticks, float phase)
        {
            Vector3 trailDir = ResolveTrailDirection(pawn);
            Vector3 right = new Vector3(trailDir.z, 0f, -trailDir.x);
            Vector3 baseLoc = groundDrawLoc - trailDir * 0.34f;
            baseLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.030f;

            for (int i = 0; i < 3; i++)
            {
                float side = (i - 1f) * 0.085f;
                float back = 0.075f * i;
                float wobble = (float)Math.Sin((phase + i * 0.31f) * Math.PI * 2.0) * 0.034f;
                Vector3 loc = baseLoc - trailDir * back + right * (side + wobble);
                float scale = Math.Max(0.028f, extension.sparkScale * (0.75f - i * 0.12f));
                DrawPlane(loc + new Vector3(0f, i * 0.002f, 0f), new Vector3(scale, 1f, scale), SparkMaterial, Quaternion.identity, Alpha(extension.ringAlpha * 0.52f));
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
