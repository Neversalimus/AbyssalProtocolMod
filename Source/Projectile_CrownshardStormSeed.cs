using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_CrownshardStormSeed : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactDrawPos = DrawPos;
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
