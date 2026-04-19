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

        private float primaryCooldownFactor = 1f;
        private float barrageCooldownFactor = 1f;
        private float warmupFactor = 1f;
        private float barrageChanceBonus;
        private int barrageShotBonus;
        private int collapseWindowUntilTick = -1;

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
            Scribe_Values.Look(ref primaryCooldownFactor, "primaryCooldownFactor", 1f);
            Scribe_Values.Look(ref barrageCooldownFactor, "barrageCooldownFactor", 1f);
            Scribe_Values.Look(ref warmupFactor, "warmupFactor", 1f);
            Scribe_Values.Look(ref barrageChanceBonus, "barrageChanceBonus", 0f);
            Scribe_Values.Look(ref barrageShotBonus, "barrageShotBonus", 0);
            Scribe_Values.Look(ref collapseWindowUntilTick, "collapseWindowUntilTick", -1);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (!CanOperate(pawn))
            {
                ResetAttackState();
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (TryMaintainSpacing(pawn))
            {
                ResetAttackState();
                return;
            }

            if (burstShotsRemaining > 0)
            {
                if (!CanContinueCurrentAttack(pawn))
                {
                    ResetAttackState();
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
                return;
            }

            nextSearchTick = ticksGame + Math.Max(5, Props.scanIntervalTicks);
            Thing target = FindBestTargetThing(pawn);
            if (target == null)
            {
                TryForceAdjacentBuildingBash(pawn);
                return;
            }

            int attackMode = ResolveAttackMode(pawn, target, ticksGame);
            if (attackMode == AttackModeNone)
            {
                TryForceAdjacentBuildingBash(pawn);
                return;
            }

            currentAttackMode = attackMode;
            currentTarget = target;
            currentTargetCell = target.PositionHeld;
            warmupCompleteTick = ticksGame + GetWarmupTicksForCurrentMode();
            nextBurstShotTick = -1;
            burstShotsRemaining = 0;

            if (!Props.aimSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayAt(Props.aimSoundDefName, pawn.Position, pawn.Map);
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

        private Thing FindBestTargetThing(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
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
                    return CanFirePrimaryAt(pawn, currentTarget);
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
                ABY_SoundUtility.PlayAt(Props.primaryFireSoundDefName, pawn.Position, pawn.Map);
            }

            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.directProjectileDefName);
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
                ABY_SoundUtility.PlayAt(Props.barrageFireSoundDefName, pawn.Position, pawn.Map);
            }

            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.barrageProjectileDefName);
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
            currentAttackMode = AttackModeNone;
            warmupCompleteTick = -1;
            nextBurstShotTick = -1;
            burstShotsRemaining = 0;
        }
    }
}
