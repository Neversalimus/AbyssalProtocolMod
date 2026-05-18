using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_NullDisruptorPulse : Bullet
    {
        private const float MechOnlyBonusDamage = 8f;
        private const float MechOnlyBonusArmorPenetration = 0.32f;
        private const float ImpactGlowSize = 0.92f;
        private const float ShieldBlockedGlowSize = 0.62f;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn impactPawn = ResolveImpactPawn(hitThing);
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            SpawnImpactFeedback(impactPosition, impactMap, blockedByShield);

            if (blockedByShield || impactPawn == null || impactPawn.Dead || impactPawn.health == null || impactPawn.RaceProps == null || !impactPawn.RaceProps.IsMechanoid)
            {
                return;
            }

            ApplyMechOnlyStaticRupture(impactPawn, instigator);
        }

        private static void ApplyMechOnlyStaticRupture(Pawn mechanoid, Thing instigator)
        {
            if (mechanoid == null || mechanoid.Dead || mechanoid.health == null)
            {
                return;
            }

            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Burn,
                MechOnlyBonusDamage,
                MechOnlyBonusArmorPenetration,
                -1f,
                instigator,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown);

            mechanoid.TakeDamage(damageInfo);
        }

        private static void SpawnImpactFeedback(Vector3 position, Map map, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? ShieldBlockedGlowSize : ImpactGlowSize);
            FleckMaker.ThrowMicroSparks(position, map);
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
                if (things[i] is Pawn pawn)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}
