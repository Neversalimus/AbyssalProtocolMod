using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_BossPresentationUtility
    {
        private const int VignetteTextureSize = 256;
        private const int BloomTextureSize = 192;
        private const int NoiseTextureSize = 96;

        private static Texture2D vignetteTexture;
        private static Texture2D bloomTexture;
        private static Texture2D noiseTexture;

        public static ABY_BossPresentationProfile ResolveProfile(Pawn boss, ABY_BossBarProfileDef bossBarProfile)
        {
            string thingDef = boss?.def?.defName;
            string kindDef = boss?.kindDef?.defName;
            string style = bossBarProfile?.styleId;

            if (Matches(thingDef, kindDef, "ABY_ReactorSaint") || style == "abyssal_reactor_saint")
            {
                return new ABY_BossPresentationProfile
                {
                    id = "reactor_saint",
                    vignetteColor = new Color(0.02f, 0.32f, 0.55f, 1f),
                    bloomColor = new Color(0.46f, 0.92f, 1.00f, 1f),
                    noiseColor = new Color(0.70f, 0.96f, 1.00f, 1f),
                    ritualColor = new Color(0.25f, 0.84f, 1.00f, 1f),
                    vignetteAlpha = 0.24f,
                    bloomAlpha = 0.16f,
                    noiseAlpha = 0.060f,
                    pulseSpeed = 4.4f,
                    introSurgeAlpha = 0.30f,
                    mapEffectIntervalTicks = 28,
                    mapEffectCount = 4,
                    mapEffectRadius = 8.5f,
                    lightningSize = 1.10f,
                    fireGlowSize = 0.18f,
                    microSparkChance = 0.72f,
                    extraScreenPulse = 0.08f
                };
            }

            if (Matches(thingDef, kindDef, "ABY_ArchonOfRupture") || style == "abyssal_rupture")
            {
                return new ABY_BossPresentationProfile
                {
                    id = "rupture",
                    vignetteColor = new Color(0.38f, 0.02f, 0.58f, 1f),
                    bloomColor = new Color(0.82f, 0.12f, 1.00f, 1f),
                    noiseColor = new Color(0.96f, 0.34f, 1.00f, 1f),
                    ritualColor = new Color(0.62f, 0.10f, 0.92f, 1f),
                    vignetteAlpha = 0.31f,
                    bloomAlpha = 0.15f,
                    noiseAlpha = 0.070f,
                    pulseSpeed = 3.8f,
                    introSurgeAlpha = 0.30f,
                    mapEffectIntervalTicks = 31,
                    mapEffectCount = 4,
                    mapEffectRadius = 7.5f,
                    lightningSize = 1.00f,
                    fireGlowSize = 0.22f,
                    microSparkChance = 0.68f,
                    extraScreenPulse = 0.09f
                };
            }

            if (Matches(thingDef, kindDef, "ABY_ArchonBeast") || style == "abyssal_archon")
            {
                return new ABY_BossPresentationProfile
                {
                    id = "archon_beast",
                    vignetteColor = new Color(0.72f, 0.02f, 0.02f, 1f),
                    bloomColor = new Color(1.00f, 0.20f, 0.06f, 1f),
                    noiseColor = new Color(1.00f, 0.30f, 0.12f, 1f),
                    ritualColor = new Color(0.92f, 0.08f, 0.02f, 1f),
                    vignetteAlpha = 0.36f,
                    bloomAlpha = 0.14f,
                    noiseAlpha = 0.065f,
                    pulseSpeed = 3.2f,
                    introSurgeAlpha = 0.34f,
                    mapEffectIntervalTicks = 26,
                    mapEffectCount = 5,
                    mapEffectRadius = 7.0f,
                    lightningSize = 0.95f,
                    fireGlowSize = 0.42f,
                    microSparkChance = 0.58f,
                    extraScreenPulse = 0.10f
                };
            }

            if (Matches(thingDef, kindDef, "ABY_WardenOfAsh"))
            {
                return new ABY_BossPresentationProfile
                {
                    id = "warden_ash",
                    vignetteColor = new Color(0.62f, 0.12f, 0.03f, 1f),
                    bloomColor = new Color(1.00f, 0.34f, 0.10f, 1f),
                    noiseColor = new Color(1.00f, 0.42f, 0.18f, 1f),
                    ritualColor = new Color(0.90f, 0.18f, 0.04f, 1f),
                    vignetteAlpha = 0.27f,
                    bloomAlpha = 0.12f,
                    noiseAlpha = 0.050f,
                    pulseSpeed = 3.0f,
                    introSurgeAlpha = 0.22f,
                    mapEffectIntervalTicks = 34,
                    mapEffectCount = 3,
                    mapEffectRadius = 5.5f,
                    lightningSize = 0.72f,
                    fireGlowSize = 0.38f,
                    microSparkChance = 0.44f,
                    extraScreenPulse = 0.05f
                };
            }

            if (Matches(thingDef, kindDef, "ABY_ChoirEngine"))
            {
                return new ABY_BossPresentationProfile
                {
                    id = "choir_engine",
                    vignetteColor = new Color(0.20f, 0.02f, 0.42f, 1f),
                    bloomColor = new Color(0.34f, 0.48f, 1.00f, 1f),
                    noiseColor = new Color(0.72f, 0.62f, 1.00f, 1f),
                    ritualColor = new Color(0.35f, 0.24f, 0.92f, 1f),
                    vignetteAlpha = 0.28f,
                    bloomAlpha = 0.14f,
                    noiseAlpha = 0.075f,
                    pulseSpeed = 5.2f,
                    introSurgeAlpha = 0.24f,
                    mapEffectIntervalTicks = 30,
                    mapEffectCount = 4,
                    mapEffectRadius = 6.0f,
                    lightningSize = 0.82f,
                    fireGlowSize = 0.12f,
                    microSparkChance = 0.76f,
                    extraScreenPulse = 0.07f
                };
            }

            return ABY_BossPresentationProfile.Default;
        }

        public static void DrawBossScreenOverlay(Pawn boss, ABY_BossBarProfileDef profileDef, float bossStrength, float ritualStrength, float introSurgeStrength, int effectStartTick)
        {
            if (!AbyssalProtocolMod.Settings.enableBossScreenEffects)
            {
                return;
            }

            float totalStrength = Mathf.Clamp01(bossStrength + ritualStrength * 0.78f + introSurgeStrength * 0.65f);
            if (totalStrength <= 0.001f)
            {
                return;
            }

            EnsureTextures();
            ABY_BossPresentationProfile profile = ResolveProfile(boss, profileDef);
            float t = effectStartTick > 0 && Find.TickManager != null ? (Find.TickManager.TicksGame - effectStartTick) / 60f : Time.realtimeSinceStartup;
            float wave = 0.5f + 0.5f * Mathf.Sin(t * profile.pulseSpeed);
            float slowWave = 0.5f + 0.5f * Mathf.Sin(t * (profile.pulseSpeed * 0.43f) + 1.7f);
            float fade = Mathf.SmoothStep(0f, 1f, totalStrength);
            float intro = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(introSurgeStrength));
            float bossFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(bossStrength));
            float ritual = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ritualStrength));
            Rect full = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);

            Color vignette = profile.vignetteColor;
            vignette.a = fade * (profile.vignetteAlpha + wave * profile.extraScreenPulse + intro * profile.introSurgeAlpha * 0.55f);
            DrawTexture(full, VignetteTexture, vignette);

            Color bloom = Color.Lerp(profile.bloomColor, profile.ritualColor, ritual * 0.45f);
            bloom.a = fade * (profile.bloomAlpha + slowWave * 0.05f + intro * profile.introSurgeAlpha);
            DrawTexture(full, BloomTexture, bloom);

            if (!AbyssalProtocolMod.Settings.reducedMotion)
            {
                Color noise = Color.Lerp(profile.noiseColor, profile.ritualColor, ritual * 0.35f);
                noise.a = fade * (profile.noiseAlpha + intro * 0.035f + bossFade * wave * 0.022f);
                Rect texCoords = new Rect((t * 0.017f) % 1f, (t * 0.011f) % 1f, 3.2f, 2.1f);
                DrawTextureTiled(full, NoiseTexture, texCoords, noise);
            }
        }

        public static void SpawnIntroBurst(Pawn boss, ABY_BossBarProfileDef profileDef)
        {
            if (boss?.MapHeld == null)
            {
                return;
            }

            ABY_BossPresentationProfile profile = ResolveProfile(boss, profileDef);
            for (int i = 0; i < Mathf.Max(4, profile.mapEffectCount + 3); i++)
            {
                Vector3 loc = RandomPointNear(boss, profile.mapEffectRadius + 2.5f);
                FleckMaker.ThrowLightningGlow(loc, boss.MapHeld, profile.lightningSize * Rand.Range(1.0f, 1.8f));
                if (profile.fireGlowSize > 0.01f)
                {
                    FleckMaker.ThrowFireGlow(loc, boss.MapHeld, profile.fireGlowSize * Rand.Range(1.0f, 1.65f));
                }
                if (Rand.Chance(0.8f))
                {
                    FleckMaker.ThrowMicroSparks(loc, boss.MapHeld);
                }
            }
        }

        public static void SpawnAmbientMapEffects(Pawn boss, ABY_BossBarProfileDef profileDef, float strength)
        {
            if (!AbyssalProtocolMod.Settings.enableBossMapPresentationEffects || boss?.MapHeld == null || strength <= 0.001f)
            {
                return;
            }

            ABY_BossPresentationProfile profile = ResolveProfile(boss, profileDef);
            int count = Mathf.Clamp(Mathf.RoundToInt(profile.mapEffectCount * Mathf.Clamp01(strength)), 1, 8);
            for (int i = 0; i < count; i++)
            {
                Vector3 loc = RandomPointNear(boss, profile.mapEffectRadius);
                FleckMaker.ThrowLightningGlow(loc, boss.MapHeld, profile.lightningSize * Rand.Range(0.75f, 1.25f));
                if (profile.fireGlowSize > 0.01f && Rand.Chance(0.50f))
                {
                    FleckMaker.ThrowFireGlow(loc, boss.MapHeld, profile.fireGlowSize * Rand.Range(0.75f, 1.25f));
                }
                if (Rand.Chance(profile.microSparkChance))
                {
                    FleckMaker.ThrowMicroSparks(loc, boss.MapHeld);
                }
            }
        }

        public static int ResolveMapEffectIntervalTicks(Pawn boss, ABY_BossBarProfileDef profileDef)
        {
            return Mathf.Clamp(ResolveProfile(boss, profileDef).mapEffectIntervalTicks, 18, 90);
        }

        private static bool Matches(string thingDef, string kindDef, string expected)
        {
            return string.Equals(thingDef, expected, StringComparison.Ordinal) || string.Equals(kindDef, expected, StringComparison.Ordinal);
        }

        private static Vector3 RandomPointNear(Pawn boss, float radius)
        {
            Map map = boss.MapHeld;
            float angle = Rand.Range(0f, Mathf.PI * 2f);
            float dist = Rand.Range(1.2f, Mathf.Max(1.3f, radius));
            IntVec3 cell = boss.Position + new IntVec3(Mathf.RoundToInt(Mathf.Cos(angle) * dist), 0, Mathf.RoundToInt(Mathf.Sin(angle) * dist));
            if (!cell.IsValid || !cell.InBounds(map))
            {
                cell = boss.Position;
            }

            return cell.ToVector3Shifted() + new Vector3(Rand.Range(-0.34f, 0.34f), 0f, Rand.Range(-0.34f, 0.34f));
        }

        private static void DrawTexture(Rect rect, Texture texture, Color color)
        {
            if (texture == null || rect.width <= 0f || rect.height <= 0f || color.a <= 0.001f)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
        }

        private static void DrawTextureTiled(Rect rect, Texture texture, Rect texCoords, Color color)
        {
            if (texture == null || rect.width <= 0f || rect.height <= 0f || color.a <= 0.001f)
            {
                return;
            }

            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(rect, texture, texCoords);
            GUI.color = oldColor;
        }

        private static void EnsureTextures()
        {
            if (vignetteTexture == null)
            {
                vignetteTexture = BuildRadialTexture(VignetteTextureSize, true, 0.18f, 0.98f, 1.75f);
                vignetteTexture.wrapMode = TextureWrapMode.Clamp;
            }
            if (bloomTexture == null)
            {
                bloomTexture = BuildRadialTexture(BloomTextureSize, false, 0.08f, 0.92f, 2.25f);
                bloomTexture.wrapMode = TextureWrapMode.Clamp;
            }
            if (noiseTexture == null)
            {
                noiseTexture = BuildNoiseTexture(NoiseTextureSize);
                noiseTexture.wrapMode = TextureWrapMode.Repeat;
            }
        }

        private static Texture2D BuildRadialTexture(int size, bool edge, float inner, float outer, float power)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color32[] pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - half) / half;
                    float ny = (y - half) / half;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.InverseLerp(inner, outer, d);
                    a = Mathf.Clamp01(a);
                    a = edge ? Mathf.Pow(a, power) : Mathf.Pow(1f - a, power);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D BuildNoiseTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color32[] pixels = new Color32[size * size];
            System.Random random = new System.Random(177013);
            for (int i = 0; i < pixels.Length; i++)
            {
                int raw = random.Next(0, 256);
                int a = raw > 212 ? random.Next(36, 110) : random.Next(0, 20);
                pixels[i] = new Color32(255, 255, 255, (byte)a);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D VignetteTexture => vignetteTexture;
        private static Texture2D BloomTexture => bloomTexture;
        private static Texture2D NoiseTexture => noiseTexture;
    }
}
