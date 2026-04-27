using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_DominionTargetUtility
    {
        private const string AbyssalFactionDefName = "ABY_AbyssalHost";

        public static bool IsDominionEntryOrExitPortal(Thing thing)
        {
            string defName = thing?.def?.defName ?? string.Empty;
            return string.Equals(defName, "ABY_DominionGateCore", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_DominionPocketExit", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDominionHostileAnchorOrHeart(Thing thing)
        {
            string defName = thing?.def?.defName ?? string.Empty;
            return defName.StartsWith("ABY_DominionSliceAnchor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_DominionSliceHeart", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsHostileHordePortal(Thing thing)
        {
            string defName = thing?.def?.defName ?? string.Empty;
            return string.Equals(defName, "ABY_ImpPortal", StringComparison.OrdinalIgnoreCase)
                || defName.IndexOf("AbyssalPortal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static void MakeDominionPortalFriendly(Thing thing)
        {
            if (thing == null || thing.Destroyed || !IsDominionEntryOrExitPortal(thing))
            {
                return;
            }

            TrySetFaction(thing, Faction.OfPlayer);
        }

        public static void MakeDominionAnchorHostile(Thing thing)
        {
            if (thing == null || thing.Destroyed || !IsDominionHostileAnchorOrHeart(thing))
            {
                return;
            }

            Faction abyssal = ResolveAbyssalFaction();
            if (abyssal != null)
            {
                TrySetFaction(thing, abyssal);
            }
        }

        public static Faction ResolveAbyssalFaction()
        {
            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail(AbyssalFactionDefName);
            if (def == null || Find.FactionManager == null)
            {
                return null;
            }

            Faction faction = Find.FactionManager.FirstFactionOfDef(def);
            if (faction != null)
            {
                return faction;
            }

            try
            {
                Faction generated = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
                if (generated != null)
                {
                    List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
                    if (factions == null || !factions.Contains(generated))
                    {
                        Find.FactionManager.Add(generated);
                    }

                    return generated;
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("dominion-resolve-abyssal-faction", "[Abyssal Protocol] Could not generate ABY_AbyssalHost for dominion hostile target setup: " + ex.Message, 5000);
            }

            return null;
        }

        private static void TrySetFaction(Thing thing, Faction faction)
        {
            if (thing == null || faction == null || thing.Faction == faction)
            {
                return;
            }

            try
            {
                thing.SetFaction(faction);
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("dominion-set-faction-" + (thing.def?.defName ?? "unknown"), "[Abyssal Protocol] Could not set dominion target faction for " + (thing.def?.defName ?? "unknown") + ": " + ex.Message, 5000);
            }
        }
    }
}
