using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_DominionHeartShield : CompProperties
    {
        public string guardianKindDefName = "ABY_AorticChainHarrower";
        public float guardianDamageReductionPerGuardian = 0.08f;
        public float maxGuardianDamageReduction = 0.24f;

        public CompProperties_ABY_DominionHeartShield()
        {
            compClass = typeof(CompABY_DominionHeartShield);
        }
    }
}
