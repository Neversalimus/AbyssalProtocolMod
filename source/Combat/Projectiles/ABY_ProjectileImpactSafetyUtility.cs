using System;
using System.Reflection;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ProjectileImpactSafetyUtility
    {
        public static bool TryRunBaseImpact(Projectile projectile, string contextKey, Action baseImpact)
        {
            try
            {
                baseImpact?.Invoke();
                return true;
            }
            catch (TargetInvocationException ex)
            {
                HandleImpactException(projectile, contextKey, ex.InnerException ?? ex);
                return false;
            }
            catch (NullReferenceException ex)
            {
                HandleImpactException(projectile, contextKey, ex);
                return false;
            }
            catch (Exception ex)
            {
                HandleImpactException(projectile, contextKey, ex);
                return false;
            }
        }

        private static void HandleImpactException(Projectile projectile, string contextKey, Exception ex)
        {
            string key = "projectile-impact-safety-" + (contextKey ?? "unknown");
            ABY_LogThrottleUtility.Warning(
                key,
                "[Abyssal Protocol] Suppressed external combat-stack exception during " + (contextKey ?? "projectile") + " impact: " + ex.GetType().Name + ": " + ex.Message,
                2500);

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
