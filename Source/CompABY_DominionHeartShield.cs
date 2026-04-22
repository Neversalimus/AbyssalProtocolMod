using Verse;

namespace AbyssalProtocol
{
    public class CompABY_DominionHeartShield : ThingComp
    {
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
            }
        }
    }
}
