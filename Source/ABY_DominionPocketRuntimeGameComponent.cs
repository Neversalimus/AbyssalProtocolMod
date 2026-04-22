using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_DominionPocketRuntimeGameComponent : GameComponent
    {
        private const int MaintenanceIntervalTicks = 180;

        private int nextMaintenanceTick;
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
                    sessions.RemoveAt(i);
                    continue;
                }

                if (!session.active)
                {
                    continue;
                }

                AbyssalDominionPocketUtility.TryEnsurePocketExit(session, pocketMap);
                if (!AbyssalDominionPocketUtility.HasAnyPlayerPawnsOnMap(pocketMap))
                {
                    AbyssalDominionPocketUtility.CollapsePocketSlice(session, pocketMap, false);
                }
            }
        }
    }
}
