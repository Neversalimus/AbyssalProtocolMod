using System.Collections.Generic;
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
            Anchorfall,
            Gatecore,
            Standby,
            Cancelled,
            Failed,
            Completed
        }

        private const int SynchronizationTicks = 4500;
        private const int AnchorfallTicks = 30000;
        private const int GatecoreTicks = 24000;
        private const int FailurePowerGraceTicks = 3000;
        private const int AmbientPulseIntervalTicks = 180;
        private const int AmbientSoundIntervalTicks = 900;
        private const int AnchorReminderIntervalTicks = 3000;
        private const int GateReminderIntervalTicks = 2400;
        private const int WaveRetryTicks = 360;
        private const int RuntimeMaintenanceIntervalTicks = 300;
        private const string DominionResearchDefName = "ABY_DominionGateBootstrapping";
        private const string DominionGateThingDefName = "ABY_DominionGateCore";

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
        private int nextWaveTick;
        private int lastWaveTick;
        private int wavesTriggered;
        private string lastWaveSummary;
        private string lastOutcomeReason;
        private int lastOutcomeTick;
        private List<Building_AbyssalDominionAnchor> activeAnchors = new List<Building_AbyssalDominionAnchor>();
        private int initialAnchorCount;
        private Building_AbyssalDominionGate gateCore;
        private int completionCount;
        private int failureCount;
        private int cancelledCount;
        private int cooldownUntilTick;
        private string lastRewardSummary;
        private int nextMaintenanceTick;
        private string lastMaintenanceSummary;

        public MapComponent_DominionCrisis(Map map) : base(map)
        {
        }

        public DominionCrisisPhase Phase => phase;
        public bool IsActive => phase == DominionCrisisPhase.Synchronizing || phase == DominionCrisisPhase.Anchorfall || phase == DominionCrisisPhase.Gatecore;
        public bool IsAnchorPhaseActive => phase == DominionCrisisPhase.Anchorfall;
        public bool IsGatePhaseActive => phase == DominionCrisisPhase.Gatecore;
        public bool IsTerminal => phase == DominionCrisisPhase.Cancelled || phase == DominionCrisisPhase.Failed || phase == DominionCrisisPhase.Completed;
        public Building_AbyssalSummoningCircle SourceCircle => sourceCircle;
        public IntVec3 SourceCell => sourceCell;
        public int StartedTick => startedTick;
        public int LastOutcomeTick => lastOutcomeTick;
        public string LastOutcomeReason => lastOutcomeReason;
        public int InitialAnchorCount => initialAnchorCount;
        public int ActiveAnchorCount => CountLiveAnchors();
        public int WavesTriggered => wavesTriggered;
        public string LastWaveSummary => lastWaveSummary;
        public Building_AbyssalDominionGate GateCore => gateCore;
        public Map CrisisMap => map;
        public int CompletionCount => completionCount;
        public int FailureCount => failureCount;
        public int CancelledCount => cancelledCount;
        public int CooldownUntilTick => cooldownUntilTick;
        public string LastRewardSummary => lastRewardSummary;
        public string LastMaintenanceSummary => lastMaintenanceSummary;
        public bool HasCooldown => !IsActive && Find.TickManager != null && cooldownUntilTick > Find.TickManager.TicksGame;

        public int CooldownTicksRemaining
        {
            get
            {
                int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                return Mathf.Max(0, cooldownUntilTick - now);
            }
        }

        public int TicksRemaining
        {
            get
            {
                int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                return Mathf.Max(0, phaseEndsTick - now);
            }
        }

        public int TicksUntilNextWave
        {
            get
            {
                if (Find.TickManager == null)
                {
                    return 0;
                }

                if (phase == DominionCrisisPhase.Anchorfall && nextWaveTick > 0)
                {
                    return Mathf.Max(0, nextWaveTick - Find.TickManager.TicksGame);
                }

                if (phase == DominionCrisisPhase.Gatecore && gateCore != null && !gateCore.Destroyed)
                {
                    return gateCore.TicksUntilNextPulse;
                }

                return 0;
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
            Scribe_Values.Look(ref nextWaveTick, "nextWaveTick", 0);
            Scribe_Values.Look(ref lastWaveTick, "lastWaveTick", 0);
            Scribe_Values.Look(ref wavesTriggered, "wavesTriggered", 0);
            Scribe_Values.Look(ref lastWaveSummary, "lastWaveSummary");
            Scribe_Values.Look(ref lastOutcomeReason, "lastOutcomeReason");
            Scribe_Values.Look(ref lastOutcomeTick, "lastOutcomeTick", 0);
            Scribe_Collections.Look(ref activeAnchors, "activeAnchors", LookMode.Reference);
            Scribe_Values.Look(ref initialAnchorCount, "initialAnchorCount", 0);
            Scribe_References.Look(ref gateCore, "gateCore");
            Scribe_Values.Look(ref completionCount, "completionCount", 0);
            Scribe_Values.Look(ref failureCount, "failureCount", 0);
            Scribe_Values.Look(ref cancelledCount, "cancelledCount", 0);
            Scribe_Values.Look(ref cooldownUntilTick, "cooldownUntilTick", 0);
            Scribe_Values.Look(ref lastRewardSummary, "lastRewardSummary");
            Scribe_Values.Look(ref nextMaintenanceTick, "nextMaintenanceTick", 0);
            Scribe_Values.Look(ref lastMaintenanceSummary, "lastMaintenanceSummary");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                activeAnchors ??= new List<Building_AbyssalDominionAnchor>();
                CleanupAnchorReferences();
                CleanupGateReference();
            }
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

            CleanupAnchorReferences();
            CleanupGateReference();

            if (now >= nextMaintenanceTick)
            {
                nextMaintenanceTick = now + Mathf.Max(RuntimeMaintenanceIntervalTicks, AbyssalDominionBalanceUtility.GetMaintenanceIntervalTicks(map, this));
                lastMaintenanceSummary = RunRuntimeMaintenance(now);
            }

            if (now >= nextAmbientPulseTick)
            {
                nextAmbientPulseTick = now + AbyssalDominionBalanceUtility.GetAmbientPulseIntervalTicks(map, this);
                TickAmbientEffects(powered);
            }

            if (now >= nextAmbientSoundTick)
            {
                nextAmbientSoundTick = now + AbyssalDominionBalanceUtility.GetAmbientSoundIntervalTicks(map, this);
                if (sourceCell.IsValid)
                {
                    ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", sourceCell, map);
                }
            }

            if (phase == DominionCrisisPhase.Synchronizing && now >= phaseEndsTick)
            {
                BeginAnchorfall();
                return;
            }

            if (phase == DominionCrisisPhase.Anchorfall)
            {
                if (ActiveAnchorCount <= 0)
                {
                    BeginGatecore();
                    return;
                }

                if (now >= phaseEndsTick)
                {
                    ForceFail("ABY_DominionCrisisFail_AnchorsPersisted".Translate(), true);
                    return;
                }

                if (now >= nextReminderTick)
                {
                    nextReminderTick = now + AnchorReminderIntervalTicks;
                    SendAnchorReminder();
                }

                if (nextWaveTick > 0 && now >= nextWaveTick)
                {
                    TryFireWavePulse();
                }

                return;
            }

            if (phase == DominionCrisisPhase.Gatecore)
            {
                if (gateCore == null || gateCore.Destroyed || gateCore.Map != map)
                {
                    SetTerminalState(DominionCrisisPhase.Completed, "ABY_DominionCrisisCompletedReason".Translate(), true);
                    return;
                }

                if (now >= phaseEndsTick)
                {
                    ForceFail("ABY_DominionCrisisFail_GatePersisted".Translate(), true);
                    return;
                }

                if (now >= nextReminderTick)
                {
                    nextReminderTick = now + GateReminderIntervalTicks;
                    SendGateReminder();
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

            CleanupAnchorReferences();

            if (IsActive)
            {
                failReason = "ABY_DominionCrisisFail_AlreadyActive".Translate();
                return false;
            }

            if (CooldownTicksRemaining > 0)
            {
                failReason = "ABY_DominionCrisisFail_Cooldown".Translate(GetCooldownValue());
                return false;
            }

            if (ActiveAnchorCount > 0)
            {
                failReason = "ABY_DominionCrisisFail_AnchorRemnants".Translate(ActiveAnchorCount);
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
            nextWaveTick = 0;
            lastWaveTick = 0;
            nextMaintenanceTick = 0;
            wavesTriggered = 0;
            lastWaveSummary = null;
            lastOutcomeReason = null;
            lastRewardSummary = null;
            nextMaintenanceTick = now + 60;
            lastMaintenanceSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionMaintenance_Stable", "stable");
            activeAnchors.Clear();
            initialAnchorCount = 0;
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

        public void RegisterAnchor(Building_AbyssalDominionAnchor anchor)
        {
            if (anchor == null)
            {
                return;
            }

            activeAnchors ??= new List<Building_AbyssalDominionAnchor>();
            if (!activeAnchors.Contains(anchor))
            {
                activeAnchors.Add(anchor);
            }
        }

        public bool IsRegisteredAnchor(Building_AbyssalDominionAnchor anchor)
        {
            if (anchor == null)
            {
                return false;
            }

            CleanupAnchorReferences();
            return activeAnchors.Contains(anchor);
        }

        public void NotifyAnchorDestroyed(Building_AbyssalDominionAnchor anchor)
        {
            if (anchor == null || activeAnchors == null)
            {
                return;
            }

            activeAnchors.Remove(anchor);

            if (phase == DominionCrisisPhase.Anchorfall && map != null)
            {
                Messages.Message(
                    "ABY_DominionCrisisAnchorDestroyed".Translate(anchor.LabelCap, ActiveAnchorCount, Mathf.Max(1, initialAnchorCount)),
                    new TargetInfo(anchor.PositionHeld, map),
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }
        }

        public List<Building_AbyssalDominionAnchor> GetLiveAnchors()
        {
            CleanupAnchorReferences();
            return activeAnchors == null ? new List<Building_AbyssalDominionAnchor>() : new List<Building_AbyssalDominionAnchor>(activeAnchors);
        }

        public Thing GetPrimaryObjectiveThing()
        {
            CleanupAnchorReferences();
            CleanupGateReference();

            if (phase == DominionCrisisPhase.Gatecore && gateCore != null && !gateCore.Destroyed)
            {
                return gateCore;
            }

            if (phase == DominionCrisisPhase.Anchorfall)
            {
                Building_AbyssalDominionAnchor bestAnchor = GetBestAnchorObjective();
                if (bestAnchor != null)
                {
                    return bestAnchor;
                }
            }

            if (sourceCircle != null && !sourceCircle.Destroyed)
            {
                return sourceCircle;
            }

            return null;
        }

        public string GetPrimaryObjectiveLabel()
        {
            switch (phase)
            {
                case DominionCrisisPhase.Synchronizing:
                    return "ABY_DominionOpsObjective_Sync".Translate();
                case DominionCrisisPhase.Anchorfall:
                    return "ABY_DominionOpsObjective_Anchors".Translate();
                case DominionCrisisPhase.Gatecore:
                    return "ABY_DominionOpsObjective_Gate".Translate();
                case DominionCrisisPhase.Cancelled:
                case DominionCrisisPhase.Failed:
                case DominionCrisisPhase.Completed:
                    return "ABY_DominionOpsObjective_Rearm".Translate();
                default:
                    return "ABY_DominionOpsObjective_Dormant".Translate();
            }
        }

        public string GetDirectiveSummary()
        {
            switch (phase)
            {
                case DominionCrisisPhase.Synchronizing:
                    return "ABY_DominionOpsDirective_Sync".Translate();
                case DominionCrisisPhase.Anchorfall:
                    return "ABY_DominionOpsDirective_Anchors".Translate(ActiveAnchorCount, Mathf.Max(1, initialAnchorCount), GetWavePressureLabel());
                case DominionCrisisPhase.Gatecore:
                    return "ABY_DominionOpsDirective_Gate".Translate(GetGateIntegrityValue(), GetGatePulseEtaValue());
                case DominionCrisisPhase.Cancelled:
                case DominionCrisisPhase.Failed:
                case DominionCrisisPhase.Completed:
                    return "ABY_DominionOpsDirective_Rearm".Translate(GetCooldownValue(), GetReplayStatusValue());
                default:
                    return "ABY_DominionOpsObjective_Dormant".Translate();
            }
        }

        private Building_AbyssalDominionAnchor GetBestAnchorObjective()
        {
            List<Building_AbyssalDominionAnchor> liveAnchors = GetLiveAnchors();
            if (liveAnchors.Count == 0)
            {
                return null;
            }

            Building_AbyssalDominionAnchor best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < liveAnchors.Count; i++)
            {
                Building_AbyssalDominionAnchor anchor = liveAnchors[i];
                if (anchor == null || anchor.Destroyed)
                {
                    continue;
                }

                float score = GetAnchorObjectivePriority(anchor.AnchorRole) * 100000f;
                if (sourceCell.IsValid)
                {
                    score += anchor.PositionHeld.DistanceToSquared(sourceCell);
                }

                if (score < bestScore)
                {
                    best = anchor;
                    bestScore = score;
                }
            }

            return best;
        }

        private static int GetAnchorObjectivePriority(DominionAnchorRole role)
        {
            switch (role)
            {
                case DominionAnchorRole.Breach:
                    return 0;
                case DominionAnchorRole.Ward:
                    return 1;
                case DominionAnchorRole.Drain:
                    return 2;
                default:
                    return 3;
            }
        }

        public int GetActiveAnchorCount(DominionAnchorRole role)
        {
            CleanupAnchorReferences();

            if (activeAnchors == null || activeAnchors.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < activeAnchors.Count; i++)
            {
                Building_AbyssalDominionAnchor anchor = activeAnchors[i];
                if (anchor != null && !anchor.Destroyed && anchor.AnchorRole == role)
                {
                    count++;
                }
            }

            return count;
        }

        public void AddExternalContamination(float amount)
        {
            if (amount <= 0f || map == null)
            {
                return;
            }

            map.GetComponent<MapComponent_AbyssalCircleInstability>()?.AddContamination(amount);
        }

        public void AccelerateAnchorDeadline(int ticks)
        {
            if (phase != DominionCrisisPhase.Anchorfall || ticks <= 0)
            {
                return;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            phaseEndsTick = Mathf.Max(now + 900, phaseEndsTick - ticks);
        }

        public string GetCooldownValue()
        {
            if (IsActive)
            {
                return "ABY_DominionCooldown_Active".Translate();
            }

            int ticks = CooldownTicksRemaining;
            if (ticks > 0)
            {
                return ticks.ToStringTicksToPeriod();
            }

            return "ABY_DominionCooldown_Ready".Translate();
        }

        public string GetReplayStatusValue()
        {
            return "ABY_DominionReplay_Value".Translate(completionCount, failureCount, cancelledCount);
        }

        public string GetRewardForecastValue()
        {
            return AbyssalDominionRewardUtility.GetRewardForecastText(this);
        }

        public List<string> GetRewardConsoleLines()
        {
            return AbyssalDominionRewardUtility.GetRewardConsoleLines(this);
        }

        public List<string> GetBalanceConsoleLines()
        {
            return AbyssalDominionBalanceUtility.GetConsoleLines(map, this);
        }

        public string GetCalibrationValue()
        {
            return AbyssalDominionBalanceUtility.GetCalibrationValue(map, this);
        }

        public string GetRuntimeBudgetValue()
        {
            return AbyssalDominionBalanceUtility.GetRuntimeBudgetValue(map, this);
        }

        public string GetFxModeValue()
        {
            return AbyssalDominionBalanceUtility.GetFxModeValue(map, this);
        }

        public void DebugAdvancePhase()
        {
            if (phase == DominionCrisisPhase.Synchronizing)
            {
                BeginAnchorfall();
                return;
            }

            if (phase == DominionCrisisPhase.Anchorfall)
            {
                DestroyTrackedAnchors(true);
                BeginGatecore();
                return;
            }

            if (phase == DominionCrisisPhase.Gatecore)
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
            DestroyTrackedAnchors(true);
            DestroyTrackedGate(true);
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
            nextWaveTick = 0;
            lastWaveTick = 0;
            nextMaintenanceTick = 0;
            wavesTriggered = 0;
            lastWaveSummary = null;
            lastOutcomeReason = null;
            lastOutcomeTick = 0;
            activeAnchors.Clear();
            initialAnchorCount = 0;
            gateCore = null;
            completionCount = 0;
            failureCount = 0;
            cancelledCount = 0;
            cooldownUntilTick = 0;
            lastRewardSummary = null;
            nextMaintenanceTick = 0;
            lastMaintenanceSummary = null;
        }

        public string GetPhaseLabel()
        {
            switch (phase)
            {
                case DominionCrisisPhase.Synchronizing:
                    return "ABY_DominionCrisisPhase_Synchronizing".Translate();
                case DominionCrisisPhase.Anchorfall:
                    return "ABY_DominionCrisisPhase_Anchorfall".Translate();
                case DominionCrisisPhase.Gatecore:
                    return "ABY_DominionCrisisPhase_Gatecore".Translate();
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
            if (phase == DominionCrisisPhase.Anchorfall)
            {
                return "ABY_DominionCrisisStatusLine_Anchors".Translate(GetPhaseLabel(), ActiveAnchorCount, Mathf.Max(1, initialAnchorCount), GetNextWaveEtaValue(), TicksRemaining.ToStringTicksToPeriod());
            }

            if (phase == DominionCrisisPhase.Gatecore)
            {
                return "ABY_DominionCrisisStatusLine_Gate".Translate(GetPhaseLabel(), GetGateIntegrityValue(), GetGatePulseEtaValue(), TicksRemaining.ToStringTicksToPeriod());
            }

            if (IsActive)
            {
                return "ABY_DominionCrisisStatusLine_Active".Translate(GetPhaseLabel(), TicksRemaining.ToStringTicksToPeriod());
            }

            if (phase == DominionCrisisPhase.Dormant)
            {
                if (CooldownTicksRemaining > 0 || completionCount > 0 || failureCount > 0 || cancelledCount > 0)
                {
                    return "ABY_DominionCrisisStatusLine_Cooldown".Translate(GetCooldownValue(), GetReplayStatusValue());
                }

                return "ABY_DominionCrisisStatusLine_Dormant".Translate();
            }

            if (!lastOutcomeReason.NullOrEmpty())
            {
                return "ABY_DominionCrisisStatusLine_Terminal".Translate(GetPhaseLabel(), lastOutcomeReason);
            }

            return "ABY_DominionCrisisStatusLine_Terminal".Translate(GetPhaseLabel(), "ABY_CircleStatus_Clear".Translate());
        }

        public string GetAnchorStatusShort()
        {
            if (phase == DominionCrisisPhase.Gatecore)
            {
                return "ABY_DominionAnchor_StatusShort_Cleared".Translate();
            }

            if (phase != DominionCrisisPhase.Anchorfall)
            {
                return "ABY_DominionAnchor_StatusShort_None".Translate();
            }

            return "ABY_DominionAnchor_StatusShort_Active".Translate(ActiveAnchorCount, Mathf.Max(1, initialAnchorCount));
        }

        public string GetAnchorStatusValue()
        {
            if (phase == DominionCrisisPhase.Gatecore)
            {
                return "ABY_DominionAnchor_StatusValue_Cleared".Translate();
            }

            if (phase != DominionCrisisPhase.Anchorfall)
            {
                return "ABY_DominionAnchor_StatusValue_Pending".Translate();
            }

            return "ABY_DominionAnchor_StatusValue_Active".Translate(ActiveAnchorCount, Mathf.Max(1, initialAnchorCount));
        }

        public string GetAnchorPressureLabel()
        {
            int pressure = GetActiveAnchorCount(DominionAnchorRole.Suppression)
                + GetActiveAnchorCount(DominionAnchorRole.Drain)
                + GetActiveAnchorCount(DominionAnchorRole.Ward)
                + GetActiveAnchorCount(DominionAnchorRole.Breach);

            if (pressure <= 0)
            {
                return "ABY_DominionAnchor_Pressure_None".Translate();
            }

            if (pressure <= 1)
            {
                return "ABY_DominionAnchor_Pressure_Low".Translate();
            }

            if (pressure <= 3)
            {
                return "ABY_DominionAnchor_Pressure_High".Translate();
            }

            return "ABY_DominionAnchor_Pressure_Maximum".Translate();
        }

        public List<string> GetAnchorConsoleLines()
        {
            List<string> lines = new List<string>();

            if (phase == DominionCrisisPhase.Gatecore)
            {
                lines.Add("ABY_DominionAnchor_ConsoleGatecore".Translate());
                return lines;
            }

            if (phase != DominionCrisisPhase.Anchorfall)
            {
                lines.Add("ABY_DominionAnchor_ConsoleIdle".Translate());
                return lines;
            }

            AddAnchorRoleLine(lines, DominionAnchorRole.Suppression, "ABY_DominionAnchor_Role_Suppression".Translate(), "ABY_DominionAnchor_Effect_Suppression".Translate());
            AddAnchorRoleLine(lines, DominionAnchorRole.Drain, "ABY_DominionAnchor_Role_Drain".Translate(), "ABY_DominionAnchor_Effect_Drain".Translate());
            AddAnchorRoleLine(lines, DominionAnchorRole.Ward, "ABY_DominionAnchor_Role_Ward".Translate(), "ABY_DominionAnchor_Effect_Ward".Translate());
            AddAnchorRoleLine(lines, DominionAnchorRole.Breach, "ABY_DominionAnchor_Role_Breach".Translate(), "ABY_DominionAnchor_Effect_Breach".Translate());

            if (lines.Count == 0)
            {
                lines.Add("ABY_DominionAnchor_ConsoleCleared".Translate());
            }

            return lines;
        }

        private void AddAnchorRoleLine(List<string> lines, DominionAnchorRole role, string label, string effect)
        {
            int count = GetActiveAnchorCount(role);
            if (count <= 0)
            {
                return;
            }

            lines.Add("ABY_DominionAnchor_ConsoleLine".Translate(label, count, effect));
        }

        public string GetWaveStatusValue()
        {
            if (phase == DominionCrisisPhase.Gatecore)
            {
                return "ABY_DominionWavePreviewStatus_Gatebound".Translate();
            }

            if (phase != DominionCrisisPhase.Anchorfall)
            {
                return "ABY_DominionWavePreviewStatus_Dormant".Translate();
            }

            return AbyssalDominionWaveUtility.GetWavePreviewText(map, this);
        }

        public string GetWavePressureLabel()
        {
            return AbyssalDominionWaveUtility.GetWavePressureLabel(map, this);
        }

        public string GetNextWaveEtaValue()
        {
            if (phase == DominionCrisisPhase.Gatecore)
            {
                return GetGatePulseEtaValue();
            }

            if (phase != DominionCrisisPhase.Anchorfall)
            {
                return "ABY_DominionWaveEta_Pending".Translate();
            }

            if (nextWaveTick <= 0)
            {
                return "ABY_DominionWaveEta_Queued".Translate();
            }

            int ticks = TicksUntilNextWave;
            if (ticks <= 90)
            {
                return "ABY_DominionWaveEta_Imminent".Translate();
            }

            return ticks.ToStringTicksToPeriod();
        }

        public List<string> GetWaveConsoleLines()
        {
            return AbyssalDominionWaveUtility.GetConsoleLines(map, this);
        }


        public void RegisterGate(Building_AbyssalDominionGate gate)
        {
            if (gate == null)
            {
                return;
            }

            gateCore = gate;
        }

        public bool IsRegisteredGate(Building_AbyssalDominionGate gate)
        {
            CleanupGateReference();
            return gate != null && gate == gateCore;
        }

        public void NotifyGateDestroyed(Building_AbyssalDominionGate gate)
        {
            if (gate == null)
            {
                return;
            }

            if (gateCore == gate)
            {
                gateCore = null;
            }

            if (phase == DominionCrisisPhase.Gatecore)
            {
                SetTerminalState(DominionCrisisPhase.Completed, "ABY_DominionCrisisCompletedReason".Translate(), true);
            }
        }

        public string GetGateStatusValue()
        {
            if (phase != DominionCrisisPhase.Gatecore || gateCore == null || gateCore.Destroyed)
            {
                return "ABY_DominionGate_Status_Dormant".Translate();
            }

            return gateCore.GetStatusValue();
        }

        public string GetGateIntegrityValue()
        {
            if (phase != DominionCrisisPhase.Gatecore || gateCore == null || gateCore.Destroyed)
            {
                return "ABY_DominionGate_Integrity_Dormant".Translate();
            }

            return gateCore.GetIntegrityValue();
        }

        public string GetGatePulseEtaValue()
        {
            if (phase != DominionCrisisPhase.Gatecore || gateCore == null || gateCore.Destroyed)
            {
                return "ABY_DominionGate_PulseEta_Pending".Translate();
            }

            return gateCore.GetNextPulseEtaValue();
        }

        public List<string> GetGateConsoleLines()
        {
            if (phase != DominionCrisisPhase.Gatecore || gateCore == null || gateCore.Destroyed)
            {
                return new List<string> { "ABY_DominionGate_ConsoleIdle".Translate() };
            }

            return gateCore.GetConsoleLines();
        }

        public void NotifyGatePulse(string summary, IntVec3 focusCell)
        {
            wavesTriggered += 1;
            lastWaveTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            lastWaveSummary = summary;

            if (focusCell.IsValid)
            {
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", focusCell, map);
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.16f);
            }

            Messages.Message(
                "ABY_DominionGateCall_Message".Translate(summary, GetGatePulseEtaValue()),
                new TargetInfo(focusCell.IsValid ? focusCell : sourceCell, map),
                MessageTypeDefOf.CautionInput,
                false);
        }

        private void TryFireWavePulse()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (!AbyssalDominionWaveUtility.TryExecuteWave(map, this, out string summary, out IntVec3 focusCell))
            {
                nextWaveTick = now + WaveRetryTicks;
                return;
            }

            wavesTriggered++;
            lastWaveTick = now;
            lastWaveSummary = summary;
            nextWaveTick = now + AbyssalDominionWaveUtility.GetNextWaveDelayTicks(this);

            if (wavesTriggered == 1)
            {
                Find.LetterStack.ReceiveLetter(
                    "ABY_DominionWavePulseLabel".Translate(),
                    "ABY_DominionWavePulseDesc".Translate(summary, GetNextWaveEtaValue()),
                    LetterDefOf.ThreatBig,
                    new TargetInfo(focusCell.IsValid ? focusCell : sourceCell, map));
            }
            else
            {
                Messages.Message(
                    "ABY_DominionWavePulseMessage".Translate(summary, GetNextWaveEtaValue()),
                    new TargetInfo(focusCell.IsValid ? focusCell : sourceCell, map),
                    MessageTypeDefOf.CautionInput,
                    false);
            }
        }

        private void BeginAnchorfall()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (!TrySpawnAnchors(out string failReason))
            {
                ForceFail(failReason, true);
                return;
            }

            phase = DominionCrisisPhase.Anchorfall;
            phaseStartedTick = now;
            phaseEndsTick = now + AnchorfallTicks;
            nextReminderTick = now + AnchorReminderIntervalTicks;
            nextWaveTick = now + AbyssalDominionWaveUtility.GetInitialWaveDelayTicks(this);
            lastWaveTick = 0;
            wavesTriggered = 0;
            lastWaveSummary = null;
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.20f);

            Find.LetterStack.ReceiveLetter(
                "ABY_DominionCrisisAnchorfallLabel".Translate(),
                "ABY_DominionCrisisAnchorfallDesc".Translate(sourceCircle?.LabelCap ?? "summoning circle", initialAnchorCount),
                LetterDefOf.ThreatBig,
                new TargetInfo(sourceCell.IsValid ? sourceCell : sourceCircle?.PositionHeld ?? IntVec3.Invalid, map));
        }

        private void BeginGatecore()
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            DestroyTrackedAnchors(true);

            if (!TrySpawnGateCore(out string failReason))
            {
                ForceFail(failReason, true);
                return;
            }

            phase = DominionCrisisPhase.Gatecore;
            phaseStartedTick = now;
            phaseEndsTick = now + GatecoreTicks;
            nextReminderTick = now + GateReminderIntervalTicks;
            nextWaveTick = 0;
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.26f);

            Find.LetterStack.ReceiveLetter(
                "ABY_DominionCrisisGateLabel".Translate(),
                "ABY_DominionCrisisGateDesc".Translate(sourceCircle?.LabelCap ?? "summoning circle"),
                LetterDefOf.ThreatBig,
                new TargetInfo(gateCore != null ? gateCore.PositionHeld : sourceCell, map));
        }

        private bool TrySpawnGateCore(out string failReason)
        {
            failReason = null;
            CleanupGateReference();

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(DominionGateThingDefName);
            if (def == null)
            {
                failReason = "Missing dominion gate def: " + DominionGateThingDefName;
                return false;
            }

            if (!TryFindGateSpawnCell(def.size, out IntVec3 cell))
            {
                failReason = "ABY_DominionCrisisFail_NoGateCell".Translate();
                return false;
            }

            if (!(ThingMaker.MakeThing(def) is Building_AbyssalDominionGate gate))
            {
                failReason = "Failed to create dominion gate core.";
                return false;
            }

            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            GenSpawn.Spawn(gate, cell, map, Rot4.North);
            if (hostileFaction != null)
            {
                gate.SetFaction(hostileFaction);
            }

            RegisterGate(gate);
            FleckMaker.ThrowLightningGlow(gate.DrawPos, map, 2.8f);
            FleckMaker.ThrowMicroSparks(gate.DrawPos, map);
            ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", cell, map);
            return true;
        }

        private bool TryFindGateSpawnCell(IntVec2 size, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || !sourceCell.IsValid)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;
            float minRadius = 5f;
            float maxRadius = 16f;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(sourceCell, maxRadius, true))
            {
                if (!candidate.InBounds(map))
                {
                    continue;
                }

                float distance = candidate.DistanceTo(sourceCell);
                if (distance < minRadius || distance > maxRadius)
                {
                    continue;
                }

                CellRect rect = GenAdj.OccupiedRect(candidate, Rot4.North, size);
                if (!CanUseGateRect(rect))
                {
                    continue;
                }

                float score = 0f;
                score -= Mathf.Abs(distance - 9f);
                score += CountAdjacentStandableCells(candidate) * 0.12f;
                score += !candidate.Roofed(map) ? 0.7f : -0.35f;
                score += CountNearbyPowerBuildings(candidate, 9f) * 0.22f;
                score += CountNearbyTurrets(candidate, 9f) * 0.18f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private bool CanUseGateRect(CellRect rect)
        {
            foreach (IntVec3 rectCell in rect.Cells)
            {
                if (!rectCell.InBounds(map) || !rectCell.Standable(map))
                {
                    return false;
                }

                if (rectCell.GetEdifice(map) != null || rectCell.GetFirstPawn(map) != null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TrySpawnAnchors(out string failReason)
        {
            failReason = null;
            CleanupAnchorReferences();

            List<DominionAnchorRole> roles = new List<DominionAnchorRole>
            {
                DominionAnchorRole.Suppression,
                DominionAnchorRole.Drain,
                DominionAnchorRole.Ward,
                DominionAnchorRole.Breach
            };

            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            List<IntVec3> usedCells = new List<IntVec3>();

            for (int i = 0; i < roles.Count; i++)
            {
                DominionAnchorRole role = roles[i];
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(GetAnchorDefName(role));
                if (def == null)
                {
                    failReason = "Missing dominion anchor def: " + GetAnchorDefName(role);
                    DestroyTrackedAnchors(false);
                    return false;
                }

                if (!TryFindAnchorSpawnCell(role, usedCells, out IntVec3 cell))
                {
                    failReason = "ABY_DominionCrisisFail_NoAnchorCell".Translate();
                    DestroyTrackedAnchors(false);
                    return false;
                }

                if (!(ThingMaker.MakeThing(def) is Building_AbyssalDominionAnchor anchor))
                {
                    failReason = "Failed to create dominion anchor: " + def.defName;
                    DestroyTrackedAnchors(false);
                    return false;
                }

                GenSpawn.Spawn(anchor, cell, map, Rot4.Random);
                if (hostileFaction != null)
                {
                    anchor.SetFaction(hostileFaction);
                }

                RegisterAnchor(anchor);
                usedCells.Add(cell);

                FleckMaker.ThrowLightningGlow(anchor.DrawPos, map, 1.8f);
                FleckMaker.ThrowMicroSparks(anchor.DrawPos, map);
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", cell, map);
            }

            initialAnchorCount = CountLiveAnchors();
            return initialAnchorCount > 0;
        }

        private bool TryFindAnchorSpawnCell(DominionAnchorRole role, List<IntVec3> usedCells, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || !sourceCell.IsValid)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;
            float minRadius = 14f;
            float maxRadius = Mathf.Min(38f, Mathf.Max(18f, Mathf.Min(map.Size.x, map.Size.z) * 0.24f));
            float preferredRadius = role == DominionAnchorRole.Ward ? 18f : 24f;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(sourceCell, maxRadius, true))
            {
                if (!candidate.InBounds(map) || candidate.Fogged(map) || !candidate.Standable(map))
                {
                    continue;
                }

                float distance = candidate.DistanceTo(sourceCell);
                if (distance < minRadius || distance > maxRadius)
                {
                    continue;
                }

                if (candidate.GetEdifice(map) != null || candidate.GetFirstPawn(map) != null)
                {
                    continue;
                }

                bool tooClose = false;
                for (int i = 0; i < usedCells.Count; i++)
                {
                    if (usedCells[i].DistanceTo(candidate) < 10.5f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }

                float score = 0f;
                score -= Mathf.Abs(distance - preferredRadius);
                score += CountAdjacentStandableCells(candidate) * 0.08f;
                score += !candidate.Roofed(map) ? 0.55f : -0.15f;
                score += DistanceToNearestEdge(candidate) * 0.018f;

                if (role == DominionAnchorRole.Suppression)
                {
                    score += CountNearbyTurrets(candidate, 12f) * 0.8f;
                }
                else if (role == DominionAnchorRole.Drain)
                {
                    score += CountNearbyPowerBuildings(candidate, 12f) * 0.7f;
                }
                else if (role == DominionAnchorRole.Ward)
                {
                    score -= candidate.DistanceTo(sourceCell) * 0.12f;
                }
                else if (role == DominionAnchorRole.Breach)
                {
                    score += !candidate.Roofed(map) ? 0.8f : 0f;
                    score += distance * 0.04f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private int CountAdjacentStandableCells(IntVec3 center)
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                IntVec3 cell = center + GenAdj.AdjacentCells[i];
                if (cell.InBounds(map) && cell.Standable(map))
                {
                    count++;
                }
            }

            return count;
        }

        private int DistanceToNearestEdge(IntVec3 cell)
        {
            int toWest = cell.x;
            int toEast = map.Size.x - 1 - cell.x;
            int toSouth = cell.z;
            int toNorth = map.Size.z - 1 - cell.z;
            return Mathf.Min(Mathf.Min(toWest, toEast), Mathf.Min(toSouth, toNorth));
        }

        private int CountNearbyTurrets(IntVec3 center, float radius)
        {
            int count = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Building_Turret turret && turret.Faction == Faction.OfPlayer)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private int CountNearbyPowerBuildings(IntVec3 center, float radius)
        {
            int count = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Building building
                        && building.Faction == Faction.OfPlayer
                        && (building.GetComp<CompPowerBattery>() != null || building.GetComp<CompPowerTrader>() != null))
                    {
                        count++;
                    }
                }
            }

            return count;
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

            if (phase == DominionCrisisPhase.Anchorfall)
            {
                contamination += Mathf.Max(0, ActiveAnchorCount) * 0.0015f;
            }
            else if (phase == DominionCrisisPhase.Gatecore)
            {
                contamination += 0.0115f;
            }

            contamination *= AbyssalDominionBalanceUtility.GetAmbientContaminationMultiplier(map, this);
            map.GetComponent<MapComponent_AbyssalCircleInstability>()?.AddContamination(contamination);

            float pulseStrength = phase == DominionCrisisPhase.Synchronizing ? 0.05f : (phase == DominionCrisisPhase.Gatecore ? 0.12f : 0.09f);
            pulseStrength *= AbyssalDominionBalanceUtility.GetScreenFxMultiplier(map, this);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, pulseStrength);
        }

        private string RunRuntimeMaintenance(int now)
        {
            if (map == null)
            {
                return null;
            }

            int removedPortals = AbyssalDominionBalanceUtility.CollapseExcessPortals(map, this, sourceCell);
            int waveDelay = 0;
            AbyssalDominionBalanceUtility.RuntimeProfile profile = AbyssalDominionBalanceUtility.BuildProfile(map, this);
            if (phase == DominionCrisisPhase.Anchorfall && nextWaveTick > 0 && profile.ActiveHostiles > profile.MaxActiveHostiles)
            {
                waveDelay = Mathf.Clamp((profile.ActiveHostiles - profile.MaxActiveHostiles) * 45, 240, 900);
                nextWaveTick = Mathf.Max(nextWaveTick, now + waveDelay);
            }

            if (removedPortals > 0 && waveDelay > 0)
            {
                return "ABY_DominionMaintenance_Combined".Translate(removedPortals, waveDelay.ToStringTicksToPeriod());
            }

            if (removedPortals > 0)
            {
                return "ABY_DominionMaintenance_Portals".Translate(removedPortals);
            }

            if (waveDelay > 0)
            {
                return "ABY_DominionMaintenance_Delay".Translate(waveDelay.ToStringTicksToPeriod());
            }

            return "ABY_DominionMaintenance_Stable".Translate();
        }

        private void SendAnchorReminder()
        {
            if (map == null)
            {
                return;
            }

            Messages.Message(
                "ABY_DominionCrisisReminderAnchors".Translate(ActiveAnchorCount, Mathf.Max(1, initialAnchorCount), TicksRemaining.ToStringTicksToPeriod()),
                new TargetInfo(sourceCell.IsValid ? sourceCell : sourceCircle?.PositionHeld ?? IntVec3.Invalid, map),
                MessageTypeDefOf.CautionInput,
                false);
        }

        private void SendGateReminder()
        {
            if (map == null)
            {
                return;
            }

            Messages.Message(
                "ABY_DominionCrisisReminderGate".Translate(GetGateIntegrityValue(), GetGatePulseEtaValue(), TicksRemaining.ToStringTicksToPeriod()),
                new TargetInfo(gateCore != null ? gateCore.PositionHeld : sourceCell, map),
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
            nextWaveTick = 0;
            lastWaveTick = 0;
            nextMaintenanceTick = 0;

            switch (terminalPhase)
            {
                case DominionCrisisPhase.Completed:
                    completionCount++;
                    break;
                case DominionCrisisPhase.Failed:
                    failureCount++;
                    break;
                case DominionCrisisPhase.Cancelled:
                    cancelledCount++;
                    break;
            }

            cooldownUntilTick = lastOutcomeTick + AbyssalDominionRewardUtility.GetCooldownTicks(this, terminalPhase);
            IntVec3 outcomeCell = gateCore != null && !gateCore.Destroyed ? gateCore.PositionHeld : sourceCell;
            lastRewardSummary = AbyssalDominionRewardUtility.ApplyOutcome(this, terminalPhase, outcomeCell);

            if (sourceCircle != null && !sourceCircle.Destroyed && sourceCircle.Map == map)
            {
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, terminalPhase == DominionCrisisPhase.Completed ? 0.10f : 0.16f);

                if (terminalPhase == DominionCrisisPhase.Failed)
                {
                    sourceCircle.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 12f, 0f, -1f, null, null, null));
                }
            }

            DestroyTrackedAnchors(false);
            DestroyTrackedGate(false);

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
            string baseText;
            switch (terminalPhase)
            {
                case DominionCrisisPhase.Cancelled:
                    baseText = "ABY_DominionCrisisCancelledDesc".Translate(reason);
                    break;
                case DominionCrisisPhase.Completed:
                    baseText = "ABY_DominionCrisisCompletedDesc".Translate(reason);
                    break;
                default:
                    baseText = "ABY_DominionCrisisFailedDesc".Translate(reason);
                    break;
            }

            List<string> parts = new List<string> { baseText };
            if (!lastRewardSummary.NullOrEmpty())
            {
                parts.Add("ABY_DominionOutcome_RewardLine".Translate(lastRewardSummary));
            }

            if (CooldownTicksRemaining > 0)
            {
                parts.Add("ABY_DominionOutcome_CooldownLine".Translate(GetCooldownValue()));
            }

            return string.Join("\n\n", parts);
        }

        private void CleanupAnchorReferences()
        {
            if (activeAnchors == null)
            {
                activeAnchors = new List<Building_AbyssalDominionAnchor>();
                return;
            }

            for (int i = activeAnchors.Count - 1; i >= 0; i--)
            {
                Building_AbyssalDominionAnchor anchor = activeAnchors[i];
                if (anchor == null || anchor.Destroyed || anchor.Map != map)
                {
                    activeAnchors.RemoveAt(i);
                }
            }
        }

        private int CountLiveAnchors()
        {
            CleanupAnchorReferences();
            return activeAnchors?.Count ?? 0;
        }

        private void DestroyTrackedAnchors(bool silent)
        {
            CleanupAnchorReferences();

            if (activeAnchors == null)
            {
                activeAnchors = new List<Building_AbyssalDominionAnchor>();
                return;
            }

            List<Building_AbyssalDominionAnchor> anchors = new List<Building_AbyssalDominionAnchor>(activeAnchors);
            activeAnchors.Clear();
            for (int i = 0; i < anchors.Count; i++)
            {
                Building_AbyssalDominionAnchor anchor = anchors[i];
                if (anchor == null || anchor.Destroyed)
                {
                    continue;
                }

                if (!silent)
                {
                    FleckMaker.ThrowLightningGlow(anchor.DrawPos, map, 1.35f);
                }

                anchor.Destroy(DestroyMode.Vanish);
            }

            initialAnchorCount = 0;
        }

        private void CleanupGateReference()
        {
            if (gateCore != null && (gateCore.Destroyed || gateCore.Map != map))
            {
                gateCore = null;
            }
        }

        private void DestroyTrackedGate(bool silent)
        {
            CleanupGateReference();
            Building_AbyssalDominionGate gate = gateCore;
            gateCore = null;
            if (gate == null || gate.Destroyed)
            {
                return;
            }

            if (!silent)
            {
                FleckMaker.ThrowLightningGlow(gate.DrawPos, map, 2.1f);
            }

            gate.Destroy(DestroyMode.Vanish);
        }

        private static string GetAnchorDefName(DominionAnchorRole role)
        {
            switch (role)
            {
                case DominionAnchorRole.Drain:
                    return "ABY_DominionAnchor_Drain";
                case DominionAnchorRole.Ward:
                    return "ABY_DominionAnchor_Ward";
                case DominionAnchorRole.Breach:
                    return "ABY_DominionAnchor_Breach";
                default:
                    return "ABY_DominionAnchor_Suppression";
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
