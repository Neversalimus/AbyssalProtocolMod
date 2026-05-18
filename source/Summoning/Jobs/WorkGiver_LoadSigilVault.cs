using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class WorkGiver_LoadSigilVault : WorkGiver_Scanner
    {
        private static readonly JobDef HaulToVaultJobDef = DefDatabase<JobDef>.GetNamed("ABY_HaulToSigilVault");
        private static readonly ThingDef VaultDef = DefDatabase<ThingDef>.GetNamed("ABY_SigilVault");

        public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            Map map = pawn?.Map;
            return map == null || !MapHasAcceptingVault(map) || !MapHasLooseAcceptedSigils(map);
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            Map map = pawn?.Map;
            if (map == null || !MapHasAcceptingVault(map))
            {
                yield break;
            }

            List<ThingDef> sigilDefs = Building_ABY_SigilVault.AcceptedSigilDefs;
            for (int i = 0; i < sigilDefs.Count; i++)
            {
                List<Thing> things = map.listerThings.ThingsOfDef(sigilDefs[i]);
                for (int j = 0; j < things.Count; j++)
                {
                    Thing thing = things[j];
                    if (thing != null && thing.Spawned)
                    {
                        yield return thing;
                    }
                }
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn == null || t == null || !t.Spawned || !Building_ABY_SigilVault.IsAcceptedSigilDef(t.def))
            {
                return false;
            }

            if (t.IsForbidden(pawn) && !forced)
            {
                return false;
            }

            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }

            return FindBestVaultFor(pawn, t, forced) != null;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_ABY_SigilVault vault = FindBestVaultFor(pawn, t, forced);
            if (vault == null)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(HaulToVaultJobDef, t, vault);
            job.count = 1;
            return job;
        }

        private static bool MapHasAcceptingVault(Map map)
        {
            if (map == null || VaultDef == null)
            {
                return false;
            }

            List<Thing> vaults = map.listerThings.ThingsOfDef(VaultDef);
            for (int i = 0; i < vaults.Count; i++)
            {
                if (vaults[i] is Building_ABY_SigilVault vault && vault.Spawned && !vault.Destroyed && vault.FreeSigilSlots > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MapHasLooseAcceptedSigils(Map map)
        {
            if (map == null)
            {
                return false;
            }

            List<ThingDef> sigilDefs = Building_ABY_SigilVault.AcceptedSigilDefs;
            for (int i = 0; i < sigilDefs.Count; i++)
            {
                List<Thing> things = map.listerThings.ThingsOfDef(sigilDefs[i]);
                for (int j = 0; j < things.Count; j++)
                {
                    Thing thing = things[j];
                    if (thing != null && thing.Spawned)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Building_ABY_SigilVault FindBestVaultFor(Pawn pawn, Thing sigil, bool forced)
        {
            if (pawn?.Map == null || sigil == null || VaultDef == null)
            {
                return null;
            }

            Thing found = GenClosest.ClosestThingReachable(
                sigil.Position,
                pawn.Map,
                ThingRequest.ForDef(VaultDef),
                PathEndMode.Touch,
                TraverseParms.For(pawn),
                9999f,
                thing => thing is Building_ABY_SigilVault vault
                    && vault.Spawned
                    && !vault.Destroyed
                    && !vault.IsForbidden(pawn)
                    && pawn.CanReserve(vault, 1, -1, null, forced)
                    && vault.CanAccept(sigil));

            return found as Building_ABY_SigilVault;
        }
    }
}
