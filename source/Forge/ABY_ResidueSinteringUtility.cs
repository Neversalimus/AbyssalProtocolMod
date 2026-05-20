using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ResidueSinteringUtility
    {
        // Legacy fallback kept deliberately so existing saves and older XML remain stable.
        // New abyssal enemies should define AbyssalProtocol.ABY_ResidueSinteringExtension
        // on their PawnKindDef instead of requiring another C# edit here.
        private static readonly Dictionary<string, int> LegacyResidueBySafeRaceOrKind = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            { "ABY_RiftImp", 6 },
            { "ABY_EmberHound", 7 },
            { "ABY_HexgunThrall", 8 },

            { "ABY_ChainZealot", 12 },
            { "ABY_RiftSniper", 14 },
            { "ABY_NullPriest", 14 },
            { "ABY_RiftSapper", 15 },
            { "ABY_BreachBrute", 18 },
            { "ABY_BreachBruteEscort", 18 },

            { "ABY_Harvester", 20 },
            { "ABY_GateWarden", 22 },
            { "ABY_SiegeIdol", 23 },
            { "ABY_SiegeIdolEscort", 23 }
        };


        public static bool IsSinterableAbyssalCorpse(Thing thing)
        {
            return TryGetResidueAmount(thing, out int _);
        }

        public static bool IsBillUsableSinteringIngredient(Thing thing)
        {
            return IsBillUsableSinteringIngredient(thing, out int _);
        }

        public static bool IsBillUsableSinteringIngredient(Thing thing, out int residueAmount)
        {
            residueAmount = 0;

            if (!TryGetResidueAmount(thing, out residueAmount))
            {
                return false;
            }

            if (thing.Destroyed || !thing.Spawned || thing.Map == null)
            {
                return false;
            }

            Faction playerFaction = Faction.OfPlayer;
            if (playerFaction != null && thing.IsForbidden(playerFaction))
            {
                return false;
            }

            if (thing.Position.Fogged(thing.Map))
            {
                return false;
            }

            return true;
        }

        public static bool TryGetResidueAmount(Thing thing, out int residueAmount)
        {
            residueAmount = 0;

            if (!(thing is Corpse corpse) || corpse.InnerPawn == null)
            {
                return false;
            }

            Pawn innerPawn = corpse.InnerPawn;
            string raceDefName = innerPawn.def?.defName;
            string kindDefName = innerPawn.kindDef?.defName;

            if (ABY_AbyssalPawnClassificationUtility.IsBossOrMiniBoss(innerPawn))
            {
                return false;
            }

            if (TryGetExtensionResidue(innerPawn.kindDef, out residueAmount, out bool explicitKindBlock))
            {
                return true;
            }

            if (explicitKindBlock)
            {
                return false;
            }

            if (TryGetExtensionResidue(innerPawn.def, out residueAmount, out bool explicitRaceBlock))
            {
                return true;
            }

            if (explicitRaceBlock)
            {
                return false;
            }

            if (!kindDefName.NullOrEmpty() && LegacyResidueBySafeRaceOrKind.TryGetValue(kindDefName, out residueAmount))
            {
                return true;
            }

            if (!raceDefName.NullOrEmpty() && LegacyResidueBySafeRaceOrKind.TryGetValue(raceDefName, out residueAmount))
            {
                return true;
            }

            return false;
        }

        public static int CountSinterableCorpses(Map map)
        {
            if (map == null)
            {
                return 0;
            }

            int count = 0;
            List<Thing> corpses = map.listerThings?.ThingsInGroup(ThingRequestGroup.Corpse);
            if (corpses == null)
            {
                return 0;
            }

            for (int i = 0; i < corpses.Count; i++)
            {
                if (IsBillUsableSinteringIngredient(corpses[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryGetExtensionResidue(Def def, out int residueAmount, out bool explicitBlock)
        {
            residueAmount = 0;
            explicitBlock = false;

            ABY_ResidueSinteringExtension extension = def?.GetModExtension<ABY_ResidueSinteringExtension>();
            if (extension == null)
            {
                return false;
            }

            if (!extension.allowSintering || extension.residueValue < 1)
            {
                explicitBlock = true;
                return false;
            }

            residueAmount = Math.Max(1, extension.residueValue);
            return true;
        }

    }
}
