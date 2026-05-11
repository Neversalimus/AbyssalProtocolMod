using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_BranchingProjectileConfig
    {
        public float radius = 6f;
        public float sampleSpacing = 0.72f;
        public int maxSweepSamples = 14;
        public int maxTargetsPerPulse = 4;
        public int retargetCooldownTicks = 18;
        public int branchLifetimeTicks = 7;
        public string branchHaloThingDefName;
        public string branchCoreThingDefName;
        public string branchHaloTexturePath;
        public string branchCoreTexturePath;
        public float haloWidth = 0.30f;
        public float coreWidth = 0.095f;
        public Func<Thing, bool> shouldAffectThing;
        public Action<Thing, Vector3> onBranchHit;
    }

    public static class ABY_BranchingProjectileUtility
    {
        private sealed class BranchCandidate
        {
            public Thing thing;
            public Vector3 branchSource;
            public float score;
        }

        private static readonly List<BranchCandidate> ReusableCandidates = new List<BranchCandidate>();
        private static readonly HashSet<int> ReusableSeenThingIds = new HashSet<int>();

        public static int PulseSweptBranches(
            Map map,
            Thing launcher,
            Vector3 from,
            Vector3 to,
            int ticksAlive,
            Dictionary<int, int> targetRetargetTicks,
            ABY_BranchingProjectileConfig config)
        {
            if (map == null || config == null || config.onBranchHit == null)
            {
                return 0;
            }

            ReusableCandidates.Clear();
            ReusableSeenThingIds.Clear();

            Vector3 flatDelta = to - from;
            flatDelta.y = 0f;
            float distance = flatDelta.magnitude;
            int sampleCount = Mathf.Clamp(Mathf.CeilToInt(distance / Mathf.Max(0.05f, config.sampleSpacing)), 1, Mathf.Max(1, config.maxSweepSamples));
            Vector3 currentCorePos = to;
            float radius = Mathf.Max(0.1f, config.radius);
            float radiusSq = radius * radius;

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = sampleCount <= 0 ? 1f : i / (float)sampleCount;
                Vector3 samplePos = Vector3.Lerp(from, to, t);
                IntVec3 sampleCell = samplePos.ToIntVec3();
                if (!sampleCell.IsValid || !sampleCell.InBounds(map))
                {
                    continue;
                }

                foreach (IntVec3 cell in GenRadial.RadialCellsAround(sampleCell, radius, true))
                {
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    List<Thing> things = cell.GetThingList(map);
                    for (int j = 0; j < things.Count; j++)
                    {
                        Thing thing = things[j];
                        if (thing == null || ReusableSeenThingIds.Contains(thing.thingIDNumber))
                        {
                            continue;
                        }

                        if (thing == launcher || thing.Destroyed || !thing.Spawned || thing.MapHeld != map)
                        {
                            continue;
                        }

                        if (config.shouldAffectThing != null && !config.shouldAffectThing(thing))
                        {
                            continue;
                        }

                        Vector3 targetCenter = thing.TrueCenter();
                        float sampleDistanceSq = HorizontalDistanceSquared(samplePos, targetCenter);
                        if (sampleDistanceSq > radiusSq)
                        {
                            continue;
                        }

                        if (!HasLineOfSightFromSample(map, sampleCell, thing))
                        {
                            continue;
                        }

                        ReusableSeenThingIds.Add(thing.thingIDNumber);
                        ReusableCandidates.Add(new BranchCandidate
                        {
                            thing = thing,
                            branchSource = SelectBranchSource(currentCorePos, samplePos, targetCenter, radius),
                            score = sampleDistanceSq + Mathf.Abs(0.66f - t) * 2.25f
                        });
                    }
                }
            }

            if (ReusableCandidates.Count <= 0)
            {
                return 0;
            }

            ReusableCandidates.Sort((a, b) => a.score.CompareTo(b.score));
            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : ticksAlive;
            int affectedCount = 0;
            int maxTargets = Mathf.Max(1, config.maxTargetsPerPulse);

            for (int i = 0; i < ReusableCandidates.Count && affectedCount < maxTargets; i++)
            {
                Thing thing = ReusableCandidates[i].thing;
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (targetRetargetTicks != null
                    && targetRetargetTicks.TryGetValue(thing.thingIDNumber, out int nextTick)
                    && currentTick < nextTick)
                {
                    continue;
                }

                config.onBranchHit(thing, ReusableCandidates[i].branchSource);
                if (targetRetargetTicks != null)
                {
                    targetRetargetTicks[thing.thingIDNumber] = currentTick + Mathf.Max(1, config.retargetCooldownTicks);
                }
                affectedCount++;
            }

            return affectedCount;
        }

        public static void SpawnCurvedBranchBeam(
            Map map,
            Vector3 from,
            Vector3 to,
            int seed,
            int ticksAlive,
            int lifetimeTicks,
            string haloThingDefName,
            string coreThingDefName,
            string haloTexturePath,
            string coreTexturePath,
            float haloWidth,
            float coreWidth)
        {
            if (map == null || haloThingDefName.NullOrEmpty() || coreThingDefName.NullOrEmpty())
            {
                return;
            }

            ThingDef haloDef = DefDatabase<ThingDef>.GetNamedSilentFail(haloThingDefName);
            ThingDef coreDef = DefDatabase<ThingDef>.GetNamedSilentFail(coreThingDefName);
            if (haloDef == null || coreDef == null)
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
                SpawnBeamThing(haloDef, previous, point, map, Mathf.Max(0.01f, haloWidth) * widthFactor, lifetimeTicks, haloTexturePath, true);
                SpawnBeamThing(coreDef, previous, point, map, Mathf.Max(0.01f, coreWidth) * widthFactor, Mathf.Max(1, lifetimeTicks - 1), coreTexturePath, false);
                previous = point;
            }
        }

        private static Vector3 SelectBranchSource(Vector3 currentCorePos, Vector3 samplePos, Vector3 targetCenter, float radius)
        {
            float currentDistanceSq = HorizontalDistanceSquared(currentCorePos, targetCenter);
            if (currentDistanceSq <= radius * radius * 1.18f)
            {
                return currentCorePos;
            }

            return samplePos;
        }

        private static bool HasLineOfSightFromSample(Map map, IntVec3 sampleCell, Thing thing)
        {
            if (map == null || thing == null || !thing.Spawned)
            {
                return false;
            }

            IntVec3 targetCell = thing.PositionHeld;
            if (!sampleCell.IsValid || !targetCell.IsValid || !sampleCell.InBounds(map) || !targetCell.InBounds(map))
            {
                return false;
            }

            return sampleCell == targetCell || GenSight.LineOfSight(sampleCell, targetCell, map, true);
        }

        private static float HorizontalDistanceSquared(Vector3 origin, Vector3 target)
        {
            float dx = target.x - origin.x;
            float dz = target.z - origin.z;
            return dx * dx + dz * dz;
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
    }
}
