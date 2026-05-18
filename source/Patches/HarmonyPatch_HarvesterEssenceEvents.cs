using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    [HarmonyPatch]
    public static class HarmonyPatch_HarvesterEssenceEvents
    {
        public struct PawnKillState
        {
            public bool wasDead;
            public Map map;
            public IntVec3 cell;
        }

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        [HarmonyPrefix]
        public static void PawnKill_Prefix(Pawn __instance, ref PawnKillState __state)
        {
            __state = new PawnKillState
            {
                wasDead = __instance == null || __instance.Dead,
                map = __instance?.MapHeld,
                cell = __instance?.PositionHeld ?? IntVec3.Invalid
            };
        }

        [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
        [HarmonyPostfix]
        public static void PawnKill_Postfix(Pawn __instance, PawnKillState __state)
        {
            if (__instance == null || __state.wasDead || !__instance.Dead)
            {
                return;
            }

            ABY_HarvesterEssenceEventUtility.NotifyPawnKilled(__instance, __state.map, __state.cell);
        }

        [HarmonyPatch(typeof(Corpse), nameof(Corpse.SpawnSetup))]
        [HarmonyPostfix]
        public static void CorpseSpawnSetup_Postfix(Corpse __instance, bool respawningAfterLoad)
        {
            if (respawningAfterLoad)
            {
                return;
            }

            ABY_HarvesterEssenceEventUtility.NotifyCorpseSpawned(__instance);
        }
    }
}
