using Verse;

namespace AbyssalProtocol
{
    public static class ABY_DominionCrisisResolveUtility
    {
        private const int MissingCrisisRetryTicks = 250;

        public static MapComponent_DominionCrisis Resolve(
            Map map,
            ref MapComponent_DominionCrisis cachedCrisis,
            ref int nextCrisisResolveTick)
        {
            if (cachedCrisis != null)
            {
                return cachedCrisis;
            }

            if (map == null)
            {
                return null;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (now > 0 && now < nextCrisisResolveTick)
            {
                return null;
            }

            int stagger = map.uniqueID % 31;
            if (stagger < 0)
            {
                stagger = -stagger;
            }
            nextCrisisResolveTick = now + MissingCrisisRetryTicks + stagger;

            cachedCrisis = map.GetComponent<MapComponent_DominionCrisis>();
            return cachedCrisis;
        }
    }
}
