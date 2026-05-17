using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ABY_CrownfireCarrierRocket : Bullet
    {
        private const int SplitTicks = 15;
        private const int MicroRocketCount = 8;
        private const int VisualRiseTicks = 13;
        private const float TargetSearchRadius = 15f;
        private const float FallbackScatterRadius = 4.2f;
        private const float VisualRiseDistance = 1.58f;
        private const float VisualForwardStartOffset = 0.10f;
        private const string MicroRocketDefName = "ABY_CrownfireMicroRocket";
        private const string CarrierVisualTexturePath = "Things/Projectile/ABY_CrownfireCarrierRocket";

        private int ticksAlive;
        private bool launchVfxSpawned;
        private bool splitTriggered;
        private bool launchOriginInitialized;
        private Vector3 launchOrigin;
        private Material cachedCarrierMaterial;

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
                launchOrigin = previousPosition;
                launchOriginInitialized = true;
                CrownfireRocketChoirVfxUtility.SpawnTubeIgnition(previousPosition, Map);
                CrownfireRocketChoirVfxUtility.SpawnLaunchExhaust(previousPosition, Map, 1f, 24);
            }

            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksAlive++;
            if (ticksAlive <= 4)
            {
                CrownfireRocketChoirVfxUtility.SpawnLaunchExhaust(ResolveCarrierVisualPosition(), Map, 0.42f, 8);
            }

            if (ticksAlive >= SplitTicks)
            {
                TriggerSplit(ResolveCarrierVisualPosition(), blockedByShield: false);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!launchOriginInitialized || splitTriggered)
            {
                return;
            }

            Vector3 drawPos = ResolveCarrierVisualPosition();
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead) + 0.032f;

            float progress = Mathf.Clamp01(ticksAlive / (float)VisualRiseTicks);
            float ignitionScale = Mathf.Lerp(0.86f, 1.12f, Mathf.Sin(progress * Mathf.PI));
            float launchStretch = Mathf.Lerp(0.92f, 1.10f, progress);
            float wobble = Mathf.Sin((ticksAlive + 1) * 0.36f) * 1.8f;

            DrawCarrierPlane(drawPos, wobble, new Vector3(0.50f * ignitionScale, 1f, 1.04f * launchStretch));
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            TriggerSplit(ResolveCarrierVisualPosition(), blockedByShield);
        }

        private Vector3 ResolveCarrierVisualPosition()
        {
            Vector3 origin = launchOriginInitialized ? launchOrigin : ExactPosition;
            float progress = Mathf.Clamp01(ticksAlive / (float)VisualRiseTicks);
            float eased = 1f - Mathf.Pow(1f - progress, 2f);
            Vector3 visualPosition = origin + Vector3.forward * (VisualForwardStartOffset + VisualRiseDistance * eased);
            visualPosition.y = ExactPosition.y;
            return visualPosition;
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
            List<Vector3> releaseDirections = new List<Vector3>(MicroRocketCount);

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
                Vector2 radial = Rand.InsideUnitCircle.normalized * Rand.Range(0.06f, 0.22f);
                origin.x += radial.x;
                origin.z += radial.y;

                Vector3 direction = targetInfo.CenterVector3 - origin;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    releaseDirections.Add(direction.normalized);
                    CrownfireRocketChoirVfxUtility.SpawnMicroTrail(origin, direction.normalized, map, 1.08f);
                }

                projectile.Launch(instigator, origin, targetInfo, targetInfo, ProjectileHitFlags.IntendedTarget, false, null, null);
            }

            CrownfireRocketChoirVfxUtility.SpawnSplitReleaseAccent(splitPosition, releaseDirections, map);
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

        private void DrawCarrierPlane(Vector3 center, float angle, Vector3 scale)
        {
            Material material = CarrierMaterial;
            if (material == null)
            {
                return;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private Material CarrierMaterial
        {
            get
            {
                if (cachedCarrierMaterial == null)
                {
                    cachedCarrierMaterial = MaterialPool.MatFrom(CarrierVisualTexturePath, ShaderDatabase.MoteGlow);
                }

                return cachedCarrierMaterial;
            }
        }
    }
}
