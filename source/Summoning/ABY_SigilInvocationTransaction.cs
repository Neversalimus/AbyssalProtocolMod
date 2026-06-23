using System;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Save-backed one-sigil transaction for a normal player invocation.
    /// A sigil becomes permanently spent only after the encounter has accepted its real
    /// world-side activation. Any pre-activation abort returns exactly one replacement.
    /// </summary>
    public sealed class ABY_SigilInvocationTransaction : IExposable
    {
        private ThingDef consumedSigilDef;
        private bool consumptionRegistered;
        private bool encounterCommitted;
        private bool refundIssued;
        private int consumedTick;
        private string lastReason;

        public ThingDef ConsumedSigilDef => consumedSigilDef;
        public bool ConsumptionRegistered => consumptionRegistered;
        public bool EncounterCommitted => encounterCommitted;
        public bool RefundIssued => refundIssued;
        public bool NeedsRefund => consumptionRegistered && !encounterCommitted && !refundIssued && consumedSigilDef != null;
        public int ConsumedTick => consumedTick;
        public string LastReason => lastReason;

        public void Register(ThingDef sigilDef, int tick)
        {
            if (sigilDef == null)
            {
                return;
            }

            consumedSigilDef = sigilDef;
            consumptionRegistered = true;
            encounterCommitted = false;
            refundIssued = false;
            consumedTick = tick;
            lastReason = "Sigil consumed after ritual preparation was accepted.";
        }

        public void Commit(string reason)
        {
            if (!consumptionRegistered)
            {
                return;
            }

            encounterCommitted = true;
            lastReason = reason ?? "Encounter activation committed.";
        }

        public void MarkRefunded(string reason)
        {
            if (!NeedsRefund)
            {
                return;
            }

            refundIssued = true;
            lastReason = reason ?? "Sigil refunded after pre-activation abort.";
        }

        public void SetReason(string reason)
        {
            if (!reason.NullOrEmpty())
            {
                lastReason = reason;
            }
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref consumedSigilDef, "consumedSigilDef");
            Scribe_Values.Look(ref consumptionRegistered, "consumptionRegistered", false);
            Scribe_Values.Look(ref encounterCommitted, "encounterCommitted", false);
            Scribe_Values.Look(ref refundIssued, "refundIssued", false);
            Scribe_Values.Look(ref consumedTick, "consumedTick", 0);
            Scribe_Values.Look(ref lastReason, "lastReason");
        }
    }
}
