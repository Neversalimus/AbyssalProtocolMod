using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_DominionHeartShield : ThingComp
    {
        public CompProperties_ABY_DominionHeartShield Props => (CompProperties_ABY_DominionHeartShield)props;

        private Building_ABY_DominionSliceHeart HeartParent => parent as Building_ABY_DominionSliceHeart;

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            Building_ABY_DominionSliceHeart heart = HeartParent;
            if (heart == null || heart.Destroyed || heart.MapHeld == null)
            {
                return;
            }

            MapComponent_DominionSliceEncounter encounter = heart.MapHeld.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null && !encounter.IsHeartExposed)
            {
                absorbed = true;
                heart.NotifyShieldBlocked();
                return;
            }

            if (encounter == null || !encounter.IsActiveEncounter || !encounter.IsHeartExposed)
            {
                return;
            }

            float factor = encounter.GetHeartGuardianDamageFactor(
                Props.guardianKindDefName,
                Mathf.Max(0f, Props.guardianDamageReductionPerGuardian),
                Mathf.Clamp01(Props.maxGuardianDamageReduction));

            if (factor < 0.999f)
            {
                dinfo.SetAmount(Mathf.Max(0f, dinfo.Amount * factor));
            }
        }
    }
}
