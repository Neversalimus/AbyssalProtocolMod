using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ReactorSaintBolt : Bullet
    {
        private const int TrailIntervalTicks = 2;
        private const float TrailGlowSize = 0.34f;
        private const float ImpactGlowSize = 1.55f;
        private const float SplashRadius = 1.05f;
        private const int SplashDamage = 6;
        private const float SplashArmorPenetration = 0.24f;

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
                SpawnTrail(lastExactPosition, ExactPosition, Map);
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

            SpawnImpactEffects(impactPosition, impactMap);
            if (blockedByShield)
            {
                return;
            }

            GenExplosion.DoExplosion(impactCell, impactMap, SplashRadius, DamageDefOf.Burn, instigator, SplashDamage, SplashArmorPenetration);
        }

        private static void SpawnTrail(Vector3 from, Vector3 to, Map map)
        {
            for (int i = 1; i <= 2; i++)
            {
                Vector3 point = Vector3.Lerp(from, to, i / 3f);
                FleckMaker.ThrowLightningGlow(point, map, TrailGlowSize);
                if (i == 2)
                {
                    FleckMaker.ThrowMicroSparks(point, map);
                }
            }
        }

        private static void SpawnImpactEffects(Vector3 position, Map map)
        {
            FleckMaker.ThrowLightningGlow(position, map, ImpactGlowSize);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
        }
    }
}
