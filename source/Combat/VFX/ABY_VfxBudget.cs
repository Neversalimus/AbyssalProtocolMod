using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public enum ABY_VfxBudgetCategory
    {
        CombatLight,
        CombatHeavy,
        DominionAmbient,
        UIOrDecorative
    }

    /// <summary>
    /// Per-map soft VFX budget used by high-frequency Abyssal visuals. This only gates optional visuals;
    /// gameplay, damage and targeting must remain outside this budget.
    /// </summary>
    public static class ABY_VfxBudget
    {
        private const int WindowTicks = 30;
        private static readonly Dictionary<int, MapBudgetState> StateByMapId = new Dictionary<int, MapBudgetState>();

        private sealed class MapBudgetState
        {
            public Map map;
            public int windowStartTick = -1;
            public int combatLightSpent;
            public int combatHeavySpent;
            public int dominionAmbientSpent;
            public int decorativeSpent;
            public int lastSeenTick;
        }

        public static bool TrySpend(Map map, ABY_VfxBudgetCategory category, int cost = 1)
        {
            if (map == null)
            {
                return false;
            }

            cost = Math.Max(1, cost);
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            MapBudgetState state = ResolveState(map, now);
            int cap = ResolveCap(category);

            switch (category)
            {
                case ABY_VfxBudgetCategory.CombatHeavy:
                    if (state.combatHeavySpent + cost > cap)
                    {
                        return false;
                    }
                    state.combatHeavySpent += cost;
                    return true;
                case ABY_VfxBudgetCategory.DominionAmbient:
                    if (state.dominionAmbientSpent + cost > cap)
                    {
                        return false;
                    }
                    state.dominionAmbientSpent += cost;
                    return true;
                case ABY_VfxBudgetCategory.UIOrDecorative:
                    if (state.decorativeSpent + cost > cap)
                    {
                        return false;
                    }
                    state.decorativeSpent += cost;
                    return true;
                default:
                    if (state.combatLightSpent + cost > cap)
                    {
                        return false;
                    }
                    state.combatLightSpent += cost;
                    return true;
            }
        }

        public static int ScaleCount(int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            float scale = ABY_PerformanceSettingsUtility.ResolveVfxIntensityScale();
            if (ABY_PerformanceSettingsUtility.IsMinimal)
            {
                scale *= 0.70f;
            }

            return Mathf.Max(1, Mathf.RoundToInt(count * Mathf.Clamp(scale, 0.25f, 1f)));
        }

        public static int ScaleInterval(int baseInterval)
        {
            return ABY_PerformanceSettingsUtility.ScaleVfxInterval(Mathf.Max(1, baseInterval));
        }

        private static MapBudgetState ResolveState(Map map, int now)
        {
            int id = map.uniqueID;
            if (!StateByMapId.TryGetValue(id, out MapBudgetState state) || state == null || state.map != map)
            {
                state = new MapBudgetState { map = map };
                StateByMapId[id] = state;
            }

            state.lastSeenTick = now;
            if (state.windowStartTick < 0 || now < state.windowStartTick || now - state.windowStartTick >= WindowTicks)
            {
                ResetWindow(state, now);
            }

            CleanupOldStates(now);
            return state;
        }

        public static void ClearAll()
        {
            StateByMapId.Clear();
        }

        private static void ResetWindow(MapBudgetState state, int now)
        {
            if (state == null)
            {
                return;
            }

            state.windowStartTick = now;
            state.combatLightSpent = 0;
            state.combatHeavySpent = 0;
            state.dominionAmbientSpent = 0;
            state.decorativeSpent = 0;
        }

        private static int ResolveCap(ABY_VfxBudgetCategory category)
        {
            ABY_VisualIntensity intensity = ABY_PerformanceSettingsUtility.CurrentIntensity;
            switch (category)
            {
                case ABY_VfxBudgetCategory.CombatHeavy:
                    return intensity == ABY_VisualIntensity.Minimal ? 18 : intensity == ABY_VisualIntensity.Reduced ? 36 : 72;
                case ABY_VfxBudgetCategory.DominionAmbient:
                    return intensity == ABY_VisualIntensity.Minimal ? 0 : intensity == ABY_VisualIntensity.Reduced ? 18 : 42;
                case ABY_VfxBudgetCategory.UIOrDecorative:
                    return intensity == ABY_VisualIntensity.Minimal ? 8 : intensity == ABY_VisualIntensity.Reduced ? 20 : 48;
                default:
                    return intensity == ABY_VisualIntensity.Minimal ? 42 : intensity == ABY_VisualIntensity.Reduced ? 78 : 150;
            }
        }

        private static void CleanupOldStates(int now)
        {
            if (StateByMapId.Count == 0 || now % 1800 != 0)
            {
                return;
            }

            List<int> remove = null;
            foreach (KeyValuePair<int, MapBudgetState> pair in StateByMapId)
            {
                MapBudgetState state = pair.Value;
                if (state == null || state.map == null || now < state.lastSeenTick || now - state.lastSeenTick > 3600)
                {
                    if (remove == null)
                    {
                        remove = new List<int>();
                    }

                    remove.Add(pair.Key);
                }
            }

            if (remove == null)
            {
                return;
            }

            for (int i = 0; i < remove.Count; i++)
            {
                StateByMapId.Remove(remove[i]);
            }
        }
    }
}
