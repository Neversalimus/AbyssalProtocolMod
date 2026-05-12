using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_HeartGuardianLeash : CompProperties
    {
        public string heartDefName = "ABY_DominionSliceHeart";
        public List<string> anchorDefNames = new List<string>
        {
            "ABY_DominionSliceAnchor_Seal",
            "ABY_DominionSliceAnchor_Choir",
            "ABY_DominionSliceAnchor_Law"
        };

        public int scanIntervalTicks = 45;
        public float defendRadius = 10.5f;
        public float leashDistance = 8.0f;
        public float hardLeashDistance = 13.5f;
        public float returnRadiusMin = 2.0f;
        public float returnRadiusMax = 5.5f;
        public int interceptJobExpiryTicks = 90;
        public int returnJobExpiryTicks = 90;
        public bool preferRangedTargets = true;
        public bool defendNearestAnchorBeforeHeartExposed = true;
        public bool allowMeleeIntercept = true;

        public CompProperties_ABY_HeartGuardianLeash()
        {
            compClass = typeof(CompABY_HeartGuardianLeash);
        }
    }

    public class CompABY_HeartGuardianLeash : ThingComp
    {
        private Thing currentFocus;
        private Pawn currentThreat;

        public CompProperties_ABY_HeartGuardianLeash Props => (CompProperties_ABY_HeartGuardianLeash)props;

        private Pawn PawnParent => parent as Pawn;

        public override void CompTick()
        {
            base.CompTick();

            try
            {
                TickLeashSafe();
            }
            catch (System.Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "aortic-leash-tick-failed",
                    "[Abyssal Protocol] Aortic Chain Harrower leash tick failed and was skipped: " + ex.GetType().Name + ": " + ex.Message,
                    1200);
            }
        }

        private void TickLeashSafe()
        {
            Pawn pawn = PawnParent;
            if (!ShouldOperate(pawn) || !parent.IsHashIntervalTick(Mathf.Max(12, Props.scanIntervalTicks)))
            {
                return;
            }

            currentFocus = ResolveDefendFocus(pawn);
            if (currentFocus == null)
            {
                currentThreat = null;
                return;
            }

            float distanceToFocus = pawn.PositionHeld.DistanceTo(currentFocus.PositionHeld);
            if (distanceToFocus > Props.hardLeashDistance)
            {
                if (TryFindReturnCell(pawn, currentFocus, out IntVec3 emergencyCell))
                {
                    ForceReturnJob(pawn, emergencyCell, true);
                }

                currentThreat = null;
                return;
            }

            if (distanceToFocus > Props.leashDistance)
            {
                if (TryFindReturnCell(pawn, currentFocus, out IntVec3 returnCell))
                {
                    ForceReturnJob(pawn, returnCell, false);
                }

                currentThreat = null;
                return;
            }

            if (!Props.allowMeleeIntercept)
            {
                currentThreat = null;
                return;
            }

            currentThreat = FindThreatNearFocus(pawn, currentFocus);
            if (currentThreat != null)
            {
                EnsureInterceptJob(pawn, currentThreat);
            }
        }

        private Thing ResolveDefendFocus(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return null;
            }

            MapComponent_DominionSliceEncounter encounter = map.GetComponent<MapComponent_DominionSliceEncounter>();
            if (Props.defendNearestAnchorBeforeHeartExposed && encounter != null && encounter.IsActiveEncounter && !encounter.IsHeartExposed)
            {
                Thing anchor = ResolveNearestLiveAnchor(pawn, map);
                if (IsValidFocus(pawn, anchor))
                {
                    return anchor;
                }
            }

            Thing heart = encounter != null ? encounter.HeartBuilding : null;
            if (IsValidFocus(pawn, heart))
            {
                return heart;
            }

            if (IsValidFocus(pawn, currentFocus))
            {
                return currentFocus;
            }

            return ResolveNearestHeart(pawn, map);
        }

        private Thing ResolveNearestLiveAnchor(Pawn pawn, Map map)
        {
            if (pawn == null || map?.listerThings == null || Props.anchorDefNames == null || Props.anchorDefNames.Count == 0)
            {
                return null;
            }

            Thing best = null;
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

                List<Thing> candidates = map.listerThings.ThingsOfDef(def);
                if (candidates == null)
                {
                    continue;
                }

                for (int j = 0; j < candidates.Count; j++)
                {
                    Thing candidate = candidates[j];
                    if (!IsValidFocus(pawn, candidate))
                    {
                        continue;
                    }

                    float distance = pawn.PositionHeld.DistanceTo(candidate.PositionHeld);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = candidate;
                    }
                }
            }

            return best;
        }

        private Thing ResolveNearestHeart(Pawn pawn, Map map)
        {
            if (pawn == null || map?.listerThings == null || Props.heartDefName.NullOrEmpty())
            {
                return null;
            }

            ThingDef heartDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.heartDefName);
            if (heartDef == null)
            {
                return null;
            }

            List<Thing> hearts = map.listerThings.ThingsOfDef(heartDef);
            if (hearts == null)
            {
                return null;
            }

            Thing best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hearts.Count; i++)
            {
                Thing candidate = hearts[i];
                if (!IsValidFocus(pawn, candidate))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceTo(candidate.PositionHeld);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private Pawn FindThreatNearFocus(Pawn pawn, Thing focus)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null || focus == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = pawn.MapHeld.mapPawns.AllPawnsSpawned;
            Pawn best = null;
            float bestScore = float.MinValue;
            IntVec3 focusCell = focus.PositionHeld;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float focusDistance = focusCell.DistanceTo(candidate.PositionHeld);
                if (focusDistance > Props.defendRadius)
                {
                    continue;
                }

                float pawnDistance = pawn.PositionHeld.DistanceTo(candidate.PositionHeld);
                float score = (Props.defendRadius - focusDistance) * 4.0f;
                score -= pawnDistance * 0.35f;

                if (Props.preferRangedTargets && AbyssalThreatPawnUtility.HasRangedWeapon(candidate))
                {
                    score += 4.5f;
                }

                if (candidate == currentThreat)
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
            if (pawn?.jobs == null || target == null || !target.Spawned)
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
            attackJob.expiryInterval = Mathf.Max(45, Props.interceptJobExpiryTicks);
            attackJob.checkOverrideOnExpire = true;
            attackJob.collideWithPawns = true;
            attackJob.canBashDoors = true;
            pawn.jobs.TryTakeOrderedJob(attackJob, JobTag.Misc);
        }

        private void ForceReturnJob(Pawn pawn, IntVec3 cell, bool interruptCurrent)
        {
            if (pawn?.jobs == null || !cell.IsValid)
            {
                return;
            }

            Job currentJob = pawn.CurJob;
            if (currentJob != null && currentJob.def == JobDefOf.Goto && currentJob.targetA.Cell == cell)
            {
                return;
            }

            Job goJob = JobMaker.MakeJob(JobDefOf.Goto, cell);
            goJob.expiryInterval = Mathf.Max(45, Props.returnJobExpiryTicks);
            goJob.checkOverrideOnExpire = true;
            goJob.collideWithPawns = false;
            goJob.locomotionUrgency = LocomotionUrgency.Sprint;

            if (interruptCurrent)
            {
                pawn.jobs.StartJob(goJob, JobCondition.InterruptForced, null, false, true);
            }
            else
            {
                pawn.jobs.TryTakeOrderedJob(goJob, JobTag.Misc);
            }
        }

        private bool TryFindReturnCell(Pawn pawn, Thing focus, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            Map map = pawn?.MapHeld;
            if (map == null || focus == null)
            {
                return false;
            }

            float bestScore = float.MinValue;
            IntVec3 center = focus.PositionHeld;
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(center, Props.returnRadiusMax, true))
            {
                if (!candidate.InBounds(map) || !candidate.Standable(map) || AbyssalThreatPawnUtility.CellHasOtherPawn(candidate, map, pawn))
                {
                    continue;
                }

                float focusDistance = center.DistanceTo(candidate);
                if (focusDistance < Props.returnRadiusMin || focusDistance > Props.returnRadiusMax)
                {
                    continue;
                }

                float moveDistance = pawn.PositionHeld.DistanceTo(candidate);
                float score = (18f - moveDistance) + (focusDistance * 0.18f) + Rand.Value * 0.1f;
                if (score > bestScore)
                {
                    bestScore = score;
                    cell = candidate;
                }
            }

            return cell.IsValid;
        }

        private static bool IsValidFocus(Pawn pawn, Thing focus)
        {
            if (pawn == null || focus == null || focus.Destroyed || !focus.Spawned || focus.MapHeld != pawn.MapHeld)
            {
                return false;
            }

            return focus.def != null;
        }

        private static bool ShouldOperate(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed && pawn.Faction != null;
        }
    }
}
