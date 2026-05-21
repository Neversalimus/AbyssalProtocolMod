using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_VesperLanceBolt : Bullet
    {
        private const string JudgementHediffDefName = "ABY_VesperJudgement";
        private const float SeverityPerHit = 0.38f;
        private const float SentenceThreshold = 0.95f;
        private const float SentenceDamage = 14f;
        private const float SentenceArmorPenetration = 0.30f;
        private const float MechanicalEmpDamage = 8f;
        private const float SentenceEmpDamage = 10f;
        private const float SentenceEmpRadius = 1.9f;
        private const int DebuffDurationTicks = 480;
        private const int TrailIntervalTicks = 2;
        private const float TrailGlowSize = 0.18f;
        private const float ImpactGlowSize = 1.45f;
        private const float SentenceGlowSize = 2.25f;

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
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;
            Pawn impactPawn = ResolveImpactPawn(hitThing);

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, hitThing, "Projectile_VesperLanceBolt", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null)
            {
                return;
            }

            SpawnImpactEffects(impactPosition, impactMap, blockedByShield ? 0.95f : ImpactGlowSize);
            if (impactCell.IsValid)
            {
                ABY_SoundUtility.PlayAt("ABY_VesperLanceImpact", impactCell, impactMap);
            }

            if (blockedByShield)
            {
                return;
            }

            if (hitThing != null)
            {
                ApplyDirectSanction(hitThing, instigator);
            }

            if (impactPawn != null && !impactPawn.Dead && impactPawn.health != null)
            {
                ApplyJudgement(impactPawn, instigator);
            }
        }

        private static void ApplyDirectSanction(Thing hitThing, Thing instigator)
        {
            if (hitThing == null || hitThing.Destroyed)
            {
                return;
            }

            Pawn pawn = hitThing as Pawn;
            if (pawn != null && pawn.RaceProps != null && pawn.RaceProps.IsMechanoid)
            {
                ABY_ProjectileProcUtility.ApplyDamage(
                    pawn,
                    DamageDefOf.EMP,
                    MechanicalEmpDamage,
                    0f,
                    instigator);
                return;
            }

            if (hitThing.def != null && hitThing.def.category == ThingCategory.Building && hitThing.def.useHitPoints)
            {
                ABY_ProjectileProcUtility.ApplyDamage(
                    hitThing,
                    DamageDefOf.EMP,
                    MechanicalEmpDamage,
                    0f,
                    instigator);
            }
        }

        private static void ApplyJudgement(Pawn pawn, Thing instigator)
        {
            Hediff hediff = ABY_ProjectileProcUtility.ApplyOrRefreshHediff(
                pawn,
                JudgementHediffDefName,
                SeverityPerHit,
                0.01f,
                0.99f,
                DebuffDurationTicks);
            if (hediff == null)
            {
                return;
            }

            if (hediff.Severity >= SentenceThreshold)
            {
                TriggerSentence(pawn, instigator);
                ABY_ProjectileProcUtility.RemoveHediff(pawn, hediff);
            }
        }

        private static void TriggerSentence(Pawn pawn, Thing instigator)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            if (pawn.MapHeld != null)
            {
                Vector3 drawPos = pawn.DrawPos;
                FleckMaker.ThrowLightningGlow(drawPos, pawn.MapHeld, SentenceGlowSize);
                FleckMaker.ThrowMicroSparks(drawPos, pawn.MapHeld);
                FleckMaker.ThrowMicroSparks(drawPos, pawn.MapHeld);
                ABY_SoundUtility.PlayAt("ABY_VesperLanceImpact", pawn.PositionHeld, pawn.MapHeld);
                DoEmpPulse(pawn.PositionHeld, pawn.MapHeld, instigator);
            }

            ABY_ProjectileProcUtility.ApplyDamage(
                pawn,
                DamageDefOf.Burn,
                SentenceDamage,
                SentenceArmorPenetration,
                instigator);
        }

        private static void DoEmpPulse(IntVec3 center, Map map, Thing instigator)
        {
            if (map == null || !center.IsValid)
            {
                return;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, SentenceEmpRadius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                var things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed)
                    {
                        continue;
                    }

                    Pawn pawn = thing as Pawn;
                    if (pawn != null)
                    {
                        if (pawn.RaceProps != null && pawn.RaceProps.IsMechanoid)
                        {
                            ABY_ProjectileProcUtility.ApplyDamage(
                                pawn,
                                DamageDefOf.EMP,
                                SentenceEmpDamage,
                                0f,
                                instigator);
                        }

                        continue;
                    }

                    if (thing.def != null && thing.def.category == ThingCategory.Building && thing.def.useHitPoints)
                    {
                        ABY_ProjectileProcUtility.ApplyDamage(
                            thing,
                            DamageDefOf.EMP,
                            SentenceEmpDamage,
                            0f,
                            instigator);
                    }
                }
            }
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
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, glowSize);
            FleckMaker.ThrowMicroSparks(position, map);
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

            var things = Position.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                Pawn pawn = things[i] as Pawn;
                if (pawn != null)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}
