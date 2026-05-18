using Verse;

namespace AbyssalProtocol
{
    public class ABY_StabilityDiagnosticsGameComponent : GameComponent
    {
        private int nextHeartbeatTick;

        public ABY_StabilityDiagnosticsGameComponent(Game game)
        {
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (AbyssalProtocolMod.Settings?.showHarmonyPatchReportOnLoad ?? true)
            {
                ABY_HarmonyPatchReportUtility.LogReport(false);
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (!ABY_StabilityDiagnosticsUtility.DiagnosticsEnabled)
            {
                return;
            }

            int tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (tick < nextHeartbeatTick)
            {
                return;
            }

            nextHeartbeatTick = tick + (ABY_StabilityDiagnosticsUtility.VerboseDiagnostics ? 2500 : 9000);
            if (ABY_StabilityDiagnosticsUtility.VerboseDiagnostics)
            {
                ABY_StabilityDiagnosticsUtility.LogSnapshot(false);
            }
        }
    }
}
