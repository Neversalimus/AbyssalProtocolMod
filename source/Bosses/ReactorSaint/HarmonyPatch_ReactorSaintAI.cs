using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class HarmonyPatch_ReactorSaintAI_AIGotoNearestHostile
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(JobGiver_AIGotoNearestHostile), "TryGiveJob");
        }

        private static void Postfix(Pawn pawn, ref Job __result)
        {
            ABY_ReactorSaintAIUtility.StabilizeAIGotoNearestHostileResult(pawn, ref __result);
        }
    }
}
