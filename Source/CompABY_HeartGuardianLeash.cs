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
        public int scanIntervalTicks = 30;
        public float defendRadius = 13.0f;
        public float leashDistance = 15.5f;
        public float hardLeashDistance = 21.5f;
        public float returnRadiusMin = 5.0f;
        public float returnRadiusMax = 8.0f;
        public int interceptJobExpiryTicks = 110;
        public int returnJobExpiryTicks = 100;
        public bool preferRangedTargets = true;

        public CompProperties_ABY_HeartGuardianLeash()
        {
            compClass = typeof(CompABY_HeartGuardianLeash);
        }
    }

    public class CompABY_HeartGuardianLeash : ThingComp
    {
        private Thing currentHeart;
        private Pawn currentThreat;

        public CompProperties_ABY_HeartGuardianLeash Props => (CompProperties_ABY_HeartGuardianLeash)props;

        private Pawn PawnParent => parent as Pawn;

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperate(pawn) || !parent.IsHashIntervalTick(Mathf.Max(12, Props.scanIntervalTicks)))
            {
                return;
            }

            currentHeart = ResolveHeart(pawn);
            if (currentHeart == null)
            {
                currentThreat = null;
                return;
            }

            float distanceToHeart = pawn.PositionHeld.DistanceTo(currentHeart.PositionHeld);
            if (distanceToHeart > Props.hardLeashDistance)
            {
                if (TryFindReturnCell(pawn, currentHeart, out IntVec3 emergencyCell))
                {
                    ForceReturnJob(pawn, emergencyCell, true);
                }

                currentThreat = null;
                return;
            }

            if (distanceToHeart > Props.leashDistance)
            {
                if (TryFindReturnCell(pawn, currentHeart, out IntVec3 returnCell))
                {
                    ForceReturnJob(pawn, returnCell, false);
                }

                currentThreat = null;
                return;
            }

            currentThreat = FindThreatNearHeart(pawn, currentHeart);
            if (currentThreat != null)
            {
                EnsureInterceptJob(pawn, currentThreat);
            }
        }

        private Thing ResolveHeart(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return null;
            }

            MapComponent_DominionSliceEncounter encounter = map.GetComponent<MapComponent_DominionSliceEncounter>();
            Thing heart = encounter?.HeartBuilding;
            if (IsValidHeart(pawn, heart))
            {
                return heart;
            }

            if (IsValidHeart(pawn, currentHeart))
            {
                return currentHeart;
            }

            if (Props.heartDefName.NullOrEmpty())
            {
                return null;
            }

            ThingDef heartDef = DefDatabase<ThingDef>.GetNamedSilentFail(Props.heartDefName);
            if (heartDef == null || map.listerThings == null)
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
                if (!IsValidHeart(pawn, candidate))
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

        private Pawn FindThreatNearHeart(Pawn pawn, Thing heart)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null || heart == null)
            {
                return null;
            }

            IReadOnlyList<Pawn> pawns = pawn.MapHeld.mapPawns.AllPawnsSpawned;
            Pawn best = null;
            float bestScore = float.MinValue;
            IntVec3 heartCell = heart.PositionHeld;

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn candidate = pawns[i];
                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, candidate))
                {
                    continue;
                }

                float heartDistance = heartCell.DistanceTo(candidate.PositionHeld);
                if (heartDistance > Props.defendRadius)
                {
                    continue;
                }

                float pawnDistance = pawn.PositionHeld.DistanceTo(candidate.PositionHeld);
                float score = (Props.defendRadius - heartDistance) * 4.0f;
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

        private bool TryFindReturnCell(Pawn pawn, Thing heart, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            Map map = pawn?.MapHeld;
            if (map == null || heart == null)
            {
                return false;
            }

            float bestScore = float.MinValue;
            IntVec3 center = heart.PositionHeld;
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(center, Props.returnRadiusMax, true))
            {
                if (!candidate.InBounds(map) || !candidate.Standable(map) || AbyssalThreatPawnUtility.CellHasOtherPawn(candidate, map, pawn))
                {
                    continue;
                }

                float heartDistance = center.DistanceTo(candidate);
                if (heartDistance < Props.returnRadiusMin || heartDistance > Props.returnRadiusMax)
                {
                    continue;
                }

                float moveDistance = pawn.PositionHeld.DistanceTo(candidate);
                float score = (12f - moveDistance) + (heartDistance * 0.35f) + Rand.Value * 0.1f;
                if (score > bestScore)
                {
                    bestScore = score;
                    cell = candidate;
                }
            }

            return cell.IsValid;
        }

        private static bool IsValidHeart(Pawn pawn, Thing heart)
        {
            if (pawn == null || heart == null || heart.Destroyed || !heart.Spawned || heart.MapHeld != pawn.MapHeld)
            {
                return false;
            }

            return heart.def != null && heart.def.defName == "ABY_DominionSliceHeart";
        }

        private static bool ShouldOperate(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed && pawn.Faction != null;
        }
    }
}
