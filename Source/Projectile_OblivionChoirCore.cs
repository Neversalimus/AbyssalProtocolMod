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
        private const int ArcIntervalTicks = 3;
        private const int ArcRetargetCooldownTicks = 18;
        private const int BranchBeamLifetimeTicks = 7;
        private const int MaxArcTargetsPerPulse = 4;
        private const int MaxSweepSamples = 14;

        private const float TrailGlowSize = 0.34f;
        private const float TrailFireGlowSize = 0.18f;
        private const float CoreGlowBaseSize = 0.50f;
        private const float CoreFireGlowBaseSize = 0.24f;
        private const float ArcGlowSize = 0.58f;
        private const float ImpactGlowSize = 2.45f;
        private const float ArcRadius = 6.0f;
        private const float SweepSampleSpacing = 0.72f;
        private const float ArcDamage = 4f;
        private const float ArcArmorPenetration = 0.34f;
        private const float ImpactExplosionRadius = 4.8f;
        private const int ImpactExplosionDamage = 60;
        private const float ImpactExplosionArmorPenetration = 1.22f;

        private const string BodyTexturePath = "Things/Projectile/ABY_OblivionChoirCore";
        private const string BlobHaloTexturePath = "Things/VFX/OblivionChoir/ABY_OblivionChoir_BlobHalo";
        private const string BlobCoreTexturePath = "Things/VFX/OblivionChoir/ABY_OblivionChoir_BlobCore";
        private const string BranchHaloThingDefName = "ABY_Mote_OblivionChoirBranchHalo";
        private const string BranchCoreThingDefName = "ABY_Mote_OblivionChoirBranchCore";
        private const string BranchHaloTexturePath = "Things/VFX/OblivionChoir/ABY_OblivionChoir_BranchHalo";
        private const string BranchCoreTexturePath = "Things/VFX/OblivionChoir/ABY_OblivionChoir_BranchCore";

        private static ThingDef branchHaloDef;
        private static ThingDef branchCoreDef;

        private readonly Dictionary<int, int> targetRetargetTicks = new Dictionary<int, int>();
        private readonly List<ArcCandidate> reusableCandidates = new List<ArcCandidate>();
        private readonly HashSet<int> reusableSeenThingIds = new HashSet<int>();

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private Vector3 lastDrawDirection = Vector3.forward;
        private bool lastPositionInitialized;

        private Material cachedBodyMaterial;
        private Material cachedBlobHaloMaterial;
        private Material cachedBlobCoreMaterial;

        private sealed class ArcCandidate
        {
            public Thing thing;
            public Vector3 branchSource;
            public float score;
        }

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

            if (ticksAlive % TrailIntervalTicks == 0)
            {
                SpawnTrail(lastExactPosition, currentPosition, Map, ticksAlive);
            }

            if (ticksAlive % CorePulseIntervalTicks == 0)
            {
                SpawnCorePulse(currentPosition, Map, ticksAlive);
            }

            if (ticksAlive % ArcIntervalTicks == 0)
            {
                PulseTargetsAlongSweptPath(lastExactPosition, currentPosition);
            }

            lastExactPosition = currentPosition;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = drawLoc;
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);

            Vector3 direction = lastDrawDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }
            direction.Normalize();

            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pulse = 0.92f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.31f)) * 0.18f;
            float hotPulse = 0.82f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.57f + 1.2f)) * 0.34f;
            float wobble = Mathf.Sin(ticksAlive * 0.23f) * 0.035f;

            DrawPlane(drawPos, angle + wobble * 90f, new Vector3(1.42f * pulse, 1f, 2.72f * pulse), BlobHaloMaterial);
            DrawPlane(drawPos + direction * 0.03f, angle, new Vector3(1.02f * (0.96f + hotPulse * 0.06f), 1f, 2.42f * (0.96f + hotPulse * 0.04f)), BodyMaterial);
            DrawPlane(drawPos + direction * 0.18f, angle - wobble * 120f, new Vector3(0.78f * hotPulse, 1f, 1.26f * hotPulse), BlobCoreMaterial);
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

        private void PulseTargetsAlongSweptPath(Vector3 from, Vector3 to)
        {
            if (Map == null)
            {
                return;
            }

            EnsureBranchDefsLoaded();
            reusableCandidates.Clear();
            reusableSeenThingIds.Clear();

            Vector3 flatDelta = to - from;
            flatDelta.y = 0f;
            float distance = flatDelta.magnitude;
            int sampleCount = Mathf.Clamp(Mathf.CeilToInt(distance / SweepSampleSpacing), 1, MaxSweepSamples);
            Vector3 currentCorePos = to;

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = sampleCount <= 0 ? 1f : i / (float)sampleCount;
                Vector3 samplePos = Vector3.Lerp(from, to, t);
                IntVec3 sampleCell = samplePos.ToIntVec3();
                if (!sampleCell.IsValid || !sampleCell.InBounds(Map))
                {
                    continue;
                }

                foreach (IntVec3 cell in GenRadial.RadialCellsAround(sampleCell, ArcRadius, true))
                {
                    if (!cell.InBounds(Map))
                    {
                        continue;
                    }

                    List<Thing> things = cell.GetThingList(Map);
                    for (int j = 0; j < things.Count; j++)
                    {
                        Thing thing = things[j];
                        if (thing == null || reusableSeenThingIds.Contains(thing.thingIDNumber) || !ShouldAffectThing(thing))
                        {
                            continue;
                        }

                        Vector3 targetCenter = thing.TrueCenter();
                        float sampleDistanceSq = HorizontalDistanceSquared(samplePos, targetCenter);
                        if (sampleDistanceSq > ArcRadius * ArcRadius)
                        {
                            continue;
                        }

                        if (!HasLineOfSightFromSample(sampleCell, thing))
                        {
                            continue;
                        }

                        reusableSeenThingIds.Add(thing.thingIDNumber);
                        reusableCandidates.Add(new ArcCandidate
                        {
                            thing = thing,
                            branchSource = SelectBranchSource(currentCorePos, samplePos, targetCenter),
                            score = sampleDistanceSq + Mathf.Abs(0.66f - t) * 2.25f
                        });
                    }
                }
            }

            if (reusableCandidates.Count <= 0)
            {
                return;
            }

            reusableCandidates.Sort((a, b) => a.score.CompareTo(b.score));
            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : ticksAlive;
            int affectedCount = 0;

            for (int i = 0; i < reusableCandidates.Count && affectedCount < MaxArcTargetsPerPulse; i++)
            {
                Thing thing = reusableCandidates[i].thing;
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (targetRetargetTicks.TryGetValue(thing.thingIDNumber, out int nextTick) && currentTick < nextTick)
                {
                    continue;
                }

                ApplyArcDamage(thing, reusableCandidates[i].branchSource);
                targetRetargetTicks[thing.thingIDNumber] = currentTick + ArcRetargetCooldownTicks;
                affectedCount++;
            }
        }

        private Vector3 SelectBranchSource(Vector3 currentCorePos, Vector3 samplePos, Vector3 targetCenter)
        {
            float currentDistanceSq = HorizontalDistanceSquared(currentCorePos, targetCenter);
            if (currentDistanceSq <= ArcRadius * ArcRadius * 1.18f)
            {
                return currentCorePos;
            }

            return samplePos;
        }

        private bool ShouldAffectThing(Thing thing)
        {
            if (thing == null || thing.Destroyed || thing == Launcher || !thing.Spawned)
            {
                return false;
            }

            if (thing.def == null || thing.def.category == ThingCategory.Mote || thing.def.category == ThingCategory.Projectile || thing is Fire)
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                if (pawn.Dead)
                {
                    return false;
                }

                return Launcher == null || GenHostility.HostileTo(Launcher, pawn);
            }

            Building building = thing as Building;
            if (building != null)
            {
                if (thing is Blueprint || thing is Frame)
                {
                    return false;
                }

                if (building.def.mineable || (building.def.building != null && building.def.building.isNaturalRock))
                {
                    return false;
                }

                if (Launcher != null && !GenHostility.HostileTo(Launcher, building))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private bool HasLineOfSightFromSample(IntVec3 sampleCell, Thing thing)
        {
            if (Map == null || thing == null || !thing.Spawned)
            {
                return false;
            }

            IntVec3 targetCell = thing.PositionHeld;
            if (!sampleCell.IsValid || !targetCell.IsValid || !sampleCell.InBounds(Map) || !targetCell.InBounds(Map))
            {
                return false;
            }

            return sampleCell == targetCell || GenSight.LineOfSight(sampleCell, targetCell, Map, true);
        }

        private void ApplyArcDamage(Thing thing, Vector3 branchSource)
        {
            if (thing == null)
            {
                return;
            }

            Map map = thing.MapHeld;
            if (map != null)
            {
                Vector3 drawPos = thing.TrueCenter();
                SpawnBranchBeam(map, branchSource, drawPos, thing.thingIDNumber);
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

        private void SpawnBranchBeam(Map map, Vector3 from, Vector3 to, int targetId)
        {
            if (map == null || branchHaloDef == null || branchCoreDef == null)
            {
                return;
            }

            from.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);
            to.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);

            Vector3 direction = to - from;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance <= 0.08f)
            {
                return;
            }

            Vector3 normal = direction / distance;
            Vector3 perpendicular = new Vector3(-normal.z, 0f, normal.x);
            int seed = targetId * 397 ^ ticksAlive * 101;
            float phase = seed * 0.017f + ticksAlive * 0.64f;
            float amplitude = Mathf.Clamp(distance * 0.11f, 0.10f, 0.42f);
            int segmentCount = Mathf.Clamp(Mathf.CeilToInt(distance * 0.75f), 2, 5);
            Vector3 previous = from;

            for (int i = 1; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector3 point = Vector3.Lerp(from, to, t);
                float envelope = Mathf.Sin(t * Mathf.PI);
                float sway = Mathf.Sin(phase + t * 9.8f) * amplitude * envelope;
                float snap = Mathf.Sin(phase * 1.9f + t * 19.2f) * amplitude * 0.38f * envelope;
                point += perpendicular * (sway + snap);
                point.y = Altitudes.AltitudeFor(AltitudeLayer.MoteOverhead);

                float widthFactor = 0.76f + envelope * 0.34f;
                SpawnBeamThing(branchHaloDef, previous, point, map, 0.30f * widthFactor, BranchBeamLifetimeTicks, BranchHaloTexturePath, true);
                SpawnBeamThing(branchCoreDef, previous, point, map, 0.095f * widthFactor, BranchBeamLifetimeTicks - 1, BranchCoreTexturePath, false);
                previous = point;
            }
        }

        private static void SpawnBeamThing(ThingDef thingDef, Vector3 source, Vector3 target, Map map, float width, int ticks, string texturePath, bool pulse)
        {
            if (thingDef == null || map == null || ticks <= 0)
            {
                return;
            }

            Mote_CrownspikeRailBeam beam = ThingMaker.MakeThing(thingDef) as Mote_CrownspikeRailBeam;
            if (beam == null)
            {
                return;
            }

            beam.start = source;
            beam.end = target;
            beam.width = width;
            beam.ticksLeft = ticks;
            beam.startingTicks = ticks;
            beam.texturePath = texturePath;
            beam.additivePulse = pulse;

            IntVec3 spawnCell = ((source + target) * 0.5f).ToIntVec3();
            if (!spawnCell.InBounds(map))
            {
                spawnCell = source.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                spawnCell = target.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                return;
            }

            GenSpawn.Spawn(beam, spawnCell, map);
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

        private static float HorizontalDistanceSquared(Vector3 origin, Thing thing)
        {
            return HorizontalDistanceSquared(origin, thing.TrueCenter());
        }

        private static float HorizontalDistanceSquared(Vector3 origin, Vector3 target)
        {
            float dx = target.x - origin.x;
            float dz = target.z - origin.z;
            return dx * dx + dz * dz;
        }

        private static void EnsureBranchDefsLoaded()
        {
            if (branchHaloDef == null)
            {
                branchHaloDef = DefDatabase<ThingDef>.GetNamedSilentFail(BranchHaloThingDefName);
            }

            if (branchCoreDef == null)
            {
                branchCoreDef = DefDatabase<ThingDef>.GetNamedSilentFail(BranchCoreThingDefName);
            }
        }

        private void DrawPlane(Vector3 center, float angle, Vector3 scale, Material material)
        {
            if (material == null)
            {
                return;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private Material BodyMaterial
        {
            get
            {
                if (cachedBodyMaterial == null)
                {
                    cachedBodyMaterial = MaterialPool.MatFrom(BodyTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedBodyMaterial;
            }
        }

        private Material BlobHaloMaterial
        {
            get
            {
                if (cachedBlobHaloMaterial == null)
                {
                    cachedBlobHaloMaterial = MaterialPool.MatFrom(BlobHaloTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedBlobHaloMaterial;
            }
        }

        private Material BlobCoreMaterial
        {
            get
            {
                if (cachedBlobCoreMaterial == null)
                {
                    cachedBlobCoreMaterial = MaterialPool.MatFrom(BlobCoreTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedBlobCoreMaterial;
            }
        }
    }
}
