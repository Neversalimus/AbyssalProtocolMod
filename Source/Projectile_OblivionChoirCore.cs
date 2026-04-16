using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_OblivionChoirCore : Bullet
    {
        private const int TrailIntervalTicks = 1;
        private const int CorePulseIntervalTicks = 2;
        private const int ArcIntervalTicks = 4;
        private const int ArcRetargetCooldownTicks = 11;
        private const float TrailGlowSize = 0.34f;
        private const float TrailFireGlowSize = 0.18f;
        private const float CoreGlowBaseSize = 0.44f;
        private const float CoreFireGlowBaseSize = 0.22f;
        private const float ArcGlowSize = 0.54f;
        private const float ImpactGlowSize = 2.45f;
        private const float ArcRadius = 2.10f;
        private const int MaxArcTargetsPerPulse = 3;
        private const float ArcDamage = 4f;
        private const float ArcArmorPenetration = 0.34f;
        private const float ImpactExplosionRadius = 4.8f;
        private const int ImpactExplosionDamage = 60;
        private const float ImpactExplosionArmorPenetration = 1.22f;

        private readonly Dictionary<int, int> targetRetargetTicks = new Dictionary<int, int>();
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
                SpawnTrail(lastExactPosition, ExactPosition, Map, ticksAlive);
            }

            if (ticksAlive % CorePulseIntervalTicks == 0)
            {
                SpawnCorePulse(ExactPosition, Map, ticksAlive);
            }

            if (ticksAlive % ArcIntervalTicks == 0)
            {
                PulseNearbyTargets();
            }

            lastExactPosition = ExactPosition;
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;

            base.Impact(hitThing, blockedByShield);

            if (impactMap == null || !impactCell.IsValid)
            {
                return;
            }

            SpawnImpactEffects(impactPosition, impactMap);

            if (blockedByShield)
            {
                return;
            }

            ABY_SoundUtility.PlayAt("ABY_UltraPlasmaTail", impactCell, impactMap);
            GenExplosion.DoExplosion(impactCell, impactMap, ImpactExplosionRadius, DamageDefOf.Burn, instigator, ImpactExplosionDamage, ImpactExplosionArmorPenetration);
        }

        private void PulseNearbyTargets()
        {
            if (Map == null)
            {
                return;
            }

            IntVec3 centerCell = ExactPosition.ToIntVec3();
            if (!centerCell.InBounds(Map))
            {
                return;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : ticksAlive;
            List<Thing> candidates = new List<Thing>();
            HashSet<int> seenThingIds = new HashSet<int>();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(centerCell, ArcRadius, true))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || seenThingIds.Contains(thing.thingIDNumber) || !ShouldAffectThing(thing))
                    {
                        continue;
                    }

                    seenThingIds.Add(thing.thingIDNumber);
                    candidates.Add(thing);
                }
            }

            candidates.Sort((a, b) => HorizontalDistanceSquared(ExactPosition, a).CompareTo(HorizontalDistanceSquared(ExactPosition, b)));

            int affectedCount = 0;
            for (int i = 0; i < candidates.Count && affectedCount < MaxArcTargetsPerPulse; i++)
            {
                Thing thing = candidates[i];
                if (targetRetargetTicks.TryGetValue(thing.thingIDNumber, out int nextTick) && currentTick < nextTick)
                {
                    continue;
                }

                ApplyArcDamage(thing);
                targetRetargetTicks[thing.thingIDNumber] = currentTick + ArcRetargetCooldownTicks;
                affectedCount++;
            }
        }

        private bool ShouldAffectThing(Thing thing)
        {
            if (thing == null || thing.Destroyed || thing == Launcher || !thing.Spawned)
            {
                return false;
            }

            Faction launcherFaction = Launcher != null ? Launcher.Faction : null;

            if (thing is Pawn pawn)
            {
                if (pawn.Dead)
                {
                    return false;
                }

                if (launcherFaction != null && pawn.Faction == launcherFaction)
                {
                    return false;
                }

                return true;
            }

            if (thing is Building building)
            {
                if (thing is Blueprint || thing is Frame)
                {
                    return false;
                }

                if (building.def.mineable || (building.def.building != null && building.def.building.isNaturalRock))
                {
                    return false;
                }

                if (launcherFaction != null && building.Faction == launcherFaction)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private void ApplyArcDamage(Thing thing)
        {
            if (thing == null)
            {
                return;
            }

            Map map = thing.MapHeld;
            if (map != null)
            {
                Vector3 drawPos = thing.TrueCenter();
                FleckMaker.ThrowLightningGlow(drawPos, map, ArcGlowSize);
                FleckMaker.ThrowMicroSparks(drawPos, map);
            }

            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Burn,
                ArcDamage,
                ArcArmorPenetration,
                -1f,
                Launcher,
                null,
                def,
                DamageInfo.SourceCategory.ThingOrUnknown);

            thing.TakeDamage(damageInfo);
        }

        private static void SpawnTrail(Vector3 from, Vector3 to, Map map, int ticksAlive)
        {
            if (map == null)
            {
                return;
            }

            for (int i = 1; i <= 3; i++)
            {
                float t = i / 4f;
                Vector3 point = Vector3.Lerp(from, to, t);
                float pulse = 0.90f + Mathf.Abs(Mathf.Sin((ticksAlive + i * 3) * 0.38f)) * 0.35f;
                FleckMaker.ThrowLightningGlow(point, map, TrailGlowSize * pulse);
                if (((ticksAlive + i) & 1) == 0)
                {
                    FleckMaker.ThrowFireGlow(point, map, TrailFireGlowSize * pulse);
                }
                if (i >= 2 || Rand.Chance(0.40f))
                {
                    FleckMaker.ThrowMicroSparks(point, map);
                }
            }
        }

        private static void SpawnCorePulse(Vector3 position, Map map, int ticksAlive)
        {
            if (map == null)
            {
                return;
            }

            float pulse = 0.92f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.42f)) * 0.40f;
            FleckMaker.ThrowLightningGlow(position, map, CoreGlowBaseSize * pulse);
            FleckMaker.ThrowFireGlow(position, map, CoreFireGlowBaseSize * pulse);
            if ((ticksAlive % 4) == 0)
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        private static void SpawnImpactEffects(Vector3 position, Map map)
        {
            FleckMaker.ThrowLightningGlow(position, map, ImpactGlowSize);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowFireGlow(position, map, 0.72f);
        }
    }
}
