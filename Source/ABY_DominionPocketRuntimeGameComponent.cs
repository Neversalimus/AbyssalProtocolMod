using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_DominionPocketRuntimeGameComponent : GameComponent
    {
        private const int MaintenanceIntervalTicks = 180;

        private int nextMaintenanceTick;
        private bool dominionHeartDestroyedLoreLetterSent;
        private List<ABY_DominionPocketSession> sessions = new List<ABY_DominionPocketSession>();

        public ABY_DominionPocketRuntimeGameComponent(Game game)
        {
        }

        public static ABY_DominionPocketRuntimeGameComponent Get()
        {
            return Current.Game?.GetComponent<ABY_DominionPocketRuntimeGameComponent>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextMaintenanceTick, "nextMaintenanceTick", 0);
            Scribe_Values.Look(ref dominionHeartDestroyedLoreLetterSent, "dominionHeartDestroyedLoreLetterSent", false);
            Scribe_Collections.Look(ref sessions, "sessions", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                sessions ??= new List<ABY_DominionPocketSession>();
                sessions.RemoveAll(session => session == null || session.sessionId.NullOrEmpty());
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager == null || sessions == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (now < nextMaintenanceTick)
            {
                return;
            }

            nextMaintenanceTick = now + MaintenanceIntervalTicks;
            RunMaintenance();
        }

        public void RegisterSession(ABY_DominionPocketSession session)
        {
            if (session == null || session.sessionId.NullOrEmpty())
            {
                return;
            }

            sessions ??= new List<ABY_DominionPocketSession>();
            ForgetSession(session.sessionId);
            sessions.Add(session);
        }

        public bool TryGetActiveSessionForSourceMap(Map sourceMap, out ABY_DominionPocketSession session)
        {
            session = null;
            if (sourceMap == null || sessions == null)
            {
                return false;
            }

            for (int i = 0; i < sessions.Count; i++)
            {
                ABY_DominionPocketSession candidate = sessions[i];
                if (candidate != null && candidate.active && candidate.sourceMapId == sourceMap.uniqueID)
                {
                    session = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSessionByPocketMap(Map pocketMap, out ABY_DominionPocketSession session)
        {
            session = null;
            if (pocketMap == null || sessions == null)
            {
                return false;
            }

            for (int i = 0; i < sessions.Count; i++)
            {
                ABY_DominionPocketSession candidate = sessions[i];
                if (candidate != null && candidate.active && candidate.pocketMapId == pocketMap.uniqueID)
                {
                    session = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSessionById(string sessionId, out ABY_DominionPocketSession session)
        {
            session = null;
            if (sessionId.NullOrEmpty() || sessions == null)
            {
                return false;
            }

            for (int i = 0; i < sessions.Count; i++)
            {
                ABY_DominionPocketSession candidate = sessions[i];
                if (candidate != null && candidate.sessionId == sessionId)
                {
                    session = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TrySendDominionHeartDestroyedLoreLetterOnce(Map map, IntVec3 focusCell)
        {
            if (dominionHeartDestroyedLoreLetterSent || map == null)
            {
                return false;
            }

            dominionHeartDestroyedLoreLetterSent = true;
            IntVec3 targetCell = focusCell.IsValid ? focusCell : map.Center;
            Find.LetterStack.ReceiveLetter(
                "ABY_DominionSliceEncounter_HeartDestroyedLoreTitle".Translate(),
                "ABY_DominionSliceEncounter_HeartDestroyedLoreText".Translate(),
                LetterDefOf.PositiveEvent,
                new TargetInfo(targetCell, map));
            return true;
        }

        public void ForgetSession(string sessionId)
        {
            if (sessionId.NullOrEmpty() || sessions == null)
            {
                return;
            }

            sessions.RemoveAll(session => session == null || session.sessionId == sessionId);
        }

        private void RunMaintenance()
        {
            if (sessions == null)
            {
                sessions = new List<ABY_DominionPocketSession>();
                return;
            }

            for (int i = sessions.Count - 1; i >= 0; i--)
            {
                ABY_DominionPocketSession session = sessions[i];
                if (session == null || session.sessionId.NullOrEmpty())
                {
                    sessions.RemoveAt(i);
                    continue;
                }

                Map pocketMap = AbyssalDominionPocketUtility.ResolveMap(session.pocketMapId);
                if (pocketMap == null)
                {
                    // Package 3: after a successful safe extraction the pocket map may already be gone.
                    // Do not turn a recorded victory into a late failure while cleaning orphaned sessions.
                    sessions.RemoveAt(i);
                    continue;
                }

                Map sourceMap = AbyssalDominionPocketUtility.ResolveMap(session.sourceMapId);
                if (sourceMap == null)
                {
                    if (!session.victoryAchieved)
                    {
                        AbyssalDominionPocketUtility.FailAndCollapsePocketSlice(session, pocketMap, "ABY_DominionPocketOutcome_FailureLost".Translate(), true);
                    }
                    else
                    {
                        AbyssalDominionPocketUtility.CollapsePocketSlice(session, pocketMap, true);
                    }
                    sessions.RemoveAt(i);
                    continue;
                }

                if (!session.active)
                {
                    continue;
                }

                ReconcileVictoryState(session, pocketMap);
                session.lastKnownPocketPawnCount = AbyssalDominionPocketUtility.GetPocketPlayerCount(pocketMap);
                AbyssalDominionPocketUtility.TryEnsurePocketExit(session, pocketMap);

                MapComponent_DominionCrisis crisis = sourceMap.GetComponent<MapComponent_DominionCrisis>();
                if (crisis != null && crisis.IsTerminal && !session.victoryAchieved)
                {
                    AbyssalDominionPocketUtility.FailAndCollapsePocketSlice(session, pocketMap, "ABY_DominionPocketOutcome_FailureLost".Translate(), false);
                    continue;
                }

                if (session.lastKnownPocketPawnCount <= 0)
                {
                    if (session.victoryAchieved)
                    {
                        // Package 3: a victory session is extraction-pending/cleanup-pending, not a loss.
                        // Large modpacks can temporarily hide/despawn pawns during transfer; do not fail it here.
                        continue;
                    }

                    AbyssalDominionPocketUtility.FailAndCollapsePocketSlice(session, pocketMap, "ABY_DominionPocketOutcome_FailureLost".Translate(), false);
                }
            }
        }

        private void ReconcileVictoryState(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null || pocketMap == null)
            {
                return;
            }

            MapComponent_DominionSliceEncounter encounter = pocketMap.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter == null)
            {
                return;
            }

            if (encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse)
            {
                session.victoryAchieved = true;
                if (session.collapseAtTick <= 0 && Find.TickManager != null)
                {
                    session.collapseAtTick = Find.TickManager.TicksGame + 3600;
                }

                if (session.rewardSummary.NullOrEmpty())
                {
                    session.rewardSummary = encounter.GetRewardForecastValue();
                }

                IntVec3 focusCell = session.heartCell.IsValid ? session.heartCell : pocketMap.Center;
                TrySendDominionHeartDestroyedLoreLetterOnce(pocketMap, focusCell);
            }
        }
    }
}
