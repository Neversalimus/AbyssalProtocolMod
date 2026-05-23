using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Centralized material helper for Abyssal draw/VFX paths.
    ///
    /// RimWorld's MaterialPool already caches materials, but many Abyssal VFX draw paths pass pulse-driven
    /// alpha/color values every frame. Quantizing those colors prevents unbounded material-key churn while
    /// preserving smooth enough in-game fades for small overlays and projectile effects.
    /// </summary>
    public static class ABY_MaterialCacheUtility
    {
        private const int ColorLevels = 31;
        private const int CleanupIntervalTicks = 2500;
        private const int MaxCachedMaterials = 4096;

        private static readonly Dictionary<MaterialKey, Material> Cache = new Dictionary<MaterialKey, Material>(512);
        private static int lastCleanupTick = -1;

        public static Material MatFrom(string texPath, Shader shader)
        {
            return MatFrom(texPath, shader, Color.white);
        }

        public static Material MatFrom(string texPath, Shader shader, Color color)
        {
            if (texPath.NullOrEmpty())
            {
                return BaseContent.BadMat;
            }

            Shader resolvedShader = shader ?? ShaderDatabase.Cutout;
            Color quantized = QuantizeColor(color);
            MaterialKey key = new MaterialKey(texPath, resolvedShader, quantized);
            if (Cache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            MaybeCleanup();
            Material material = MaterialPool.MatFrom(texPath, resolvedShader, quantized);
            Cache[key] = material;
            return material;
        }

        public static void Clear()
        {
            Cache.Clear();
            lastCleanupTick = TryGetTicksGame(out int ticksGame) ? ticksGame : -1;
        }

        private static Color QuantizeColor(Color color)
        {
            return new Color(
                Quantize01(color.r),
                Quantize01(color.g),
                Quantize01(color.b),
                Quantize01(color.a));
        }

        private static float Quantize01(float value)
        {
            float clamped = Mathf.Clamp01(value);
            return Mathf.Round(clamped * ColorLevels) / ColorLevels;
        }

        private static void MaybeCleanup()
        {
            int ticksGame = TryGetTicksGame(out int resolvedTicksGame) ? resolvedTicksGame : 0;
            if (lastCleanupTick >= 0 && ticksGame >= lastCleanupTick && ticksGame - lastCleanupTick < CleanupIntervalTicks && Cache.Count < MaxCachedMaterials)
            {
                return;
            }

            lastCleanupTick = ticksGame;
            if (Cache.Count <= MaxCachedMaterials)
            {
                return;
            }

            // MaterialPool owns the actual material instances; this cache only removes local lookup keys.
            Cache.Clear();
        }

        private static bool TryGetTicksGame(out int ticksGame)
        {
            ticksGame = 0;
            try
            {
                if (Current.Game == null || Current.Game.tickManager == null)
                {
                    return false;
                }

                ticksGame = Current.Game.tickManager.TicksGame;
                return true;
            }
            catch
            {
                ticksGame = 0;
                return false;
            }
        }

        private struct MaterialKey : IEquatable<MaterialKey>
        {
            private readonly string texPath;
            private readonly int shaderId;
            private readonly int r;
            private readonly int g;
            private readonly int b;
            private readonly int a;

            public MaterialKey(string texPath, Shader shader, Color color)
            {
                this.texPath = texPath ?? string.Empty;
                shaderId = shader != null ? shader.GetInstanceID() : 0;
                r = ToByte(color.r);
                g = ToByte(color.g);
                b = ToByte(color.b);
                a = ToByte(color.a);
            }

            public bool Equals(MaterialKey other)
            {
                return shaderId == other.shaderId
                    && r == other.r
                    && g == other.g
                    && b == other.b
                    && a == other.a
                    && string.Equals(texPath, other.texPath, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is MaterialKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + texPath.GetHashCode();
                    hash = hash * 31 + shaderId;
                    hash = hash * 31 + r;
                    hash = hash * 31 + g;
                    hash = hash * 31 + b;
                    hash = hash * 31 + a;
                    return hash;
                }
            }

            private static int ToByte(float value)
            {
                return Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
            }
        }
    }
}
