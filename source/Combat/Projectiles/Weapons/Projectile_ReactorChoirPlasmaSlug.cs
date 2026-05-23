using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_ReactorChoirPlasmaSlug : Bullet
    {
        private const string ThermalSaturationHediffDefName = "ABY_ReactorChoirThermalSaturation";
        private const float SeverityPerHit = 0.17f;
        private const float VentThreshold = 0.99f;
        private const float VentDamage = 7.5f;
        private const float VentArmorPenetration = 0.30f;
        private const int DebuffDurationTicks = 420;
        private const int TravelSparkIntervalTicks = 5;
        private const string VentBurstSoundDefName = "ABY_ReactorChoirMinigunVentBurst";

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private Vector3 lastDrawDirection = Vector3.right;
        private bool lastPositionInitialized;
        private bool muzzleSpawned;

        protected override void Tick()
        {
            Vector3 previousPosition = ExactPosition;
            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksAlive++;
            if (!lastPositionInitialized)
            {
                lastExactPosition = previousPosition;
                lastPositionInitialized = true;
            }

            Vector3 currentPosition = ExactPosition;
            Vector3 movement = currentPosition - lastExactPosition;
            movement.y = 0f;
            if (movement.sqrMagnitude > 0.0001f)
            {
                lastDrawDirection = movement.normalized;
            }

            if (!muzzleSpawned)
            {
                muzzleSpawned = true;
                ReactorChoirMinigunVfxUtility.SpawnMuzzle(previousPosition, destination, Map);
            }

            if (ticksAlive % TravelSparkIntervalTicks == 0 && Rand.Chance(0.55f))
            {
                ReactorChoirMinigunVfxUtility.SpawnTravelSpark(currentPosition, Map);
            }

            lastExactPosition = currentPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn impactPawn = ResolveImpactPawn(hitThing);
            Map impactMap = Map;
            Vector3 impactPosition = ExactPosition;
            Vector3 impactDirection = lastDrawDirection;
            Thing instigator = Launcher;

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, hitThing, "Projectile_ReactorChoirPlasmaSlug", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null)
            {
                return;
            }

            ReactorChoirMinigunVfxUtility.SpawnImpact(impactPosition, impactDirection, impactMap, blockedByShield);
            if (blockedByShield || impactPawn == null || impactPawn.Dead || impactPawn.health == null)
            {
                return;
            }

            ApplyThermalSaturation(impactPawn, instigator, impactDirection);
        }

        private static void ApplyThermalSaturation(Pawn pawn, Thing instigator, Vector3 travelDirection)
        {
            Hediff hediff = ABY_ProjectileProcUtility.ApplyOrRefreshHediff(
                pawn,
                ThermalSaturationHediffDefName,
                SeverityPerHit,
                0.01f,
                0.99f,
                DebuffDurationTicks);
            if (hediff == null)
            {
                return;
            }

            if (hediff.Severity >= VentThreshold)
            {
                TriggerVentBurst(pawn, instigator, travelDirection);
                ABY_ProjectileProcUtility.RemoveHediff(pawn, hediff);
            }
        }

        private static void TriggerVentBurst(Pawn pawn, Thing instigator, Vector3 travelDirection)
        {
            if (pawn?.MapHeld != null)
            {
                Vector3 drawPos = pawn.DrawPos;
                ReactorChoirMinigunVfxUtility.SpawnVentBurst(drawPos, travelDirection, pawn.MapHeld);
                ABY_SoundUtility.PlayAt(VentBurstSoundDefName, pawn.PositionHeld, pawn.MapHeld);
            }

            ABY_ProjectileProcUtility.ApplyDamage(
                pawn,
                DamageDefOf.Burn,
                VentDamage,
                VentArmorPenetration,
                instigator);
        }

        private Pawn ResolveImpactPawn(Thing hitThing)
        {
            Pawn directPawn = hitThing as Pawn;
            if (directPawn != null)
            {
                return directPawn;
            }

            if (Map == null || !Position.IsValid)
            {
                return null;
            }

            for (int i = 0; i < Position.GetThingList(Map).Count; i++)
            {
                Pawn pawn = Position.GetThingList(Map)[i] as Pawn;
                if (pawn != null)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}
