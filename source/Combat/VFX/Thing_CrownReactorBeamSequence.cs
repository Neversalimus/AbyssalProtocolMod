using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Transient beam presentation for the Crown Reactor Multilance.
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
        private const int RailCount = 4;
        private const int RailChargeStepTicks = 10;
        private const int PreDischargeDelayTicks = 12;
        private const int BeamHoldTicks = 20;
        private const int BeamGapTicks = 4;
        private const int FadeTicks = 12;

        // These values are intentionally visual-only. Damage remains four single pulses in Tick().
        private const float MainBeamWidth = 0.48f;
        private const float ChargeBeamWidth = 0.22f;
        private const float ChargeLength = 1.04f;

        // Muzzle/rail alignment is estimated from the actual weapon drawSize and shot direction.
        // RimWorld does not expose a stable per-weapon muzzle transform for equipment graphics, so
        // keep this deterministic, cheap, and tied to the fired direction instead of pawn rotation scans.
        private const float MinimumMuzzleForwardOffset = 1.12f;
        private const float MaximumMuzzleForwardOffset = 1.48f;
        private const float MuzzleForwardInset = 0.16f;
        private const float ChargeStartFallbackForwardOffset = 0.28f;

        // Four barrel lanes from top to bottom, perpendicular to the shot direction.
        private static readonly float[] RailOffsets = { 0.24f, 0.08f, -0.08f, -0.24f };
        private static readonly Color ChargeColor = new Color(0.82f, 1f, 1f, 0.58f);
        private static readonly Color FadeColor = new Color(1f, 1f, 1f, 0.72f);

        private static Material cachedBeamMaterial;
        private static Material cachedChargeMaterial;
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

        private static Material ChargeMaterial
        {
            get
            {
                if (cachedChargeMaterial == null)
                {
                    cachedChargeMaterial = ABY_MaterialCacheUtility.MatFrom(BeamTexturePath, ShaderDatabase.MoteGlow, ChargeColor);
                }

                return cachedChargeMaterial;
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
            muzzleBase = launcherPos + shotDirection * muzzleOffset;

            // Charge is drawn along the visible weapon rails, not in front of the muzzle.
            float chargeStartForward = Mathf.Max(ChargeStartFallbackForwardOffset, muzzleOffset - ChargeLength + MuzzleForwardInset);
            chargeBase = launcherPos + shotDirection * chargeStartForward;
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
            if (material == null)
            {
                return;
            }

            if (shotDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            int chargedRails = Mathf.Clamp(ageTicks / RailChargeStepTicks + 1, 0, RailCount);
            if (ageTicks < BeamStartTick)
            {
                Material chargeMaterial = ChargeMaterial ?? material;
                for (int rail = 0; rail < chargedRails; rail++)
                {
                    DrawBeamSegment(chargeMaterial, RailChargeStart(rail), shotDirection, ChargeLength, ChargeBeamWidth);
                }
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
                DrawBeamSegment(activeMaterial, railMuzzle, beamDirection, distance, MainBeamWidth);
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
            return muzzleBase + shotPerpendicular * RailOffsets[Mathf.Clamp(rail, 0, RailOffsets.Length - 1)];
        }

        private Vector3 RailChargeStart(int rail)
        {
            return chargeBase + shotPerpendicular * RailOffsets[Mathf.Clamp(rail, 0, RailOffsets.Length - 1)];
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
            float weaponLength = 1.95f;
            if (equipment?.def?.graphicData != null)
            {
                weaponLength = Mathf.Max(weaponLength, equipment.def.graphicData.drawSize.x);
            }

            return Mathf.Clamp(weaponLength * 0.62f + MuzzleForwardInset, MinimumMuzzleForwardOffset, MaximumMuzzleForwardOffset);
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
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(resolvedLength, 1f, Mathf.Max(0.02f, width)));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
