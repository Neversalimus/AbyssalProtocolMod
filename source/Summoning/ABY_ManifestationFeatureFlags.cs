namespace AbyssalProtocol
{
    public static class ABY_ManifestationFeatureFlags
    {
        public static readonly bool EnableFutureManifestationMatrix = false;
        public static readonly bool EnableSigilBloomFutureMatrix = false;
        public static readonly bool EnableStaticPhaseInFutureMatrix = false;
        public static readonly bool EnableSeamBreachFutureMatrix = false;

        public static bool IsGlobalFutureMatrixEnabled()
        {
            return EnableFutureManifestationMatrix;
        }

        public static bool IsTypeEnabled(ABY_ArrivalManifestationType manifestationType)
        {
            if (!EnableFutureManifestationMatrix)
            {
                return false;
            }

            switch (manifestationType)
            {
                case ABY_ArrivalManifestationType.SigilBloom:
                    return EnableSigilBloomFutureMatrix;
                case ABY_ArrivalManifestationType.StaticPhaseIn:
                    return EnableStaticPhaseInFutureMatrix;
                case ABY_ArrivalManifestationType.SeamBreach:
                    return EnableSeamBreachFutureMatrix;
                default:
                    return false;
            }
        }

        public static bool IsNamedFlagEnabled(string featureFlag)
        {
            if (string.IsNullOrEmpty(featureFlag))
            {
                return true;
            }

            switch (featureFlag)
            {
                case "ABY_EnableFutureManifestationMatrix":
                    return EnableFutureManifestationMatrix;
                case "ABY_EnableSigilBloomFutureMatrix":
                    return EnableFutureManifestationMatrix && EnableSigilBloomFutureMatrix;
                case "ABY_EnableStaticPhaseInFutureMatrix":
                    return EnableFutureManifestationMatrix && EnableStaticPhaseInFutureMatrix;
                case "ABY_EnableSeamBreachFutureMatrix":
                    return EnableFutureManifestationMatrix && EnableSeamBreachFutureMatrix;
                default:
                    return false;
            }
        }
    }
}
