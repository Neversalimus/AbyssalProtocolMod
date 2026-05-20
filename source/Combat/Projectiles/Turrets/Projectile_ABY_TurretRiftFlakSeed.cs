using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_TurretRiftFlakSeed : Bullet
    {
        private const float BloomRadius = 2.65f;
        private const int MaxShardTargets = 8;
        private const int MaxVisualShardImpacts = 6;
        private const float ShardDamage = 8.5f;
        private const float ShardArmorPenetration = 0.24f;
        private const float PrimaryTargetShardBonus = 2.5f;

        private int ticksAlive;
        private Vector3 lastExactPosition;
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

            if (!muzzleSpawned)
            {
                muzzleSpawned = true;
                RiftFlakBloomVfxUtility.SpawnMuzzle(previousPosition, destination, Map);
            }

            if (ticksAlive % 8 == 0)
            {
                FleckMaker.ThrowLightningGlow(ExactPosition, Map, 0.10f);
            }

            lastExactPosition = ExactPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;
            Thing primaryTarget = ResolvePrimaryTarget(hitThing);

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, "Projectile_ABY_TurretRiftFlakSeed", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null)
            {
                return;
            }

            RiftFlakBloomVfxUtility.SpawnBloom(impactPosition, impactMap, blockedByShield);
            if (!blockedByShield)
            {
                ApplyRiftBloomPayload(impactPosition, impactMap, instigator, primaryTarget);
            }
        }

        private Thing ResolvePrimaryTarget(Thing hitThing)
        {
            if (hitThing != null && !hitThing.Destroyed)
            {
                return hitThing;
            }

            if (Map == null || !Position.IsValid || !Position.InBounds(Map))
            {
                return null;
            }

            List<Thing> things = Position.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                if (IsValidShardTarget(things[i], Launcher?.Faction, Map))
                {
                    return things[i];
                }
            }

            return null;
        }

        private static void ApplyRiftBloomPayload(Vector3 impactPosition, Map map, Thing instigator, Thing primaryTarget)
        {
            if (map == null)
            {
                return;
            }

            List<Pawn> targets = FindShardTargets(impactPosition.ToIntVec3(), map, instigator?.Faction, primaryTarget);
            DamageDef damageDef = DefDatabase<DamageDef>.GetNamedSilentFail("Cut") ?? DamageDefOf.Bullet;
            int visualImpacts = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                Pawn pawn = targets[i];
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    continue;
                }

                float damage = ShardDamage;
                if (pawn == primaryTarget)
                {
                    damage += PrimaryTargetShardBonus;
                }

                if (pawn.RaceProps != null && (pawn.RaceProps.IsMechanoid || pawn.BodySize >= 2.0f))
                {
                    damage *= 0.72f;
                }

                DamageInfo info = new DamageInfo(
                    damageDef,
                    Mathf.Max(1f, damage),
                    ShardArmorPenetration,
                    -1f,
                    instigator,
                    null,
                    null,
                    DamageInfo.SourceCategory.ThingOrUnknown);
                ABY_ProjectileImpactSafetyUtility.TryApplyDamage(pawn, info, "Projectile_ABY_TurretRiftFlakSeed");

                if (visualImpacts < MaxVisualShardImpacts)
                {
                    RiftFlakBloomVfxUtility.SpawnShardImpact(pawn.DrawPos, map, pawn == primaryTarget ? 1.08f : 0.92f);
                    visualImpacts++;
                }
            }

            while (visualImpacts < Mathf.Min(MaxVisualShardImpacts, 3))
            {
                Vector3 point = impactPosition;
                Vector2 offset = Rand.InsideUnitCircle * Rand.Range(0.35f, BloomRadius * 0.82f);
                point.x += offset.x;
                point.z += offset.y;
                if (point.ToIntVec3().InBounds(map))
                {
                    RiftFlakBloomVfxUtility.SpawnShardImpact(point, map, 0.82f);
                    visualImpacts++;
                }
                else
                {
                    break;
                }
            }
        }

        private static List<Pawn> FindShardTargets(IntVec3 center, Map map, Faction launcherFaction, Thing primaryTarget)
        {
            List<Pawn> result = new List<Pawn>(MaxShardTargets);
            if (map?.mapPawns == null || !center.IsValid)
            {
                return result;
            }

            float radiusSquared = BloomRadius * BloomRadius;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            IEnumerable<Pawn> ordered = pawns
                .Where(pawn => IsValidShardTarget(pawn, launcherFaction, map))
                .Where(pawn => pawn.Position.DistanceToSquared(center) <= radiusSquared)
                .OrderByDescending(pawn => pawn == primaryTarget ? 1 : 0)
                .ThenBy(pawn => pawn.Position.DistanceToSquared(center));

            foreach (Pawn pawn in ordered)
            {
                result.Add(pawn);
                if (result.Count >= MaxShardTargets)
                {
                    break;
                }
            }

            return result;
        }

        private static bool IsValidShardTarget(Thing thing, Faction launcherFaction, Map map)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned || thing.Map != map)
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            if (pawn == null || pawn.Dead || pawn.Downed)
            {
                return false;
            }

            if (launcherFaction == null || pawn.Faction == null || !ABY_FactionHostilityUtility.SafeHostileTo(pawn.Faction, launcherFaction))
            {
                return false;
            }

            return true;
        }
    }
}
