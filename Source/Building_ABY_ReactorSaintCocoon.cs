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
        private const int ReleaseDelayTicks = 834;
        private const int PostReleaseTicks = 417;
        private const int LaunchDurationTicks = 72;
        private const float LaunchVerticalOffset = 8.50f;
        private const float LaunchForwardDrift = 1.35f;
        private const float LaunchSideDrift = 0.18f;
        private const float ImpactExplosionRadius = 3.9f;
        private const int ImpactExplosionDamage = 28;
        private const float ImpactExplosionArmorPenetration = 0.18f;

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

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!launching)
            {
                base.DrawAt(drawLoc, flip);
                return;
            }

            float progress = Mathf.Clamp01(launchTicks / (float)LaunchDurationTicks);
            Vector3 liftedLoc = drawLoc;
            liftedLoc.y += 0.10f + progress * LaunchVerticalOffset;
            liftedLoc.z += progress * LaunchForwardDrift;
            liftedLoc.x += progress * LaunchSideDrift;
            base.DrawAt(liftedLoc, flip);
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
                if (launchTicks % 4 == 0)
                {
                    FleckMaker.ThrowMicroSparks(DrawPos, Map);
                }

                if (launchTicks % 9 == 0)
                {
                    float glowSize = 1.20f + 1.20f * (launchTicks / (float)LaunchDurationTicks);
                    FleckMaker.ThrowLightningGlow(DrawPos, Map, glowSize);
                }
            }

            if (launchTicks >= LaunchDurationTicks)
            {
                Destroy(DestroyMode.Vanish);
            }
        }
    }
}
