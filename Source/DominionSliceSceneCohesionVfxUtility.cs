using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSliceSceneCohesionVfxUtility
    {
        private const string CohesionHaloMoteDefName = "ABY_Mote_DominionSliceCohesionHalo";
        private const string AxisAccentMoteDefName = "ABY_Mote_DominionSliceCohesionAxisAccent";
        private const string CrownSealMoteDefName = "ABY_Mote_DominionSliceCohesionCrownSeal";
        private const string CollapseVeilMoteDefName = "ABY_Mote_DominionSliceCohesionCollapseVeil";
        private const string QuietEmberMoteDefName = "ABY_Mote_DominionSliceCohesionQuietEmber";

        private static ThingDef cohesionHaloMoteDef;
        private static ThingDef axisAccentMoteDef;
        private static ThingDef crownSealMoteDef;
        private static ThingDef collapseVeilMoteDef;
        private static ThingDef quietEmberMoteDef;

        private static ThingDef CohesionHaloMoteDef
        {
            get { return cohesionHaloMoteDef ?? (cohesionHaloMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(CohesionHaloMoteDefName)); }
        }

        private static ThingDef AxisAccentMoteDef
        {
            get { return axisAccentMoteDef ?? (axisAccentMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(AxisAccentMoteDefName)); }
        }

        private static ThingDef CrownSealMoteDef
        {
            get { return crownSealMoteDef ?? (crownSealMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(CrownSealMoteDefName)); }
        }

        private static ThingDef CollapseVeilMoteDef
        {
            get { return collapseVeilMoteDef ?? (collapseVeilMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(CollapseVeilMoteDefName)); }
        }

        private static ThingDef QuietEmberMoteDef
        {
            get { return quietEmberMoteDef ?? (quietEmberMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(QuietEmberMoteDefName)); }
        }

        public static void SpawnHeartCohesionHalo(IntVec3 heartCell, Map map, float intensity, bool collapse)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            float clamped = Mathf.Clamp01(intensity / 1.45f);
            Vector3 pos = heartCell.ToVector3Shifted();
            SpawnStaticMote(pos, map, CohesionHaloMoteDef, Mathf.Lerp(collapse ? 5.0f : 4.2f, collapse ? 7.8f : 6.2f, clamped));
            if (collapse)
            {
                SpawnStaticMote(pos + new Vector3(0f, 0.005f, 0f), map, CollapseVeilMoteDef, Mathf.Lerp(5.4f, 8.0f, clamped));
            }
        }

        public static void SpawnPhaseTransitionSeal(IntVec3 heartCell, Map map, float intensity, bool collapse)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            Vector3 pos = heartCell.ToVector3Shifted();
            float clamped = Mathf.Clamp01(intensity / 1.45f);
            SpawnStaticMote(pos, map, CrownSealMoteDef, Mathf.Lerp(3.2f, collapse ? 5.8f : 4.6f, clamped));
            SpawnStaticMote(pos + new Vector3(0f, 0.004f, 0f), map, CohesionHaloMoteDef, Mathf.Lerp(4.6f, collapse ? 7.3f : 5.8f, clamped));
            FleckMaker.ThrowLightningGlow(pos, map, Mathf.Lerp(1.2f, collapse ? 3.1f : 2.1f, clamped));
        }

        public static void SpawnCrownSeal(IntVec3 heartCell, Map map, float intensity, int liveAnchors)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            float clamped = Mathf.Clamp01(intensity / 1.35f);
            float anchorBoost = Mathf.Clamp(liveAnchors, 0, 3) * 0.12f;
            SpawnStaticMote(heartCell.ToVector3Shifted() + new Vector3(0f, 0.006f, 0f), map, CrownSealMoteDef, Mathf.Lerp(2.9f, 4.85f + anchorBoost, clamped));
        }

        public static void SpawnAxisAccent(IntVec3 from, IntVec3 to, Map map, float intensity, int count)
        {
            if (!IsValid(from, map) || !IsValid(to, map))
            {
                return;
            }

            int safeCount = Mathf.Clamp(count, 2, 9);
            float clamped = Mathf.Clamp01(intensity / 1.45f);
            for (int i = 1; i <= safeCount; i++)
            {
                float t = i / (float)(safeCount + 1);
                IntVec3 cell = LerpCell(from, to, t);
                cell = ClampToMap(cell, map);
                if (!IsValid(cell, map))
                {
                    continue;
                }

                Vector3 pos = cell.ToVector3Shifted();
                pos.y += 0.003f + i * 0.001f;
                SpawnStaticMote(pos, map, AxisAccentMoteDef, Mathf.Lerp(0.70f, 1.28f, clamped));
            }
        }

        public static void SpawnRadialCohesion(IntVec3 heartCell, Map map, float intensity, int arms)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            int safeArms = Mathf.Clamp(arms, 3, 8);
            float radius = Mathf.Lerp(7f, 14f, Mathf.Clamp01(intensity / 1.45f));
            for (int i = 0; i < safeArms; i++)
            {
                float angle = (360f / safeArms) * i + Rand.Range(-10f, 10f);
                float rad = angle * Mathf.Deg2Rad;
                IntVec3 end = new IntVec3(
                    heartCell.x + GenMath.RoundRandom(Mathf.Cos(rad) * radius),
                    0,
                    heartCell.z + GenMath.RoundRandom(Mathf.Sin(rad) * radius));
                SpawnAxisAccent(heartCell, ClampToMap(end, map), map, intensity * 0.75f, 2);
            }
        }

        public static void SpawnCollapseVeil(IntVec3 heartCell, IntVec3 extractionCell, Map map, float intensity)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            float clamped = Mathf.Clamp01(intensity / 1.55f);
            Vector3 pos = heartCell.ToVector3Shifted();
            SpawnStaticMote(pos, map, CollapseVeilMoteDef, Mathf.Lerp(6.4f, 9.4f, clamped));
            if (IsValid(extractionCell, map))
            {
                IntVec3 mid = LerpCell(heartCell, extractionCell, 0.55f);
                SpawnStaticMote(mid.ToVector3Shifted() + new Vector3(0f, 0.004f, 0f), map, CollapseVeilMoteDef, Mathf.Lerp(3.3f, 5.1f, clamped));
            }
        }

        public static void SpawnSubtleEdgeCohesion(Map map, float intensity, bool collapse)
        {
            if (map == null || map.Size.x <= 16 || map.Size.z <= 16)
            {
                return;
            }

            int count = collapse ? 4 : 2;
            float clamped = Mathf.Clamp01(intensity / 1.45f);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                if (!TryFindEdgeCell(map, out cell))
                {
                    continue;
                }

                SpawnStaticMote(cell.ToVector3Shifted(), map, QuietEmberMoteDef, Mathf.Lerp(0.55f, collapse ? 1.12f : 0.82f, clamped));
            }
        }

        public static void SpawnQuietEmbers(Map map, IntVec3 focus, float intensity, bool collapse)
        {
            if (!IsValid(focus, map))
            {
                return;
            }

            int count = collapse ? 4 : 2;
            float radius = collapse ? 19f : 13f;
            float clamped = Mathf.Clamp01(intensity / 1.45f);
            for (int i = 0; i < count; i++)
            {
                float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Rand.Range(3.5f, radius);
                IntVec3 cell = new IntVec3(
                    focus.x + GenMath.RoundRandom(Mathf.Cos(angle) * dist),
                    0,
                    focus.z + GenMath.RoundRandom(Mathf.Sin(angle) * dist));
                cell = ClampToMap(cell, map);
                if (!IsValid(cell, map))
                {
                    continue;
                }

                SpawnStaticMote(cell.ToVector3Shifted(), map, QuietEmberMoteDef, Mathf.Lerp(0.45f, collapse ? 0.95f : 0.70f, clamped));
            }
        }

        private static bool TryFindEdgeCell(Map map, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || map.Size.x <= 16 || map.Size.z <= 16)
            {
                return false;
            }

            int side = Rand.RangeInclusive(0, 3);
            int x;
            int z;
            if (side == 0)
            {
                x = Rand.RangeInclusive(7, map.Size.x - 8);
                z = Rand.RangeInclusive(7, 12);
            }
            else if (side == 1)
            {
                x = Rand.RangeInclusive(7, map.Size.x - 8);
                z = Rand.RangeInclusive(map.Size.z - 13, map.Size.z - 8);
            }
            else if (side == 2)
            {
                x = Rand.RangeInclusive(7, 12);
                z = Rand.RangeInclusive(7, map.Size.z - 8);
            }
            else
            {
                x = Rand.RangeInclusive(map.Size.x - 13, map.Size.x - 8);
                z = Rand.RangeInclusive(7, map.Size.z - 8);
            }

            cell = new IntVec3(x, 0, z);
            return cell.InBounds(map);
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

        private static void SpawnStaticMote(Vector3 pos, Map map, ThingDef moteDef, float scale)
        {
            if (map == null || moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(pos, map, moteDef, scale);
        }

        private static bool IsValid(IntVec3 cell, Map map)
        {
            return map != null && cell.IsValid && cell.InBounds(map);
        }
    }
}
