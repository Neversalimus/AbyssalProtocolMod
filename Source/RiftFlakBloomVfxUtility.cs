using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class RiftFlakBloomVfxUtility
    {
        private const string MuzzleMoteDefName = "ABY_Mote_RiftFlakBloomMuzzle";
        private const string BloomMoteDefName = "ABY_Mote_RiftFlakBloomBurst";
        private const string ShardImpactMoteDefName = "ABY_Mote_RiftFlakShardImpact";

        private const string MuzzleFramePrefix = "Things/VFX/RiftFlakBloom/ABY_RiftFlakMuzzle_";
        private const string BloomFramePrefix = "Things/VFX/RiftFlakBloom/ABY_RiftFlakBloom_";
        private const string ShardImpactFramePrefix = "Things/VFX/RiftFlakBloom/ABY_RiftFlakShardImpact_";

        private const int MuzzleFrameCount = 6;
        private const int BloomFrameCount = 8;
        private const int ShardImpactFrameCount = 6;
        private const int MuzzleTicksPerFrame = 1;
        private const int BloomTicksPerFrame = 2;
        private const int ShardImpactTicksPerFrame = 1;

        private static ThingDef muzzleMoteDef;
        private static ThingDef bloomMoteDef;
        private static ThingDef shardImpactMoteDef;

        private static ThingDef MuzzleMoteDef => muzzleMoteDef ?? (muzzleMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MuzzleMoteDefName));
        private static ThingDef BloomMoteDef => bloomMoteDef ?? (bloomMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(BloomMoteDefName));
        private static ThingDef ShardImpactMoteDef => shardImpactMoteDef ?? (shardImpactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ShardImpactMoteDefName));

        public static void SpawnMuzzle(Vector3 source, Vector3 destination, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = destination - source;
            direction.y = 0f;
            float angle = DirectionAngle(direction);
            SpawnAnimatedMote(MuzzleMoteDef, MuzzleFramePrefix, MuzzleFrameCount, MuzzleTicksPerFrame, MuzzleFrameCount * MuzzleTicksPerFrame, 0.58f, 0.58f, source, map, angle - 90f);
            FleckMaker.ThrowLightningGlow(source, map, 0.22f);
            if (Rand.Chance(0.25f))
            {
                FleckMaker.ThrowMicroSparks(source, map);
            }
        }

        public static void SpawnBloom(Vector3 position, Map map, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            float drawSize = blockedByShield ? 0.82f : 1.30f;
            SpawnAnimatedMote(BloomMoteDef, BloomFramePrefix, BloomFrameCount, BloomTicksPerFrame, BloomFrameCount * BloomTicksPerFrame, drawSize, drawSize, position, map, Rand.Range(0f, 360f));
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.45f : 0.72f);
            if (!blockedByShield)
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnShardImpact(Vector3 position, Map map, float sizeMultiplier = 1f)
        {
            if (map == null)
            {
                return;
            }

            float drawSize = Mathf.Clamp(0.34f * sizeMultiplier, 0.22f, 0.46f);
            Vector3 adjusted = position;
            adjusted.x += Rand.Range(-0.06f, 0.06f);
            adjusted.z += Rand.Range(-0.06f, 0.06f);
            SpawnAnimatedMote(ShardImpactMoteDef, ShardImpactFramePrefix, ShardImpactFrameCount, ShardImpactTicksPerFrame, ShardImpactFrameCount * ShardImpactTicksPerFrame, drawSize, drawSize, adjusted, map, Rand.Range(0f, 360f));

            if (Rand.Chance(0.30f))
            {
                FleckMaker.ThrowLightningGlow(adjusted, map, 0.12f);
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
