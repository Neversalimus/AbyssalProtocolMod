using RimWorld;
using Verse;
using UnityEngine;

namespace AbyssalProtocol
{
    public class Projectile_RiftSapperSpike : Bullet
    {
        private const int TrailIntervalTicks = 4;
        private const float TrailGlowSize = 0.18f;
        private const float ImpactGlowSize = 1.45f;
        private const float ImpactHeatSize = 0.82f;
        private const float ExplosionRadius = 1.35f;
        private const int ExplosionDamage = 7;
        private const float ExplosionArmorPenetration = 0.22f;
        private const int StructureDamagePerSpike = 44;
        private const float StructureArmorPenetration = 1.35f;
        private const int DoorBonusDamage = 16;
        private const int TurretBonusDamage = 22;
        private const int CoverBonusDamage = 18;

        private int ticksAlive;

        protected override void Tick()
        {
            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksAlive++;
            if (ticksAlive % TrailIntervalTicks != 0)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(ExactPosition, Map, TrailGlowSize);
            if (Rand.Chance(0.40f))
            {
                FleckMaker.ThrowMicroSparks(ExactPosition, Map);
            }
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
            FleckMaker.ThrowHeatGlow(impactCell, impactMap, ImpactHeatSize);
            FleckMaker.ThrowMicroSparks(impactPosition, impactMap);
            FleckMaker.ThrowMicroSparks(impactPosition, impactMap);
            ABY_SoundUtility.PlayAt("ABY_SigilRepeaterImpact", impactCell, impactMap);

            if (blockedByShield)
            {
                return;
            }

            ApplyStructureBlastBonus(impactCell, impactMap, instigator);
            GenExplosion.DoExplosion(impactCell, impactMap, ExplosionRadius, DamageDefOf.Bomb, instigator, ExplosionDamage, ExplosionArmorPenetration);
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
                    if (!IsValidStructureTarget(building, instigator))
                    {
                        continue;
                    }

                    int damage = StructureDamagePerSpike;
                    if (building is Building_Door)
                    {
                        damage += DoorBonusDamage;
                    }
                    else if (building is Building_Turret)
                    {
                        damage += TurretBonusDamage;
                    }

                    if (IsCoverLike(building))
                    {
                        damage += CoverBonusDamage;
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

        private static bool IsValidStructureTarget(Building building, Thing instigator)
        {
            if (building == null
                || !building.Spawned
                || building.Destroyed
                || building.def == null
                || !building.def.useHitPoints)
            {
                return false;
            }

            if (instigator?.Faction == null || building.Faction == null)
            {
                return false;
            }

            return instigator.Faction.HostileTo(building.Faction);
        }

        private static bool IsCoverLike(Building building)
        {
            string defName = building?.def?.defName;
            if (defName.NullOrEmpty())
            {
                return false;
            }

            return defName.IndexOf("Sandbag", System.StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Barricade", System.StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Embrasure", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
