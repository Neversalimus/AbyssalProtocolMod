using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class ABY_AbyssalJobLoopGuardUtility
    {
        public static void StabilizeAIGotoNearestHostileResult(Pawn pawn, ref Job job)
        {
            if (pawn == null || job == null)
            {
                return;
            }

            if (!ABY_AntiTameUtility.IsAbyssalPawn(pawn))
            {
                return;
            }

            if (ABY_ReactorSaintAIUtility.IsReactorSaintPawn(pawn))
            {
                // Reactor Saint has its own dedicated Harmony postfix. Do not run the
                // generic large-modpack loop guard on it as a second AI route.
                return;
            }

            ABY_AbyssalMonsterBrain.TryStabilizeAIGotoNearestHostileResult(pawn, ref job);
        }
    }
}
