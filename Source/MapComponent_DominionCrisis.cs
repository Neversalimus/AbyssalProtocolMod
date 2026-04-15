using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class MapComponent_DominionCrisis : MapComponent
    {
        public enum DominionCrisisPhase
        {
            Dormant,
            Synchronizing,
            Standby,
            Cancelled,
            Failed,
            Completed
        }

        private const int SynchronizationTicks = 4500;
        private const int StandbyTicks = 18000;
        private const int FailurePowerGraceTicks = 3000;
        private const int AmbientPulseIntervalTicks = 180;
        private const int AmbientSoundIntervalTicks = 900;
        private const int StandbyReminderIntervalTicks = 4500;
        private const string DominionResearchDefName = "ABY_DominionGateBootstrapping";

        private DominionCrisisPhase phase = DominionCrisisPhase.Dormant;
        private Building_AbyssalSummoningCircle sourceCircle;
        private IntVec3 sourceCell = IntVec3.Invalid;
        private int startedTick;
        private int phaseStartedTick;
        private int phaseEndsTick;
        private int stalledPowerTicks;
        private int nextAmbientPulseTick;
        private int nextAmbientSoundTick;
        private int nextReminderTick;
        private string lastOutcomeReason;
        private int lastOutcomeTick;

        public MapComponent_DominionCrisis(Map map) : base(map)
        {
        }

        public DominionCrisisPhase Phase => phase;
        public bool IsActive => phase == DominionCrisisPhase.Synchronizing || phase == DominionCrisisPhase.Standby;
        public bool IsTerminal => phase == DominionCrisisPhase.Cancelled || phase == DominionCrisisPhase.Failed || phase == DominionCrisisPhase.Completed;
        public Building_AbyssalSummoningCircle SourceCircle => sourceCircle;
        public IntVec3 SourceCell => sourceCell;
        public int StartedTick => startedTick;
        public int LastOutcomeTick => lastOutcomeTick;
        public string LastOutcomeReason => lastOutcomeReason;
        public int TicksRemaining
        {
            get
            {
                int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                return Mathf.Max(0, phaseEndsTick - now);
            }
        }

        public float PhaseProgress
        {
            get
            {
                int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                int duration = Mathf.Max(1, phaseEndsTick - phaseStartedTick);
                if (!IsActive)
                {
                    return IsTerminal ? 1f : 0f;
                }

                return Mathf.Clamp01((float)(now - phaseStartedTick) / duration);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref phase, "phase", DominionCrisisPhase.Dormant);
            Scribe_References.Look(ref sourceCircle, "sourceCircle");
            Scribe_Values.Look(ref sourceCell, "sourceCell", IntVec3.Invalid);
            Scribe_Values.Look(ref startedTick, "startedTick", 0);
            Scribe_Values.Look(ref phaseStartedTick, "phaseStartedTick", 0);
            Scribe_Values.Look(ref phaseEndsTick, "phaseEndsTick", 0);
            Scribe_Values.Look(ref stalledPowerTicks, "stalledPowerTicks", 0);
            Scribe_Values.Look(ref nextAmbientPulseTick, "nextAmbientPulseTick", 0);
            Scribe_Values.Look(ref nextAmbientSoundTick, "nextAmbientSoundTick", 0);
            Scribe_Values.Look(ref nextReminderTick, "nextReminderTick", 0);
            Scribe_Values.Look(ref lastOutcomeReason, "lastOutcomeReason");
            Scribe_Values.Look(ref lastOutcomeTick, "lastOutcomeTick", 0);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (!IsActive || Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (sourceCircle == null || sourceCircle.Destroyed || sourceCircle.Map != map)
            {
                ForceFail("ABY_DominionCrisisFail_CircleLost".Translate(), true);
                return;
            }

            bool powered = sourceCircle.IsPoweredForRitual;
            if (!powered)
            {
                stalledPowerTicks += 1;
                if (stalledPowerTicks >= FailurePowerGraceTicks)
                {
                    ForceFail("ABY_DominionCrisisFail_Power".Translate(), true);
                    return;
                }
            }
            else
            {
                stalledPowerTicks = Mathf.Max(0, stalledPowerTicks - 3);
            }

            if (now >= nextAmbientPulseTick)
            {
                nextAmbientPulseTick = now + AmbientPulseIntervalTicks;
                TickAmbientEffects(powered);
            }

            if (now >= nextAmbientSoundTick)
            {
                nextAmbientSoundTick = now + AmbientSoundIntervalTicks;
                if (sourceCell.IsValid)
                {
                    ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", sourceCell, map);
                }
            }

            if (phase == DominionCrisisPhase.Synchronizing && now >= phaseEndsTick)
            {
                BeginStandby();
                return;
            }

            if (phase == DominionCrisisPhase.Standby)
            {
                if (now >= phaseEndsTick)
                {
                    ForceFail("ABY_DominionCrisisFail_WindowExpired".Translate(), true);
                    return;
                }

                if (now >= nextReminderTick)
                {
                    nextReminderTick = now + StandbyReminderIntervalTicks;
                    SendStandbyReminder();
                }
            }
        }

        public bool CanBegin(Building_AbyssalSummoningCircle circle, out string failReason)
        {
            failReason = null;

            if (circle == null || circle.Destroyed || circle.Map != map)
            {
                failReason = "ABY_DominionCrisisFail_NoCircle".Translate();
                return false;
            }

            if (IsActive)
            {
                failReason = "ABY_DominionCrisisFail_AlreadyActive".Translate();
                return false;
            }

            if (AbyssalBossSummonUtility.HasActiveAbyssalEncounter(map))
            {
                failReason = "ABY_BossSummonFail_EncounterActive".Translate();
                return false;
            }

            if (!IsResearchComplete(DominionResearchDefName))
            {
                failReason = "ABY_DominionCrisisFail_NoResearch".Translate();
                return false;
            }

            if (circle.InstalledStabilizerCount < 4)
            {
                failReason = "ABY_DominionCrisisFail_Stabilizers".Translate(circle.InstalledStabilizerCount, 4);
                return false;
            }

            if (circle.GetInstalledCapacitorCount() < 2)
            {
                failReason = "ABY_DominionCrisisFail_Capacitors".Translate(circle.GetInstalledCapacitorCount(), 2);
                return false;
            }

            return true;
        }

        public bool TryBegin(Building_AbyssalSummoningCircle circle, out string failReason)
        {
            if (!CanBegin(circle, out failReason))
            {
                return false;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            sourceCircle = circle;
            sourceCell = circle.RitualFocusCell;
            startedTick = now;
            phaseStartedTick = now;
            phaseEndsTick = now + SynchronizationTicks;
            stalledPowerTicks = 0;
            nextAmbientPulseTick = now + 30;
            nextAmbientSoundTick = now + 90;
            nextReminderTick = 0;
            lastOutcomeReason = null;
            phase = DominionCrisisPhase.Synchronizing;

            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.18f);
            if (sourceCell.IsValid)
            {
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", sourceCell, map);
            }

            Find.LetterStack.ReceiveLetter(
                "ABY_DominionCrisisStartLabel".Translate(),
                "ABY_DominionCrisisStartDesc".Translate(circle.LabelCap),
                LetterDefOf.ThreatSmall,
                new TargetInfo(sourceCell.IsValid ? sourceCell : circle.PositionHeld, map));

            failReason = null;
            return true;
        }

        public bool TryAbort(Building_AbyssalSummoningCircle circle, out string failReason)
        {
            failReason = null;

            if (!IsActive)
            {
                failReason = "ABY_DominionCrisisAbortFail_NoActive".Translate();
                return false;
            }

            if (circle == null || circle.Map != map)
            {
                failReason = "ABY_DominionCrisisFail_NoCircle".Translate();
                return false;
            }

            SetTerminalState(DominionCrisisPhase.Cancelled, "ABY_DominionCrisisCancelledReason".Translate(), true);
            return true;
        }

        public void DebugAdvancePhase()
        {
            if (phase == DominionCrisisPhase.Synchronizing)
            {
                BeginStandby();
                return;
            }

            if (phase == DominionCrisisPhase.Standby)
            {
                SetTerminalState(DominionCrisisPhase.Completed, "ABY_DominionCrisisCompletedReason".Translate(), true);
            }
        }

        public void DebugComplete()
        {
            SetTerminalState(DominionCrisisPhase.Completed, "ABY_DominionCrisisCompletedReason".Translate(), true);
        }

        public void ForceFail(string reason, bool sendLetter)
        {
            SetTerminalState(DominionCrisisPhase.Failed, reason, sendLetter);
        }

        public void DebugReset()
        {
            phase = DominionCrisisPhase.Dormant;
            sourceCircle = null;
            sourceCell = IntVec3.Invalid;
            startedTick = 0;
            phaseStartedTick = 0;
            phaseEndsTick = 0;
            stalledPowerTicks = 0;
            nextAmbientPulseTick = 0;
            nextAmbientSoundTick = 0;
            nextReminderTick = 0;
            lastOutcomeReason = null;
            lastOutcomeTick = 0;
        }

        public string GetPhaseLabel()
        {
            switch (phase)
            {
                case DominionCrisisPhase.Synchronizing:
                    return "ABY_DominionCrisisPhase_Synchronizing".Translate();
                case DominionCrisisPhase.Standby:
                    return "ABY_DominionCrisisPhase_Standby".Translate();
                case DominionCrisisPhase.Cancelled:
                    return "ABY_DominionCrisisPhase_Cancelled".Translate();
                case DominionCrisisPhase.Failed:
                    return "ABY_DominionCrisisPhase_Failed".Translate();
                case DominionCrisisPhase.Completed:
                    return "ABY_DominionCrisisPhase_Completed".Translate();
                default:
                    return "ABY_DominionCrisisPhase_Dormant".Translate();
            }
        }

        public string GetStatusLine()
        {
            if (IsActive)
            {
                return "ABY_DominionCrisisStatusLine_Active".Translate(GetPhaseLabel(), TicksRemaining.ToStringTicksToPeriod());
            }

            if (phase == DominionCrisisPhase.Dormant)
            {
                return "ABY_DominionCrisisStatusLine_Dormant".Translate();
            }

            if (!lastOutcomeReason.NullOrEmpty())
            {
                return "ABY_DominionCrisisStatusLine_Terminal".Translate(GetPhaseLabel(), lastOutcomeReason);
            }

            return "ABY_DominionCrisisStatusLine_Terminal".Translate(GetPhaseLabel(), "ABY_CircleStatus_Clear".Translate());
        }

        private void BeginStandby()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            phase = DominionCrisisPhase.Standby;
            phaseStartedTick = now;
            phaseEndsTick = now + StandbyTicks;
            nextReminderTick = now + StandbyReminderIntervalTicks;
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.14f);

            Find.LetterStack.ReceiveLetter(
                "ABY_DominionCrisisStandbyLabel".Translate(),
                "ABY_DominionCrisisStandbyDesc".Translate(sourceCircle?.LabelCap ?? "summoning circle"),
                LetterDefOf.NegativeEvent,
                new TargetInfo(sourceCell.IsValid ? sourceCell : sourceCircle?.PositionHeld ?? IntVec3.Invalid, map));
        }

        private void TickAmbientEffects(bool powered)
        {
            if (map == null)
            {
                return;
            }

            float contamination = phase == DominionCrisisPhase.Synchronizing ? 0.0045f : 0.0065f;
            if (!powered)
            {
                contamination *= 1.35f;
            }

            map.GetComponent<MapComponent_AbyssalCircleInstability>()?.AddContamination(contamination);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, phase == DominionCrisisPhase.Synchronizing ? 0.05f : 0.08f);
        }

        private void SendStandbyReminder()
        {
            if (map == null)
            {
                return;
            }

            Messages.Message(
                "ABY_DominionCrisisReminder".Translate(TicksRemaining.ToStringTicksToPeriod()),
                new TargetInfo(sourceCell.IsValid ? sourceCell : sourceCircle?.PositionHeld ?? IntVec3.Invalid, map),
                MessageTypeDefOf.CautionInput,
                false);
        }

        private void SetTerminalState(DominionCrisisPhase terminalPhase, string reason, bool sendLetter)
        {
            phase = terminalPhase;
            lastOutcomeReason = reason;
            lastOutcomeTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            phaseStartedTick = lastOutcomeTick;
            phaseEndsTick = lastOutcomeTick;
            stalledPowerTicks = 0;
            nextAmbientPulseTick = 0;
            nextAmbientSoundTick = 0;
            nextReminderTick = 0;

            if (sourceCircle != null && !sourceCircle.Destroyed && sourceCircle.Map == map)
            {
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, terminalPhase == DominionCrisisPhase.Completed ? 0.10f : 0.16f);

                if (terminalPhase == DominionCrisisPhase.Failed)
                {
                    sourceCircle.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 12f, 0f, -1f, null, null, null));
                }
            }

            if (sendLetter)
            {
                Find.LetterStack.ReceiveLetter(
                    GetLetterLabel(terminalPhase),
                    GetLetterDescription(terminalPhase, reason),
                    terminalPhase == DominionCrisisPhase.Completed ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent,
                    new TargetInfo(sourceCell.IsValid ? sourceCell : sourceCircle?.PositionHeld ?? IntVec3.Invalid, map));
            }
        }

        private string GetLetterLabel(DominionCrisisPhase terminalPhase)
        {
            switch (terminalPhase)
            {
                case DominionCrisisPhase.Cancelled:
                    return "ABY_DominionCrisisCancelledLabel".Translate();
                case DominionCrisisPhase.Completed:
                    return "ABY_DominionCrisisCompletedLabel".Translate();
                default:
                    return "ABY_DominionCrisisFailedLabel".Translate();
            }
        }

        private string GetLetterDescription(DominionCrisisPhase terminalPhase, string reason)
        {
            switch (terminalPhase)
            {
                case DominionCrisisPhase.Cancelled:
                    return "ABY_DominionCrisisCancelledDesc".Translate(reason);
                case DominionCrisisPhase.Completed:
                    return "ABY_DominionCrisisCompletedDesc".Translate(reason);
                default:
                    return "ABY_DominionCrisisFailedDesc".Translate(reason);
            }
        }

        private static bool IsResearchComplete(string defName)
        {
            ResearchProjectDef def = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return false;
            }

            return def.IsFinished;
        }
    }
}
