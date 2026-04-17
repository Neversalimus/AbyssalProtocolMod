using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public sealed class Building_ABY_ReactorSaintManifestation : Building_ABY_HostileManifestationBase
    {
        private const string CocoonPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon";
        private const string ShadowPath = "Things/VFX/ReactorSaintArrival/ABY_ReactorSaintCocoon_Shadow";
        private const string HaloPath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Halo";
        private const string NoisePath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Noise";
        private const string CrackPath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Crack";
        private const string CorePath = "Things/VFX/SigilBloom/ABY_SigilBloom_Core";
        private const string RingPath = "Things/VFX/SigilBloom/ABY_SigilBloom_Ring";

        private static readonly Graphic CocoonGraphic = GraphicDatabase.Get<Graphic_Single>(
            CocoonPath,
            ShaderDatabase.Cutout,
            new Vector2(4.75f, 4.75f),
            Color.white);

        private const int SkyfallImpactTick = 18;

        private PawnKindDef bossKindDef;
        private IntVec3 bossArrivalCell = IntVec3.Invalid;
        private string bossLabel;
        private string arrivalSoundDefName;
        private string completionLetterLabelKey;
        private string completionLetterDescKey;
        private bool impactTriggered;

        protected override bool CreateAshOnComplete => true;

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
            base.Initialize(faction, null, warmup, null, null, null);
            bossKindDef = kindDef;
            bossArrivalCell = arrivalCell;
            this.bossLabel = bossLabel;
            this.arrivalSoundDefName = arrivalSoundDefName;
            this.completionLetterLabelKey = completionLetterLabelKey;
            this.completionLetterDescKey = completionLetterDescKey;
            impactTriggered = false;
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
            Scribe_Values.Look(ref impactTriggered, "impactTriggered", false);
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

            if (!impactTriggered && ticksActive >= SkyfallImpactTick)
            {
                DoSkyfallImpact();
                impactTriggered = true;
            }

            if (ticksActive % 20 == 0)
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, impactTriggered ? 2 : 1);
            }

            if (!impactTriggered)
            {
                if (ticksActive % 5 == 0)
                {
                    FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.10f + Progress * 1.05f);
                }

                if (ticksActive % 7 == 0)
                {
                    FleckMaker.ThrowMicroSparks(DrawPos, Map);
                }

                return;
            }

            if (ticksActive % 10 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
            }

            if (ticksActive % 24 == 0)
            {
                FleckMaker.ThrowHeatGlow(Position, Map, 0.92f + Progress * 0.72f);
            }

            if (ticksActive % 30 == 0)
            {
                FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.45f + Progress * 0.95f);
            }
        }

        protected override void OnManifestationCompleted()
        {
            if (Map == null || manifestationFaction == null || bossKindDef == null)
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

            AbyssalBossSummonUtility.FinalizeBossArrival(
                pawn,
                manifestationFaction,
                Map,
                spawnCell,
                bossLabel,
                arrivalSoundDefName.NullOrEmpty() ? "ABY_ReactorSaintCharge" : arrivalSoundDefName,
                completionLetterLabelKey,
                completionLetterDescKey);
        }

        protected override void DrawManifestation(Vector3 drawLoc)
        {
            int ticks = Find.TickManager.TicksGame + seed;
            float progress = Progress;
            float descentProgress = Mathf.Clamp01(ticksActive / (float)SkyfallImpactTick);
            float postImpactProgress = impactTriggered
                ? Mathf.Clamp01((ticksActive - SkyfallImpactTick) / (float)Mathf.Max(1, warmupTicks - SkyfallImpactTick))
                : 0f;

            float haloPulse = Pulse(0.11f, 0.2f);
            float ringPulse = Pulse(0.08f, 1.6f);
            float corePulse = Pulse(0.17f, 2.9f);
            float jitterPulse = Pulse(0.14f, 0.7f);

            Vector3 groundLoc = drawLoc;
            groundLoc.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + 0.028f;

            float shadowScaleX = Mathf.Lerp(2.55f, 4.55f, descentProgress) * (0.94f + haloPulse * 0.07f);
            float shadowScaleZ = Mathf.Lerp(1.75f, 3.30f, descentProgress) * (0.92f + haloPulse * 0.08f);
            float haloScale = Mathf.Lerp(0.58f, 2.10f, progress) * (0.90f + haloPulse * 0.16f);
            float ringScale = Mathf.Lerp(0.62f, 2.30f, progress) * (0.90f + ringPulse * 0.18f);
            float coreScale = Mathf.Lerp(0.18f, 1.16f, progress) * (0.90f + corePulse * 0.18f);
            float noiseScale = Mathf.Lerp(0.38f, 1.44f, progress) * (0.92f + jitterPulse * 0.14f);
            float crackScale = impactTriggered
                ? Mathf.Lerp(0.34f, 1.18f, postImpactProgress) * (0.90f + haloPulse * 0.10f)
                : 0f;

            float alpha = Mathf.Lerp(0.22f, 1f, progress);
            float angle = ticks * 1.42f;
            float counterAngle = -ticks * 1.08f;
            float crackAngle = 8f + Mathf.Sin(ticks * 0.07f) * 10f;

            DrawPlane(ShadowPath, groundLoc, shadowScaleX, shadowScaleZ, 0f, new Color(0.08f, 0.02f, 0.01f, alpha * 0.78f));
            DrawPlane(HaloPath, groundLoc + new Vector3(0f, 0.004f, 0f), haloScale, angle * 0.06f, new Color(0.98f, 0.38f, 0.14f, alpha * 0.44f));
            DrawPlane(RingPath, groundLoc + new Vector3(0f, 0.006f, 0f), ringScale, angle, new Color(1f, 0.34f, 0.12f, alpha * 0.78f));
            DrawPlane(RingPath, groundLoc + new Vector3(0f, 0.008f, 0f), ringScale * 0.76f, counterAngle, new Color(1f, 0.86f, 0.32f, alpha * 0.32f));
            DrawPlane(NoisePath, groundLoc + new Vector3(0f, 0.010f, 0f), noiseScale, 1.12f + Mathf.Sin(ticks * 0.15f) * 0.05f, 90f + Mathf.Sin(ticks * 0.04f) * 11f, new Color(1f, 0.92f, 0.72f, alpha * 0.26f));
            DrawPlane(CorePath, groundLoc + new Vector3(0f, 0.012f, 0f), coreScale, counterAngle * 0.30f, new Color(1f, 0.58f, 0.16f, alpha * 0.92f));

            if (impactTriggered)
            {
                DrawPlane(CrackPath, groundLoc + new Vector3(0f, 0.014f, 0f), crackScale, crackScale * 1.18f, crackAngle, new Color(1f, 0.74f, 0.24f, alpha * 0.30f));
            }

            Vector3 cocoonLoc = drawLoc;
            cocoonLoc.y = AltitudeLayer.Building.AltitudeFor() + 0.080f;
            cocoonLoc.z += Mathf.Lerp(0.72f, 0f, descentProgress);

            if (impactTriggered)
            {
                float settleJitter = (1f - postImpactProgress) * 0.020f;
                cocoonLoc.x += Mathf.Sin(ticks * 0.18f) * settleJitter;
                cocoonLoc.z += Mathf.Cos(ticks * 0.14f) * settleJitter;
            }

            float cocoonRotation = Mathf.Lerp(-10f, 0f, descentProgress);
            if (!impactTriggered)
            {
                cocoonRotation += Mathf.Sin(ticks * 0.11f) * 3.0f;
            }
            else
            {
                cocoonRotation += Mathf.Sin(ticks * 0.08f) * (1.10f - postImpactProgress * 0.85f);
            }

            CocoonGraphic.Draw(cocoonLoc, Rot4.South, this, cocoonRotation);
        }

        private void DoSkyfallImpact()
        {
            if (Map == null)
            {
                return;
            }

            ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", Position, Map);
            FleckMaker.ThrowLightningGlow(DrawPos, Map, 2.35f);
            FleckMaker.ThrowLightningGlow(DrawPos, Map, 1.70f);
            FleckMaker.ThrowHeatGlow(Position, Map, 1.45f);
            FleckMaker.ThrowMicroSparks(DrawPos, Map);
            FleckMaker.ThrowMicroSparks(DrawPos, Map);
            FleckMaker.ThrowMicroSparks(DrawPos, Map);

            for (int i = 0; i < 8; i++)
            {
                IntVec3 cell = Position + GenRadial.RadialPattern[i];
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                FleckMaker.ThrowDustPuff(cell.ToVector3Shifted(), Map, 1.00f + Rand.Value * 0.55f);

                if (Rand.Chance(0.35f))
                {
                    FilthMaker.TryMakeFilth(cell, Map, ThingDefOf.Filth_Ash, 1);
                }
            }
        }
    }
}
