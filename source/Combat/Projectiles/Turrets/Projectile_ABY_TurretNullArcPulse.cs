using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_TurretNullArcPulse : Bullet
    {
        private const float ChainRadius = 6.25f;
        private const int MaxChainTargets = 3;
        private const float PrimaryEmpAmount = 30f;
        private const float ChainEmpAmount = 18f;
        private const float PrimaryThermalDamage = 8f;
        private const float ChainThermalDamage = 4.5f;
        private const float MechDamageMultiplier = 1.65f;
        private const float FleshDamageMultiplier = 0.45f;

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
                NullArcDischargerVfxUtility.SpawnMuzzle(previousPosition, destination, Map);
                NullArcDischargerVfxUtility.SpawnBeam(previousPosition, destination, Map, chained: false);
            }

            if (ticksAlive % 3 == 0)
            {
                FleckMaker.ThrowLightningGlow(currentPosition, Map, 0.13f);
            }

            lastExactPosition = currentPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;
            Thing primaryTarget = ResolvePrimaryTarget(hitThing);

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, hitThing, "Projectile_ABY_TurretNullArcPulse", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null)
            {
                return;
            }

            NullArcDischargerVfxUtility.SpawnImpact(impactPosition, impactMap, blockedByShield, chained: false);

            if (primaryTarget != null && !primaryTarget.Destroyed)
            {
                ApplyNullArcPayload(primaryTarget, impactMap, instigator, PrimaryEmpAmount, PrimaryThermalDamage, primary: true);
            }

            ChainFromImpact(impactPosition, primaryTarget, impactMap, instigator);
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
                if (IsValidChainTarget(things[i], Launcher?.Faction, null, Map))
                {
                    return things[i];
                }
            }

            return null;
        }

        private static void ChainFromImpact(Vector3 impactPosition, Thing primaryTarget, Map map, Thing instigator)
        {
            if (map == null)
            {
                return;
            }

            Faction launcherFaction = instigator?.Faction;
            List<Thing> chainTargets = FindChainTargets(impactPosition.ToIntVec3(), map, launcherFaction, primaryTarget);
            Vector3 source = primaryTarget != null && !primaryTarget.Destroyed ? primaryTarget.DrawPos : impactPosition;

            for (int i = 0; i < chainTargets.Count; i++)
            {
                Thing target = chainTargets[i];
                if (target == null || target.Destroyed)
                {
                    continue;
                }

                Vector3 targetPos = target.DrawPos;
                float falloff = Mathf.Clamp01(1f - i * 0.23f);
                NullArcDischargerVfxUtility.SpawnBeam(source, targetPos, map, chained: true);
                NullArcDischargerVfxUtility.SpawnImpact(targetPos, map, blockedByShield: false, chained: true);
                ApplyNullArcPayload(target, map, instigator, ChainEmpAmount * falloff, ChainThermalDamage * falloff, primary: false);
                source = targetPos;
            }
        }

        private static List<Thing> FindChainTargets(IntVec3 center, Map map, Faction launcherFaction, Thing primaryTarget)
        {
            List<Thing> result = new List<Thing>(MaxChainTargets);
            if (map?.mapPawns == null || !center.IsValid)
            {
                return result;
            }

            float radiusSquared = ChainRadius * ChainRadius;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            IEnumerable<Pawn> ordered = pawns
                .Where(pawn => IsValidChainTarget(pawn, launcherFaction, primaryTarget, map))
                .Where(pawn => pawn.Position.DistanceToSquared(center) <= radiusSquared)
                .OrderByDescending(ChainTargetPriority)
                .ThenBy(pawn => pawn.Position.DistanceToSquared(center));

            foreach (Pawn pawn in ordered)
            {
                result.Add(pawn);
                if (result.Count >= MaxChainTargets)
                {
                    break;
                }
            }

            return result;
        }

        private static float ChainTargetPriority(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0f;
            }

            float score = 0f;
            if (pawn.RaceProps != null && pawn.RaceProps.IsMechanoid)
            {
                score += 1000f;
            }
            if (HasActiveShield(pawn))
            {
                score += 650f;
            }
            if (pawn.RaceProps != null && !pawn.RaceProps.Humanlike)
            {
                score += 80f;
            }
            return score;
        }

        private static bool IsValidChainTarget(Thing thing, Faction launcherFaction, Thing excluded, Map map)
        {
            if (thing == null || thing == excluded || thing.Destroyed || !thing.Spawned || thing.Map != map)
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

        private static void ApplyNullArcPayload(Thing target, Map map, Thing instigator, float empAmount, float thermalAmount, bool primary)
        {
            if (target == null || target.Destroyed)
            {
                return;
            }

            DamageInfo empInfo = new DamageInfo(
                DamageDefOf.EMP,
                Mathf.Max(1f, empAmount),
                0f,
                -1f,
                instigator,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown);
            ABY_ProjectileImpactSafetyUtility.TryApplyDamage(map, target, empInfo, "Projectile_ABY_TurretNullArcPulse");

            Pawn pawn = target as Pawn;
            float adjustedThermal = thermalAmount;
            float armorPenetration = primary ? 0.32f : 0.18f;
            if (pawn != null)
            {
                if (pawn.RaceProps != null && pawn.RaceProps.IsMechanoid)
                {
                    adjustedThermal *= MechDamageMultiplier;
                    armorPenetration += primary ? 0.38f : 0.24f;
                }
                else
                {
                    adjustedThermal *= FleshDamageMultiplier;
                    armorPenetration = 0.08f;
                }
            }

            if (adjustedThermal <= 0.25f)
            {
                return;
            }

            DamageInfo burnInfo = new DamageInfo(
                DamageDefOf.Burn,
                adjustedThermal,
                armorPenetration,
                -1f,
                instigator,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown);
            ABY_ProjectileImpactSafetyUtility.TryApplyDamage(map, target, burnInfo, "Projectile_ABY_TurretNullArcPulse");
        }

        private static bool HasActiveShield(Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null)
            {
                return false;
            }

            List<Apparel> apparel = pawn.apparel.WornApparel;
            for (int i = 0; i < apparel.Count; i++)
            {
                CompShield shield = apparel[i]?.GetComp<CompShield>();
                if (shield != null && shield.ShieldState == ShieldState.Active)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
