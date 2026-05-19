using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompABY_RiftSapperShooter : ThingComp
    {
        private int nextSearchTick;
        private int nextReadyTick;
        private int warmupCompleteTick = -1;
        private int nextWarmupTelegraphTick = -1;
        private Thing currentTarget;

        private CompProperties_ABY_RiftSapperShooter Props => (CompProperties_ABY_RiftSapperShooter)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextSearchTick, "nextSearchTick", 0);
            Scribe_Values.Look(ref nextReadyTick, "nextReadyTick", 0);
            Scribe_Values.Look(ref warmupCompleteTick, "warmupCompleteTick", -1);
            Scribe_Values.Look(ref nextWarmupTelegraphTick, "nextWarmupTelegraphTick", -1);
            Scribe_References.Look(ref currentTarget, "currentTarget");
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (!CanOperate(pawn))
            {
                ResetTargeting();
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (TryMaintainSpacing(pawn))
            {
                ResetTargeting();
                return;
            }

            if (TryPanicMelee(pawn))
            {
                ResetTargeting();
                return;
            }

            if (warmupCompleteTick >= 0)
            {
                if (!CanFireAt(pawn, currentTarget))
                {
                    ResetTargeting();
                    return;
                }

                if (ticksGame >= nextWarmupTelegraphTick)
                {
                    ShowTargetLockFX(pawn, currentTarget, false);
                    nextWarmupTelegraphTick = ticksGame + 12;
                }

                if (ticksGame >= warmupCompleteTick)
                {
                    FireShot(pawn, currentTarget);
                    currentTarget = null;
                    warmupCompleteTick = -1;
                    nextWarmupTelegraphTick = -1;
                    nextReadyTick = ticksGame + Math.Max(1, Props.cooldownTicks);
                }

                return;
            }

            if (ticksGame < nextReadyTick || ticksGame < nextSearchTick)
            {
                return;
            }

            nextSearchTick = ticksGame + Math.Max(5, Props.scanIntervalTicks);
            Thing target = FindBestTarget(pawn);
            if (target == null)
            {
                currentTarget = null;
                return;
            }

            currentTarget = target;
            warmupCompleteTick = ticksGame + Math.Max(1, Props.warmupTicks);
            nextWarmupTelegraphTick = ticksGame + 10;
            if (!Props.aimSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayChargeAt(Props.aimSoundDefName, pawn.Position, pawn.Map);
            }

            if (Props.holdPositionWhenTargeting)
            {
                pawn.pather?.StopDead();
            }

            pawn.rotationTracker?.FaceTarget(target.PositionHeld);
            ShowTargetLockFX(pawn, target, true);
        }

        private bool CanOperate(Pawn pawn)
        {
            return ABY_AbyssalRangedBrain.CanOperateHostilePawn(pawn);
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
                IReadOnlyList<Building> buildings = ABY_RuntimeTargetCache.CombatTargetBuildingsFor(pawn.Map);
                for (int i = 0; i < buildings.Count; i++)
                {
                    Building building = buildings[i];
                    if (!CanConsiderBuildingTarget(pawn, building))
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

            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.CombatTargetPawnsFor(pawn.Map);
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

        private bool CanConsiderBuildingTarget(Pawn pawn, Building building)
        {
            if (building == null || !AbyssalThreatPawnUtility.IsValidHostileThingTarget(pawn, building))
            {
                return false;
            }

            if (!CanFireAt(pawn, building))
            {
                return false;
            }

            return IsTurretLike(building)
                || building is Building_Door
                || IsCoverLike(building)
                || building.def?.Fillage == FillCategory.Full
                || building.TryGetComp<CompPowerTrader>() != null;
        }

        private float ScoreBuildingTarget(Pawn pawn, Building building)
        {
            float distance = pawn.Position.DistanceTo(building.PositionHeld);
            float score = 36f - distance;

            if (Props.prioritizeTurrets && IsTurretLike(building))
            {
                score += 88f;
            }

            if (Props.prioritizeDoors && building is Building_Door)
            {
                score += 62f;
            }

            if (Props.prioritizeCover && IsCoverLike(building))
            {
                score += 54f;
            }

            if (building.def?.Fillage == FillCategory.Full)
            {
                score += 20f;
            }

            if (building.TryGetComp<CompPowerTrader>() != null)
            {
                score += 14f;
            }

            score += CountNearbyPlayerPawns(pawn.Map, building.PositionHeld, 4.9f) * 9f;
            score += CountAdjacentCoverTiles(pawn.Map, building.PositionHeld) * 3.5f;
            score += Mathf.Min(16f, Mathf.Max(0f, building.HitPoints) * 0.035f);
            return score;
        }

        private float ScorePawnTarget(Pawn pawn, Pawn target)
        {
            float distance = pawn.Position.DistanceTo(target.PositionHeld);
            float score = 18f - distance;
            if (Props.preferRangedTargets && AbyssalThreatPawnUtility.HasRangedWeapon(target))
            {
                score += 24f;
            }

            score += CountAdjacentCoverTiles(pawn.Map, target.PositionHeld) * 5.5f;
            if (distance >= Props.preferredMinRange)
            {
                score += 4f;
            }

            return score;
        }

        private int CountNearbyPlayerPawns(Map map, IntVec3 center, float radius)
        {
            if (map == null)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.CombatTargetPawnsFor(map);
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

        private int CountAdjacentCoverTiles(Map map, IntVec3 center)
        {
            if (map == null || !center.IsValid)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                IntVec3 cell = center + GenAdj.AdjacentCells[i];
                if (!cell.InBounds(map))
                {
                    continue;
                }

                var things = cell.GetThingList(map);
                for (int j = 0; j < things.Count; j++)
                {
                    if (things[j] is Building building && IsCoverLike(building))
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        private bool CanFireAt(Pawn shooter, Thing target)
        {
            return ABY_AbyssalRangedBrain.HasThingFireSolution(
                shooter,
                target,
                Mathf.Max(0f, Props.targetMinRange),
                Props.range,
                true);
        }

        private bool TryMaintainSpacing(Pawn pawn)
        {
            return AbyssalThreatPawnUtility.TryMaintainSpacing(
                pawn,
                currentTarget,
                Props.preferredMinRange,
                Props.retreatSearchRadius,
                Props.holdPositionWhenTargeting);
        }

        private bool TryPanicMelee(Pawn pawn)
        {
            if (pawn == null || pawn.jobs == null || Props.panicMeleeRange <= 0f)
            {
                return false;
            }

            Pawn nearestThreat = AbyssalThreatPawnUtility.FindClosestThreatWithin(pawn, Props.panicMeleeRange);
            if (nearestThreat == null)
            {
                return false;
            }

            if (AbyssalThreatPawnUtility.TryFindRetreatCell(
                pawn,
                nearestThreat,
                Props.preferredMinRange,
                Props.retreatSearchRadius,
                out IntVec3 retreatCell) && retreatCell.IsValid && retreatCell != pawn.Position)
            {
                return false;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.AttackMelee && pawn.CurJob.targetA.Thing == nearestThreat)
            {
                return true;
            }

            currentTarget = nearestThreat;
            pawn.rotationTracker?.FaceTarget(nearestThreat.Position);
            Job meleeJob = JobMaker.MakeJob(JobDefOf.AttackMelee, nearestThreat);
            meleeJob.expiryInterval = Math.Max(60, Props.panicMeleeJobExpiryTicks);
            meleeJob.checkOverrideOnExpire = true;
            meleeJob.collideWithPawns = true;
            pawn.jobs.TryTakeOrderedJob(meleeJob, JobTag.Misc);
            return true;
        }

        private void FireShot(Pawn pawn, Thing target)
        {
            if (pawn == null || target == null)
            {
                return;
            }

            if (!ABY_AbyssalRangedBrain.TryFireProjectile(
                pawn,
                target,
                Props.projectileDefName,
                Props.castSoundDefName,
                out _))
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(target.DrawPos, pawn.Map, target is Building ? 1.2f : 0.9f);
            FleckMaker.ThrowMicroSparks(target.DrawPos, pawn.Map);
        }

        private static bool IsTurretLike(Building building)
        {
            return AbyssalThreatPawnUtility.IsCombatTurretLikeBuilding(building);
        }

        private static bool IsCoverLike(Building building)
        {
            string defName = building?.def?.defName;
            if (defName.NullOrEmpty())
            {
                return false;
            }

            return defName.IndexOf("Sandbag", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Barricade", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Barrier", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Embrasure", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ShowTargetLockFX(Pawn pawn, Thing target, bool initial)
        {
            if (pawn?.Map == null || target == null || !target.Spawned || target.Map != pawn.Map || target.Destroyed)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(target.DrawPos, pawn.Map, initial ? 1.05f : 0.62f);
            if (initial)
            {
                FleckMaker.Static(target.PositionHeld, pawn.Map, FleckDefOf.ExplosionFlash, target is Building ? 0.96f : 0.75f);
            }
        }

        private void ResetTargeting()
        {
            currentTarget = null;
            warmupCompleteTick = -1;
            nextWarmupTelegraphTick = -1;
        }
    }
}
