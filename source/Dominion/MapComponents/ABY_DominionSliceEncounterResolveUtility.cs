using Verse;

namespace AbyssalProtocol
{
    public static class ABY_DominionSliceEncounterResolveUtility
    {
        private const int MissingEncounterRetryTicks = 250;

        public static MapComponent_DominionSliceEncounter Resolve(
            Map map,
            ref MapComponent_DominionSliceEncounter cachedEncounter,
            ref int nextEncounterResolveTick)
        {
            if (cachedEncounter != null)
            {
                return cachedEncounter;
            }

            if (map == null)
            {
                return null;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now > 0 && now < nextEncounterResolveTick)
            {
                return null;
            }

            int stagger = map.uniqueID % 31;
            if (stagger < 0)
            {
                stagger = -stagger;
            }
            nextEncounterResolveTick = now + MissingEncounterRetryTicks + stagger;

            cachedEncounter = map.GetComponent<MapComponent_DominionSliceEncounter>();
            return cachedEncounter;
        }
    }
}
