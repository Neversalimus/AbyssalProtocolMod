using System;
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

        private static readonly Material RingMaterial = MaterialPool.MatFrom(RingTexPath, ShaderDatabase.MoteGlow, Color.white);
        private static readonly Material SparkMaterial = MaterialPool.MatFrom(SparkTexPath, ShaderDatabase.MoteGlow, Color.white);
        private static readonly Material ShadowMaterial = MaterialPool.MatFrom(ShadowTexPath, ShaderDatabase.Transparent, Color.white);

        public static void DrawUnderfootFx(Pawn pawn, Vector3 originalDrawLoc, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null || !extension.enableUnderfootFx)
            {
                return;
            }

            int ticks = ABY_HoverArmorUtility.SafeTicksGame() + pawn.thingIDNumber * 11;
            float phase = (ticks % 96) / 96f;
            float pulse = (float)Math.Sin(phase * Math.PI * 2.0);
            float ringScale = Math.Max(0.15f, extension.ringScale + pulse * extension.ringPulseScale);

            Vector3 ground = originalDrawLoc;
            ground.y = AltitudeLayer.MoteLow.AltitudeFor() + 0.018f;

            DrawPlane(ground, new Vector3(extension.shadowScale, 1f, extension.shadowScale * 0.62f), ShadowMaterial, Quaternion.identity);
            DrawPlane(ground + new Vector3(0f, 0.006f, 0f), new Vector3(ringScale, 1f, ringScale), RingMaterial, Quaternion.AngleAxis(phase * 360f, Vector3.up));

            if (pawn.pather != null && pawn.pather.MovingNow)
            {
                DrawSparkSet(pawn, originalDrawLoc, extension, ticks, phase);
            }
        }

        private static void DrawSparkSet(Pawn pawn, Vector3 originalDrawLoc, ABY_HoverArmorExtension extension, int ticks, float phase)
        {
            Vector3 trailDir = ResolveTrailDirection(pawn);
            Vector3 right = new Vector3(trailDir.z, 0f, -trailDir.x);
            Vector3 baseLoc = originalDrawLoc - trailDir * 0.28f;
            baseLoc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.012f;

            for (int i = 0; i < 3; i++)
            {
                float side = (i - 1) * 0.115f;
                float back = 0.06f * i;
                float wobble = (float)Math.Sin((phase + i * 0.31f) * Math.PI * 2.0) * 0.035f;
                Vector3 loc = baseLoc - trailDir * back + right * (side + wobble);
                float scale = Math.Max(0.025f, extension.sparkScale * (1f - i * 0.16f));
                Quaternion rot = Quaternion.AngleAxis((ticks * 13 + i * 67) % 360, Vector3.up);
                DrawPlane(loc + new Vector3(0f, i * 0.002f, 0f), new Vector3(scale, 1f, scale), SparkMaterial, rot);
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

        private static void DrawPlane(Vector3 loc, Vector3 scale, Material material, Quaternion rotation)
        {
            if (material == null)
            {
                return;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(loc, rotation, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
