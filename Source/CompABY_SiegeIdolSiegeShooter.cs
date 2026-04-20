using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_SiegeIdolSiegeShooter : ThingComp
    {
        private int nextSearchTick;
        private int nextReadyTick;
        private int deployCompleteTick = -1;
        private int warmupCompleteTick = -1;
        private int nextTelegraphTick = -1;
        private int anchorReleaseTick = -1;
        private bool anchored;
        private Thing currentTarget;

        private CompProperties_ABY_SiegeIdolSiegeShooter Props => (CompProperties_ABY_SiegeIdolSiegeShooter)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextSearchTick, "nextSearchTick", 0);
            Scribe_Values.Look(ref nextReadyTick, "nextReadyTick", 0);
            Scribe_Values.Look(ref deployCompleteTick, "deployCompleteTick", -1);
            Scribe_Values.Look(ref warmupCompleteTick, "warmupCompleteTick", -1);
            Scribe_Values.Look(ref nextTelegraphTick, "nextTelegraphTick", -1);
            Scribe_Values.Look(ref anchorReleaseTick, "anchorReleaseTick", -1);
            Scribe_Values.Look(ref anchored, "anchored", false);
            Scribe_References.Look(ref currentTarget, "currentTarget");
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (!CanOperate(pawn))
            {
                ResetAll();
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (TryBreakForCloseThreat(pawn))
            {
                return;
            }

            if (deployCompleteTick >= 0)
            {
                if (!CanAnchorAtTarget(pawn, currentTarget))
                {
                    CancelDeploy();
                    return;
                }

                HoldAnchorPose(pawn, currentTarget);
                if (ticksGame >= deployCompleteTick)
                {
                    anchored = true;
                    deployCompleteTick = -1;
                    anchorReleaseTick = ticksGame + Math.Max(45, Props.anchoredIdleReleaseTicks);
                    FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.45f);
                    FleckMaker.ThrowMicroSparks(pawn.DrawPos, pawn.Map);
                    FleckMaker.Static(pawn.PositionHeld, pawn.Map, FleckDefOf.ExplosionFlash, 1.05f);
                }

                return;
            }

            if (warmupCompleteTick >= 0)
            {
                if (!CanFireAt(pawn, currentTarget))
                {
                    CancelWarmup();
                    return;
                }

                HoldAnchorPose(pawn, currentTarget);
                if (ticksGame >= nextTelegraphTick)
                {
                    ShowTelegraphFX(pawn, currentTarget, false);
                    nextTelegraphTick = ticksGame + Math.Max(6, Props.telegraphIntervalTicks);
                }

                if (ticksGame >= warmupCompleteTick)
                {
                    FireShot(pawn, currentTarget);
                    warmupCompleteTick = -1;
                    nextTelegraphTick = -1;
                    nextReadyTick = ticksGame + Math.Max(1, Props.cooldownTicks);
                    anchorReleaseTick = ticksGame + Math.Max(45, Props.anchoredIdleReleaseTicks);
                }

                return;
            }

            if (anchored)
            {
                if (currentTarget != null && CanFireAt(pawn, currentTarget))
                {
                    HoldAnchorPose(pawn, currentTarget);
                }
                else
                {
                    currentTarget = null;
                }

                if (ticksGame >= nextSearchTick)
                {
                    nextSearchTick = ticksGame + Math.Max(8, Props.scanIntervalTicks);
                    Thing replacement = FindBestTarget(pawn);
                    if (replacement != null && CanFireAt(pawn, replacement))
                    {
                        currentTarget = replacement;
                        anchorReleaseTick = ticksGame + Math.Max(45, Props.anchoredIdleReleaseTicks);
                    }
                }

                if (currentTarget != null && CanFireAt(pawn, currentTarget))
                {
                    if (ticksGame >= nextReadyTick)
                    {
                        BeginWarmup(pawn, currentTarget, ticksGame);
                    }

                    return;
                }

                if (anchorReleaseTick < 0)
                {
                    anchorReleaseTick = ticksGame + Math.Max(45, Props.anchoredIdleReleaseTicks);
                }

                if (ticksGame >= anchorReleaseTick)
                {
                    ReleaseAnchor();
                }

                return;
            }

            if (ticksGame < nextSearchTick)
            {
                return;
            }

            nextSearchTick = ticksGame + Math.Max(8, Props.scanIntervalTicks);
            Thing target = FindBestTarget(pawn);
            if (target == null)
            {
                currentTarget = null;
                return;
            }

            currentTarget = target;
            if (CanAnchorAtTarget(pawn, target))
            {
                BeginDeploy(pawn, target, ticksGame);
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!(parent is Pawn pawn) || !pawn.Spawned)
            {
                return null;
            }

            if (deployCompleteTick >= 0 && Find.TickManager != null)
            {
                int remaining = Math.Max(0, deployCompleteTick - Find.TickManager.TicksGame);
                return "Siege stance: deploying (" + remaining.ToStringTicksToPeriod() + ")";
            }

            if (warmupCompleteTick >= 0 && Find.TickManager != null)
            {
                int remaining = Math.Max(0, warmupCompleteTick - Find.TickManager.TicksGame);
                return "Siege stance: charging (" + remaining.ToStringTicksToPeriod() + ")";
            }

            if (anchored)
            {
                return "Siege stance: anchored";
            }

            return null;
        }

        private bool CanOperate(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || pawn.Dead || !pawn.Spawned || pawn.Downed)
            {
                return false;
            }

            if (pawn.Faction == null || Faction.OfPlayer == null || !pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            return true;
        }

        private bool TryBreakForCloseThreat(Pawn pawn)
        {
            if (pawn == null || Props.panicMeleeRange <= 0f)
            {
                return false;
            }

            Pawn nearestThreat = AbyssalThreatPawnUtility.FindClosestThreatWithin(pawn, Props.panicMeleeRange);
            if (nearestThreat == null)
            {
                return false;
            }

            if (anchored || deployCompleteTick >= 0 || warmupCompleteTick >= 0)
            {
                FleckMaker.ThrowMicroSparks(pawn.DrawPos, pawn.Map);
            }

            ResetAnchorState();
            currentTarget = nearestThreat;
            return true;
        }

        private void BeginDeploy(Pawn pawn, Thing target, int ticksGame)
        {
            if (pawn == null || target == null)
            {
                return;
            }

            deployCompleteTick = ticksGame + Math.Max(1, Props.deployTicks);
            anchorReleaseTick = -1;
            pawn.rotationTracker?.FaceTarget(target.PositionHeld);
            pawn.pather?.StopDead();
            ShowDeployFX(pawn);
        }

        private void BeginWarmup(Pawn pawn, Thing target, int ticksGame)
        {
            if (pawn == null || target == null)
            {
                return;
            }

            warmupCompleteTick = ticksGame + Math.Max(1, Props.warmupTicks);
            nextTelegraphTick = ticksGame;
            pawn.rotationTracker?.FaceTarget(target.PositionHeld);
            pawn.pather?.StopDead();
            if (!Props.aimSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.aimSoundDefName, pawn.PositionHeld, pawn.Map);
            }

            ShowTelegraphFX(pawn, target, true);
        }

        private void HoldAnchorPose(Pawn pawn, Thing target)
        {
            if (pawn == null)
            {
                return;
            }

            pawn.pather?.StopDead();
            if (target != null)
            {
                pawn.rotationTracker?.FaceTarget(target.PositionHeld);
            }
        }

        private void ReleaseAnchor()
        {
            ResetAnchorState();
            currentTarget = null;
        }

        private void CancelDeploy()
        {
            deployCompleteTick = -1;
            currentTarget = null;
        }

        private void CancelWarmup()
        {
            warmupCompleteTick = -1;
            nextTelegraphTick = -1;
            nextReadyTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + 24;
            currentTarget = null;
        }

        private void ResetAnchorState()
        {
            anchored = false;
            deployCompleteTick = -1;
            warmupCompleteTick = -1;
            nextTelegraphTick = -1;
            anchorReleaseTick = -1;
        }

        private void ResetAll()
        {
            nextSearchTick = 0;
            nextReadyTick = 0;
            ResetAnchorState();
            currentTarget = null;
        }

        private Thing FindBestTarget(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Thing best = null;
            float bestScore = float.MinValue;

            if (Props.preferBuildingTargets)
            {
                List<Building> buildings = pawn.Map.listerBuildings?.allBuildingsColonist;
                if (buildings != null)
                {
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        Building building = buildings[i];
                        if (!CanFireAt(pawn, building))
                        {
                            continue;
                        }

                        float score = ScoreBuildingTarget(pawn, building);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = building;
                        }
                    }
                }
            }

            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return best;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                if (!CanFireAt(pawn, candidate))
                {
                    continue;
                }

                float score = ScorePawnTarget(pawn, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private float ScoreBuildingTarget(Pawn pawn, Building building)
        {
            float distance = pawn.Position.DistanceTo(building.PositionHeld);
            float score = 40f - distance;

            if (Props.prioritizeTurrets && IsTurretLike(building))
            {
                score += 95f;
            }

            if (Props.prioritizeDoors && building is Building_Door)
            {
                score += 64f;
            }

            if (Props.prioritizeCover && IsCoverLike(building))
            {
                score += 52f;
            }

            if (building.def?.Fillage == FillCategory.Full)
            {
                score += 22f;
            }

            if (building.TryGetComp<CompPowerTrader>() != null)
            {
                score += 16f;
            }

            score += CountNearbyPlayerPawns(pawn.Map, building.PositionHeld, 4.9f) * 9f;
            return score;
        }

        private float ScorePawnTarget(Pawn pawn, Pawn target)
        {
            float distance = pawn.Position.DistanceTo(target.PositionHeld);
            float score = 22f - distance;
            if (Props.preferRangedTargets && AbyssalThreatPawnUtility.HasRangedWeapon(target))
            {
                score += 28f;
            }

            if (distance >= Props.anchorMinRange)
            {
                score += 6f;
            }

            return score;
        }

        private int CountNearbyPlayerPawns(Map map, IntVec3 center, float radius)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || pawn.Downed || pawn.Faction != Faction.OfPlayer)
                {
                    continue;
                }

                if (pawn.PositionHeld.DistanceTo(center) <= radius)
                {
                    count++;
                }
            }

            return count;
        }

        private bool CanAnchorAtTarget(Pawn pawn, Thing target)
        {
            if (!CanFireAt(pawn, target))
            {
                return false;
            }

            float distance = pawn.Position.DistanceTo(target.PositionHeld);
            return distance >= Mathf.Max(0f, Props.anchorMinRange);
        }

        private bool CanFireAt(Pawn shooter, Thing target)
        {
            if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(shooter, target))
            {
                return false;
            }

            IntVec3 targetCell = target.PositionHeld;
            if (!targetCell.IsValid || targetCell == shooter.PositionHeld)
            {
                return false;
            }

            float distance = shooter.Position.DistanceTo(targetCell);
            if (distance > Props.range || distance < Mathf.Max(0f, Props.targetMinRange))
            {
                return false;
            }

            return GenSight.LineOfSight(shooter.PositionHeld, targetCell, shooter.Map);
        }

        private bool IsTurretLike(Building building)
        {
            return building is Building_Turret;
        }

        private bool IsCoverLike(Building building)
        {
            if (building?.def?.defName == null)
            {
                return false;
            }

            string defName = building.def.defName;
            return defName.IndexOf("Sandbag", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Barricade", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Barrier", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Embrasure", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void FireShot(Pawn pawn, Thing target)
        {
            if (pawn == null || target == null)
            {
                return;
            }

            pawn.rotationTracker?.FaceTarget(target.PositionHeld);
            if (!Props.castSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.castSoundDefName, pawn.PositionHeld, pawn.Map);
            }

            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.projectileDefName);
            if (projectileDef == null)
            {
                return;
            }

            Projectile projectile = GenSpawn.Spawn(projectileDef, pawn.PositionHeld, pawn.Map, WipeMode.Vanish) as Projectile;
            if (projectile == null)
            {
                return;
            }

            if (!TryLaunchProjectile(projectile, pawn, target))
            {
                projectile.Destroy(DestroyMode.Vanish);
                return;
            }

            FleckMaker.ThrowLightningGlow(target.DrawPos, pawn.Map, 1.2f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, pawn.Map);
        }

        private bool TryLaunchProjectile(Projectile projectile, Pawn pawn, Thing target)
        {
            MethodInfo[] methods = projectile.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == "Launch")
                .OrderByDescending(m => m.GetParameters().Length)
                .ToArray();

            for (int i = 0; i < methods.Length; i++)
            {
                if (!TryBuildLaunchArgs(methods[i], pawn, target, out object[] args))
                {
                    continue;
                }

                try
                {
                    methods[i].Invoke(projectile, args);
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private bool TryBuildLaunchArgs(MethodInfo method, Pawn pawn, Thing target, out object[] args)
        {
            ParameterInfo[] parameters = method.GetParameters();
            args = new object[parameters.Length];
            int thingSlot = 0;
            LocalTargetInfo targetInfo = new LocalTargetInfo(target);

            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                if (typeof(Thing).IsAssignableFrom(parameterType))
                {
                    args[i] = thingSlot == 0 ? (object)pawn : null;
                    thingSlot++;
                    continue;
                }

                if (parameterType == typeof(Vector3))
                {
                    args[i] = pawn.DrawPos;
                    continue;
                }

                if (parameterType == typeof(LocalTargetInfo))
                {
                    args[i] = targetInfo;
                    continue;
                }

                if (parameterType == typeof(ProjectileHitFlags))
                {
                    args[i] = ProjectileHitFlags.IntendedTarget;
                    continue;
                }

                if (parameterType == typeof(bool))
                {
                    args[i] = false;
                    continue;
                }

                if (parameterType == typeof(ThingDef))
                {
                    args[i] = null;
                    continue;
                }

                if (parameterType.IsEnum)
                {
                    args[i] = Activator.CreateInstance(parameterType);
                    continue;
                }

                args = null;
                return false;
            }

            return true;
        }

        private void ShowDeployFX(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, 1.15f);
            FleckMaker.ThrowMicroSparks(pawn.DrawPos, pawn.Map);
        }

        private void ShowTelegraphFX(Pawn pawn, Thing target, bool initial)
        {
            if (pawn?.Map == null || target == null || !target.Spawned || target.Map != pawn.Map)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(pawn.DrawPos, pawn.Map, initial ? 1.45f : 0.9f);
            FleckMaker.ThrowMicroSparks(pawn.DrawPos, pawn.Map);
            FleckMaker.ThrowLightningGlow(target.DrawPos, pawn.Map, initial ? 1.2f : 0.72f);
            if (initial)
            {
                FleckMaker.Static(target.PositionHeld, pawn.Map, FleckDefOf.ExplosionFlash, 0.9f);
            }
        }
    }
}
