using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalHarpoonVfxUtility
    {
        private const string ImpactMoteDefName = "ABY_Mote_AbyssalHarpoonImpact";
        private const string TetherMoteDefName = "ABY_Mote_AbyssalHarpoonTetherLine";
        private const string MarkerMoteDefName = "ABY_Mote_AbyssalHarpoonMarker";

        private const string ImpactFramePrefix = "Things/VFX/AbyssalHarpoon/ABY_HarpoonImpact_";
        private const string TetherFramePrefix = "Things/VFX/AbyssalHarpoon/ABY_HarpoonTether_";
        private const string MarkerFramePrefix = "Things/VFX/AbyssalHarpoon/ABY_HarpoonMarker_";

        private const int ImpactFrameCount = 8;
        private const int TetherFrameCount = 6;
        private const int MarkerFrameCount = 6;
        private const int ImpactTicksPerFrame = 2;
        private const int TetherTicksPerFrame = 1;
        private const int MarkerTicksPerFrame = 4;

        private static ThingDef impactMoteDef;
        private static ThingDef tetherMoteDef;
        private static ThingDef markerMoteDef;

        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));
        private static ThingDef TetherMoteDef => tetherMoteDef ?? (tetherMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(TetherMoteDefName));
        private static ThingDef MarkerMoteDef => markerMoteDef ?? (markerMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MarkerMoteDefName));

        public static void SpawnLaunchSpark(Vector3 source, Vector3 destination, Map map)
        {
            if (map == null)
            {
                return;
            }

            IntVec3 cell = source.ToIntVec3();
            if (!cell.InBounds(map))
            {
                return;
            }

            FleckMaker.ThrowMicroSparks(source, map);
            if (Rand.Chance(0.45f))
            {
                FleckMaker.ThrowDustPuffThick(source, map, 0.38f, new Color(0.58f, 0.08f, 0.10f, 0.48f));
            }
        }

        public static void SpawnImpact(Vector3 position, Map map, bool reduced = false)
        {
            if (map == null)
            {
                return;
            }

            float drawSize = reduced ? 0.72f : 0.88f;
            SpawnAnimatedMote(ImpactMoteDef, ImpactFramePrefix, ImpactFrameCount, ImpactTicksPerFrame, ImpactFrameCount * ImpactTicksPerFrame, drawSize, drawSize, position, map, Rand.Range(0f, 360f));
            FleckMaker.ThrowMicroSparks(position, map);
            if (!reduced && Rand.Chance(0.38f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnMarker(Vector3 position, Map map, bool reduced = false)
        {
            if (map == null)
            {
                return;
            }

            float drawSize = reduced ? 0.58f : 0.72f;
            SpawnAnimatedMote(MarkerMoteDef, MarkerFramePrefix, MarkerFrameCount, MarkerTicksPerFrame, MarkerFrameCount * MarkerTicksPerFrame, drawSize, drawSize, position, map, Rand.Range(0f, 360f));
        }

        public static void SpawnTether(Vector3 source, Vector3 target, Map map, bool reduced = false)
        {
            ThingDef tetherDef = TetherMoteDef;
            if (tetherDef == null || map == null)
            {
                return;
            }

            Vector3 start = source;
            Vector3 end = target;
            start.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            end.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            float distance = (end - start).MagnitudeHorizontal();
            if (distance <= 0.10f)
            {
                return;
            }

            Mote_ABY_NullArcBeamSegment tether = ThingMaker.MakeThing(tetherDef) as Mote_ABY_NullArcBeamSegment;
            if (tether == null)
            {
                return;
            }

            int lifetime = reduced ? 7 : 9;
            tether.start = start;
            tether.end = end;
            tether.framePathPrefix = TetherFramePrefix;
            tether.frameCount = TetherFrameCount;
            tether.ticksPerFrame = TetherTicksPerFrame;
            tether.ticksLeft = lifetime;
            tether.startingTicks = lifetime;
            tether.width = reduced ? 0.105f : 0.145f;

            IntVec3 spawnCell = start.ToIntVec3();
            if (!spawnCell.InBounds(map))
            {
                spawnCell = end.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                return;
            }

            GenSpawn.Spawn(tether, spawnCell, map);
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
    }
}
