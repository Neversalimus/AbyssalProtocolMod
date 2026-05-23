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
    public class Thing_CrownReactorBeamSequence : Thing
    {
        private const string BeamTexturePath = "Things/Projectile/ABY_CrownReactorBeamSegment";
        private const int RailCount = 4;
        private const int RailChargeStepTicks = 10;
        private const int PreDischargeDelayTicks = 12;
        private const int BeamHoldTicks = 20;
        private const int BeamGapTicks = 4;
        private const int FadeTicks = 12;
        private const float MainBeamWidth = 0.34f;
        private const float ChargeBeamWidth = 0.16f;
        private const float ChargeLength = 1.45f;

        private static readonly float[] RailOffsets = { 0.27f, 0.09f, -0.09f, -0.27f };
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
        private Vector3 origin;
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
            Vector3 targetPos = ResolveInitialTargetPosition(targetInfo);
            Vector3 direction = targetPos - launcherPos;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();
            origin = launcherPos + direction * 0.82f;
            fixedTarget = targetPos;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref launcher, "launcher");
            Scribe_References.Look(ref equipment, "equipment");
            Scribe_References.Look(ref targetThing, "targetThing");
            Scribe_Defs.Look(ref payloadProjectileDef, "payloadProjectileDef");
            Scribe_Values.Look(ref targetCell, "targetCell");
            Scribe_Values.Look(ref origin, "origin");
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
        }

        protected override void Tick()
        {
            base.Tick();
            ageTicks++;

            for (int rail = 0; rail < RailCount; rail++)
            {
                if (!damageApplied[rail] && IsRailDamageFrame(rail))
                {
                    ApplyRailDamage();
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

            Vector3 beamTarget = ResolveCurrentTargetPosition();
            Vector3 beamDirection = beamTarget - origin;
            beamDirection.y = 0f;
            float distance = beamDirection.magnitude;
            if (distance < 0.1f)
            {
                return;
            }

            beamDirection /= distance;
            Vector3 perpendicular = new Vector3(-beamDirection.z, 0f, beamDirection.x);

            int chargedRails = Mathf.Clamp(ageTicks / RailChargeStepTicks + 1, 0, RailCount);
            if (ageTicks < BeamStartTick)
            {
                Material chargeMaterial = ChargeMaterial ?? material;
                for (int rail = 0; rail < chargedRails; rail++)
                {
                    DrawBeamSegment(chargeMaterial, origin + perpendicular * RailOffsets[rail], beamDirection, ChargeLength, ChargeBeamWidth);
                }
                return;
            }

            int localBeamAge = ageTicks - BeamStartTick;
            int activeRail = localBeamAge / BeamCycleTicks;
            int activeRailTick = localBeamAge % BeamCycleTicks;
            if (activeRail >= 0 && activeRail < RailCount && activeRailTick < BeamHoldTicks)
            {
                Material activeMaterial = (activeRailTick < 3 || activeRailTick > BeamHoldTicks - 5) ? (BeamFadeMaterial ?? material) : material;
                DrawBeamSegment(activeMaterial, origin + perpendicular * RailOffsets[activeRail], beamDirection, distance, MainBeamWidth);
            }
        }

        private bool IsRailDamageFrame(int rail)
        {
            int localBeamAge = ageTicks - BeamStartTick;
            return localBeamAge >= 0 && localBeamAge == rail * BeamCycleTicks;
        }

        private void ApplyRailDamage()
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

            ProjectileProperties projectile = payloadProjectileDef.projectile;
            DamageDef damageDef = projectile.damageDef ?? DamageDefOf.Burn;
            float damageAmount = Mathf.Max(1f, projectile.GetDamageAmount(launcher, null));
            float armorPenetration = Mathf.Max(0f, projectile.GetArmorPenetration(launcher, null));
            float angle = (ResolveCurrentTargetPosition() - origin).AngleFlat();

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

        private static Vector3 ResolveInitialTargetPosition(LocalTargetInfo targetInfo)
        {
            if (targetInfo.HasThing && targetInfo.Thing != null)
            {
                return targetInfo.Thing.DrawPos;
            }

            return targetInfo.Cell.ToVector3Shifted();
        }

        private static void DrawBeamSegment(Material material, Vector3 start, Vector3 direction, float length, float width)
        {
            if (material == null || length <= 0.01f)
            {
                return;
            }

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
