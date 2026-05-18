using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class CrowncoilGaussVfxUtility
    {
        private const string ImpactMoteDefName = "ABY_Mote_CrowncoilGaussImpact";
        private const string ImpactFramePrefix = "Things/VFX/CrowncoilGauss/ABY_CrowncoilGaussImpact_";
        private const int ImpactFrameCount = 8;
        private const int ImpactTicksPerFrame = 1;

        private static ThingDef impactMoteDef;

        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));

        public static void SpawnTravelSpark(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, 0.075f);
            if (Rand.Chance(0.08f))
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
            float drawSize = blockedByShield ? 0.42f : 0.56f;
            SpawnAnimatedMote(
                ImpactMoteDef,
                ImpactFramePrefix,
                ImpactFrameCount,
                ImpactTicksPerFrame,
                ImpactFrameCount * ImpactTicksPerFrame + 1,
                drawSize,
                drawSize,
                position,
                map,
                angle);

            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.22f : 0.34f);
            if (!blockedByShield && Rand.Chance(0.18f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
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
