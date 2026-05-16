using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class NullArcDischargerVfxUtility
    {
        private const string MuzzleMoteDefName = "ABY_Mote_NullArcDischargerMuzzle";
        private const string ImpactMoteDefName = "ABY_Mote_NullArcDischargerImpact";
        private const string BeamMoteDefName = "ABY_Mote_NullArcDischargerBeam";

        private const string MuzzleFramePrefix = "Things/VFX/NullArcDischarger/ABY_NullArcMuzzle_";
        private const string ImpactFramePrefix = "Things/VFX/NullArcDischarger/ABY_NullArcImpact_";
        private const string BeamFramePrefix = "Things/VFX/NullArcDischarger/ABY_NullArcChain_";

        private const int MuzzleFrameCount = 6;
        private const int ImpactFrameCount = 8;
        private const int BeamFrameCount = 6;
        private const int MuzzleTicksPerFrame = 1;
        private const int ImpactTicksPerFrame = 2;
        private const int BeamTicksPerFrame = 1;

        private static ThingDef muzzleMoteDef;
        private static ThingDef impactMoteDef;
        private static ThingDef beamMoteDef;

        private static ThingDef MuzzleMoteDef => muzzleMoteDef ?? (muzzleMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MuzzleMoteDefName));
        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));
        private static ThingDef BeamMoteDef => beamMoteDef ?? (beamMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(BeamMoteDefName));

        public static void SpawnMuzzle(Vector3 source, Vector3 destination, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = destination - source;
            direction.y = 0f;
            float angle = DirectionAngle(direction);
            SpawnAnimatedMote(MuzzleMoteDef, MuzzleFramePrefix, MuzzleFrameCount, MuzzleTicksPerFrame, MuzzleFrameCount * MuzzleTicksPerFrame, 0.72f, 0.72f, source, map, angle - 90f);
            FleckMaker.ThrowLightningGlow(source, map, 0.42f);
            if (Rand.Chance(0.35f))
            {
                FleckMaker.ThrowMicroSparks(source, map);
            }
        }

        public static void SpawnImpact(Vector3 position, Map map, bool blockedByShield, bool chained)
        {
            if (map == null)
            {
                return;
            }

            float drawSize = blockedByShield ? 1.12f : chained ? 0.78f : 0.96f;
            SpawnAnimatedMote(ImpactMoteDef, ImpactFramePrefix, ImpactFrameCount, ImpactTicksPerFrame, ImpactFrameCount * ImpactTicksPerFrame, drawSize, drawSize, position, map, Rand.Range(0f, 360f));
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 1.25f : chained ? 0.78f : 1.02f);
            FleckMaker.ThrowMicroSparks(position, map);
            if (blockedByShield || Rand.Chance(0.45f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnBeam(Vector3 source, Vector3 target, Map map, bool chained)
        {
            ThingDef beamDef = BeamMoteDef;
            if (beamDef == null || map == null)
            {
                return;
            }

            Vector3 start = source;
            Vector3 end = target;
            start.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            end.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            float distance = (end - start).MagnitudeHorizontal();
            if (distance <= 0.08f)
            {
                return;
            }

            Mote_ABY_NullArcBeamSegment beam = ThingMaker.MakeThing(beamDef) as Mote_ABY_NullArcBeamSegment;
            if (beam == null)
            {
                return;
            }

            int lifetime = chained ? 6 : 8;
            beam.start = start;
            beam.end = end;
            beam.framePathPrefix = BeamFramePrefix;
            beam.frameCount = BeamFrameCount;
            beam.ticksPerFrame = BeamTicksPerFrame;
            beam.ticksLeft = lifetime;
            beam.startingTicks = lifetime;
            beam.width = chained ? 0.18f : 0.24f;

            IntVec3 spawnCell = start.ToIntVec3();
            if (!spawnCell.InBounds(map))
            {
                spawnCell = end.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                return;
            }

            GenSpawn.Spawn(beam, spawnCell, map);

            int sparkSteps = Mathf.Clamp(Mathf.CeilToInt(distance * 0.22f), 1, 6);
            for (int i = 1; i <= sparkSteps; i++)
            {
                if (!Rand.Chance(chained ? 0.33f : 0.55f))
                {
                    continue;
                }

                float t = i / (float)(sparkSteps + 1);
                Vector3 point = Vector3.Lerp(start, end, t);
                point += new Vector3(Rand.Range(-0.035f, 0.035f), 0f, Rand.Range(-0.035f, 0.035f));
                FleckMaker.ThrowLightningGlow(point, map, chained ? 0.20f : 0.28f);
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
