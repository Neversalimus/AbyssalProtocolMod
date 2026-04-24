using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_DominionSliceVoidEdgeVisuals : MapComponent
    {
        private int nextRimPulseTick;
        private int nextVoidCrackTick;
        private int nextBoundaryRiftTick;
        private int nextShardTick;

        public MapComponent_DominionSliceVoidEdgeVisuals(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextRimPulseTick, "nextRimPulseTick", 0);
            Scribe_Values.Look(ref nextVoidCrackTick, "nextVoidCrackTick", 0);
            Scribe_Values.Look(ref nextBoundaryRiftTick, "nextBoundaryRiftTick", 0);
            Scribe_Values.Look(ref nextShardTick, "nextShardTick", 0);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null || Find.TickManager == null)
            {
                return;
            }

            MapComponent_DominionSliceEncounter encounter = map.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter == null || !encounter.IsActiveEncounter)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            float intensity = GetPhaseIntensity(encounter);

            if (now >= nextRimPulseTick)
            {
                EmitVoidRimPulses(intensity);
                nextRimPulseTick = now + Mathf.RoundToInt(Rand.Range(110f, 185f) / Mathf.Max(0.62f, intensity));
            }

            if (now >= nextVoidCrackTick)
            {
                EmitVoidCracks(intensity);
                nextVoidCrackTick = now + Mathf.RoundToInt(Rand.Range(75f, 135f) / Mathf.Max(0.62f, intensity));
            }

            if (now >= nextBoundaryRiftTick)
            {
                EmitBoundaryRifts(intensity);
                nextBoundaryRiftTick = now + Mathf.RoundToInt(Rand.Range(180f, 320f) / Mathf.Max(0.62f, intensity));
            }

            if (now >= nextShardTick)
            {
                EmitEdgeShards(intensity);
                nextShardTick = now + Mathf.RoundToInt(Rand.Range(95f, 165f) / Mathf.Max(0.62f, intensity));
            }
        }

        private static float GetPhaseIntensity(MapComponent_DominionSliceEncounter encounter)
        {
            if (encounter == null)
            {
                return 0f;
            }

            float hazard = Mathf.Clamp(encounter.HazardPressure, 0, 10) * 0.035f;
            switch (encounter.CurrentPhase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    return 0.72f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return 0.95f + encounter.LiveAnchorCount * 0.06f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return 1.22f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return 1.75f + hazard;
                default:
                    return 0f;
            }
        }

        private void EmitVoidRimPulses(float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(2f + intensity * 2.2f), 3, 7);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                Rot4 edge;
                if (!TryFindEdgeBandCell(out cell, out edge, 6, 12))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.008f, 0.032f));
                float scale = Rand.Range(1.25f, 2.05f) * Mathf.Lerp(0.85f, 1.28f, Mathf.Clamp01(intensity - 0.6f));
                DominionSliceVoidEdgeVfxUtility.SpawnVoidRim(pos, map, scale);
            }
        }

        private void EmitVoidCracks(float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(1.5f + intensity * 2f), 2, 6);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                Rot4 edge;
                if (!TryFindEdgeBandCell(out cell, out edge, 7, 15))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.010f, 0.040f));
                float scale = Rand.Range(0.75f, 1.55f) * Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(intensity - 0.5f));
                DominionSliceVoidEdgeVfxUtility.SpawnVoidCrack(pos, map, scale);
            }
        }

        private void EmitBoundaryRifts(float intensity)
        {
            int count = encounterSafeCount(intensity >= 1.45f ? 3 : 2, 1, 4);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                Rot4 edge;
                if (!TryFindEdgeBandCell(out cell, out edge, 7, 13))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.015f, 0.045f));
                float scale = Rand.Range(1.15f, 1.95f) * Mathf.Lerp(0.90f, 1.40f, Mathf.Clamp01(intensity - 0.55f));
                DominionSliceVoidEdgeVfxUtility.SpawnBoundaryRift(pos, map, scale);
            }
        }

        private void EmitEdgeShards(float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(2f + intensity * 1.65f), 2, 6);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                Rot4 edge;
                if (!TryFindEdgeBandCell(out cell, out edge, 8, 17))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.020f, 0.055f));
                float scale = Rand.Range(0.65f, 1.25f) * Mathf.Lerp(0.85f, 1.35f, Mathf.Clamp01(intensity - 0.5f));
                DominionSliceVoidEdgeVfxUtility.SpawnEdgeShard(pos, map, scale);
            }
        }

        private static int encounterSafeCount(int value, int min, int max)
        {
            return System.Math.Max(min, System.Math.Min(max, value));
        }

        private bool TryFindEdgeBandCell(out IntVec3 cell, out Rot4 edge, int minInset, int maxInset)
        {
            cell = IntVec3.Invalid;
            edge = Rot4.North;
            if (map == null || map.Size.x < 24 || map.Size.z < 24)
            {
                return false;
            }

            int safeMinInset = System.Math.Max(1, minInset);
            int safeMaxInset = System.Math.Max(safeMinInset, maxInset);
            for (int i = 0; i < 12; i++)
            {
                int side = Rand.RangeInclusive(0, 3);
                int inset = Rand.RangeInclusive(safeMinInset, safeMaxInset);
                int x;
                int z;
                if (side == 0)
                {
                    x = Rand.RangeInclusive(8, map.Size.x - 9);
                    z = inset;
                    edge = Rot4.South;
                }
                else if (side == 1)
                {
                    x = Rand.RangeInclusive(8, map.Size.x - 9);
                    z = map.Size.z - 1 - inset;
                    edge = Rot4.North;
                }
                else if (side == 2)
                {
                    x = inset;
                    z = Rand.RangeInclusive(8, map.Size.z - 9);
                    edge = Rot4.West;
                }
                else
                {
                    x = map.Size.x - 1 - inset;
                    z = Rand.RangeInclusive(8, map.Size.z - 9);
                    edge = Rot4.East;
                }

                IntVec3 candidate = new IntVec3(x, 0, z);
                if (candidate.InBounds(map))
                {
                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        private static Vector3 CellToDrawPos(IntVec3 cell, float yOffset)
        {
            Vector3 pos = cell.ToVector3Shifted();
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor() + yOffset;
            return pos;
        }
    }
}
