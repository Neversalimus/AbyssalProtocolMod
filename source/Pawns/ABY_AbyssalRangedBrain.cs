using RimWorld;
using Verse;
using UnityEngine;

namespace AbyssalProtocol
{
    public static class ABY_AbyssalRangedBrain
    {
        public static bool CanOperateHostilePawn(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null || pawn.Dead || !pawn.Spawned || pawn.Downed)
            {
                return false;
            }

            return pawn.Faction != null
                && Faction.OfPlayer != null
                && ABY_FactionHostilityUtility.SafeHostileToPlayer(pawn);
        }

        public static bool HasPawnFireSolution(Pawn shooter, Thing target, float minRange, float maxRange)
        {
            Pawn targetPawn = target as Pawn;
            if (!AbyssalThreatPawnUtility.CanFireAt(shooter, targetPawn))
            {
                return false;
            }

            return IsInRange(shooter, targetPawn, minRange, maxRange);
        }

        public static bool HasThingFireSolution(Pawn shooter, Thing target, float minRange, float maxRange, bool requireLineOfSight = true)
        {
            if (!AbyssalThreatPawnUtility.IsValidHostileThingTarget(shooter, target))
            {
                return false;
            }

            if (!IsInRange(shooter, target, minRange, maxRange))
            {
                return false;
            }

            return !requireLineOfSight || GenSight.LineOfSight(shooter.PositionHeld, target.PositionHeld, shooter.Map);
        }

        public static bool TryFireProjectile(Pawn shooter, Thing target, string projectileDefName, string castSoundDefName, out Projectile projectile)
        {
            projectile = null;
            if (shooter == null || target == null)
            {
                return false;
            }

            shooter.rotationTracker?.FaceTarget(target.PositionHeld);
            if (!castSoundDefName.NullOrEmpty())
            {
                ABY_SoundUtility.PlayOneShotAt(castSoundDefName, shooter.PositionHeld, shooter.Map);
            }

            return ABY_AbyssalProjectileLaunchUtility.TrySpawnAndLaunch(
                shooter,
                target,
                projectileDefName,
                out projectile);
        }

        public static bool IsInRange(Pawn shooter, Thing target, float minRange, float maxRange)
        {
            if (shooter == null || target == null)
            {
                return false;
            }

            float distance = shooter.PositionHeld.DistanceTo(target.PositionHeld);
            if (distance > maxRange)
            {
                return false;
            }

            return distance >= Mathf.Max(0f, minRange);
        }
    }
}
