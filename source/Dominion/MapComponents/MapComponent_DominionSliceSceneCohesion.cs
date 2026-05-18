using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_DominionSliceSceneCohesion : MapComponent
    {
        private MapComponent_DominionSliceEncounter.SlicePhase lastPhase = MapComponent_DominionSliceEncounter.SlicePhase.Dormant;
        private int nextHeartCohesionTick;
        private int nextAxisCohesionTick;
        private int nextEdgeCohesionTick;
        private int nextPhaseSealTick;
        private int nextQuietEmberTick;

        public MapComponent_DominionSliceSceneCohesion(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastPhase, "lastPhase", MapComponent_DominionSliceEncounter.SlicePhase.Dormant);
            Scribe_Values.Look(ref nextHeartCohesionTick, "nextHeartCohesionTick", 0);
            Scribe_Values.Look(ref nextAxisCohesionTick, "nextAxisCohesionTick", 0);
            Scribe_Values.Look(ref nextEdgeCohesionTick, "nextEdgeCohesionTick", 0);
            Scribe_Values.Look(ref nextPhaseSealTick, "nextPhaseSealTick", 0);
            Scribe_Values.Look(ref nextQuietEmberTick, "nextQuietEmberTick", 0);
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
            MapComponent_DominionSliceEncounter.SlicePhase phase = encounter.CurrentPhase;
            ABY_DominionPocketSession session = ResolveSession();

            if (phase != lastPhase)
            {
                lastPhase = phase;
                ResetPhaseTimers(now, phase);
                SpawnPhaseTransitionSignature(encounter, session, phase);
            }

            float intensity = GetIntensity(encounter, session);
            IntVec3 heartCell = ResolveHeartCell(encounter, session);
            IntVec3 entryCell = ResolveEntryCell(session);
            IntVec3 extractionCell = ResolveExtractionCell(session);
            IntVec3 rewardCell = ResolveRewardCell(session);

            if (now >= nextHeartCohesionTick)
            {
                DominionSliceSceneCohesionVfxUtility.SpawnHeartCohesionHalo(heartCell, map, intensity, phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse);
                nextHeartCohesionTick = now + GetHeartInterval(phase);
            }

            if (now >= nextAxisCohesionTick)
            {
                SpawnAxisCohesion(phase, heartCell, entryCell, extractionCell, rewardCell, session, intensity);
                nextAxisCohesionTick = now + GetAxisInterval(phase);
            }

            if (now >= nextEdgeCohesionTick)
            {
                DominionSliceSceneCohesionVfxUtility.SpawnSubtleEdgeCohesion(map, intensity, phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse);
                nextEdgeCohesionTick = now + GetEdgeInterval(phase);
            }

            if (now >= nextPhaseSealTick)
            {
                DominionSliceSceneCohesionVfxUtility.SpawnCrownSeal(heartCell, map, intensity, encounter.LiveAnchorCount);
                nextPhaseSealTick = now + GetSealInterval(phase);
            }

            if (now >= nextQuietEmberTick)
            {
                DominionSliceSceneCohesionVfxUtility.SpawnQuietEmbers(map, heartCell, intensity, phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse);
                nextQuietEmberTick = now + GetEmberInterval(phase);
            }
        }

        private void ResetPhaseTimers(int now, MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            nextHeartCohesionTick = now + 15;
            nextAxisCohesionTick = now + 45;
            nextEdgeCohesionTick = now + 90;
            nextPhaseSealTick = now + (phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse ? 55 : 140);
            nextQuietEmberTick = now + 75;
        }

        private void SpawnPhaseTransitionSignature(MapComponent_DominionSliceEncounter encounter, ABY_DominionPocketSession session, MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            IntVec3 heartCell = ResolveHeartCell(encounter, session);
            float intensity = GetIntensity(encounter, session);
            DominionSliceSceneCohesionVfxUtility.SpawnPhaseTransitionSeal(heartCell, map, intensity, phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse);
        }

        private void SpawnAxisCohesion(
            MapComponent_DominionSliceEncounter.SlicePhase phase,
            IntVec3 heartCell,
            IntVec3 entryCell,
            IntVec3 extractionCell,
            IntVec3 rewardCell,
            ABY_DominionPocketSession session,
            float intensity)
        {
            if (phase == MapComponent_DominionSliceEncounter.SlicePhase.Breach)
            {
                DominionSliceSceneCohesionVfxUtility.SpawnAxisAccent(entryCell, heartCell, map, intensity * 0.62f, 4);
                return;
            }

            if (phase == MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall)
            {
                SpawnAnchorAxisAccents(session, heartCell, intensity);
                return;
            }

            if (phase == MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed)
            {
                DominionSliceSceneCohesionVfxUtility.SpawnRadialCohesion(heartCell, map, intensity, 6);
                return;
            }

            if (phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse)
            {
                DominionSliceSceneCohesionVfxUtility.SpawnCollapseVeil(heartCell, extractionCell, map, intensity);
                DominionSliceSceneCohesionVfxUtility.SpawnAxisAccent(heartCell, extractionCell, map, intensity * 1.15f, 7);
                if (session != null && session.victoryAchieved && rewardCell.IsValid)
                {
                    DominionSliceSceneCohesionVfxUtility.SpawnAxisAccent(rewardCell, extractionCell, map, intensity * 0.92f, 5);
                }
            }
        }

        private void SpawnAnchorAxisAccents(ABY_DominionPocketSession session, IntVec3 heartCell, float intensity)
        {
            if (session == null || session.anchorCells == null)
            {
                return;
            }

            int count = session.anchorCells.Count;
            for (int i = 0; i < count; i++)
            {
                IntVec3 anchorCell = ClampToMap(session.anchorCells[i]);
                if (anchorCell.IsValid)
                {
                    DominionSliceSceneCohesionVfxUtility.SpawnAxisAccent(anchorCell, heartCell, map, intensity * 0.78f, 4);
                }
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

        private IntVec3 ResolveRewardCell(ABY_DominionPocketSession session)
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

        private static float GetIntensity(MapComponent_DominionSliceEncounter encounter, ABY_DominionPocketSession session)
        {
            if (encounter == null)
            {
                return 0.60f;
            }

            float hazard = Mathf.Clamp(encounter.HazardPressure, 0, 10) * 0.025f;
            switch (encounter.CurrentPhase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Breach:
                    return 0.62f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return 0.82f + hazard + encounter.LiveAnchorCount * 0.025f;
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return 1.02f + hazard;
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return 1.24f + hazard + (session != null && session.victoryAchieved ? 0.10f : 0f);
                default:
                    return 0.60f;
            }
        }

        private static int GetHeartInterval(MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            switch (phase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return Rand.RangeInclusive(160, 220);
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return Rand.RangeInclusive(200, 280);
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return Rand.RangeInclusive(260, 340);
                default:
                    return Rand.RangeInclusive(360, 460);
            }
        }

        private static int GetAxisInterval(MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            switch (phase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return Rand.RangeInclusive(135, 190);
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return Rand.RangeInclusive(210, 290);
                case MapComponent_DominionSliceEncounter.SlicePhase.Anchorfall:
                    return Rand.RangeInclusive(180, 260);
                default:
                    return Rand.RangeInclusive(300, 420);
            }
        }

        private static int GetEdgeInterval(MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            return phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse ? Rand.RangeInclusive(230, 330) : Rand.RangeInclusive(420, 560);
        }

        private static int GetSealInterval(MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            switch (phase)
            {
                case MapComponent_DominionSliceEncounter.SlicePhase.Collapse:
                    return Rand.RangeInclusive(300, 420);
                case MapComponent_DominionSliceEncounter.SlicePhase.HeartExposed:
                    return Rand.RangeInclusive(420, 600);
                default:
                    return Rand.RangeInclusive(620, 820);
            }
        }

        private static int GetEmberInterval(MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            return phase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse ? Rand.RangeInclusive(110, 150) : Rand.RangeInclusive(190, 260);
        }
    }
}
