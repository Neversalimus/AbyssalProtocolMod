using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class ABY_ReactorSaintProgressionGameComponent : GameComponent
    {
        private const string ReactorSaintRaceDefName = "ABY_ReactorSaint";
        private const int ScanIntervalTicks = 90;

        private bool firstReactorSaintKillRecorded;
        private int nextScanTick;
        private List<int> processedReactorSaintPawnIds = new List<int>();

        public bool FirstReactorSaintKillRecorded => firstReactorSaintKillRecorded;

        public ABY_ReactorSaintProgressionGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref firstReactorSaintKillRecorded, "firstReactorSaintKillRecorded", false);
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
            Scribe_Collections.Look(ref processedReactorSaintPawnIds, "processedReactorSaintPawnIds", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && processedReactorSaintPawnIds == null)
            {
                processedReactorSaintPawnIds = new List<int>();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager == null || Find.Maps == null || firstReactorSaintKillRecorded)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < nextScanTick)
            {
                return;
            }

            nextScanTick = ticksGame + ScanIntervalTicks;
            TryRecordFirstReactorSaintKill();
        }

        private void TryRecordFirstReactorSaintKill()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map?.listerThings == null)
                {
                    continue;
                }

                List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                if (corpses == null)
                {
                    continue;
                }

                for (int j = 0; j < corpses.Count; j++)
                {
                    if (!(corpses[j] is Corpse corpse) || corpse.InnerPawn == null)
                    {
                        continue;
                    }

                    Pawn deadPawn = corpse.InnerPawn;
                    if (deadPawn.def?.defName != ReactorSaintRaceDefName)
                    {
                        continue;
                    }

                    int pawnId = deadPawn.thingIDNumber;
                    if (processedReactorSaintPawnIds.Contains(pawnId))
                    {
                        continue;
                    }

                    processedReactorSaintPawnIds.Add(pawnId);
                    firstReactorSaintKillRecorded = true;

                    Find.LetterStack.ReceiveLetter(
                        "ABY_ReactorSaintKillLabel".Translate(),
                        "ABY_ReactorSaintKillDesc".Translate(),
                        LetterDefOf.PositiveEvent,
                        new LookTargets(new TargetInfo(corpse.PositionHeld, map)));

                    AbyssalProgressRecapUtility.SendReactorRecap(map, corpse.PositionHeld);
                    return;
                }
            }
        }
    }
}
