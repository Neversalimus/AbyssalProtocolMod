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
        private readonly HashSet<int> processedReactorSaintPawnIdLookup = new HashSet<int>();

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

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RebuildProcessedReactorSaintLookup();
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
                    if (processedReactorSaintPawnIdLookup.Contains(pawnId))
                    {
                        continue;
                    }

                    AddProcessedReactorSaintPawnId(pawnId);
                    firstReactorSaintKillRecorded = true;

                    TrySendReactorSaintKillLetter(map, corpse.PositionHeld);
                    AbyssalProgressRecapUtility.SendReactorRecap(map, corpse.PositionHeld);
                    return;
                }
            }
        }

        private void AddProcessedReactorSaintPawnId(int pawnId)
        {
            if (processedReactorSaintPawnIds == null)
            {
                processedReactorSaintPawnIds = new List<int>();
            }

            if (processedReactorSaintPawnIdLookup.Add(pawnId))
            {
                processedReactorSaintPawnIds.Add(pawnId);
            }
        }

        private void RebuildProcessedReactorSaintLookup()
        {
            if (processedReactorSaintPawnIds == null)
            {
                processedReactorSaintPawnIds = new List<int>();
            }

            processedReactorSaintPawnIdLookup.Clear();
            for (int i = 0; i < processedReactorSaintPawnIds.Count; i++)
            {
                processedReactorSaintPawnIdLookup.Add(processedReactorSaintPawnIds[i]);
            }
        }

        private static void TrySendReactorSaintKillLetter(Map map, IntVec3 cell)
        {
            try
            {
                if (Find.LetterStack == null)
                {
                    return;
                }

                ABY_LetterUtility.TryReceiveLetter(
                    "ABY_ReactorSaintKillLabel".Translate(),
                    "ABY_ReactorSaintKillDesc".Translate(),
                    LetterDefOf.PositiveEvent,
                    map != null && cell.IsValid ? new LookTargets(new TargetInfo(cell, map)) : null);
            }
            catch
            {
                // The progression flag is more important than a notification if a heavily-modded LetterStack fails.
            }
        }
    }
}
