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
            if (pawn?.Map?.lordManager?.lords == null)
            {
                return null;
            }

            List<Lord> lords = pawn.Map.lordManager.lords;
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

        public static bool EnsureAssaultLord(Pawn pawn, bool sappers)
        {
            if (pawn?.Map == null || pawn.Faction == null)
            {
                return false;
            }

            if (FindLordFor(pawn) != null)
            {
                return true;
            }

            LordJob lordJob = new LordJob_AssaultColony(
                pawn.Faction,
                canKidnap: false,
                canTimeoutOrFlee: false,
                sappers: sappers,
                useAvoidGridSmart: true,
                canSteal: false);

            LordMaker.MakeNewLord(pawn.Faction, lordJob, pawn.Map, new List<Pawn> { pawn });
            return true;
        }
    }
}
