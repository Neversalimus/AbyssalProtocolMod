using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public sealed class WorkGiver_ABY_DecodeProtocolProject : WorkGiver_Scanner
    {
        private static ThingDef cachedNexusDef;

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                cachedNexusDef ??= DefDatabase<ThingDef>.GetNamedSilentFail("ABY_ProtocolNexus");
                return cachedNexusDef != null ? ThingRequest.ForDef(cachedNexusDef) : ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!AbyssalProtocolMod.Settings.enableProtocolNexusGating)
            {
                return false;
            }

            Building_ABY_ProtocolNexus nexus = t as Building_ABY_ProtocolNexus;
            if (pawn == null || nexus == null || !nexus.Spawned || !nexus.IsPowerActive || !nexus.HasActiveDecode)
            {
                return false;
            }

            if (pawn.skills == null || pawn.WorkTagIsDisabled(WorkTags.Intellectual))
            {
                return false;
            }

            if (ABY_ProtocolResearchGateUtility.IsDecoded(nexus.ActiveDecodeProjectDefName))
            {
                nexus.CompleteActiveDecode(pawn);
                return false;
            }

            return pawn.CanReserveAndReach(nexus, PathEndMode.InteractionCell, Danger.Some, 1, -1, null, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("ABY_DecodeProtocolProject");
            return jobDef == null ? null : JobMaker.MakeJob(jobDef, t);
        }
    }
}
