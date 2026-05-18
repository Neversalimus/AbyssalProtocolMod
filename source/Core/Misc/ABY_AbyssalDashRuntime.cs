using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class ABY_AbyssalDashRuntime
    {
        public const string DefaultTrailMoteDefName = "ABY_Mote_ArchonDashTrail";
        public const string DefaultDashSoundDefName = "ABY_SigilChargePulse";

        public static bool TryStartDash(
            Pawn pawn,
            Pawn target,
            IntVec3 requestedLandingCell,
            string impactHediffDefName,
            int durationTicks,
            string trailMoteDefName,
            float trailMoteScale,
            string soundDefName,
            string reasonTag)
        {
            if (!CanDash(pawn, target, requestedLandingCell))
            {
                return false;
            }

            Map map = pawn.Map;
            MapComponent_ABY_AbyssalDashRuntime component = map.GetComponent<MapComponent_ABY_AbyssalDashRuntime>();
            if (component == null || component.IsPawnDashing(pawn))
            {
                return false;
            }

            if (!ValidateLandingCell(pawn, map, requestedLandingCell))
            {
                if (!AbyssalThreatPawnUtility.TryFindAdjacentLandingCell(pawn, target, out requestedLandingCell))
                {
                    return false;
                }
            }

            if (!ValidateLandingCell(pawn, map, requestedLandingCell))
            {
                return false;
            }

            durationTicks = Mathf.Clamp(durationTicks, 3, 45);
            trailMoteScale = Mathf.Max(0.1f, trailMoteScale);
            if (trailMoteDefName.NullOrEmpty())
            {
                trailMoteDefName = DefaultTrailMoteDefName;
            }
            if (soundDefName.NullOrEmpty())
            {
                soundDefName = DefaultDashSoundDefName;
            }

            pawn.pather?.StopDead();
            pawn.stances?.CancelBusyStanceSoft();
            pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, true, true);

            component.StartDash(new ABY_AbyssalDashInstance(
                pawn,
                target,
                pawn.Position,
                requestedLandingCell,
                impactHediffDefName,
                durationTicks,
                trailMoteDefName,
                trailMoteScale,
                soundDefName,
                reasonTag));
            return true;
        }

        public static bool IsDashing(Pawn pawn)
        {
            Map map = pawn?.Map;
            if (pawn == null || map == null)
            {
                return false;
            }

            MapComponent_ABY_AbyssalDashRuntime component = map.GetComponent<MapComponent_ABY_AbyssalDashRuntime>();
            return component != null && component.IsPawnDashing(pawn);
        }

        internal static bool ValidateLandingCell(Pawn pawn, Map map, IntVec3 cell)
        {
            return pawn != null
                && map != null
                && cell.IsValid
                && cell.InBounds(map)
                && cell.Standable(map)
                && !AbyssalThreatPawnUtility.CellHasOtherPawn(cell, map, pawn);
        }

        internal static bool TryCompleteDash(ABY_AbyssalDashInstance dash)
        {
            if (dash == null || dash.Pawn == null || dash.Map == null)
            {
                return false;
            }

            Pawn pawn = dash.Pawn;
            Map map = dash.Map;
            if (pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.Map != map)
            {
                return false;
            }

            IntVec3 landingCell = dash.LandingCell;
            Pawn target = dash.Target;
            if (!ValidateLandingCell(pawn, map, landingCell) && target != null)
            {
                AbyssalThreatPawnUtility.TryFindAdjacentLandingCell(pawn, target, out landingCell);
            }

            if (!ValidateLandingCell(pawn, map, landingCell))
            {
                return false;
            }

            SpawnTrailMote(map, pawn.Position, dash.TrailMoteDefName, dash.TrailMoteScale);

            Rot4 rotation = pawn.Rotation;
            if (target != null && target.Spawned)
            {
                rotation = Rot4.FromAngleFlat((target.DrawPos - landingCell.ToVector3Shifted()).AngleFlat());
            }

            pawn.pather?.StopDead();
            pawn.stances?.CancelBusyStanceSoft();
            pawn.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(pawn, landingCell, map, rotation, WipeMode.Vanish, false, false);
            pawn.Drawer?.tweener?.ResetTweenedPosToRoot();

            if (target != null && target.Spawned)
            {
                pawn.rotationTracker?.FaceCell(target.Position);
            }

            SpawnTrailMote(map, landingCell, dash.TrailMoteDefName, dash.TrailMoteScale);
            ABY_SoundUtility.PlayAt(dash.SoundDefName, landingCell, map);

            if (AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, target))
            {
                AbyssalThreatPawnUtility.ApplyOrRefreshHediff(target, dash.ImpactHediffDefName);
                TryQueueFollowupMelee(pawn, target);
            }

            return true;
        }

        internal static void SpawnTrailMote(Map map, IntVec3 cell, string moteDefName, float scale)
        {
            if (map == null || !cell.IsValid || moteDefName.NullOrEmpty())
            {
                return;
            }

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(moteDefName);
            if (moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(cell.ToVector3Shifted(), map, moteDef, Mathf.Max(0.1f, scale));
        }

        private static bool CanDash(Pawn pawn, Pawn target, IntVec3 requestedLandingCell)
        {
            if (pawn == null || target == null || !requestedLandingCell.IsValid)
            {
                return false;
            }

            return pawn.Spawned
                && target.Spawned
                && pawn.Map != null
                && pawn.Map == target.Map
                && !pawn.Dead
                && !pawn.Downed
                && !target.Dead
                && !target.Destroyed;
        }

        private static void TryQueueFollowupMelee(Pawn pawn, Pawn target)
        {
            if (pawn?.jobs == null || !AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, target))
            {
                return;
            }

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.expiryInterval = 90;
            job.checkOverrideOnExpire = true;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }

    public sealed class ABY_AbyssalDashInstance
    {
        public readonly Pawn Pawn;
        public readonly Pawn Target;
        public readonly IntVec3 SourceCell;
        public readonly IntVec3 LandingCell;
        public readonly string ImpactHediffDefName;
        public readonly int StartTick;
        public readonly int DurationTicks;
        public readonly string TrailMoteDefName;
        public readonly float TrailMoteScale;
        public readonly string SoundDefName;
        public readonly string ReasonTag;
        public readonly Map Map;

        public int AgeTicks => Find.TickManager.TicksGame - StartTick;
        public bool ShouldComplete => AgeTicks >= DurationTicks;

        public ABY_AbyssalDashInstance(
            Pawn pawn,
            Pawn target,
            IntVec3 sourceCell,
            IntVec3 landingCell,
            string impactHediffDefName,
            int durationTicks,
            string trailMoteDefName,
            float trailMoteScale,
            string soundDefName,
            string reasonTag)
        {
            Pawn = pawn;
            Target = target;
            SourceCell = sourceCell;
            LandingCell = landingCell;
            ImpactHediffDefName = impactHediffDefName;
            StartTick = Find.TickManager.TicksGame;
            DurationTicks = Mathf.Max(1, durationTicks);
            TrailMoteDefName = trailMoteDefName;
            TrailMoteScale = trailMoteScale;
            SoundDefName = soundDefName;
            ReasonTag = reasonTag ?? string.Empty;
            Map = pawn?.Map;
        }
    }
}
