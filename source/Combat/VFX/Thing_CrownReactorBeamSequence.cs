using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Transient four-rail beam presentation for the Crown Reactor Multilance.
    ///
    /// Runtime budget notes:
    /// - no map-wide scans;
    /// - no per-tick damage;
    /// - four single primary damage applications, one per rail discharge;
    /// - short line/retarget checks are tightly bounded;
    /// - cached/quantized beam materials through the shared Abyssal material cache.
    /// </summary>
    [StaticConstructorOnStartup]
    public class Thing_CrownReactorBeamSequence : Thing
    {
        private const string BeamTexturePath = "Things/Projectile/ABY_CrownReactorBeamSegment";
        private const string ChargeDotTexturePath = "Things/Projectile/ABY_CrownReactorChargeDot";
        private const int RailCount = 4;

        // Intentionally fast post-warmup presentation: about 1.6x faster than the first pass.
        private const int RailChargeStepTicks = 6;
        private const int PreDischargeDelayTicks = 8;
        private const int BeamHoldTicks = 12;
        private const int BeamGapTicks = 3;
        private const int FadeTicks = 8;

        // Visual profile tuning: keep the VFX tightly bound to the actual four barrel lanes.
        private const float MinimumWeaponLength = 1.95f;
        private const float MinimumWeaponHeight = 0.62f;
        private const float MuzzleForwardRatio = 0.66f;
        private const float BarrelStartForwardRatio = 0.31f;
        private const float MinMuzzleForwardOffset = 1.16f;
        private const float MaxMuzzleForwardOffset = 1.34f;
        private const float MinBarrelStartForwardOffset = 0.56f;
        private const float MaxBarrelStartForwardOffset = 0.72f;
        private const float OuterRailOffsetRatio = 0.23f;
        private const float InnerRailOffsetRatio = 0.075f;
        private const float MinOuterRailOffset = 0.12f;
        private const float MaxOuterRailOffset = 0.16f;
        private const float MinInnerRailOffset = 0.04f;
        private const float MaxInnerRailOffset = 0.065f;
        private const float MainBeamWidthRatio = 0.13f;
        private const float MinMainBeamWidth = 0.068f;
        private const float MaxMainBeamWidth = 0.09f;
        private const float ChargeDotSize = 0.105f;
        private const float CompletedChargeDotSize = 0.075f;

        // Four-Rail Verdict tuning. Values are deliberately modest because this is a high-tier weapon
        // that already fires four reliable damage pulses.
        private const float ShieldShearSystemMultiplier = 1.32f;
        private const float ShieldShearEmpDamage = 10f;
        private const float OverlineDamageMultiplier = 0.45f;
        private const float OverlineArmorMultiplier = 0.70f;
        private const int OverlineCells = 8;
        private const int OverlineMaxHits = 3;
        private const float CrownVerdictMultiplier = 1.42f;
        private const float CrownVerdictBossMultiplier = 1.18f;
        private const float RupturePulseDamageMultiplier = 0.35f;
        private const float RupturePulseArmorMultiplier = 0.55f;
        private const int RetargetRadius = 5;
        private const int RetargetCheckIntervalTicks = 4;

        private static readonly Color ChargeDotColor = new Color(0.82f, 1f, 1f, 0.82f);
        private static readonly Color FadeColor = new Color(1f, 1f, 1f, 0.72f);

        private static Material cachedBeamMaterial;
        private static Material cachedChargeDotMaterial;
        private static Material cachedBeamFadeMaterial;

        private Thing launcher;
        private Thing equipment;
        private Thing targetThing;
        private Thing retargetThing;
        private Thing lockedThing;
        private ThingDef payloadProjectileDef;
        private IntVec3 targetCell;
        private Vector3 muzzleBase;
        private Vector3 chargeBase;
        private Vector3 shotDirection;
        private Vector3 shotPerpendicular;
        private Vector3 fixedTarget;
        private int ageTicks;
        private int nextRetargetCheckTick;
        private bool rupturePulseApplied;
        private readonly bool[] damageApplied = new bool[RailCount];

        private int ChargeTicks => RailChargeStepTicks * RailCount;
        private int BeamStartTick => ChargeTicks + PreDischargeDelayTicks;
        private int BeamCycleTicks => BeamHoldTicks + BeamGapTicks;
        private int TotalLifetimeTicks => BeamStartTick + RailCount * BeamCycleTicks + FadeTicks;

        private static Material BeamMaterial
        {
            get
            {
                if (cachedBeamMaterial == null)
                {
                    cachedBeamMaterial = ABY_MaterialCacheUtility.MatFrom(BeamTexturePath, ShaderDatabase.MoteGlow, Color.white);
                }

                return cachedBeamMaterial;
            }
        }

        private static Material ChargeDotMaterial
        {
            get
            {
                if (cachedChargeDotMaterial == null)
                {
                    cachedChargeDotMaterial = ABY_MaterialCacheUtility.MatFrom(ChargeDotTexturePath, ShaderDatabase.MoteGlow, ChargeDotColor);
                }

                return cachedChargeDotMaterial;
            }
        }

        private static Material BeamFadeMaterial
        {
            get
            {
                if (cachedBeamFadeMaterial == null)
                {
                    cachedBeamFadeMaterial = ABY_MaterialCacheUtility.MatFrom(BeamTexturePath, ShaderDatabase.MoteGlow, FadeColor);
                }

                return cachedBeamFadeMaterial;
            }
        }

        public void Initialize(Thing launcher, Thing equipment, LocalTargetInfo targetInfo, ThingDef payloadProjectileDef)
        {
            this.launcher = launcher;
            this.equipment = equipment;
            this.payloadProjectileDef = payloadProjectileDef;
            targetThing = targetInfo.Thing;
            targetCell = targetInfo.Cell;
            retargetThing = null;
            lockedThing = null;
            rupturePulseApplied = false;
            nextRetargetCheckTick = 0;

            Vector3 launcherPos = launcher?.DrawPos ?? Position.ToVector3Shifted();
            fixedTarget = ResolveInitialTargetPosition(targetInfo);

            Vector3 initialDirection = fixedTarget - launcherPos;
            initialDirection.y = 0f;
            if (initialDirection.sqrMagnitude < 0.001f)
            {
                initialDirection = Vector3.forward;
            }

            initialDirection.Normalize();
            shotDirection = initialDirection;
            shotPerpendicular = new Vector3(-shotDirection.z, 0f, shotDirection.x);

            float muzzleOffset = ResolveMuzzleForwardOffset(equipment);
            float barrelStartOffset = ResolveBarrelStartForwardOffset(equipment);
            muzzleBase = launcherPos + shotDirection * muzzleOffset;
            chargeBase = launcherPos + shotDirection * barrelStartOffset;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_References.Look(ref equipment, "equipment");
            Scribe_References.Look(ref targetThing, "targetThing");
            Scribe_References.Look(ref retargetThing, "retargetThing");
            Scribe_References.Look(ref lockedThing, "lockedThing");
            Scribe_Defs.Look(ref payloadProjectileDef, "payloadProjectileDef");
            Scribe_Values.Look(ref targetCell, "targetCell");
            Scribe_Values.Look(ref muzzleBase, "muzzleBase");
            Scribe_Values.Look(ref chargeBase, "chargeBase");
            Scribe_Values.Look(ref shotDirection, "shotDirection");
            Scribe_Values.Look(ref shotPerpendicular, "shotPerpendicular");
            Scribe_Values.Look(ref fixedTarget, "fixedTarget");
            Scribe_Values.Look(ref ageTicks, "ageTicks", 0);
            Scribe_Values.Look(ref nextRetargetCheckTick, "nextRetargetCheckTick", 0);
            Scribe_Values.Look(ref rupturePulseApplied, "rupturePulseApplied", false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                List<bool> applied = new List<bool>(damageApplied);
                Scribe_Collections.Look(ref applied, "damageApplied", LookMode.Value);
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                List<bool> applied = null;
                Scribe_Collections.Look(ref applied, "damageApplied", LookMode.Value);
                if (applied != null)
                {
                    int count = Mathf.Min(applied.Count, damageApplied.Length);
                    for (int i = 0; i < count; i++)
                    {
                        damageApplied[i] = applied[i];
                    }
                }
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (shotDirection.sqrMagnitude < 0.001f)
                {
                    Vector3 launcherPos = launcher?.DrawPos ?? Position.ToVector3Shifted();
                    Vector3 targetPos = fixedTarget == Vector3.zero ? targetCell.ToVector3Shifted() : fixedTarget;
                    shotDirection = targetPos - launcherPos;
                    shotDirection.y = 0f;
                    if (shotDirection.sqrMagnitude < 0.001f)
                    {
                        shotDirection = Vector3.forward;
                    }
                    shotDirection.Normalize();
                }

                if (shotPerpendicular.sqrMagnitude < 0.001f)
                {
                    shotPerpendicular = new Vector3(-shotDirection.z, 0f, shotDirection.x);
                }
            }
        }

        protected override void Tick()
        {
            base.Tick();
            ageTicks++;

            for (int rail = 0; rail < RailCount; rail++)
            {
                if (!damageApplied[rail] && IsRailDamageFrame(rail))
                {
                    ApplyRailDamage(rail);
                    damageApplied[rail] = true;
                }
            }

            if (ageTicks > TotalLifetimeTicks)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Material material = BeamMaterial;
            if (material == null || shotDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            if (ageTicks < BeamStartTick)
            {
                DrawChargeDots();
                return;
            }

            int localBeamAge = ageTicks - BeamStartTick;
            int activeRail = localBeamAge / BeamCycleTicks;
            int activeRailTick = localBeamAge % BeamCycleTicks;
            if (activeRail >= 0 && activeRail < RailCount && activeRailTick < BeamHoldTicks)
            {
                Vector3 railMuzzle = RailMuzzle(activeRail);
                Vector3 beamTarget = ResolveCurrentTargetPosition();

                Vector3 beamDirection = beamTarget - railMuzzle;
                beamDirection.y = 0f;
                float distance = beamDirection.magnitude;
                if (distance < 0.1f)
                {
                    return;
                }

                beamDirection /= distance;
                Material activeMaterial = (activeRailTick < 2 || activeRailTick > BeamHoldTicks - 4) ? (BeamFadeMaterial ?? material) : material;
                DrawBeamSegment(activeMaterial, railMuzzle, beamDirection, distance, ResolveMainBeamWidth(equipment));
            }
        }

        private void DrawChargeDots()
        {
            Material chargeMaterial = ChargeDotMaterial;
            if (chargeMaterial == null)
            {
                return;
            }

            for (int rail = 0; rail < RailCount; rail++)
            {
                int railStartTick = rail * RailChargeStepTicks;
                if (ageTicks < railStartTick)
                {
                    continue;
                }

                int railAge = Mathf.Max(0, ageTicks - railStartTick);
                float t = Mathf.Clamp01(railAge / (float)RailChargeStepTicks);
                Vector3 start = RailChargeStart(rail);
                Vector3 muzzle = RailMuzzle(rail);
                Vector3 dotPos = Vector3.Lerp(start, muzzle, 0.18f + 0.82f * t);
                float pulse = 0.82f + Mathf.Sin(t * Mathf.PI) * 0.36f;
                float size = (railAge < RailChargeStepTicks) ? ChargeDotSize * pulse : CompletedChargeDotSize;
                DrawBillboardDot(chargeMaterial, dotPos, size);
            }
        }

        private bool IsRailDamageFrame(int rail)
        {
            int localBeamAge = ageTicks - BeamStartTick;
            return localBeamAge >= 0 && localBeamAge == rail * BeamCycleTicks;
        }

        private void ApplyRailDamage(int rail)
        {
            if (Map == null || payloadProjectileDef?.projectile == null)
            {
                return;
            }

            Thing hitThing = ResolveHitThingForRail(rail);
            if (hitThing == null || hitThing.Destroyed)
            {
                if (rail == RailCount - 1 && !rupturePulseApplied)
                {
                    ApplyRupturePulse(ResolveCurrentTargetCell());
                    rupturePulseApplied = true;
                }
                return;
            }

            if (rail == 0 && lockedThing == null)
            {
                lockedThing = hitThing;
            }

            ProjectileProperties projectile = payloadProjectileDef.projectile;
            DamageDef damageDef = projectile.damageDef ?? DamageDefOf.Burn;
            float baseDamageAmount = Mathf.Max(1f, projectile.GetDamageAmount(launcher, null));
            float armorPenetration = Mathf.Max(0f, projectile.GetArmorPenetration(launcher, null));
            float damageMultiplier = ResolveRailDamageMultiplier(rail, hitThing);
            float damageAmount = baseDamageAmount * damageMultiplier;

            Vector3 railMuzzle = RailMuzzle(Mathf.Clamp(rail, 0, RailCount - 1));
            Vector3 targetPosition = hitThing.DrawPos;
            Vector3 hitDirection = targetPosition - railMuzzle;
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude < 0.001f)
            {
                hitDirection = shotDirection;
            }

            float angle = hitDirection.AngleFlat();
            DamageInfo damageInfo = new DamageInfo(
                damageDef,
                damageAmount,
                armorPenetration,
                angle,
                launcher,
                null,
                equipment?.def,
                DamageInfo.SourceCategory.ThingOrUnknown,
                hitThing);

            bool applied = ABY_ProjectileImpactSafetyUtility.TryApplyDamage(Map, hitThing, damageInfo, "Thing_CrownReactorBeamSequence");
            if (!applied)
            {
                return;
            }

            if (rail == 1 && IsSystemTarget(hitThing))
            {
                ApplyEmpShear(hitThing, angle, ShieldShearEmpDamage);
            }

            if (rail == 2)
            {
                ApplyOverlineSecondaryHits(railMuzzle, targetPosition, hitThing, baseDamageAmount, armorPenetration);
            }

            if (rail == RailCount - 1 && !rupturePulseApplied && (hitThing.Destroyed || (hitThing is Pawn pawn && pawn.Dead)))
            {
                ApplyRupturePulse(hitThing.PositionHeld.IsValid ? hitThing.PositionHeld : ResolveCurrentTargetCell());
                rupturePulseApplied = true;
            }
        }

        private float ResolveRailDamageMultiplier(int rail, Thing hitThing)
        {
            switch (Mathf.Clamp(rail, 0, RailCount - 1))
            {
                case 1:
                    return IsSystemTarget(hitThing) ? ShieldShearSystemMultiplier : 1f;
                case 3:
                    if (lockedThing != null && !lockedThing.Destroyed && ReferenceEquals(lockedThing, hitThing))
                    {
                        Pawn pawn = hitThing as Pawn;
                        return pawn != null && ABY_AbyssalPawnClassificationUtility.IsBossOrMiniBoss(pawn)
                            ? CrownVerdictBossMultiplier
                            : CrownVerdictMultiplier;
                    }
                    return 1f;
                default:
                    return 1f;
            }
        }

        private void ApplyEmpShear(Thing hitThing, float angle, float amount)
        {
            if (hitThing == null || hitThing.Destroyed || amount <= 0.1f)
            {
                return;
            }

            DamageInfo empInfo = new DamageInfo(
                DamageDefOf.EMP,
                amount,
                999f,
                angle,
                launcher,
                null,
                equipment?.def,
                DamageInfo.SourceCategory.ThingOrUnknown,
                hitThing);
            ABY_ProjectileImpactSafetyUtility.TryApplyDamage(Map, hitThing, empInfo, "Thing_CrownReactorBeamSequence-emp-shear");
        }

        private void ApplyOverlineSecondaryHits(Vector3 railMuzzle, Vector3 primaryTargetPosition, Thing primaryHit, float baseDamageAmount, float baseArmorPenetration)
        {
            if (Map == null || primaryTargetPosition == default(Vector3))
            {
                return;
            }

            HashSet<Thing> alreadyHit = new HashSet<Thing>();
            if (primaryHit != null)
            {
                alreadyHit.Add(primaryHit);
            }

            int hits = 0;
            IntVec3 lastCell = IntVec3.Invalid;
            for (int step = 1; step <= OverlineCells && hits < OverlineMaxHits; step++)
            {
                Vector3 sample = primaryTargetPosition + shotDirection * step;
                IntVec3 cell = IntVec3.FromVector3(sample);
                if (!cell.IsValid || cell == lastCell || !cell.InBounds(Map))
                {
                    continue;
                }

                lastCell = cell;
                Thing secondary = ResolveSecondaryThingInCell(cell, alreadyHit);
                if (secondary == null)
                {
                    continue;
                }

                alreadyHit.Add(secondary);
                hits++;
                ApplySecondaryLineDamage(secondary, railMuzzle, baseDamageAmount * OverlineDamageMultiplier, baseArmorPenetration * OverlineArmorMultiplier, "overline");
            }
        }

        private void ApplyRupturePulse(IntVec3 center)
        {
            if (!center.IsValid || Map == null || !center.InBounds(Map) || payloadProjectileDef?.projectile == null)
            {
                return;
            }

            ProjectileProperties projectile = payloadProjectileDef.projectile;
            float baseDamageAmount = Mathf.Max(1f, projectile.GetDamageAmount(launcher, null));
            float armorPenetration = Mathf.Max(0f, projectile.GetArmorPenetration(launcher, null));
            float pulseDamage = Mathf.Max(4f, baseDamageAmount * RupturePulseDamageMultiplier);
            float pulseArmor = armorPenetration * RupturePulseArmorMultiplier;
            int applied = 0;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 1.45f, true))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing candidate = things[i];
                    if (!IsValidSecondaryDamageTarget(candidate, null))
                    {
                        continue;
                    }

                    ApplySecondaryLineDamage(candidate, muzzleBase, pulseDamage, pulseArmor, "rupture-pulse");
                    applied++;
                    if (applied >= 4)
                    {
                        return;
                    }
                }
            }
        }

        private Thing ResolveHitThingForRail(int rail)
        {
            Thing direct = ResolveDirectTargetThing();
            if (direct != null)
            {
                return direct;
            }

            if (ageTicks >= nextRetargetCheckTick)
            {
                nextRetargetCheckTick = ageTicks + RetargetCheckIntervalTicks;
                retargetThing = FindNearbyHostileRetarget();
            }

            if (IsValidPrimaryTarget(retargetThing))
            {
                return retargetThing;
            }

            return ResolveHitThingInCell(targetCell);
        }

        private Thing ResolveDirectTargetThing()
        {
            if (IsValidPrimaryTarget(targetThing))
            {
                return targetThing;
            }

            return null;
        }

        private Thing FindNearbyHostileRetarget()
        {
            if (Map == null)
            {
                return null;
            }

            IntVec3 center = ResolveCurrentTargetCell();
            if (!center.IsValid || !center.InBounds(Map))
            {
                return null;
            }

            Thing best = null;
            float bestDistance = float.MaxValue;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, RetargetRadius, true))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing candidate = things[i];
                    if (!IsValidPrimaryTarget(candidate) || !IsHostileOrValidCombatStructure(candidate))
                    {
                        continue;
                    }

                    float dist = candidate.PositionHeld.DistanceToSquared(center);
                    if (dist < bestDistance)
                    {
                        best = candidate;
                        bestDistance = dist;
                    }
                }
            }

            return best;
        }

        private Thing ResolveHitThingInCell(IntVec3 cell)
        {
            if (!cell.IsValid || Map == null || !cell.InBounds(Map))
            {
                return null;
            }

            List<Thing> things = cell.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing candidate = things[i];
                if (IsValidPrimaryTarget(candidate) && IsHostileOrValidCombatStructure(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private Thing ResolveSecondaryThingInCell(IntVec3 cell, HashSet<Thing> alreadyHit)
        {
            if (!cell.IsValid || Map == null || !cell.InBounds(Map))
            {
                return null;
            }

            List<Thing> things = cell.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing candidate = things[i];
                if (IsValidSecondaryDamageTarget(candidate, alreadyHit))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ApplySecondaryLineDamage(Thing target, Vector3 origin, float damageAmount, float armorPenetration, string stage)
        {
            if (target == null || target.Destroyed || damageAmount <= 0.1f)
            {
                return;
            }

            Vector3 hitDirection = target.DrawPos - origin;
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude < 0.001f)
            {
                hitDirection = shotDirection;
            }

            DamageInfo damageInfo = new DamageInfo(
                payloadProjectileDef?.projectile?.damageDef ?? DamageDefOf.Burn,
                damageAmount,
                Mathf.Max(0f, armorPenetration),
                hitDirection.AngleFlat(),
                launcher,
                null,
                equipment?.def,
                DamageInfo.SourceCategory.ThingOrUnknown,
                target);
            ABY_ProjectileImpactSafetyUtility.TryApplyDamage(Map, target, damageInfo, "Thing_CrownReactorBeamSequence-" + stage);
        }

        private bool IsValidPrimaryTarget(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            if (thing.MapHeld != null && Map != null && thing.MapHeld != Map)
            {
                return false;
            }

            if (!thing.Spawned && thing.MapHeld == null)
            {
                return false;
            }

            return thing is Pawn || thing.def.category == ThingCategory.Building;
        }

        private bool IsValidSecondaryDamageTarget(Thing thing, HashSet<Thing> alreadyHit)
        {
            if (!IsValidPrimaryTarget(thing))
            {
                return false;
            }

            if (alreadyHit != null && alreadyHit.Contains(thing))
            {
                return false;
            }

            if (thing == launcher || ReferenceEquals(thing, equipment))
            {
                return false;
            }

            if (thing is Pawn pawn)
            {
                if (pawn.Dead || pawn.Downed && !IsHostileOrValidCombatStructure(pawn))
                {
                    return false;
                }

                return IsHostileOrValidCombatStructure(pawn);
            }

            return IsHostileOrValidCombatStructure(thing);
        }

        private bool IsHostileOrValidCombatStructure(Thing thing)
        {
            if (thing == null)
            {
                return false;
            }

            if (launcher != null && thing.HostileTo(launcher))
            {
                return true;
            }

            if (thing.def.category == ThingCategory.Building)
            {
                if (thing.Faction == null)
                {
                    return true;
                }

                return launcher == null || thing.Faction != launcher.Faction;
            }

            return false;
        }

        private bool IsSystemTarget(Thing hitThing)
        {
            if (hitThing == null)
            {
                return false;
            }

            Pawn pawn = hitThing as Pawn;
            if (pawn != null)
            {
                if (pawn.RaceProps?.IsMechanoid == true)
                {
                    return true;
                }

                if (HasActiveShield(pawn))
                {
                    return true;
                }

                if (pawn.TryGetComp<CompABY_ReactorAegis>()?.AegisActive == true)
                {
                    return true;
                }
            }

            return hitThing.def.category == ThingCategory.Building;
        }

        private static bool HasActiveShield(Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null)
            {
                return false;
            }

            List<Apparel> apparel = pawn.apparel.WornApparel;
            for (int i = 0; i < apparel.Count; i++)
            {
                CompShield shield = apparel[i]?.GetComp<CompShield>();
                if (shield != null && shield.ShieldState == ShieldState.Active)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 ResolveCurrentTargetPosition()
        {
            Thing direct = ResolveDirectTargetThing();
            if (direct != null)
            {
                return direct.DrawPos;
            }

            if (IsValidPrimaryTarget(retargetThing))
            {
                return retargetThing.DrawPos;
            }

            if (targetCell.IsValid)
            {
                return targetCell.ToVector3Shifted();
            }

            return fixedTarget;
        }

        private IntVec3 ResolveCurrentTargetCell()
        {
            Thing direct = ResolveDirectTargetThing();
            if (direct != null && direct.PositionHeld.IsValid)
            {
                return direct.PositionHeld;
            }

            if (IsValidPrimaryTarget(retargetThing) && retargetThing.PositionHeld.IsValid)
            {
                return retargetThing.PositionHeld;
            }

            if (targetCell.IsValid)
            {
                return targetCell;
            }

            return IntVec3.FromVector3(fixedTarget);
        }

        private Vector3 RailMuzzle(int rail)
        {
            return muzzleBase + shotPerpendicular * ResolveRailOffset(rail, equipment);
        }

        private Vector3 RailChargeStart(int rail)
        {
            return chargeBase + shotPerpendicular * ResolveRailOffset(rail, equipment);
        }

        private static Vector3 ResolveInitialTargetPosition(LocalTargetInfo targetInfo)
        {
            if (targetInfo.HasThing && targetInfo.Thing != null)
            {
                return targetInfo.Thing.DrawPos;
            }

            return targetInfo.Cell.ToVector3Shifted();
        }

        private static float ResolveMuzzleForwardOffset(Thing equipment)
        {
            float weaponLength = ResolveWeaponLength(equipment);
            return Mathf.Clamp(weaponLength * MuzzleForwardRatio, MinMuzzleForwardOffset, MaxMuzzleForwardOffset);
        }

        private static float ResolveBarrelStartForwardOffset(Thing equipment)
        {
            float weaponLength = ResolveWeaponLength(equipment);
            return Mathf.Clamp(weaponLength * BarrelStartForwardRatio, MinBarrelStartForwardOffset, MaxBarrelStartForwardOffset);
        }

        private static float ResolveRailOffset(int rail, Thing equipment)
        {
            float weaponHeight = ResolveWeaponHeight(equipment);
            float outer = Mathf.Clamp(weaponHeight * OuterRailOffsetRatio, MinOuterRailOffset, MaxOuterRailOffset);
            float inner = Mathf.Clamp(weaponHeight * InnerRailOffsetRatio, MinInnerRailOffset, MaxInnerRailOffset);
            switch (Mathf.Clamp(rail, 0, RailCount - 1))
            {
                case 0:
                    return outer;
                case 1:
                    return inner;
                case 2:
                    return -inner;
                default:
                    return -outer;
            }
        }

        private static float ResolveMainBeamWidth(Thing equipment)
        {
            float weaponHeight = ResolveWeaponHeight(equipment);
            return Mathf.Clamp(weaponHeight * MainBeamWidthRatio, MinMainBeamWidth, MaxMainBeamWidth);
        }

        private static float ResolveWeaponLength(Thing equipment)
        {
            if (equipment?.def?.graphicData != null)
            {
                return Mathf.Max(MinimumWeaponLength, equipment.def.graphicData.drawSize.x);
            }

            return MinimumWeaponLength;
        }

        private static float ResolveWeaponHeight(Thing equipment)
        {
            if (equipment?.def?.graphicData != null)
            {
                return Mathf.Max(MinimumWeaponHeight, equipment.def.graphicData.drawSize.y);
            }

            return MinimumWeaponHeight;
        }

        private static void DrawBeamSegment(Material material, Vector3 start, Vector3 direction, float length, float width)
        {
            if (material == null || length <= 0.01f)
            {
                return;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            direction.Normalize();

            Vector3 end = start + direction * length;
            start.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            end.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Vector3 delta = end - start;
            float resolvedLength = delta.MagnitudeHorizontal();
            if (resolvedLength <= 0.05f)
            {
                return;
            }

            Vector3 center = (start + end) * 0.5f;
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            // Source beam texture is horizontal left-to-right. Align local X with the world-space target vector.
            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg - 90f;
            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(resolvedLength, 1f, Mathf.Max(0.02f, width)));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private static void DrawBillboardDot(Material material, Vector3 center, float size)
        {
            if (material == null || size <= 0.01f)
            {
                return;
            }

            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
