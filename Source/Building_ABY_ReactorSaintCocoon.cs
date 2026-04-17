using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public sealed class Building_ABY_ReactorSaintCocoon : Building
    {
        private const string BossPawnKindDefName = "ABY_ReactorSaint";
        private const string BossLabel = "Infernal Reactor Saint";
        private const string ArrivalSoundDefName = "ABY_ReactorSaintCharge";
        private const string CompletionLetterLabelKey = "ABY_ReactorSaintSummonSuccessLabel";
        private const string CompletionLetterDescKey = "ABY_ReactorSaintSummonSuccessDesc";
        private const string CocoonPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon";
        private const string CocoonShadowPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon_Shadow";
        private const int ReleaseDelayTicks = 834;
        private const int PostReleaseTicks = 417;
        private const int LaunchDurationTicks = 90;
        private const float LaunchRise = 7.2f;
        private const float LaunchForwardDrift = 1.85f;
        private const float LaunchEndScaleMultiplier = 0.22f;
        private const float ImpactExplosionRadius = 3.9f;
        private const int ImpactExplosionDamage = 28;
        private const float ImpactExplosionArmorPenetration = 0.18f;

        private static readonly Material CocoonMaterial = MaterialPool.MatFrom(CocoonPath, ShaderDatabase.Cutout);
        private static readonly Material CocoonShadowMaterial = MaterialPool.MatFrom(CocoonShadowPath, ShaderDatabase.TransparentPostLight);

        private int ticksSinceImpact;
        private bool bossReleased;
        private bool releaseFailedPermanently;
        private bool impactProcessed;
        private bool launchInProgress;
        private int launchTicks;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceImpact, "ticksSinceImpact", 0);
            Scribe_Values.Look(ref bossReleased, "bossReleased", false);
            Scribe_Values.Look(ref releaseFailedPermanently, "releaseFailedPermanently", false);
            Scribe_Values.Look(ref impactProcessed, "impactProcessed", false);
            Scribe_Values.Look(ref launchInProgress, "launchInProgress", false);
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

            if (launchInProgress)
            {
                TickLaunchSequence();
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
                BeginLaunchAway();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!launchInProgress)
            {
                base.DrawAt(drawLoc, flip);
                return;
            }

            DrawLaunchingCocoon(drawLoc);
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

        private void BeginLaunchAway()
        {
            if (launchInProgress || Map == null || Destroyed)
            {
                return;
            }

            launchInProgress = true;
            launchTicks = 0;

            FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.20f);
            FleckMaker.ThrowMicroSparks(DrawPos, Map);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.35f);
            FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
        }

        private void TickLaunchSequence()
        {
            launchTicks++;

            if (launchTicks % 5 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (launchTicks % 12 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.45f);
            }

            if (launchTicks >= LaunchDurationTicks)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.85f);
                FleckMaker.ThrowHeatGlow(Position, Map, 1.15f);
                Destroy(DestroyMode.Vanish);
            }
        }

        private void DrawLaunchingCocoon(Vector3 drawLoc)
        {
            Vector2 baseSize = def?.graphicData?.drawSize ?? new Vector2(15.95f, 23.10f);
            float progress = Mathf.Clamp01(launchTicks / (float)Mathf.Max(1, LaunchDurationTicks));
            float eased = progress * progress;
            float scaleMultiplier = Mathf.Lerp(1f, LaunchEndScaleMultiplier, eased);

            float driftX = Mathf.Sin((Find.TickManager.TicksGame + thingIDNumber) * 0.07f) * 0.22f * (1f - progress);
            Vector3 bodyLoc = drawLoc;
            bodyLoc.x += driftX;
            bodyLoc.z += Mathf.Lerp(0f, LaunchForwardDrift, eased);
            bodyLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + Mathf.Lerp(0f, LaunchRise, eased);

            Vector3 bodyScale = new Vector3(baseSize.x * scaleMultiplier, 1f, baseSize.y * scaleMultiplier);
            Matrix4x4 bodyMatrix = Matrix4x4.TRS(bodyLoc, Quaternion.identity, bodyScale);
            Graphics.DrawMesh(MeshPool.plane10, bodyMatrix, CocoonMaterial, 0);

            float shadowAlpha = 1f - progress;
            if (shadowAlpha > 0.01f)
            {
                Vector3 shadowLoc = drawLoc;
                shadowLoc.y = AltitudeLayer.Shadows.AltitudeFor();
                float shadowScaleMultiplier = Mathf.Lerp(1f, 0.55f, eased);
                Vector3 shadowScale = new Vector3(baseSize.x * shadowScaleMultiplier, 1f, baseSize.y * shadowScaleMultiplier);
                Matrix4x4 shadowMatrix = Matrix4x4.TRS(shadowLoc, Quaternion.identity, shadowScale);
                Graphics.DrawMesh(MeshPool.plane10, shadowMatrix, FadedMaterialPool.FadedVersionOf(CocoonShadowMaterial, shadowAlpha), 0);
            }
        }
    }
}
