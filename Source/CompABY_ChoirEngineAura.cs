using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_ChoirEngineAura : ThingComp
    {
        public CompProperties_ABY_ChoirEngineAura Props => (CompProperties_ABY_ChoirEngineAura)props;

        private Pawn PawnParent => parent as Pawn;

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn)
                || IsRelaySuppressed(pawn)
                || !parent.IsHashIntervalTick(System.Math.Max(15, Props.scanIntervalTicks)))
            {
                return;
            }

            ApplyAura(Props.allyRadius, Props.enemyRadius, Props.allySeverity, Props.enemySeverity);
        }

        public void ApplyPulseAura(float allyRadius, float enemyRadius, float allySeverity, float enemySeverity)
        {
            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || IsRelaySuppressed(pawn))
            {
                return;
            }

            ApplyAura(allyRadius, enemyRadius, allySeverity, enemySeverity);
        }

        private static bool IsRelaySuppressed(Pawn pawn)
        {
            CompABY_ChoirEngineRelay relay = pawn?.TryGetComp<CompABY_ChoirEngineRelay>();
            return relay != null && relay.IsDisrupted;
        }

        private void ApplyAura(float allyRadius, float enemyRadius, float allySeverity, float enemySeverity)
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

                float distance = pawn.PositionHeld.DistanceTo(other.PositionHeld);
                if (IsFriendlyTarget(pawn, other) && distance <= allyRadius)
                {
                    AbyssalThreatPawnUtility.ApplyOrRefreshHediff(other, Props.allyHediffDefName, allySeverity);
                }
                else if (IsEnemyTarget(pawn, other) && distance <= enemyRadius)
                {
                    AbyssalThreatPawnUtility.ApplyOrRefreshHediff(other, Props.enemyHediffDefName, enemySeverity);
                }
            }
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && pawn.MapHeld != null && !pawn.Dead && !pawn.Downed;
        }

        private static bool IsFriendlyTarget(Pawn owner, Pawn other)
        {
            if (owner?.Faction == null || other?.Faction == null)
            {
                return false;
            }

            return owner.Faction == other.Faction && !owner.HostileTo(other);
        }

        private static bool IsEnemyTarget(Pawn owner, Pawn other)
        {
            return owner?.Faction != null && other?.Faction != null && owner.HostileTo(other);
        }
    }
}
