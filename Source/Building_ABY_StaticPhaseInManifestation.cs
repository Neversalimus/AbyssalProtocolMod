using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public sealed class Building_ABY_StaticPhaseInManifestation : Building_ABY_HostileManifestationBase
    {
        private const string HaloPath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Halo";
        private const string NoisePath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Noise";
        private const string CrackPath = "Things/VFX/StaticPhaseIn/ABY_StaticPhaseIn_Crack";

        protected override void TickManifestation()
        {
            if (Map == null || !Position.IsValid)
            {
                return;
            }

            if (ticksActive % 30 == 0)
            {
                FilthMaker.TryMakeFilth(Position, Map, ThingDefOf.Filth_Ash, 1);
            }
        }

        protected override void DrawManifestation(Vector3 drawLoc)
        {
            float progress = Progress;
            float stutter = Mathf.Round(Pulse(0.19f, 0.6f) * 4f) / 4f;
            float sweep = Pulse(0.09f, 2.4f);
            float fracture = Pulse(0.14f, 4.1f);

            Vector3 loc = drawLoc;
            loc.y += 0.030f;

            float haloScale = Mathf.Lerp(0.36f, 1.30f, progress) * (0.95f + sweep * 0.12f);
            float noiseScale = Mathf.Lerp(0.32f, 1.12f, progress) * (0.92f + stutter * 0.15f);
            float crackScale = Mathf.Lerp(0.20f, 0.96f, progress) * (0.88f + fracture * 0.18f);

            float alpha = Mathf.Lerp(0.20f, 1f, progress);
            float scanAngle = 90f + Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.032f) * 7f;
            float wobble = Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.115f) * 6f;

            DrawPlane(HaloPath, loc, haloScale, scanAngle * 0.10f, new Color(0.56f, 1f, 1f, alpha * 0.72f));
            DrawPlane(NoisePath, loc + new Vector3(0f, 0.004f, 0f), noiseScale, 1.14f + Mathf.Sin((Find.TickManager.TicksGame + seed) * 0.22f) * 0.05f, scanAngle + wobble, new Color(0.78f, 0.98f, 1f, alpha * 0.78f));
            DrawPlane(CrackPath, loc + new Vector3(0f, 0.008f, 0f), crackScale, crackScale * 1.20f, wobble * 0.5f, new Color(0.95f, 0.34f, 1f, alpha * 0.48f));
            DrawPlane(CrackPath, loc + new Vector3(0f, 0.010f, 0f), crackScale * 0.72f, crackScale * 0.92f, -wobble - 18f, new Color(0.82f, 1f, 1f, alpha * 0.55f));
        }
    }
}
