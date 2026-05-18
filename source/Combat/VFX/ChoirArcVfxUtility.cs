using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ChoirArcVfxUtility
    {
        private const string MuzzleMoteDefName = "ABY_Mote_ChoirArcMuzzleAnimated";
        private const string ImpactMoteDefName = "ABY_Mote_ChoirArcImpactAnimated";
        private const string MuzzleFramePrefix = "Things/VFX/ChoirArc/ABY_ChoirArcMuzzle_";
        private const string ImpactFramePrefix = "Things/VFX/ChoirArc/ABY_ChoirArcImpact_";

        private static ThingDef muzzleMoteDef;
        private static ThingDef impactMoteDef;

        private static ThingDef MuzzleMoteDef => muzzleMoteDef ?? (muzzleMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MuzzleMoteDefName));
        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));

        public static void SpawnMuzzle(Vector3 source, Vector3 target, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = target - source;
            direction.y = 0f;
            float angle = DirectionAngle(direction);
            SpawnAnimated(MuzzleMoteDef, source, map, MuzzleFramePrefix, 8, 2, 16, 1.18f, 1.18f, angle - 90f);
            FleckMaker.ThrowLightningGlow(source, map, 0.90f);
            if (Rand.Chance(0.70f))
            {
                FleckMaker.ThrowMicroSparks(source, map);
            }
        }

        public static void SpawnTravelSpark(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, 0.22f);
            if (Rand.Chance(0.35f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnImpact(Vector3 position, Vector3 travelDirection, Map map, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            float angle = DirectionAngle(travelDirection) - 90f;
            float scale = blockedByShield ? 1.00f : 1.18f;
            SpawnAnimated(ImpactMoteDef, position, map, ImpactFramePrefix, 8, 2, 16, scale, scale, angle);
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.95f : 1.35f);
            FleckMaker.ThrowMicroSparks(position, map);
            if (!blockedByShield && Rand.Chance(0.65f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        private static void SpawnAnimated(
            ThingDef moteDef,
            Vector3 position,
            Map map,
            string framePathPrefix,
            int frameCount,
            int ticksPerFrame,
            int lifetimeTicks,
            float drawSizeX,
            float drawSizeZ,
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

            Mote_ABY_ChoirArcAnimated mote = ThingMaker.MakeThing(moteDef) as Mote_ABY_ChoirArcAnimated;
            if (mote == null)
            {
                return;
            }

            mote.framePathPrefix = framePathPrefix;
            mote.frameCount = frameCount;
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
