using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_CrownfireCarrierRocket : Bullet
    {
        private const int SplitTicks = 18;
        private const int MicroRocketCount = 8;
        private const float TargetSearchRadius = 15f;
        private const float FallbackScatterRadius = 4.2f;
        private const string MicroRocketDefName = "ABY_CrownfireMicroRocket";

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private bool lastPositionInitialized;
        private bool launchVfxSpawned;
        private bool splitTriggered;

        private static ThingDef microRocketDef;
        private static ThingDef MicroRocketDef => microRocketDef ?? (microRocketDef = DefDatabase<ThingDef>.GetNamedSilentFail(MicroRocketDefName));

        protected override void Tick()
        {
            Vector3 previousPosition = ExactPosition;

            if (!Spawned || Map == null)
            {
                base.Tick();
                return;
            }

            if (!launchVfxSpawned)
            {
                launchVfxSpawned = true;
                CrownfireRocketChoirVfxUtility.SpawnTubeIgnition(previousPosition, Map);
                CrownfireRocketChoirVfxUtility.SpawnLaunchExhaust(previousPosition, Map);
            }

            ticksAlive++;
            if (ticksAlive >= SplitTicks)
            {
                TriggerSplit(ExactPosition, blockedByShield: false);
                return;
            }

            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            if (!lastPositionInitialized)
            {
                lastExactPosition = previousPosition;
                lastPositionInitialized = true;
            }

            lastExactPosition = ExactPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Vector3 impactPosition = ExactPosition;
            TriggerSplit(impactPosition, blockedByShield);
        }

        private void TriggerSplit(Vector3 splitPosition, bool blockedByShield)
        {
            if (splitTriggered)
            {
                return;
            }

            splitTriggered = true;
            Map splitMap = Map;
            Thing instigator = Launcher;
                        Vector3 targetPosition = destination;

            if (splitMap != null)
            {
                CrownfireRocketChoirVfxUtility.SpawnSplitBurst(splitPosition, splitMap);
                if (!blockedByShield)
                {
                    LaunchMicroRockets(splitPosition, targetPosition, splitMap, instigator);
                }
            }

            if (!Destroyed)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        private static void LaunchMicroRockets(Vector3 splitPosition, Vector3 targetPosition, Map map, Thing instigator)
        {
            if (map == null || MicroRocketDef == null)
            {
                return;
            }

            List<LocalTargetInfo> targets = BuildMicroTargets(targetPosition.ToIntVec3(), splitPosition, map, instigator?.Faction);
            for (int i = 0; i < MicroRocketCount; i++)
            {
                LocalTargetInfo targetInfo = i < targets.Count ? targets[i] : new LocalTargetInfo(RandomFallbackCell(targetPosition.ToIntVec3(), map));
                if (!targetInfo.IsValid)
                {
                    continue;
                }

                Projectile projectile = GenSpawn.Spawn(MicroRocketDef, splitPosition.ToIntVec3(), map, WipeMode.Vanish) as Projectile;
                if (projectile == null)
                {
                    continue;
                }

                Vector3 origin = splitPosition;
                Vector2 radial = Rand.InsideUnitCircle.normalized * Rand.Range(0.04f, 0.18f);
                origin.x += radial.x;
                origin.z += radial.y;
                projectile.Launch(instigator, origin, targetInfo, targetInfo, ProjectileHitFlags.IntendedTarget, false, null, null);
            }
        }

        private static List<LocalTargetInfo> BuildMicroTargets(IntVec3 primaryCell, Vector3 splitPosition, Map map, Faction launcherFaction)
        {
            List<LocalTargetInfo> result = new List<LocalTargetInfo>(MicroRocketCount);
            if (map?.mapPawns != null)
            {
                float radiusSquared = TargetSearchRadius * TargetSearchRadius;
                IEnumerable<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                    .Where(pawn => IsValidMicroTarget(pawn, launcherFaction, map))
                    .Where(pawn => pawn.Position.DistanceToSquared(primaryCell) <= radiusSquared || pawn.Position.DistanceToSquared(splitPosition.ToIntVec3()) <= radiusSquared)
                    .OrderBy(pawn => pawn.Position.DistanceToSquared(primaryCell))
                    .ThenBy(pawn => pawn.Position.DistanceToSquared(splitPosition.ToIntVec3()));

                foreach (Pawn pawn in candidates)
                {
                    result.Add(new LocalTargetInfo(pawn));
                    if (result.Count >= MicroRocketCount)
                    {
                        break;
                    }
                }
            }

            while (result.Count < MicroRocketCount)
            {
                IntVec3 fallback = RandomFallbackCell(primaryCell, map);
                if (!fallback.IsValid)
                {
                    break;
                }

                result.Add(new LocalTargetInfo(fallback));
            }

            return result;
        }

        private static bool IsValidMicroTarget(Pawn pawn, Faction launcherFaction, Map map)
        {
            if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map != map || pawn.Dead || pawn.Downed)
            {
                return false;
            }

            if (launcherFaction == null || pawn.Faction == null || !pawn.Faction.HostileTo(launcherFaction))
            {
                return false;
            }

            return true;
        }

        private static IntVec3 RandomFallbackCell(IntVec3 primaryCell, Map map)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 offset = Rand.InsideUnitCircle * FallbackScatterRadius;
                IntVec3 cell = new IntVec3(primaryCell.x + Mathf.RoundToInt(offset.x), primaryCell.y, primaryCell.z + Mathf.RoundToInt(offset.y));
                if (cell.InBounds(map) && !cell.Fogged(map))
                {
                    return cell;
                }
            }

            return primaryCell.InBounds(map) ? primaryCell : IntVec3.Invalid;
        }
    }
}
