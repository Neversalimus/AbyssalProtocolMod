using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public sealed class Building_ABY_SigilBloomManifestation : Building_ABY_HostileManifestationBase
    {
        private const string CorePath = "Things/VFX/SigilBloom/ABY_SigilBloom_Core";
        private const string RingPath = "Things/VFX/SigilBloom/ABY_SigilBloom_Ring";
        private const string GlyphsPath = "Things/VFX/SigilBloom/ABY_SigilBloom_Glyphs";

        protected override void TickManifestation()
        {
            if (Map == null || !Position.IsValid)
            {
                return;
            }

            if (ticksActive % 22 == 0)
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
            }
        }

        protected override void DrawManifestation(Vector3 drawLoc)
        {
            float progress = Progress;
            float outerPulse = Pulse(0.085f);
            float glyphPulse = Pulse(0.11f, 1.3f);
            float bloomPulse = Pulse(0.06f, 2.2f);

            Vector3 loc = drawLoc;
            loc.y += 0.028f;

            float coreScale = Mathf.Lerp(0.30f, 1.10f, progress) * (0.95f + bloomPulse * 0.12f);
            float ringScale = Mathf.Lerp(0.55f, 1.62f, progress) * (0.94f + outerPulse * 0.08f);
            float glyphScale = Mathf.Lerp(0.48f, 1.46f, progress) * (0.96f + glyphPulse * 0.10f);

            float alpha = Mathf.Lerp(0.24f, 1f, progress);
            float angle = (Find.TickManager.TicksGame + seed) * 1.10f;
            float counterAngle = -(Find.TickManager.TicksGame + seed) * 0.78f;

            DrawPlane(CorePath, loc, coreScale, angle * 0.22f, new Color(0.82f, 0.98f, 1f, alpha * 0.92f));
            DrawPlane(RingPath, loc + new Vector3(0f, 0.004f, 0f), ringScale, angle, new Color(0.16f, 0.98f, 0.90f, alpha * 0.90f));
            DrawPlane(RingPath, loc + new Vector3(0f, 0.006f, 0f), ringScale * 0.82f, counterAngle, new Color(1f, 0.22f, 0.44f, alpha * 0.42f));
            DrawPlane(GlyphsPath, loc + new Vector3(0f, 0.010f, 0f), glyphScale, counterAngle * 0.92f, new Color(0.72f, 0.98f, 1f, alpha * 0.88f));
        }
    }
}
