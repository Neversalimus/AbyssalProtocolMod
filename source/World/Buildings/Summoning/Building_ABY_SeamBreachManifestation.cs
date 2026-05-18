using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public sealed class Building_ABY_SeamBreachManifestation : Building_ABY_HostileManifestationBase
    {
        private const string RiftPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Rift";
        private const string SparksPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Sparks";
        private const string ShadowPath = "Things/VFX/SeamBreach/ABY_SeamBreach_Shadow";

        private Rot4 seamSide = Rot4.South;

        public void Initialize(
            Faction faction,
            System.Collections.Generic.List<ABY_HostileManifestEntry> entries,
            int warmup,
            Rot4 seamSide,
            string packLabel = null,
            string letterLabel = null,
            string letterDesc = null)
        {
            base.Initialize(faction, entries, warmup, packLabel, letterLabel, letterDesc);
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

            if (ticksActive % 18 == 0)
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
            }
        }

        protected override void DrawManifestation(Vector3 drawLoc)
        {
            float progress = Progress;
            float riftPulse = Pulse(0.095f, 0.7f);
            float sparkPulse = Pulse(0.15f, 1.7f);

            IntVec3 seamVectorCell = seamSide.FacingCell;
            Vector3 seamOffset = new Vector3(seamVectorCell.x * 0.26f, 0f, seamVectorCell.z * 0.26f);

            Vector3 loc = drawLoc + seamOffset;
            loc.y += 0.034f;

            float angle = seamSide.AsAngle;
            float shadowScaleX = Mathf.Lerp(0.32f, 0.92f, progress) * (0.95f + riftPulse * 0.05f);
            float shadowScaleZ = Mathf.Lerp(0.64f, 1.22f, progress) * (0.92f + riftPulse * 0.06f);
            float riftScaleX = Mathf.Lerp(0.16f, 0.44f, progress) * (0.96f + riftPulse * 0.10f);
            float riftScaleZ = Mathf.Lerp(0.44f, 1.40f, progress) * (0.92f + riftPulse * 0.14f);
            float sparkScaleX = riftScaleX * 1.65f;
            float sparkScaleZ = riftScaleZ * 1.05f;

            float alpha = Mathf.Lerp(0.22f, 1f, progress);

            DrawPlane(ShadowPath, loc, shadowScaleX, shadowScaleZ, angle, new Color(0.06f, 0.01f, 0.01f, alpha * 0.72f));
            DrawPlane(RiftPath, loc + new Vector3(0f, 0.006f, 0f), riftScaleX, riftScaleZ, angle, new Color(1f, 0.22f, 0.18f, alpha * 0.94f));
            DrawPlane(RiftPath, loc + new Vector3(0f, 0.010f, 0f), riftScaleX * 0.56f, riftScaleZ * 0.84f, angle, new Color(0.52f, 1f, 0.96f, alpha * 0.50f));
            DrawPlane(SparksPath, loc + new Vector3(0f, 0.012f, 0f), sparkScaleX, sparkScaleZ, angle + Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.10f) * 5f, new Color(1f, 0.72f, 0.32f, alpha * (0.30f + sparkPulse * 0.28f)));
        }
    }
}
