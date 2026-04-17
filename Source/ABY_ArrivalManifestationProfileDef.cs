using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_ArrivalManifestationProfileDef : Def
    {
        public bool enabledByDefault = false;
        public int defaultWarmupTicks = 90;
        public string requiredFeatureFlag;
        public List<ABY_ArrivalManifestationProfileEntry> options = new List<ABY_ArrivalManifestationProfileEntry>();

        public bool IsEnabledForFutureUse()
        {
            if (!ABY_ManifestationFeatureFlags.IsGlobalFutureMatrixEnabled())
            {
                return false;
            }

            if (!string.IsNullOrEmpty(requiredFeatureFlag))
            {
                return ABY_ManifestationFeatureFlags.IsNamedFlagEnabled(requiredFeatureFlag);
            }

            return enabledByDefault;
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (defaultWarmupTicks < 30)
            {
                yield return defName + " has defaultWarmupTicks < 30.";
            }

            if (options == null || options.Count == 0)
            {
                yield return defName + " has no manifestation options.";
                yield break;
            }

            for (int i = 0; i < options.Count; i++)
            {
                ABY_ArrivalManifestationProfileEntry option = options[i];
                if (option == null)
                {
                    yield return defName + " has a null manifestation option.";
                    continue;
                }

                if (option.weight <= 0f)
                {
                    yield return defName + " has an option with non-positive weight (index " + i + ").";
                }

                if (option.warmupTicksOverride > 0 && option.warmupTicksOverride < 30)
                {
                    yield return defName + " has an option with warmupTicksOverride < 30 (index " + i + ").";
                }
            }
        }
    }
}
