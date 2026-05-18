using Verse;

namespace AbyssalProtocol
{
    public class DefModExtension_AbyssalCircleCapacitor : DefModExtension
    {
        public int tier = 1;
        public float chargeCapacity = 0f;
        public float throughput = 0f;
        public float chargeRatePerSecond = 0f;
        public float surgeTolerance = 0f;
        public float passiveLeakage = 0f;
        public bool allowCoreBay = true;
        public bool allowAuxiliaryBay = true;
        public string mountedTexPath;
        public string mountedGlowTexPath;
        public float mountedDrawScale = 1f;
    }
}
