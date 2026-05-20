using System;
using System.Reflection;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ProjectileImpactSafetyUtility
    {
        public static bool TryRunBaseImpact(Projectile projectile, string contextKey, Action baseImpact)
        {
            return TryRunImpactAction(projectile, contextKey, "base impact", baseImpact, destroyProjectileOnFailure: true);
        }

        public static bool TryRunPostImpactAction(Projectile projectile, string contextKey, string stageKey, Action action)
        {
            return TryRunImpactAction(projectile, contextKey, stageKey, action, destroyProjectileOnFailure: false);
        }

        public static bool TryApplyDamage(Thing target, DamageInfo damageInfo, string contextKey)
        {
            if (target == null || target.Destroyed)
            {
                return false;
            }

            try
            {
                target.TakeDamage(damageInfo);
                return true;
            }
            catch (TargetInvocationException ex)
            {
                HandleCombatException(contextKey, "direct damage", ex.InnerException ?? ex, null, destroyProjectileOnFailure: false);
                return false;
            }
            catch (NullReferenceException ex)
            {
                HandleCombatException(contextKey, "direct damage", ex, null, destroyProjectileOnFailure: false);
                return false;
            }
            catch (Exception ex)
            {
                HandleCombatException(contextKey, "direct damage", ex, null, destroyProjectileOnFailure: false);
                return false;
            }
        }

        public static bool TryRunCombatAction(string contextKey, string stageKey, Action action)
        {
            return TryRunImpactAction(null, contextKey, stageKey, action, destroyProjectileOnFailure: false);
        }

        private static bool TryRunImpactAction(Projectile projectile, string contextKey, string stageKey, Action action, bool destroyProjectileOnFailure)
        {
            try
            {
                action?.Invoke();
                return true;
            }
            catch (TargetInvocationException ex)
            {
                HandleCombatException(contextKey, stageKey, ex.InnerException ?? ex, projectile, destroyProjectileOnFailure);
                return false;
            }
            catch (NullReferenceException ex)
            {
                HandleCombatException(contextKey, stageKey, ex, projectile, destroyProjectileOnFailure);
                return false;
            }
            catch (Exception ex)
            {
                HandleCombatException(contextKey, stageKey, ex, projectile, destroyProjectileOnFailure);
                return false;
            }
        }

        private static void HandleCombatException(string contextKey, string stageKey, Exception ex, Projectile projectile, bool destroyProjectileOnFailure)
        {
            string safeContext = contextKey.NullOrEmpty() ? "projectile" : contextKey;
            string safeStage = stageKey.NullOrEmpty() ? "impact" : stageKey;
            string key = "projectile-impact-safety-" + safeContext + "-" + safeStage;

            ABY_LogThrottleUtility.Warning(
                key,
                "[Abyssal Protocol] Suppressed external combat-stack exception during " + safeContext + " " + safeStage + ": " + ex.GetType().Name + ": " + ex.Message,
                2500);

            if (!destroyProjectileOnFailure)
            {
                return;
            }

            try
            {
                if (projectile != null && !projectile.Destroyed)
                {
                    projectile.Destroy(DestroyMode.Vanish);
                }
            }
            catch
            {
            }
        }
    }
}
