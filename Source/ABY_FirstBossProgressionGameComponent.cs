using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class ABY_FirstBossProgressionGameComponent : GameComponent
    {
        private const string ArchonBeastRaceDefName = "ABY_ArchonBeast";
        private const int ScanIntervalTicks = 90;

        private bool firstBeastKillRecorded;
        private int nextScanTick;
        private List<int> processedArchonPawnIds = new List<int>();

        public bool FirstBossKillRecorded => firstBeastKillRecorded;

        public ABY_FirstBossProgressionGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref firstBeastKillRecorded, "firstBeastKillRecorded", false);
            Scribe_Values.Look(ref nextScanTick, "nextScanTick", 0);
            Scribe_Collections.Look(ref processedArchonPawnIds, "processedArchonPawnIds", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && processedArchonPawnIds == null)
            {
                processedArchonPawnIds = new List<int>();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager == null || Find.Maps == null || firstBeastKillRecorded)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < nextScanTick)
            {
                return;
            }

            nextScanTick = ticksGame + ScanIntervalTicks;
            TryRecordFirstBeastKill();
        }

        private void TryRecordFirstBeastKill()
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
                    if (deadPawn.def?.defName != ArchonBeastRaceDefName)
                    {
                        continue;
                    }

                    int pawnId = deadPawn.thingIDNumber;
                    if (processedArchonPawnIds.Contains(pawnId))
                    {
                        continue;
                    }

                    processedArchonPawnIds.Add(pawnId);
                    firstBeastKillRecorded = true;

                    Find.LetterStack.ReceiveLetter(
                        "ABY_FirstBossKillLabel".Translate(),
                        "ABY_FirstBossKillDesc".Translate(),
                        LetterDefOf.PositiveEvent,
                        new LookTargets(new TargetInfo(corpse.PositionHeld, map)));

                    AbyssalProgressRecapUtility.SendFirstBossRecap(map, corpse.PositionHeld);
                    return;
                }
            }
        }
    }
}
