using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class CrownfireRocketChoirVfxUtility
    {
        private const string TubeIgnitionMoteDefName = "ABY_Mote_CrownfireTubeIgnition";
        private const string LaunchExhaustMoteDefName = "ABY_Mote_CrownfireLaunchExhaust";
        private const string SplitBurstMoteDefName = "ABY_Mote_CrownfireSplitBurst";
        private const string MicroTrailMoteDefName = "ABY_Mote_CrownfireMicroTrail";
        private const string MicroImpactMoteDefName = "ABY_Mote_CrownfireMicroImpact";

        private const string TubeIgnitionFramePrefix = "Things/VFX/CrownfireRocketChoir/ABY_CrownfireTubeIgnition_";
        private const string LaunchExhaustFramePrefix = "Things/VFX/CrownfireRocketChoir/ABY_CrownfireLaunchExhaust_";
        private const string SplitBurstFramePrefix = "Things/VFX/CrownfireRocketChoir/ABY_CrownfireSplitBurst_";
        private const string MicroTrailFramePrefix = "Things/VFX/CrownfireRocketChoir/ABY_CrownfireMicroTrail_";
        private const string MicroImpactFramePrefix = "Things/VFX/CrownfireRocketChoir/ABY_CrownfireMicroImpact_";

        private static ThingDef tubeIgnitionMoteDef;
        private static ThingDef launchExhaustMoteDef;
        private static ThingDef splitBurstMoteDef;
        private static ThingDef microTrailMoteDef;
        private static ThingDef microImpactMoteDef;

        private static ThingDef TubeIgnitionMoteDef => tubeIgnitionMoteDef ?? (tubeIgnitionMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(TubeIgnitionMoteDefName));
        private static ThingDef LaunchExhaustMoteDef => launchExhaustMoteDef ?? (launchExhaustMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(LaunchExhaustMoteDefName));
        private static ThingDef SplitBurstMoteDef => splitBurstMoteDef ?? (splitBurstMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(SplitBurstMoteDefName));
        private static ThingDef MicroTrailMoteDef => microTrailMoteDef ?? (microTrailMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MicroTrailMoteDefName));
        private static ThingDef MicroImpactMoteDef => microImpactMoteDef ?? (microImpactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MicroImpactMoteDefName));

        public static void SpawnTubeIgnition(Vector3 position, Map map)
        {
            SpawnAnimatedMote(TubeIgnitionMoteDef, TubeIgnitionFramePrefix, 6, 1, 7, 0.60f, 0.60f, position, map, 0f);
            SpawnAnimatedMote(TubeIgnitionMoteDef, TubeIgnitionFramePrefix, 6, 1, 5, 0.38f, 0.38f, position + new Vector3(0f, 0f, 0.03f), map, Rand.Range(0f, 360f));
            if (map != null)
            {
                FleckMaker.ThrowLightningGlow(position, map, 0.26f);
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnLaunchExhaust(Vector3 position, Map map, float sizeMultiplier = 1f, int lifetimeTicks = 24)
        {
            float primarySize = 1.18f * sizeMultiplier;
            float secondarySize = 0.82f * sizeMultiplier;
            SpawnAnimatedMote(LaunchExhaustMoteDef, LaunchExhaustFramePrefix, 8, 2, lifetimeTicks, primarySize, primarySize, position, map, 0f);
            SpawnAnimatedMote(LaunchExhaustMoteDef, LaunchExhaustFramePrefix, 8, 2, Mathf.Max(10, lifetimeTicks - 6), secondarySize, secondarySize, position + new Vector3(0f, 0f, -0.10f), map, 0f);
            if (map != null)
            {
                FleckMaker.ThrowLightningGlow(position, map, 0.22f * sizeMultiplier);
                if (Rand.Chance(0.85f))
                {
                    FleckMaker.ThrowMicroSparks(position, map);
                }
            }
        }

        public static void SpawnSplitBurst(Vector3 position, Map map)
        {
            SpawnAnimatedMote(SplitBurstMoteDef, SplitBurstFramePrefix, 8, 1, 12, 1.42f, 1.42f, position, map, Rand.Range(0f, 360f));
            SpawnAnimatedMote(SplitBurstMoteDef, SplitBurstFramePrefix, 8, 1, 9, 1.02f, 1.02f, position + new Vector3(0f, 0f, 0.02f), map, Rand.Range(0f, 360f));
            if (map != null)
            {
                FleckMaker.ThrowLightningGlow(position, map, 0.56f);
                FleckMaker.ThrowMicroSparks(position, map);
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnMicroTrail(Vector3 position, Vector3 direction, Map map, float sizeMultiplier = 1f)
        {
            float angle = DirectionAngle(direction) - 90f;
            float size = 0.30f * sizeMultiplier;
            int lifetime = Mathf.Max(5, Mathf.RoundToInt(7f * sizeMultiplier));
            SpawnAnimatedMote(MicroTrailMoteDef, MicroTrailFramePrefix, 6, 1, lifetime, size, size, position, map, angle);
        }

        public static void SpawnSplitReleaseAccent(Vector3 position, IEnumerable<Vector3> directions, Map map)
        {
            if (map == null || directions == null)
            {
                return;
            }

            foreach (Vector3 direction in directions)
            {
                Vector3 normalized = direction;
                normalized.y = 0f;
                if (normalized.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                normalized.Normalize();
                Vector3 offset = normalized * 0.12f;
                SpawnMicroTrail(position + offset, normalized, map, 1.15f);
            }
        }

        public static void SpawnMicroImpact(Vector3 position, Map map, float sizeMultiplier = 1f)
        {
            SpawnMicroDetonation(position, Vector3.forward, map, sizeMultiplier);
        }

        public static void SpawnMicroDetonation(Vector3 position, Vector3 incomingDirection, Map map, float sizeMultiplier = 1f)
        {
            Vector3 direction = incomingDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();
            Vector3 side = new Vector3(direction.z, 0f, -direction.x);
            float size = Mathf.Clamp(0.68f * sizeMultiplier, 0.44f, 0.96f);
            float angle = DirectionAngle(direction) - 90f;

            SpawnAnimatedMote(MicroImpactMoteDef, MicroImpactFramePrefix, 8, 1, 10, size, size, position, map, angle + Rand.Range(-16f, 16f));
            SpawnAnimatedMote(MicroImpactMoteDef, MicroImpactFramePrefix, 8, 1, 8, size * 0.74f, size * 0.74f, position - direction * 0.10f, map, angle + Rand.Range(130f, 230f));
            SpawnAnimatedMote(MicroTrailMoteDef, MicroTrailFramePrefix, 6, 1, 6, 0.34f * sizeMultiplier, 0.34f * sizeMultiplier, position - direction * 0.16f, map, angle + 180f + Rand.Range(-18f, 18f));
            SpawnAnimatedMote(MicroTrailMoteDef, MicroTrailFramePrefix, 6, 1, 5, 0.22f * sizeMultiplier, 0.22f * sizeMultiplier, position + side * 0.10f, map, angle + 85f + Rand.Range(-20f, 20f));
            SpawnAnimatedMote(MicroTrailMoteDef, MicroTrailFramePrefix, 6, 1, 5, 0.22f * sizeMultiplier, 0.22f * sizeMultiplier, position - side * 0.10f, map, angle - 85f + Rand.Range(-20f, 20f));

            if (map != null)
            {
                FleckMaker.ThrowLightningGlow(position, map, 0.30f * sizeMultiplier);
                FleckMaker.ThrowMicroSparks(position, map);
                if (Rand.Chance(0.72f))
                {
                    FleckMaker.ThrowMicroSparks(position - direction * 0.08f, map);
                }
            }
        }

        private static void SpawnAnimatedMote(
            ThingDef moteDef,
            string framePathPrefix,
            int frameCount,
            int ticksPerFrame,
            int lifetimeTicks,
            float drawSizeX,
            float drawSizeZ,
            Vector3 position,
            Map map,
            float rotation)
        {
            if (moteDef == null || map == null)
            {
                return;
            }

            IntVec3 cell = position.ToIntVec3();
            if (!cell.InBounds(map))
            {
                return;
            }

            Mote_ABY_PlasmaLanceAnimated mote = ThingMaker.MakeThing(moteDef) as Mote_ABY_PlasmaLanceAnimated;
            if (mote == null)
            {
                return;
            }

            mote.framePathPrefix = framePathPrefix;
            mote.frameCount = Mathf.Max(1, frameCount);
            mote.ticksPerFrame = Mathf.Max(1, ticksPerFrame);
            mote.ticksLeft = Mathf.Max(1, lifetimeTicks);
            mote.startingTicks = mote.ticksLeft;
            mote.drawSizeX = drawSizeX;
            mote.drawSizeZ = drawSizeZ;
            mote.rotation = rotation;
            mote.exactPosition = position;

            GenSpawn.Spawn(mote, cell, map);
        }

        private static float DirectionAngle(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
    }
}
