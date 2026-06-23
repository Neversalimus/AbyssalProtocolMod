using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Save-backed lifecycle records for active Abyssal encounters on a map.
    ///
    /// Normal player paths still enforce one encounter at a time. Multiple records exist only
    /// because the explicitly confirmed Dev rehearsal route can intentionally overlap compatible
    /// encounters for testing. Concrete world state remains authoritative for stale recovery.
    /// </summary>
    public class MapComponent_ABY_SummonEncounterRuntime : MapComponent
    {
        public enum EncounterStage
        {
            None,
            Preparing,
            Active,
            Completed,
            Aborted,
            ClearedAsStale
        }

        public sealed class EncounterRecord : IExposable
        {
            public EncounterStage Stage;
            public int Sequence;
            public string EncounterId;
            public string RitualId;
            public string SummonMode;
            public int OwningCircleId;
            public int StartedTick;
            public int ActivatedTick;
            public int LastMeaningfulProgressTick;
            public int LastStateChangeTick;
            public int NoConcreteSignalSinceTick = -1;
            public string LastReason;

            public bool HasBlockingLifecycle => Stage == EncounterStage.Preparing || Stage == EncounterStage.Active;

            public void ExposeData()
            {
                Scribe_Values.Look(ref Stage, "stage", EncounterStage.None);
                Scribe_Values.Look(ref Sequence, "sequence", 0);
                Scribe_Values.Look(ref EncounterId, "encounterId");
                Scribe_Values.Look(ref RitualId, "ritualId");
                Scribe_Values.Look(ref SummonMode, "summonMode");
                Scribe_Values.Look(ref OwningCircleId, "owningCircleId", 0);
                Scribe_Values.Look(ref StartedTick, "startedTick", 0);
                Scribe_Values.Look(ref ActivatedTick, "activatedTick", 0);
                Scribe_Values.Look(ref LastMeaningfulProgressTick, "lastMeaningfulProgressTick", 0);
                Scribe_Values.Look(ref LastStateChangeTick, "lastStateChangeTick", 0);
                Scribe_Values.Look(ref NoConcreteSignalSinceTick, "noConcreteSignalSinceTick", -1);
                Scribe_Values.Look(ref LastReason, "lastReason");
            }
        }

        private const int WatchdogIntervalTicks = 120;
        private const int ActiveSignalGraceTicks = 600;
        private const int TerminalRecordRetentionTicks = 120000;

        private List<EncounterRecord> records = new List<EncounterRecord>();
        private int sequence;
        private int nextWatchdogTick;

        // Legacy single-record fields remain read only for old saves and are migrated on load.
        private EncounterStage legacyStage;
        private int legacySequence;
        private string legacyEncounterId;
        private string legacyRitualId;
        private string legacySummonMode;
        private int legacyOwningCircleId;
        private int legacyStartedTick;
        private int legacyActivatedTick;
        private int legacyLastMeaningfulProgressTick;
        private int legacyLastStateChangeTick;
        private int legacyNoConcreteSignalSinceTick = -1;
        private string legacyLastReason;

        public MapComponent_ABY_SummonEncounterRuntime(Map map) : base(map)
        {
        }

        public IReadOnlyList<EncounterRecord> Records => records;
        public bool HasBlockingLifecycle
        {
            get
            {
                for (int i = 0; i < records.Count; i++)
                {
                    if (records[i] != null && records[i].HasBlockingLifecycle)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref records, "abySummonEncounter_records", LookMode.Deep);
            Scribe_Values.Look(ref sequence, "abySummonEncounter_sequenceV2", 0);
            Scribe_Values.Look(ref nextWatchdogTick, "abySummonEncounter_nextWatchdogTickV2", 0);

            // Preserve compatibility with saves written by the previous single-record runtime.
            Scribe_Values.Look(ref legacyStage, "abySummonEncounter_stage", EncounterStage.None);
            Scribe_Values.Look(ref legacySequence, "abySummonEncounter_sequence", 0);
            Scribe_Values.Look(ref legacyEncounterId, "abySummonEncounter_id");
            Scribe_Values.Look(ref legacyRitualId, "abySummonEncounter_ritualId");
            Scribe_Values.Look(ref legacySummonMode, "abySummonEncounter_summonMode");
            Scribe_Values.Look(ref legacyOwningCircleId, "abySummonEncounter_circleId", 0);
            Scribe_Values.Look(ref legacyStartedTick, "abySummonEncounter_startedTick", 0);
            Scribe_Values.Look(ref legacyActivatedTick, "abySummonEncounter_activatedTick", 0);
            Scribe_Values.Look(ref legacyLastMeaningfulProgressTick, "abySummonEncounter_lastProgressTick", 0);
            Scribe_Values.Look(ref legacyLastStateChangeTick, "abySummonEncounter_lastStateTick", 0);
            Scribe_Values.Look(ref legacyNoConcreteSignalSinceTick, "abySummonEncounter_noSignalSinceTick", -1);
            Scribe_Values.Look(ref legacyLastReason, "abySummonEncounter_lastReason");
            Scribe_Values.Look(ref nextWatchdogTick, "abySummonEncounter_nextWatchdogTick", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (records == null)
                {
                    records = new List<EncounterRecord>();
                }

                MigrateLegacyRecordIfNeeded();
                PruneTerminalRecords(Now);
            }
        }

        public void BeginPreparation(Building_AbyssalSummoningCircle circle, CompProperties_UseEffectSummonBoss props)
        {
            if (circle == null || props == null || map == null)
            {
                return;
            }

            int now = Now;
            EncounterRecord existing = FindLatestRecordForCircle(circle, true);
            if (existing != null && existing.HasBlockingLifecycle)
            {
                SetTerminal(existing, EncounterStage.Aborted, "A new preparation replaced an unfinished lifecycle on the same summoning circle.");
            }

            sequence++;
            EncounterRecord record = new EncounterRecord
            {
                Stage = EncounterStage.Preparing,
                Sequence = sequence,
                EncounterId = "aby-" + map.uniqueID + "-" + circle.thingIDNumber + "-" + sequence + "-" + now,
                RitualId = props.ritualId ?? string.Empty,
                SummonMode = props.summonMode ?? "Boss",
                OwningCircleId = circle.thingIDNumber,
                StartedTick = now,
                ActivatedTick = 0,
                LastMeaningfulProgressTick = now,
                LastStateChangeTick = now,
                NoConcreteSignalSinceTick = -1,
                LastReason = "Invocation committed; ritual preparation is in progress."
            };
            records.Add(record);
            AbyssalBossSummonUtility.NotifyActiveEncounterStateMaybeChanged(map);
        }

        public void NotifyRitualPhase(Building_AbyssalSummoningCircle circle, string phase)
        {
            EncounterRecord record = FindLatestRecordForCircle(circle, false);
            if (record == null || record.Stage != EncounterStage.Preparing)
            {
                return;
            }

            record.LastMeaningfulProgressTick = Now;
            record.LastReason = "Ritual phase: " + (phase ?? "unknown") + ".";
            AbyssalBossSummonUtility.NotifyActiveEncounterStateMaybeChanged(map);
        }

        public void Activate(Building_AbyssalSummoningCircle circle, string activatedRitualId, string activatedSummonMode)
        {
            if (circle == null || map == null)
            {
                return;
            }

            EncounterRecord record = FindLatestRecordForCircle(circle, false);
            if (record == null)
            {
                record = BeginExternal(circle, activatedRitualId, activatedSummonMode);
            }

            record.Stage = EncounterStage.Active;
            record.RitualId = activatedRitualId ?? record.RitualId ?? string.Empty;
            record.SummonMode = activatedSummonMode ?? record.SummonMode ?? "Boss";
            record.ActivatedTick = Now;
            record.LastMeaningfulProgressTick = record.ActivatedTick;
            record.LastStateChangeTick = record.ActivatedTick;
            record.NoConcreteSignalSinceTick = -1;
            record.LastReason = "Encounter manifestation accepted by the map.";
            AbyssalBossSummonUtility.NotifyActiveEncounterStateMaybeChanged(map);
        }

        public EncounterRecord BeginExternal(Building_AbyssalSummoningCircle circle, string externalRitualId, string externalSummonMode)
        {
            if (circle == null || map == null)
            {
                return null;
            }

            int now = Now;
            sequence++;
            EncounterRecord record = new EncounterRecord
            {
                Stage = EncounterStage.Active,
                Sequence = sequence,
                EncounterId = "aby-" + map.uniqueID + "-" + circle.thingIDNumber + "-external-" + sequence + "-" + now,
                RitualId = externalRitualId ?? string.Empty,
                SummonMode = externalSummonMode ?? "Boss",
                OwningCircleId = circle.thingIDNumber,
                StartedTick = now,
                ActivatedTick = now,
                LastMeaningfulProgressTick = now,
                LastStateChangeTick = now,
                NoConcreteSignalSinceTick = -1,
                LastReason = "External Abyssal encounter started."
            };
            records.Add(record);
            return record;
        }

        public void NotifyCircleRitualReset(Building_AbyssalSummoningCircle circle)
        {
            EncounterRecord record = FindLatestRecordForCircle(circle, false);
            if (record == null || record.Stage != EncounterStage.Preparing)
            {
                return;
            }

            SetTerminal(record, EncounterStage.Aborted, "Ritual ended before an encounter was activated.");
        }

        public void AbortPreparation(Building_AbyssalSummoningCircle circle, string reason)
        {
            EncounterRecord record = FindLatestRecordForCircle(circle, false);
            if (record == null || record.Stage != EncounterStage.Preparing)
            {
                return;
            }

            SetTerminal(record, EncounterStage.Aborted, reason.NullOrEmpty() ? "Invocation aborted before activation." : reason);
        }

        public void NotifyWorldStateChanged()
        {
            int now = Now;
            for (int i = 0; i < records.Count; i++)
            {
                EncounterRecord record = records[i];
                if (record != null && record.HasBlockingLifecycle)
                {
                    record.LastMeaningfulProgressTick = now;
                    record.NoConcreteSignalSinceTick = -1;
                }
            }
        }

        public bool TryGetBlocker(out string blocker)
        {
            blocker = null;
            EncounterRecord latestPreparing = null;
            EncounterRecord latestActive = null;
            for (int i = 0; i < records.Count; i++)
            {
                EncounterRecord record = records[i];
                if (record == null || !record.HasBlockingLifecycle)
                {
                    continue;
                }

                if (record.Stage == EncounterStage.Preparing)
                {
                    latestPreparing = PickLater(latestPreparing, record);
                }
                else if (record.Stage == EncounterStage.Active)
                {
                    latestActive = PickLater(latestActive, record);
                }
            }

            if (latestPreparing != null)
            {
                blocker = AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_SummonBlocker_RuntimePreparing",
                    "A previous invocation is still preparing at the summoning circle.");
                return true;
            }

            if (latestActive != null)
            {
                blocker = AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_SummonBlocker_RuntimeActive",
                    "A previous Abyssal encounter is still active on this map.");
                return true;
            }

            return false;
        }

        public void AppendDiagnosticReport(StringBuilder sb)
        {
            if (sb == null)
            {
                return;
            }

            sb.AppendLine("Runtime encounter lifecycles: " + records.Count);
            if (records.Count == 0)
            {
                sb.AppendLine(" - none");
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                EncounterRecord record = records[i];
                if (record == null)
                {
                    continue;
                }

                sb.AppendLine(" - [" + i + "] " + record.Stage
                    + " | id=" + (record.EncounterId ?? "none")
                    + " | ritual=" + (record.RitualId ?? "none")
                    + " | mode=" + (record.SummonMode ?? "none")
                    + " | circle=" + record.OwningCircleId
                    + " | started=" + record.StartedTick
                    + " | activated=" + record.ActivatedTick
                    + " | lastProgress=" + record.LastMeaningfulProgressTick
                    + " | reason=" + (record.LastReason ?? "none"));
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null || Find.TickManager == null || records.Count == 0)
            {
                return;
            }

            int now = Now;
            if (now < nextWatchdogTick)
            {
                return;
            }

            nextWatchdogTick = now + WatchdogIntervalTicks;
            RunWatchdog(now);
            PruneTerminalRecords(now);
        }

        private void RunWatchdog(int now)
        {
            for (int i = records.Count - 1; i >= 0; i--)
            {
                EncounterRecord record = records[i];
                if (record == null || !record.HasBlockingLifecycle)
                {
                    continue;
                }

                if (record.Stage == EncounterStage.Preparing)
                {
                    Building_AbyssalSummoningCircle owner = FindOwningCircle(record);
                    if (owner != null && owner.RitualActive)
                    {
                        continue;
                    }

                    SetTerminal(record, EncounterStage.Aborted, "Preparation watchdog cleared a ritual record after the owning circle stopped.");
                    continue;
                }

                if (AbyssalBossSummonUtility.TryGetConcreteActiveAbyssalEncounterBlocker(map, out _))
                {
                    record.NoConcreteSignalSinceTick = -1;
                    record.LastMeaningfulProgressTick = now;
                    continue;
                }

                if (record.NoConcreteSignalSinceTick < 0)
                {
                    record.NoConcreteSignalSinceTick = now;
                    continue;
                }

                if (now - record.NoConcreteSignalSinceTick >= ActiveSignalGraceTicks)
                {
                    SetTerminal(record, EncounterStage.ClearedAsStale, "State watchdog verified no active Abyssal encounter objects, portals, waves, Dominion crisis or combat-capable Abyssal pawns.");
                }
            }
        }

        private EncounterRecord FindLatestRecordForCircle(Building_AbyssalSummoningCircle circle, bool includeTerminal)
        {
            if (circle == null)
            {
                return null;
            }

            EncounterRecord result = null;
            for (int i = 0; i < records.Count; i++)
            {
                EncounterRecord record = records[i];
                if (record == null || record.OwningCircleId != circle.thingIDNumber || (!includeTerminal && !record.HasBlockingLifecycle))
                {
                    continue;
                }

                result = PickLater(result, record);
            }

            return result;
        }

        private static EncounterRecord PickLater(EncounterRecord first, EncounterRecord second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return second.Sequence >= first.Sequence ? second : first;
        }

        private Building_AbyssalSummoningCircle FindOwningCircle(EncounterRecord record)
        {
            if (record == null || record.OwningCircleId <= 0 || map?.listerThings?.AllThings == null)
            {
                return null;
            }

            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Building_AbyssalSummoningCircle circle = things[i] as Building_AbyssalSummoningCircle;
                if (circle != null && !circle.Destroyed && circle.thingIDNumber == record.OwningCircleId)
                {
                    return circle;
                }
            }

            return null;
        }

        private void SetTerminal(EncounterRecord record, EncounterStage terminalStage, string reason)
        {
            if (record == null)
            {
                return;
            }

            record.Stage = terminalStage;
            record.LastReason = reason;
            record.LastStateChangeTick = Now;
            record.NoConcreteSignalSinceTick = -1;
            AbyssalBossSummonUtility.NotifyActiveEncounterStateMaybeChanged(map);
        }

        private void PruneTerminalRecords(int now)
        {
            if (records == null)
            {
                records = new List<EncounterRecord>();
                return;
            }

            for (int i = records.Count - 1; i >= 0; i--)
            {
                EncounterRecord record = records[i];
                if (record == null)
                {
                    records.RemoveAt(i);
                    continue;
                }

                if (!record.HasBlockingLifecycle
                    && record.LastStateChangeTick > 0
                    && now - record.LastStateChangeTick > TerminalRecordRetentionTicks)
                {
                    records.RemoveAt(i);
                }
            }
        }

        private void MigrateLegacyRecordIfNeeded()
        {
            if (legacyStage != EncounterStage.Preparing && legacyStage != EncounterStage.Active)
            {
                return;
            }

            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] != null && records[i].EncounterId == legacyEncounterId)
                {
                    return;
                }
            }

            EncounterRecord migrated = new EncounterRecord
            {
                Stage = legacyStage,
                Sequence = Math.Max(legacySequence, sequence + 1),
                EncounterId = legacyEncounterId,
                RitualId = legacyRitualId,
                SummonMode = legacySummonMode,
                OwningCircleId = legacyOwningCircleId,
                StartedTick = legacyStartedTick,
                ActivatedTick = legacyActivatedTick,
                LastMeaningfulProgressTick = legacyLastMeaningfulProgressTick,
                LastStateChangeTick = legacyLastStateChangeTick,
                NoConcreteSignalSinceTick = legacyNoConcreteSignalSinceTick,
                LastReason = legacyLastReason ?? "Migrated from the previous single-record summon runtime."
            };
            records.Add(migrated);
            sequence = Math.Max(sequence, migrated.Sequence);
        }

        private int Now => Find.TickManager != null ? Find.TickManager.TicksGame : 0;
    }
}
