using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSliceFlowVfxUtility
    {
        private const string FlowPulseDefName = "ABY_Mote_DominionSliceFlowPulse";
        private const string FlowNodeDefName = "ABY_Mote_DominionSliceFlowNode";
        private const string FlowSurgeDefName = "ABY_Mote_DominionSliceFlowSurge";

        private static ThingDef flowPulseDef;
        private static ThingDef flowNodeDef;
        private static ThingDef flowSurgeDef;

        private static ThingDef FlowPulseDef
        {
            get { return flowPulseDef ?? (flowPulseDef = DefDatabase<ThingDef>.GetNamedSilentFail(FlowPulseDefName)); }
        }

        private static ThingDef FlowNodeDef
        {
            get { return flowNodeDef ?? (flowNodeDef = DefDatabase<ThingDef>.GetNamedSilentFail(FlowNodeDefName)); }
        }

        private static ThingDef FlowSurgeDef
        {
            get { return flowSurgeDef ?? (flowSurgeDef = DefDatabase<ThingDef>.GetNamedSilentFail(FlowSurgeDefName)); }
        }

        public static void SpawnFlowLine(IntVec3 from, IntVec3 to, Map map, float intensity, bool extractionFlow, int requestedSamples)
        {
            if (map == null || !from.IsValid || !to.IsValid)
            {
                return;
            }

            int samples = Mathf.Clamp(requestedSamples, 2, 9);
            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float phaseOffset = ((ticks % 90) / 90f) * (extractionFlow ? 0.24f : 0.18f);
            float clamped = Mathf.Clamp01(intensity / 2.0f);

            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)(samples + 1);
                t += extractionFlow ? phaseOffset : -phaseOffset;
                while (t > 0.96f)
                {
                    t -= 0.82f;
                }

                if (t < 0.04f)
                {
                    t += 0.08f;
                }

                IntVec3 cell = LerpCell(from, to, t);
                cell = ClampToMap(cell, map);
                if (!cell.IsValid || !cell.InBounds(map))
                {
                    continue;
                }

                float scale = Mathf.Lerp(extractionFlow ? 0.92f : 0.76f, extractionFlow ? 1.42f : 1.22f, clamped);
                SpawnStaticMote(CellToFlowPos(cell, i * 0.002f), map, FlowPulseDef, scale);
            }
        }

        public static void SpawnFlowNode(IntVec3 cell, Map map, float intensity)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            float scale = Mathf.Lerp(1.1f, 2.05f, Mathf.Clamp01(intensity / 2.1f));
            SpawnStaticMote(CellToFlowPos(cell, 0.010f), map, FlowNodeDef, scale);
        }

        public static void SpawnFlowSurge(IntVec3 cell, Map map, float intensity)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            float scale = Mathf.Lerp(1.35f, 2.75f, Mathf.Clamp01(intensity / 2.2f));
            SpawnStaticMote(CellToFlowPos(cell, 0.016f), map, FlowSurgeDef, scale);
            if (Rand.Chance(0.20f + Mathf.Clamp01(intensity / 2.5f) * 0.25f))
            {
                FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, scale * 0.75f);
            }
        }

        public static void SpawnRadialFlow(IntVec3 center, Map map, float intensity, int arms, float radius)
        {
            if (map == null || !center.IsValid)
            {
                return;
            }

            int safeArms = Mathf.Clamp(arms, 3, 10);
            for (int i = 0; i < safeArms; i++)
            {
                float angle = (360f / safeArms) * i + Rand.Range(-8f, 8f);
                float rad = angle * Mathf.Deg2Rad;
                IntVec3 end = new IntVec3(
                    center.x + GenMath.RoundRandom(Mathf.Cos(rad) * radius),
                    0,
                    center.z + GenMath.RoundRandom(Mathf.Sin(rad) * radius));
                end = ClampToMap(end, map);
                SpawnFlowLine(center, end, map, intensity * 0.88f, false, 3);
            }
        }

        private static IntVec3 LerpCell(IntVec3 from, IntVec3 to, float t)
        {
            return new IntVec3(
                GenMath.RoundRandom(Mathf.Lerp(from.x, to.x, t)),
                0,
                GenMath.RoundRandom(Mathf.Lerp(from.z, to.z, t)));
        }

        private static IntVec3 ClampToMap(IntVec3 cell, Map map)
        {
            if (map == null || !cell.IsValid)
            {
                return IntVec3.Invalid;
            }

            int x = System.Math.Max(6, System.Math.Min(map.Size.x - 7, cell.x));
            int z = System.Math.Max(6, System.Math.Min(map.Size.z - 7, cell.z));
            return new IntVec3(x, 0, z);
        }

        private static Vector3 CellToFlowPos(IntVec3 cell, float altitudeOffset)
        {
            return new Vector3(cell.x + 0.5f, AltitudeLayer.MoteOverhead.AltitudeFor() + 0.026f + altitudeOffset, cell.z + 0.5f);
        }

        private static void SpawnStaticMote(Vector3 pos, Map map, ThingDef moteDef, float scale)
        {
            if (map == null || moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(pos, map, moteDef, scale);
        }
    }
}
