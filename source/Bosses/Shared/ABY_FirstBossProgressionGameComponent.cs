using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class ABY_FirstBossProgressionGameComponent : GameComponent
    {
        private const int ScanIntervalTicks = 90;

        private bool firstBeastKillRecorded;
        private int nextScanTick;
        private List<int> processedArchonPawnIds = new List<int>();
        private readonly HashSet<int> processedArchonPawnIdLookup = new HashSet<int>();

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

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RebuildProcessedArchonLookup();
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
                    if (!AbyssalArchonVariantUtility.IsArchonBeastFamily(deadPawn))
                    {
                        continue;
                    }

                    int pawnId = deadPawn.thingIDNumber;
                    if (processedArchonPawnIdLookup.Contains(pawnId))
                    {
                        continue;
                    }

                    AddProcessedArchonPawnId(pawnId);
                    firstBeastKillRecorded = true;

                    TrySendFirstBossKillLetter(map, corpse.PositionHeld);
                    AbyssalProgressRecapUtility.SendFirstBossRecap(map, corpse.PositionHeld);
                    return;
                }
            }
        }

        private void AddProcessedArchonPawnId(int pawnId)
        {
            if (processedArchonPawnIds == null)
            {
                processedArchonPawnIds = new List<int>();
            }

            if (processedArchonPawnIdLookup.Add(pawnId))
            {
                processedArchonPawnIds.Add(pawnId);
            }
        }

        private void RebuildProcessedArchonLookup()
        {
            if (processedArchonPawnIds == null)
            {
                processedArchonPawnIds = new List<int>();
            }

            processedArchonPawnIdLookup.Clear();
            for (int i = 0; i < processedArchonPawnIds.Count; i++)
            {
                processedArchonPawnIdLookup.Add(processedArchonPawnIds[i]);
            }
        }

        private static void TrySendFirstBossKillLetter(Map map, IntVec3 cell)
        {
            try
            {
                ABY_LetterUtility.TryReceiveLetter(
                    "ABY_FirstBossKillLabel".Translate(),
                    "ABY_FirstBossKillDesc".Translate(),
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
