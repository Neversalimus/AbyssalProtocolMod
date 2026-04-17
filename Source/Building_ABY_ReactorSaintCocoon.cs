using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class Building_ABY_ReactorSaintCocoon : Building
    {
        private const string BossPawnKindDefName = "ABY_ReactorSaint";
        private const string BossLabel = "Infernal Reactor Saint";
        private const string ArrivalSoundDefName = "ABY_ReactorSaintCharge";
        private const string CompletionLetterLabelKey = "ABY_ReactorSaintSummonSuccessLabel";
        private const string CompletionLetterDescKey = "ABY_ReactorSaintSummonSuccessDesc";
        private const string CocoonTexPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon";
        private const string CocoonShadowTexPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon_Shadow";

        private const int ReleaseDelayTicks = 834;
        private const int PostReleaseTicks = 417;
        private const int LaunchDurationTicks = 84;

        private const float ImpactExplosionRadius = 3.9f;
        private const int ImpactExplosionDamage = 28;
        private const float ImpactExplosionArmorPenetration = 0.18f;

        private const float BodyScaleX = 15.95f;
        private const float BodyScaleZ = 23.10f;
        private const float ShadowScale = 23.10f;
        private const float LaunchNorthDrift = 58.00f;
        private const float LaunchSideDrift = 0.30f;
        private const float LaunchAltitudeBoost = 4.20f;
        private const float LaunchBodyScaleEnd = 0.98f;
        private const float LaunchShadowScaleEnd = 0.42f;
        private const float ShadowAlpha = 0.62f;

        private static readonly Material CocoonMat = MaterialPool.MatFrom(CocoonTexPath, ShaderDatabase.Cutout, Color.white);
        private static readonly Material CocoonShadowMat = MaterialPool.MatFrom(CocoonShadowTexPath, ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, ShadowAlpha));

        private int ticksSinceImpact;
        private bool bossReleased;
        private bool releaseFailedPermanently;
        private bool impactProcessed;
        private bool launching;
        private int launchTicks;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceImpact, "ticksSinceImpact", 0);
            Scribe_Values.Look(ref bossReleased, "bossReleased", false);
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

            if (!bossReleased)
            {
                TickDormantCocoon();

                if (!releaseFailedPermanently && ticksSinceImpact >= ReleaseDelayTicks)
                {
                    TryReleaseBoss();
                }

                return;
            }

            TickSpentCocoon();

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

            DrawCocoonShadow(shadowLoc, shadowScale, shadowAlpha);
            DrawCocoonBody(bodyLoc, bodyScaleX, bodyScaleZ);
        }


        private void ApplyLaunchTransform(ref Vector3 bodyLoc, ref Vector3 shadowLoc, ref float bodyScaleX, ref float bodyScaleZ, ref float shadowScale, ref float shadowAlpha)
        {
            if (launching)
            {
                float progress = Mathf.Clamp01(launchTicks / (float)LaunchDurationTicks);
                float eased = 1f - Mathf.Pow(1f - progress, 2.2f);

                bodyLoc.x += eased * LaunchSideDrift;
                bodyLoc.z += eased * LaunchNorthDrift;
                bodyLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.04f + eased * LaunchAltitudeBoost;

                shadowLoc.x += eased * (LaunchSideDrift * 0.14f);
                shadowLoc.z += eased * (LaunchNorthDrift * 0.10f);
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

        private Vector3 GetCurrentBodyDrawPos()
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
        private void DrawCocoonBody(Vector3 loc, float scaleX, float scaleZ)
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.identity, new Vector3(scaleX, 1f, scaleZ));
            Graphics.DrawMesh(MeshPool.plane10, matrix, CocoonMat, 0);
        }

        private void DrawCocoonShadow(Vector3 loc, float scale, float alpha)
        {
            if (alpha <= 0.01f)
            {
                return;
            }

            Material shadowMat = MaterialPool.MatFrom(CocoonShadowTexPath, ShaderDatabase.TransparentPostLight, new Color(1f, 1f, 1f, alpha));
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.identity, new Vector3(scale, 1f, scale));
            Graphics.DrawMesh(MeshPool.plane10, matrix, shadowMat, 0);
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
            GenExplosion.DoExplosion(Position, Map, ImpactExplosionRadius, DamageDefOf.Burn, this, ImpactExplosionDamage, ImpactExplosionArmorPenetration);
        }

        private void TickDormantCocoon()
        {
            if (ticksSinceImpact % 11 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (ticksSinceImpact % 30 == 0)
            {
                FleckMaker.ThrowHeatGlow(Position, Map, 1.20f);
            }

            if (ticksSinceImpact % 60 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.65f);
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
            }
        }

        private void TickSpentCocoon()
        {
            if (ticksSinceImpact % 24 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (ticksSinceImpact % 52 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.10f);
            }
        }

        private void TryReleaseBoss()
        {
            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(BossPawnKindDefName);
            Faction faction = AbyssalBossSummonUtility.ResolveHostileFaction();

            if (kindDef == null || faction == null)
            {
                releaseFailedPermanently = true;
                Log.Warning("[AbyssalProtocol] Reactor Saint cocoon could not resolve boss kind or hostile faction.");
                return;
            }

            if (!AbyssalBossSummonUtility.TryGenerateBoss(Map, kindDef, faction, BossLabel, out Pawn pawn, out string failReason))
            {
                if (ticksSinceImpact % 60 == 0 && !failReason.NullOrEmpty())
                {
                    Log.Warning("[AbyssalProtocol] Reactor Saint cocoon failed to generate boss: " + failReason);
                }

                return;
            }

            IntVec3 releaseCell = FindReleaseCell();
            AbyssalBossSummonUtility.FinalizeBossArrival(
                pawn,
                faction,
                Map,
                releaseCell,
                BossLabel,
                ArrivalSoundDefName,
                CompletionLetterLabelKey,
                CompletionLetterDescKey);

            bossReleased = true;
            FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.30f);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.55f);
            FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 2);
        }

        private IntVec3 FindReleaseCell()
        {
            if (IsValidReleaseCell(Position))
            {
                return Position;
            }

            for (int i = 0; i < GenRadial.NumCellsInRadius(2.9f); i++)
            {
                IntVec3 candidate = Position + GenRadial.RadialPattern[i];
                if (IsValidReleaseCell(candidate))
                {
                    return candidate;
                }
            }

            return Position;
        }

        private bool IsValidReleaseCell(IntVec3 cell)
        {
            return cell.IsValid && cell.InBounds(Map) && cell.Standable(Map) && !cell.Fogged(Map);
        }

        private void BeginLaunch()
        {
            if (launching || Map == null || Destroyed)
            {
                return;
            }

            launching = true;
            launchTicks = 0;

            FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.40f);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.55f);
            FleckMaker.ThrowMicroSparks(DrawPos, Map);
        }

        private void TickLaunching()
        {
            launchTicks++;

            if (Map != null)
            {
                float progress = Mathf.Clamp01(launchTicks / (float)LaunchDurationTicks);
                Vector3 bodyPos = GetCurrentBodyDrawPos();
                Vector3 exhaustCenter = bodyPos + new Vector3(0f, 0f, -4.40f);
                Vector3 exhaustLeft = exhaustCenter + new Vector3(-3.10f, 0f, -0.45f);
                Vector3 exhaustRight = exhaustCenter + new Vector3(3.10f, 0f, -0.45f);
                float fireGlowSize = 1.55f + progress * 1.55f;
                float smokeSize = 1.10f + progress * 1.30f;
                float dustSize = 1.40f + progress * 1.35f;
                float lightningGlow = 1.25f + progress * 1.65f;

                if (launchTicks % 2 == 0)
                {
                    FleckMaker.ThrowMicroSparks(exhaustLeft, Map);
                    FleckMaker.ThrowMicroSparks(exhaustRight, Map);
                    FleckMaker.ThrowFireGlow(exhaustLeft, Map, fireGlowSize);
                    FleckMaker.ThrowFireGlow(exhaustRight, Map, fireGlowSize);
                }

                if (launchTicks % 3 == 0)
                {
                    FleckMaker.ThrowSmoke(exhaustLeft, Map, smokeSize);
                    FleckMaker.ThrowSmoke(exhaustRight, Map, smokeSize);
                }

                if (launchTicks % 4 == 0)
                {
                    Vector3 groundFxCenter = DrawPos + new Vector3(0f, 0f, -2.80f);
                    FleckMaker.ThrowDustPuff(groundFxCenter + new Vector3(-2.20f, 0f, 0f), Map, dustSize);
                    FleckMaker.ThrowDustPuff(groundFxCenter + new Vector3(2.20f, 0f, 0f), Map, dustSize);
                    FleckMaker.ThrowHeatGlow(Position, Map, 1.35f + progress * 1.10f);
                }

                if (launchTicks % 6 == 0)
                {
                    FleckMaker.ThrowLightningGlow(exhaustCenter, Map, lightningGlow);
                }

                if (launchTicks % 10 == 0)
                {
                    FleckMaker.ThrowLightningGlow(bodyPos, Map, 1.65f + progress * 1.80f);
                }
            }

            if (launchTicks >= LaunchDurationTicks)
            {
                Destroy(DestroyMode.Vanish);
            }
        }
    }
}
