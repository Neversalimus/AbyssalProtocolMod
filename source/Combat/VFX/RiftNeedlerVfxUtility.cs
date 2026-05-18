using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class RiftNeedlerVfxUtility
    {
        private const string MuzzleMoteDefName = "ABY_Mote_RiftNeedlerMuzzleAnimated";
        private const string ImpactMoteDefName = "ABY_Mote_RiftNeedlerImpactAnimated";
        private const string MuzzleFramePrefix = "Things/VFX/RiftNeedler/ABY_RiftNeedlerMuzzle_";
        private const string ImpactFramePrefix = "Things/VFX/RiftNeedler/ABY_RiftNeedlerImpact_";

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
            SpawnAnimated(MuzzleMoteDef, source, map, MuzzleFramePrefix, 8, 1, 8, 0.86f, 0.86f, angle - 90f);
            FleckMaker.ThrowLightningGlow(source, map, 0.48f);
            if (Rand.Chance(0.32f))
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

            FleckMaker.ThrowLightningGlow(position, map, 0.14f);
            if (Rand.Chance(0.18f))
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
            float scale = blockedByShield ? 0.60f : 0.72f;
            SpawnAnimated(ImpactMoteDef, position, map, ImpactFramePrefix, 8, 1, 8, scale, scale, angle);
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.44f : 0.62f);
            if (Rand.Chance(blockedByShield ? 0.25f : 0.45f))
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

            Mote_ABY_RiftNeedlerAnimated mote = ThingMaker.MakeThing(moteDef) as Mote_ABY_RiftNeedlerAnimated;
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
