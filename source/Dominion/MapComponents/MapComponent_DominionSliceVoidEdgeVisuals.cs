using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_DominionSliceVoidEdgeVisuals : MapComponent
    {
        private int nextVoidVeilTick;
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
            Scribe_Values.Look(ref nextVoidVeilTick, "nextVoidVeilTick", 0);
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

            if (now >= nextVoidVeilTick)
            {
                EmitVoidVeil(intensity);
                nextVoidVeilTick = now + Mathf.RoundToInt(Rand.Range(135f, 225f) / Mathf.Max(0.70f, intensity));
            }

            if (now >= nextRimPulseTick)
            {
                EmitVoidRimPulses(intensity);
                nextRimPulseTick = now + Mathf.RoundToInt(Rand.Range(85f, 145f) / Mathf.Max(0.70f, intensity));
            }

            if (now >= nextVoidCrackTick)
            {
                EmitVoidCracks(intensity);
                nextVoidCrackTick = now + Mathf.RoundToInt(Rand.Range(115f, 190f) / Mathf.Max(0.70f, intensity));
            }

            if (now >= nextBoundaryRiftTick)
            {
                EmitBoundaryRifts(intensity);
                nextBoundaryRiftTick = now + Mathf.RoundToInt(Rand.Range(220f, 380f) / Mathf.Max(0.70f, intensity));
            }

            if (now >= nextShardTick)
            {
                EmitEdgeShards(intensity);
                nextShardTick = now + Mathf.RoundToInt(Rand.Range(125f, 230f) / Mathf.Max(0.70f, intensity));
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
                    return 0.78f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return 1.02f + encounter.LiveAnchorCount * 0.055f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return 1.30f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return 1.92f + hazard;
                default:
                    return 0f;
            }
        }

        private void EmitVoidVeil(float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(2f + intensity * 1.45f), 3, 6);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                Rot4 edge;
                if (!TryFindEdgeBandCell(out cell, out edge, 4, 10))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.002f, 0.010f));
                float scale = Rand.Range(2.75f, 4.35f) * Mathf.Lerp(0.88f, 1.35f, Mathf.Clamp01(intensity - 0.65f));
                DominionSliceVoidEdgeVfxUtility.SpawnVoidVeil(pos, map, scale);
            }
        }

        private void EmitVoidRimPulses(float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(3f + intensity * 2.25f), 4, 8);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                Rot4 edge;
                if (!TryFindEdgeBandCell(out cell, out edge, 5, 12))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.012f, 0.038f));
                float scale = Rand.Range(1.75f, 3.15f) * Mathf.Lerp(0.90f, 1.36f, Mathf.Clamp01(intensity - 0.6f));
                DominionSliceVoidEdgeVfxUtility.SpawnVoidRim(pos, map, scale);
            }
        }

        private void EmitVoidCracks(float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(1.25f + intensity * 1.75f), 2, 5);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                Rot4 edge;
                if (!TryFindEdgeBandCell(out cell, out edge, 6, 15))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.018f, 0.050f));
                float scale = Rand.Range(1.05f, 2.15f) * Mathf.Lerp(0.95f, 1.42f, Mathf.Clamp01(intensity - 0.55f));
                DominionSliceVoidEdgeVfxUtility.SpawnVoidCrack(pos, map, scale);
            }
        }

        private void EmitBoundaryRifts(float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(0.75f + intensity * 1.15f), 1, 4);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                Rot4 edge;
                if (!TryFindEdgeBandCell(out cell, out edge, 6, 13))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.026f, 0.060f));
                float scale = Rand.Range(1.65f, 3.05f) * Mathf.Lerp(0.95f, 1.48f, Mathf.Clamp01(intensity - 0.6f));
                DominionSliceVoidEdgeVfxUtility.SpawnBoundaryRift(pos, map, scale);
            }
        }

        private void EmitEdgeShards(float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(1.15f + intensity * 1.25f), 2, 5);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                Rot4 edge;
                if (!TryFindEdgeBandCell(out cell, out edge, 7, 17))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.030f, 0.065f));
                float scale = Rand.Range(0.80f, 1.55f) * Mathf.Lerp(0.90f, 1.42f, Mathf.Clamp01(intensity - 0.5f));
                DominionSliceVoidEdgeVfxUtility.SpawnEdgeShard(pos, map, scale);
            }
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
