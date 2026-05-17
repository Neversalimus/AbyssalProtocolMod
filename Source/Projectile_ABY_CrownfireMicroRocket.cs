using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_CrownfireMicroRocket : Bullet
    {
        private const int TrailIntervalTicks = 4;

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private Vector3 lastDrawDirection = Vector3.forward;
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

            Vector3 currentPosition = ExactPosition;
            Vector3 movement = currentPosition - lastExactPosition;
            movement.y = 0f;
            if (movement.sqrMagnitude > 0.0001f)
            {
                lastDrawDirection = movement.normalized;
            }

            if (ticksAlive % TrailIntervalTicks == 0)
            {
                CrownfireRocketChoirVfxUtility.SpawnMicroTrail(currentPosition, lastDrawDirection, Map);
            }

            lastExactPosition = currentPosition;
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

            CrownfireRocketChoirVfxUtility.SpawnMicroImpact(impactPosition, impactMap, blockedByShield ? 0.72f : 1f);
        }
    }
}
