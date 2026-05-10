using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_HoverArmorRenderUtility
    {
        public const bool PawnRendererOwnsHoverFx = true;
        private const int MaxSparks = 128;
        private const float RingYOffset = 0.020f;
        private static readonly Dictionary<int, int> NextSparkTickByPawnId = new Dictionary<int, int>();
        private static readonly List<HoverSpark> Sparks = new List<HoverSpark>();

        public static void DrawForPawn(Pawn pawn, Vector3 baseDrawLoc, ABY_HoverArmorExtension extension)
        {
            if (pawn == null || extension == null || !ABY_HoverArmorUtility.IsWorldPawnDraw(pawn, baseDrawLoc))
            {
                return;
            }

            int ticks = ABY_HoverArmorUtility.SafeTicksGame();
            CleanupSparks(ticks);

            if (extension.enableUnderfootFx)
            {
                DrawHoverRing(pawn, baseDrawLoc, extension, ticks);
            }

            if (extension.enableMovingSparks && IsMoving(pawn))
            {
                TryAddSpark(pawn, baseDrawLoc, extension, ticks);
            }

            DrawSparks(ticks);
        }

        private static void DrawHoverRing(Pawn pawn, Vector3 baseDrawLoc, ABY_HoverArmorExtension extension, int ticks)
        {
            string texPath = string.IsNullOrEmpty(extension.ringTexPath) ? "Effects/ABY_HoverGravRing" : extension.ringTexPath;

            float seed = Mathf.Abs((pawn.thingIDNumber * 37) % 997);
            float pulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed) * 0.105f);
            float movementBonus = IsMoving(pawn) ? Mathf.Max(0f, extension.movingRingScaleBonus) : 0f;
            float scale = Mathf.Max(0.18f, extension.ringScale + movementBonus + (pulse - 0.5f) * Mathf.Max(0f, extension.pulseAmplitude));
            float alpha = Mathf.Clamp01(Mathf.Max(0.28f, extension.ringAlpha) * (0.72f + pulse * 0.42f));

            Vector3 drawPos = baseDrawLoc;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + RingYOffset;
            DrawPlane(texPath, drawPos, scale, alpha, 0f);
        }

        private static void TryAddSpark(Pawn pawn, Vector3 baseDrawLoc, ABY_HoverArmorExtension extension, int ticks)
        {
            if (Sparks.Count >= MaxSparks)
            {
                return;
            }

            int pawnId = pawn.thingIDNumber;
            if (NextSparkTickByPawnId.TryGetValue(pawnId, out int nextTick) && ticks < nextTick)
            {
                return;
            }

            int interval = Mathf.Max(5, extension.sparkIntervalTicks);
            NextSparkTickByPawnId[pawnId] = ticks + interval;

            string texPath = string.IsNullOrEmpty(extension.sparkTexPath) ? "Effects/ABY_HoverSpark" : extension.sparkTexPath;
            Vector3 offset = BackTrailOffset(pawn.Rotation);
            Vector3 jitter = new Vector3(Rand.Range(-0.07f, 0.07f), 0f, Rand.Range(-0.05f, 0.05f));
            Vector3 pos = baseDrawLoc + offset + jitter;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + RingYOffset + 0.006f;

            Sparks.Add(new HoverSpark
            {
                TexPath = texPath,
                Position = pos,
                SpawnTick = ticks,
                LifetimeTicks = Mathf.Max(6, extension.sparkLifetimeTicks),
                Scale = Mathf.Max(0.05f, extension.sparkScale * Rand.Range(0.85f, 1.25f)),
                Alpha = Mathf.Clamp01(Mathf.Max(0.35f, extension.sparkAlpha)),
                Angle = Rand.Range(0f, 360f)
            });
        }

        private static void DrawSparks(int ticks)
        {
            for (int i = Sparks.Count - 1; i >= 0; i--)
            {
                HoverSpark spark = Sparks[i];
                float age = ticks - spark.SpawnTick;
                if (age < 0f || age > spark.LifetimeTicks)
                {
                    continue;
                }

                float progress = Mathf.Clamp01(age / Mathf.Max(1f, spark.LifetimeTicks));
                float alpha = spark.Alpha * (1f - progress);
                float scale = spark.Scale * (1f + progress * 0.45f);
                Vector3 pos = spark.Position;
                pos.y += progress * 0.012f;
                DrawPlane(spark.TexPath, pos, scale, alpha, spark.Angle + progress * 25f);
            }
        }

        private static void CleanupSparks(int ticks)
        {
            for (int i = Sparks.Count - 1; i >= 0; i--)
            {
                if (ticks - Sparks[i].SpawnTick > Sparks[i].LifetimeTicks)
                {
                    Sparks.RemoveAt(i);
                }
            }

            if (ticks % 250 != 0 || NextSparkTickByPawnId.Count == 0)
            {
                return;
            }

            List<int> remove = null;
            foreach (KeyValuePair<int, int> kvp in NextSparkTickByPawnId)
            {
                if (ticks - kvp.Value > 900)
                {
                    if (remove == null)
                    {
                        remove = new List<int>();
                    }
                    remove.Add(kvp.Key);
                }
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                NextSparkTickByPawnId.Remove(remove[i]);
            }
        }

        private static bool IsMoving(Pawn pawn)
        {
            return pawn != null && pawn.pather != null && pawn.pather.Moving;
        }

        private static Vector3 BackTrailOffset(Rot4 rot)
        {
            if (rot == Rot4.North)
            {
                return new Vector3(0f, 0f, -0.25f);
            }

            if (rot == Rot4.South)
            {
                return new Vector3(0f, 0f, 0.25f);
            }

            if (rot == Rot4.East)
            {
                return new Vector3(-0.25f, 0f, 0f);
            }

            if (rot == Rot4.West)
            {
                return new Vector3(0.25f, 0f, 0f);
            }

            return Vector3.zero;
        }

        private static void DrawPlane(string texPath, Vector3 loc, float scale, float alpha, float angle)
        {
            if (string.IsNullOrEmpty(texPath) || alpha <= 0.01f || scale <= 0.01f)
            {
                return;
            }

            try
            {
                Color color = new Color(1f, 1f, 1f, QuantizeAlpha(alpha));
                Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.Transparent, color);
                Matrix4x4 matrix = Matrix4x4.identity;
                matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(scale, 1f, scale));
                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("hoverArmorRendererFx:" + texPath, "[Abyssal Protocol] Failed to draw hover armor renderer FX texture '" + texPath + "': " + ex.Message, 600);
            }
        }

        private static float QuantizeAlpha(float alpha)
        {
            return Mathf.Round(Mathf.Clamp01(alpha) * 16f) / 16f;
        }

        private struct HoverSpark
        {
            public string TexPath;
            public Vector3 Position;
            public int SpawnTick;
            public int LifetimeTicks;
            public float Scale;
            public float Alpha;
            public float Angle;
        }
    }
}
