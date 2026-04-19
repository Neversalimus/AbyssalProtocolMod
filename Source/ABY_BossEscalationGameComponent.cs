using RimWorld;
using System.Collections.Generic;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_BossEscalationScheduledEscort : IExposable
    {
        public int mapUniqueId = -1;
        public int triggerTick = 0;
        public string ritualId = string.Empty;
        public string bossKindDefName = string.Empty;
        public string packageDefName = string.Empty;
        public string packLabel = string.Empty;
        public IntVec3 fallbackCell = IntVec3.Invalid;
        public float fallbackBudget = 0f;

        public void ExposeData()
        {
            Scribe_Values.Look(ref mapUniqueId, "mapUniqueId", -1);
            Scribe_Values.Look(ref triggerTick, "triggerTick", 0);
            Scribe_Values.Look(ref ritualId, "ritualId");
            Scribe_Values.Look(ref bossKindDefName, "bossKindDefName");
            Scribe_Values.Look(ref packageDefName, "packageDefName");
            Scribe_Values.Look(ref packLabel, "packLabel");
            Scribe_Values.Look(ref fallbackCell, "fallbackCell");
            Scribe_Values.Look(ref fallbackBudget, "fallbackBudget", 0f);
        }
    }

    public sealed class ABY_BossEscalationGameComponent : GameComponent
    {
        private List<ABY_BossEscalationScheduledEscort> scheduledEscorts = new List<ABY_BossEscalationScheduledEscort>();

        public ABY_BossEscalationGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref scheduledEscorts, "scheduledEscorts", LookMode.Deep);
            if (scheduledEscorts == null)
            {
                scheduledEscorts = new List<ABY_BossEscalationScheduledEscort>();
            }
        }

        public override void GameComponentTick()
        {
            if (scheduledEscorts == null || scheduledEscorts.Count == 0 || Find.TickManager == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (currentTick % 30 != 0)
            {
                return;
            }

            for (int i = scheduledEscorts.Count - 1; i >= 0; i--)
            {
                ABY_BossEscalationScheduledEscort escort = scheduledEscorts[i];
                if (escort == null)
                {
                    scheduledEscorts.RemoveAt(i);
                    continue;
                }

                if (escort.triggerTick > currentTick)
                {
                    continue;
                }

                Map map = Find.Maps?.Find(m => m != null && m.uniqueID == escort.mapUniqueId);
                if (map != null)
                {
                    Faction faction = AbyssalBossSummonUtility.ResolveHostileFaction();
                    if (faction != null)
                    {
                        IntVec3 anchor = AbyssalBossOrchestrationUtility.TryResolveActiveBossAnchorCell(map, escort.ritualId, escort.bossKindDefName, escort.fallbackCell);
                        AbyssalBossOrchestrationUtility.TrySpawnEscortPack(
                            map,
                            faction,
                            escort.ritualId,
                            anchor,
                            escort.fallbackBudget,
                            escort.packLabel,
                            out _,
                            out string failReason,
                            escort.packageDefName,
                            true,
                            false);

                        if (!failReason.NullOrEmpty())
                        {
                            Log.Warning("[Abyssal Protocol] Delayed boss escalation escort warning: " + failReason);
                        }
                    }
                }

                scheduledEscorts.RemoveAt(i);
            }
        }

        public void ScheduleEscort(ABY_BossEscalationScheduledEscort escort)
        {
            if (escort == null)
            {
                return;
            }

            if (scheduledEscorts == null)
            {
                scheduledEscorts = new List<ABY_BossEscalationScheduledEscort>();
            }

            scheduledEscorts.Add(escort);
        }
    }
}
