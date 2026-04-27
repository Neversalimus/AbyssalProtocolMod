using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Conservative one-shot cleanup runner for obviously orphaned Abyssal Protocol legacy state.
    /// This is intentionally narrow and avoids active encounters, player pawns, normal colony maps,
    /// Reactor Saint, Horde, jobs, research, UI, and combat systems.
    /// </summary>
    public sealed class ABY_LegacyCleanupGameComponent : GameComponent
    {
        private const int InitialCleanupDelayTicks = 900;

        private bool cleanupCompleted;
        private int scheduledCleanupTick = -1;

        public ABY_LegacyCleanupGameComponent(Game game)
        {
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            ScheduleCleanup();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            ScheduleCleanup();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cleanupCompleted, "abyLegacyCleanup11BCompleted", false);
            Scribe_Values.Look(ref scheduledCleanupTick, "abyLegacyCleanup11BScheduledTick", -1);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (cleanupCompleted || Find.TickManager == null)
            {
                return;
            }

            if (scheduledCleanupTick < 0)
            {
                ScheduleCleanup();
            }

            if (Find.TickManager.TicksGame < scheduledCleanupTick)
            {
                return;
            }

            cleanupCompleted = true;
            ABY_LegacyCleanupUtility.CleanupReport report = ABY_LegacyCleanupUtility.RunConservativeCleanup("initial-load");
            if (report == null)
            {
                return;
            }

            if (report.ActionCount > 0)
            {
                Log.Warning(report.ToLogString());
                if (Prefs.DevMode)
                {
                    Messages.Message(
                        "[Abyssal Protocol] Legacy cleanup applied " + report.ActionCount + " conservative fix(es). See log.",
                        MessageTypeDefOf.CautionInput,
                        false);
                }
            }
            else if (Prefs.DevMode)
            {
                Log.Message(report.ToLogString());
            }
        }

        private void ScheduleCleanup()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            scheduledCleanupTick = now + InitialCleanupDelayTicks;
        }
    }
}
