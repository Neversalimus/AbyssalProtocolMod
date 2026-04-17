using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public sealed class Building_ABY_ReactorSaintManifestation : Building_ABY_HostileManifestationBase
    {
        private const string HaloPath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Halo";
        private const string NoisePath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Noise";
        private const string CrackPath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Crack";
        private const string CorePath = "Things/VFX/SigilBloom/ABY_SigilBloom_Core";
        private const string RingPath = "Things/VFX/SigilBloom/ABY_SigilBloom_Ring";

        private PawnKindDef bossKindDef;
        private IntVec3 bossArrivalCell = IntVec3.Invalid;
        private string bossLabel;
        private string arrivalSoundDefName;
        private string completionLetterLabelKey;
        private string completionLetterDescKey;

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

            if (ticksActive % 24 == 0)
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
            }

            if (ticksActive % 20 == 0)
            {
                FleckMaker.ThrowMicroSparks(DrawPos, Map);
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
            float progress = Progress;
            float pulseA = Pulse(0.11f, 0.2f);
            float pulseB = Pulse(0.08f, 1.6f);
            float pulseC = Pulse(0.17f, 2.9f);

            Vector3 loc = drawLoc;
            loc.y += 0.031f;

            float haloScale = Mathf.Lerp(0.40f, 1.75f, progress) * (0.92f + pulseA * 0.14f);
            float ringScale = Mathf.Lerp(0.55f, 1.95f, progress) * (0.90f + pulseB * 0.16f);
            float noiseScale = Mathf.Lerp(0.35f, 1.35f, progress) * (0.92f + pulseC * 0.18f);
            float crackScale = Mathf.Lerp(0.18f, 1.05f, progress) * (0.88f + pulseA * 0.20f);
            float coreScale = Mathf.Lerp(0.16f, 0.98f, progress) * (0.92f + pulseB * 0.18f);

            float alpha = Mathf.Lerp(0.18f, 1f, progress);
            float angle = (Find.TickManager.TicksGame + seed) * 1.55f;
            float counterAngle = -(Find.TickManager.TicksGame + seed) * 1.10f;
            float scanAngle = 90f + Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.04f) * 10f;

            DrawPlane(HaloPath, loc, haloScale, angle * 0.08f, new Color(0.58f, 0.98f, 1f, alpha * 0.64f));
            DrawPlane(RingPath, loc + new Vector3(0f, 0.004f, 0f), ringScale, angle, new Color(0.14f, 0.96f, 1f, alpha * 0.80f));
            DrawPlane(RingPath, loc + new Vector3(0f, 0.006f, 0f), ringScale * 0.74f, counterAngle, new Color(1f, 0.34f, 0.14f, alpha * 0.46f));
            DrawPlane(NoisePath, loc + new Vector3(0f, 0.008f, 0f), noiseScale, 1.16f + Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.19f) * 0.06f, scanAngle, new Color(0.78f, 0.98f, 1f, alpha * 0.76f));
            DrawPlane(CrackPath, loc + new Vector3(0f, 0.010f, 0f), crackScale, crackScale * 1.22f, angle * 0.36f, new Color(0.96f, 0.42f, 1f, alpha * 0.30f));
            DrawPlane(CorePath, loc + new Vector3(0f, 0.012f, 0f), coreScale, counterAngle * 0.34f, new Color(1f, 0.58f, 0.18f, alpha * 0.88f));
        }
    }
}
