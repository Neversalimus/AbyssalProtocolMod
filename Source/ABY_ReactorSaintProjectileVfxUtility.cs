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

        private const int MaxVfxBudgetPerTick = 12;
        private static int budgetTick = -1;
        private static int budgetMapId = -1;
        private static int budgetUsed;

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
            if (distance <= 0.08f || !TryConsumeVfxBudget(map, 4))
            {
                return;
            }

            bool reduced = AbyssalProtocolMod.Settings.reducedMotion;
            int life = Mathf.Clamp(Mathf.RoundToInt(5f * phaseFactor), 4, 8);
            SpawnBeam(LanceTrailHaloDef, from, to, map, 0.42f * phaseFactor, life, LanceTrailHaloTexturePath, true);

            if (!reduced && (phaseFactor > 1.12f || ticksAlive % 6 == 0) && TryConsumeVfxBudget(map, 2))
            {
                SpawnBeam(LanceTrailCoreDef, from, to, map, 0.11f * phaseFactor, Mathf.Max(3, life - 2), LanceTrailCoreTexturePath, false);
            }

            if (!reduced && ticksAlive % 9 == 0 && TryConsumeVfxBudget(map, 2))
            {
                SpawnBeam(LanceAfterimageDef, from, to, map, 0.22f * phaseFactor, life + 4, LanceAfterimageTexturePath, false);
            }

            if (!reduced && TryConsumeVfxBudget(map, 1))
            {
                Vector3 point = Vector3.Lerp(from, to, 0.65f);
                point += new Vector3(Rand.Range(-0.045f, 0.045f), 0f, Rand.Range(-0.045f, 0.045f));
                FleckMaker.ThrowLightningGlow(point, map, Rand.Range(0.20f, 0.34f) * phaseFactor);
            }
        }

        public static void SpawnBarrageTrail(Vector3 from, Vector3 to, Map map, int ticksAlive, float phaseFactor)
        {
            if (map == null || !TryConsumeVfxBudget(map, 2))
            {
                return;
            }

            Vector3 mid = Vector3.Lerp(from, to, 0.55f);
            FleckMaker.ThrowLightningGlow(mid, map, 0.30f * phaseFactor);
            if (!AbyssalProtocolMod.Settings.reducedMotion && ticksAlive % 8 == 0 && TryConsumeVfxBudget(map, 2))
            {
                SpawnBeam(LanceAfterimageDef, from, to, map, 0.13f * phaseFactor, 5, LanceAfterimageTexturePath, false);
            }
        }

        public static void SpawnBarrageWarning(IntVec3 cell, Map map, float phaseFactor)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            if (!TryConsumeVfxBudget(map, 3))
            {
                return;
            }

            Vector3 pos = cell.ToVector3Shifted();
            MakeStaticMote(pos, map, BarrageWarningDef, 0.92f * phaseFactor);
            FleckMaker.ThrowLightningGlow(pos, map, 0.56f * phaseFactor);
        }

        public static void SpawnLanceImpact(Vector3 position, IntVec3 cell, Map map, Thing hitThing, bool blockedByShield, float phaseFactor)
        {
            if (map == null)
            {
                return;
            }

            if (IsPawnTarget(hitThing))
            {
                SpawnLightPawnLanceImpact(position, map, blockedByShield, phaseFactor);
                return;
            }

            if (TryConsumeVfxBudget(map, 4))
            {
                MakeStaticMote(position, map, ImpactRingDef, (blockedByShield ? 1.05f : 1.24f) * phaseFactor);
                FleckMaker.ThrowLightningGlow(position, map, (blockedByShield ? 1.25f : 1.72f) * phaseFactor);
                if (!AbyssalProtocolMod.Settings.reducedMotion && TryConsumeVfxBudget(map, 1))
                {
                    FleckMaker.ThrowMicroSparks(position, map);
                }
            }

            if (!AbyssalProtocolMod.Settings.reducedMotion && (phaseFactor > 1.1f || IsDenseTarget(hitThing)) && TryConsumeVfxBudget(map, 1))
            {
                FleckMaker.ThrowFireGlow(position, map, 0.28f * phaseFactor);
            }

            if (IsDenseTarget(hitThing))
            {
                SpawnShortRadialArcs(position, map, 1 + Mathf.RoundToInt(phaseFactor * 0.5f), 1.45f * phaseFactor);
            }

            if (cell.IsValid && cell.InBounds(map) && TryConsumeVfxBudget(map, 1))
            {
                MakeStaticMote(cell.ToVector3Shifted(), map, BarrageScorchDef, 0.58f * phaseFactor);
            }
        }

        public static void SpawnBarrageImpact(Vector3 position, IntVec3 cell, Map map, Thing hitThing, float phaseFactor)
        {
            if (map == null)
            {
                return;
            }

            if (IsPawnTarget(hitThing))
            {
                SpawnLightPawnBarrageImpact(position, map, phaseFactor);
                return;
            }

            if (TryConsumeVfxBudget(map, 5))
            {
                MakeStaticMote(position, map, BarrageShockDef, 1.06f * phaseFactor);
                MakeStaticMote(position, map, BarrageScorchDef, 0.80f * phaseFactor);
                FleckMaker.ThrowLightningGlow(position, map, 1.80f * phaseFactor);
                if (!AbyssalProtocolMod.Settings.reducedMotion)
                {
                    if (TryConsumeVfxBudget(map, 1))
                    {
                        FleckMaker.ThrowFireGlow(position, map, 0.46f * phaseFactor);
                    }
                    if (TryConsumeVfxBudget(map, 1))
                    {
                        FleckMaker.ThrowMicroSparks(position, map);
                    }
                }
            }

            SpawnShortRadialArcs(position, map, 1 + Mathf.RoundToInt(phaseFactor * 0.5f), 1.70f * phaseFactor);
        }

        private static void SpawnLightPawnLanceImpact(Vector3 position, Map map, bool blockedByShield, float phaseFactor)
        {
            if (map == null || !TryConsumeVfxBudget(map, 2))
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, (blockedByShield ? 0.82f : 1.05f) * phaseFactor);
            if (!AbyssalProtocolMod.Settings.reducedMotion && TryConsumeVfxBudget(map, 1))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        private static void SpawnLightPawnBarrageImpact(Vector3 position, Map map, float phaseFactor)
        {
            if (map == null || !TryConsumeVfxBudget(map, 2))
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(position, map, 1.15f * phaseFactor);
            if (!AbyssalProtocolMod.Settings.reducedMotion && TryConsumeVfxBudget(map, 1))
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        private static void SpawnShortRadialArcs(Vector3 center, Map map, int count, float radius)
        {
            if (map == null || count <= 0)
            {
                return;
            }

            if (AbyssalProtocolMod.Settings.reducedMotion)
            {
                return;
            }

            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            int resolvedCount = Mathf.Min(count, 3);
            for (int i = 0; i < resolvedCount; i++)
            {
                if (!TryConsumeVfxBudget(map, 2))
                {
                    return;
                }

                float angle = Rand.Range(0f, Mathf.PI * 2f);
                float length = Rand.Range(radius * 0.45f, radius);
                Vector3 target = center + new Vector3(Mathf.Cos(angle) * length, 0f, Mathf.Sin(angle) * length);
                target.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                SpawnBeam(LanceTrailHaloDef, center, target, map, Rand.Range(0.08f, 0.14f), Rand.Range(3, 5), LanceTrailHaloTexturePath, true);
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

        private static bool TryConsumeVfxBudget(Map map, int cost)
        {
            if (map == null || cost <= 0 || Find.TickManager == null)
            {
                return true;
            }

            int tick = Find.TickManager.TicksGame;
            int mapId = map.uniqueID;
            if (budgetTick != tick || budgetMapId != mapId)
            {
                budgetTick = tick;
                budgetMapId = mapId;
                budgetUsed = 0;
            }

            int maxBudget = AbyssalProtocolMod.Settings.reducedMotion ? 8 : MaxVfxBudgetPerTick;
            if (budgetUsed + cost > maxBudget)
            {
                return false;
            }

            budgetUsed += cost;
            return true;
        }

        private static bool IsPawnTarget(Thing hitThing)
        {
            return hitThing is Pawn;
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
