using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_SummoningCircleActivationVFXUtility
    {
        private const int FrameCount = 8;
        private const float GroundOverlayAltitude = 0.052f;
        private const float BloomOverlayAltitude = 0.058f;
        private const float BeamOverlayAltitude = 0.082f;
        private const float IgnitionScale = 8.66f;
        private const float BloomScale = 4.72f;
        private const float BeamWidth = 2.72f;
        private const float BeamHeight = 8.92f;
        private const float BeamBaseAnchorFromBottom = 0.0625f;

        private static readonly string[] IgnitionFramePaths = BuildFramePaths("ABY_CircleIgnition");
        private static readonly string[] BloomFramePaths = BuildFramePaths("ABY_ReactorBloom");
        private static readonly string[] BeamFramePaths = BuildFramePaths("ABY_AscensionBeam");
        private static readonly HashSet<string> MissingTexturePaths = new HashSet<string>();

        public static void Draw(
            Building_AbyssalSummoningCircle circle,
            Vector3 center,
            Building_AbyssalSummoningCircle.ConsoleRitualPhase phase,
            float phaseProgress,
            int seed,
            bool reducedEffects)
        {
            if (circle == null || circle.Map == null || phase == Building_AbyssalSummoningCircle.ConsoleRitualPhase.Idle)
            {
                return;
            }

            float progress = Mathf.Clamp01(phaseProgress);
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float pulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed * 0.037f) * 0.078f);
            float reducedFactor = reducedEffects ? 0.58f : 1f;

            switch (phase)
            {
                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Charging:
                    DrawIgnition(center, progress, Mathf.Lerp(0.62f, 0.96f, progress) * reducedFactor, pulse, seed);
                    break;

                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Surge:
                    DrawIgnition(center, 1f, 0.34f * reducedFactor, pulse, seed);
                    DrawBloom(center, progress, Mathf.Lerp(0.52f, 1.00f, progress) * reducedFactor, pulse, seed);
                    break;

                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Breach:
                    DrawIgnition(center, 1f, Mathf.Lerp(0.22f, 0.10f, progress) * reducedFactor, pulse, seed);
                    DrawBloom(center, Mathf.Lerp(0.72f, 1f, progress), Mathf.Lerp(0.64f, 0.26f, progress) * reducedFactor, pulse, seed);
                    if (!reducedEffects)
                    {
                        DrawBeam(center, progress, pulse, seed);
                    }
                    else if (progress < 0.52f)
                    {
                        DrawBloom(center, 1f, Mathf.Lerp(0.46f, 0.18f, progress / 0.52f), pulse, seed);
                    }
                    break;

                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Cooldown:
                    float fade = 1f - progress;
                    DrawIgnition(center, 1f, 0.12f * fade * reducedFactor, pulse, seed);
                    DrawBloom(center, 1f, 0.16f * fade * reducedFactor, pulse, seed);
                    break;
            }
        }

        private static void DrawIgnition(Vector3 center, float progress, float alpha, float pulse, int seed)
        {
            int frame = FrameFromProgress(progress);
            float drawScale = IgnitionScale * (1f + (pulse - 0.5f) * 0.024f);
            Vector3 loc = center;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + GroundOverlayAltitude;
            DrawFrame(IgnitionFramePaths, frame, loc, new Vector2(drawScale, drawScale), 0f, alpha, true);
        }

        private static void DrawBloom(Vector3 center, float progress, float alpha, float pulse, int seed)
        {
            int frame = FrameFromProgress(progress);
            float drawScale = BloomScale * (1f + (pulse - 0.5f) * 0.040f);
            Vector3 loc = center;
            loc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + BloomOverlayAltitude;
            DrawFrame(BloomFramePaths, frame, loc, new Vector2(drawScale, drawScale), 0f, alpha, true);
        }

        private static void DrawBeam(Vector3 center, float progress, float pulse, int seed)
        {
            float beamProgress;
            float alpha;

            if (progress <= 0.72f)
            {
                beamProgress = progress / 0.72f;
                alpha = Mathf.Lerp(0.82f, 1.00f, Mathf.Clamp01(beamProgress * 1.45f));
            }
            else
            {
                beamProgress = 1f;
                alpha = Mathf.Lerp(0.34f, 0f, (progress - 0.72f) / 0.28f);
            }

            int frame = FrameFromProgress(beamProgress);
            float width = BeamWidth * (1f + (pulse - 0.5f) * 0.026f);
            float height = BeamHeight * (1f + (pulse - 0.5f) * 0.014f);
            Vector3 loc = center;
            loc.z += height * (0.5f - BeamBaseAnchorFromBottom);
            loc.y = AltitudeLayer.MoteOverhead.AltitudeFor() + BeamOverlayAltitude;
            DrawFrame(BeamFramePaths, frame, loc, new Vector2(width, height), 0f, alpha, true);
        }

        private static int FrameFromProgress(float progress)
        {
            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress) * FrameCount), 0, FrameCount - 1);
        }

        private static string[] BuildFramePaths(string prefix)
        {
            string[] paths = new string[FrameCount];
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = "Things/VFX/SummoningCircleActivation/" + prefix + "_Frame" + i;
            }

            return paths;
        }

        private static void DrawFrame(string[] paths, int frame, Vector3 loc, Vector2 size, float angle, float alpha, bool postLight)
        {
            if (paths == null || paths.Length == 0 || alpha <= 0.001f)
            {
                return;
            }

            string path = paths[PositiveModulo(frame, paths.Length)];
            if (string.IsNullOrEmpty(path) || MissingTexturePaths.Contains(path))
            {
                return;
            }

            if (ContentFinder<Texture2D>.Get(path, false) == null)
            {
                MissingTexturePaths.Add(path);
                return;
            }

            Color color = new Color(1f, 1f, 1f, QuantizeAlpha(alpha));
            Material material = MaterialPool.MatFrom(path, postLight ? ShaderDatabase.TransparentPostLight : ShaderDatabase.Transparent, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(size.x, 1f, size.y));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
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

        private static float QuantizeAlpha(float value)
        {
            return Mathf.Clamp01(Mathf.Round(value * 48f) / 48f);
        }
    }
}
