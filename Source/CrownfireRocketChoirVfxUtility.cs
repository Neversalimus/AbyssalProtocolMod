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
            SpawnAnimatedMote(TubeIgnitionMoteDef, TubeIgnitionFramePrefix, 6, 1, 6, 0.48f, 0.48f, position, map, 0f);
            if (map != null)
            {
                FleckMaker.ThrowLightningGlow(position, map, 0.18f);
            }
        }

        public static void SpawnLaunchExhaust(Vector3 position, Map map)
        {
            SpawnAnimatedMote(LaunchExhaustMoteDef, LaunchExhaustFramePrefix, 8, 2, 16, 0.82f, 0.82f, position, map, 0f);
            if (map != null && Rand.Chance(0.45f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnSplitBurst(Vector3 position, Map map)
        {
            SpawnAnimatedMote(SplitBurstMoteDef, SplitBurstFramePrefix, 8, 1, 8, 1.08f, 1.08f, position, map, Rand.Range(0f, 360f));
            if (map != null)
            {
                FleckMaker.ThrowLightningGlow(position, map, 0.45f);
            }
        }

        public static void SpawnMicroTrail(Vector3 position, Vector3 direction, Map map)
        {
            float angle = DirectionAngle(direction) - 90f;
            SpawnAnimatedMote(MicroTrailMoteDef, MicroTrailFramePrefix, 6, 1, 6, 0.26f, 0.26f, position, map, angle);
        }

        public static void SpawnMicroImpact(Vector3 position, Map map, float sizeMultiplier = 1f)
        {
            float size = Mathf.Clamp(0.48f * sizeMultiplier, 0.30f, 0.66f);
            SpawnAnimatedMote(MicroImpactMoteDef, MicroImpactFramePrefix, 8, 1, 8, size, size, position, map, Rand.Range(0f, 360f));
            if (map != null && Rand.Chance(0.35f))
            {
                FleckMaker.ThrowLightningGlow(position, map, 0.18f);
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
