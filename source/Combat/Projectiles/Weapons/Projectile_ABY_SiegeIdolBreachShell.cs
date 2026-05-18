using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_SiegeIdolBreachShell : Bullet
    {
        private const int TrailIntervalTicks = 3;
        private const float TrailGlowSize = 0.26f;
        private const float ImpactGlowSize = 2.15f;
        private const float ExplosionRadius = 1.7f;
        private const int ExplosionDamage = 14;
        private const float ExplosionArmorPenetration = 0.55f;
        private const int StructureDamagePerShell = 96;
        private const float StructureArmorPenetration = 2.10f;
        private const int DoorBonusDamage = 36;
        private const int TurretBonusDamage = 48;

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

            ApplyStructureBlastBonus(impactCell, impactMap, instigator);
            GenExplosion.DoExplosion(impactCell, impactMap, ExplosionRadius, DamageDefOf.Burn, instigator, ExplosionDamage, ExplosionArmorPenetration);
        }

        private static void ApplyStructureBlastBonus(IntVec3 impactCell, Map map, Thing instigator)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(impactCell, ExplosionRadius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                var things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Building building = things[i] as Building;
                    if (!IsValidStructureTarget(building))
                    {
                        continue;
                    }

                    int damage = StructureDamagePerShell;
                    if (building is Building_Door)
                    {
                        damage += DoorBonusDamage;
                    }
                    else if (building is Building_Turret)
                    {
                        damage += TurretBonusDamage;
                    }

                    building.TakeDamage(new DamageInfo(
                        DamageDefOf.Bomb,
                        damage,
                        StructureArmorPenetration,
                        -1f,
                        instigator,
                        null,
                        null,
                        DamageInfo.SourceCategory.ThingOrUnknown));
                }
            }
        }

        private static bool IsValidStructureTarget(Building building)
        {
            return building != null
                && building.Spawned
                && !building.Destroyed
                && building.def != null
                && building.def.useHitPoints;
        }
    }
}
