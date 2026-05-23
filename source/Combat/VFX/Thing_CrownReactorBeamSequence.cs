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
    /// - four single damage applications, one per rail discharge;
    /// - cached/quantized beam materials through the shared Abyssal material cache.
    /// </summary>
    [StaticConstructorOnStartup]
    public class Thing_CrownReactorBeamSequence : Thing
    {
        private const string BeamTexturePath = "Things/Projectile/ABY_CrownReactorBeamSegment";
        private const string ChargeDotTexturePath = "Things/Projectile/ABY_CrownReactorChargeDot";
        private const int RailCount = 4;
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
        private const float ChargeDotSizeRatio = 0.16f;
        private const float MainBeamWidthRatio = 0.105f;
        private const float MinChargeDotSize = 0.075f;
        private const float MaxChargeDotSize = 0.105f;
        private const float MinMainBeamWidth = 0.058f;
        private const float MaxMainBeamWidth = 0.078f;

        private static readonly Color ChargeDotColor = new Color(0.82f, 1f, 1f, 0.86f);
        private static readonly Color FadeColor = new Color(1f, 1f, 1f, 0.72f);

        private static Material cachedBeamMaterial;
        private static Material cachedChargeDotMaterial;
        private static Material cachedBeamFadeMaterial;

        private Thing launcher;
        private Thing equipment;
        private Thing targetThing;
        private ThingDef payloadProjectileDef;
        private IntVec3 targetCell;
        private Vector3 muzzleBase;
        private Vector3 chargeBase;
        private Vector3 shotDirection;
        private Vector3 shotPerpendicular;
        private Vector3 fixedTarget;
        private int ageTicks;
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
            Scribe_Defs.Look(ref payloadProjectileDef, "payloadProjectileDef");
            Scribe_Values.Look(ref targetCell, "targetCell");
            Scribe_Values.Look(ref muzzleBase, "muzzleBase");
            Scribe_Values.Look(ref chargeBase, "chargeBase");
            Scribe_Values.Look(ref shotDirection, "shotDirection");
            Scribe_Values.Look(ref shotPerpendicular, "shotPerpendicular");
            Scribe_Values.Look(ref fixedTarget, "fixedTarget");
            Scribe_Values.Look(ref ageTicks, "ageTicks", 0);

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

            float chargeDotSize = ResolveChargeDotSize(equipment);
            float mainBeamWidth = ResolveMainBeamWidth(equipment);
            if (ageTicks < BeamStartTick)
            {
                DrawChargeDots(ChargeDotMaterial ?? material, chargeDotSize);
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
                Material activeMaterial = (activeRailTick < 3 || activeRailTick > BeamHoldTicks - 5) ? (BeamFadeMaterial ?? material) : material;
                DrawBeamSegment(activeMaterial, railMuzzle, beamDirection, distance, mainBeamWidth);
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

            Thing hitThing = ResolveHitThing();
            if (hitThing == null || hitThing.Destroyed)
            {
                return;
            }

            Vector3 railMuzzle = RailMuzzle(Mathf.Clamp(rail, 0, RailCount - 1));
            Vector3 targetPosition = ResolveCurrentTargetPosition();
            Vector3 hitDirection = targetPosition - railMuzzle;
            hitDirection.y = 0f;
            if (hitDirection.sqrMagnitude < 0.001f)
            {
                hitDirection = shotDirection;
            }

            ProjectileProperties projectile = payloadProjectileDef.projectile;
            DamageDef damageDef = projectile.damageDef ?? DamageDefOf.Burn;
            float damageAmount = Mathf.Max(1f, projectile.GetDamageAmount(launcher, null));
            float armorPenetration = Mathf.Max(0f, projectile.GetArmorPenetration(launcher, null));
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

            ABY_ProjectileImpactSafetyUtility.TryApplyDamage(Map, hitThing, damageInfo, "Thing_CrownReactorBeamSequence");
        }

        private Thing ResolveHitThing()
        {
            if (targetThing != null && !targetThing.Destroyed && targetThing.MapHeld == Map)
            {
                return targetThing;
            }

            if (!targetCell.IsValid || Map == null || !targetCell.InBounds(Map))
            {
                return null;
            }

            List<Thing> things = targetCell.GetThingList(Map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing candidate = things[i];
                if (candidate == null || candidate.Destroyed)
                {
                    continue;
                }

                if (candidate is Pawn || candidate.def.category == ThingCategory.Building)
                {
                    return candidate;
                }
            }

            return null;
        }

        private Vector3 ResolveCurrentTargetPosition()
        {
            if (targetThing != null && !targetThing.Destroyed)
            {
                return targetThing.DrawPos;
            }

            if (targetCell.IsValid)
            {
                return targetCell.ToVector3Shifted();
            }

            return fixedTarget;
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

        private static float ResolveChargeLength(Thing equipment)
        {
            return Mathf.Max(0.32f, ResolveMuzzleForwardOffset(equipment) - ResolveBarrelStartForwardOffset(equipment));
        }

        private void DrawChargeDots(Material material, float dotSize)
        {
            if (material == null)
            {
                return;
            }

            int completedRails = Mathf.Clamp(ageTicks / RailChargeStepTicks, 0, RailCount);
            for (int rail = 0; rail < completedRails; rail++)
            {
                DrawDot(material, RailMuzzle(rail), dotSize * 0.88f);
            }

            if (completedRails >= RailCount)
            {
                return;
            }

            float railProgress = (ageTicks % RailChargeStepTicks) / (float)Mathf.Max(1, RailChargeStepTicks - 1);
            railProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(railProgress));
            Vector3 dotPosition = Vector3.Lerp(RailChargeStart(completedRails), RailMuzzle(completedRails), railProgress);
            float pulse = 0.92f + Mathf.Sin(ageTicks * 0.9f) * 0.10f;
            DrawDot(material, dotPosition, dotSize * pulse);
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

        private static float ResolveChargeDotSize(Thing equipment)
        {
            float weaponHeight = ResolveWeaponHeight(equipment);
            return Mathf.Clamp(weaponHeight * ChargeDotSizeRatio, MinChargeDotSize, MaxChargeDotSize);
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

        private static void DrawDot(Material material, Vector3 position, float size)
        {
            if (material == null || size <= 0.01f)
            {
                return;
            }

            position.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Matrix4x4 matrix = Matrix4x4.TRS(
                position,
                Quaternion.identity,
                new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
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
    }
}
