using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Repairs hidden Abyssal faction relation rows in existing saves.
    ///
    /// ABY_AbyssalHost is generated on demand and hidden, so older saves or mid-combat generated factions can
    /// miss the PlayerColony relation row that vanilla melee/damage code expects. Keeping the repair here makes
    /// vanilla HostileTo/PreApplyDamage paths safe even when they do not go through Abyssal helper utilities.
    /// </summary>
    public sealed class ABY_FactionRelationRepairGameComponent : GameComponent
    {
        private int nextRepairTick;

        public ABY_FactionRelationRepairGameComponent(Game game)
        {
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            RepairNow();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            RepairNow();
        }

        public override void GameComponentTick()
        {
            int ticks = Find.TickManager?.TicksGame ?? 0;
            if (ticks < nextRepairTick)
            {
                return;
            }

            nextRepairTick = ticks + 600;
            RepairNow();
        }

        public static void RepairNow()
        {
            ABY_FactionHostilityUtility.RepairAllAbyssalFactionRelations();
        }
    }
}
