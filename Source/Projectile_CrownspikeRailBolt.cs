using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_CrownspikeRailBolt : Bullet
    {
        private const int MaxPierceTargets = 2;
        private const float PierceReachBeyondImpact = 13.5f;
        private const float PierceSampleStep = 0.42f;
        private const float PierceLineRadius = 0.54f;
        private const int BasePierceDamage = 62;
        private const float BasePierceArmorPenetration = 1.45f;
        private const float FirstPierceDamageMultiplier = 0.58f;
        private const float SecondPierceDamageMultiplier = 0.38f;
        private const float PierceArmorPenetrationMultiplier = 0.82f;
        private const float DenseDirectEmpDamage = 6.0f;
        private const float DensePierceEmpDamage = 3.5f;
        private const float DenseStructurePulseDamage = 8.0f;

        private bool preImpactFlashDone;

        protected override void Tick()
        {
            if (!preImpactFlashDone && Spawned && Map != null)
            {
                preImpactFlashDone = true;
                Vector3 source = ResolveSourcePosition();
                Vector3 forwardPoint = ExactPosition;
                if ((forwardPoint - source).MagnitudeHorizontal() > 0.15f)
                {
                    CrownspikeRailVfxUtility.SpawnRailDischarge(source, forwardPoint, Map, null, false, false);
                }
            }

            base.Tick();
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;
            Vector3 source = ResolveSourcePosition();
            Vector3 finalDestination = ResolveFinalDestination(source, impactPosition);
            Thing instigator = Launcher;
            bool directTargetWasDense = CrownspikeRailVfxUtility.IsDenseTarget(hitThing);
            Pawn directPawn = hitThing as Pawn;
            bool directPawnWasAlive = directPawn != null && !directPawn.Dead && !directPawn.Destroyed;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            CrownspikeRailVfxUtility.SpawnRailDischarge(source, impactPosition, impactMap, hitThing, blockedByShield, false);

            if (blockedByShield)
            {
                CrownspikeRailVfxUtility.SpawnShieldReaction(impactPosition, impactMap);
                return;
            }

            if (directTargetWasDense)
            {
                ApplyDenseResonance(hitThing, instigator, impactPosition, impactMap, false);
            }

            if (directPawnWasAlive && directPawn != null && (directPawn.Dead || directPawn.Destroyed))
            {
                CrownspikeRailVfxUtility.SpawnExecutionFlare(impactPosition, impactMap);
            }

            ApplyPierceLine(source, impactPosition, finalDestination, impactMap, hitThing, instigator, impactCell);
        }

        private void ApplyPierceLine(Vector3 source, Vector3 impactPosition, Vector3 finalDestination, Map map, Thing primaryHitThing, Thing instigator, IntVec3 impactCell)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = finalDestination - source;
            direction.y = 0f;
            if (direction.MagnitudeHorizontalSquared() < 0.001f)
            {
                direction = impactPosition - source;
                direction.y = 0f;
            }
            if (direction.MagnitudeHorizontalSquared() < 0.001f)
            {
                return;
            }

            direction.Normalize();

            float startDistance = Mathf.Max(0f, DistanceAlongLine(source, direction, impactPosition) + 0.38f);
            float endDistance = startDistance + PierceReachBeyondImpact;

            List<PierceCandidate> candidates = new List<PierceCandidate>();
            HashSet<int> seenThingIds = new HashSet<int>();
            HashSet<IntVec3> sampledCells = new HashSet<IntVec3>();

            for (float distance = startDistance; distance <= endDistance; distance += PierceSampleStep)
            {
                Vector3 sample = source + direction * distance;
                IntVec3 centerCell = sample.ToIntVec3();
                for (int ox = -1; ox <= 1; ox++)
                {
                    for (int oz = -1; oz <= 1; oz++)
                    {
                        IntVec3 cell = new IntVec3(centerCell.x + ox, centerCell.y, centerCell.z + oz);
                        if (!cell.InBounds(map) || sampledCells.Contains(cell))
                        {
                            continue;
                        }

                        sampledCells.Add(cell);
                        List<Thing> things = cell.GetThingList(map);
                        for (int i = 0; i < things.Count; i++)
                        {
                            Thing thing = things[i];
                            if (!IsValidPierceTarget(thing, primaryHitThing, instigator, seenThingIds))
                            {
                                continue;
                            }

                            Vector3 thingPosition = thing.DrawPos;
                            float along = DistanceAlongLine(source, direction, thingPosition);
                            if (along < startDistance - 0.15f || along > endDistance + 0.15f)
                            {
                                continue;
                            }

                            float lateral = LateralDistanceToLine(source, direction, thingPosition);
                            if (lateral > PierceLineRadius + thing.def.fillPercent * 0.38f)
                            {
                                continue;
                            }

                            seenThingIds.Add(thing.thingIDNumber);
                            candidates.Add(new PierceCandidate(thing, along, lateral));
                        }
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            candidates.Sort((a, b) =>
            {
                int byDistance = a.distance.CompareTo(b.distance);
                return byDistance != 0 ? byDistance : a.lateralDistance.CompareTo(b.lateralDistance);
            });

            int affected = 0;
            for (int i = 0; i < candidates.Count && affected < MaxPierceTargets; i++)
            {
                Thing target = candidates[i].thing;
                if (target == null || target.Destroyed)
                {
                    continue;
                }

                float damageMultiplier = affected == 0 ? FirstPierceDamageMultiplier : SecondPierceDamageMultiplier;
                ApplyPierceDamage(target, instigator, damageMultiplier);

                Vector3 targetPos = target.DrawPos;
                CrownspikeRailVfxUtility.SpawnPierceImpact(targetPos, map, target, affected);

                if (CrownspikeRailVfxUtility.IsDenseTarget(target))
                {
                    ApplyDenseResonance(target, instigator, targetPos, map, true);
                }

                affected++;
            }
        }

        private void ApplyPierceDamage(Thing target, Thing instigator, float damageMultiplier)
        {
            if (target == null || target.Destroyed)
            {
                return;
            }

            int damage = Mathf.Max(1, Mathf.RoundToInt(BasePierceDamage * damageMultiplier));
            float armorPenetration = BasePierceArmorPenetration * PierceArmorPenetrationMultiplier;

            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Bullet,
                damage,
                armorPenetration,
                -1f,
                instigator,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown);

            target.TakeDamage(damageInfo);
        }

        private static void ApplyDenseResonance(Thing target, Thing instigator, Vector3 position, Map map, bool fromPierce)
        {
            if (target == null || target.Destroyed || map == null)
            {
                return;
            }

            bool dense = CrownspikeRailVfxUtility.IsDenseTarget(target);
            if (!dense)
            {
                return;
            }

            float empDamage = fromPierce ? DensePierceEmpDamage : DenseDirectEmpDamage;
            target.TakeDamage(new DamageInfo(
                DamageDefOf.EMP,
                empDamage,
                0f,
                -1f,
                instigator,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown));

            Building building = target as Building;
            if (building != null && building.def != null && building.def.useHitPoints)
            {
                building.TakeDamage(new DamageInfo(
                    DamageDefOf.Bomb,
                    fromPierce ? DenseStructurePulseDamage * 0.55f : DenseStructurePulseDamage,
                    0.35f,
                    -1f,
                    instigator,
                    null,
                    null,
                    DamageInfo.SourceCategory.ThingOrUnknown));
            }

            CrownspikeRailVfxUtility.SpawnDenseResonance(position, map, fromPierce);
        }

        private static bool IsValidPierceTarget(Thing thing, Thing primaryHitThing, Thing instigator, HashSet<int> seenThingIds)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned || thing == primaryHitThing || thing == instigator)
            {
                return false;
            }

            if (seenThingIds.Contains(thing.thingIDNumber))
            {
                return false;
            }

            if (thing is Mote || thing is Filth || thing is Blueprint || thing is Frame)
            {
                return false;
            }

            Faction instigatorFaction = instigator != null ? instigator.Faction : null;

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                if (pawn.Dead || pawn.Downed && pawn.RaceProps != null && pawn.RaceProps.Animal)
                {
                    return false;
                }

                if (instigatorFaction != null && pawn.Faction == instigatorFaction)
                {
                    return false;
                }

                return true;
            }

            Building building = thing as Building;
            if (building != null)
            {
                if (building.def == null || !building.def.useHitPoints)
                {
                    return false;
                }

                if (instigatorFaction != null && building.Faction == instigatorFaction)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private Vector3 ResolveSourcePosition()
        {
            Thing launcher = Launcher;
            if (launcher != null && !launcher.Destroyed)
            {
                return launcher.DrawPos;
            }

            return ExactPosition;
        }

        private Vector3 ResolveFinalDestination(Vector3 source, Vector3 impactPosition)
        {
            Vector3 direction = impactPosition - source;
            direction.y = 0f;
            if (direction.MagnitudeHorizontalSquared() < 0.001f)
            {
                return impactPosition;
            }

            direction.Normalize();
            return impactPosition + direction * PierceReachBeyondImpact;
        }

        private static float DistanceAlongLine(Vector3 origin, Vector3 normalizedDirection, Vector3 point)
        {
            Vector3 delta = point - origin;
            delta.y = 0f;
            return Vector3.Dot(delta, normalizedDirection);
        }

        private static float LateralDistanceToLine(Vector3 origin, Vector3 normalizedDirection, Vector3 point)
        {
            Vector3 delta = point - origin;
            delta.y = 0f;
            float along = Vector3.Dot(delta, normalizedDirection);
            Vector3 projected = origin + normalizedDirection * along;
            return (point - projected).MagnitudeHorizontal();
        }

        private struct PierceCandidate
        {
            public readonly Thing thing;
            public readonly float distance;
            public readonly float lateralDistance;

            public PierceCandidate(Thing thing, float distance, float lateralDistance)
            {
                this.thing = thing;
                this.distance = distance;
                this.lateralDistance = lateralDistance;
            }
        }
    }
}
