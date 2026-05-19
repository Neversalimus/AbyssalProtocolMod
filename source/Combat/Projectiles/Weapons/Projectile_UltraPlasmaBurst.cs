using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_UltraPlasmaBurst : Bullet
    {
        private const string DestabilizationHediffDefName = "ABY_UltraPlasmaDestabilization";
        private const float SeverityPerHit = 0.34f;
        private const float DetonationThreshold = 0.95f;
        private const float OverloadDamage = 12f;
        private const float OverloadArmorPenetration = 0.35f;
        private const int DebuffDurationTicks = 300;
        private const int TrailIntervalTicks = 2;
        private const float TrailGlowSize = 0.28f;
        private const float ImpactGlowSize = 1.25f;
        private const float OverloadGlowSize = 2.15f;

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private bool lastPositionInitialized;

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

            if (ticksAlive % TrailIntervalTicks == 0 && ABY_VfxBudget.TrySpend(Map, ABY_VfxBudgetCategory.CombatLight, 1))
            {
                SpawnTrail(lastExactPosition, ExactPosition, Map);
            }

            lastExactPosition = ExactPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Pawn impactPawn = ResolveImpactPawn(hitThing);
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            SpawnImpactEffects(impactPosition, impactMap, blockedByShield ? 0.95f : ImpactGlowSize);

            if (blockedByShield || impactPawn == null || impactPawn.Dead || impactPawn.health == null)
            {
                return;
            }

            ApplyDestabilization(impactPawn, instigator);

            if (impactCell.IsValid)
            {
                FleckMaker.ThrowMicroSparks(impactPosition, impactMap);
            }
        }

        private static void ApplyDestabilization(Pawn pawn, Thing instigator)
        {
            Hediff hediff = ABY_ProjectileProcUtility.ApplyOrRefreshHediff(
                pawn,
                DestabilizationHediffDefName,
                SeverityPerHit,
                0.01f,
                0.99f,
                DebuffDurationTicks);
            if (hediff == null)
            {
                return;
            }

            if (hediff.Severity >= DetonationThreshold)
            {
                TriggerOverload(pawn, instigator);
                ABY_ProjectileProcUtility.RemoveHediff(pawn, hediff);
            }
        }

        private static void TriggerOverload(Pawn pawn, Thing instigator)
        {
            if (pawn.MapHeld != null)
            {
                Vector3 drawPos = pawn.DrawPos;
                FleckMaker.ThrowLightningGlow(drawPos, pawn.MapHeld, OverloadGlowSize);
                FleckMaker.ThrowMicroSparks(drawPos, pawn.MapHeld);
                FleckMaker.ThrowMicroSparks(drawPos, pawn.MapHeld);
                ABY_SoundUtility.PlayAt("ABY_UltraPlasmaFire", pawn.PositionHeld, pawn.MapHeld);
            }

            ABY_ProjectileProcUtility.ApplyDamage(
                pawn,
                DamageDefOf.Burn,
                OverloadDamage,
                OverloadArmorPenetration,
                instigator);
        }

        private static void SpawnTrail(Vector3 from, Vector3 to, Map map)
        {
            if (map == null)
            {
                return;
            }

            for (int i = 1; i <= 2; i++)
            {
                float t = i / 3f;
                Vector3 point = Vector3.Lerp(from, to, t);
                FleckMaker.ThrowLightningGlow(point, map, TrailGlowSize);
                if (i == 2)
                {
                    FleckMaker.ThrowMicroSparks(point, map);
                }
            }
        }

        private static void SpawnImpactEffects(Vector3 position, Map map, float glowSize)
        {
            FleckMaker.ThrowLightningGlow(position, map, glowSize);
            FleckMaker.ThrowMicroSparks(position, map);
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
