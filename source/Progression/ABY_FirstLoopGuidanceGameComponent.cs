using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class ABY_FirstLoopGuidanceGameComponent : GameComponent
    {
        private const string SignalTheoryDefName = "ABY_AbyssalSignalTheory";
        private const string SigilDefName = "ABY_ArchonSigil";
        private const int ScanIntervalTicks = 150;

        private bool signalTheoryLetterSent;
        private bool firstSigilLetterSent;
        private int nextScanTick;

        public ABY_FirstLoopGuidanceGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref signalTheoryLetterSent, "signalTheoryLetterSent", false);
            Scribe_Values.Look(ref firstSigilLetterSent, "firstSigilLetterSent", false);
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager == null || Find.Maps == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < nextScanTick)
            {
                return;
            }

            nextScanTick = ticksGame + ScanIntervalTicks;
            TrySendSignalTheoryLetter();
            TrySendFirstSigilLetter();
        }

        private void TrySendSignalTheoryLetter()
        {
            if (signalTheoryLetterSent)
            {
                return;
            }

            ResearchProjectDef signalTheory = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(SignalTheoryDefName);
            if (signalTheory == null || !signalTheory.IsFinished)
            {
                return;
            }

            signalTheoryLetterSent = true;
            Find.LetterStack.ReceiveLetter(
                "ABY_SignalTheoryGuidanceLabel".Translate(),
                "ABY_SignalTheoryGuidanceDesc".Translate(),
                LetterDefOf.PositiveEvent);
        }

        private void TrySendFirstSigilLetter()
        {
            if (firstSigilLetterSent)
            {
                return;
            }

            if (!TryFindAnySigil(out Thing sigil))
            {
                return;
            }

            firstSigilLetterSent = true;
            LookTargets lookTargets = sigil != null ? new LookTargets(sigil) : LookTargets.Invalid;
            Find.LetterStack.ReceiveLetter(
                "ABY_FirstSigilGuidanceLabel".Translate(),
                "ABY_FirstSigilGuidanceDesc".Translate(),
                LetterDefOf.PositiveEvent,
                lookTargets);
        }

        private static bool TryFindAnySigil(out Thing sigil)
        {
            sigil = null;
            ThingDef sigilDef = DefDatabase<ThingDef>.GetNamedSilentFail(SigilDefName);
            if (sigilDef == null || Find.Maps == null)
            {
                return false;
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map?.listerThings == null)
                {
                    continue;
                }

                List<Thing> things = map.listerThings.ThingsOfDef(sigilDef);
                if (things == null || things.Count == 0)
                {
                    continue;
                }

                for (int j = 0; j < things.Count; j++)
                {
                    Thing thing = things[j];
                    if (thing == null || thing.Destroyed)
                    {
                        continue;
                    }

                    sigil = thing;
                    return true;
                }
            }

            return false;
        }
    }
}
