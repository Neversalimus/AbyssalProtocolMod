using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class CrownspikeRailVfxUtility
    {
        private const string RailBeamThingDefName = "ABY_Mote_CrownspikeRailBeam";
        private const string RailHaloThingDefName = "ABY_Mote_CrownspikeRailHalo";
        private const string RailCoreThingDefName = "ABY_Mote_CrownspikeRailCoreBeam";
        private const string MuzzleMoteDefName = "ABY_Mote_CrownspikeRailMuzzle";
        private const string ImpactMoteDefName = "ABY_Mote_CrownspikeRailImpact";
        private const string ShieldMoteDefName = "ABY_Mote_CrownspikeRailShieldImpact";
        private const string DenseImpactMoteDefName = "ABY_Mote_CrownspikeRailDenseImpact";

        private static ThingDef railBeamDef;
        private static ThingDef railHaloDef;
        private static ThingDef railCoreDef;
        private static ThingDef muzzleMoteDef;
        private static ThingDef impactMoteDef;
        private static ThingDef shieldMoteDef;
        private static ThingDef denseImpactMoteDef;

        private static ThingDef RailBeamDef => railBeamDef ?? (railBeamDef = DefDatabase<ThingDef>.GetNamedSilentFail(RailBeamThingDefName));
        private static ThingDef RailHaloDef => railHaloDef ?? (railHaloDef = DefDatabase<ThingDef>.GetNamedSilentFail(RailHaloThingDefName));
        private static ThingDef RailCoreDef => railCoreDef ?? (railCoreDef = DefDatabase<ThingDef>.GetNamedSilentFail(RailCoreThingDefName));
        private static ThingDef MuzzleMoteDef => muzzleMoteDef ?? (muzzleMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MuzzleMoteDefName));
        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));
        private static ThingDef ShieldMoteDef => shieldMoteDef ?? (shieldMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ShieldMoteDefName));
        private static ThingDef DenseImpactMoteDef => denseImpactMoteDef ?? (denseImpactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(DenseImpactMoteDefName));

        public static void SpawnRailDischarge(Vector3 source, Vector3 target, Map map, Thing hitThing, bool blockedByShield)
        {
            if (map == null)
            {
                return;
            }

            source.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            target.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            SpawnMuzzleFlash(source, target, map);
            SpawnRailLine(source, target, map);
            SpawnImpact(target, map, hitThing, blockedByShield);
        }

        private static void SpawnMuzzleFlash(Vector3 source, Vector3 target, Map map)
        {
            ThingDef muzzleDef = MuzzleMoteDef;
            if (muzzleDef != null)
            {
                MakeStaticMote(source, map, muzzleDef, 1.62f);
                MakeStaticMote(Vector3.Lerp(source, target, 0.028f), map, muzzleDef, 0.92f);
                MakeStaticMote(Vector3.Lerp(source, target, 0.058f), map, muzzleDef, 0.48f);
            }

            FleckMaker.ThrowLightningGlow(source, map, 2.25f);
            FleckMaker.ThrowMicroSparks(source, map);
            FleckMaker.ThrowMicroSparks(source, map);
            if (Rand.Chance(0.55f))
            {
                FleckMaker.ThrowMicroSparks(source, map);
            }
        }

        private static void SpawnRailLine(Vector3 source, Vector3 target, Map map)
        {
            Vector3 delta = target - source;
            float distance = delta.MagnitudeHorizontal();
            if (distance < 0.18f)
            {
                return;
            }

            SpawnBeamThing(RailHaloDef, source, target, map, 0.96f, 8, "Things/VFX/CrownspikeRail/ABY_CrownspikeRail_BeamHalo", true);
            SpawnBeamThing(RailBeamDef, source, target, map, 0.48f, 7, "Things/VFX/CrownspikeRail/ABY_CrownspikeRail_BeamGlow", true);
            SpawnBeamThing(RailCoreDef, source, target, map, 0.16f, 5, "Things/VFX/CrownspikeRail/ABY_CrownspikeRail_BeamCore", false);

            int sparkleSteps = Mathf.Clamp(Mathf.CeilToInt(distance * 0.42f), 3, 13);
            for (int i = 1; i <= sparkleSteps; i++)
            {
                float t = i / (float)(sparkleSteps + 1);
                Vector3 point = Vector3.Lerp(source, target, t);
                point += new Vector3(Rand.Range(-0.045f, 0.045f), 0f, Rand.Range(-0.045f, 0.045f));
                point.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                if (i % 2 == 0)
                {
                    FleckMaker.ThrowLightningGlow(point, map, 0.50f);
                }
            }
        }

        private static void SpawnImpact(Vector3 target, Map map, Thing hitThing, bool blockedByShield)
        {
            bool denseTarget = IsDenseTarget(hitThing);
            ThingDef impactDef = blockedByShield ? ShieldMoteDef : denseTarget ? DenseImpactMoteDef : ImpactMoteDef;
            float baseScale = blockedByShield ? 1.48f : denseTarget ? 1.74f : 1.34f;

            if (impactDef != null)
            {
                MakeStaticMote(target, map, impactDef, baseScale);
                MakeStaticMote(target + new Vector3(Rand.Range(-0.10f, 0.10f), 0f, Rand.Range(-0.10f, 0.10f)), map, impactDef, baseScale * 0.55f);
            }

            FleckMaker.ThrowLightningGlow(target, map, blockedByShield ? 2.40f : denseTarget ? 2.85f : 2.05f);
            FleckMaker.ThrowMicroSparks(target, map);
            FleckMaker.ThrowMicroSparks(target, map);
            if (blockedByShield || denseTarget)
            {
                FleckMaker.ThrowMicroSparks(target, map);
                FleckMaker.ThrowMicroSparks(target, map);
                if (Rand.Chance(0.70f))
                {
                    FleckMaker.ThrowMicroSparks(target, map);
                }
            }
        }

        private static void SpawnBeamThing(ThingDef def, Vector3 source, Vector3 target, Map map, float width, int ticks, string texturePath, bool pulse)
        {
            if (def == null || map == null)
            {
                return;
            }

            Mote_CrownspikeRailBeam beam = ThingMaker.MakeThing(def) as Mote_CrownspikeRailBeam;
            if (beam == null)
            {
                return;
            }

            beam.start = source;
            beam.end = target;
            beam.width = width;
            beam.ticksLeft = ticks;
            beam.startingTicks = ticks;
            beam.texturePath = texturePath;
            beam.additivePulse = pulse;

            IntVec3 spawnCell = source.ToIntVec3();
            if (!spawnCell.InBounds(map))
            {
                spawnCell = target.ToIntVec3();
            }
            if (!spawnCell.InBounds(map))
            {
                return;
            }

            GenSpawn.Spawn(beam, spawnCell, map);
        }

        private static bool IsDenseTarget(Thing hitThing)
        {
            if (hitThing == null || hitThing.Destroyed)
            {
                return false;
            }

            Pawn pawn = hitThing as Pawn;
            if (pawn != null)
            {
                return pawn.RaceProps != null && pawn.RaceProps.IsMechanoid;
            }

            return hitThing.def != null && hitThing.def.category == ThingCategory.Building;
        }

        private static void MakeStaticMote(Vector3 position, Map map, ThingDef moteDef, float scale)
        {
            if (map == null || moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(position, map, moteDef, scale);
        }
    }
}
