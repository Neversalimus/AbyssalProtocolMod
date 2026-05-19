using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    /// <summary>
    /// Skips vanilla/modded trait behavior think-node evaluation for Abyssal hostile pawns.
    ///
    /// Some large modpacks inject trait-based job logic that assumes human colonist-style needs/traits and can
    /// throw on custom hostile pawns. Abyssal enemies use their own combat think-trees, so trait-behavior jobs are
    /// not needed and should fail closed without warning spam.
    /// </summary>
    [HarmonyPatch(typeof(ThinkNode_TraitBehaviors), "TryIssueJobPackage")]
    public static class HarmonyPatch_ABY_AbyssalThinkNodeTraitGuard
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Pawn pawn, ref ThinkResult __result)
        {
            if (!ABY_AntiTameUtility.IsAbyssalPawn(pawn))
            {
                return true;
            }

            __result = ThinkResult.NoJob;
            return false;
        }
    }
}
