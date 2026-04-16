using Verse;

namespace AbyssalProtocol
{
    public enum DominionAnchorRole
    {
        Suppression,
        Drain,
        Ward,
        Breach
    }

    public class DefModExtension_DominionAnchor : DefModExtension
    {
        public DominionAnchorRole role = DominionAnchorRole.Suppression;
        public int pulseIntervalTicks = 240;
        public float pulseRadius = 12f;
        public float empDamage = 3f;
        public int maxAffectedTargets = 4;
        public int healAmount = 14;
        public float contaminationPulse = 0.012f;
        public int timerDrainTicks = 120;
        public string glowTexPath;
        public float glowDrawScale = 2.4f;
    }
}
