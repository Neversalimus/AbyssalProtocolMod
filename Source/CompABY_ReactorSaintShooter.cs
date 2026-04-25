using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompABY_ReactorSaintShooter : ThingComp
    {
        private const int AttackModeNone = 0;
        private const int AttackModePrimary = 1;
        private const int AttackModeBarrage = 2;

        private int nextSearchTick;
        private int nextPrimaryReadyTick;
        private int nextBarrageReadyTick;
        private int warmupCompleteTick = -1;
        private int nextBurstShotTick = -1;
        private int burstShotsRemaining;
        private int currentAttackMode;
        private Thing currentTarget;
        private IntVec3 currentTargetCell = IntVec3.Invalid;
        private int targetLockUntilTick = -1;

        private float primaryCooldownFactor = 1f;
        private float barrageCooldownFactor = 1f;
        private float warmupFactor = 1f;
        private float barrageChanceBonus;
        private int barrageShotBonus;
        private int collapseWindowUntilTick = -1;
        private int nextStructureCrushTick;
        private int forcedAdvanceUntilTick = -1;
        private int lastPositionChangeTick = -1;
        private IntVec3 lastTrackedPosition = IntVec3.Invalid;

        private int cachedCrowdPressureUntilTick = -1;
        private bool cachedCrowdPressure;
        private IntVec3 cachedCrowdPressurePosition = IntVec3.Invalid;

        private int cachedAdvanceTargetTick = -1;
        private Thing cachedAdvanceTarget;
        private IntVec3 cachedAdvanceTargetPosition = IntVec3.Invalid;

        private int cachedAdjacentThreatTick = -1;
        private Pawn cachedAdjacentThreat;
        private float cachedAdjacentThreatMaxDistance = -1f;
        private IntVec3 cachedAdjacentThreatPosition = IntVec3.Invalid;

        private CompProperties_ABY_ReactorSaintShooter Props => (CompProperties_ABY_ReactorSaintShooter)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextSearchTick, "nextSearchTick", 0);
            Scribe_Values.Look(ref nextPrimaryReadyTick, "nextPrimaryReadyTick", 0);
            Scribe_Values.Look(ref nextBarrageReadyTick, "nextBarrageReadyTick", 0);
            Scribe_Values.Look(ref warmupCompleteTick, "warmupCompleteTick", -1);
            Scribe_Values.Look(ref nextBurstShotTick, "nextBurstShotTick", -1);
            Scribe_Values.Look(ref burstShotsRemaining, "burstShotsRemaining", 0);
            Scribe_Values.Look(ref currentAttackMode, "currentAttackMode", 0);
            Scribe_Values.Look(ref currentTargetCell, "currentTargetCell");
            Scribe_References.Look(ref currentTarget, "currentTarget");
            Scribe_Values.Look(ref targetLockUntilTick, "targetLockUntilTick", -1);
            Scribe_Values.Look(ref primaryCooldownFactor, "primaryCooldownFactor", 1f);
            Scribe_Values.Look(ref barrageCooldownFactor, "barrageCooldownFactor", 1f);
            Scribe_Values.Look(ref warmupFactor, "warmupFactor", 1f);
            Scribe_Values.Look(ref barrageChanceBonus, "barrageChanceBonus", 0f);
            Scribe_Values.Look(ref barrageShotBonus, "barrageShotBonus", 0);
            Scribe_Values.Look(ref collapseWindowUntilTick, "collapseWindowUntilTick", -1);
            Scribe_Values.Look(ref nextStructureCrushTick, "nextStructureCrushTick", 0);
            Scribe_Values.Look(ref forcedAdvanceUntilTick, "forcedAdvanceUntilTick", -1);
            Scribe_Values.Look(ref lastPositionChangeTick, "lastPositionChangeTick", -1);
            Scribe_Values.Look(ref lastTrackedPosition, "lastTrackedPosition");
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (!CanOperate(pawn))
            {
                forcedAdvanceUntilTick = -1;
                lastTrackedPosition = IntVec3.Invalid;
                ResetAttackState();
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            UpdateMovementProgress(pawn, ticksGame);
            TryApplyStructureCrushBonus(pawn, ticksGame);
            bool crowdPressure = HasCrowdPressure(pawn);
            if (TryForceCloseQuartersEngagement(pawn))
            {
                forcedAdvanceUntilTick = -1;
                ResetAttackState();
                return;
            }

            if (TryHandleForcedAdvance(pawn, ticksGame))
            {
                ResetAttackState();
                return;
            }

            if (!crowdPressure && TryMaintainSpacing(pawn))
            {
                ResetAttackState();
                return;
            }

            if (burstShotsRemaining > 0)
            {
                if (!CanContinueCurrentAttack(pawn))
                {
                    ResetAttackState();
                    TryAdvanceTowardDistantThreat(pawn);
                    return;
                }

                if (ticksGame >= nextBurstShotTick)
                {
                    ExecuteCurrentShot(pawn);
                    burstShotsRemaining--;
                    if (burstShotsRemaining > 0)
                    {
                        nextBurstShotTick = ticksGame + GetTicksBetweenShotsForCurrentMode();
                    }
                    else
                    {
                        FinalizeAttackCycle(ticksGame);
                    }
                }

                return;
            }

            if (warmupCompleteTick >= 0)
            {
                if (!CanContinueCurrentAttack(pawn))
                {
                    ResetAttackState();
                    TryAdvanceTowardDistantThreat(pawn);
                    return;
                }

                if (ticksGame >= warmupCompleteTick)
                {
                    warmupCompleteTick = -1;
                    ExecuteCurrentShot(pawn);
                    burstShotsRemaining = GetShotCountForCurrentMode() - 1;
                    if (burstShotsRemaining > 0)
                    {
                        nextBurstShotTick = ticksGame + GetTicksBetweenShotsForCurrentMode();
                    }
                    else
                    {
                        FinalizeAttackCycle(ticksGame);
                    }
                }

                return;
            }

            if (ticksGame < nextSearchTick)
            {
                if (TryForceCloseQuartersEngagement(pawn))
                {
                    return;
                }

                if (TryAdvanceTowardDistantThreat(pawn))
                {
                    return;
                }

                return;
            }

            nextSearchTick = ticksGame + Math.Max(5, Props.scanIntervalTicks);
            Thing target = FindBestTargetThing(pawn, ticksGame);
            if (target == null)
            {
                if (TryForceCloseQuartersEngagement(pawn))
                {
                    return;
                }

                if (TryAdvanceTowardDistantThreat(pawn))
                {
                    return;
                }

                TryForceAdjacentBuildingBash(pawn);
                return;
            }

            int attackMode = ResolveAttackMode(pawn, target, ticksGame);
            if (attackMode == AttackModeNone)
            {
                if (TryForceCloseQuartersEngagement(pawn))
                {
                    return;
                }

                if (TryAdvanceTowardDistantThreat(pawn))
                {
                    return;
                }

                TryForceAdjacentBuildingBash(pawn);
                return;
            }

            currentAttackMode = attackMode;
            currentTarget = target;
            currentTargetCell = target.PositionHeld;
            SetTargetLock(ticksGame, attackMode);
            warmupCompleteTick = ticksGame + GetWarmupTicksForCurrentMode();
            nextBurstShotTick = -1;
            burstShotsRemaining = 0;
            forcedAdvanceUntilTick = -1;

            if (!Props.aimSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayChargeAt(Props.aimSoundDefName, pawn.Position, pawn.Map);
            }

            if (Props.holdPositionWhenTargeting)
            {
                pawn.pather?.StopDead();
            }

            pawn.rotationTracker?.FaceTarget(target.PositionHeld);
        }

        public void SetPhaseTuning(
            float primaryCdFactor,
            float barrageCdFactor,
            float phaseWarmupFactor,
            float phaseBarrageChanceBonus,
            int phaseBarrageShotBonus)
        {
            primaryCooldownFactor = Mathf.Max(0.2f, primaryCdFactor);
            barrageCooldownFactor = Mathf.Max(0.2f, barrageCdFactor);
            warmupFactor = Mathf.Max(0.2f, phaseWarmupFactor);
            barrageChanceBonus = Mathf.Clamp(phaseBarrageChanceBonus, -0.9f, 0.9f);
            barrageShotBonus = Mathf.Max(0, phaseBarrageShotBonus);
        }

        public void NotifyAegisCollapsed(int durationTicks)
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            collapseWindowUntilTick = Math.Max(collapseWindowUntilTick, ticksGame + Math.Max(60, durationTicks));
            nextPrimaryReadyTick = Math.Max(nextPrimaryReadyTick, ticksGame + 45);
            nextBarrageReadyTick = Math.Max(nextBarrageReadyTick, ticksGame + Math.Max(60, durationTicks));
            nextSearchTick = Math.Max(nextSearchTick, ticksGame + 15);
            ResetAttackState();
        }

        private void UpdateMovementProgress(Pawn pawn, int ticksGame)
        {
            if (pawn == null || !pawn.Spawned)
            {
                lastTrackedPosition = IntVec3.Invalid;
                return;
            }

            if (!lastTrackedPosition.IsValid || pawn.Position != lastTrackedPosition)
            {
                lastTrackedPosition = pawn.Position;
                lastPositionChangeTick = ticksGame;
            }
        }

        private bool TryHandleForcedAdvance(Pawn pawn, int ticksGame)
        {
            if (pawn?.Map == null)
            {
                forcedAdvanceUntilTick = -1;
                return false;
            }

            if (ShouldStartForcedAdvance(pawn, ticksGame))
            {
                forcedAdvanceUntilTick = ticksGame + 180;
            }

            if (ticksGame >= forcedAdvanceUntilTick)
            {
                return false;
            }

            Thing target = FindBestAdvanceTarget(pawn);
            if (target == null)
            {
                forcedAdvanceUntilTick = -1;
                return false;
            }

            IntVec3 targetCell = target.PositionHeld;
            if (!targetCell.IsValid || !targetCell.InBounds(pawn.Map))
            {
                forcedAdvanceUntilTick = -1;
                return false;
            }

            float desiredRange = Mathf.Clamp(Props.range * 0.78f, Props.preferredMinRange + 2f, Props.range - 1.5f);
            if (pawn.Position.DistanceTo(targetCell) <= desiredRange && GenSight.LineOfSight(pawn.Position, targetCell, pawn.Map))
            {
                forcedAdvanceUntilTick = -1;
                return false;
            }

            if (TryAdvanceTowardDistantThreat(pawn))
            {
                return true;
            }

            return TryForceAdjacentBuildingBash(pawn);
        }

        private bool ShouldStartForcedAdvance(Pawn pawn, int ticksGame)
        {
            if (warmupCompleteTick >= 0 || burstShotsRemaining > 0 || currentAttackMode != AttackModeNone)
            {
                return false;
            }

            Thing target = FindBestAdvanceTarget(pawn);
            if (target == null)
            {
                return false;
            }

            IntVec3 targetCell = target.PositionHeld;
            if (!targetCell.IsValid || !targetCell.InBounds(pawn.Map))
            {
                return false;
            }

            float desiredRange = Mathf.Clamp(Props.range * 0.78f, Props.preferredMinRange + 2f, Props.range - 1.5f);
            if (pawn.Position.DistanceTo(targetCell) <= desiredRange)
            {
                return false;
            }

            int stalledTicks = lastPositionChangeTick < 0 ? 0 : ticksGame - lastPositionChangeTick;
            return stalledTicks >= 150;
        }

        public override string CompInspectStringExtra()
        {
            if (!CollapseWindowActive || Find.TickManager == null)
            {
                return null;
            }

            int ticksRemaining = Math.Max(0, collapseWindowUntilTick - Find.TickManager.TicksGame);
            return "Reactor destabilized: " + (ticksRemaining / 60f).ToString("0.0") + "s";
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

        private Thing FindBestTargetThing(Pawn pawn, int ticksGame)
        {
            if (pawn == null)
            {
                return null;
            }

            Thing lockedTarget = TryResolveLockedTarget(pawn, ticksGame);
            if (lockedTarget != null)
            {
                return lockedTarget;
            }

            if (HasCrowdPressure(pawn))
            {
                Pawn crowdTarget = FindNearestThreatWithLineOfSight(pawn, Props.range);
                if (crowdTarget != null)
                {
                    return crowdTarget;
                }
            }

            Pawn pawnTarget = AbyssalThreatPawnUtility.FindBestTarget(
                pawn,
                0f,
                Props.range,
                Props.preferFarthestTargets,
                Props.preferRangedTargets,
                false,
                5.5f,
                1.1f);
            if (pawnTarget != null)
            {
                return pawnTarget;
            }

            Building blockingBuilding = FindNearbyBlockingBuilding(pawn);
            if (blockingBuilding != null)
            {
                return blockingBuilding;
            }

            return FindBestBuildingTarget(pawn);
        }

        private Thing TryResolveLockedTarget(Pawn pawn, int ticksGame)
        {
            if (pawn == null || ticksGame >= targetLockUntilTick)
            {
                return null;
            }

            if (currentTarget != null && currentTarget.Spawned && currentTarget.Map == pawn.Map && !currentTarget.Destroyed)
            {
                if (!(currentTarget is Pawn currentPawn) || !currentPawn.Dead)
                {
                    IntVec3 targetCell = currentTarget.PositionHeld;
                    if (targetCell.IsValid && targetCell.InBounds(pawn.Map) && pawn.Position.DistanceTo(targetCell) <= Props.range + 0.5f)
                    {
                        currentTargetCell = targetCell;
                        return currentTarget;
                    }
                }
            }

            Pawn replacementPawn = FindReplacementPawnNearCell(pawn, currentTargetCell, 5.9f);
            if (replacementPawn != null)
            {
                currentTarget = replacementPawn;
                currentTargetCell = replacementPawn.PositionHeld;
                return replacementPawn;
            }

            return null;
        }

        private Building FindBestBuildingTarget(Pawn pawn)
        {
            if (pawn?.Map == null || pawn.Faction == null)
            {
                return null;
            }

            Building best = null;
            float bestScore = float.MinValue;
            List<Building> colonistBuildings = pawn.Map.listerBuildings?.allBuildingsColonist;
            if (colonistBuildings == null)
            {
                return null;
            }

            for (int i = 0; i < colonistBuildings.Count; i++)
            {
                Building building = colonistBuildings[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, building))
                {
                    continue;
                }

                float distance = pawn.Position.DistanceTo(building.Position);
                if (distance > Props.range || distance < 0.5f)
                {
                    continue;
                }

                bool hasLos = GenSight.LineOfSight(pawn.Position, building.Position, pawn.Map);
                float score = -distance;
                if (hasLos)
                {
                    score += 6f;
                }

                if (IsTurretLike(building))
                {
                    score += 8f;
                }

                if (building.def?.Fillage == FillCategory.Full)
                {
                    score += 4f;
                }

                if (distance <= 8.5f)
                {
                    score += 3.5f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = building;
                }
            }

            return best;
        }

        private Building FindNearbyBlockingBuilding(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Building best = null;
            float bestScore = float.MinValue;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, 3.4f, true))
            {
                if (!cell.InBounds(pawn.Map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(pawn.Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Building building = things[i] as Building;
                    if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, building))
                    {
                        continue;
                    }

                    float distance = pawn.Position.DistanceTo(building.Position);
                    float score = 10f - distance;
                    if (IsTurretLike(building))
                    {
                        score += 3f;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = building;
                    }
                }
            }

            return best;
        }


        private Thing FindBestAdvanceTarget(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                cachedAdvanceTargetTick = -1;
                cachedAdvanceTarget = null;
                cachedAdvanceTargetPosition = IntVec3.Invalid;
                return null;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (cachedAdvanceTargetTick == ticksGame && cachedAdvanceTargetPosition == pawn.Position)
            {
                return cachedAdvanceTarget;
            }

            Thing resolved = FindBestAdvanceTargetUncached(pawn);
            cachedAdvanceTargetTick = ticksGame;
            cachedAdvanceTarget = resolved;
            cachedAdvanceTargetPosition = pawn.Position;
            return resolved;
        }

        private Thing FindBestAdvanceTargetUncached(Pawn pawn)
        {
            Pawn bestPawn = null;
            float bestPawnDistanceSq = float.MaxValue;
            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn candidate = pawns[i];
                    if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                    {
                        continue;
                    }

                    float distanceSq = (candidate.Position - pawn.Position).LengthHorizontalSquared;
                    if (distanceSq < bestPawnDistanceSq)
                    {
                        bestPawnDistanceSq = distanceSq;
                        bestPawn = candidate;
                    }
                }
            }

            if (bestPawn != null)
            {
                return bestPawn;
            }

            Building bestBuilding = null;
            float bestBuildingDistanceSq = float.MaxValue;
            List<Building> colonistBuildings = pawn.Map.listerBuildings?.allBuildingsColonist;
            if (colonistBuildings == null)
            {
                return null;
            }

            for (int i = 0; i < colonistBuildings.Count; i++)
            {
                Building building = colonistBuildings[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, building))
                {
                    continue;
                }

                float distanceSq = (building.Position - pawn.Position).LengthHorizontalSquared;
                if (distanceSq < bestBuildingDistanceSq)
                {
                    bestBuildingDistanceSq = distanceSq;
                    bestBuilding = building;
                }
            }

            return bestBuilding;
        }
        private bool TryAdvanceTowardDistantThreat(Pawn pawn)
        {
            if (pawn?.Map == null || pawn.jobs == null)
            {
                return false;
            }

            Thing target = FindBestAdvanceTarget(pawn);
            if (target == null)
            {
                return false;
            }

            IntVec3 targetCell = target.PositionHeld;
            if (!targetCell.IsValid || !targetCell.InBounds(pawn.Map))
            {
                return false;
            }

            float distance = pawn.Position.DistanceTo(targetCell);
            float desiredRange = Mathf.Clamp(Props.range * 0.78f, Props.preferredMinRange + 2f, Props.range - 1.5f);
            if (distance <= desiredRange)
            {
                return false;
            }

            IntVec3 destination = targetCell;
            if (TryFindAdvanceCell(pawn, targetCell, desiredRange, out IntVec3 approachCell))
            {
                destination = approachCell;
            }

            if (!destination.IsValid || destination == pawn.Position)
            {
                return false;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Goto && pawn.CurJob.targetA.IsValid && pawn.CurJob.targetA.Cell == destination)
            {
                return true;
            }

            Job goJob = JobMaker.MakeJob(JobDefOf.Goto, destination);
            goJob.expiryInterval = 150;
            goJob.checkOverrideOnExpire = true;
            goJob.collideWithPawns = true;
            pawn.jobs.TryTakeOrderedJob(goJob, JobTag.Misc);
            pawn.rotationTracker?.FaceTarget(targetCell);
            return true;
        }

        private bool TryFindAdvanceCell(Pawn pawn, IntVec3 targetCell, float desiredRange, out IntVec3 destination)
        {
            destination = IntVec3.Invalid;
            Map map = pawn?.Map;
            if (map == null || !targetCell.IsValid || !targetCell.InBounds(map))
            {
                return false;
            }

            float minRange = Mathf.Max(Props.preferredMinRange + 0.8f, 6f);
            float maxRange = Mathf.Min(Props.range - 1f, Mathf.Max(minRange + 2f, desiredRange + 4f));
            float bestScore = float.MinValue;
            int maxCells = Math.Min(GenRadial.NumCellsInRadius(maxRange), GenRadial.RadialPattern.Length);
            for (int i = 0; i < maxCells; i++)
            {
                IntVec3 cell = targetCell + GenRadial.RadialPattern[i];
                if (!cell.InBounds(map) || !cell.Walkable(map) || !cell.Standable(map) || AbyssalThreatPawnUtility.CellHasOtherPawn(cell, map, pawn))
                {
                    continue;
                }

                float targetDistance = cell.DistanceTo(targetCell);
                if (targetDistance < minRange || targetDistance > maxRange)
                {
                    continue;
                }

                if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    continue;
                }

                float moveDistance = pawn.Position.DistanceTo(cell);
                float score = 120f - moveDistance;
                if (GenSight.LineOfSight(cell, targetCell, map))
                {
                    score += 14f;
                }

                if (targetDistance >= desiredRange - 1.5f && targetDistance <= desiredRange + 3.5f)
                {
                    score += 8f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    destination = cell;
                }
            }

            return destination.IsValid;
        }

        private static bool IsTurretLike(Building building)
        {
            if (building == null)
            {
                return false;
            }

            if (building is Building_Turret)
            {
                return true;
            }

            System.Type thingClass = building.def?.thingClass;
            return thingClass != null && typeof(Building_Turret).IsAssignableFrom(thingClass);
        }

        private bool CanContinueCurrentAttack(Pawn pawn)
        {
            switch (currentAttackMode)
            {
                case AttackModePrimary:
                    if (CanFirePrimaryAt(pawn, currentTarget))
                    {
                        return true;
                    }

                    Pawn replacementPawn = FindReplacementPawnNearCell(pawn, currentTargetCell, 5.9f);
                    if (replacementPawn != null)
                    {
                        currentTarget = replacementPawn;
                        currentTargetCell = replacementPawn.PositionHeld;
                        return CanFirePrimaryAt(pawn, replacementPawn);
                    }

                    return false;
                case AttackModeBarrage:
                    return CanFireBarrageAt(pawn, currentTarget, currentTargetCell);
                default:
                    return false;
            }
        }

        private bool CanFirePrimaryAt(Pawn shooter, Thing target)
        {
            if (!AbyssalThreatPawnUtility.CanFireAt(shooter, target))
            {
                return false;
            }

            IntVec3 targetCell = target.PositionHeld;
            return targetCell.IsValid && shooter.Position.DistanceTo(targetCell) <= Props.range;
        }

        private bool CanFireBarrageAt(Pawn shooter, Thing target, IntVec3 fallbackCell)
        {
            if (shooter == null || shooter.Map == null)
            {
                return false;
            }

            IntVec3 targetCell = fallbackCell;
            if (target != null && target.Spawned && target.Map == shooter.Map)
            {
                if (!(target is Pawn pawnTarget) || !pawnTarget.Dead)
                {
                    targetCell = target.PositionHeld;
                }
            }

            if (!targetCell.IsValid || !targetCell.InBounds(shooter.Map))
            {
                return false;
            }

            if (shooter.Position.DistanceTo(targetCell) > Props.range)
            {
                return false;
            }

            return GenSight.LineOfSight(shooter.Position, targetCell, shooter.Map);
        }

        private bool TryMaintainSpacing(Pawn pawn)
        {
            return AbyssalThreatPawnUtility.TryMaintainSpacing(
                pawn,
                currentTarget as Pawn,
                Props.preferredMinRange,
                Props.retreatSearchRadius,
                Props.holdPositionWhenTargeting);
        }

        private bool HasCrowdPressure(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                cachedCrowdPressureUntilTick = -1;
                cachedCrowdPressurePosition = IntVec3.Invalid;
                cachedCrowdPressure = false;
                return false;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (ticksGame < cachedCrowdPressureUntilTick && cachedCrowdPressurePosition == pawn.Position)
            {
                return cachedCrowdPressure;
            }

            float radius = Mathf.Max(Props.preferredMinRange + 2.5f, 11.5f);
            int threshold = Props.preferredMinRange >= 9f ? 4 : 5;
            cachedCrowdPressure = CountNearbyHostilePawns(pawn, radius) >= threshold;
            cachedCrowdPressurePosition = pawn.Position;
            cachedCrowdPressureUntilTick = ticksGame + 15;
            return cachedCrowdPressure;
        }
        private int CountNearbyHostilePawns(Pawn pawn, float radius)
        {
            if (pawn?.Map == null)
            {
                return 0;
            }

            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return 0;
            }

            int count = 0;
            float radiusSq = radius * radius;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                if ((candidate.Position - pawn.Position).LengthHorizontalSquared > radiusSq)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private Pawn FindNearestThreatWithLineOfSight(Pawn pawn, float maxRange)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Pawn best = null;
            float bestDistance = maxRange + 0.01f;
            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float distance = pawn.Position.DistanceTo(candidate.Position);
                if (distance > bestDistance)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.Position, candidate.Position, pawn.Map))
                {
                    continue;
                }

                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        private bool CollapseWindowActive
        {
            get
            {
                int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                return ticksGame < collapseWindowUntilTick;
            }
        }

        private int ResolveAttackMode(Pawn pawn, Thing target, int ticksGame)
        {
            bool primaryReady = ticksGame >= nextPrimaryReadyTick;
            bool barrageReady = !CollapseWindowActive && ticksGame >= nextBarrageReadyTick;

            if (!primaryReady && !barrageReady)
            {
                return AttackModeNone;
            }

            if (HasCrowdPressure(pawn))
            {
                if (barrageReady)
                {
                    return AttackModeBarrage;
                }

                if (primaryReady)
                {
                    return AttackModePrimary;
                }
            }

            if (barrageReady)
            {
                int nearbyThreats = CountNearbyThreats(target, Props.barrageTargetClusterRadius, pawn);
                float barrageChance = Mathf.Clamp01(Props.barrageRandomChance + barrageChanceBonus);
                if (!primaryReady || nearbyThreats >= Math.Max(1, Props.barrageClusterThreshold) || Rand.Chance(barrageChance))
                {
                    return AttackModeBarrage;
                }
            }

            if (primaryReady)
            {
                return AttackModePrimary;
            }

            return barrageReady ? AttackModeBarrage : AttackModeNone;
        }

        private int CountNearbyThreats(Thing focusTarget, float radius, Pawn shooter)
        {
            if (focusTarget?.Map == null || shooter?.Faction == null)
            {
                return 0;
            }

            IntVec3 center = focusTarget.PositionHeld;
            if (!center.IsValid)
            {
                return 0;
            }

            int count = 0;
            var pawns = focusTarget.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return 0;
            }

            float radiusSq = radius * radius;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || pawn.Downed || pawn.Faction == null)
                {
                    continue;
                }

                if (!shooter.Faction.HostileTo(pawn.Faction))
                {
                    continue;
                }

                if ((pawn.Position - center).LengthHorizontalSquared > radiusSq)
                {
                    continue;
                }

                count++;
            }

            return count;
        }

        private void ExecuteCurrentShot(Pawn pawn)
        {
            switch (currentAttackMode)
            {
                case AttackModePrimary:
                    FirePrimaryShot(pawn, currentTarget);
                    break;
                case AttackModeBarrage:
                    FireBarrageShot(pawn, currentTarget, currentTargetCell);
                    break;
            }
        }

        private void FirePrimaryShot(Pawn pawn, Thing target)
        {
            if (pawn == null || target == null)
            {
                return;
            }

            IntVec3 targetCell = target.PositionHeld;
            if (!targetCell.IsValid)
            {
                return;
            }

            pawn.rotationTracker?.FaceTarget(targetCell);
            if (!Props.primaryFireSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayOneShotAt(Props.primaryFireSoundDefName, pawn.Position, pawn.Map);
            }

            ThingDef projectileDef = ABY_DefCache.ThingDefNamed(Props.directProjectileDefName);
            if (projectileDef == null)
            {
                return;
            }

            Projectile projectile = GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map, WipeMode.Vanish) as Projectile;
            if (projectile == null)
            {
                return;
            }

            if (!TryLaunchProjectile(projectile, pawn, new LocalTargetInfo(target)))
            {
                projectile.Destroy(DestroyMode.Vanish);
            }
        }

        private void FireBarrageShot(Pawn pawn, Thing target, IntVec3 fallbackCell)
        {
            if (pawn == null || pawn.Map == null)
            {
                return;
            }

            IntVec3 anchorCell = fallbackCell;
            if (target != null && target.Spawned && target.Map == pawn.Map)
            {
                if (!(target is Pawn pawnTarget) || !pawnTarget.Dead)
                {
                    anchorCell = target.PositionHeld;
                }
            }

            IntVec3 targetCell = ResolveBarrageCell(anchorCell, pawn.Map);
            if (!targetCell.IsValid)
            {
                targetCell = anchorCell;
            }

            if (!targetCell.IsValid)
            {
                return;
            }

            currentTargetCell = targetCell;
            pawn.rotationTracker?.FaceTarget(targetCell);
            if (!Props.barrageFireSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayOneShotAt(Props.barrageFireSoundDefName, pawn.Position, pawn.Map);
            }

            ThingDef projectileDef = ABY_DefCache.ThingDefNamed(Props.barrageProjectileDefName);
            if (projectileDef == null)
            {
                return;
            }

            Projectile projectile = GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map, WipeMode.Vanish) as Projectile;
            if (projectile == null)
            {
                return;
            }

            if (!TryLaunchProjectile(projectile, pawn, new LocalTargetInfo(targetCell)))
            {
                projectile.Destroy(DestroyMode.Vanish);
            }
        }

        private IntVec3 ResolveBarrageCell(IntVec3 anchorCell, Map map)
        {
            if (map == null || !anchorCell.IsValid)
            {
                return IntVec3.Invalid;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            int candidateCount = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(anchorCell, Props.barrageScatterRadius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                candidateCount++;
                if (Rand.Chance(1f / candidateCount))
                {
                    bestCell = cell;
                }
            }

            return bestCell.IsValid ? bestCell : anchorCell;
        }

        private bool TryLaunchProjectile(Projectile projectile, Pawn pawn, LocalTargetInfo targetInfo)
        {
            MethodInfo[] methods = projectile.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.Name == "Launch")
                .OrderByDescending(m => m.GetParameters().Length)
                .ToArray();

            for (int i = 0; i < methods.Length; i++)
            {
                if (!TryBuildLaunchArgs(methods[i], pawn, targetInfo, out object[] args))
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

        private bool TryBuildLaunchArgs(MethodInfo method, Pawn pawn, LocalTargetInfo targetInfo, out object[] args)
        {
            ParameterInfo[] parameters = method.GetParameters();
            args = new object[parameters.Length];
            int thingSlot = 0;
            IntVec3 targetCell = targetInfo.Cell.IsValid
                ? targetInfo.Cell
                : (targetInfo.Thing != null ? targetInfo.Thing.PositionHeld : IntVec3.Invalid);
            TargetInfo mapTarget = new TargetInfo(targetCell, pawn.Map);

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

                if (parameterType == typeof(TargetInfo))
                {
                    args[i] = mapTarget;
                    continue;
                }

                if (parameterType == typeof(IntVec3))
                {
                    if (!targetCell.IsValid)
                    {
                        args = null;
                        return false;
                    }

                    args[i] = targetCell;
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

        private void SetTargetLock(int ticksGame, int attackMode)
        {
            int lockDuration = attackMode == AttackModeBarrage ? 120 : 90;
            targetLockUntilTick = ticksGame + lockDuration;
        }

        private Pawn FindReplacementPawnNearCell(Pawn pawn, IntVec3 center, float radius)
        {
            if (pawn?.Map == null || !center.IsValid)
            {
                return null;
            }

            Pawn best = null;
            float bestDistance = float.MaxValue;
            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns == null)
            {
                return null;
            }

            float radiusSq = radius * radius;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                if ((candidate.Position - center).LengthHorizontalSquared > radiusSq)
                {
                    continue;
                }

                float shooterDistance = pawn.Position.DistanceTo(candidate.Position);
                if (shooterDistance > Props.range + 0.5f)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.Position, candidate.Position, pawn.Map))
                {
                    continue;
                }

                float centerDistance = candidate.Position.DistanceToSquared(center);
                if (centerDistance < bestDistance)
                {
                    bestDistance = centerDistance;
                    best = candidate;
                }
            }

            return best;
        }

        private bool TryForceCloseQuartersEngagement(Pawn pawn)
        {
            Pawn adjacentThreat = FindAdjacentThreat(pawn, 2.1f);
            if (adjacentThreat == null)
            {
                return false;
            }

            if (pawn.CurJob != null
                && pawn.CurJob.def == JobDefOf.AttackMelee
                && pawn.CurJob.targetA.IsValid
                && pawn.CurJob.targetA.Thing == adjacentThreat)
            {
                return true;
            }

            pawn.jobs?.StartJob(JobMaker.MakeJob(JobDefOf.AttackMelee, adjacentThreat), JobCondition.InterruptForced, null, false, true);
            pawn.rotationTracker?.FaceTarget(adjacentThreat.Position);
            return true;
        }

        private Pawn FindAdjacentThreat(Pawn pawn, float maxDistance)
        {
            if (pawn?.Map == null)
            {
                cachedAdjacentThreatTick = -1;
                cachedAdjacentThreat = null;
                cachedAdjacentThreatPosition = IntVec3.Invalid;
                return null;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (cachedAdjacentThreatTick == ticksGame
                && cachedAdjacentThreatPosition == pawn.Position
                && Mathf.Abs(cachedAdjacentThreatMaxDistance - maxDistance) <= 0.01f)
            {
                return cachedAdjacentThreat;
            }

            Pawn best = null;
            float bestDistanceSq = (maxDistance + 0.01f) * (maxDistance + 0.01f);
            IReadOnlyList<Pawn> pawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn candidate = pawns[i];
                    if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                    {
                        continue;
                    }

                    float distanceSq = (candidate.Position - pawn.Position).LengthHorizontalSquared;
                    if (distanceSq > bestDistanceSq)
                    {
                        continue;
                    }

                    bestDistanceSq = distanceSq;
                    best = candidate;
                }
            }

            cachedAdjacentThreatTick = ticksGame;
            cachedAdjacentThreat = best;
            cachedAdjacentThreatMaxDistance = maxDistance;
            cachedAdjacentThreatPosition = pawn.Position;
            return best;
        }
        private bool TryForceAdjacentBuildingBash(Pawn pawn)
        {
            if (pawn?.Map == null)
            {
                return false;
            }

            Building building = FindNearbyBlockingBuilding(pawn);
            if (building == null || !building.Spawned)
            {
                return false;
            }

            pawn.jobs?.StartJob(JobMaker.MakeJob(JobDefOf.AttackMelee, building), JobCondition.InterruptForced, null, false, true);
            return true;
        }

        private void TryApplyStructureCrushBonus(Pawn pawn, int ticksGame)
        {
            if (pawn?.Map == null || ticksGame < nextStructureCrushTick)
            {
                return;
            }

            if (pawn.CurJob == null || pawn.CurJob.def != JobDefOf.AttackMelee)
            {
                return;
            }

            Building building = pawn.CurJob.targetA.Thing as Building;
            if (!IsValidStructureTarget(building))
            {
                return;
            }

            if (!pawn.Position.AdjacentTo8WayOrInside(building.Position))
            {
                return;
            }

            nextStructureCrushTick = ticksGame + 22;
            building.TakeDamage(new DamageInfo(
                DamageDefOf.Blunt,
                85f,
                3.4f,
                -1f,
                pawn,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown));

            FleckMaker.ThrowMicroSparks(building.DrawPos, pawn.Map);
        }

        private static bool IsValidStructureTarget(Building building)
        {
            return building != null
                && building.Spawned
                && !building.Destroyed
                && building.def != null
                && building.def.useHitPoints;
        }

        private int GetWarmupTicksForCurrentMode()
        {
            int baseTicks = currentAttackMode == AttackModeBarrage ? Props.barrageWarmupTicks : Props.primaryWarmupTicks;
            float factor = warmupFactor;
            if (CollapseWindowActive && currentAttackMode == AttackModePrimary)
            {
                factor *= 1.15f;
            }

            return Math.Max(1, Mathf.RoundToInt(baseTicks * factor));
        }

        private int GetShotCountForCurrentMode()
        {
            if (currentAttackMode == AttackModeBarrage)
            {
                return Math.Max(1, Props.barrageShotCount + barrageShotBonus);
            }

            return Math.Max(1, Props.primaryBurstShotCount);
        }

        private int GetTicksBetweenShotsForCurrentMode()
        {
            return currentAttackMode == AttackModeBarrage
                ? Math.Max(1, Props.ticksBetweenBarrageShots)
                : Math.Max(1, Props.ticksBetweenPrimaryShots);
        }

        private void FinalizeAttackCycle(int ticksGame)
        {
            if (currentAttackMode == AttackModeBarrage)
            {
                nextBarrageReadyTick = ticksGame + Math.Max(1, Mathf.RoundToInt(Props.barrageCooldownTicks * barrageCooldownFactor));
            }
            else if (currentAttackMode == AttackModePrimary)
            {
                nextPrimaryReadyTick = ticksGame + Math.Max(1, Mathf.RoundToInt(Props.primaryCooldownTicks * primaryCooldownFactor));
            }

            ResetAttackState();
        }

        private void ResetAttackState()
        {
            currentTarget = null;
            currentTargetCell = IntVec3.Invalid;
            targetLockUntilTick = -1;
            currentAttackMode = AttackModeNone;
            warmupCompleteTick = -1;
            nextBurstShotTick = -1;
            burstShotsRemaining = 0;
        }
    }
}
