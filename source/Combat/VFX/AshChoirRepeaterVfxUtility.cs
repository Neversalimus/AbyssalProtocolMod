using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AshChoirRepeaterVfxUtility
    {
        private const string MuzzleMoteDefName = "ABY_Mote_AshChoirRepeaterMuzzle";
        private const string ImpactMoteDefName = "ABY_Mote_AshChoirRepeaterImpact";
        private const string MuzzleFramePrefix = "Things/VFX/AshChoirRepeater/ABY_AshChoirRepeaterMuzzle_";
        private const string ImpactFramePrefix = "Things/VFX/AshChoirRepeater/ABY_AshChoirRepeaterImpact_";

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
            SpawnStatic(MuzzleMoteDef, source, map, MuzzleFramePrefix, 5, 0.58f, 0.58f, angle - 90f);
            FleckMaker.ThrowLightningGlow(source, map, 0.28f);
            if (Rand.Chance(0.18f))
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

            FleckMaker.ThrowLightningGlow(position, map, 0.08f);
            if (Rand.Chance(0.10f))
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
            float scale = blockedByShield ? 0.42f : 0.56f;
            SpawnStatic(ImpactMoteDef, position, map, ImpactFramePrefix, blockedByShield ? 4 : 5, scale, scale, angle);
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.24f : 0.36f);
            if (Rand.Chance(blockedByShield ? 0.12f : 0.24f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        private static void SpawnStatic(
            ThingDef moteDef,
            Vector3 position,
            Map map,
            string framePathPrefix,
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
            mote.frameCount = 1;
            mote.ticksPerFrame = 1;
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
