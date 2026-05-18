using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_AshenPikeSpike : Bullet
    {
        private const int TrailIntervalTicks = 3;
        private const float TrailHeatGlowSize = 0.18f;
        private const float TrailDustSize = 0.34f;
        private const float ImpactHeatGlowSize = 0.90f;
        private const float ImpactFireGlowSize = 0.62f;
        private const float ImpactSmokeSize = 0.72f;
        private const float ImpactDustSize = 0.95f;

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
            Vector3 impactPosition = ExactPosition;
            base.Impact(hitThing, blockedByShield);
            if (impactMap == null)
            {
                return;
            }

            SpawnImpactEffects(impactPosition, impactMap, blockedByShield);
        }

        private static void SpawnTrail(Vector3 from, Vector3 to, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 point = Vector3.Lerp(from, to, 0.5f);
            FleckMaker.ThrowHeatGlow(point.ToIntVec3(), map, TrailHeatGlowSize);
            FleckMaker.ThrowDustPuff(point, map, TrailDustSize);
        }

        private static void SpawnImpactEffects(Vector3 position, Map map, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowHeatGlow(position.ToIntVec3(), map, blockedByShield ? 0.55f : ImpactHeatGlowSize);
            FleckMaker.ThrowFireGlow(position, map, blockedByShield ? 0.38f : ImpactFireGlowSize);
            FleckMaker.ThrowDustPuff(position, map, blockedByShield ? 0.55f : ImpactDustSize);
            FleckMaker.ThrowSmoke(position, map, blockedByShield ? 0.42f : ImpactSmokeSize);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
        }
    }
}
