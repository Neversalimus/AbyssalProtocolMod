using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_NullPriestAura : ThingComp
    {
        public CompProperties_ABY_NullPriestAura Props => (CompProperties_ABY_NullPriestAura)props;

        private Pawn PawnParent => parent as Pawn;

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || !parent.IsHashIntervalTick(System.Math.Max(15, Props.scanIntervalTicks)))
            {
                return;
            }

            ApplyAura();
        }

        private void ApplyAura()
        {
            Pawn pawn = PawnParent;
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return;
            }

            foreach (Pawn other in pawn.MapHeld.mapPawns.AllPawnsSpawned)
            {
                if (other == null || other == pawn || other.Dead || other.Downed || !other.Spawned)
                {
                    continue;
                }

                if (!IsEligibleAbyssalAlly(pawn, other))
                {
                    continue;
                }

                if (pawn.PositionHeld.DistanceTo(other.PositionHeld) > Props.allyRadius)
                {
                    continue;
                }

                AbyssalThreatPawnUtility.ApplyOrRefreshHediff(other, Props.allyHediffDefName, Props.allySeverity);
            }
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed;
        }

        private static bool IsEligibleAbyssalAlly(Pawn owner, Pawn other)
        {
            if (owner?.Faction == null || other?.Faction == null)
            {
                return false;
            }

            if (owner.Faction != other.Faction || owner.HostileTo(other))
            {
                return false;
            }

            if (other.TryGetComp<CompAbyssalPawnController>() != null)
            {
                return true;
            }

            string defName = other.def?.defName;
            return !string.IsNullOrEmpty(defName) && defName.StartsWith("ABY_");
        }
    }
}
