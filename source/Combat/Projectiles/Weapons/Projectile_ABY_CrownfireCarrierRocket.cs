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
        private const string MicroRocketSlowDefName = "ABY_CrownfireMicroRocket_Slow";
        private const string MicroRocketNormalDefName = "ABY_CrownfireMicroRocket";
        private const string MicroRocketFastDefName = "ABY_CrownfireMicroRocket_Fast";
        private const string CarrierVisualTexturePath = "Things/Projectile/ABY_CrownfireCarrierRocket";

        private int ticksAlive;
        private bool launchVfxSpawned;
        private bool splitTriggered;
        private bool launchOriginInitialized;
        private Vector3 launchOrigin;
        private Material cachedCarrierMaterial;

        private List<LocalTargetInfo> pendingTargets;
        private List<Vector3> pendingReleaseDirections;
        private Vector3 pendingSplitPosition;
        private Vector3 pendingTargetPosition;
        private Thing pendingInstigator;
        private int pendingReleaseIndex;
        private int pendingReleaseTick;

        private static ThingDef microRocketSlowDef;
        private static ThingDef microRocketNormalDef;
        private static ThingDef microRocketFastDef;
        private static ThingDef MicroRocketSlowDef => microRocketSlowDef ?? (microRocketSlowDef = DefDatabase<ThingDef>.GetNamedSilentFail(MicroRocketSlowDefName));
        private static ThingDef MicroRocketNormalDef => microRocketNormalDef ?? (microRocketNormalDef = DefDatabase<ThingDef>.GetNamedSilentFail(MicroRocketNormalDefName));
        private static ThingDef MicroRocketFastDef => microRocketFastDef ?? (microRocketFastDef = DefDatabase<ThingDef>.GetNamedSilentFail(MicroRocketFastDefName));

        protected override void Tick()
        {
            Vector3 previousPosition = ExactPosition;

            if (!Spawned || Map == null)
            {
                base.Tick();
                return;
            }

            if (splitTriggered)
            {
                TickPendingMicroRelease();
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
                    PreparePendingMicroRelease(splitPosition, targetPosition, splitMap, instigator);
                    TickPendingMicroRelease();
                    return;
                }
            }

            if (!Destroyed)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        private void PreparePendingMicroRelease(Vector3 splitPosition, Vector3 targetPosition, Map map, Thing instigator)
        {
            pendingSplitPosition = splitPosition;
            pendingTargetPosition = targetPosition;
            pendingInstigator = instigator;
            pendingReleaseIndex = 0;
            pendingReleaseTick = 0;
            pendingTargets = BuildMicroTargets(targetPosition.ToIntVec3(), splitPosition, map, instigator?.Faction);
            pendingReleaseDirections = new List<Vector3>(MicroRocketCount);
        }

        private void TickPendingMicroRelease()
        {
            Map releaseMap = Map;
            if (releaseMap == null || pendingTargets == null || pendingReleaseIndex >= MicroRocketCount)
            {
                if (!Destroyed)
                {
                    Destroy(DestroyMode.Vanish);
                }

                return;
            }

            int batchSize = pendingReleaseTick == 0 ? 3 : pendingReleaseTick == 1 ? 3 : 2;
            int releasedThisTick = 0;
            while (pendingReleaseIndex < MicroRocketCount && releasedThisTick < batchSize)
            {
                LocalTargetInfo targetInfo = pendingReleaseIndex < pendingTargets.Count
                    ? pendingTargets[pendingReleaseIndex]
                    : new LocalTargetInfo(RandomFallbackCell(pendingTargetPosition.ToIntVec3(), releaseMap));

                if (targetInfo.IsValid)
                {
                    LaunchSingleMicroRocket(pendingSplitPosition, targetInfo, releaseMap, pendingInstigator, pendingReleaseIndex, pendingReleaseTick, pendingReleaseDirections);
                }

                pendingReleaseIndex++;
                releasedThisTick++;
            }

            if (pendingReleaseDirections != null && pendingReleaseDirections.Count > 0)
            {
                CrownfireRocketChoirVfxUtility.SpawnSplitReleaseAccent(pendingSplitPosition, pendingReleaseDirections, releaseMap);
                pendingReleaseDirections.Clear();
            }

            pendingReleaseTick++;
            if (pendingReleaseIndex >= MicroRocketCount && !Destroyed)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        private static void LaunchSingleMicroRocket(
            Vector3 splitPosition,
            LocalTargetInfo targetInfo,
            Map map,
            Thing instigator,
            int rocketIndex,
            int releaseTick,
            List<Vector3> releaseDirections)
        {
            int speedProfile = ResolveSpeedProfile(rocketIndex, releaseTick);
            ThingDef projectileDef = ResolveMicroRocketDef(speedProfile);
            if (map == null || projectileDef == null)
            {
                return;
            }

            Projectile projectile = GenSpawn.Spawn(projectileDef, splitPosition.ToIntVec3(), map, WipeMode.Vanish) as Projectile;
            if (projectile == null)
            {
                return;
            }

            Vector3 origin = splitPosition;
            Vector2 radial = Rand.InsideUnitCircle.normalized * Rand.Range(0.08f, 0.28f);
            origin.x += radial.x;
            origin.z += radial.y;

            Vector3 direction = targetInfo.CenterVector3 - origin;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                direction.Normalize();
                releaseDirections?.Add(direction);
                origin += direction * (0.05f * releaseTick);
                CrownfireRocketChoirVfxUtility.SpawnMicroTrail(origin, direction, map, speedProfile == 2 ? 1.16f : 1.04f);
            }

            if (projectile is Projectile_ABY_CrownfireMicroRocket crownfireMicro)
            {
                crownfireMicro.ConfigureCrownfireVisualProfile(rocketIndex, speedProfile);
            }

            projectile.Launch(instigator, origin, targetInfo, targetInfo, ProjectileHitFlags.IntendedTarget, false, null, null);
        }

        private static int ResolveSpeedProfile(int rocketIndex, int releaseTick)
        {
            int profile = (rocketIndex + releaseTick + Rand.Range(0, 3)) % 3;
            if (rocketIndex == 0)
            {
                return 1;
            }

            return profile;
        }

        private static ThingDef ResolveMicroRocketDef(int speedProfile)
        {
            if (speedProfile <= 0 && MicroRocketSlowDef != null)
            {
                return MicroRocketSlowDef;
            }

            if (speedProfile >= 2 && MicroRocketFastDef != null)
            {
                return MicroRocketFastDef;
            }

            return MicroRocketNormalDef ?? MicroRocketSlowDef ?? MicroRocketFastDef;
        }

        private static List<LocalTargetInfo> BuildMicroTargets(IntVec3 primaryCell, Vector3 splitPosition, Map map, Faction launcherFaction)
        {
            List<LocalTargetInfo> result = new List<LocalTargetInfo>(MicroRocketCount);
            if (map != null)
            {
                float radiusSquared = TargetSearchRadius * TargetSearchRadius;
                IntVec3 splitCell = splitPosition.ToIntVec3();
                IReadOnlyList<Pawn> candidates = ABY_RuntimeTargetCache.CombatTargetPawnsFor(map);
                Pawn[] bestPawns = new Pawn[MicroRocketCount];
                float[] bestScores = new float[MicroRocketCount];
                for (int i = 0; i < bestScores.Length; i++)
                {
                    bestScores[i] = float.MaxValue;
                }

                for (int i = 0; i < candidates.Count; i++)
                {
                    Pawn pawn = candidates[i];
                    if (!IsValidMicroTarget(pawn, launcherFaction, map))
                    {
                        continue;
                    }

                    float primaryDistance = pawn.Position.DistanceToSquared(primaryCell);
                    float splitDistance = pawn.Position.DistanceToSquared(splitCell);
                    if (primaryDistance > radiusSquared && splitDistance > radiusSquared)
                    {
                        continue;
                    }

                    float score = primaryDistance + splitDistance * 0.18f;
                    for (int slot = 0; slot < bestPawns.Length; slot++)
                    {
                        if (score >= bestScores[slot])
                        {
                            continue;
                        }

                        for (int move = bestPawns.Length - 1; move > slot; move--)
                        {
                            bestPawns[move] = bestPawns[move - 1];
                            bestScores[move] = bestScores[move - 1];
                        }

                        bestPawns[slot] = pawn;
                        bestScores[slot] = score;
                        break;
                    }
                }

                for (int i = 0; i < bestPawns.Length; i++)
                {
                    if (bestPawns[i] == null)
                    {
                        continue;
                    }

                    result.Add(new LocalTargetInfo(bestPawns[i]));
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
