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
            if (launcher == null || target == null || projectileDef == null)
            {
                return false;
            }

            Map map = launcher.Map;
            if (map == null || !launcher.Spawned || launcher.Dead || launcher.Destroyed)
            {
                return false;
            }

            if (target.Destroyed || !target.Spawned || target.Map != map)
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

            LocalTargetInfo targetInfo = new LocalTargetInfo(target);
            try
            {
                projectile.Launch(
                    launcher,
                    launcher.DrawPos,
                    targetInfo,
                    targetInfo,
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
    }
}
