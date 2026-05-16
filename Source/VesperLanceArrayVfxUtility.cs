using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class VesperLanceArrayVfxUtility
    {
        private const string MuzzleMoteDefName = "ABY_Mote_VesperLanceArrayMuzzle";
        private const string ImpactMoteDefName = "ABY_Mote_VesperLanceArrayImpact";
        private const string MuzzleFramePrefix = "Things/VFX/VesperLanceArray/ABY_VesperLanceArrayMuzzle_";
        private const string ImpactFramePrefix = "Things/VFX/VesperLanceArray/ABY_VesperLanceArrayImpact_";
        private const int MuzzleFrameCount = 6;
        private const int ImpactFrameCount = 6;
        private const int TicksPerFrame = 1;
        private const float MuzzleDrawSize = 0.94f;
        private const float ImpactDrawSize = 0.88f;

        private static ThingDef muzzleMoteDef;
        private static ThingDef impactMoteDef;

        private static ThingDef MuzzleMoteDef => muzzleMoteDef ?? (muzzleMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MuzzleMoteDefName));
        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));

        public static void SpawnMuzzle(Vector3 source, Vector3 destination, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = destination - source;
            direction.y = 0f;
            float angle = DirectionAngle(direction);
            SpawnAnimatedMote(MuzzleMoteDef, MuzzleFramePrefix, MuzzleFrameCount, TicksPerFrame, MuzzleFrameCount * TicksPerFrame, MuzzleDrawSize, MuzzleDrawSize, source, map, angle - 90f);
            FleckMaker.ThrowLightningGlow(source, map, 0.42f);
        }

        public static void SpawnImpact(Vector3 position, Vector3 direction, Map map, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            float angle = DirectionAngle(direction);
            float drawSize = blockedByShield ? 0.76f : ImpactDrawSize;
            SpawnAnimatedMote(ImpactMoteDef, ImpactFramePrefix, ImpactFrameCount, TicksPerFrame, ImpactFrameCount * TicksPerFrame, drawSize, drawSize, position, map, angle - 90f);
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.62f : 0.85f);
            if (Rand.Chance(blockedByShield ? 0.20f : 0.45f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnTravelSpark(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, 0.16f);
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
