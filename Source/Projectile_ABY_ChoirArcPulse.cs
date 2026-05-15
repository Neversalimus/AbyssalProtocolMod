using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_ChoirArcPulse : Bullet
    {
        private const int TravelSparkIntervalTicks = 5;
        private const string ImpactSoundDefName = "ABY_ChoirArcEmitterImpact";

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
                ChoirArcVfxUtility.SpawnMuzzle(previousPosition, destination, Map);
            }

            if (ticksAlive % TravelSparkIntervalTicks == 0)
            {
                ChoirArcVfxUtility.SpawnTravelSpark(currentPosition, Map);
            }

            lastExactPosition = currentPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Vector3 impactDirection = lastDrawDirection;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            ChoirArcVfxUtility.SpawnImpact(impactPosition, impactDirection, impactMap, blockedByShield);
            ABY_SoundUtility.PlayAt(ImpactSoundDefName, impactPosition.ToIntVec3(), impactMap);
        }
    }
}
