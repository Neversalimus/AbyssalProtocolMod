using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_CrownshardStormSeed : Projectile
    {
        private const int TrailIntervalTicks = 2;

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
                CrownshardStormVfxUtility.SpawnSeedTrail(lastExactPosition, ExactPosition, Map);
            }

            lastExactPosition = ExactPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactDrawPos = ExactPosition;
            Thing sourceLauncher = launcher;
            ThingDef sourceWeaponDef = equipmentDef;

            CrownshardStormVfxUtility.SpawnSeedImpact(impactMap, impactDrawPos, blockedByShield);

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null || !impactCell.InBounds(impactMap))
            {
                return;
            }

            Thing_CrownshardStormNode.SpawnStorm(impactCell, impactMap, sourceLauncher, sourceWeaponDef, blockedByShield);
        }
    }
}
