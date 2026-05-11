using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ReactorSaintProjectileVfxUtility
    {
        public const string LanceHaloTexturePath = "Things/Projectile/ReactorSaint/ABY_ReactorSaint_LanceHalo";
        public const string LanceCoreTexturePath = "Things/Projectile/ReactorSaint/ABY_ReactorSaint_LanceCore";
        public const string LanceNeedleTexturePath = "Things/Projectile/ReactorSaint/ABY_ReactorSaint_LanceNeedle";
        public const string BarrageHaloTexturePath = "Things/Projectile/ReactorSaint/ABY_ReactorSaint_BarrageHalo";
        public const string BarrageCoreTexturePath = "Things/Projectile/ReactorSaint/ABY_ReactorSaint_BarrageCore";

        private const string LanceTrailHaloDefName = "ABY_Mote_ReactorSaintLanceTrailHalo";
        private const string LanceTrailCoreDefName = "ABY_Mote_ReactorSaintLanceTrailCore";
        private const string LanceAfterimageDefName = "ABY_Mote_ReactorSaintLanceAfterimage";
        private const string ImpactRingDefName = "ABY_Mote_ReactorSaintImpactRing";
        private const string BarrageWarningDefName = "ABY_Mote_ReactorSaintBarrageWarning";
        private const string BarrageScorchDefName = "ABY_Mote_ReactorSaintBarrageScorch";
        private const string BarrageShockDefName = "ABY_Mote_ReactorSaintBarrageShock";

        private const string LanceTrailHaloTexturePath = "Things/VFX/ReactorSaintProjectile/ABY_ReactorSaint_LanceTrailHalo";
        private const string LanceTrailCoreTexturePath = "Things/VFX/ReactorSaintProjectile/ABY_ReactorSaint_LanceTrailCore";
        private const string LanceAfterimageTexturePath = "Things/VFX/ReactorSaintProjectile/ABY_ReactorSaint_LanceAfterimage";

        private static ThingDef lanceTrailHaloDef;
        private static ThingDef lanceTrailCoreDef;
        private static ThingDef lanceAfterimageDef;
        private static ThingDef impactRingDef;
        private static ThingDef barrageWarningDef;
        private static ThingDef barrageScorchDef;
        private static ThingDef barrageShockDef;

        private static ThingDef LanceTrailHaloDef => lanceTrailHaloDef ?? (lanceTrailHaloDef = DefDatabase<ThingDef>.GetNamedSilentFail(LanceTrailHaloDefName));
        private static ThingDef LanceTrailCoreDef => lanceTrailCoreDef ?? (lanceTrailCoreDef = DefDatabase<ThingDef>.GetNamedSilentFail(LanceTrailCoreDefName));
        private static ThingDef LanceAfterimageDef => lanceAfterimageDef ?? (lanceAfterimageDef = DefDatabase<ThingDef>.GetNamedSilentFail(LanceAfterimageDefName));
        private static ThingDef ImpactRingDef => impactRingDef ?? (impactRingDef = DefDatabase<ThingDef>.GetNamedSilentFail(ImpactRingDefName));
        private static ThingDef BarrageWarningDef => barrageWarningDef ?? (barrageWarningDef = DefDatabase<ThingDef>.GetNamedSilentFail(BarrageWarningDefName));
        private static ThingDef BarrageScorchDef => barrageScorchDef ?? (barrageScorchDef = DefDatabase<ThingDef>.GetNamedSilentFail(BarrageScorchDefName));
        private static ThingDef BarrageShockDef => barrageShockDef ?? (barrageShockDef = DefDatabase<ThingDef>.GetNamedSilentFail(BarrageShockDefName));

        public static int ResolvePhase(Thing launcher)
        {
            Pawn pawn = launcher as Pawn;
            CompABY_ReactorSaintPhaseController phaseController = pawn?.TryGetComp<CompABY_ReactorSaintPhaseController>();
            if (phaseController == null)
            {
                return 1;
            }

            return Mathf.Clamp(phaseController.CurrentPhase, 1, 3);
        }

        public static float ResolvePhaseFactor(Thing launcher)
        {
            int phase = ResolvePhase(launcher);
            if (phase >= 3)
            {
                return 1.34f;
            }
            if (phase == 2)
            {
                return 1.16f;
            }
            return 1.0f;
        }

        public static void SpawnLanceTrail(Vector3 from, Vector3 to, Map map, int ticksAlive, float phaseFactor)
        {
            if (map == null)
            {
                return;
            }

            float distance = (to - from).MagnitudeHorizontal();
            if (distance <= 0.08f)
            {
                return;
            }

            int life = Mathf.Clamp(Mathf.RoundToInt(7f * phaseFactor), 5, 11);
            SpawnBeam(LanceAfterimageDef, from, to, map, 0.34f * phaseFactor, life + 9, LanceAfterimageTexturePath, false);
            SpawnBeam(LanceTrailHaloDef, from, to, map, 0.64f * phaseFactor, life, LanceTrailHaloTexturePath, true);
            SpawnBeam(LanceTrailCoreDef, from, to, map, 0.18f * phaseFactor, Mathf.Max(3, life - 2), LanceTrailCoreTexturePath, false);

            int sparks = Mathf.Clamp(Mathf.RoundToInt(distance * 0.45f), 1, 4);
            for (int i = 0; i < sparks; i++)
            {
                if ((ticksAlive + i) % 2 != 0 && !Rand.Chance(0.38f))
                {
                    continue;
                }

                float t = (i + 1f) / (sparks + 1f);
                Vector3 point = Vector3.Lerp(from, to, t);
                point += new Vector3(Rand.Range(-0.055f, 0.055f), 0f, Rand.Range(-0.055f, 0.055f));
                FleckMaker.ThrowLightningGlow(point, map, Rand.Range(0.26f, 0.46f) * phaseFactor);
            }
        }

        public static void SpawnBarrageTrail(Vector3 from, Vector3 to, Map map, int ticksAlive, float phaseFactor)
        {
            if (map == null)
            {
                return;
            }

            Vector3 mid = Vector3.Lerp(from, to, 0.55f);
            FleckMaker.ThrowLightningGlow(mid, map, 0.40f * phaseFactor);
            if (ticksAlive % 6 == 0 || Rand.Chance(0.18f * phaseFactor))
            {
                FleckMaker.ThrowMicroSparks(mid, map);
            }

            if (ticksAlive % 5 == 0)
            {
                SpawnBeam(LanceAfterimageDef, from, to, map, 0.18f * phaseFactor, 8, LanceAfterimageTexturePath, false);
            }
        }

        public static void SpawnBarrageWarning(IntVec3 cell, Map map, float phaseFactor)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            Vector3 pos = cell.ToVector3Shifted();
            MakeStaticMote(pos, map, BarrageWarningDef, 1.05f * phaseFactor);
            FleckMaker.ThrowLightningGlow(pos, map, 0.80f * phaseFactor);
        }

        public static void SpawnLanceImpact(Vector3 position, IntVec3 cell, Map map, Thing hitThing, bool blockedByShield, float phaseFactor)
        {
            if (map == null)
            {
                return;
            }

            MakeStaticMote(position, map, ImpactRingDef, (blockedByShield ? 1.32f : 1.62f) * phaseFactor);
            FleckMaker.ThrowLightningGlow(position, map, (blockedByShield ? 2.10f : 2.85f) * phaseFactor);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);

            if (phaseFactor > 1.1f || IsDenseTarget(hitThing))
            {
                FleckMaker.ThrowMicroSparks(position, map);
                FleckMaker.ThrowFireGlow(position, map, 0.42f * phaseFactor);
            }

            SpawnShortRadialArcs(position, map, 4 + Mathf.RoundToInt(phaseFactor * 2f), 2.1f * phaseFactor);
            if (cell.IsValid && cell.InBounds(map))
            {
                MakeStaticMote(cell.ToVector3Shifted(), map, BarrageScorchDef, 0.72f * phaseFactor);
            }
        }

        public static void SpawnBarrageImpact(Vector3 position, IntVec3 cell, Map map, float phaseFactor)
        {
            if (map == null)
            {
                return;
            }

            MakeStaticMote(position, map, BarrageShockDef, 1.45f * phaseFactor);
            MakeStaticMote(position, map, BarrageScorchDef, 1.10f * phaseFactor);
            FleckMaker.ThrowLightningGlow(position, map, 3.05f * phaseFactor);
            FleckMaker.ThrowFireGlow(position, map, 0.85f * phaseFactor);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            SpawnShortRadialArcs(position, map, 5 + Mathf.RoundToInt(phaseFactor * 2f), 2.8f * phaseFactor);
        }

        private static void SpawnShortRadialArcs(Vector3 center, Map map, int count, float radius)
        {
            if (map == null || count <= 0)
            {
                return;
            }

            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            for (int i = 0; i < count; i++)
            {
                float angle = Rand.Range(0f, Mathf.PI * 2f);
                float length = Rand.Range(radius * 0.45f, radius);
                Vector3 target = center + new Vector3(Mathf.Cos(angle) * length, 0f, Mathf.Sin(angle) * length);
                target.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                SpawnBeam(LanceTrailHaloDef, center, target, map, Rand.Range(0.10f, 0.20f), Rand.Range(4, 8), LanceTrailHaloTexturePath, true);
                if (Rand.Chance(0.55f))
                {
                    SpawnBeam(LanceTrailCoreDef, center, target, map, Rand.Range(0.035f, 0.070f), Rand.Range(3, 6), LanceTrailCoreTexturePath, false);
                }
            }
        }

        private static void SpawnBeam(ThingDef def, Vector3 source, Vector3 target, Map map, float width, int ticks, string texturePath, bool pulse)
        {
            if (def == null || map == null || ticks <= 0)
            {
                return;
            }

            Mote_CrownspikeRailBeam beam = ThingMaker.MakeThing(def) as Mote_CrownspikeRailBeam;
            if (beam == null)
            {
                return;
            }

            source.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            target.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            beam.start = source;
            beam.end = target;
            beam.width = Mathf.Max(0.01f, width);
            beam.ticksLeft = ticks;
            beam.startingTicks = ticks;
            beam.texturePath = texturePath;
            beam.additivePulse = pulse;

            IntVec3 spawnCell = ((source + target) * 0.5f).ToIntVec3();
            if (!spawnCell.InBounds(map))
            {
                spawnCell = source.ToIntVec3();
            }
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
            if (moteDef == null || map == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(position, map, moteDef, scale);
        }
    }
}
