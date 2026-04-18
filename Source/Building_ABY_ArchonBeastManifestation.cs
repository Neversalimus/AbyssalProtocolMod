using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public sealed class Building_ABY_ArchonBeastManifestation : Building_ABY_HostileManifestationBase
    {
        private const string SeamRiftPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Rift";
        private const string SeamShadowPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Shadow";
        private const string SeamSparksPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Sparks";
        private const string HaloCorePath = "Things/VFX/RuptureHalo/RuptureHaloCore";
        private const string HaloRingPath = "Things/VFX/RuptureHalo/RuptureHaloRing";
        private const string NoisePath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Noise";
        private const string CrackPath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Crack";
        private const string SilhouettePath = "Things/VFX/ArchonArrival/ABY_ArchonArrival_Silhouette";
        private const string SilhouetteShadowPath = "Things/VFX/ArchonArrival/ABY_ArchonArrival_Shadow";

        private const int DefaultReleaseDelayTicks = 132;
        private const int DefaultPostReleaseTicks = 56;
        private const int CompanionPortalStartTick = 54;
        private const int CompanionPortalWarmupOffsetTicks = 22;
        private const int CompanionPortalStaggerTicks = 26;
        private const int ReleaseWarmupLeadTicks = 18;

        private PawnKindDef bossKindDef;
        private IntVec3 bossArrivalCell = IntVec3.Invalid;
        private string bossLabel;
        private string arrivalSoundDefName;
        private string completionLetterLabelKey;
        private string completionLetterDescKey;
        private int bossReleaseDelayTicks = DefaultReleaseDelayTicks;
        private int postReleaseTicks = DefaultPostReleaseTicks;
        private bool bossReleased;
        private bool companionPortalsSpawned;
        private bool releaseWarmupTriggered;

        protected override bool CreateAshOnComplete => false;

        public void Initialize(
            PawnKindDef kindDef,
            Faction faction,
            int warmup,
            IntVec3 arrivalCell,
            string bossLabel,
            string arrivalSoundDefName,
            string completionLetterLabelKey,
            string completionLetterDescKey)
        {
            int resolvedReleaseDelay = Mathf.Max(60, DefaultReleaseDelayTicks);
            int resolvedPostRelease = Mathf.Max(30, DefaultPostReleaseTicks);
            int totalWarmup = Mathf.Max(Mathf.Max(90, warmup), resolvedReleaseDelay + resolvedPostRelease);

            base.Initialize(faction, null, totalWarmup, null, null, null);
            bossKindDef = kindDef;
            bossArrivalCell = arrivalCell;
            this.bossLabel = bossLabel;
            this.arrivalSoundDefName = arrivalSoundDefName;
            this.completionLetterLabelKey = completionLetterLabelKey;
            this.completionLetterDescKey = completionLetterDescKey;
            bossReleaseDelayTicks = resolvedReleaseDelay;
            postReleaseTicks = resolvedPostRelease;
            bossReleased = false;
            companionPortalsSpawned = false;
            releaseWarmupTriggered = false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref bossKindDef, "bossKindDef");
            Scribe_Values.Look(ref bossArrivalCell, "bossArrivalCell");
            Scribe_Values.Look(ref bossLabel, "bossLabel");
            Scribe_Values.Look(ref arrivalSoundDefName, "arrivalSoundDefName");
            Scribe_Values.Look(ref completionLetterLabelKey, "completionLetterLabelKey");
            Scribe_Values.Look(ref completionLetterDescKey, "completionLetterDescKey");
            Scribe_Values.Look(ref bossReleaseDelayTicks, "bossReleaseDelayTicks", DefaultReleaseDelayTicks);
            Scribe_Values.Look(ref postReleaseTicks, "postReleaseTicks", DefaultPostReleaseTicks);
            Scribe_Values.Look(ref bossReleased, "bossReleased", false);
            Scribe_Values.Look(ref companionPortalsSpawned, "companionPortalsSpawned", false);
            Scribe_Values.Look(ref releaseWarmupTriggered, "releaseWarmupTriggered", false);
        }

        protected override IntVec3 GetSpawnRootCell()
        {
            return bossArrivalCell.IsValid ? bossArrivalCell : base.GetSpawnRootCell();
        }

        protected override void TickManifestation()
        {
            if (Map == null || !Position.IsValid)
            {
                return;
            }

            if (!companionPortalsSpawned && ticksActive >= CompanionPortalStartTick)
            {
                AbyssalArchonBeastPortalEncounterUtility.TrySpawnCompanionHoundPortals(
                    Map,
                    manifestationFaction,
                    GetSpawnRootCell(),
                    CompanionPortalWarmupOffsetTicks,
                    -1,
                    CompanionPortalStaggerTicks);
                companionPortalsSpawned = true;
            }

            if (!bossReleased && !releaseWarmupTriggered && ticksActive >= bossReleaseDelayTicks - ReleaseWarmupLeadTicks)
            {
                releaseWarmupTriggered = true;
                DoReleaseWarmup();
            }

            if (!bossReleased && ticksActive >= bossReleaseDelayTicks)
            {
                ReleaseBoss();
            }

            if (ticksActive % 18 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (ticksActive % 28 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, bossReleased ? 1.18f : 1.30f + Progress * 0.85f);
            }

            if (!bossReleased)
            {
                if (ticksActive % 22 == 0)
                {
                    FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
                }

                if (ticksActive % 34 == 0)
                {
                    Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.10f + Progress * 0.12f);
                }

                return;
            }

            if (ticksActive % 36 == 0)
            {
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.08f + (1f - GetPostReleaseProgress()) * 0.10f);
            }
        }

        protected override void OnManifestationCompleted()
        {
            if (!bossReleased)
            {
                ReleaseBoss();
            }

            if (Map == null)
            {
                return;
            }

            FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.85f);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.10f);
            ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", Position, Map);
            FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 2);
        }

        protected override void DrawManifestation(Vector3 drawLoc)
        {
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float progress = Progress;
            float preReleaseProgress = Mathf.Clamp01(ticksActive / (float)Mathf.Max(1, bossReleaseDelayTicks));
            float postReleaseProgress = GetPostReleaseProgress();
            float silhouetteProgress = Mathf.Clamp01((preReleaseProgress - 0.28f) / 0.50f);
            float fadeOut = bossReleased ? 1f - postReleaseProgress : 1f;

            float seamPulse = Pulse(0.10f, 0.7f);
            float haloPulse = Pulse(0.07f, 1.8f);
            float crackPulse = Pulse(0.15f, 2.4f);
            float noisePulse = Pulse(0.18f, 0.9f);

            Vector3 groundLoc = drawLoc;
            groundLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.028f;

            float seamAngle = 90f + Mathf.Sin((ticks + seed) * 0.036f) * 6f;
            float shadowScaleX = Mathf.Lerp(0.55f, 1.60f, preReleaseProgress) * (0.92f + seamPulse * 0.08f);
            float shadowScaleZ = Mathf.Lerp(1.20f, 4.05f, preReleaseProgress) * (0.90f + seamPulse * 0.10f);
            float riftScaleX = Mathf.Lerp(0.18f, 0.56f, preReleaseProgress) * (0.94f + seamPulse * 0.14f);
            float riftScaleZ = Mathf.Lerp(0.78f, 3.55f, preReleaseProgress) * (0.90f + seamPulse * 0.18f);
            float sparkScaleX = riftScaleX * 1.85f;
            float sparkScaleZ = riftScaleZ * 1.05f;

            float haloScale = Mathf.Lerp(0.42f, 2.30f, progress) * (0.92f + haloPulse * 0.16f);
            float ringScale = Mathf.Lerp(0.64f, 2.65f, progress) * (0.94f + haloPulse * 0.12f);
            float crackScale = Mathf.Lerp(0.18f, 1.22f, progress) * (0.92f + crackPulse * 0.16f);
            float noiseScale = Mathf.Lerp(0.34f, 1.32f, progress) * (0.92f + noisePulse * 0.16f);

            float alpha = Mathf.Lerp(0.18f, 1f, preReleaseProgress) * fadeOut;
            DrawPlane(SeamShadowPath, groundLoc, shadowScaleX, shadowScaleZ, seamAngle, new Color(0.05f, 0.01f, 0.01f, alpha * 0.82f));
            DrawPlane(SeamRiftPath, groundLoc + new Vector3(0f, 0.006f, 0f), riftScaleX, riftScaleZ, seamAngle, new Color(1f, 0.18f, 0.12f, alpha * 0.95f));
            DrawPlane(SeamRiftPath, groundLoc + new Vector3(0f, 0.010f, 0f), riftScaleX * 0.54f, riftScaleZ * 0.84f, seamAngle, new Color(1f, 0.86f, 0.26f, alpha * 0.42f));
            DrawPlane(SeamSparksPath, groundLoc + new Vector3(0f, 0.012f, 0f), sparkScaleX, sparkScaleZ, seamAngle + Mathf.Sin((ticks + seed) * 0.11f) * 4f, new Color(1f, 0.76f, 0.34f, alpha * (0.24f + crackPulse * 0.22f)));
            DrawPlane(HaloCorePath, groundLoc + new Vector3(0f, 0.014f, 0f), haloScale, seamAngle * 0.15f, new Color(1f, 0.34f, 0.18f, alpha * 0.68f));
            DrawPlane(HaloRingPath, groundLoc + new Vector3(0f, 0.016f, 0f), ringScale, (ticks + seed) * 1.05f, new Color(0.98f, 0.14f, 0.10f, alpha * 0.72f));
            DrawPlane(HaloRingPath, groundLoc + new Vector3(0f, 0.018f, 0f), ringScale * 0.80f, -(ticks + seed) * 0.76f, new Color(1f, 0.88f, 0.42f, alpha * 0.26f));
            DrawPlane(NoisePath, groundLoc + new Vector3(0f, 0.020f, 0f), noiseScale, 1.10f + Mathf.Sin((ticks + seed) * 0.17f) * 0.04f, seamAngle + Mathf.Sin((ticks + seed) * 0.05f) * 9f, new Color(1f, 0.82f, 0.72f, alpha * 0.16f));
            DrawPlane(CrackPath, groundLoc + new Vector3(0f, 0.022f, 0f), crackScale, crackScale * 1.25f, Mathf.Sin((ticks + seed) * 0.07f) * 8f, new Color(1f, 0.42f, 0.16f, alpha * 0.26f));

            float silhouetteAlpha = Mathf.Clamp01(silhouetteProgress) * Mathf.Clamp01(fadeOut * 1.08f);
            if (silhouetteAlpha <= 0.001f)
            {
                return;
            }

            Vector3 silhouetteLoc = drawLoc;
            silhouetteLoc.y = AltitudeLayer.Building.AltitudeFor() + 0.082f;
            silhouetteLoc.z += Mathf.Lerp(0.48f, 0f, silhouetteProgress);
            float jitter = bossReleased ? 0.004f : (1f - silhouetteProgress) * 0.018f;
            silhouetteLoc.x += Mathf.Sin((ticks + seed) * 0.12f) * jitter;
            silhouetteLoc.z += Mathf.Cos((ticks + seed) * 0.09f) * jitter;

            float silhouetteScaleX = Mathf.Lerp(3.55f, 5.05f, silhouetteProgress);
            float silhouetteScaleZ = Mathf.Lerp(3.35f, 4.90f, silhouetteProgress);
            float silhouetteAngle = Mathf.Sin((ticks + seed) * 0.03f) * (bossReleased ? 1.4f : 2.4f);

            DrawTransparentPlane(SilhouetteShadowPath, silhouetteLoc + new Vector3(0f, -0.004f, 0.08f), silhouetteScaleX * 1.12f, silhouetteScaleZ * 0.98f, 0f, new Color(0f, 0f, 0f, silhouetteAlpha * 0.34f));
            DrawCutoutPlane(SilhouettePath, silhouetteLoc, silhouetteScaleX, silhouetteScaleZ, silhouetteAngle, new Color(1f, 1f, 1f, silhouetteAlpha));
            DrawPlane(HaloCorePath, silhouetteLoc + new Vector3(0f, -0.006f, 0f), silhouetteScaleX * 0.58f, silhouetteAngle * 0.2f, new Color(1f, 0.22f, 0.12f, silhouetteAlpha * 0.22f));
        }

        private void ReleaseBoss()
        {
            if (bossReleased || Map == null || manifestationFaction == null || bossKindDef == null)
            {
                return;
            }

            IntVec3 spawnCell = bossArrivalCell.IsValid ? bossArrivalCell : Position;
            if (!spawnCell.IsValid || !spawnCell.InBounds(Map) || !spawnCell.Standable(Map))
            {
                spawnCell = Position;
            }

            if (!AbyssalBossSummonUtility.TryGenerateBoss(
                    Map,
                    bossKindDef,
                    manifestationFaction,
                    bossLabel,
                    out Pawn pawn,
                    out string failReason))
            {
                if (!failReason.NullOrEmpty())
                {
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                }

                return;
            }

            bossReleased = true;
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.36f);
            FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.45f);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.42f);
            FleckMaker.ThrowMicroSparks(DrawPos, Map);
            FleckMaker.ThrowMicroSparks(DrawPos, Map);
            FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 3);

            AbyssalBossSummonUtility.FinalizeBossArrival(
                pawn,
                manifestationFaction,
                Map,
                spawnCell,
                bossLabel,
                arrivalSoundDefName.NullOrEmpty() ? "ABY_RuptureArrive" : arrivalSoundDefName,
                completionLetterLabelKey,
                completionLetterDescKey);
        }

        private void DoReleaseWarmup()
        {
            if (Map == null)
            {
                return;
            }

            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.22f);
            FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.92f);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.02f);
            ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", Position, Map);
        }

        private float GetPostReleaseProgress()
        {
            if (!bossReleased)
            {
                return 0f;
            }

            return Mathf.Clamp01((ticksActive - bossReleaseDelayTicks) / (float)Mathf.Max(1, postReleaseTicks));
        }

        private static void DrawCutoutPlane(string texPath, Vector3 loc, float scaleX, float scaleZ, float angle, Color color)
        {
            if (string.IsNullOrEmpty(texPath))
            {
                return;
            }

            Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.Cutout, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(scaleX, 1f, scaleZ));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private static void DrawTransparentPlane(string texPath, Vector3 loc, float scaleX, float scaleZ, float angle, Color color)
        {
            if (string.IsNullOrEmpty(texPath))
            {
                return;
            }

            Material material = MaterialPool.MatFrom(texPath, ShaderDatabase.TransparentPostLight, color);
            Matrix4x4 matrix = Matrix4x4.identity;
            matrix.SetTRS(loc, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(scaleX, 1f, scaleZ));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
