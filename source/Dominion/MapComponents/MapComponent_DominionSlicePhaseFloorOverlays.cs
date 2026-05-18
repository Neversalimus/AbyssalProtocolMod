using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_DominionSlicePhaseFloorOverlays : MapComponent
    {
        private const string HeartDefName = "ABY_DominionSliceHeart";
        private int nextPhaseRingTick;
        private int nextConduitPulseTick;
        private int nextAnchorGlyphTick;
        private int nextHeartCrackTick;

        public MapComponent_DominionSlicePhaseFloorOverlays(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextPhaseRingTick, "nextPhaseRingTick", 0);
            Scribe_Values.Look(ref nextConduitPulseTick, "nextConduitPulseTick", 0);
            Scribe_Values.Look(ref nextAnchorGlyphTick, "nextAnchorGlyphTick", 0);
            Scribe_Values.Look(ref nextHeartCrackTick, "nextHeartCrackTick", 0);
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
            IntVec3 heartCell = ResolveHeartCell(encounter);

            if (now >= nextPhaseRingTick)
            {
                EmitPhaseRings(encounter, heartCell, intensity);
                nextPhaseRingTick = now + GetPhaseRingInterval(encounter);
            }

            if (now >= nextConduitPulseTick)
            {
                EmitConduitPulses(encounter, heartCell, intensity);
                nextConduitPulseTick = now + GetConduitPulseInterval(encounter);
            }

            if (encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall && now >= nextAnchorGlyphTick)
            {
                EmitAnchorGlyphs(encounter, intensity);
                nextAnchorGlyphTick = now + Rand.RangeInclusive(140, 210);
            }

            if ((encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed || encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse) && now >= nextHeartCrackTick)
            {
                EmitHeartFloorCracks(heartCell, intensity, encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse);
                nextHeartCrackTick = now + (encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse ? Rand.RangeInclusive(70, 105) : Rand.RangeInclusive(115, 165));
            }
        }

        private static float GetPhaseIntensity(MapComponent_DominionSliceEncounter encounter)
        {
            if (encounter == null)
            {
                return 0f;
            }

            float hazard = Mathf.Clamp(encounter.HazardPressure, 0, 10) * 0.045f;
            switch (encounter.CurrentPhase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    return 0.75f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return 1.10f + encounter.LiveAnchorCount * 0.07f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return 1.45f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return 1.95f + hazard;
                default:
                    return 0f;
            }
        }

        private static int GetPhaseRingInterval(MapComponent_DominionSliceEncounter encounter)
        {
            if (encounter == null)
            {
                return 240;
            }

            switch (encounter.CurrentPhase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return Rand.RangeInclusive(90, 130);
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return Rand.RangeInclusive(120, 170);
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return Rand.RangeInclusive(160, 220);
                default:
                    return Rand.RangeInclusive(210, 300);
            }
        }

        private static int GetConduitPulseInterval(MapComponent_DominionSliceEncounter encounter)
        {
            if (encounter == null)
            {
                return 240;
            }

            switch (encounter.CurrentPhase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return Rand.RangeInclusive(70, 105);
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return Rand.RangeInclusive(110, 155);
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return Rand.RangeInclusive(95, 145);
                default:
                    return Rand.RangeInclusive(180, 260);
            }
        }

        private void EmitPhaseRings(MapComponent_DominionSliceEncounter encounter, IntVec3 heartCell, float intensity)
        {
            if (!heartCell.IsValid)
            {
                heartCell = map.Center;
            }

            switch (encounter.CurrentPhase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    DominionSlicePhaseFloorVfxUtility.SpawnPhaseRing(heartCell, map, Mathf.Lerp(4.5f, 5.8f, Mathf.Clamp01(intensity / 1.4f)));
                    break;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    DominionSlicePhaseFloorVfxUtility.SpawnPhaseRing(heartCell, map, Mathf.Lerp(5.2f, 7.2f, Mathf.Clamp01(intensity / 1.7f)));
                    List<Building_ABY_DominionSliceAnchor> anchors = ResolveAnchors();
                    for (int i = 0; i < anchors.Count; i++)
                    {
                        Building_ABY_DominionSliceAnchor anchor = anchors[i];
                        if (anchor != null && !anchor.Destroyed)
                        {
                            DominionSlicePhaseFloorVfxUtility.SpawnPhaseRing(anchor.PositionHeld, map, 3.0f + intensity * 0.65f);
                        }
                    }
                    break;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    DominionSlicePhaseFloorVfxUtility.SpawnPhaseRing(heartCell, map, 7.2f + intensity * 1.1f);
                    DominionSlicePhaseFloorVfxUtility.SpawnPhaseRing(heartCell + new IntVec3(0, 0, 1), map, 4.2f + intensity * 0.55f);
                    break;
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    DominionSlicePhaseFloorVfxUtility.SpawnPhaseRing(heartCell, map, 8.8f + intensity * 1.1f);
                    DominionSlicePhaseFloorVfxUtility.SpawnPhaseRing(map.Center, map, 10.5f);
                    break;
            }
        }

        private void EmitConduitPulses(MapComponent_DominionSliceEncounter encounter, IntVec3 heartCell, float intensity)
        {
            if (!heartCell.IsValid)
            {
                heartCell = map.Center;
            }

            List<Building_ABY_DominionSliceAnchor> anchors = ResolveAnchors();
            if (anchors.Count > 0)
            {
                for (int i = 0; i < anchors.Count; i++)
                {
                    Building_ABY_DominionSliceAnchor anchor = anchors[i];
                    if (anchor == null || anchor.Destroyed)
                    {
                        continue;
                    }

                    EmitPulseLine(anchor.PositionHeld, heartCell, intensity, encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall ? 5 : 3);
                }
            }

            if (encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Breach)
            {
                EmitPulseLine(map.Center + new IntVec3(0, 0, -36), heartCell + new IntVec3(0, 0, -12), intensity * 0.75f, 4);
            }
            else if (encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed)
            {
                EmitRadialHeartPulses(heartCell, intensity, 6, 9f);
            }
            else if (encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse)
            {
                EmitRadialHeartPulses(heartCell, intensity, 8, 13f);
            }
        }

        private void EmitAnchorGlyphs(MapComponent_DominionSliceEncounter encounter, float intensity)
        {
            List<Building_ABY_DominionSliceAnchor> anchors = ResolveAnchors();
            for (int i = 0; i < anchors.Count; i++)
            {
                Building_ABY_DominionSliceAnchor anchor = anchors[i];
                if (anchor == null || anchor.Destroyed)
                {
                    continue;
                }

                float scale = 3.6f + intensity * 0.75f;
                DominionSlicePhaseFloorVfxUtility.SpawnAnchorGlyph(anchor.PositionHeld, map, scale);
            }
        }

        private void EmitHeartFloorCracks(IntVec3 heartCell, float intensity, bool collapse)
        {
            int count = collapse ? Rand.RangeInclusive(5, 8) : Rand.RangeInclusive(3, 5);
            float maxRadius = collapse ? 12.5f : 8.0f;
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i + Rand.Range(-16f, 16f);
                float rad = angle * Mathf.Deg2Rad;
                float radius = Rand.Range(2.2f, maxRadius);
                IntVec3 cell = new IntVec3(
                    heartCell.x + GenMath.RoundRandom(Mathf.Cos(rad) * radius),
                    0,
                    heartCell.z + GenMath.RoundRandom(Mathf.Sin(rad) * radius));

                if (!cell.InBounds(map))
                {
                    continue;
                }

                DominionSlicePhaseFloorVfxUtility.SpawnHeartCrack(cell, map, Rand.Range(1.15f, 2.4f) * Mathf.Lerp(0.9f, 1.35f, Mathf.Clamp01(intensity / 2.2f)));
            }
        }

        private void EmitRadialHeartPulses(IntVec3 heartCell, float intensity, int arms, float radius)
        {
            for (int i = 0; i < arms; i++)
            {
                float angle = (360f / arms) * i + Rand.Range(-7f, 7f);
                float rad = angle * Mathf.Deg2Rad;
                IntVec3 end = new IntVec3(
                    heartCell.x + GenMath.RoundRandom(Mathf.Cos(rad) * radius),
                    0,
                    heartCell.z + GenMath.RoundRandom(Mathf.Sin(rad) * radius));
                EmitPulseLine(heartCell, end, intensity, 3);
            }
        }

        private void EmitPulseLine(IntVec3 from, IntVec3 to, float intensity, int samples)
        {
            int safeSamples = Mathf.Clamp(samples, 2, 8);
            for (int i = 1; i <= safeSamples; i++)
            {
                float t = i / (float)(safeSamples + 1);
                int x = GenMath.RoundRandom(Mathf.Lerp(from.x, to.x, t));
                int z = GenMath.RoundRandom(Mathf.Lerp(from.z, to.z, t));
                IntVec3 cell = new IntVec3(x, 0, z);
                if (!cell.InBounds(map))
                {
                    continue;
                }

                float scale = Rand.Range(0.8f, 1.25f) * Mathf.Lerp(0.85f, 1.45f, Mathf.Clamp01(intensity / 2f));
                DominionSlicePhaseFloorVfxUtility.SpawnConduitPulse(cell, map, scale);
            }
        }

        private IntVec3 ResolveHeartCell(MapComponent_DominionSliceEncounter encounter)
        {
            if (encounter != null)
            {
                Building_ABY_DominionSliceHeart heart = encounter.HeartBuilding;
                if (heart != null && !heart.Destroyed)
                {
                    return heart.PositionHeld;
                }
            }

            ThingDef heartDef = DefDatabase<ThingDef>.GetNamedSilentFail(HeartDefName);
            if (heartDef != null && map.listerThings != null)
            {
                List<Thing> hearts = map.listerThings.ThingsOfDef(heartDef);
                if (hearts != null && hearts.Count > 0 && hearts[0] != null && !hearts[0].Destroyed)
                {
                    return hearts[0].PositionHeld;
                }
            }

            return map.Center;
        }

        private List<Building_ABY_DominionSliceAnchor> ResolveAnchors()
        {
            List<Building_ABY_DominionSliceAnchor> result = new List<Building_ABY_DominionSliceAnchor>();
            if (map?.listerThings?.AllThings == null)
            {
                return result;
            }

            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Building_ABY_DominionSliceAnchor anchor = things[i] as Building_ABY_DominionSliceAnchor;
                if (anchor != null && !anchor.Destroyed && anchor.Spawned && anchor.Map == map)
                {
                    result.Add(anchor);
                }
            }

            return result;
        }
    }
}
