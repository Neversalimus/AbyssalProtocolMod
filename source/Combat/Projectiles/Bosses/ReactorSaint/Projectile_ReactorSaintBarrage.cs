using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ReactorSaintBarrage : Bullet
    {
        private const int TrailIntervalTicks = 4;
        private const float ExplosionRadius = 2.05f;
        private const int ExplosionDamage = 18;
        private const float ExplosionArmorPenetration = 0.48f;
        private const int StructureDamagePerShell = 78;
        private const float StructureArmorPenetration = 1.85f;

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private Vector3 lastDrawDirection = Vector3.forward;
        private bool lastPositionInitialized;
        private bool warningSpawned;
        private Material cachedHaloMaterial;
        private Material cachedCoreMaterial;

        protected override void Tick()
        {
            Vector3 previousPosition = ExactPosition;
            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksAlive++;
            float phaseFactor = ABY_ReactorSaintProjectileVfxUtility.ResolvePhaseFactor(Launcher);

            if (!warningSpawned)
            {
                warningSpawned = true;
                IntVec3 targetCell = destination.ToIntVec3();
                ABY_ReactorSaintProjectileVfxUtility.SpawnBarrageWarning(targetCell, Map, phaseFactor);
            }

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
                ABY_ReactorSaintProjectileVfxUtility.SpawnBarrageTrail(lastExactPosition, currentPosition, Map, ticksAlive, phaseFactor);
            }


            lastExactPosition = currentPosition;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = drawLoc;
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);

            Vector3 direction = lastDrawDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }
            direction.Normalize();

            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float phaseFactor = ABY_ReactorSaintProjectileVfxUtility.ResolvePhaseFactor(Launcher);
            float pulse = 0.92f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.54f)) * 0.18f;
            float roll = ticksAlive * (8.0f + phaseFactor * 2.2f);

            DrawPlane(drawPos, angle + roll * 0.10f, new Vector3(1.22f * pulse * phaseFactor, 1f, 2.05f * phaseFactor), HaloMaterial);
            DrawPlane(drawPos + direction * 0.02f, angle, new Vector3(0.62f * phaseFactor, 1f, 1.48f * pulse * phaseFactor), CoreMaterial);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;
            float phaseFactor = ABY_ReactorSaintProjectileVfxUtility.ResolvePhaseFactor(Launcher);

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, "Projectile_ReactorSaintBarrage", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null || !impactCell.IsValid)
            {
                return;
            }

            ABY_ReactorSaintProjectileVfxUtility.SpawnBarrageImpact(impactPosition, impactCell, impactMap, hitThing, phaseFactor);
            ABY_SoundUtility.PlayAt("ABY_ReactorSaintBarrageImpact", impactCell, impactMap);

            if (blockedByShield)
            {
                return;
            }

            bool directPawnHit = hitThing is Pawn;
            ApplyStructureBlastBonus(impactCell, impactMap, instigator, phaseFactor);
            ABY_ProjectileImpactSafetyUtility.TryRunPostImpactAction(this, "Projectile_ReactorSaintBarrage", "explosion", () =>
            {
                GenExplosion.DoExplosion(
                                impactCell,
                                impactMap,
                                ExplosionRadius * Mathf.Lerp(1f, 1.18f, phaseFactor - 1f),
                                DamageDefOf.Burn,
                                instigator,
                                Mathf.RoundToInt(ExplosionDamage * phaseFactor),
                                ExplosionArmorPenetration * phaseFactor,
                                doVisualEffects: !directPawnHit,
                                screenShakeFactor: directPawnHit ? 0f : 1f);
            });
        }

        private static void ApplyStructureBlastBonus(IntVec3 impactCell, Map map, Thing instigator, float phaseFactor)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(impactCell, ExplosionRadius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Building building = things[i] as Building;
                    if (!IsValidStructureTarget(building))
                    {
                        continue;
                    }

                    ABY_ProjectileImpactSafetyUtility.TryApplyDamage(building, new DamageInfo(
                        DamageDefOf.Bomb,
                        Mathf.RoundToInt(StructureDamagePerShell * phaseFactor),
                        StructureArmorPenetration * phaseFactor,
                        -1f,
                        instigator,
                        null,
                        null,
                        DamageInfo.SourceCategory.ThingOrUnknown), "Projectile_ReactorSaintBarrage");
                }
            }
        }

        private static bool IsValidStructureTarget(Building building)
        {
            return building != null
                && building.Spawned
                && !building.Destroyed
                && building.def != null
                && building.def.useHitPoints
                && building.def.destroyable
                && !AbyssalThreatPawnUtility.ShouldIgnoreAsHostileBuildingTarget(building);
        }

        private void DrawPlane(Vector3 center, float angle, Vector3 scale, Material material)
        {
            if (material == null)
            {
                return;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private Material HaloMaterial
        {
            get
            {
                if (cachedHaloMaterial == null)
                {
                    cachedHaloMaterial = MaterialPool.MatFrom(ABY_ReactorSaintProjectileVfxUtility.BarrageHaloTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedHaloMaterial;
            }
        }

        private Material CoreMaterial
        {
            get
            {
                if (cachedCoreMaterial == null)
                {
                    cachedCoreMaterial = MaterialPool.MatFrom(ABY_ReactorSaintProjectileVfxUtility.BarrageCoreTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedCoreMaterial;
            }
        }
    }
}
