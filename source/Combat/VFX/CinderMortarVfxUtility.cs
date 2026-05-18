using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class CinderMortarVfxUtility
    {
        private const string LaunchMoteDefName = "ABY_Mote_CinderMortarLaunch";
        private const string ImpactMoteDefName = "ABY_Mote_CinderMortarImpact";
        private const string ResiduePatchDefName = "ABY_CinderResiduePatch";

        private const string LaunchFramePrefix = "Things/VFX/CinderMortar/ABY_CinderMortarLaunch_";
        private const string ImpactFramePrefix = "Things/VFX/CinderMortar/ABY_CinderMortarImpact_";

        private const int LaunchFrameCount = 6;
        private const int ImpactFrameCount = 8;
        private const int LaunchTicksPerFrame = 1;
        private const int ImpactTicksPerFrame = 2;

        private const float LaunchDrawSize = 0.78f;
        private const float ImpactDrawSize = 2.18f;
        private const float ResidueSpawnRadius = 1.65f;

        private static ThingDef launchMoteDef;
        private static ThingDef impactMoteDef;
        private static ThingDef residuePatchDef;

        private static ThingDef LaunchMoteDef => launchMoteDef ?? (launchMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(LaunchMoteDefName));
        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));
        private static ThingDef ResiduePatchDef => residuePatchDef ?? (residuePatchDef = DefDatabase<ThingDef>.GetNamedSilentFail(ResiduePatchDefName));

        public static void SpawnLaunch(Vector3 source, Vector3 destination, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = destination - source;
            direction.y = 0f;
            float angle = DirectionAngle(direction);
            SpawnAnimatedMote(LaunchMoteDef, LaunchFramePrefix, LaunchFrameCount, LaunchTicksPerFrame, LaunchFrameCount * LaunchTicksPerFrame, LaunchDrawSize, LaunchDrawSize, source, map, angle - 90f);
            FleckMaker.ThrowLightningGlow(source, map, 0.34f);
        }

        public static void SpawnImpact(Vector3 position, Vector3 direction, Map map, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            float angle = DirectionAngle(direction);
            float drawSize = blockedByShield ? ImpactDrawSize * 0.76f : ImpactDrawSize;
            SpawnAnimatedMote(ImpactMoteDef, ImpactFramePrefix, ImpactFrameCount, ImpactTicksPerFrame, ImpactFrameCount * ImpactTicksPerFrame, drawSize, drawSize, position, map, angle - 90f);
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.55f : 0.85f);
            if (Rand.Chance(blockedByShield ? 0.25f : 0.50f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnResiduePatch(IntVec3 center, Map map, Thing instigator = null)
        {
            ThingDef patchDef = ResiduePatchDef;
            if (patchDef == null || map == null || !center.InBounds(map))
            {
                return;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, ResidueSpawnRadius, true))
            {
                if (!cell.InBounds(map) || cell.Impassable(map))
                {
                    continue;
                }

                if (cell != center && !Rand.Chance(0.58f))
                {
                    continue;
                }

                bool alreadyPresent = false;
                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing != null && thing.def == patchDef)
                    {
                        alreadyPresent = true;
                        break;
                    }
                }

                if (alreadyPresent)
                {
                    continue;
                }

                Thing thingPatch = ThingMaker.MakeThing(patchDef);
                Thing_ABY_CinderResiduePatch residuePatch = thingPatch as Thing_ABY_CinderResiduePatch;
                residuePatch?.Initialize(instigator);
                GenSpawn.Spawn(thingPatch, cell, map);
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
