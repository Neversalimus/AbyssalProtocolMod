using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Save-backed ownership record for the single active Abyssal summon pipeline on a map.
    /// It is a safety layer, not a replacement for concrete world checks: stale records are
    /// cleared only after the map no longer contains a ritual, portal, manifestation, wave,
    /// Dominion crisis or live combat-capable Abyssal pawn.
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

        private const int WatchdogIntervalTicks = 120;
        private const int ActiveSignalGraceTicks = 600;

        private EncounterStage stage;
        private int sequence;
        private string encounterId;
        private string ritualId;
        private string summonMode;
        private int owningCircleId;
        private int startedTick;
        private int activatedTick;
        private int lastMeaningfulProgressTick;
        private int lastStateChangeTick;
        private int noConcreteSignalSinceTick = -1;
        private string lastReason;
        private int nextWatchdogTick;

        public MapComponent_ABY_SummonEncounterRuntime(Map map) : base(map)
        {
        }

        public EncounterStage Stage => stage;
        public bool HasBlockingLifecycle => stage == EncounterStage.Preparing || stage == EncounterStage.Active;
        public string EncounterId => encounterId;
        public string RitualId => ritualId;
        public string SummonMode => summonMode;
        public int StartedTick => startedTick;
        public int LastMeaningfulProgressTick => lastMeaningfulProgressTick;
        public string LastReason => lastReason;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stage, "abySummonEncounter_stage", EncounterStage.None);
            Scribe_Values.Look(ref sequence, "abySummonEncounter_sequence", 0);
            Scribe_Values.Look(ref encounterId, "abySummonEncounter_id");
            Scribe_Values.Look(ref ritualId, "abySummonEncounter_ritualId");
            Scribe_Values.Look(ref summonMode, "abySummonEncounter_summonMode");
            Scribe_Values.Look(ref owningCircleId, "abySummonEncounter_circleId", 0);
            Scribe_Values.Look(ref startedTick, "abySummonEncounter_startedTick", 0);
            Scribe_Values.Look(ref activatedTick, "abySummonEncounter_activatedTick", 0);
            Scribe_Values.Look(ref lastMeaningfulProgressTick, "abySummonEncounter_lastProgressTick", 0);
            Scribe_Values.Look(ref lastStateChangeTick, "abySummonEncounter_lastStateTick", 0);
            Scribe_Values.Look(ref noConcreteSignalSinceTick, "abySummonEncounter_noSignalSinceTick", -1);
            Scribe_Values.Look(ref lastReason, "abySummonEncounter_lastReason");
            Scribe_Values.Look(ref nextWatchdogTick, "abySummonEncounter_nextWatchdogTick", 0);
        }

        public void BeginPreparation(Building_AbyssalSummoningCircle circle, CompProperties_UseEffectSummonBoss props)
        {
            if (circle == null || props == null || map == null)
            {
                return;
            }

            int now = Now;
            sequence++;
            stage = EncounterStage.Preparing;
            encounterId = "aby-" + map.uniqueID + "-" + circle.thingIDNumber + "-" + sequence + "-" + now;
            ritualId = props.ritualId ?? string.Empty;
            summonMode = props.summonMode ?? "Boss";
            owningCircleId = circle.thingIDNumber;
            startedTick = now;
            activatedTick = 0;
            lastMeaningfulProgressTick = now;
            lastStateChangeTick = now;
            noConcreteSignalSinceTick = -1;
            lastReason = "Invocation committed; ritual preparation is in progress.";
            AbyssalBossSummonUtility.NotifyActiveEncounterStateMaybeChanged(map);
        }

        public void NotifyRitualPhase(Building_AbyssalSummoningCircle circle, string phase)
        {
            if (!Owns(circle) || stage != EncounterStage.Preparing)
            {
                return;
            }

            lastMeaningfulProgressTick = Now;
            lastReason = "Ritual phase: " + (phase ?? "unknown") + ".";
            AbyssalBossSummonUtility.NotifyActiveEncounterStateMaybeChanged(map);
        }

        public void Activate(Building_AbyssalSummoningCircle circle, string activatedRitualId, string activatedSummonMode)
        {
            if (circle == null || map == null)
            {
                return;
            }

            if (!Owns(circle))
            {
                // Dev/direct paths can begin an external crisis without a regular sigil transaction.
                BeginExternal(circle, activatedRitualId, activatedSummonMode);
            }

            stage = EncounterStage.Active;
            ritualId = activatedRitualId ?? ritualId ?? string.Empty;
            summonMode = activatedSummonMode ?? summonMode ?? "Boss";
            activatedTick = Now;
            lastMeaningfulProgressTick = activatedTick;
            lastStateChangeTick = activatedTick;
            noConcreteSignalSinceTick = -1;
            lastReason = "Encounter manifestation accepted by the map.";
            AbyssalBossSummonUtility.NotifyActiveEncounterStateMaybeChanged(map);
        }

        public void BeginExternal(Building_AbyssalSummoningCircle circle, string externalRitualId, string externalSummonMode)
        {
            if (circle == null || map == null)
            {
                return;
            }

            int now = Now;
            sequence++;
            encounterId = "aby-" + map.uniqueID + "-" + circle.thingIDNumber + "-external-" + sequence + "-" + now;
            ritualId = externalRitualId ?? string.Empty;
            summonMode = externalSummonMode ?? "Boss";
            owningCircleId = circle.thingIDNumber;
            startedTick = now;
            activatedTick = now;
            lastMeaningfulProgressTick = now;
            lastStateChangeTick = now;
            noConcreteSignalSinceTick = -1;
            lastReason = "External Abyssal encounter started.";
        }

        public void NotifyCircleRitualReset(Building_AbyssalSummoningCircle circle)
        {
            if (!Owns(circle) || stage != EncounterStage.Preparing)
            {
                return;
            }

            SetTerminal(EncounterStage.Aborted, "Ritual ended before an encounter was activated.");
        }

        public void AbortPreparation(Building_AbyssalSummoningCircle circle, string reason)
        {
            if (!Owns(circle) || stage != EncounterStage.Preparing)
            {
                return;
            }

            SetTerminal(EncounterStage.Aborted, reason.NullOrEmpty() ? "Invocation aborted before activation." : reason);
        }

        public void NotifyWorldStateChanged()
        {
            if (HasBlockingLifecycle)
            {
                lastMeaningfulProgressTick = Now;
                noConcreteSignalSinceTick = -1;
            }
        }

        public bool TryGetBlocker(out string blocker)
        {
            blocker = null;
            if (!HasBlockingLifecycle)
            {
                return false;
            }

            if (stage == EncounterStage.Preparing)
            {
                blocker = AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_SummonBlocker_RuntimePreparing",
                    "A previous invocation is still preparing at the summoning circle.");
                return true;
            }

            blocker = AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_SummonBlocker_RuntimeActive",
                "A previous Abyssal encounter is still active on this map.");
            return true;
        }

        public void AppendDiagnosticReport(StringBuilder sb)
        {
            if (sb == null)
            {
                return;
            }

            sb.AppendLine("Runtime encounter lifecycle:");
            sb.AppendLine(" - Stage: " + stage);
            sb.AppendLine(" - Id: " + (encounterId ?? "none"));
            sb.AppendLine(" - Ritual: " + (ritualId ?? "none") + " | mode: " + (summonMode ?? "none"));
            sb.AppendLine(" - Circle id: " + owningCircleId + " | started tick: " + startedTick + " | activated tick: " + activatedTick);
            sb.AppendLine(" - Last progress tick: " + lastMeaningfulProgressTick + " | reason: " + (lastReason ?? "none"));
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (!HasBlockingLifecycle || map == null || Find.TickManager == null)
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
        }

        private void RunWatchdog(int now)
        {
            if (stage == EncounterStage.Preparing)
            {
                Building_AbyssalSummoningCircle owner = FindOwningCircle();
                if (owner != null && owner.RitualActive)
                {
                    return;
                }

                SetTerminal(EncounterStage.Aborted, "Preparation watchdog cleared a ritual record after the owning circle stopped.");
                return;
            }

            if (AbyssalBossSummonUtility.TryGetConcreteActiveAbyssalEncounterBlocker(map, out _))
            {
                noConcreteSignalSinceTick = -1;
                lastMeaningfulProgressTick = now;
                return;
            }

            if (noConcreteSignalSinceTick < 0)
            {
                noConcreteSignalSinceTick = now;
                return;
            }

            if (now - noConcreteSignalSinceTick >= ActiveSignalGraceTicks)
            {
                SetTerminal(EncounterStage.ClearedAsStale, "State watchdog verified no active Abyssal encounter objects, portals, waves, Dominion crisis or combat-capable Abyssal pawns.");
            }
        }

        private Building_AbyssalSummoningCircle FindOwningCircle()
        {
            if (owningCircleId <= 0 || map?.listerThings?.AllThings == null)
            {
                return null;
            }

            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Building_AbyssalSummoningCircle circle = things[i] as Building_AbyssalSummoningCircle;
                if (circle != null && !circle.Destroyed && circle.thingIDNumber == owningCircleId)
                {
                    return circle;
                }
            }

            return null;
        }

        private bool Owns(Building_AbyssalSummoningCircle circle)
        {
            return circle != null && owningCircleId > 0 && circle.thingIDNumber == owningCircleId;
        }

        private void SetTerminal(EncounterStage terminalStage, string reason)
        {
            stage = terminalStage;
            lastReason = reason;
            lastStateChangeTick = Now;
            noConcreteSignalSinceTick = -1;
            AbyssalBossSummonUtility.NotifyActiveEncounterStateMaybeChanged(map);
        }

        private int Now => Find.TickManager != null ? Find.TickManager.TicksGame : 0;
    }
}
