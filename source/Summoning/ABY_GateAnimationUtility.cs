using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_GateAnimationUtility
    {
        public static void DrawAnimatedPortal(string[] framePaths, string[] glowPaths, Vector3 drawLoc, float baseScale, int ticks, int seed, int frameDuration = 6, float pulseMagnitude = 0.04f, float glowMinAlpha = 0.24f, float glowMaxAlpha = 0.78f, float yOffset = 0.034f)
        {
            if (framePaths == null || framePaths.Length == 0)
            {
                return;
            }

            int duration = Mathf.Max(1, frameDuration);
            float anim = (ticks + Mathf.Abs(seed % 997)) / (float)duration;
            int current = PositiveModulo(Mathf.FloorToInt(anim), framePaths.Length);
            int next = PositiveModulo(current + 1, framePaths.Length);
            float blend = anim - Mathf.Floor(anim);
            float pulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed) * 0.070f);
            float scale = baseScale * (1f + (pulse - 0.5f) * pulseMagnitude);

            Vector3 loc = drawLoc;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + yOffset;

            DrawPlane(framePaths[current], loc, scale, Mathf.Clamp01(1f - blend * 0.85f));
            if (blend > 0.001f)
            {
                DrawPlane(framePaths[next], loc + new Vector3(0f, 0.0015f, 0f), scale * 1.001f, Mathf.Clamp01(blend * 0.92f));
            }

            if (glowPaths == null || glowPaths.Length == 0)
            {
                return;
            }

            int glowCurrent = PositiveModulo(current, glowPaths.Length);
            int glowNext = PositiveModulo(next, glowPaths.Length);
            float glowAlpha = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, pulse);
            float glowScale = scale * 1.03f;
            DrawPlane(glowPaths[glowCurrent], loc + new Vector3(0f, 0.003f, 0f), glowScale, glowAlpha * Mathf.Clamp01(1f - blend * 0.75f));
            if (blend > 0.001f)
            {
                DrawPlane(glowPaths[glowNext], loc + new Vector3(0f, 0.0045f, 0f), glowScale * 1.004f, glowAlpha * Mathf.Clamp01(blend * 0.88f));
            }
        }

        private static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
            {
                return 0;
            }

            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }

        private static void DrawPlane(string texPath, Vector3 loc, float scale, float alpha)
        {
            if (string.IsNullOrEmpty(texPath) || alpha <= 0.001f)
            {
                return;
            }

            Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
