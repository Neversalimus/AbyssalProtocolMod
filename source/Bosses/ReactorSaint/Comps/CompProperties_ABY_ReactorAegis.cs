using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_ReactorAegis : CompProperties
    {
        public float maxAegisPoints = 1900f;
        public int rechargeDelayTicks = 360;
        public int rechargeIntervalTicks = 30;
        public float rechargePerInterval = 105f;
        public string breakSoundDefName = "ABY_ReactorSaintImpact";
        public string restoreSoundDefName = "ABY_ReactorSaintCharge";
        public float breakFlashScale = 3.0f;
        public float restoreFlashScale = 2.4f;

        public bool drawPawnAegisOverlay = true;
        public string activeOverlayTexPath = "Pawn/ReactorSaint/Aegis/ABY_ReactorSaint_AegisActive";
        public string breakOverlayTexPath = "Pawn/ReactorSaint/Aegis/ABY_ReactorSaint_AegisBreak";
        public float activeOverlayAlpha = 0.82f;
        public float breakOverlayAlpha = 0.92f;
        public float overlayDrawScale = 1.0f;
        public float overlayLayerOffset = 0.038f;
        public int collapseOverlayFadeTicks = 90;
        public bool pulseActiveOverlay = true;
        public bool respectReducedMotion = true;

        public CompProperties_ABY_ReactorAegis()
        {
            compClass = typeof(CompABY_ReactorAegis);
        }
    }
}
