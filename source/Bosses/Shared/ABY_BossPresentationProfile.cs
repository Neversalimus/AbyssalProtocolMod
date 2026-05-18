using UnityEngine;

namespace AbyssalProtocol
{
    public sealed class ABY_BossPresentationProfile
    {
        public string id;
        public Color vignetteColor;
        public Color bloomColor;
        public Color noiseColor;
        public Color ritualColor;
        public float vignetteAlpha;
        public float bloomAlpha;
        public float noiseAlpha;
        public float pulseSpeed;
        public float introSurgeAlpha;
        public int mapEffectIntervalTicks;
        public int mapEffectCount;
        public float mapEffectRadius;
        public float lightningSize;
        public float fireGlowSize;
        public float microSparkChance;
        public float extraScreenPulse;

        public static ABY_BossPresentationProfile Default => new ABY_BossPresentationProfile
        {
            id = "default",
            vignetteColor = new Color(0.58f, 0.06f, 0.06f, 1f),
            bloomColor = new Color(0.95f, 0.18f, 0.08f, 1f),
            noiseColor = new Color(0.95f, 0.28f, 0.16f, 1f),
            ritualColor = new Color(0.82f, 0.08f, 0.08f, 1f),
            vignetteAlpha = 0.30f,
            bloomAlpha = 0.12f,
            noiseAlpha = 0.045f,
            pulseSpeed = 3.1f,
            introSurgeAlpha = 0.24f,
            mapEffectIntervalTicks = 37,
            mapEffectCount = 3,
            mapEffectRadius = 6.5f,
            lightningSize = 0.95f,
            fireGlowSize = 0.34f,
            microSparkChance = 0.45f,
            extraScreenPulse = 0.06f
        };
    }
}
