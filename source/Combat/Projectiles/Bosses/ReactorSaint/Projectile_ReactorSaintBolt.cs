using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ReactorSaintBolt : Bullet
    {
        private const int TrailIntervalTicks = 3;
        private const float SplashRadius = 1.12f;
        private const int SplashDamage = 7;
        private const float SplashArmorPenetration = 0.30f;
        private const int DirectStructureDamage = 128;
        private const float DirectStructureArmorPenetration = 3.1f;
        private const int SplashStructureDamage = 62;
        private const float SplashStructureArmorPenetration = 2.05f;

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private Vector3 lastDrawDirection = Vector3.forward;
        private bool lastPositionInitialized;
        private Material cachedHaloMaterial;
        private Material cachedCoreMaterial;
        private Material cachedNeedleMaterial;

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
                ABY_ReactorSaintProjectileVfxUtility.SpawnLanceTrail(
                    lastExactPosition,
                    currentPosition,
                    Map,
                    ticksAlive,
                    ABY_ReactorSaintProjectileVfxUtility.ResolvePhaseFactor(Launcher));
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
            float pulse = 0.92f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.46f)) * 0.16f;
            float hotPulse = 0.84f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.71f + 1.2f)) * 0.24f;
            float jitter = Mathf.Sin(ticksAlive * 0.37f) * 0.018f * phaseFactor;

            DrawPlane(drawPos - direction * 0.20f, angle, new Vector3(0.92f * pulse * phaseFactor, 1f, 4.35f * phaseFactor), HaloMaterial);
            DrawPlane(drawPos + direction * 0.08f, angle + jitter * 80f, new Vector3(0.42f * hotPulse * phaseFactor, 1f, 3.82f * phaseFactor), CoreMaterial);
            DrawPlane(drawPos + direction * 0.28f, angle, new Vector3(0.18f * phaseFactor, 1f, 4.86f * phaseFactor), NeedleMaterial);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;
            float phaseFactor = ABY_ReactorSaintProjectileVfxUtility.ResolvePhaseFactor(Launcher);

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, hitThing, "Projectile_ReactorSaintBolt", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null || !impactCell.IsValid)
            {
                return;
            }

            ABY_ReactorSaintProjectileVfxUtility.SpawnLanceImpact(impactPosition, impactCell, impactMap, hitThing, blockedByShield, phaseFactor);
            if (blockedByShield)
            {
                return;
            }

            bool directPawnHit = hitThing is Pawn;
            ApplyStructureImpactBonus(hitThing, impactCell, impactMap, instigator, phaseFactor);
            ABY_ProjectileImpactSafetyUtility.TryRunPostImpactAction(this, "Projectile_ReactorSaintBolt", "explosion", () =>
            {
                GenExplosion.DoExplosion(
                                impactCell,
                                impactMap,
                                SplashRadius * Mathf.Lerp(1f, 1.16f, phaseFactor - 1f),
                                DamageDefOf.Burn,
                                instigator,
                                Mathf.RoundToInt(SplashDamage * phaseFactor),
                                SplashArmorPenetration * phaseFactor,
                                doVisualEffects: !directPawnHit,
                                screenShakeFactor: directPawnHit ? 0f : 1f);
            });
        }

        private static void ApplyStructureImpactBonus(Thing hitThing, IntVec3 impactCell, Map map, Thing instigator, float phaseFactor)
        {
            Building directBuilding = hitThing as Building;
            if (IsValidStructureTarget(directBuilding))
            {
                ABY_ProjectileImpactSafetyUtility.TryApplyDamage(map, directBuilding, new DamageInfo(
                    DamageDefOf.Bomb,
                    Mathf.RoundToInt(DirectStructureDamage * phaseFactor),
                    DirectStructureArmorPenetration * phaseFactor,
                    -1f,
                    instigator,
                    null,
                    null,
                    DamageInfo.SourceCategory.ThingOrUnknown), "Projectile_ReactorSaintBolt");
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(impactCell, SplashRadius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Building building = things[i] as Building;
                    if (!IsValidStructureTarget(building) || building == directBuilding)
                    {
                        continue;
                    }

                    ABY_ProjectileImpactSafetyUtility.TryApplyDamage(map, building, new DamageInfo(
                        DamageDefOf.Bomb,
                        Mathf.RoundToInt(SplashStructureDamage * phaseFactor),
                        SplashStructureArmorPenetration * phaseFactor,
                        -1f,
                        instigator,
                        null,
                        null,
                        DamageInfo.SourceCategory.ThingOrUnknown), "Projectile_ReactorSaintBolt");
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
                    cachedHaloMaterial = MaterialPool.MatFrom(ABY_ReactorSaintProjectileVfxUtility.LanceHaloTexturePath, ShaderDatabase.MoteGlow);
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
                    cachedCoreMaterial = MaterialPool.MatFrom(ABY_ReactorSaintProjectileVfxUtility.LanceCoreTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedCoreMaterial;
            }
        }

        private Material NeedleMaterial
        {
            get
            {
                if (cachedNeedleMaterial == null)
                {
                    cachedNeedleMaterial = MaterialPool.MatFrom(ABY_ReactorSaintProjectileVfxUtility.LanceNeedleTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedNeedleMaterial;
            }
        }
    }
}
