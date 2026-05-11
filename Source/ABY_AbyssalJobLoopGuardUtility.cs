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
                ABY_ReactorSaintAIUtility.StabilizeAIGotoNearestHostileResult(pawn, ref job);
                return;
            }

            ABY_AbyssalMonsterBrain.TryStabilizeAIGotoNearestHostileResult(pawn, ref job);
        }
    }
}
