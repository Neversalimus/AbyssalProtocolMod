using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public sealed class JobDriver_ABY_DecodeProtocolProject : JobDriver
    {
        private Building_ABY_ProtocolNexus Nexus => job.GetTarget(TargetIndex.A).Thing as Building_ABY_ProtocolNexus;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Nexus == null || !Nexus.IsPowerActive || !Nexus.HasActiveDecode);
            this.FailOn(() => ABY_ProtocolResearchGateUtility.IsDecoded(Nexus.ActiveDecodeProjectDefName));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

            Toil decode = ToilMaker.MakeToil("DecodeProtocolProject");
            decode.defaultCompleteMode = ToilCompleteMode.Never;
            decode.tickAction = delegate
            {
                Building_ABY_ProtocolNexus nexus = Nexus;
                if (nexus == null || !nexus.Spawned || !nexus.IsPowerActive || !nexus.HasActiveDecode)
                {
                    ReadyForNextToil();
                    return;
                }

                pawn.skills?.Learn(SkillDefOf.Intellectual, 0.08f);
                nexus.NotifyDecodeWorkTick(pawn);
                if (!nexus.HasActiveDecode)
                {
                    ReadyForNextToil();
                }
            };
            decode.WithProgressBar(TargetIndex.A, () => Nexus?.ActiveDecodeProgress ?? 0f, false, -0.5f);
            decode.handlingFacing = true;
            yield return decode;
        }
    }
}
