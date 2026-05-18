using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class DominionSliceSceneCohesionVfxUtility
    {
        private const string AxisAccentMoteDefName = "ABY_Mote_DominionSliceCohesionAxisAccent";
        private const string CollapseVeilMoteDefName = "ABY_Mote_DominionSliceCohesionCollapseVeil";
        private const string QuietEmberMoteDefName = "ABY_Mote_DominionSliceCohesionQuietEmber";

        private static ThingDef axisAccentMoteDef;
        private static ThingDef collapseVeilMoteDef;
        private static ThingDef quietEmberMoteDef;

        private static ThingDef AxisAccentMoteDef
        {
            get { return axisAccentMoteDef ?? (axisAccentMoteDef = DefDatabase<ThingDef>.GetNamedSilentFail(AxisAccentMoteDefName)); }
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
            // Disabled: the old cohesion halo was one of the large magic-circle layers around the heart.
        }

        public static void SpawnPhaseTransitionSeal(IntVec3 heartCell, Map map, float intensity, bool collapse)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            Vector3 pos = heartCell.ToVector3Shifted();
            FleckMaker.ThrowLightningGlow(pos, map, collapse ? 0.92f : 0.55f);
        }

        public static void SpawnCrownSeal(IntVec3 heartCell, Map map, float intensity, int liveAnchors)
        {
            // Disabled: no crown-seal magic circle on the new industrial heart platform.
        }

        public static void SpawnAxisAccent(IntVec3 from, IntVec3 to, Map map, float intensity, int count)
        {
            if (!IsValid(from, map) || !IsValid(to, map))
            {
                return;
            }

            int safeCount = Mathf.Clamp(count, 1, 4);
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
                SpawnStaticMote(pos, map, AxisAccentMoteDef, Mathf.Lerp(0.28f, 0.68f, clamped));
            }
        }

        public static void SpawnRadialCohesion(IntVec3 heartCell, Map map, float intensity, int arms)
        {
            // Disabled: radial cohesion read as a ritual circle around the heart.
        }

        public static void SpawnCollapseVeil(IntVec3 heartCell, IntVec3 extractionCell, Map map, float intensity)
        {
            if (!IsValid(heartCell, map))
            {
                return;
            }

            float clamped = Mathf.Clamp01(intensity / 1.55f);
            Vector3 pos = heartCell.ToVector3Shifted();
            SpawnStaticMote(pos, map, CollapseVeilMoteDef, Mathf.Lerp(1.25f, 2.20f, clamped));
            if (IsValid(extractionCell, map))
            {
                IntVec3 mid = LerpCell(heartCell, extractionCell, 0.55f);
                SpawnStaticMote(mid.ToVector3Shifted() + new Vector3(0f, 0.004f, 0f), map, CollapseVeilMoteDef, Mathf.Lerp(0.85f, 1.40f, clamped));
            }
        }

        public static void SpawnSubtleEdgeCohesion(Map map, float intensity, bool collapse)
        {
            if (map == null || map.Size.x <= 16 || map.Size.z <= 16)
            {
                return;
            }

            int count = collapse ? 3 : 1;
            float clamped = Mathf.Clamp01(intensity / 1.45f);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                if (!TryFindEdgeCell(map, out cell))
                {
                    continue;
                }

                SpawnStaticMote(cell.ToVector3Shifted(), map, QuietEmberMoteDef, Mathf.Lerp(0.42f, collapse ? 0.90f : 0.62f, clamped));
            }
        }

        public static void SpawnQuietEmbers(Map map, IntVec3 focus, float intensity, bool collapse)
        {
            if (!IsValid(focus, map))
            {
                return;
            }

            int count = collapse ? 3 : 1;
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

                SpawnStaticMote(cell.ToVector3Shifted(), map, QuietEmberMoteDef, Mathf.Lerp(0.36f, collapse ? 0.82f : 0.58f, clamped));
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
            if (moteDef == null || map == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(pos, map, moteDef, Mathf.Max(0.05f, scale));
        }

        private static bool IsValid(IntVec3 cell, Map map)
        {
            return map != null && cell.IsValid && cell.InBounds(map) && !cell.Fogged(map);
        }
    }
}
