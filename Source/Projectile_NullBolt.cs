using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_NullBolt : Bullet
    {
        private const string NullExposureHediffDefName = "ABY_NullExposure";
        private const float DirectSeverity = 0.46f;
        private const float SplashSeverity = 0.24f;
        private const float SplashRadius = 1.65f;
        private const int DebuffDurationTicks = 360;
        private const int TrailIntervalTicks = 3;
        private const float TrailGlowSize = 0.16f;
        private const float ImpactGlowSize = 0.86f;

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
            Pawn directPawn = ResolveImpactPawn(hitThing);
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;
            Pawn launcherPawn = Launcher as Pawn;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null)
            {
                return;
            }

            SpawnImpactEffects(impactPosition, impactMap, blockedByShield ? 0.62f : ImpactGlowSize);
            if (blockedByShield || !impactCell.IsValid)
            {
                return;
            }

            HediffDef exposureDef = DefDatabase<HediffDef>.GetNamedSilentFail(NullExposureHediffDefName);
            if (exposureDef == null)
            {
                return;
            }

            ApplyExposureBurst(impactMap, impactCell, exposureDef, launcherPawn, directPawn);
        }

        private static void ApplyExposureBurst(Map map, IntVec3 center, HediffDef exposureDef, Pawn launcherPawn, Pawn directPawn)
        {
            HashSet<Pawn> affected = new HashSet<Pawn>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, SplashRadius, useCenter: true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (!(things[i] is Pawn pawn) || pawn.Dead || pawn.health == null || !affected.Add(pawn))
                    {
                        continue;
                    }

                    if (launcherPawn != null && !launcherPawn.HostileTo(pawn))
                    {
                        continue;
                    }

                    float severityGain = pawn == directPawn ? DirectSeverity : SplashSeverity;
                    Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(exposureDef);
                    if (existing == null)
                    {
                        existing = HediffMaker.MakeHediff(exposureDef, pawn);
                        pawn.health.AddHediff(existing);
                    }

                    existing.Severity = Mathf.Clamp(existing.Severity + severityGain, 0.01f, 0.99f);
                    HediffComp_Disappears disappears = existing.TryGetComp<HediffComp_Disappears>();
                    if (disappears != null)
                    {
                        disappears.ticksToDisappear = DebuffDurationTicks;
                    }

                    pawn.health.hediffSet.DirtyCache();
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

            List<Thing> things = Position.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn pawn)
                {
                    return pawn;
                }
            }

            return null;
        }
    }
}
