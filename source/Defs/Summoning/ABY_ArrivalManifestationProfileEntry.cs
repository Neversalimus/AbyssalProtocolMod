using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_ArrivalManifestationProfileEntry : IExposable
    {
        public ABY_ArrivalManifestationType type = ABY_ArrivalManifestationType.SigilBloom;
        public float weight = 1f;
        public int warmupTicksOverride = -1;
        public bool enabledByDefault = false;
        public string requiredFeatureFlag;

        public bool IsEnabledForFutureUse()
        {
            if (!ABY_ManifestationFeatureFlags.IsGlobalFutureMatrixEnabled())
            {
                return false;
            }

            if (!ABY_ManifestationFeatureFlags.IsTypeEnabled(type))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(requiredFeatureFlag))
            {
                return ABY_ManifestationFeatureFlags.IsNamedFlagEnabled(requiredFeatureFlag);
            }

            return enabledByDefault;
        }

        public int ResolveWarmupTicks(int fallbackWarmupTicks)
        {
            return warmupTicksOverride > 0 ? warmupTicksOverride : fallbackWarmupTicks;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type", ABY_ArrivalManifestationType.SigilBloom);
            Scribe_Values.Look(ref weight, "weight", 1f);
            Scribe_Values.Look(ref warmupTicksOverride, "warmupTicksOverride", -1);
            Scribe_Values.Look(ref enabledByDefault, "enabledByDefault", false);
            Scribe_Values.Look(ref requiredFeatureFlag, "requiredFeatureFlag");
        }
    }
}
