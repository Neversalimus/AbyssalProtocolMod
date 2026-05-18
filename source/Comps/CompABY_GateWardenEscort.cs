using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompABY_GateWardenEscort : ThingComp
    {
        private Thing currentAnchor;
        private Pawn currentInterceptTarget;
        private bool hasAnchorThreat;

        public CompProperties_ABY_GateWardenEscort Props => (CompProperties_ABY_GateWardenEscort)props;

        private Pawn PawnParent => parent as Pawn;

        public bool HasAnchorThreatNow => hasAnchorThreat;

        public Thing CurrentAnchor => currentAnchor;

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || !parent.IsHashIntervalTick(Mathf.Max(15, Props.scanIntervalTicks)))
            {
                return;
            }

            currentAnchor = ResolveAnchor(pawn);
            if (currentAnchor == null)
            {
                currentInterceptTarget = null;
                hasAnchorThreat = false;
                return;
            }

            currentInterceptTarget = FindThreatNearAnchor(pawn, currentAnchor);
            hasAnchorThreat = currentInterceptTarget != null;

            if (currentInterceptTarget != null)
            {
                AbyssalThreatPawnUtility.ApplyOrRefreshHediff(pawn, Props.rushHediffDefName, Props.rushSeverity);
                EnsureInterceptJob(pawn, currentInterceptTarget);
                return;
            }

            if (AbyssalThreatPawnUtility.FindClosestThreatWithin(pawn, 2.1f) != null)
            {
                return;
            }

            if (pawn.PositionHeld.DistanceTo(currentAnchor.PositionHeld) > Props.leashDistance
                && TryFindEscortCell(pawn, currentAnchor, out IntVec3 escortCell))
            {
                EnsureReturnJob(pawn, escortCell);
            }
        }

        private Thing ResolveAnchor(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null || Props.anchorDefNames == null || Props.anchorDefNames.Count == 0)
            {
                return null;
            }

            float extendedRadius = Mathf.Max(Props.anchorSearchRadius * 1.85f, Props.anchorSearchRadius + 10f);
            if (IsValidAnchor(pawn, currentAnchor, extendedRadius))
            {
                return currentAnchor;
            }

            Thing bestAnchor = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < Props.anchorDefNames.Count; i++)
            {
                string defName = Props.anchorDefNames[i];
                if (defName.NullOrEmpty())
                {
                    continue;
                }

                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (def == null)
                {
                    continue;
                }

                List<Thing> candidates = map.listerThings?.ThingsOfDef(def);
                if (candidates == null)
                {
                    continue;
                }

                for (int j = 0; j < candidates.Count; j++)
                {
                    Thing candidate = candidates[j];
                    if (!IsValidAnchor(pawn, candidate, Props.anchorSearchRadius))
                    {
                        continue;
                    }

                    float distance = pawn.PositionHeld.DistanceTo(candidate.PositionHeld);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestAnchor = candidate;
                    }
                }
            }

            return bestAnchor;
        }

        private Pawn FindThreatNearAnchor(Pawn pawn, Thing anchor)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null || anchor == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = pawn.MapHeld.mapPawns.AllPawnsSpawned;
            Pawn best = null;
            float bestScore = float.MinValue;
            IntVec3 anchorCell = anchor.PositionHeld;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float anchorDistance = anchorCell.DistanceTo(candidate.PositionHeld);
                if (anchorDistance > Props.defendRadius)
                {
                    continue;
                }

                float score = (Props.defendRadius - anchorDistance) * 4f;
                if (AbyssalThreatPawnUtility.HasRangedWeapon(candidate))
                {
                    score += 2.8f;
                }

                score -= pawn.PositionHeld.DistanceTo(candidate.PositionHeld) * 0.35f;
                if (candidate == currentInterceptTarget)
                {
                    score += 1.4f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private void EnsureInterceptJob(Pawn pawn, Pawn target)
        {
            if (pawn?.jobs == null || target == null)
            {
                return;
            }

            Job currentJob = pawn.CurJob;
            if (currentJob != null && currentJob.def == JobDefOf.AttackMelee && currentJob.targetA.Thing == target)
            {
                return;
            }

            pawn.rotationTracker?.FaceTarget(target.PositionHeld);
            Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            attackJob.expiryInterval = Mathf.Max(60, Props.interceptJobExpiryTicks);
            attackJob.checkOverrideOnExpire = true;
            attackJob.collideWithPawns = true;
            attackJob.canBashDoors = true;
            pawn.jobs.TryTakeOrderedJob(attackJob, JobTag.Misc);
        }

        private void EnsureReturnJob(Pawn pawn, IntVec3 escortCell)
        {
            if (pawn?.jobs == null || !escortCell.IsValid)
            {
                return;
            }

            Job currentJob = pawn.CurJob;
            if (currentJob != null && currentJob.def == JobDefOf.Goto && currentJob.targetA.Cell == escortCell)
            {
                return;
            }

            Job goJob = JobMaker.MakeJob(JobDefOf.Goto, escortCell);
            goJob.expiryInterval = Mathf.Max(45, Props.returnJobExpiryTicks);
            goJob.checkOverrideOnExpire = true;
            goJob.collideWithPawns = false;
            goJob.locomotionUrgency = LocomotionUrgency.Sprint;
            pawn.jobs.TryTakeOrderedJob(goJob, JobTag.Misc);
        }

        private bool TryFindEscortCell(Pawn pawn, Thing anchor, out IntVec3 escortCell)
        {
            escortCell = IntVec3.Invalid;
            Map map = pawn?.MapHeld;
            if (map == null || anchor == null)
            {
                return false;
            }

            float bestScore = float.MinValue;
            IntVec3 center = anchor.PositionHeld;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, Props.returnRadiusMax, true))
            {
                if (!cell.InBounds(map) || !cell.Standable(map) || AbyssalThreatPawnUtility.CellHasOtherPawn(cell, map, pawn))
                {
                    continue;
                }

                float anchorDistance = center.DistanceTo(cell);
                if (anchorDistance < Props.returnRadiusMin || anchorDistance > Props.returnRadiusMax)
                {
                    continue;
                }

                float moveDistance = pawn.PositionHeld.DistanceTo(cell);
                float score = (8f - moveDistance) + (anchorDistance * 0.4f);
                if (score > bestScore)
                {
                    bestScore = score;
                    escortCell = cell;
                }
            }

            return escortCell.IsValid;
        }

        private static bool IsValidAnchor(Pawn pawn, Thing anchor, float maxDistance)
        {
            if (pawn == null || anchor == null || anchor.Destroyed || !anchor.Spawned || anchor.MapHeld != pawn.MapHeld)
            {
                return false;
            }

            if (anchor == pawn)
            {
                return false;
            }

            if (anchor is Pawn anchorPawn && (anchorPawn.Dead || anchorPawn.Downed))
            {
                return false;
            }

            if (anchor.Faction != null && pawn.Faction != null && anchor.Faction != pawn.Faction)
            {
                return false;
            }

            return pawn.PositionHeld.DistanceTo(anchor.PositionHeld) <= maxDistance;
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed && pawn.Faction != null;
        }
    }
}
