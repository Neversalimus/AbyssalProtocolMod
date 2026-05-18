using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Hediff_ABY_AbyssalHarpoonTether : HediffWithComps
    {
        public Thing anchorThing;
        public Vector3 anchorPosition;
        public bool reducedTether;

        private int tickOffset = -1;

        public void ConfigureAnchor(Thing anchor, Vector3 fallbackAnchorPosition, bool reduced, int durationTicks)
        {
            anchorThing = anchor;
            anchorPosition = fallbackAnchorPosition;
            reducedTether = reduced;
            if (tickOffset < 0)
            {
                tickOffset = Rand.RangeInclusive(0, 23);
            }
            ResetDisappearTicks(durationTicks);
        }

        public override void Tick()
        {
            base.Tick();

            Pawn pawn = this.pawn;
            if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.Map == null)
            {
                return;
            }

            int now = Find.TickManager?.TicksGame ?? 0;
            if (tickOffset < 0)
            {
                tickOffset = Mathf.Abs((pawn.thingIDNumber * 17) % 24);
            }

            Vector3 anchor = ResolveAnchorPosition(pawn);
            Vector3 target = pawn.DrawPos;
            target.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            if ((now + tickOffset) % 6 == 0)
            {
                AbyssalHarpoonVfxUtility.SpawnTether(anchor, target, pawn.Map, reducedTether);
            }

            if ((now + tickOffset) % 30 == 0)
            {
                AbyssalHarpoonVfxUtility.SpawnMarker(target, pawn.Map, reducedTether);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref anchorThing, "anchorThing");
            Scribe_Values.Look(ref anchorPosition, "anchorPosition");
            Scribe_Values.Look(ref reducedTether, "reducedTether", false);
            Scribe_Values.Look(ref tickOffset, "tickOffset", -1);
        }

        private Vector3 ResolveAnchorPosition(Pawn pawn)
        {
            if (anchorThing != null && !anchorThing.Destroyed && anchorThing.Spawned && anchorThing.Map == pawn.Map)
            {
                Vector3 pos = anchorThing.DrawPos;
                pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                return pos;
            }

            Vector3 fallback = anchorPosition;
            if (fallback == default(Vector3))
            {
                fallback = pawn.DrawPos;
            }
            fallback.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            return fallback;
        }

        private void ResetDisappearTicks(int durationTicks)
        {
            if (durationTicks <= 0)
            {
                return;
            }

            HediffComp_Disappears disappears = this.TryGetComp<HediffComp_Disappears>();
            if (disappears != null)
            {
                disappears.ticksToDisappear = durationTicks;
            }
        }
    }
}
