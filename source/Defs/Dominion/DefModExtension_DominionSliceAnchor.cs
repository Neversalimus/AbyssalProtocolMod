using Verse;

namespace AbyssalProtocol
{
    public enum DominionSliceAnchorRole
    {
        Seal,
        Choir,
        Law
    }

    public class DefModExtension_DominionSliceAnchor : DefModExtension
    {
        public DominionSliceAnchorRole role = DominionSliceAnchorRole.Seal;
        public int pulseIntervalTicks = 240;
        public float pulseRadius = 12f;
        public string glowTexPath;
        public float glowDrawScale = 2.35f;
    }
}
