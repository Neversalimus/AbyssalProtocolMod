using System;
using System.Reflection;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ProjectileImpactSafetyUtility
    {
        public static bool TryRunBaseImpact(Projectile projectile, string contextKey, Action baseImpact)
        {
            if (!IsProjectileReadyForBaseImpact(projectile, contextKey))
            {
                return false;
            }

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

        private static bool IsProjectileReadyForBaseImpact(Projectile projectile, string contextKey)
        {
            if (projectile == null)
            {
                ABY_LogThrottleUtility.Message(
                    "projectile-impact-safety-null-projectile-" + SafeContext(contextKey),
                    "[Abyssal Protocol] Skipped projectile base impact because the projectile reference was already null. Context: " + SafeContext(contextKey),
                    15000);
                return false;
            }

            try
            {
                if (projectile.Destroyed || projectile.Map == null)
                {
                    ABY_LogThrottleUtility.Message(
                        "projectile-impact-safety-invalid-projectile-" + SafeContext(contextKey),
                        "[Abyssal Protocol] Skipped projectile base impact because the projectile was already despawned/destroyed before impact resolution. Context: " + SafeContext(contextKey) + ", def=" + SafeProjectileDef(projectile),
                        15000);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Message(
                    "projectile-impact-safety-invalid-projectile-check-" + SafeContext(contextKey),
                    "[Abyssal Protocol] Skipped projectile base impact because projectile state could not be read safely. Context: " + SafeContext(contextKey) + ", exception=" + ex.GetType().Name,
                    15000);
                return false;
            }

            return true;
        }

        private static void HandleCombatException(string contextKey, string stageKey, Exception ex, Projectile projectile, bool destroyProjectileOnFailure)
        {
            string safeContext = SafeContext(contextKey);
            string safeStage = stageKey.NullOrEmpty() ? "impact" : stageKey;
            string key = "projectile-impact-safety-" + safeContext + "-" + safeStage;
            bool expectedExternalBaseImpactFailure = IsExpectedExternalBaseImpactFailure(safeStage, ex);
            string message = BuildExceptionMessage(safeContext, safeStage, ex, projectile, expectedExternalBaseImpactFailure);

            if (expectedExternalBaseImpactFailure)
            {
                ABY_LogThrottleUtility.Message(key, message, 15000);
            }
            else
            {
                ABY_LogThrottleUtility.Warning(key, message, 2500);
            }

            if (!destroyProjectileOnFailure)
            {
                return;
            }

            TryDestroyProjectile(projectile);
        }

        private static bool IsExpectedExternalBaseImpactFailure(string safeStage, Exception ex)
        {
            return string.Equals(safeStage, "base impact", StringComparison.OrdinalIgnoreCase)
                && ex is NullReferenceException;
        }

        private static string BuildExceptionMessage(string safeContext, string safeStage, Exception ex, Projectile projectile, bool expectedExternalBaseImpactFailure)
        {
            string prefix = expectedExternalBaseImpactFailure
                ? "[Abyssal Protocol] Suppressed non-fatal projectile base-impact exception from RimWorld/external combat stack"
                : "[Abyssal Protocol] Suppressed combat-stack exception";

            string exceptionName = ex == null ? "unknown" : ex.GetType().Name;
            string exceptionMessage = ex == null || ex.Message.NullOrEmpty() ? string.Empty : ": " + ex.Message;

            return prefix
                + " during " + safeContext + " " + safeStage
                + ". Projectile=" + SafeProjectileDef(projectile)
                + ", launcher=" + SafeLauncherDef(projectile)
                + ", exception=" + exceptionName + exceptionMessage
                + (expectedExternalBaseImpactFailure ? ". The projectile was safely removed and gameplay should continue." : string.Empty);
        }

        private static void TryDestroyProjectile(Projectile projectile)
        {
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

        private static string SafeContext(string contextKey)
        {
            return contextKey.NullOrEmpty() ? "projectile" : contextKey;
        }

        private static string SafeProjectileDef(Projectile projectile)
        {
            try
            {
                return projectile?.def?.defName ?? "null";
            }
            catch
            {
                return "unknown";
            }
        }

        private static string SafeLauncherDef(Projectile projectile)
        {
            try
            {
                return projectile?.Launcher?.def?.defName ?? "null";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
