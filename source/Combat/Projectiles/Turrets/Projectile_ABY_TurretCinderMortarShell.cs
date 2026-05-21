using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_TurretCinderMortarShell : Bullet
    {
        private int ticksAlive;
        private Vector3 lastExactPosition;
        private Vector3 lastDrawDirection = Vector3.forward;
        private bool lastPositionInitialized;
        private bool launchSpawned;

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

            if (!launchSpawned)
            {
                launchSpawned = true;
                CinderMortarVfxUtility.SpawnLaunch(previousPosition, destination, Map);
            }

            if (ticksAlive % 9 == 0)
            {
                FleckMaker.ThrowLightningGlow(currentPosition, Map, 0.18f);
            }

            lastExactPosition = currentPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Vector3 impactDirection = lastDrawDirection;
            IntVec3 impactCell = impactPosition.ToIntVec3();

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, hitThing, "Projectile_ABY_TurretCinderMortarShell", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null)
            {
                return;
            }

            CinderMortarVfxUtility.SpawnImpact(impactPosition, impactDirection, impactMap, blockedByShield);
            if (!blockedByShield)
            {
                CinderMortarVfxUtility.SpawnResiduePatch(impactCell, impactMap, launcher);
            }
        }
    }
}
