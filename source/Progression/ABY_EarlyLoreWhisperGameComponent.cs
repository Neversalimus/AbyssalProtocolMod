using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class ABY_EarlyLoreWhisperGameComponent : GameComponent
    {
        private const string SignalTheoryDefName = "ABY_AbyssalSignalTheory";
        private const int ScanIntervalTicks = 150;
        private const int DelayTicks = 15 * 2500;

        private bool signalTheoryCompletionObserved;
        private bool loreWhisperLetterSent;
        private int signalTheoryCompletionTick = -1;
        private int nextScanTick;

        public ABY_EarlyLoreWhisperGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref signalTheoryCompletionObserved, "signalTheoryCompletionObserved", false);
            Scribe_Values.Look(ref loreWhisperLetterSent, "loreWhisperLetterSent", false);
            Scribe_Values.Look(ref signalTheoryCompletionTick, "signalTheoryCompletionTick", -1);
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (loreWhisperLetterSent || Find.TickManager == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < nextScanTick)
            {
                return;
            }

            nextScanTick = ticksGame + ScanIntervalTicks;
            TryTrackSignalTheoryCompletion(ticksGame);
            TrySendLoreWhisper(ticksGame);
        }

        private void TryTrackSignalTheoryCompletion(int ticksGame)
        {
            if (signalTheoryCompletionObserved)
            {
                return;
            }

            ResearchProjectDef signalTheory = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(SignalTheoryDefName);
            if (signalTheory == null || !signalTheory.IsFinished)
            {
                return;
            }

            signalTheoryCompletionObserved = true;
            signalTheoryCompletionTick = ticksGame;
        }

        private void TrySendLoreWhisper(int ticksGame)
        {
            if (!signalTheoryCompletionObserved || signalTheoryCompletionTick < 0)
            {
                return;
            }

            if (ticksGame < signalTheoryCompletionTick + DelayTicks)
            {
                return;
            }

            loreWhisperLetterSent = true;
            Find.LetterStack.ReceiveLetter(
                "ABY_EarlyLoreWhisperLabel".Translate(),
                "ABY_EarlyLoreWhisperDesc".Translate(),
                LetterDefOf.NeutralEvent);
        }
    }
}
