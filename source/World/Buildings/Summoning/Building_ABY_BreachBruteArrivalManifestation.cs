using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public sealed class Building_ABY_BreachBruteArrivalManifestation : Building_ABY_HostileManifestationBase
    {
        private const string RiftPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Rift";
        private const string SparksPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Sparks";
        private const string ShadowPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Shadow";

        private Rot4 seamSide = Rot4.South;

        protected override bool CreateAshOnComplete => true;

        public void Initialize(int warmupTicks, Rot4 seamSide)
        {
            base.Initialize(null, null, warmupTicks, null, null, null);
            this.seamSide = seamSide;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref seamSide, "seamSide", Rot4.South);
        }

        protected override void TickManifestation()
        {
            if (Map == null || !Position.IsValid)
            {
                return;
            }

            if (ticksActive % 15 == 0)
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
            }
        }

        protected override void DrawManifestation(Vector3 drawLoc)
        {
            float progress = Progress;
            float riftPulse = Pulse(0.09f, 0.8f);
            float sparkPulse = Pulse(0.16f, 2.1f);
            float furnacePulse = Pulse(0.07f, 1.2f);

            IntVec3 seamVectorCell = seamSide.FacingCell;
            Vector3 seamOffset = new Vector3(seamVectorCell.x * 0.34f, 0f, seamVectorCell.z * 0.34f);

            Vector3 loc = drawLoc + seamOffset;
            loc.y += 0.036f;

            float angle = seamSide.AsAngle;
            float alpha = Mathf.Lerp(0.24f, 1f, progress);

            float shadowScaleX = Mathf.Lerp(0.52f, 1.34f, progress) * (0.94f + riftPulse * 0.06f);
            float shadowScaleZ = Mathf.Lerp(0.88f, 2.00f, progress) * (0.90f + riftPulse * 0.08f);
            float riftScaleX = Mathf.Lerp(0.20f, 0.72f, progress) * (0.96f + riftPulse * 0.10f);
            float riftScaleZ = Mathf.Lerp(0.60f, 2.10f, progress) * (0.90f + riftPulse * 0.16f);
            float sparkScaleX = riftScaleX * 1.95f;
            float sparkScaleZ = riftScaleZ * 1.10f;
            float furnaceScaleX = riftScaleX * 0.66f;
            float furnaceScaleZ = riftScaleZ * 0.74f;

            DrawPlane(ShadowPath, loc, shadowScaleX, shadowScaleZ, angle, new Color(0.05f, 0.01f, 0.01f, alpha * 0.78f));
            DrawPlane(RiftPath, loc + new Vector3(0f, 0.006f, 0f), riftScaleX, riftScaleZ, angle, new Color(1f, 0.18f, 0.14f, alpha * 0.98f));
            DrawPlane(RiftPath, loc + new Vector3(0f, 0.010f, 0f), furnaceScaleX, furnaceScaleZ, angle, new Color(1f, 0.62f, 0.18f, alpha * (0.40f + furnacePulse * 0.24f)));
            DrawPlane(RiftPath, loc + new Vector3(0f, 0.012f, 0f), furnaceScaleX * 0.58f, furnaceScaleZ * 0.78f, angle, new Color(0.58f, 1f, 0.94f, alpha * 0.38f));
            DrawPlane(SparksPath, loc + new Vector3(0f, 0.014f, 0f), sparkScaleX, sparkScaleZ, angle + Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.11f) * 7f, new Color(1f, 0.74f, 0.28f, alpha * (0.34f + sparkPulse * 0.34f)));
        }
    }
}
