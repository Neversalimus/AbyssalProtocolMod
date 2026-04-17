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
        private const string CocoonPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon";
        private const string ShadowPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon_Shadow";
        private const int ReleaseDelayTicks = 834;
        private const int PostReleaseTicks = 417;
        private const int LaunchDurationTicks = 84;
        private const float LaunchVerticalOffset = 0.16f;
        private const float LaunchForwardDrift = 6.40f;
        private const float LaunchSideDrift = 0.70f;
        private const float LaunchEndScale = 0.58f;
        private const float ImpactExplosionRadius = 3.9f;
        private const int ImpactExplosionDamage = 28;
        private const float ImpactExplosionArmorPenetration = 0.18f;
        private static readonly Vector3 BodyScale = new Vector3(15.95f, 1f, 23.10f);
        private static readonly Vector3 ShadowScale = new Vector3(23.10f, 1f, 23.10f);

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
            float bodyScale = 1f;
            float shadowAlpha = 0.42f;
            float bodyAlpha = 1f;

            if (launching)
            {
                float progress = Mathf.Clamp01(launchTicks / (float)LaunchDurationTicks);
                bodyLoc.y += 0.036f + progress * LaunchVerticalOffset;
                bodyLoc.z += progress * LaunchForwardDrift;
                bodyLoc.x += progress * LaunchSideDrift;
                shadowLoc.y += 0.005f;
                shadowLoc.z += progress * (LaunchForwardDrift * 0.35f);
                shadowLoc.x += progress * (LaunchSideDrift * 0.35f);
                bodyScale = Mathf.Lerp(1f, LaunchEndScale, progress);
                shadowAlpha = Mathf.Lerp(0.42f, 0.03f, progress);
                bodyAlpha = Mathf.Lerp(1f, 0.92f, progress);
            }
            else
            {
                bodyLoc.y += 0.036f;
                shadowLoc.y += 0.005f;
            }

            DrawQuad(ShadowPath, shadowLoc, ShadowScale, 0f, new Color(1f, 1f, 1f, shadowAlpha), ShaderDatabase.Transparent, 1f);
            DrawQuad(CocoonPath, bodyLoc, BodyScale, 0f, new Color(1f, 1f, 1f, bodyAlpha), ShaderDatabase.Cutout, bodyScale);

            if (launching && Map != null)
            {
                float progress = Mathf.Clamp01(launchTicks / (float)LaunchDurationTicks);
                if (launchTicks % 5 == 0)
                {
                    FleckMaker.ThrowMicroSparks(bodyLoc, Map);
                }

                if (launchTicks % 10 == 0)
                {
                    FleckMaker.ThrowLightningGlow(bodyLoc, Map, 1.2f + progress * 1.6f);
                }
            }
        }

        private static void DrawQuad(string texPath, Vector3 loc, Vector3 baseScale, float angle, Color color, Shader shader, float scaleMultiplier)
        {
            if (ContentFinder<Texture2D>.Get(texPath, false) == null)
            {
                return;
            }

            Material material = MaterialPool.MatFrom(texPath, shader, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            Vector3 scale = new Vector3(baseScale.x * scaleMultiplier, 1f, baseScale.z * scaleMultiplier);
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
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
