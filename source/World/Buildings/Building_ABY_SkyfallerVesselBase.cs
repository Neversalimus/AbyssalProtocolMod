using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public abstract class Building_ABY_SkyfallerVesselBase : Building
    {
        private int ticksSinceImpact;
        private bool payloadReleased;
        private bool releaseFailedPermanently;
        private bool impactProcessed;
        private bool launching;
        private int launchTicks;

        protected int TicksSinceImpact => ticksSinceImpact;
        protected bool PayloadReleased => payloadReleased;
        protected bool Launching => launching;
        protected float LaunchProgress => Mathf.Clamp01(launchTicks / (float)Math.Max(1, LaunchDurationTicks));

        protected virtual int ReleaseDelayTicks => 600;
        protected virtual int PostReleaseTicks => 300;
        protected virtual int LaunchDurationTicks => 60;

        protected virtual float ImpactExplosionRadius => 0f;
        protected virtual int ImpactExplosionDamage => 0;
        protected virtual float ImpactExplosionArmorPenetration => 0f;

        protected virtual string BodyTexPath => null;
        protected virtual string ShadowTexPath => null;
        protected virtual Shader BodyShader => ShaderDatabase.Cutout;
        protected virtual Shader ShadowShader => ShaderDatabase.TransparentPostLight;
        protected virtual float BodyScaleX => 1f;
        protected virtual float BodyScaleZ => 1f;
        protected virtual float ShadowScale => 1f;
        protected virtual float ShadowAlpha => 0.62f;

        protected virtual float LaunchDriftX => 0f;
        protected virtual float LaunchDriftZ => 0f;
        protected virtual float LaunchAltitudeBoost => 0f;
        protected virtual float LaunchBodyScaleEnd => 1f;
        protected virtual float LaunchShadowScaleEnd => 0.4f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceImpact, "ticksSinceImpact", 0);
            Scribe_Values.Look(ref payloadReleased, "payloadReleased", false);
            Scribe_Values.Look(ref releaseFailedPermanently, "releaseFailedPermanently", false);
            Scribe_Values.Look(ref impactProcessed, "impactProcessed", false);
            Scribe_Values.Look(ref launching, "launching", false);
            Scribe_Values.Look(ref launchTicks, "launchTicks", 0);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad && !impactProcessed)
            {
                impactProcessed = true;
                TriggerImpactEffects();
                OnImpactProcessed();
            }
        }

        protected override void Tick()
        {
            base.Tick();

            if (Map == null || Destroyed)
            {
                return;
            }

            if (launching)
            {
                TickLaunching();
                return;
            }

            ticksSinceImpact++;

            if (!payloadReleased)
            {
                TickDormantVessel();

                if (!releaseFailedPermanently && ticksSinceImpact >= ReleaseDelayTicks)
                {
                    bool permanentFailure;
                    if (TryReleasePayload(out permanentFailure))
                    {
                        payloadReleased = true;
                        OnPayloadReleased();
                    }
                    else if (permanentFailure)
                    {
                        releaseFailedPermanently = true;
                        OnReleaseFailedPermanently();
                    }
                }

                return;
            }

            TickSpentVessel();

            if (ticksSinceImpact >= ReleaseDelayTicks + PostReleaseTicks)
            {
                BeginLaunch();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Vector3 bodyLoc = drawLoc;
            Vector3 shadowLoc = drawLoc;
            float bodyScaleX = BodyScaleX;
            float bodyScaleZ = BodyScaleZ;
            float shadowScale = ShadowScale;
            float shadowAlpha = ShadowAlpha;

            ApplyLaunchTransform(ref bodyLoc, ref shadowLoc, ref bodyScaleX, ref bodyScaleZ, ref shadowScale, ref shadowAlpha);

            DrawShadow(shadowLoc, shadowScale, shadowAlpha);
            DrawBody(bodyLoc, bodyScaleX, bodyScaleZ);
        }

        protected virtual void TickDormantVessel()
        {
        }

        protected virtual void TickSpentVessel()
        {
        }

        protected virtual void TickLaunchFx()
        {
        }

        protected virtual void OnPayloadReleased()
        {
        }

        protected virtual void OnImpactProcessed()
        {
        }

        protected virtual void OnBeginLaunch()
        {
        }

        protected virtual void OnReleaseFailedPermanently()
        {
        }

        protected abstract bool TryReleasePayload(out bool permanentFailure);

        protected virtual IntVec3 FindReleaseCell(float searchRadius = 2.9f)
        {
            if (IsValidReleaseCell(Position))
            {
                return Position;
            }

            for (int i = 0; i < GenRadial.NumCellsInRadius(searchRadius); i++)
            {
                IntVec3 candidate = Position + GenRadial.RadialPattern[i];
                if (IsValidReleaseCell(candidate))
                {
                    return candidate;
                }
            }

            return Position;
        }

        protected virtual bool IsValidReleaseCell(IntVec3 cell)
        {
            return cell.IsValid && cell.InBounds(Map) && cell.Standable(Map) && !cell.Fogged(Map);
        }

        protected Vector3 GetCurrentBodyDrawPos()
        {
            Vector3 bodyLoc = DrawPos;
            Vector3 shadowLoc = DrawPos;
            float bodyScaleX = BodyScaleX;
            float bodyScaleZ = BodyScaleZ;
            float shadowScale = ShadowScale;
            float shadowAlpha = ShadowAlpha;
            ApplyLaunchTransform(ref bodyLoc, ref shadowLoc, ref bodyScaleX, ref bodyScaleZ, ref shadowScale, ref shadowAlpha);
            return bodyLoc;
        }

        private void TriggerImpactEffects()
        {
            if (Map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.8f);
            FleckMaker.ThrowHeatGlow(Position, Map, 2.1f);
            FleckMaker.ThrowMicroSparks(DrawPos, Map);
            FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 3);

            if (ImpactExplosionRadius > 0.1f && ImpactExplosionDamage > 0)
            {
                GenExplosion.DoExplosion(Position, Map, ImpactExplosionRadius, DamageDefOf.Burn, this, ImpactExplosionDamage, ImpactExplosionArmorPenetration);
            }
        }

        private void BeginLaunch()
        {
            if (launching || Map == null || Destroyed)
            {
                return;
            }

            launching = true;
            launchTicks = 0;
            OnBeginLaunch();
        }

        private void TickLaunching()
        {
            launchTicks++;
            TickLaunchFx();

            if (launchTicks >= LaunchDurationTicks)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        private void ApplyLaunchTransform(ref Vector3 bodyLoc, ref Vector3 shadowLoc, ref float bodyScaleX, ref float bodyScaleZ, ref float shadowScale, ref float shadowAlpha)
        {
            if (launching)
            {
                float progress = LaunchProgress;
                float eased = 1f - Mathf.Pow(1f - progress, 2.2f);

                bodyLoc.x += eased * LaunchDriftX;
                bodyLoc.z += eased * LaunchDriftZ;
                bodyLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.04f + eased * LaunchAltitudeBoost;

                shadowLoc.x += eased * (LaunchDriftX * 0.14f);
                shadowLoc.z += eased * (LaunchDriftZ * 0.10f);
                shadowLoc.y = AltitudeLayer.Shadows.AltitudeFor();

                float bodyScale = Mathf.Lerp(1f, LaunchBodyScaleEnd, eased);
                float shadowScaleFactor = Mathf.Lerp(1f, LaunchShadowScaleEnd, eased);
                bodyScaleX *= bodyScale;
                bodyScaleZ *= bodyScale;
                shadowScale *= shadowScaleFactor;
                shadowAlpha *= 1f - eased;
            }
            else
            {
                bodyLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.04f;
                shadowLoc.y = AltitudeLayer.Shadows.AltitudeFor();
            }
        }

        private void DrawBody(Vector3 loc, float scaleX, float scaleZ)
        {
            if (BodyTexPath.NullOrEmpty())
            {
                return;
            }

            Material material = MaterialPool.MatFrom(BodyTexPath, BodyShader, Color.white);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.identity, new Vector3(scaleX, 1f, scaleZ));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private void DrawShadow(Vector3 loc, float scale, float alpha)
        {
            if (ShadowTexPath.NullOrEmpty() || alpha <= 0.01f)
            {
                return;
            }

            Material shadowMat = MaterialPool.MatFrom(ShadowTexPath, ShadowShader, new Color(1f, 1f, 1f, alpha));
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMat, 0);
        }
    }
}
