using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Projectile_OblivionChoirCore : Bullet
    {
        private const int TrailIntervalTicks = 1;
        private const int CorePulseIntervalTicks = 2;
        internal const int ArcIntervalTicks = 3;
        private const int ArcRetargetCooldownTicks = 16;
        private const int BranchBeamLifetimeTicks = 8;
        internal const int MaxArcTargetsPerPulse = 5;
        private const int MaxSweepSamples = 16;

        private const float TrailGlowSize = 0.38f;
        private const float TrailFireGlowSize = 0.16f;
        private const float CoreGlowBaseSize = 0.62f;
        private const float CoreFireGlowBaseSize = 0.20f;
        private const float ArcGlowSize = 0.68f;
        private const float ImpactGlowSize = 2.95f;
        private const float ArcRadius = 6.0f;
        private const float SweepSampleSpacing = 0.64f;
        internal const float ArcDamage = 3.75f;
        internal const float ArcArmorPenetration = 0.36f;
        private const float ResonanceSeverityGain = 0.18f;
        private const float ResonanceMaxSeverity = 1.00f;
        private const int ResonanceDisappearTicks = 420;
        internal const float ResonanceImpactRadius = 7.2f;
        internal const float ImpactExplosionRadius = 4.8f;
        internal const int ImpactExplosionDamage = 60;
        internal const float ImpactExplosionArmorPenetration = 1.22f;
        private const string ResonanceHediffDefName = "ABY_ChoirResonance";

        private const string BodyTexturePath = "Things/Projectile/ABY_OblivionChoirCore";
        private const string BlobHaloTexturePath = "Things/VFX/OblivionChoir/ABY_OblivionChoir_BlobHalo";
        private const string BlobCoreTexturePath = "Things/VFX/OblivionChoir/ABY_OblivionChoir_BlobCore";
        private const string BranchHaloThingDefName = "ABY_Mote_OblivionChoirBranchHalo";
        private const string BranchCoreThingDefName = "ABY_Mote_OblivionChoirBranchCore";
        private const string BranchHaloTexturePath = "Things/VFX/OblivionChoir/ABY_OblivionChoir_BranchHalo";
        private const string BranchCoreTexturePath = "Things/VFX/OblivionChoir/ABY_OblivionChoir_BranchCore";

        private readonly Dictionary<int, int> targetRetargetTicks = new Dictionary<int, int>();

        private int ticksAlive;
        private Vector3 lastExactPosition;
        private Vector3 lastDrawDirection = Vector3.forward;
        private bool lastPositionInitialized;
        private int lastBranchHits;

        private Material cachedBodyMaterial;
        private Material cachedBlobHaloMaterial;
        private Material cachedBlobCoreMaterial;

        protected override void Tick()
        {
            Vector3 previousPosition = ExactPosition;
            base.Tick();

            if (!Spawned || Map == null)
            {
                return;
            }

            ticksAlive++;

            if (!lastPositionInitialized)
            {
                lastExactPosition = previousPosition;
                lastPositionInitialized = true;
            }

            Vector3 currentPosition = ExactPosition;
            Vector3 movement = currentPosition - lastExactPosition;
            movement.y = 0f;
            if (movement.sqrMagnitude > 0.0001f)
            {
                lastDrawDirection = movement.normalized;
            }

            if (ticksAlive % TrailIntervalTicks == 0 && ABY_VfxBudget.TrySpend(Map, ABY_VfxBudgetCategory.CombatLight, 1))
            {
                SpawnTrail(lastExactPosition, currentPosition, Map, ticksAlive, lastBranchHits);
            }

            if (ticksAlive % CorePulseIntervalTicks == 0 && ABY_VfxBudget.TrySpend(Map, ABY_VfxBudgetCategory.CombatLight, 1))
            {
                SpawnCorePulse(currentPosition, Map, ticksAlive, lastBranchHits);
            }

            if (ticksAlive % ArcIntervalTicks == 0)
            {
                lastBranchHits = PulseTargetsAlongSweptPath(lastExactPosition, currentPosition);
            }

            lastExactPosition = currentPosition;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 drawPos = drawLoc;
            drawPos.y = Altitudes.AltitudeFor(AltitudeLayer.Projectile);

            Vector3 direction = lastDrawDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }
            direction.Normalize();

            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float branchPulse = Mathf.Clamp01(lastBranchHits / 4f);
            float pulse = 0.94f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.31f)) * (0.18f + branchPulse * 0.10f);
            float hotPulse = 0.84f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.57f + 1.2f)) * (0.34f + branchPulse * 0.12f);
            float wobble = Mathf.Sin(ticksAlive * 0.23f) * (0.035f + branchPulse * 0.014f);
            float ringPulse = 0.82f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.19f + 2.4f)) * 0.20f;

            DrawPlane(drawPos, angle + wobble * 95f, new Vector3(1.56f * pulse, 1f, 2.94f * pulse), BlobHaloMaterial);
            DrawPlane(drawPos + direction * 0.03f, angle, new Vector3(1.08f * (0.96f + hotPulse * 0.06f), 1f, 2.54f * (0.96f + hotPulse * 0.04f)), BodyMaterial);
            DrawPlane(drawPos + direction * 0.17f, angle - wobble * 120f, new Vector3(0.82f * hotPulse, 1f, 1.32f * hotPulse), BlobCoreMaterial);
            DrawPlane(drawPos - direction * 0.10f, angle + 90f + ticksAlive * 3.4f, new Vector3(0.56f * ringPulse, 1f, 1.84f * ringPulse), BlobHaloMaterial);
        }

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map impactMap = Map;
            IntVec3 impactCell = Position;
            Vector3 impactPosition = ExactPosition;
            Thing instigator = Launcher;

            if (!ABY_ProjectileImpactSafetyUtility.TryRunBaseImpact(this, hitThing, "Projectile_OblivionChoirCore", () => base.Impact(hitThing, blockedByShield)))
            {
                return;
            }

            if (impactMap == null || !impactCell.IsValid)
            {
                return;
            }

            SpawnImpactEffects(impactPosition, impactMap);

            if (blockedByShield)
            {
                return;
            }

            ABY_SoundUtility.PlayAt("ABY_UltraPlasmaTail", impactCell, impactMap);
            DetonateResonanceAround(impactCell, impactPosition, impactMap, instigator);
            MapComponent_ABY_OblivionChoirScar.AddScar(impactMap, impactCell, instigator);
            ABY_ProjectileImpactSafetyUtility.TryRunPostImpactAction(this, "Projectile_OblivionChoirCore", "explosion", () =>
            {
                GenExplosion.DoExplosion(impactCell, impactMap, ImpactExplosionRadius, DamageDefOf.Burn, instigator, ImpactExplosionDamage, ImpactExplosionArmorPenetration);
            });
        }

        private int PulseTargetsAlongSweptPath(Vector3 from, Vector3 to)
        {
            if (Map == null)
            {
                return 0;
            }

            return ABY_BranchingProjectileUtility.PulseSweptBranches(
                Map,
                Launcher,
                from,
                to,
                ticksAlive,
                targetRetargetTicks,
                new ABY_BranchingProjectileConfig
                {
                    radius = ArcRadius,
                    sampleSpacing = SweepSampleSpacing,
                    maxSweepSamples = MaxSweepSamples,
                    maxTargetsPerPulse = MaxArcTargetsPerPulse,
                    retargetCooldownTicks = ArcRetargetCooldownTicks,
                    branchLifetimeTicks = BranchBeamLifetimeTicks,
                    branchHaloThingDefName = BranchHaloThingDefName,
                    branchCoreThingDefName = BranchCoreThingDefName,
                    branchHaloTexturePath = BranchHaloTexturePath,
                    branchCoreTexturePath = BranchCoreTexturePath,
                    haloWidth = 0.32f,
                    coreWidth = 0.105f,
                    shouldAffectThing = ShouldAffectThing,
                    scoreOffset = ScoreBranchTarget,
                    onBranchHit = ApplyArcDamage
                });
        }

        private bool ShouldAffectThing(Thing thing)
        {
            if (thing == null || thing.Destroyed || thing == Launcher || !thing.Spawned)
            {
                return false;
            }

            if (thing.def == null || thing.def.category == ThingCategory.Mote || thing.def.category == ThingCategory.Projectile || thing is Fire)
            {
                return false;
            }

            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                if (pawn.Dead)
                {
                    return false;
                }

                return Launcher == null || ABY_FactionHostilityUtility.SafeHostileTo(Launcher, pawn);
            }

            Building building = thing as Building;
            if (building != null)
            {
                if (thing is Blueprint || thing is Frame)
                {
                    return false;
                }

                if (building.def.mineable || (building.def.building != null && building.def.building.isNaturalRock))
                {
                    return false;
                }

                if (Launcher != null && !ABY_FactionHostilityUtility.SafeHostileTo(Launcher, building))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private float ScoreBranchTarget(Thing thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                return 3.25f;
            }

            float score = -4.0f;
            if (pawn.Downed)
            {
                score += 4.5f;
            }
            if (pawn.equipment?.Primary != null && pawn.equipment.Primary.def.IsRangedWeapon)
            {
                score -= 1.1f;
            }
            if (GetResonanceSeverity(pawn) > 0.01f)
            {
                score -= 0.85f;
            }
            if (pawn.health != null)
            {
                float healthPct = pawn.health.summaryHealth.SummaryHealthPercent;
                if (healthPct < 0.22f)
                {
                    score += 1.65f;
                }
                else if (healthPct > 0.70f)
                {
                    score -= 0.35f;
                }
            }

            return score;
        }

        private void ApplyArcDamage(Thing thing, Vector3 branchSource)
        {
            if (thing == null)
            {
                return;
            }

            float resonanceBefore = 0f;
            Pawn pawn = thing as Pawn;
            if (pawn != null)
            {
                resonanceBefore = GetResonanceSeverity(pawn);
            }

            Map map = thing.MapHeld;
            if (map != null)
            {
                Vector3 drawPos = thing.TrueCenter();
                SpawnBranchBeam(map, branchSource, drawPos, thing.thingIDNumber, resonanceBefore);
                FleckMaker.ThrowLightningGlow(drawPos, map, ArcGlowSize * (1f + resonanceBefore * 0.42f));
                FleckMaker.ThrowMicroSparks(drawPos, map);
            }

            float damageAmount = ArcDamage;
            if (pawn != null)
            {
                ABY_ProjectileProcUtility.ApplyOrRefreshHediff(
                    pawn,
                    ResonanceHediffDefName,
                    ResonanceSeverityGain,
                    ResonanceSeverityGain,
                    ResonanceMaxSeverity,
                    ResonanceDisappearTicks);

                damageAmount += Mathf.Clamp(resonanceBefore, 0f, 1f) * 4.5f;
                if (resonanceBefore >= 0.82f)
                {
                    damageAmount += 4.0f;
                    if (map != null)
                    {
                        FleckMaker.ThrowLightningGlow(thing.TrueCenter(), map, ArcGlowSize * 1.35f);
                    }
                }
            }

            DamageInfo damageInfo = new DamageInfo(
                DamageDefOf.Burn,
                damageAmount,
                ArcArmorPenetration,
                -1f,
                Launcher,
                null,
                def,
                DamageInfo.SourceCategory.ThingOrUnknown);

            ABY_ProjectileImpactSafetyUtility.TryApplyDamage(this, thing, damageInfo, "Projectile_OblivionChoirCore");
        }

        private void DetonateResonanceAround(IntVec3 impactCell, Vector3 impactPosition, Map map, Thing instigator)
        {
            if (map == null)
            {
                return;
            }

            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.SpawnedLivingPawnsFor(map);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
                {
                    continue;
                }

                if (instigator != null && !ABY_FactionHostilityUtility.SafeHostileTo(instigator, pawn))
                {
                    continue;
                }

                float resonance = GetResonanceSeverity(pawn);
                if (resonance <= 0.01f || pawn.Position.DistanceTo(impactCell) > ResonanceImpactRadius)
                {
                    continue;
                }

                float detonationDamage = 5.0f + resonance * 13.0f;
                DamageInfo damageInfo = new DamageInfo(
                    DamageDefOf.Burn,
                    detonationDamage,
                    0.54f + resonance * 0.32f,
                    -1f,
                    instigator,
                    null,
                    def,
                    DamageInfo.SourceCategory.ThingOrUnknown);
                ABY_ProjectileImpactSafetyUtility.TryApplyDamage(this, pawn, damageInfo, "Projectile_OblivionChoirCore");
                SpawnBranchBeam(map, impactPosition, pawn.TrueCenter(), pawn.thingIDNumber ^ 0x51F1, resonance);
                FleckMaker.ThrowLightningGlow(pawn.TrueCenter(), map, 0.92f + resonance * 0.85f);

                Hediff resonanceHediff = GetResonanceHediff(pawn);
                if (resonanceHediff != null)
                {
                    resonanceHediff.Severity = Mathf.Max(0.10f, resonanceHediff.Severity * 0.38f);
                }
            }
        }

        private void SpawnBranchBeam(Map map, Vector3 from, Vector3 to, int targetId, float resonance)
        {
            float widthBonus = Mathf.Clamp01(resonance) * 0.13f;
            ABY_BranchingProjectileUtility.SpawnCurvedBranchBeam(
                map,
                from,
                to,
                targetId * 397 ^ ticksAlive * 101,
                ticksAlive,
                BranchBeamLifetimeTicks + Mathf.RoundToInt(Mathf.Clamp01(resonance) * 3f),
                BranchHaloThingDefName,
                BranchCoreThingDefName,
                BranchHaloTexturePath,
                BranchCoreTexturePath,
                0.32f + widthBonus,
                0.105f + widthBonus * 0.34f);
        }

        private static void SpawnTrail(Vector3 from, Vector3 to, Map map, int ticksAlive, int lastBranchHits)
        {
            if (map == null)
            {
                return;
            }

            int trailPoints = lastBranchHits > 0 ? 4 : 3;
            for (int i = 1; i <= trailPoints; i++)
            {
                float t = i / (trailPoints + 1f);
                Vector3 point = Vector3.Lerp(from, to, t);
                float pulse = 0.90f + Mathf.Abs(Mathf.Sin((ticksAlive + i * 3) * 0.38f)) * (0.35f + lastBranchHits * 0.035f);
                FleckMaker.ThrowLightningGlow(point, map, TrailGlowSize * pulse);
                if (((ticksAlive + i) & 1) == 0)
                {
                    FleckMaker.ThrowFireGlow(point, map, TrailFireGlowSize * pulse);
                }
                if (i >= 2 || Rand.Chance(0.40f + lastBranchHits * 0.04f))
                {
                    FleckMaker.ThrowMicroSparks(point, map);
                }
            }
        }

        private static void SpawnCorePulse(Vector3 position, Map map, int ticksAlive, int lastBranchHits)
        {
            if (map == null)
            {
                return;
            }

            float pulse = 0.92f + Mathf.Abs(Mathf.Sin(ticksAlive * 0.42f)) * (0.40f + lastBranchHits * 0.04f);
            FleckMaker.ThrowLightningGlow(position, map, CoreGlowBaseSize * pulse);
            FleckMaker.ThrowFireGlow(position, map, CoreFireGlowBaseSize * pulse);
            if ((ticksAlive % 4) == 0)
            {
                FleckMaker.ThrowMicroSparks(position, map);
            }
        }

        private static void SpawnImpactEffects(Vector3 position, Map map)
        {
            FleckMaker.ThrowLightningGlow(position, map, ImpactGlowSize);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowMicroSparks(position, map);
            FleckMaker.ThrowFireGlow(position, map, 0.72f);
        }

        private static float GetResonanceSeverity(Pawn pawn)
        {
            Hediff hediff = GetResonanceHediff(pawn);
            return hediff != null ? hediff.Severity : 0f;
        }

        private static Hediff GetResonanceHediff(Pawn pawn)
        {
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(ResonanceHediffDefName);
            if (pawn?.health?.hediffSet == null || hediffDef == null)
            {
                return null;
            }

            return pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
        }

        private void DrawPlane(Vector3 center, float angle, Vector3 scale, Material material)
        {
            if (material == null)
            {
                return;
            }

            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private Material BodyMaterial
        {
            get
            {
                if (cachedBodyMaterial == null)
                {
                    cachedBodyMaterial = MaterialPool.MatFrom(BodyTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedBodyMaterial;
            }
        }

        private Material BlobHaloMaterial
        {
            get
            {
                if (cachedBlobHaloMaterial == null)
                {
                    cachedBlobHaloMaterial = MaterialPool.MatFrom(BlobHaloTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedBlobHaloMaterial;
            }
        }

        private Material BlobCoreMaterial
        {
            get
            {
                if (cachedBlobCoreMaterial == null)
                {
                    cachedBlobCoreMaterial = MaterialPool.MatFrom(BlobCoreTexturePath, ShaderDatabase.MoteGlow);
                }
                return cachedBlobCoreMaterial;
            }
        }
    }
}
