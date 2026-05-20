using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Prevents Pick Up And Haul's inventory hauling comp from being exposed on Abyssal hostile pawns.
    ///
    /// Large modpacks can patch the comp onto broad pawn classes. Abyssal monsters and bosses are temporary
    /// hostile encounter pawns that can be moved between maps/world pawns during Dominion and boss cleanup.
    /// If Pick Up And Haul serializes its ThingsHauledToInventory crossref list on those pawns, existing saves can
    /// hit duplicate/missing load ID registrations while RimWorld loads WorldPawns. We do not need hauling inventory
    /// behavior on abyssal enemies, so stripping only that external comp is the safest compatibility boundary.
    /// </summary>
    public static class ABY_PickUpAndHaulCompatibilityUtility
    {
        private const string PickUpAndHaulCompFullName = "PickUpAndHaul.CompHauledToInventory";
        private const string AbyssalPrefix = "ABY_";

        public static void StripExternalHaulInventoryComp(ThingWithComps thing, string source)
        {
            if (!IsAbyssalHostilePawn(thing) || thing.AllComps == null || thing.AllComps.Count == 0)
            {
                return;
            }

            List<ThingComp> comps = thing.AllComps;
            int removed = 0;
            for (int i = comps.Count - 1; i >= 0; i--)
            {
                ThingComp comp = comps[i];
                if (comp?.GetType()?.FullName == PickUpAndHaulCompFullName)
                {
                    comps.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
            {
                string key = "puah-strip-" + (thing.thingIDNumber >= 0 ? thing.thingIDNumber.ToString() : thing.GetHashCode().ToString()) + "-" + source;
                ABY_LogThrottleUtility.Message(key, "[Abyssal Protocol] Removed Pick Up And Haul inventory comp from abyssal hostile pawn " + SafeThingLabel(thing) + " during " + source + ".", 12000);
            }
        }

        private static bool IsAbyssalHostilePawn(ThingWithComps thing)
        {
            Pawn pawn = thing as Pawn;
            if (pawn == null)
            {
                return false;
            }

            if (ABY_FactionHostilityUtility.IsAbyssalPawn(pawn))
            {
                return true;
            }

            string defName = pawn.def?.defName;
            string kindName = pawn.kindDef?.defName;
            return StartsWithAbyssalPrefix(defName) || StartsWithAbyssalPrefix(kindName);
        }

        private static bool StartsWithAbyssalPrefix(string value)
        {
            return !value.NullOrEmpty() && value.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeThingLabel(Thing thing)
        {
            try
            {
                return thing?.LabelShortCap ?? thing?.def?.defName ?? "unknown";
            }
            catch
            {
                return thing?.def?.defName ?? "unknown";
            }
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.InitializeComps))]
    public static class HarmonyPatch_ABY_PickUpAndHaulCompatibility_InitializeComps
    {
        public static void Postfix(ThingWithComps __instance)
        {
            ABY_PickUpAndHaulCompatibilityUtility.StripExternalHaulInventoryComp(__instance, "comp-initialization");
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.ExposeData))]
    public static class HarmonyPatch_ABY_PickUpAndHaulCompatibility_ExposeData
    {
        public static void Prefix(ThingWithComps __instance)
        {
            ABY_PickUpAndHaulCompatibilityUtility.StripExternalHaulInventoryComp(__instance, "save-load-exposure");
        }
    }
}
