using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class HarmonyPatch_AbyssalMonsterAI_AIGotoNearestHostile
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(JobGiver_AIGotoNearestHostile), "TryGiveJob");
        }

        private static void Postfix(Pawn pawn, ref Job __result)
        {
            if (ABY_ReactorSaintAIUtility.IsReactorSaintPawn(pawn))
            {
                return;
            }

            ABY_AbyssalMonsterBrain.TryStabilizeAIGotoNearestHostileResult(pawn, ref __result);
        }
    }
}
