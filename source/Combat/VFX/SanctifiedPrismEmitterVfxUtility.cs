using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class SanctifiedPrismEmitterVfxUtility
    {
        private const string MuzzleMoteDefName = "ABY_Mote_SanctifiedPrismMuzzle";
        private const string ImpactMoteDefName = "ABY_Mote_SanctifiedPrismImpact";
        private const string SecondaryHitMoteDefName = "ABY_Mote_SanctifiedPrismSecondaryHit";
        private const string BeamMoteDefName = "ABY_Mote_SanctifiedPrismBeam";
        private const string TravelCutMoteDefName = "ABY_Mote_SanctifiedPrismTravelCut";
        private const string ResidualScorchMoteDefName = "ABY_Mote_SanctifiedPrismResidualScorch";

        private const string MuzzleFramePrefix = "Things/VFX/SanctifiedPrism/ABY_SanctifiedPrismMuzzle_";
        private const string ImpactFramePrefix = "Things/VFX/SanctifiedPrism/ABY_SanctifiedPrismImpact_";
        private const string SecondaryHitFramePrefix = "Things/VFX/SanctifiedPrism/ABY_SanctifiedPrismSecondaryHit_";
        private const string BeamFramePrefix = "Things/VFX/SanctifiedPrism/ABY_SanctifiedPrismBeam_";
        private const string TravelCutFramePrefix = "Things/VFX/SanctifiedPrism/ABY_SanctifiedPrismTravelCut_";
        private const string ResidualScorchFramePrefix = "Things/VFX/SanctifiedPrism/ABY_SanctifiedPrismResidualScorch_";

        private const int MuzzleFrameCount = 6;
        private const int ImpactFrameCount = 8;
        private const int SecondaryHitFrameCount = 6;
        private const int BeamFrameCount = 6;
        private const int TravelCutFrameCount = 6;
        private const int MuzzleTicksPerFrame = 1;
        private const int ImpactTicksPerFrame = 2;
        private const int SecondaryHitTicksPerFrame = 1;
        private const int BeamTicksPerFrame = 1;
        private const int TravelCutTicksPerFrame = 1;

        private static ThingDef muzzleMoteDef;
        private static ThingDef impactMoteDef;
        private static ThingDef secondaryHitMoteDef;
        private static ThingDef beamMoteDef;
        private static ThingDef travelCutMoteDef;
        private static ThingDef residualScorchMoteDef;

        private static ThingDef MuzzleMoteDef => muzzleMoteDef ?? (muzzleMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MuzzleMoteDefName));
        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));
        private static ThingDef SecondaryHitMoteDef => secondaryHitMoteDef ?? (secondaryHitMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(SecondaryHitMoteDefName));
        private static ThingDef BeamMoteDef => beamMoteDef ?? (beamMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(BeamMoteDefName));
        private static ThingDef TravelCutMoteDef => travelCutMoteDef ?? (travelCutMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(TravelCutMoteDefName));
        private static ThingDef ResidualScorchMoteDef => residualScorchMoteDef ?? (residualScorchMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ResidualScorchMoteDefName));

        public static void SpawnMuzzle(Vector3 source, Vector3 destination, Map map)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = destination - source;
            direction.y = 0f;
            float angle = DirectionAngle(direction);
            SpawnAnimatedMote(MuzzleMoteDef, MuzzleFramePrefix, MuzzleFrameCount, MuzzleTicksPerFrame, MuzzleFrameCount * MuzzleTicksPerFrame + 2, 0.72f, 0.72f, source, map, angle - 90f);
            FleckMaker.ThrowLightningGlow(source, map, 0.34f);
            if (Rand.Chance(0.22f))
            {
                FleckMaker.ThrowMicroSparks(source, map);
            }
        }

        public static void SpawnPrimaryImpact(Vector3 position, Vector3 direction, Map map, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            float angle = DirectionAngle(direction);
            float drawSize = blockedByShield ? 0.72f : 0.96f;
            SpawnAnimatedMote(ImpactMoteDef, ImpactFramePrefix, ImpactFrameCount, ImpactTicksPerFrame, ImpactFrameCount * ImpactTicksPerFrame, drawSize, drawSize, position, map, angle - 90f);
            FleckMaker.ThrowLightningGlow(position, map, blockedByShield ? 0.50f : 0.88f);
            if (!blockedByShield && Rand.Chance(0.38f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnSecondaryHit(Vector3 position, Map map, float sizeMultiplier = 1f)
        {
            if (map == null)
            {
                return;
            }

            float drawSize = Mathf.Clamp(0.46f * sizeMultiplier, 0.30f, 0.62f);
            SpawnAnimatedMote(SecondaryHitMoteDef, SecondaryHitFramePrefix, SecondaryHitFrameCount, SecondaryHitTicksPerFrame, SecondaryHitFrameCount * SecondaryHitTicksPerFrame + 2, drawSize, drawSize, position, map, Rand.Range(0f, 360f));
            FleckMaker.ThrowLightningGlow(position, map, 0.32f * sizeMultiplier);
        }

        public static void SpawnResidualScorch(Vector3 position, Vector3 direction, Map map)
        {
            if (map == null)
            {
                return;
            }

            float angle = DirectionAngle(direction);
            SpawnAnimatedMote(ResidualScorchMoteDef, ResidualScorchFramePrefix, 1, 16, 16, 0.82f, 0.82f, position, map, angle - 45f);
        }


        public static void SpawnTravelCut(Vector3 source, Vector3 target, Map map, bool primaryShot = false)
        {
            ThingDef travelDef = TravelCutMoteDef;
            if (travelDef == null || map == null)
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

            Mote_ABY_NullArcBeamSegment cut = ThingMaker.MakeThing(travelDef) as Mote_ABY_NullArcBeamSegment;
            if (cut == null)
            {
                return;
            }

            // For the primary shot, draw the whole muzzle-to-target incision at once.
            // This intentionally reads as a fast guillotine trace, not as a tiny flying bullet segment.
            int lifetime = primaryShot ? 10 : 6;
            cut.start = start;
            cut.end = end;
            cut.framePathPrefix = TravelCutFramePrefix;
            cut.frameCount = TravelCutFrameCount;
            cut.ticksPerFrame = TravelCutTicksPerFrame;
            cut.ticksLeft = lifetime;
            cut.startingTicks = lifetime;
            cut.width = primaryShot ? Mathf.Clamp(0.50f + distance * 0.018f, 0.50f, 0.74f) : 0.30f;

            IntVec3 spawnCell = start.ToIntVec3();
            if (!spawnCell.InBounds(map))
            {
                spawnCell = end.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                return;
            }

            GenSpawn.Spawn(cut, spawnCell, map);
        }

        public static void SpawnRefractionBeam(Vector3 source, Vector3 target, Map map, bool faint)
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

            int lifetime = faint ? 8 : 12;
            beam.start = start;
            beam.end = end;
            beam.framePathPrefix = BeamFramePrefix;
            beam.frameCount = BeamFrameCount;
            beam.ticksPerFrame = BeamTicksPerFrame;
            beam.ticksLeft = lifetime;
            beam.startingTicks = lifetime;
            beam.width = faint ? 0.16f : 0.22f;

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

            if (!faint && Rand.Chance(0.45f))
            {
                Vector3 midpoint = Vector3.Lerp(start, end, 0.5f);
                midpoint.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                FleckMaker.ThrowLightningGlow(midpoint, map, 0.18f);
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
