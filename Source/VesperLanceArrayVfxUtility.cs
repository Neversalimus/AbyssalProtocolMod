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
        private const float MuzzleDrawSize = 0.94f;
        private const float ImpactDrawSize = 0.88f;

        public static void SpawnMuzzle(Vector3 source, Vector3 destination, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = destination - source;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            float angle = DirectionAngle(direction);
            SpawnAnimatedMote(MuzzleMoteDefName, MuzzleFramePrefix, MuzzleFrameCount, MuzzleDrawSize, source, map, angle - 90f);
        }

        public static void SpawnImpact(Vector3 position, Vector3 direction, Map map, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            Vector3 normalizedDirection = direction;
            normalizedDirection.y = 0f;
            if (normalizedDirection.sqrMagnitude <= 0.0001f)
            {
                normalizedDirection = Vector3.forward;
            }

            float angle = DirectionAngle(normalizedDirection);
            SpawnAnimatedMote(ImpactMoteDefName, ImpactFramePrefix, ImpactFrameCount, blockedByShield ? 0.76f : ImpactDrawSize, position, map, angle - 90f);
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.62f : 0.85f);
            FleckMaker.ThrowMicroSparks(position, map);
        }

        public static void SpawnTravelSpark(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, 0.16f);
        }

        private static void SpawnAnimatedMote(string defName, string framePrefix, int frameCount, float drawSize, Vector3 position, Map map, float rotation)
        {
            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (moteDef == null)
            {
                return;
            }

            Mote_ABY_PlasmaLanceAnimated mote = ThingMaker.MakeThing(moteDef) as Mote_ABY_PlasmaLanceAnimated;
            if (mote == null)
            {
                return;
            }

            mote.Initialize(framePrefix, frameCount, drawSize, rotation);
            mote.exactPosition = position;
            mote.Position = position.ToIntVec3();
            GenSpawn.Spawn(mote, mote.Position, map);
        }

        private static float DirectionAngle(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
    }
}
