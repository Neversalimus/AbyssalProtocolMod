using Verse;

namespace AbyssalProtocol
{
    public static class ABY_DevToolUtility
    {
        public static bool IsDebugToolActiveForInput()
        {
            // Dev-mode bypass detection was originally added so armed debug tools would not
            // fight boss-click helpers. In practice the reflective detector became a runtime
            // perf risk during boss fights on Dev maps. Keep this utility inert; callers that
            // need to avoid DevMode input conflicts already check Prefs.DevMode directly.
            return false;
        }

        public static bool IsDebugToolActiveOrExecuting()
        {
            return false;
        }

        public static bool IsRecentDebugToolAction(int graceTicks = 3)
        {
            return false;
        }
    }
}
