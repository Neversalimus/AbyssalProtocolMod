using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class SepulcherRailVfxUtility
    {
        private const string ImpactMoteDefName = "ABY_Mote_SepulcherRailImpactAnimated";
        private const string ImpactFramePrefix = "Things/VFX/SepulcherRail/ABY_SepulcherRailImpact_";

        private static ThingDef impactMoteDef;
        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));

        public static void SpawnTravelSpark(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, 0.20f);
            if (Rand.Chance(0.22f))
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

            float angle = DirectionAngle(travelDirection);
            float scale = blockedByShield ? 0.82f : 1.04f;
            SpawnAnimated(ImpactMoteDef, position, map, ImpactFramePrefix, 8, 1, 8, scale, scale, angle);
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.70f : 1.05f);
            FleckMaker.ThrowMicroSparks(position, map);
            if (!blockedByShield && Rand.Chance(0.48f))
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

            Mote_ABY_SepulcherRailAnimated mote = ThingMaker.MakeThing(moteDef) as Mote_ABY_SepulcherRailAnimated;
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
