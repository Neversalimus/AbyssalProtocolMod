using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public class JobDriver_HaulToSigilVault : JobDriver
    {
        private const TargetIndex SigilInd = TargetIndex.A;
        private const TargetIndex VaultInd = TargetIndex.B;

        private Thing Sigil => job.GetTarget(SigilInd).Thing;
        private Building_ABY_SigilVault Vault => job.GetTarget(VaultInd).Thing as Building_ABY_SigilVault;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Sigil, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(Vault, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(SigilInd);
            this.FailOnDestroyedOrNull(VaultInd);
            this.FailOnForbidden(SigilInd);
            this.FailOn(() => Vault == null || !Vault.Spawned || !Vault.CanAccept(Sigil));

            yield return Toils_Goto.GotoThing(SigilInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(SigilInd);
            yield return Toils_Goto.GotoThing(VaultInd, PathEndMode.Touch);

            Toil deposit = ToilMaker.MakeToil("DepositInSigilVault");
            deposit.initAction = delegate
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried == null || Vault == null)
                {
                    return;
                }

                pawn.carryTracker.TryDropCarriedThing(Vault.Position, ThingPlaceMode.Near, out Thing droppedThing);
                if (droppedThing != null)
                {
                    Vault.TryAbsorbThing(droppedThing);
                }
            };
            deposit.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return deposit;
        }
    }
}
