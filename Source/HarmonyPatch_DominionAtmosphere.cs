using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch(typeof(AbyssalDominionSliceBuilder), nameof(AbyssalDominionSliceBuilder.TryPrepareDominionSlice))]
    public static class HarmonyPatch_DominionAtmosphere_PrepareDominionSlice
    {
        public static void Postfix(Map map, ABY_DominionPocketSession session, bool __result)
        {
            if (!__result || map == null)
            {
                return;
            }

            ABY_DominionAtmosphereUtility.MarkDominionSlice(map, session, "harmony-builder-postfix");
        }
    }
}
