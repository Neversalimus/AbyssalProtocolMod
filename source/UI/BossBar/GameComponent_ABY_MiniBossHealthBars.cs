using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Lightweight overhead health presentation for abyssal minibosses that use
    /// CompABY_BossTrueDeath but should not occupy the full cinematic boss HUD.
    /// </summary>
    public sealed class GameComponent_ABY_MiniBossHealthBars : GameComponent
    {
        private const int CacheRefreshIntervalTicks = 12;

        private readonly List<Pawn> cachedMiniBosses = new List<Pawn>();
        private Map cachedMap;
        private int nextCacheRefreshTick = -1;

        public GameComponent_ABY_MiniBossHealthBars(Game game)
        {
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();

            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            Event currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.Repaint)
            {
                return;
            }

            AbyssalProtocolModSettings settings = AbyssalProtocolMod.Settings;
            if (settings == null || !settings.enableBossBars || !settings.enableMiniBossHealthBars)
            {
                return;
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                cachedMiniBosses.Clear();
                cachedMap = null;
                nextCacheRefreshTick = -1;
                return;
            }

            EnsureCache(map);
            ABY_MiniBossHealthBarRenderer.Draw(cachedMiniBosses, settings);
        }

        private void EnsureCache(Map map)
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (map == cachedMap && ticksGame < nextCacheRefreshTick)
            {
                return;
            }

            cachedMap = map;
            nextCacheRefreshTick = ticksGame + CacheRefreshIntervalTicks;
            cachedMiniBosses.Clear();

            if (map?.mapPawns == null)
            {
                return;
            }

            IEnumerable<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                if (ShouldTrackMiniBoss(pawn, map))
                {
                    cachedMiniBosses.Add(pawn);
                }
            }
        }

        private static bool ShouldTrackMiniBoss(Pawn pawn, Map map)
        {
            if (pawn == null || map == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.MapHeld != map)
            {
                return false;
            }

            if (!ABY_AbyssalPawnClassificationUtility.IsMiniBoss(pawn))
            {
                return false;
            }

            if (ABY_AbyssalPawnClassificationUtility.IsMajorBoss(pawn))
            {
                return false;
            }

            float current;
            float max;
            float pct;
            if (!ABY_BossTrueDeathUtility.TryGetBossHp(pawn, out current, out max, out pct))
            {
                return false;
            }

            if (max <= 0.001f || current <= 0f)
            {
                return false;
            }

            try
            {
                if (pawn.PositionHeld.Fogged(map))
                {
                    return false;
                }
            }
            catch
            {
            }

            return true;
        }
    }
}
