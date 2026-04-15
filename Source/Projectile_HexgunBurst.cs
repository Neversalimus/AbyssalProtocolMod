using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_HexgunBurst : Bullet
    {
        private const string HexMarkHediffDefName = "ABY_HexMark";
        private const float SeverityPerHit = 0.34f;
        private const float BurnthroughThreshold = 0.95f;
        private const float BurnthroughDamage = 8f;
        private const float BurnthroughArmorPenetration = 0.24f;
        private const int DebuffDurationTicks = 300;
        private const int TrailIntervalTicks = 3;
        private const float TrailGlowSize = 0.19f;
        private const float ImpactGlowSize = 0.92f;
        private const float BurnthroughGlowSize = 1.75f;

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
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            SpawnImpactEffects(impactPosition, impactMap, blockedByShield ? 0.72f : ImpactGlowSize);
            if (blockedByShield || impactPawn == null || impactPawn.Dead || impactPawn.health == null)
            {
                return;
            }

            ApplyHexMark(impactPawn, instigator);
            FleckMaker.ThrowMicroSparks(impactPosition, impactMap);
        }

        private static void ApplyHexMark(Pawn pawn, Thing instigator)
        {
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(HexMarkHediffDefName);
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
            if (hediff.Severity >= BurnthroughThreshold)
            {
                TriggerBurnthrough(pawn, instigator);
                pawn.health.RemoveHediff(hediff);
            }
        }

        private static void TriggerBurnthrough(Pawn pawn, Thing instigator)
        {
            if (pawn.MapHeld != null)
            {
                Vector3 drawPos = pawn.DrawPos;
                FleckMaker.ThrowLightningGlow(drawPos, pawn.MapHeld, BurnthroughGlowSize);
                FleckMaker.ThrowMicroSparks(drawPos, pawn.MapHeld);
                ABY_SoundUtility.PlayAt("ABY_RiftCarbineFire", pawn.PositionHeld, pawn.MapHeld);
            }

            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Burn,
                BurnthroughDamage,
                BurnthroughArmorPenetration,
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
