using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class Building_ABY_ReactorSaintCocoon : Building_ABY_SkyfallerVesselBase
    {
        private const string BossPawnKindDefName = "ABY_ReactorSaint";
        private const string BossLabel = "Infernal Reactor Saint";
        private const string ArrivalSoundDefName = "ABY_ReactorSaintCharge";
        private const string CompletionLetterLabelKey = "ABY_ReactorSaintSummonSuccessLabel";
        private const string CompletionLetterDescKey = "ABY_ReactorSaintSummonSuccessDesc";
        private const string CocoonTexPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon";
        private const string CocoonShadowTexPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon_Shadow";

        protected override int ReleaseDelayTicks => 834;
        protected override int PostReleaseTicks => 417;
        protected override int LaunchDurationTicks => 84;

        protected override float ImpactExplosionRadius => 3.9f;
        protected override int ImpactExplosionDamage => 28;
        protected override float ImpactExplosionArmorPenetration => 0.18f;

        protected override string BodyTexPath => CocoonTexPath;
        protected override string ShadowTexPath => CocoonShadowTexPath;
        protected override float BodyScaleX => 15.95f;
        protected override float BodyScaleZ => 23.10f;
        protected override float ShadowScale => 23.10f;
        protected override float ShadowAlpha => 0.62f;

        private const float DepartureDriftX = 0.30f;
        private const float DepartureDriftZ = 58.00f;

        protected override float LaunchDriftX => DepartureDriftX;
        protected override float LaunchDriftZ => DepartureDriftZ;
        protected override float LaunchAltitudeBoost => 4.20f;
        protected override float LaunchBodyScaleEnd => 0.98f;
        protected override float LaunchShadowScaleEnd => 0.42f;

        protected override void TickDormantVessel()
        {
            if (TicksSinceImpact % 11 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (TicksSinceImpact % 30 == 0)
            {
                FleckMaker.ThrowHeatGlow(Position, Map, 1.20f);
            }

            if (TicksSinceImpact % 60 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.65f);
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
            }
        }

        protected override void TickSpentVessel()
        {
            if (TicksSinceImpact % 24 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (TicksSinceImpact % 52 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.10f);
            }
        }

        protected override bool TryReleasePayload(out bool permanentFailure)
        {
            permanentFailure = false;

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(BossPawnKindDefName);
            Faction faction = AbyssalBossSummonUtility.ResolveHostileFaction();

            if (kindDef == null || faction == null)
            {
                permanentFailure = true;
                Log.Warning("[AbyssalProtocol] Reactor Saint cocoon could not resolve boss kind or hostile faction.");
                return false;
            }

            if (!AbyssalBossSummonUtility.TryGenerateBoss(Map, kindDef, faction, BossLabel, out Pawn pawn, out string failReason))
            {
                if (TicksSinceImpact % 60 == 0 && !failReason.NullOrEmpty())
                {
                    Log.Warning("[AbyssalProtocol] Reactor Saint cocoon failed to generate boss: " + failReason);
                }

                return false;
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

            IntVec3 escortPortalCell = releaseCell;
            if (!AbyssalBossSummonUtility.TryFindEscortPortalCellNear(Map, releaseCell, out escortPortalCell))
            {
                escortPortalCell = releaseCell;
            }

            if (!AbyssalBossOrchestrationUtility.TrySpawnEscortPackThroughPortal(
                    Map,
                    faction,
                    "reactor_saint",
                    BossPawnKindDefName,
                    escortPortalCell,
                    980f,
                    BossLabel,
                    out string escortFailReason))
            {
                if (!AbyssalBossOrchestrationUtility.TrySpawnEscortPackNearBoss(
                        Map,
                        faction,
                        "reactor_saint",
                        pawn,
                        980f,
                        BossLabel,
                        out escortFailReason) && !escortFailReason.NullOrEmpty())
                {
                    Log.Warning("[AbyssalProtocol] Reactor Saint cocoon escort spawn failed: " + escortFailReason);
                }
            }

            FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.30f);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.55f);
            FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 2);
            return true;
        }

        protected override void OnBeginLaunch()
        {
            FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.40f);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.55f);
            FleckMaker.ThrowMicroSparks(DrawPos, Map);
        }

        protected override void TickLaunchFx()
        {
            if (Map == null)
            {
                return;
            }

            float progress = LaunchProgress;
            Vector3 bodyPos = GetCurrentBodyDrawPos();
            Vector3 exhaustCenter = bodyPos + new Vector3(0f, 0f, -4.40f);
            Vector3 exhaustLeft = exhaustCenter + new Vector3(-3.10f, 0f, -0.45f);
            Vector3 exhaustRight = exhaustCenter + new Vector3(3.10f, 0f, -0.45f);
            float fireGlowSize = 1.55f + progress * 1.55f;
            float smokeSize = 1.10f + progress * 1.30f;
            float dustSize = 1.40f + progress * 1.35f;
            float lightningGlow = 1.25f + progress * 1.65f;
            int launchTicks = Mathf.RoundToInt(progress * LaunchDurationTicks);

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
    }
}
