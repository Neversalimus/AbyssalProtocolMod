using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_DominionSliceCollapseSpectacle : MapComponent
    {
        private MapComponent_DominionSliceEncounter.SlicePhase lastPhase = MapComponent_DominionSliceEncounter.SlicePhase.Dormant;
        private int nextShockwaveTick;
        private int nextExtractionGlowTick;
        private int nextRewardGlowTick;
        private int nextEdgeInstabilityTick;
        private int nextWarningPulseTick;
        private int nextExtractionGuideTick;
        private int nextRewardGuideTick;
        private bool collapseStartBurstDone;

        public MapComponent_DominionSliceCollapseSpectacle(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastPhase, "lastPhase", MapComponent_DominionSliceEncounter.SlicePhase.Dormant);
            Scribe_Values.Look(ref nextShockwaveTick, "nextShockwaveTick", 0);
            Scribe_Values.Look(ref nextExtractionGlowTick, "nextExtractionGlowTick", 0);
            Scribe_Values.Look(ref nextRewardGlowTick, "nextRewardGlowTick", 0);
            Scribe_Values.Look(ref nextEdgeInstabilityTick, "nextEdgeInstabilityTick", 0);
            Scribe_Values.Look(ref nextWarningPulseTick, "nextWarningPulseTick", 0);
            Scribe_Values.Look(ref nextExtractionGuideTick, "nextExtractionGuideTick", 0);
            Scribe_Values.Look(ref nextRewardGuideTick, "nextRewardGuideTick", 0);
            Scribe_Values.Look(ref collapseStartBurstDone, "collapseStartBurstDone", false);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Find.TickManager == null || map == null)
            {
                return;
            }

            MapComponent_DominionSliceEncounter encounter = map.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter == null)
            {
                return;
            }

            MapComponent_DominionSliceEncounter.SlicePhase phase = encounter.CurrentPhase;
            if (phase != lastPhase)
            {
                NotifyPhaseChanged(encounter, phase);
                lastPhase = phase;
            }

            if (phase != MapComponent_DominionSliceEncounter.SlicePhase.Collapse)
            {
                collapseStartBurstDone = false;
                return;
            }

            TickCollapseSpectacle(encounter);
        }

        private void NotifyPhaseChanged(MapComponent_DominionSliceEncounter encounter, MapComponent_DominionSliceEncounter.SlicePhase phase)
        {
            if (phase != MapComponent_DominionSliceEncounter.SlicePhase.Collapse)
            {
                return;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextShockwaveTick = now;
            nextExtractionGlowTick = now + 35;
            nextRewardGlowTick = now + 80;
            nextEdgeInstabilityTick = now + 70;
            nextWarningPulseTick = now + 180;
            nextExtractionGuideTick = now + 55;
            nextRewardGuideTick = now + 120;
            collapseStartBurstDone = false;

            ABY_DominionPocketSession session = ResolveSession();
            IntVec3 heartCell = ResolveHeartCell(encounter, session);
            DominionSliceCollapseSpectacleVfxUtility.SpawnCollapseStartBurst(heartCell, map);
            collapseStartBurstDone = true;
        }

        private void TickCollapseSpectacle(MapComponent_DominionSliceEncounter encounter)
        {
            int now = Find.TickManager.TicksGame;
            ABY_DominionPocketSession session = ResolveSession();

            if (!collapseStartBurstDone)
            {
                DominionSliceCollapseSpectacleVfxUtility.SpawnCollapseStartBurst(ResolveHeartCell(encounter, session), map);
                collapseStartBurstDone = true;
            }

            int remaining = session != null && session.collapseAtTick > 0 ? session.collapseAtTick - now : 3600;
            float urgency = GetUrgency(remaining);
            IntVec3 heartCell = ResolveHeartCell(encounter, session);
            IntVec3 extraction = ResolveExtractionCell(session);
            IntVec3 reward = ResolveRewardPocketCell(session);
            bool victory = session != null && session.victoryAchieved;

            if (now >= nextShockwaveTick)
            {
                DominionSliceCollapseSpectacleVfxUtility.SpawnHeartShockwave(heartCell, map, urgency);
                nextShockwaveTick = now + (urgency >= 0.75f ? 210 : 330);
            }

            if (now >= nextExtractionGlowTick)
            {
                if (extraction.IsValid)
                {
                    DominionSliceCollapseSpectacleVfxUtility.SpawnExtractionBeacon(extraction, map, urgency);
                }

                nextExtractionGlowTick = now + (urgency >= 0.75f ? 72 : 118);
            }

            if (now >= nextExtractionGuideTick)
            {
                if (extraction.IsValid)
                {
                    DominionSliceCollapseSpectacleVfxUtility.SpawnExtractionGuidance(heartCell, extraction, map, urgency);
                }

                nextExtractionGuideTick = now + (urgency >= 0.75f ? 145 : 230);
            }

            if (victory && now >= nextRewardGlowTick)
            {
                if (reward.IsValid)
                {
                    DominionSliceCollapseSpectacleVfxUtility.SpawnRewardBeacon(reward, map, urgency);
                }

                nextRewardGlowTick = now + (urgency >= 0.75f ? 135 : 210);
            }

            if (victory && now >= nextRewardGuideTick)
            {
                if (reward.IsValid && extraction.IsValid)
                {
                    DominionSliceCollapseSpectacleVfxUtility.SpawnRewardGuidance(reward, extraction, map, urgency);
                }

                nextRewardGuideTick = now + (urgency >= 0.75f ? 170 : 260);
            }

            if (now >= nextEdgeInstabilityTick)
            {
                DominionSliceCollapseSpectacleVfxUtility.SpawnEdgeInstability(map, urgency);
                nextEdgeInstabilityTick = now + (urgency >= 0.75f ? 55 : 95);
            }

            if (now >= nextWarningPulseTick)
            {
                DominionSliceCollapseSpectacleVfxUtility.SpawnCollapseWarningPulse(heartCell, map, urgency);
                nextWarningPulseTick = now + (urgency >= 0.75f ? 240 : 420);
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
                return session.heartCell;
            }

            return map != null ? map.Center : IntVec3.Invalid;
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
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            int x = System.Math.Max(6, System.Math.Min(map.Size.x - 7, cell.x));
            int z = System.Math.Max(6, System.Math.Min(map.Size.z - 7, cell.z));
            return new IntVec3(x, 0, z);
        }

        private static float GetUrgency(int remainingTicks)
        {
            if (remainingTicks <= 0)
            {
                return 1f;
            }

            if (remainingTicks <= 600)
            {
                return 1f;
            }

            if (remainingTicks <= 1200)
            {
                return 0.82f;
            }

            if (remainingTicks <= 2100)
            {
                return 0.66f;
            }

            return 0.48f;
        }
    }
}
