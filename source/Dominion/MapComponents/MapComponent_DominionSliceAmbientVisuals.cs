using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_DominionSliceAmbientVisuals : MapComponent
    {
        private const string HeartDefName = "ABY_DominionSliceHeart";
        private int nextEmberTick;
        private int nextPressurePulseTick;
        private int nextEdgeSparkTick;
        private int nextCollapseWarningTick;

        public MapComponent_DominionSliceAmbientVisuals(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextEmberTick, "nextEmberTick", 0);
            Scribe_Values.Look(ref nextPressurePulseTick, "nextPressurePulseTick", 0);
            Scribe_Values.Look(ref nextEdgeSparkTick, "nextEdgeSparkTick", 0);
            Scribe_Values.Look(ref nextCollapseWarningTick, "nextCollapseWarningTick", 0);
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

            if (now >= nextEmberTick)
            {
                EmitAmbientEmbers(encounter, intensity);
                nextEmberTick = now + Mathf.RoundToInt(Rand.Range(30f, 54f) / Mathf.Max(0.55f, intensity));
            }

            if (now >= nextPressurePulseTick)
            {
                EmitPressurePulse(encounter, intensity);
                nextPressurePulseTick = now + Mathf.RoundToInt(Rand.Range(150f, 240f) / Mathf.Max(0.65f, intensity));
            }

            if (now >= nextEdgeSparkTick)
            {
                EmitEdgeSparks(encounter, intensity);
                nextEdgeSparkTick = now + Mathf.RoundToInt(Rand.Range(100f, 180f) / Mathf.Max(0.65f, intensity));
            }

            if (encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse && now >= nextCollapseWarningTick)
            {
                EmitCollapseWarning(encounter, intensity);
                nextCollapseWarningTick = now + Rand.RangeInclusive(90, 150);
            }
        }

        private static float GetPhaseIntensity(MapComponent_DominionSliceEncounter encounter)
        {
            float hazard = encounter != null ? Mathf.Clamp(encounter.HazardPressure, 0, 10) * 0.045f : 0f;
            if (encounter == null)
            {
                return 0f;
            }

            switch (encounter.CurrentPhase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    return 0.85f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return 1.10f + encounter.LiveAnchorCount * 0.08f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return 1.35f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return 1.85f + hazard;
                default:
                    return 0f;
            }
        }

        private void EmitAmbientEmbers(MapComponent_DominionSliceEncounter encounter, float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(1.5f + intensity * 1.8f), 2, 6);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell;
                if (!TryFindAmbientCell(map.Center, 48, out cell))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.01f, 0.045f));
                float scale = Rand.Range(0.45f, 0.95f) * Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(intensity / 2f));
                DominionSliceAmbientVfxUtility.SpawnAmbientEmber(pos, map, scale);
            }
        }

        private void EmitPressurePulse(MapComponent_DominionSliceEncounter encounter, float intensity)
        {
            IntVec3 center = ResolveHeartCell();
            Vector3 pos = CellToDrawPos(center, 0.03f);
            float scale = Mathf.Lerp(4.6f, 7.8f, Mathf.Clamp01(intensity / 2.1f));
            DominionSliceAmbientVfxUtility.SpawnPressurePulse(pos, map, scale);

            if (encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed || encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse)
            {
                FleckMaker.ThrowLightningGlow(pos, map, Mathf.Lerp(1.2f, 2.3f, Mathf.Clamp01(intensity / 2.2f)));
            }
        }

        private void EmitEdgeSparks(MapComponent_DominionSliceEncounter encounter, float intensity)
        {
            int count = encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse ? 3 : 1 + (Rand.Chance(0.35f) ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = RandomPeripheralCell();
                if (!cell.IsValid || !cell.InBounds(map))
                {
                    continue;
                }

                Vector3 pos = CellToDrawPos(cell, Rand.Range(0.02f, 0.05f));
                float scale = Rand.Range(0.85f, 1.45f) * Mathf.Lerp(0.9f, 1.35f, Mathf.Clamp01(intensity / 2f));
                DominionSliceAmbientVfxUtility.SpawnEdgeSpark(pos, map, scale);
            }
        }

        private void EmitCollapseWarning(MapComponent_DominionSliceEncounter encounter, float intensity)
        {
            IntVec3 center = ResolveHeartCell();
            DominionSliceAmbientVfxUtility.SpawnPressurePulse(CellToDrawPos(center, 0.055f), map, Mathf.Lerp(7.2f, 9.5f, Mathf.Clamp01(intensity / 2.4f)));
            FleckMaker.ThrowLightningGlow(CellToDrawPos(center, 0.07f), map, 2.6f);
        }

        private bool TryFindAmbientCell(IntVec3 focus, int radius, out IntVec3 cell)
        {
            return CellFinder.TryFindRandomCellNear(focus, map, radius, c => c.InBounds(map) && c.Standable(map) && !c.Fogged(map), out cell);
        }

        private IntVec3 ResolveHeartCell()
        {
            ThingDef heartDef = DefDatabase<ThingDef>.GetNamedSilentFail(HeartDefName);
            if (heartDef != null && map.listerThings != null)
            {
                System.Collections.Generic.List<Thing> hearts = map.listerThings.ThingsOfDef(heartDef);
                if (hearts != null && hearts.Count > 0 && hearts[0] != null && !hearts[0].Destroyed)
                {
                    return hearts[0].PositionHeld;
                }
            }

            return map.Center;
        }

        private IntVec3 RandomPeripheralCell()
        {
            IntVec3 center = map.Center;
            float angle = Rand.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Rand.Range(39f, 52f);
            int x = center.x + GenMath.RoundRandom(Mathf.Cos(angle) * radius);
            int z = center.z + GenMath.RoundRandom(Mathf.Sin(angle) * radius);
            x = Mathf.Clamp(x, 8, map.Size.x - 9);
            z = Mathf.Clamp(z, 8, map.Size.z - 9);
            return new IntVec3(x, 0, z);
        }

        private static Vector3 CellToDrawPos(IntVec3 cell, float altitudeOffset)
        {
            return new Vector3(cell.x + 0.5f, AltitudeLayer.MoteOverhead.AltitudeFor() + altitudeOffset, cell.z + 0.5f);
        }
    }
}
