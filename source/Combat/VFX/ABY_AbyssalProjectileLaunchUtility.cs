using System;
using Verse;
using UnityEngine;

namespace AbyssalProtocol
{
    public static class ABY_AbyssalProjectileLaunchUtility
    {
        public static bool TrySpawnAndLaunch(
            Pawn launcher,
            Thing target,
            ThingDef projectileDef,
            out Projectile projectile,
            Thing equipment = null,
            ThingDef targetCoverDef = null,
            ProjectileHitFlags hitFlags = ProjectileHitFlags.IntendedTarget,
            bool preventFriendlyFire = false)
        {
            projectile = null;
            if (target == null)
            {
                return false;
            }

            return TrySpawnAndLaunch(
                launcher,
                new LocalTargetInfo(target),
                new LocalTargetInfo(target),
                projectileDef,
                out projectile,
                equipment,
                targetCoverDef,
                hitFlags,
                preventFriendlyFire);
        }

        public static bool TrySpawnAndLaunch(
            Pawn launcher,
            LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget,
            ThingDef projectileDef,
            out Projectile projectile,
            Thing equipment = null,
            ThingDef targetCoverDef = null,
            ProjectileHitFlags hitFlags = ProjectileHitFlags.IntendedTarget,
            bool preventFriendlyFire = false)
        {
            projectile = null;
            if (launcher == null || projectileDef == null || !usedTarget.IsValid || !intendedTarget.IsValid)
            {
                return false;
            }

            Map map = launcher.Map;
            if (map == null || !launcher.Spawned || launcher.Dead || launcher.Destroyed)
            {
                return false;
            }

            if (!TargetIsValidForMap(usedTarget, map) || !TargetIsValidForMap(intendedTarget, map))
            {
                return false;
            }

            if (!launcher.PositionHeld.IsValid || !launcher.PositionHeld.InBounds(map))
            {
                return false;
            }

            projectile = GenSpawn.Spawn(projectileDef, launcher.PositionHeld, map, WipeMode.Vanish) as Projectile;
            if (projectile == null)
            {
                return false;
            }

            try
            {
                projectile.Launch(
                    launcher,
                    launcher.DrawPos,
                    usedTarget,
                    intendedTarget,
                    hitFlags,
                    preventFriendlyFire,
                    equipment,
                    targetCoverDef);
                return true;
            }
            catch (Exception ex)
            {
                if (!projectile.Destroyed)
                {
                    projectile.Destroy(DestroyMode.Vanish);
                }

                Log.Warning("[Abyssal Protocol] Failed to launch abyssal projectile "
                    + projectileDef.defName
                    + " from "
                    + launcher.LabelShortCap
                    + ": "
                    + ex.GetType().Name
                    + " "
                    + ex.Message);
                projectile = null;
                return false;
            }
        }

        public static bool TrySpawnAndLaunch(
            Pawn launcher,
            IntVec3 targetCell,
            ThingDef projectileDef,
            out Projectile projectile,
            Thing equipment = null,
            ThingDef targetCoverDef = null,
            ProjectileHitFlags hitFlags = ProjectileHitFlags.IntendedTarget,
            bool preventFriendlyFire = false)
        {
            projectile = null;
            if (!targetCell.IsValid)
            {
                return false;
            }

            LocalTargetInfo targetInfo = new LocalTargetInfo(targetCell);
            return TrySpawnAndLaunch(
                launcher,
                targetInfo,
                targetInfo,
                projectileDef,
                out projectile,
                equipment,
                targetCoverDef,
                hitFlags,
                preventFriendlyFire);
        }

        public static bool TrySpawnAndLaunch(
            Pawn launcher,
            Thing target,
            string projectileDefName,
            out Projectile projectile,
            Thing equipment = null,
            ThingDef targetCoverDef = null,
            ProjectileHitFlags hitFlags = ProjectileHitFlags.IntendedTarget,
            bool preventFriendlyFire = false)
        {
            projectile = null;
            if (projectileDefName.NullOrEmpty())
            {
                return false;
            }

            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(projectileDefName);
            if (projectileDef == null)
            {
                Log.Warning("[Abyssal Protocol] Missing abyssal projectile ThingDef: " + projectileDefName);
                return false;
            }

            return TrySpawnAndLaunch(
                launcher,
                target,
                projectileDef,
                out projectile,
                equipment,
                targetCoverDef,
                hitFlags,
                preventFriendlyFire);
        }

        public static bool TrySpawnAndLaunch(
            Pawn launcher,
            IntVec3 targetCell,
            string projectileDefName,
            out Projectile projectile,
            Thing equipment = null,
            ThingDef targetCoverDef = null,
            ProjectileHitFlags hitFlags = ProjectileHitFlags.IntendedTarget,
            bool preventFriendlyFire = false)
        {
            projectile = null;
            if (projectileDefName.NullOrEmpty())
            {
                return false;
            }

            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail(projectileDefName);
            if (projectileDef == null)
            {
                Log.Warning("[Abyssal Protocol] Missing abyssal projectile ThingDef: " + projectileDefName);
                return false;
            }

            return TrySpawnAndLaunch(
                launcher,
                targetCell,
                projectileDef,
                out projectile,
                equipment,
                targetCoverDef,
                hitFlags,
                preventFriendlyFire);
        }

        private static bool TargetIsValidForMap(LocalTargetInfo target, Map map)
        {
            if (!target.IsValid || map == null)
            {
                return false;
            }

            if (target.HasThing)
            {
                Thing thing = target.Thing;
                return thing != null && !thing.Destroyed && thing.Spawned && thing.Map == map;
            }

            return target.Cell.IsValid && target.Cell.InBounds(map);
        }
    }
}
