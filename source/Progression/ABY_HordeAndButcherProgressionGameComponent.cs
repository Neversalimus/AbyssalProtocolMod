using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class ABY_HordeAndButcherProgressionGameComponent : GameComponent
    {
        private const string RiftButcherRaceDefName = "ABY_RiftButcher";
        private const string RiftButcherPawnKindDefName = "ABY_RiftButcher";
        private const int ScanIntervalTicks = 90;

        private bool firstHordeClearRecorded;
        private bool firstRiftButcherKillRecorded;
        private int nextButcherScanTick;
        private List<int> processedRiftButcherPawnIds = new List<int>();
        private HashSet<int> processedRiftButcherPawnIdSet = new HashSet<int>();

        public bool FirstHordeClearRecorded => firstHordeClearRecorded;
        public bool FirstRiftButcherKillRecorded => firstRiftButcherKillRecorded;

        public ABY_HordeAndButcherProgressionGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref firstHordeClearRecorded, "firstHordeClearRecorded", false);
            Scribe_Values.Look(ref firstRiftButcherKillRecorded, "firstRiftButcherKillRecorded", false);
            Scribe_Values.Look(ref nextButcherScanTick, "nextButcherScanTick", 0);
            Scribe_Collections.Look(ref processedRiftButcherPawnIds, "processedRiftButcherPawnIds", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (processedRiftButcherPawnIds == null)
                {
                    processedRiftButcherPawnIds = new List<int>();
                }

                RebuildProcessedRiftButcherPawnIdSet();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager == null || Find.Maps == null || firstRiftButcherKillRecorded)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame < nextButcherScanTick)
            {
                return;
            }

            nextButcherScanTick = ticksGame + ScanIntervalTicks;
            TryRecordFirstRiftButcherKill();
        }

        public void RecordHordeClear(Map map, IntVec3 cell)
        {
            if (firstHordeClearRecorded)
            {
                return;
            }

            firstHordeClearRecorded = true;
            try
            {
                if (Find.LetterStack != null)
                {
                    LookTargets targets = map != null && cell.IsValid
                        ? new LookTargets(new TargetInfo(cell, map))
                        : null;
                    Find.LetterStack.ReceiveLetter(
                        "ABY_FirstHordeClearLabel".Translate(),
                        "ABY_FirstHordeClearDesc".Translate(),
                        LetterDefOf.PositiveEvent,
                        targets);
                }
            }
            catch
            {
                // Progression state is more important than a notification if a heavily-modded letter stack fails.
            }
        }

        private void TryRecordFirstRiftButcherKill()
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
                    if (!IsRiftButcher(deadPawn))
                    {
                        continue;
                    }

                    int pawnId = deadPawn.thingIDNumber;
                    if (processedRiftButcherPawnIdSet.Contains(pawnId))
                    {
                        continue;
                    }

                    RecordProcessedRiftButcherPawnId(pawnId);
                    firstRiftButcherKillRecorded = true;
                    TrySendFirstRiftButcherKillLetter(corpse, map);
                    return;
                }
            }
        }

        private void RecordProcessedRiftButcherPawnId(int pawnId)
        {
            if (processedRiftButcherPawnIdSet.Add(pawnId))
            {
                if (processedRiftButcherPawnIds == null)
                {
                    processedRiftButcherPawnIds = new List<int>();
                }

                processedRiftButcherPawnIds.Add(pawnId);
            }
        }

        private void RebuildProcessedRiftButcherPawnIdSet()
        {
            processedRiftButcherPawnIdSet = new HashSet<int>();
            if (processedRiftButcherPawnIds == null)
            {
                return;
            }

            for (int i = 0; i < processedRiftButcherPawnIds.Count; i++)
            {
                processedRiftButcherPawnIdSet.Add(processedRiftButcherPawnIds[i]);
            }
        }

        private static void TrySendFirstRiftButcherKillLetter(Corpse corpse, Map map)
        {
            try
            {
                if (Find.LetterStack == null)
                {
                    return;
                }

                LookTargets targets = corpse != null && map != null
                    ? new LookTargets(new TargetInfo(corpse.PositionHeld, map))
                    : null;

                Find.LetterStack.ReceiveLetter(
                    "ABY_RiftButcherKillLabel".Translate(),
                    "ABY_RiftButcherKillDesc".Translate(),
                    LetterDefOf.PositiveEvent,
                    targets);
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "rift-butcher-first-kill-letter-failed",
                    "[Abyssal Protocol] First Rift Butcher kill was recorded, but the notification letter failed: " + ex.GetType().Name + ": " + ex.Message,
                    999999);
            }
        }

        private static bool IsRiftButcher(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.def?.defName == RiftButcherRaceDefName)
            {
                return true;
            }

            return pawn.kindDef?.defName == RiftButcherPawnKindDefName;
        }
    }
}
