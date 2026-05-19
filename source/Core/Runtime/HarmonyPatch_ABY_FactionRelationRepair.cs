using System;
using HarmonyLib;
using RimWorld;

namespace AbyssalProtocol
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.RelationWith), new Type[] { typeof(Faction), typeof(bool) })]
    public static class HarmonyPatch_ABY_FactionRelationRepair_RelationWith
    {
        public static void Prefix(Faction __instance, Faction other)
        {
            ABY_FactionHostilityUtility.EnsureHostileRelationIfAbyssalPair(__instance, other);
        }
    }
}
