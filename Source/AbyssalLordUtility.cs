using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public static class AbyssalLordUtility
    {
        public static Lord FindLordFor(Pawn pawn)
        {
            if (pawn?.MapHeld?.lordManager?.lords == null)
            {
                return null;
            }

            List<Lord> lords = pawn.MapHeld.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord lord = lords[i];
                if (lord?.ownedPawns != null && lord.ownedPawns.Contains(pawn))
                {
                    return lord;
                }
            }

            return null;
        }

        public static Lord EnsureAssaultLord(Pawn pawn, bool sappers)
        {
            if (pawn == null || pawn.Faction == null || pawn.MapHeld == null || pawn.Dead)
            {
                return null;
            }

            Lord existingLord = FindLordFor(pawn);
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

        public static Lord EnsureAssaultLord(IEnumerable<Pawn> pawns, Faction faction, Map map, bool sappers)
        {
            if (pawns == null || faction == null || map == null)
            {
                return null;
            }

            List<Pawn> lordless = new List<Pawn>();
            Lord sharedExistingLord = null;
            bool multipleDifferentLords = false;

            foreach (Pawn pawn in pawns)
            {
                if (pawn == null || pawn.Dead || pawn.MapHeld != map || pawn.Faction != faction)
                {
                    continue;
                }

                Lord existingLord = FindLordFor(pawn);
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
                    multipleDifferentLords = true;
                }
            }

            if (sharedExistingLord != null && !multipleDifferentLords)
            {
                for (int i = 0; i < lordless.Count; i++)
                {
                    if (!sharedExistingLord.ownedPawns.Contains(lordless[i]))
                    {
                        sharedExistingLord.AddPawn(lordless[i]);
                    }
                }

                return sharedExistingLord;
            }

            if (lordless.Count == 0)
            {
                return multipleDifferentLords ? null : sharedExistingLord;
            }

            LordJob lordJob = new LordJob_AssaultColony(
                faction,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: sappers,
                useAvoidGridSmart: true,
                canSteal: false);

            return LordMaker.MakeNewLord(faction, lordJob, map, lordless);
        }
    }
}
