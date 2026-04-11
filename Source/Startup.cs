using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            Log.Message("[Abyssal Protocol] Loaded source assembly.");
        }
    }
}
