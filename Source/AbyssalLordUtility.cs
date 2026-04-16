using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public static class AbyssalLordUtility
    {
        public static Lord EnsureAssaultLord(
            Pawn pawn,
            bool sappers)
        {
            if (pawn == null || pawn.Faction == null || pawn.MapHeld == null)
            {
                return null;
            }

            Lord existingLord = pawn.GetLord();
            if (existingLord != null)
            {
                return existingLord;
            }

            LordJob lordJob = new LordJob_AssaultColony(
                pawn.Faction,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: sappers,
                useAvoidGridSmart: true,
                canSteal: false);

            return LordMaker.MakeNewLord(pawn.Faction, lordJob, pawn.MapHeld, new List<Pawn> { pawn });
        }

        public static Lord EnsureAssaultLord(
            IEnumerable<Pawn> pawns,
            Faction faction,
            Map map,
            bool sappers)
        {
            if (pawns == null || faction == null || map == null)
            {
                return null;
            }

            List<Pawn> lordless = new List<Pawn>();
            Lord sharedExistingLord = null;
            bool allExistingLordsMatch = true;

            foreach (Pawn pawn in pawns)
            {
                if (pawn == null || pawn.Dead || pawn.MapHeld != map || pawn.Faction != faction)
                {
                    continue;
                }

                Lord existingLord = pawn.GetLord();
                if (existingLord == null)
                {
                    lordless.Add(pawn);
                    continue;
                }

                if (sharedExistingLord == null)
                {
                    sharedExistingLord = existingLord;
                }
                else if (sharedExistingLord != existingLord)
                {
                    allExistingLordsMatch = false;
                }
            }

            if (lordless.Count > 0)
            {
                LordJob lordJob = new LordJob_AssaultColony(
                    faction,
                    canKidnap: false,
                    canTimeoutOrFlee: false,
                    sappers: sappers,
                    useAvoidGridSmart: true,
                    canSteal: false);

                return LordMaker.MakeNewLord(faction, lordJob, map, lordless);
            }

            return allExistingLordsMatch ? sharedExistingLord : null;
        }
    }
}
