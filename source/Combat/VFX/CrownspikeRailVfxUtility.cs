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
        private const string RailAfterimageThingDefName = "ABY_Mote_CrownspikeRailAfterimage";
        private const string MuzzleMoteDefName = "ABY_Mote_CrownspikeRailMuzzle";
        private const string ChargeMoteDefName = "ABY_Mote_CrownspikeRailCharge";
        private const string ImpactMoteDefName = "ABY_Mote_CrownspikeRailImpact";
        private const string ShieldMoteDefName = "ABY_Mote_CrownspikeRailShieldImpact";
        private const string DenseImpactMoteDefName = "ABY_Mote_CrownspikeRailDenseImpact";
        private const string ExecutionMoteDefName = "ABY_Mote_CrownspikeRailExecution";

        private static ThingDef railBeamDef;
        private static ThingDef railHaloDef;
        private static ThingDef railCoreDef;
        private static ThingDef railAfterimageDef;
        private static ThingDef muzzleMoteDef;
        private static ThingDef chargeMoteDef;
        private static ThingDef impactMoteDef;
        private static ThingDef shieldMoteDef;
        private static ThingDef denseImpactMoteDef;
        private static ThingDef executionMoteDef;

        private static ThingDef RailBeamDef => railBeamDef ?? (railBeamDef = DefDatabase<ThingDef>.GetNamedSilentFail(RailBeamThingDefName));
        private static ThingDef RailHaloDef => railHaloDef ?? (railHaloDef = DefDatabase<ThingDef>.GetNamedSilentFail(RailHaloThingDefName));
        private static ThingDef RailCoreDef => railCoreDef ?? (railCoreDef = DefDatabase<ThingDef>.GetNamedSilentFail(RailCoreThingDefName));
        private static ThingDef RailAfterimageDef => railAfterimageDef ?? (railAfterimageDef = DefDatabase<ThingDef>.GetNamedSilentFail(RailAfterimageThingDefName));
        private static ThingDef MuzzleMoteDef => muzzleMoteDef ?? (muzzleMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(MuzzleMoteDefName));
        private static ThingDef ChargeMoteDef => chargeMoteDef ?? (chargeMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ChargeMoteDefName));
        private static ThingDef ImpactMoteDef => impactMoteDef ?? (impactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactMoteDefName));
        private static ThingDef ShieldMoteDef => shieldMoteDef ?? (shieldMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ShieldMoteDefName));
        private static ThingDef DenseImpactMoteDef => denseImpactMoteDef ?? (denseImpactMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(DenseImpactMoteDefName));
        private static ThingDef ExecutionMoteDef => executionMoteDef ?? (executionMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(ExecutionMoteDefName));

        public static void SpawnRailDischarge(Vector3 source, Vector3 target, Map map, Thing hitThing, bool blockedByShield, bool executionPulse)
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

            if (executionPulse)
            {
                SpawnExecutionFlare(target, map);
            }
        }

        public static void SpawnChargeAt(Vector3 source, Map map)
        {
            if (map == null)
            {
                return;
            }

            source.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            ThingDef chargeDef = ChargeMoteDef;
            if (chargeDef != null)
            {
                MakeStaticMote(source, map, chargeDef, 1.12f);
                MakeStaticMote(source + new Vector3(Rand.Range(-0.07f, 0.07f), 0f, Rand.Range(-0.07f, 0.07f)), map, chargeDef, 0.58f);
            }

            FleckMaker.ThrowLightningGlow(source, map, 1.15f);
            if (Rand.Chance(0.70f))
            {
                FleckMaker.ThrowMicroSparks(source, map);
            }
        }

        public static void SpawnShieldReaction(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            MakeStaticMote(position, map, ShieldMoteDef, 1.78f);
            FleckMaker.ThrowLightningGlow(position, map, 2.85f);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
        }

        public static void SpawnDenseResonance(Vector3 position, Map map, bool fromPierce)
        {
            if (map == null)
            {
                return;
            }

            MakeStaticMote(position, map, DenseImpactMoteDef, fromPierce ? 1.05f : 1.62f);
            FleckMaker.ThrowLightningGlow(position, map, fromPierce ? 1.55f : 2.55f);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            if (!fromPierce)
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnPierceImpact(Vector3 position, Map map, Thing hitThing, int pierceIndex)
        {
            if (map == null)
            {
                return;
            }

            bool denseTarget = IsDenseTarget(hitThing);
            ThingDef impactDef = denseTarget ? DenseImpactMoteDef : ImpactMoteDef;
            float scale = denseTarget ? 1.18f : 0.92f;
            if (pierceIndex > 0)
            {
                scale *= 0.76f;
            }

            MakeStaticMote(position, map, impactDef, scale);
            FleckMaker.ThrowLightningGlow(position, map, denseTarget ? 1.75f : 1.08f);
            if (Rand.Chance(0.75f))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        public static void SpawnExecutionFlare(Vector3 position, Map map)
        {
            if (map == null)
            {
                return;
            }

            MakeStaticMote(position, map, ExecutionMoteDef, 1.96f);
            MakeStaticMote(position + new Vector3(Rand.Range(-0.08f, 0.08f), 0f, Rand.Range(-0.08f, 0.08f)), map, ImpactMoteDef, 1.18f);
            FleckMaker.ThrowLightningGlow(position, map, 3.10f);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
        }

        private static void SpawnMuzzleFlash(Vector3 source, Vector3 target, Map map)
        {
            ThingDef muzzleDef = MuzzleMoteDef;
            if (muzzleDef != null)
            {
                MakeStaticMote(source, map, muzzleDef, 1.78f);
                MakeStaticMote(Vector3.Lerp(source, target, 0.028f), map, muzzleDef, 1.02f);
                MakeStaticMote(Vector3.Lerp(source, target, 0.058f), map, muzzleDef, 0.56f);
            }

            FleckMaker.ThrowLightningGlow(source, map, 2.55f);
            FleckMaker.ThrowMicroSparks(source, map);
            FleckMaker.ThrowMicroSparks(source, map);
            if (Rand.Chance(0.72f))
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

            SpawnBeamThing(RailAfterimageDef, source, target, map, 0.30f, 18, "Things/VFX/CrownspikeRail/ABY_CrownspikeRail_Afterimage", false);
            SpawnBeamThing(RailHaloDef, source, target, map, 1.02f, 8, "Things/VFX/CrownspikeRail/ABY_CrownspikeRail_BeamHalo", true);
            SpawnBeamThing(RailBeamDef, source, target, map, 0.50f, 7, "Things/VFX/CrownspikeRail/ABY_CrownspikeRail_BeamGlow", true);
            SpawnBeamThing(RailCoreDef, source, target, map, 0.14f, 5, "Things/VFX/CrownspikeRail/ABY_CrownspikeRail_BeamCore", false);

            int sparkleSteps = Mathf.Clamp(Mathf.CeilToInt(distance * 0.32f), 2, 9);
            for (int i = 1; i <= sparkleSteps; i++)
            {
                float t = i / (float)(sparkleSteps + 1);
                Vector3 point = Vector3.Lerp(source, target, t);
                point += new Vector3(Rand.Range(-0.035f, 0.035f), 0f, Rand.Range(-0.035f, 0.035f));
                point.y = AltitudeLayer.MoteOverhead.AltitudeFor();

                if (i % 2 == 0 || Rand.Chance(0.35f))
                {
                    FleckMaker.ThrowLightningGlow(point, map, 0.42f);
                }
            }
        }

        private static void SpawnImpact(Vector3 target, Map map, Thing hitThing, bool blockedByShield)
        {
            bool denseTarget = IsDenseTarget(hitThing);
            ThingDef impactDef = blockedByShield ? ShieldMoteDef : denseTarget ? DenseImpactMoteDef : ImpactMoteDef;
            float baseScale = blockedByShield ? 1.58f : denseTarget ? 1.84f : 1.38f;

            if (impactDef != null)
            {
                MakeStaticMote(target, map, impactDef, baseScale);
                MakeStaticMote(target + new Vector3(Rand.Range(-0.10f, 0.10f), 0f, Rand.Range(-0.10f, 0.10f)), map, impactDef, baseScale * 0.50f);
            }

            FleckMaker.ThrowLightningGlow(target, map, blockedByShield ? 2.60f : denseTarget ? 3.05f : 2.15f);
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

        public static bool IsDenseTarget(Thing hitThing)
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
