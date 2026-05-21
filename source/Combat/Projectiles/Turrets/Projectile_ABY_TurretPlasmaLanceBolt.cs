using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_TurretPlasmaLanceBolt : Bullet
    {
        private const int TravelSparkIntervalTicks = 7;

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private Vector3 lastDrawDirection = Vector3.forward;
        private bool lastPositionInitialized;
        private bool muzzleSpawned;

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

            if (!muzzleSpawned)
            {
                muzzleSpawned = true;
                PlasmaLanceVfxUtility.SpawnMuzzle(previousPosition, destination, Map);
            }

            if (ticksAlive % TravelSparkIntervalTicks == 0)
            {
                PlasmaLanceVfxUtility.SpawnTravelSpark(currentPosition, Map);
            }

            lastExactPosition = currentPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Vector3 impactDirection = lastDrawDirection;

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, hitThing, "Projectile_ABY_TurretPlasmaLanceBolt", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null)
            {
                return;
            }

            PlasmaLanceVfxUtility.SpawnImpact(impactPosition, impactDirection, impactMap, blockedByShield);
        }
    }
}
