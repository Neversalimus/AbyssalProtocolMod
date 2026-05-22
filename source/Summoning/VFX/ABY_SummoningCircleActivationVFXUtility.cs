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
        private const float IgnitionScale = 8.18f;
        private const float BloomScale = 3.62f;
        private const float BeamWidth = 2.42f;
        private const float BeamHeight = 8.64f;
        private const float BeamBaseAnchorFromBottom = 0.104f;

        private static readonly string[] IgnitionFramePaths = BuildFramePaths("ABY_CircleIgnition");
        private static readonly string[] BloomFramePaths = BuildFramePaths("ABY_ReactorBloom");
        private static readonly string[] BeamFramePaths = BuildFramePaths("ABY_AscensionBeam");
        private static readonly HashSet<string> MissingTexturePaths = new HashSet<string>();

        public static void DrawPriming(
            Building_AbyssalSummoningCircle circle,
            Vector3 center,
            float progress,
            int seed,
            bool reducedEffects,
            float visibility)
        {
            if (circle == null || circle.Map == null || visibility <= 0.001f)
            {
                return;
            }

            float clampedProgress = Mathf.Clamp01(progress);
            float fade = Mathf.Clamp01(visibility);
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float pulse = 0.5f + 0.5f * Mathf.Sin((ticks + seed * 0.041f) * 0.072f);
            float reducedFactor = reducedEffects ? 0.56f : 1f;
            float ignitionProgress = Mathf.Clamp01(clampedProgress * 0.68f + 0.03f);
            float ignitionAlpha = Mathf.Lerp(0.10f, 0.24f, Mathf.SmoothStep(0f, 1f, clampedProgress)) * fade * reducedFactor;
            int ignitionFrame = FrameFromProgress(ignitionProgress);
            float ignitionScale = IgnitionScale * Mathf.Lerp(0.84f, 0.91f, Mathf.SmoothStep(0f, 1f, clampedProgress)) * (1f + (pulse - 0.5f) * 0.018f);
            Vector3 ignitionLoc = center;
            ignitionLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + GroundOverlayAltitude;
            DrawFrame(IgnitionFramePaths, ignitionFrame, ignitionLoc, new Vector2(ignitionScale, ignitionScale), 0f, ignitionAlpha, true);

            if (clampedProgress > 0.72f)
            {
                float bloomProgress = Mathf.Clamp01((clampedProgress - 0.72f) / 0.28f) * 0.32f;
                float bloomAlpha = Mathf.Lerp(0.04f, 0.12f, bloomProgress) * fade * reducedFactor;
                float bloomScale = BloomScale * 0.62f * (1f + (pulse - 0.5f) * 0.030f);
                Vector3 bloomLoc = center;
                bloomLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + BloomOverlayAltitude;
                DrawFrame(BloomFramePaths, FrameFromProgress(bloomProgress), bloomLoc, new Vector2(bloomScale, bloomScale), 0f, bloomAlpha, true);
            }
        }

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
                    float ignitionProgress = Mathf.Clamp01(progress * 1.75f + 0.08f);
                    float ignitionAlpha = Mathf.Lerp(0.38f, 0.64f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress * 1.25f))) * reducedFactor;
                    DrawIgnition(center, ignitionProgress, ignitionAlpha, pulse, seed);

                    if (progress > 0.64f)
                    {
                        float earlyBloomProgress = Mathf.Clamp01((progress - 0.64f) / 0.36f);
                        DrawBloom(center, earlyBloomProgress * 0.42f, Mathf.Lerp(0.08f, 0.26f, earlyBloomProgress) * reducedFactor, pulse, seed);
                    }
                    break;

                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Surge:
                    DrawIgnition(center, 1f, Mathf.Lerp(0.28f, 0.18f, progress) * reducedFactor, pulse, seed);
                    DrawBloom(center, Mathf.Clamp01(progress * 1.18f), Mathf.Lerp(0.44f, 0.82f, Mathf.SmoothStep(0f, 1f, progress)) * reducedFactor, pulse, seed);
                    break;

                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Breach:
                    DrawIgnition(center, 1f, Mathf.Lerp(0.16f, 0.06f, progress) * reducedFactor, pulse, seed);
                    DrawBloom(center, Mathf.Lerp(0.82f, 1f, progress), Mathf.Lerp(0.42f, 0.16f, progress) * reducedFactor, pulse, seed);
                    if (!reducedEffects)
                    {
                        DrawBeam(center, progress, pulse, seed);
                    }
                    else if (progress < 0.48f)
                    {
                        DrawBloom(center, 1f, Mathf.Lerp(0.34f, 0.12f, progress / 0.48f), pulse, seed);
                    }
                    break;

                case Building_AbyssalSummoningCircle.ConsoleRitualPhase.Cooldown:
                    float fade = 1f - progress;
                    DrawIgnition(center, 1f, 0.07f * fade * reducedFactor, pulse, seed);
                    DrawBloom(center, 1f, 0.09f * fade * reducedFactor, pulse, seed);
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
            float clampedProgress = Mathf.Clamp01(progress);
            float alphaIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(clampedProgress / 0.10f));
            float alphaOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((clampedProgress - 0.78f) / 0.22f));
            float alpha = Mathf.Clamp01(alphaIn * alphaOut);

            int frame = FrameFromProgress(clampedProgress);
            float surge = 1f + Mathf.Sin(clampedProgress * Mathf.PI) * 0.08f;
            DrawBeamFrame(center, frame, alpha, pulse, surge);
        }

        private static void DrawBeamAfterglow(Vector3 center, float progress, float pulse, int seed)
        {
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            DrawBeamFrame(center, FrameCount - 1, 0.22f * fade, pulse, 0.72f);
        }

        private static void DrawBeamFrame(Vector3 center, int frame, float alpha, float pulse, float scaleFactor)
        {
            if (alpha <= 0.001f)
            {
                return;
            }

            float width = BeamWidth * scaleFactor * (1f + (pulse - 0.5f) * 0.026f);
            float height = BeamHeight * scaleFactor * (1f + (pulse - 0.5f) * 0.014f);
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
            Material material = ABY_MaterialCacheUtility.MatFrom(path, postLight ? ShaderDatabase.TransparentPostLight : ShaderDatabase.Transparent, color);
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
