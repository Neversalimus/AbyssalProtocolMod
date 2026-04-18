using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_RiftCarbineBurst : Bullet
    {
        private const string ArmorMeltHediffDefName = "ABY_RiftArmorMelt";
        private const float SeverityPerHit = 0.24f;
        private const float BreachThreshold = 0.95f;
        private const float BreachDamage = 8f;
        private const float BreachArmorPenetration = 0.22f;
        private const int DebuffDurationTicks = 360;
        private const int TrailIntervalTicks = 2;
        private const float TrailGlowSize = 0.24f;
        private const float ImpactGlowSize = 1.18f;
        private const float BreachGlowSize = 2.00f;

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

            if (ticksAlive % TrailIntervalTicks == 0)
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

            SpawnImpactEffects(impactPosition, impactMap, blockedByShield ? 0.92f : ImpactGlowSize);
            if (blockedByShield || impactPawn == null || impactPawn.Dead || impactPawn.health == null)
            {
                return;
            }

            ApplyArmorMelt(impactPawn, instigator);
            if (impactCell.IsValid)
            {
                FleckMaker.ThrowMicroSparks(impactPosition, impactMap);
            }
        }

        private static void ApplyArmorMelt(Pawn pawn, Thing instigator)
        {
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(ArmorMeltHediffDefName);
            if (hediffDef == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(hediff);
            }

            hediff.Severity = Mathf.Clamp(hediff.Severity + SeverityPerHit, 0.01f, 0.99f);

            HediffComp_Disappears disappears = hediff.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = DebuffDurationTicks;
            }

            pawn.health.hediffSet.DirtyCache();
            if (hediff.Severity >= BreachThreshold)
            {
                TriggerArmorBreach(pawn, instigator);
                pawn.health.RemoveHediff(hediff);
            }
        }

        private static void TriggerArmorBreach(Pawn pawn, Thing instigator)
        {
            if (pawn.MapHeld != null)
            {
                Vector3 drawPos = pawn.DrawPos;
                FleckMaker.ThrowLightningGlow(drawPos, pawn.MapHeld, BreachGlowSize);
                FleckMaker.ThrowMicroSparks(drawPos, pawn.MapHeld);
                FleckMaker.ThrowMicroSparks(drawPos, pawn.MapHeld);
                ABY_SoundUtility.PlayAt("ABY_RiftCarbineFire", pawn.PositionHeld, pawn.MapHeld);
            }

            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Burn,
                BreachDamage,
                BreachArmorPenetration,
                -1f,
                instigator,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown);

            pawn.TakeDamage(damageInfo);
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
