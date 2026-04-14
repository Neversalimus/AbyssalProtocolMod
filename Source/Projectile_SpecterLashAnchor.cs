using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_SpecterLashAnchor : Bullet
    {
        private const float ImpactGlowSize = 1.25f;
        private const float ShieldImpactGlowSize = 0.92f;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn impactPawn = ResolveImpactPawn(hitThing);
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Pawn launcherPawn = Launcher as Pawn;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(impactPosition, impactMap, blockedByShield ? ShieldImpactGlowSize : ImpactGlowSize);
            FleckMaker.ThrowMicroSparks(impactPosition, impactMap);

            if (blockedByShield || launcherPawn == null || impactPawn == null || impactPawn.Dead)
            {
                return;
            }

            if (!GenHostility.HostileTo(launcherPawn, impactPawn))
            {
                return;
            }

            SpecterLashStreamGameComponent component = Current.Game?.GetComponent<SpecterLashStreamGameComponent>();
            component?.TryStartStream(launcherPawn, impactPawn);
        }

        private Pawn ResolveImpactPawn(Thing hitThing)
        {
            Pawn directPawn = hitThing as Pawn;
            if (directPawn != null)
            {
                return directPawn;
            }

            if (Map == null || !Position.IsValid)
            {
                return null;
            }

            var things = Position.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                Pawn pawn = things[i] as Pawn;
                if (pawn != null)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}
