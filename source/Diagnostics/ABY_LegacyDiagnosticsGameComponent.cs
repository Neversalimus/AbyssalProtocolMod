using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Diagnostic-only save compatibility scanner.
    /// Package 11A intentionally does not delete, move, despawn, repair, or migrate anything.
    /// It only reports suspicious Abyssal Protocol legacy/orphan state so a later conservative
    /// cleanup package can target confirmed cases safely.
    /// </summary>
    public sealed class ABY_LegacyDiagnosticsGameComponent : GameComponent
    {
        private const int InitialScanDelayTicks = 600;
        private const int ManualRescanIntervalTicks = 60000;

        private bool initialScanCompleted;
        private int nextScanTick = -1;
        private int nextPeriodicScanTick = -1;

        public ABY_LegacyDiagnosticsGameComponent(Game game)
        {
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            ScheduleInitialScan();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            ScheduleInitialScan();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (!initialScanCompleted)
            {
                if (nextScanTick < 0)
                {
                    ScheduleInitialScan();
                }

                if (now >= nextScanTick)
                {
                    initialScanCompleted = true;
                    nextPeriodicScanTick = now + ManualRescanIntervalTicks;
                    RunDiagnostics("initial-load");
                }

                return;
            }

            if (Prefs.DevMode && nextPeriodicScanTick > 0 && now >= nextPeriodicScanTick)
            {
                nextPeriodicScanTick = now + ManualRescanIntervalTicks;
                RunDiagnostics("dev-periodic");
            }
        }

        private void ScheduleInitialScan()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            initialScanCompleted = false;
            nextScanTick = now + InitialScanDelayTicks;
        }

        private static void RunDiagnostics(string reason)
        {
            ABY_LegacyDiagnosticsUtility.DiagnosticsReport report = ABY_LegacyDiagnosticsUtility.BuildReport(reason);
            if (report == null)
            {
                return;
            }

            if (report.IssueCount > 0)
            {
                Log.Warning(report.ToLogString());
                if (Prefs.DevMode)
                {
                    Messages.Message(
                        "[Abyssal Protocol] Legacy diagnostics found " + report.IssueCount + " potential issue(s). See log. No cleanup was performed.",
                        MessageTypeDefOf.CautionInput,
                        false);
                }
            }
            else if (Prefs.DevMode)
            {
                Log.Message(report.ToLogString());
            }
        }
    }
}
