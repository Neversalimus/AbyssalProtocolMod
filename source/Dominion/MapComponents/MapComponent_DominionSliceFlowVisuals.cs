using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_DominionSliceFlowVisuals : MapComponent
    {
        private int nextEntryFlowTick;
        private int nextAnchorFlowTick;
        private int nextHeartFlowTick;
        private int nextCollapseFlowTick;
        private int nextRewardFlowTick;
        private int nextNodeTick;

        public MapComponent_DominionSliceFlowVisuals(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextEntryFlowTick, "nextEntryFlowTick", 0);
            Scribe_Values.Look(ref nextAnchorFlowTick, "nextAnchorFlowTick", 0);
            Scribe_Values.Look(ref nextHeartFlowTick, "nextHeartFlowTick", 0);
            Scribe_Values.Look(ref nextCollapseFlowTick, "nextCollapseFlowTick", 0);
            Scribe_Values.Look(ref nextRewardFlowTick, "nextRewardFlowTick", 0);
            Scribe_Values.Look(ref nextNodeTick, "nextNodeTick", 0);
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

            ABY_DominionPocketSession session = ResolveSession();
            int now = Find.TickManager.TicksGame;
            MapComponent_DominionSliceEncounter.SlicePhase phase = encounter.CurrentPhase;

            IntVec3 heart = ResolveHeartCell(encounter, session);
            IntVec3 entry = ResolveEntryCell(session);
            IntVec3 extraction = ResolveExtractionCell(session);
            IntVec3 reward = ResolveRewardPocketCell(session);
            float intensity = GetPhaseIntensity(encounter, session);

            if (now >= nextNodeTick)
            {
                DominionSliceFlowVfxUtility.SpawnFlowNode(heart, map, intensity);
                if (phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse && extraction.IsValid)
                {
                    DominionSliceFlowVfxUtility.SpawnFlowNode(extraction, map, intensity * 1.15f);
                }

                nextNodeTick = now + GetNodeInterval(phase);
            }

            if (phase == MapComponent_DominionSliceEncounter.SlicePhase.Breach)
            {
                if (now >= nextEntryFlowTick)
                {
                    DominionSliceFlowVfxUtility.SpawnFlowLine(entry, heart, map, intensity * 0.72f, false, 4);
                    nextEntryFlowTick = now + Rand.RangeInclusive(170, 240);
                }

                return;
            }

            if (phase == MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall)
            {
                if (now >= nextAnchorFlowTick)
                {
                    EmitAnchorFlows(session, heart, intensity);
                    nextAnchorFlowTick = now + Rand.RangeInclusive(85, 125);
                }

                if (now >= nextEntryFlowTick)
                {
                    DominionSliceFlowVfxUtility.SpawnFlowLine(entry, heart, map, intensity * 0.55f, false, 3);
                    nextEntryFlowTick = now + Rand.RangeInclusive(190, 270);
                }

                return;
            }

            if (phase == MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed)
            {
                if (now >= nextHeartFlowTick)
                {
                    DominionSliceFlowVfxUtility.SpawnFlowSurge(heart, map, intensity);
                    DominionSliceFlowVfxUtility.SpawnRadialFlow(heart, map, intensity, 7, 12f);
                    nextHeartFlowTick = now + Rand.RangeInclusive(95, 140);
                }

                return;
            }

            if (phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse)
            {
                if (now >= nextCollapseFlowTick)
                {
                    if (extraction.IsValid)
                    {
                        DominionSliceFlowVfxUtility.SpawnFlowLine(heart, extraction, map, intensity * 1.25f, true, 7);
                        DominionSliceFlowVfxUtility.SpawnFlowSurge(extraction, map, intensity * 1.15f);
                    }

                    nextCollapseFlowTick = now + Rand.RangeInclusive(58, 92);
                }

                if (session != null && session.victoryAchieved && now >= nextRewardFlowTick)
                {
                    if (reward.IsValid && extraction.IsValid)
                    {
                        DominionSliceFlowVfxUtility.SpawnFlowLine(reward, extraction, map, intensity * 0.95f, true, 5);
                        DominionSliceFlowVfxUtility.SpawnFlowNode(reward, map, intensity * 0.72f);
                    }

                    nextRewardFlowTick = now + Rand.RangeInclusive(125, 185);
                }
            }
        }

        private void EmitAnchorFlows(ABY_DominionPocketSession session, IntVec3 heartCell, float intensity)
        {
            if (session == null || session.anchorCells == null || session.anchorCells.Count == 0)
            {
                return;
            }

            for (int i = 0; i < session.anchorCells.Count; i++)
            {
                IntVec3 anchorCell = ClampToMap(session.anchorCells[i]);
                if (!anchorCell.IsValid)
                {
                    continue;
                }

                DominionSliceFlowVfxUtility.SpawnFlowLine(anchorCell, heartCell, map, intensity, false, 5);
                DominionSliceFlowVfxUtility.SpawnFlowNode(anchorCell, map, intensity * 0.82f);
            }
        }

        private ABY_DominionPocketSession ResolveSession()
        {
            ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
            if (runtime == null)
            {
                return null;
            }

            ABY_DominionPocketSession session;
            return runtime.TryGetSessionByPocketMap(map, out session) ? session : null;
        }

        private IntVec3 ResolveHeartCell(MapComponent_DominionSliceEncounter encounter, ABY_DominionPocketSession session)
        {
            Building_ABY_DominionSliceHeart heart = encounter != null ? encounter.HeartBuilding : null;
            if (heart != null && !heart.Destroyed)
            {
                return heart.PositionHeld;
            }

            if (session != null && session.heartCell.IsValid)
            {
                return ClampToMap(session.heartCell);
            }

            return map != null ? map.Center : IntVec3.Invalid;
        }

        private IntVec3 ResolveEntryCell(ABY_DominionPocketSession session)
        {
            if (session != null && session.pocketEntryCell.IsValid)
            {
                return ClampToMap(session.pocketEntryCell);
            }

            return map != null ? ClampToMap(map.Center + new IntVec3(0, 0, -38)) : IntVec3.Invalid;
        }

        private IntVec3 ResolveExtractionCell(ABY_DominionPocketSession session)
        {
            if (session != null && session.extractionCell.IsValid)
            {
                return ClampToMap(session.extractionCell);
            }

            return map != null ? ClampToMap(map.Center + new IntVec3(0, 0, -35)) : IntVec3.Invalid;
        }

        private IntVec3 ResolveRewardPocketCell(ABY_DominionPocketSession session)
        {
            if (session != null && session.heartCell.IsValid)
            {
                return ClampToMap(session.heartCell + new IntVec3(-36, 0, -9));
            }

            return map != null ? ClampToMap(map.Center + new IntVec3(-36, 0, -9)) : IntVec3.Invalid;
        }

        private IntVec3 ClampToMap(IntVec3 cell)
        {
            if (map == null || !cell.IsValid)
            {
                return IntVec3.Invalid;
            }

            int x = System.Math.Max(6, System.Math.Min(map.Size.x - 7, cell.x));
            int z = System.Math.Max(6, System.Math.Min(map.Size.z - 7, cell.z));
            return new IntVec3(x, 0, z);
        }

        private static float GetPhaseIntensity(MapComponent_DominionSliceEncounter encounter, ABY_DominionPocketSession session)
        {
            if (encounter == null)
            {
                return 0.65f;
            }

            float hazard = Mathf.Clamp(encounter.HazardPressure, 0, 10) * 0.035f;
            switch (encounter.CurrentPhase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    return 0.70f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return 1.00f + encounter.LiveAnchorCount * 0.06f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return 1.30f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return 1.65f + hazard + (session != null && session.victoryAchieved ? 0.18f : 0f);
                default:
                    return 0.65f;
            }
        }

        private static int GetNodeInterval(MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            switch (phase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return Rand.RangeInclusive(75, 110);
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return Rand.RangeInclusive(100, 145);
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return Rand.RangeInclusive(115, 170);
                default:
                    return Rand.RangeInclusive(190, 260);
            }
        }
    }
}
