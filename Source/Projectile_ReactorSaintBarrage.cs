using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ReactorSaintBarrage : Bullet
    {
        private const int TrailIntervalTicks = 3;
        private const float TrailGlowSize = 0.28f;
        private const float ImpactGlowSize = 1.95f;
        private const float ExplosionRadius = 1.95f;
        private const int ExplosionDamage = 17;
        private const float ExplosionArmorPenetration = 0.42f;

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private bool lastPositionInitialized;

        protected override void Tick()
        {
            Vector3 previousPosition = ExactPosition;
            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksAlive++;

            if (!lastPositionInitialized)
            {
                lastExactPosition = previousPosition;
                lastPositionInitialized = true;
            }

            if (ticksAlive % TrailIntervalTicks == 0)
            {
                Vector3 point = Vector3.Lerp(lastExactPosition, ExactPosition, 0.5f);
                FleckMaker.ThrowLightningGlow(point, Map, TrailGlowSize);
            }

            lastExactPosition = ExactPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null || !impactCell.IsValid)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(impactPosition, impactMap, ImpactGlowSize);
            FleckMaker.ThrowMicroSparks(impactPosition, impactMap);
            FleckMaker.ThrowMicroSparks(impactPosition, impactMap);
            ABY_SoundUtility.PlayAt("ABY_ReactorSaintBarrageImpact", impactCell, impactMap);

            if (blockedByShield)
            {
                return;
            }

            GenExplosion.DoExplosion(impactCell, impactMap, ExplosionRadius, DamageDefOf.Burn, instigator, ExplosionDamage, ExplosionArmorPenetration);
        }
    }
}
