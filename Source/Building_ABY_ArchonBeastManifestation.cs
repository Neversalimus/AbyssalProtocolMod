using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public sealed class Building_ABY_ArchonBeastManifestation : Building_ABY_HostileManifestationBase
    {
        private const string RiftPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Rift";
        private const string SparksPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Sparks";
        private const string ShadowPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Shadow";
        private const string GlowPath = "Things/VFX/RupturePortal/ABY_RupturePortal_Glow";
        private const string RingPath = "Things/VFX/RupturePortal/ABY_RupturePortal_Ring";
        private const string SilhouettePath = "Things/VFX/ArchonArrival/ABY_ArchonArrival_Silhouette";
        private const string SilhouetteShadowPath = "Things/VFX/ArchonArrival/ABY_ArchonArrival_Shadow";

        private const string WarmupTrailMoteDefName = "ABY_Mote_ArchonDashTrail";
        private const string WarmupEntryMoteDefName = "ABY_Mote_ArchonDashEntry";
        private const string WarmupExitMoteDefName = "ABY_Mote_ArchonDashExit";

        private const int DefaultReleaseDelayTicks = 174;
        private const int DefaultPostReleaseTicks = 62;
        private const int CompanionPortalTriggerTick = 72;
        private const int LatePulseLeadTicks = 34;
        private const int LordReleaseLeadTicks = 18;

        private PawnKindDef bossKindDef;
        private IntVec3 bossArrivalCell = IntVec3.Invalid;
        private string bossLabel;
        private string arrivalSoundDefName;
        private string completionLetterLabelKey;
        private string completionLetterDescKey;
        private int bossReleaseDelayTicks = DefaultReleaseDelayTicks;
        private int postReleaseTicks = DefaultPostReleaseTicks;
        private Rot4 seamSide = Rot4.South;
        private bool entryPulseTriggered;
        private bool companionPortalsTriggered;
        private bool latePulseTriggered;
        private bool bossReleased;
        private bool lordReleased;
        private Pawn releasedBoss;

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
            int resolvedReleaseDelay = Mathf.Max(90, warmup > 0 ? warmup : DefaultReleaseDelayTicks);
            int resolvedPostRelease = Mathf.Max(45, DefaultPostReleaseTicks);
            int totalWarmup = resolvedReleaseDelay + resolvedPostRelease;

            base.Initialize(faction, null, totalWarmup, null, null, null);
            bossKindDef = kindDef;
            bossArrivalCell = arrivalCell;
            this.bossLabel = bossLabel.NullOrEmpty() ? "Archon Beast" : bossLabel;
            this.arrivalSoundDefName = arrivalSoundDefName;
            this.completionLetterLabelKey = completionLetterLabelKey;
            this.completionLetterDescKey = completionLetterDescKey;
            bossReleaseDelayTicks = resolvedReleaseDelay;
            postReleaseTicks = resolvedPostRelease;
            seamSide = ResolveSeamSide();
            entryPulseTriggered = false;
            companionPortalsTriggered = false;
            latePulseTriggered = false;
            bossReleased = false;
            lordReleased = false;
            releasedBoss = null;
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
            Scribe_Values.Look(ref seamSide, "seamSide", Rot4.South);
            Scribe_Values.Look(ref entryPulseTriggered, "entryPulseTriggered", false);
            Scribe_Values.Look(ref companionPortalsTriggered, "companionPortalsTriggered", false);
            Scribe_Values.Look(ref latePulseTriggered, "latePulseTriggered", false);
            Scribe_Values.Look(ref bossReleased, "bossReleased", false);
            Scribe_Values.Look(ref lordReleased, "lordReleased", false);
            Scribe_References.Look(ref releasedBoss, "releasedBoss");
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

            if (!entryPulseTriggered && ticksActive >= 18)
            {
                entryPulseTriggered = true;
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.12f);
                ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", Position, Map);
                TrySpawnManifestMote(WarmupTrailMoteDefName, 1.18f);
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.40f);
            }

            if (!companionPortalsTriggered && ticksActive >= CompanionPortalTriggerTick)
            {
                companionPortalsTriggered = true;
                AbyssalArchonBeastPortalEncounterUtility.TrySpawnCompanionHoundPortals(Map, manifestationFaction, Position);
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.18f);
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", Position, Map);
                MakeAshScar(2, 2.4f);
            }

            if (!latePulseTriggered && ticksActive >= Mathf.Max(60, bossReleaseDelayTicks - LatePulseLeadTicks))
            {
                latePulseTriggered = true;
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.28f);
                ABY_SoundUtility.PlayAt("ABY_RuptureImpact", Position, Map);
                TrySpawnManifestMote(WarmupEntryMoteDefName, 1.34f);
                TrySpawnManifestMote(WarmupExitMoteDefName, 1.08f);
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.05f);
                MakeAshScar(3, 2.8f);
            }

            if (!bossReleased && ticksActive >= bossReleaseDelayTicks)
            {
                ReleaseBoss();
            }

            if (!lordReleased && bossReleased && ticksActive >= Mathf.Max(bossReleaseDelayTicks + 1, warmupTicks - LordReleaseLeadTicks))
            {
                ReleaseBossToCombat();
            }

            if (releasedBoss != null && (!releasedBoss.Spawned || releasedBoss.Destroyed || releasedBoss.MapHeld != Map))
            {
                releasedBoss = null;
            }

            if (releasedBoss != null && !lordReleased)
            {
                Pawn nearestThreat = AbyssalThreatPawnUtility.FindClosestThreatWithin(releasedBoss, 36f);
                if (nearestThreat != null)
                {
                    releasedBoss.rotationTracker?.FaceCell(nearestThreat.Position);
                }

                releasedBoss.pather?.StopDead();
            }

            if (ticksActive % 16 == 0)
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, bossReleased ? 1 : 2);
            }

            if (!bossReleased)
            {
                if (ticksActive % 8 == 0)
                {
                    FleckMaker.ThrowMicroSparks(DrawPos, Map);
                }

                if (ticksActive % 22 == 0)
                {
                    FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.10f + Progress * 1.20f);
                }

                if (ticksActive % 30 == 0)
                {
                    FleckMaker.ThrowHeatGlow(Position, Map, 0.85f + Progress * 0.80f);
                }

                return;
            }

            if (ticksActive % 14 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (ticksActive % 32 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 0.95f + GetPostReleaseProgress() * 0.65f);
            }
        }

        protected override void OnManifestationCompleted()
        {
            if (!bossReleased)
            {
                ReleaseBoss();
            }

            ReleaseBossToCombat();
            MakeAshScar(3, 2.8f);
            FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.70f);
        }

        protected override void DrawManifestation(Vector3 drawLoc)
        {
            int ticks = Find.TickManager.TicksGame + seed;
            float preReleaseProgress = Mathf.Clamp01(ticksActive / (float)Mathf.Max(1, bossReleaseDelayTicks));
            float postReleaseProgress = GetPostReleaseProgress();
            float collapse = bossReleased ? postReleaseProgress : 0f;
            float breachPulse = Pulse(0.10f, 0.7f);
            float ringPulse = Pulse(0.07f, 1.5f);
            float emberPulse = Pulse(0.14f, 2.4f);
            float silhouetteReveal = Mathf.Clamp01(Mathf.InverseLerp(0.42f, 0.98f, preReleaseProgress));
            float silhouetteFade = bossReleased ? Mathf.Clamp01(1f - postReleaseProgress * 1.15f) : 1f;

            IntVec3 seamVectorCell = seamSide.FacingCell;
            Vector3 seamOffset = new Vector3(seamVectorCell.x * 0.15f, 0f, seamVectorCell.z * 0.15f);

            Vector3 groundLoc = drawLoc + seamOffset;
            groundLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.028f;

            float alpha = Mathf.Lerp(0.28f, 1f, preReleaseProgress) * (1f - collapse * 0.72f);
            float shadowScaleX = Mathf.Lerp(0.70f, 1.48f, preReleaseProgress) * (0.94f + breachPulse * 0.08f);
            float shadowScaleZ = Mathf.Lerp(1.45f, 3.70f, preReleaseProgress) * (0.92f + breachPulse * 0.10f);
            float riftScaleX = Mathf.Lerp(0.22f, 0.74f, preReleaseProgress) * (0.94f + breachPulse * 0.10f);
            float riftScaleZ = Mathf.Lerp(0.90f, 3.90f, preReleaseProgress) * (0.92f + breachPulse * 0.18f);
            float sparksScaleX = riftScaleX * 1.95f;
            float sparksScaleZ = riftScaleZ * 1.06f;
            float glowScale = Mathf.Lerp(0.72f, 2.55f, preReleaseProgress) * (0.92f + ringPulse * 0.18f);
            float ringScale = Mathf.Lerp(0.52f, 2.10f, preReleaseProgress) * (0.90f + ringPulse * 0.16f);
            float angle = seamSide.AsAngle;
            float ringAngle = ticks * (0.95f + preReleaseProgress * 0.42f);
            float ringCounterAngle = -ticks * (0.52f + preReleaseProgress * 0.18f);

            DrawPlane(ShadowPath, groundLoc, shadowScaleX, shadowScaleZ, angle, new Color(0.06f, 0.01f, 0.01f, alpha * 0.78f));
            DrawPlane(GlowPath, groundLoc + new Vector3(0f, 0.004f, 0f), glowScale, ringAngle * 0.06f, new Color(0.92f, 0.08f, 0.14f, alpha * 0.56f));
            DrawPlane(RingPath, groundLoc + new Vector3(0f, 0.006f, 0f), ringScale, ringAngle, new Color(1f, 0.18f, 0.20f, alpha * 0.84f));
            DrawPlane(RingPath, groundLoc + new Vector3(0f, 0.008f, 0f), ringScale * 0.72f, ringCounterAngle, new Color(0.60f, 0.05f, 0.08f, alpha * 0.56f));
            DrawPlane(RiftPath, groundLoc + new Vector3(0f, 0.010f, 0f), riftScaleX, riftScaleZ, angle, new Color(1f, 0.18f, 0.16f, alpha * 0.95f));
            DrawPlane(RiftPath, groundLoc + new Vector3(0f, 0.012f, 0f), riftScaleX * 0.50f, riftScaleZ * 0.86f, angle, new Color(0.96f, 0.68f, 0.20f, alpha * 0.22f));
            DrawPlane(SparksPath, groundLoc + new Vector3(0f, 0.014f, 0f), sparksScaleX, sparksScaleZ, angle + Mathf.Sin(ticks * 0.08f) * 6f, new Color(1f, 0.70f, 0.28f, alpha * (0.28f + emberPulse * 0.24f)));

            if (silhouetteReveal <= 0.01f)
            {
                return;
            }

            Vector3 beastLoc = drawLoc;
            beastLoc.y = AltitudeLayer.Building.AltitudeFor() + 0.082f;
            beastLoc += new Vector3(-seamVectorCell.x * 0.11f, 0f, -seamVectorCell.z * Mathf.Lerp(0.34f, 0.04f, preReleaseProgress));
            float jitter = bossReleased ? 0.004f * (1f - postReleaseProgress) : 0.010f * (1f - preReleaseProgress);
            beastLoc.x += Mathf.Sin(ticks * 0.13f) * jitter;
            beastLoc.z += Mathf.Cos(ticks * 0.09f) * jitter;

            float beastScale = Mathf.Lerp(1.18f, 1.86f, silhouetteReveal) * (1f - collapse * 0.10f);
            float beastShadowScale = beastScale * 1.04f;
            float beastAngle = Mathf.Sin(ticks * 0.05f) * 1.5f;
            float beastAlpha = silhouetteReveal * silhouetteFade;

            DrawCutoutPlane(SilhouetteShadowPath, beastLoc + new Vector3(0f, -0.004f, 0.08f), beastShadowScale * 1.10f, beastShadowScale * 1.02f, beastAngle, new Color(0.04f, 0.01f, 0.01f, beastAlpha * 0.62f));
            DrawCutoutPlane(SilhouettePath, beastLoc, beastScale, beastScale, beastAngle, new Color(0.46f, 0.06f, 0.06f, beastAlpha * 0.92f));
            DrawCutoutPlane(SilhouettePath, beastLoc + new Vector3(0f, 0.002f, 0f), beastScale * 1.02f, beastScale * 1.02f, beastAngle, new Color(1f, 0.24f, 0.18f, beastAlpha * 0.16f));
        }

        private void ReleaseBoss()
        {
            if (bossReleased || Map == null || manifestationFaction == null || bossKindDef == null)
            {
                return;
            }

            if (!TryFindBossSpawnCell(out IntVec3 spawnCell))
            {
                spawnCell = bossArrivalCell.IsValid ? bossArrivalCell : Position;
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

            GenSpawn.Spawn(pawn, spawnCell, Map, Rot4.Random);
            releasedBoss = pawn;
            bossReleased = true;

            Pawn nearestThreat = AbyssalThreatPawnUtility.FindClosestThreatWithin(pawn, 36f);
            if (nearestThreat != null)
            {
                pawn.rotationTracker?.FaceCell(nearestThreat.Position);
            }

            pawn.pather?.StopDead();
            ArchonInfernalVFXUtility.DoSummonVFX(Map, spawnCell);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterBoss(pawn);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.34f);
            ABY_SoundUtility.PlayAt(arrivalSoundDefName.NullOrEmpty() ? "ABY_ArchonBossArrive" : arrivalSoundDefName, spawnCell, Map);
            TrySpawnManifestMote(WarmupEntryMoteDefName, 1.45f);
            TrySpawnManifestMote(WarmupExitMoteDefName, 1.28f);
            FleckMaker.Static(spawnCell, Map, FleckDefOf.ExplosionFlash, 1.85f);
            MakeAshScar(4, 3.1f);

            string letterLabel = completionLetterLabelKey.NullOrEmpty()
                ? "ABY_BossSummonSuccessLabel".Translate()
                : completionLetterLabelKey.Translate();
            string letterDesc = completionLetterDescKey.NullOrEmpty()
                ? "ABY_BossSummonSuccessDesc".Translate(bossLabel)
                : completionLetterDescKey.Translate();
            Find.LetterStack.ReceiveLetter(
                letterLabel,
                letterDesc,
                LetterDefOf.ThreatBig,
                new TargetInfo(spawnCell, Map));
        }

        private void ReleaseBossToCombat()
        {
            if (lordReleased)
            {
                return;
            }

            lordReleased = true;
            if (releasedBoss != null && releasedBoss.Spawned && !releasedBoss.Destroyed)
            {
                AbyssalLordUtility.EnsureAssaultLord(releasedBoss, sappers: true);
                releasedBoss = null;
            }
        }

        private bool TryFindBossSpawnCell(out IntVec3 cell)
        {
            IntVec3 root = bossArrivalCell.IsValid ? bossArrivalCell : Position;
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(root, 2.6f, true))
            {
                if (!candidate.InBounds(Map) || !candidate.Standable(Map))
                {
                    continue;
                }

                if (candidate.GetFirstPawn(Map) != null)
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            cell = root;
            return cell.IsValid && cell.InBounds(Map);
        }

        private Rot4 ResolveSeamSide()
        {
            if (Map == null)
            {
                return Rot4.South;
            }

            IntVec3 anchor = Map.Center;
            var colonists = Map.mapPawns?.FreeColonistsSpawned;
            if (colonists != null && colonists.Count > 0)
            {
                int totalX = 0;
                int totalZ = 0;
                int count = 0;
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    if (pawn == null || !pawn.Spawned)
                    {
                        continue;
                    }

                    totalX += pawn.Position.x;
                    totalZ += pawn.Position.z;
                    count++;
                }

                if (count > 0)
                {
                    anchor = new IntVec3(totalX / count, 0, totalZ / count);
                }
            }

            IntVec3 origin = bossArrivalCell.IsValid ? bossArrivalCell : Position;
            IntVec3 delta = anchor - origin;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
            {
                return delta.x >= 0 ? Rot4.East : Rot4.West;
            }

            return delta.z >= 0 ? Rot4.North : Rot4.South;
        }

        private float GetPostReleaseProgress()
        {
            if (!bossReleased)
            {
                return 0f;
            }

            return Mathf.Clamp01((ticksActive - bossReleaseDelayTicks) / (float)Mathf.Max(1, postReleaseTicks));
        }

        private void MakeAshScar(int amountPerCell, float radius)
        {
            if (Map == null || amountPerCell <= 0)
            {
                return;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, radius, true))
            {
                if (!cell.InBounds(Map) || !cell.Standable(Map))
                {
                    continue;
                }

                FilthMaker.TryMakeFilth(cell, Map, ThingDefOf.Filth_Ash, amountPerCell);
            }
        }

        private void TrySpawnManifestMote(string defName, float scale)
        {
            if (Map == null || defName.NullOrEmpty())
            {
                return;
            }

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(Position.ToVector3Shifted(), Map, moteDef, scale);
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
    }
}
