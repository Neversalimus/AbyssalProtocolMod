using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_TurretSanctifiedPrismBolt : Bullet
    {
        private const float RefractionLength = 8.5f;
        private const float RefractionHalfWidth = 0.58f;
        private const int MaxSecondaryTargets = 4;
        private const float SecondaryArmorPenetrationBase = 0.44f;

        private static readonly float[] SecondaryDamageByIndex = { 9.0f, 7.0f, 5.5f, 4.0f };

        private int ticksAlive;
        private Vector3 launchPosition;
        private Vector3 lastExactPosition;
        private bool launchPositionInitialized;
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
            if (!launchPositionInitialized)
            {
                launchPosition = previousPosition;
                lastExactPosition = previousPosition;
                launchPositionInitialized = true;
            }

            if (!muzzleSpawned)
            {
                muzzleSpawned = true;
                SanctifiedPrismEmitterVfxUtility.SpawnMuzzle(previousPosition, destination, Map);
            }

            if (ticksAlive % 4 == 0)
            {
                FleckMaker.ThrowLightningGlow(ExactPosition, Map, 0.12f);
            }

            lastExactPosition = ExactPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;
            Thing primaryTarget = ResolvePrimaryTarget(hitThing);
            Vector3 direction = ResolveFlightDirection(impactPosition);

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            SanctifiedPrismEmitterVfxUtility.SpawnPrimaryImpact(impactPosition, direction, impactMap, blockedByShield);
            if (blockedByShield)
            {
                return;
            }

            SanctifiedPrismEmitterVfxUtility.SpawnResidualScorch(impactPosition, direction, impactMap);
            ApplyRefractionPayload(impactPosition, direction, impactMap, instigator, primaryTarget);
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
                if (IsValidRefractionTarget(things[i], Launcher?.Faction, null, Map))
                {
                    return things[i];
                }
            }

            return null;
        }

        private Vector3 ResolveFlightDirection(Vector3 impactPosition)
        {
            Vector3 direction = Vector3.zero;
            if (launchPositionInitialized)
            {
                direction = impactPosition - launchPosition;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = destination - origin;
                direction.y = 0f;
            }
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = impactPosition - lastExactPosition;
                direction.y = 0f;
            }
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            return direction.normalized;
        }

        private static void ApplyRefractionPayload(Vector3 impactPosition, Vector3 direction, Map map, Thing instigator, Thing primaryTarget)
        {
            if (map == null)
            {
                return;
            }

            List<Pawn> targets = FindRefractionTargets(impactPosition, direction, map, instigator?.Faction, primaryTarget);
            DamageDef damageDef = DefDatabase<DamageDef>.GetNamedSilentFail("Cut") ?? DamageDefOf.Bullet;
            Vector3 beamSource = primaryTarget != null && !primaryTarget.Destroyed ? primaryTarget.DrawPos : impactPosition;
            if ((beamSource - impactPosition).MagnitudeHorizontalSquared() > 0.20f)
            {
                SanctifiedPrismEmitterVfxUtility.SpawnRefractionBeam(impactPosition, beamSource, map, faint: false);
            }

            for (int i = 0; i < targets.Count; i++)
            {
                Pawn pawn = targets[i];
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    continue;
                }

                float damage = SecondaryDamageByIndex[Mathf.Min(i, SecondaryDamageByIndex.Length - 1)];
                float armorPenetration = Mathf.Max(0.16f, SecondaryArmorPenetrationBase - i * 0.07f);
                if (pawn.RaceProps != null && (pawn.RaceProps.IsMechanoid || pawn.BodySize >= 2.0f))
                {
                    damage *= 0.72f;
                    armorPenetration *= 0.82f;
                }

                Vector3 targetPos = pawn.DrawPos;
                SanctifiedPrismEmitterVfxUtility.SpawnRefractionBeam(beamSource, targetPos, map, faint: false);
                SanctifiedPrismEmitterVfxUtility.SpawnSecondaryHit(targetPos, map, i == 0 ? 1.05f : 0.90f);

                DamageInfo info = new DamageInfo(
                    damageDef,
                    Mathf.Max(1f, damage),
                    armorPenetration,
                    -1f,
                    instigator,
                    null,
                    null,
                    DamageInfo.SourceCategory.ThingOrUnknown);
                pawn.TakeDamage(info);
                beamSource = targetPos;
            }

            if (targets.Count == 0)
            {
                Vector3 end = impactPosition + direction * 3.25f;
                if (end.ToIntVec3().InBounds(map) && HasLineOfSight(impactPosition.ToIntVec3(), end.ToIntVec3(), map))
                {
                    SanctifiedPrismEmitterVfxUtility.SpawnRefractionBeam(impactPosition, end, map, faint: true);
                }
            }
        }

        private static List<Pawn> FindRefractionTargets(Vector3 impactPosition, Vector3 direction, Map map, Faction launcherFaction, Thing primaryTarget)
        {
            List<Pawn> result = new List<Pawn>(MaxSecondaryTargets);
            if (map?.mapPawns == null)
            {
                return result;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return result;
            }
            direction.Normalize();

            IntVec3 impactCell = impactPosition.ToIntVec3();
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            IEnumerable<Pawn> ordered = pawns
                .Where(pawn => IsValidRefractionTarget(pawn, launcherFaction, primaryTarget, map))
                .Select(pawn => new RefractionCandidate(pawn, ProjectAlongLine(impactPosition, direction, pawn.DrawPos)))
                .Where(candidate => candidate.DistanceAlong > 0.35f && candidate.DistanceAlong <= RefractionLength)
                .Where(candidate => candidate.PerpendicularDistance <= RefractionHalfWidth + Mathf.Min(0.22f, candidate.Pawn.BodySize * 0.08f))
                .Where(candidate => HasLineOfSight(impactCell, candidate.Pawn.Position, map))
                .OrderBy(candidate => candidate.DistanceAlong)
                .ThenBy(candidate => candidate.PerpendicularDistance)
                .Select(candidate => candidate.Pawn);

            foreach (Pawn pawn in ordered)
            {
                result.Add(pawn);
                if (result.Count >= MaxSecondaryTargets)
                {
                    break;
                }
            }

            return result;
        }

        private static RefractionProjection ProjectAlongLine(Vector3 originPosition, Vector3 direction, Vector3 targetPosition)
        {
            Vector3 delta = targetPosition - originPosition;
            delta.y = 0f;
            float along = Vector3.Dot(delta, direction);
            Vector3 closest = direction * along;
            float perpendicular = (delta - closest).magnitude;
            return new RefractionProjection(along, perpendicular);
        }

        private static bool IsValidRefractionTarget(Thing thing, Faction launcherFaction, Thing excluded, Map map)
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

            if (launcherFaction == null || pawn.Faction == null || !pawn.Faction.HostileTo(launcherFaction))
            {
                return false;
            }

            return true;
        }

        private static bool HasLineOfSight(IntVec3 from, IntVec3 to, Map map)
        {
            if (map == null || !from.IsValid || !to.IsValid || !from.InBounds(map) || !to.InBounds(map))
            {
                return false;
            }

            return from == to || GenSight.LineOfSight(from, to, map, true);
        }

        private readonly struct RefractionProjection
        {
            public RefractionProjection(float distanceAlong, float perpendicularDistance)
            {
                DistanceAlong = distanceAlong;
                PerpendicularDistance = perpendicularDistance;
            }

            public float DistanceAlong { get; }
            public float PerpendicularDistance { get; }
        }

        private readonly struct RefractionCandidate
        {
            public RefractionCandidate(Pawn pawn, RefractionProjection projection)
            {
                Pawn = pawn;
                DistanceAlong = projection.DistanceAlong;
                PerpendicularDistance = projection.PerpendicularDistance;
            }

            public Pawn Pawn { get; }
            public float DistanceAlong { get; }
            public float PerpendicularDistance { get; }
        }
    }
}
