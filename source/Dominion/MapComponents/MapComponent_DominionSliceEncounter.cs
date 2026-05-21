using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class MapComponent_DominionSliceEncounter : MapComponent
    {
        private const string StaticPressureSparkMoteDefName = "ABY_Mote_DominionSliceStaticPressureSpark";
        private const string SeamDustBurstMoteDefName = "ABY_Mote_DominionSliceSeamDustBurst";

        public enum SlicePhase
        {
            Dormant,
            Breach,
            Anchorfall,
            HeartExposed,
            Collapse,
            Failed
        }

        private const string AbyssalFactionDefName = "ABY_AbyssalHost";
        private const string SealAnchorDefName = "ABY_DominionSliceAnchor_Seal";
        private const string ChoirAnchorDefName = "ABY_DominionSliceAnchor_Choir";
        private const string LawAnchorDefName = "ABY_DominionSliceAnchor_Law";
        private const string HeartDefName = "ABY_DominionSliceHeart";
        private const string HeartGuardianPawnKindDefName = "ABY_AorticChainHarrower";
        private const int HeartGuardianCount = 3;
        private const int HeartGuardianInitialSpawnDelayTicks = 150;
        private const int HeartGuardianSpawnRetryDelayTicks = 180;
        private const int ReferenceRestoreFallbackIntervalTicks = 300;

        private string sessionId;
        private SlicePhase phase = SlicePhase.Dormant;
        private int phaseStartedTick;
        private int nextWaveTick;
        private int collapseAtTick;
        private int hazardPressure;
        private float heartShieldBonus;
        private int wavesTriggered;
        private bool heartGuardiansSpawned;
        private int nextHeartGuardianSpawnRetryTick;
        private int scheduledHeartGuardianSpawnTick;
        private int heartGuardianSpawnAttempts;
        private int nextReferenceRestoreTick;
        private string lastWaveLabel;
        private string lastWaveSummary;
        private Building_ABY_DominionSliceHeart heart;
        private List<Building_ABY_DominionSliceAnchor> anchors = new List<Building_ABY_DominionSliceAnchor>();
        private readonly List<Building_ABY_DominionFissure> fissureVisuals = new List<Building_ABY_DominionFissure>();
        private List<Pawn> heartGuardians = new List<Pawn>();
        private readonly List<LinkSeverBurst> linkSeverBursts = new List<LinkSeverBurst>();
        private readonly List<HeartGuardianSeverBurst> heartGuardianSeverBursts = new List<HeartGuardianSeverBurst>();

        private struct LinkSeverBurst
        {
            public Vector3 anchorPos;
            public Vector3 heartPos;
            public DominionSliceAnchorRole role;
            public int startTick;
            public int seed;
        }

        private struct HeartGuardianSeverBurst
        {
            public Vector3 guardianPos;
            public Vector3 heartPos;
            public int startTick;
            public int seed;
        }

        public bool IsActiveEncounter
        {
            get { return phase == SlicePhase.Breach || phase == SlicePhase.Anchorfall || phase == SlicePhase.HeartExposed || phase == SlicePhase.Collapse; }
        }

        public bool IsAnchorfallActive
        {
            get { return phase == SlicePhase.Anchorfall; }
        }

        public bool IsHeartExposed
        {
            get { return phase == SlicePhase.HeartExposed || phase == SlicePhase.Collapse; }
        }

        public SlicePhase CurrentPhase
        {
            get { return phase; }
        }

        public int LiveAnchorCount
        {
            get { return GetLiveAnchorCount(); }
        }

        public int LiveHeartGuardianCount
        {
            get { return GetLiveHeartGuardianCount(); }
        }

        public int HazardPressure
        {
            get { return hazardPressure; }
        }

        public int WavesTriggeredCount
        {
            get { return wavesTriggered; }
        }

        public Building_ABY_DominionSliceHeart HeartBuilding
        {
            get
            {
                CleanupReferences();
                RestoreReferencesFromMapThrottled(Find.TickManager != null ? Find.TickManager.TicksGame : 0, heart == null);
                return heart;
            }
        }

        public bool ShouldDrawAnchorLinks
        {
            get { return IsActiveEncounter && !IsHeartExposed && GetLiveAnchorCount() > 0 && HeartBuilding != null; }
        }

        public bool ShouldDrawHeartShield
        {
            get { return IsActiveEncounter && !IsHeartExposed && GetLiveAnchorCount() > 0; }
        }

        public string LastWaveLabel
        {
            get { return lastWaveLabel; }
        }

        public string LastWaveSummary
        {
            get { return lastWaveSummary; }
        }

        public string GetRewardForecastValue()
        {
            ABY_DominionPocketSession session;
            TryResolveSession(out session);
            return AbyssalDominionSliceRewardUtility.FormatRewardProfile(
                AbyssalDominionSliceRewardUtility.BuildRewardProfile(this, session),
                session != null && session.victoryAchieved);
        }

        public MapComponent_DominionSliceEncounter(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref sessionId, "sessionId");
            Scribe_Values.Look(ref phase, "phase", SlicePhase.Dormant);
            Scribe_Values.Look(ref phaseStartedTick, "phaseStartedTick", 0);
            Scribe_Values.Look(ref nextWaveTick, "nextWaveTick", 0);
            Scribe_Values.Look(ref collapseAtTick, "collapseAtTick", 0);
            Scribe_Values.Look(ref hazardPressure, "hazardPressure", 0);
            Scribe_Values.Look(ref heartShieldBonus, "heartShieldBonus", 0f);
            Scribe_Values.Look(ref wavesTriggered, "wavesTriggered", 0);
            Scribe_Values.Look(ref heartGuardiansSpawned, "heartGuardiansSpawned", false);
            Scribe_Values.Look(ref nextHeartGuardianSpawnRetryTick, "nextHeartGuardianSpawnRetryTick", 0);
            Scribe_Values.Look(ref scheduledHeartGuardianSpawnTick, "scheduledHeartGuardianSpawnTick", 0);
            Scribe_Values.Look(ref heartGuardianSpawnAttempts, "heartGuardianSpawnAttempts", 0);
            Scribe_Values.Look(ref lastWaveLabel, "lastWaveLabel");
            Scribe_Values.Look(ref lastWaveSummary, "lastWaveSummary");
            Scribe_References.Look(ref heart, "heart");
            Scribe_Collections.Look(ref anchors, "anchors", LookMode.Reference);
            Scribe_Collections.Look(ref heartGuardians, "heartGuardians", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                anchors ??= new List<Building_ABY_DominionSliceAnchor>();
                heartGuardians ??= new List<Pawn>();
                RestoreReferencesFromMap(true);
                if (IsActiveEncounter && !heartGuardiansSpawned && GetLiveHeartGuardianCount() < HeartGuardianCount)
                {
                    ScheduleHeartGuardianSpawn(HeartGuardianInitialSpawnDelayTicks);
                }
            }
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            if (map == null || Find.CurrentMap != map)
            {
                return;
            }

            DrawPersistentAnchorLinks();
            DrawAnchorLinkSeverBursts();
            DrawHeartGuardianSeverBursts();
            DrawRegisteredFissureVisuals();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Find.TickManager == null)
            {
                return;
            }

            if (phase == SlicePhase.Dormant)
            {
                TryAutoResolveSession();
                return;
            }

            int now = Find.TickManager.TicksGame;
            CleanupReferences();
            RestoreReferencesFromMapThrottled(now);
            TryRunScheduledHeartGuardianSpawn(now);

            if (phase == SlicePhase.Breach)
            {
                if (!heartGuardiansSpawned && GetLiveHeartGuardianCount() < HeartGuardianCount)
                {
                    ScheduleHeartGuardianSpawn(HeartGuardianInitialSpawnDelayTicks);
                }

                if (now >= nextWaveTick)
                {
                    TriggerWave();
                    nextWaveTick = now + 600;
                }

                if (now - phaseStartedTick >= 240)
                {
                    BeginAnchorfall();
                }

                return;
            }

            if (phase == SlicePhase.Anchorfall)
            {
                if (GetLiveAnchorCount() <= 0)
                {
                    BeginHeartExposed();
                    return;
                }

                if (GetLiveHeartGuardianCount() < HeartGuardianCount)
                {
                    ScheduleHeartGuardianSpawn(30);
                }

                if (now >= nextWaveTick)
                {
                    TriggerWave();
                    nextWaveTick = now + 780;
                }

                return;
            }

            if (phase == SlicePhase.HeartExposed)
            {
                if (heart == null || heart.Destroyed)
                {
                    BeginCollapse(true);
                    return;
                }

                if (GetLiveHeartGuardianCount() < HeartGuardianCount)
                {
                    ScheduleHeartGuardianSpawn(30);
                }

                if (now >= nextWaveTick)
                {
                    TriggerWave();
                    nextWaveTick = now + 900;
                }

                if (hazardPressure > 0 && now % 180 == 0)
                {
                    EmitAmbientPressure();
                }

                return;
            }

            if (phase == SlicePhase.Collapse && collapseAtTick > 0 && now >= collapseAtTick)
            {
                ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
                ABY_DominionPocketSession session;
                if (runtime != null && runtime.TryGetSessionById(sessionId, out session))
                {
                    if (session.victoryAchieved)
                    {
                        // Package 3: once the heart is destroyed, victory must not be converted into a failure
                        // simply because the strike team did not extract before the old collapse timer elapsed.
                        collapseAtTick = now + 3600;
                        session.collapseAtTick = collapseAtTick;
                        nextWaveTick = 0;
                        return;
                    }

                    AbyssalDominionPocketUtility.FailAndCollapsePocketSlice(session, map, "ABY_DominionPocketOutcome_FailureCollapse".Translate(), false);
                }
                else
                {
                    phase = SlicePhase.Failed;
                }
            }
        }

        public bool TryInitialize(ABY_DominionPocketSession session)
        {
            if (session == null || session.sessionId.NullOrEmpty() || session.pocketMapId != map.uniqueID)
            {
                return false;
            }

            sessionId = session.sessionId;
            phase = SlicePhase.Breach;
            phaseStartedTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextWaveTick = phaseStartedTick + 180;
            collapseAtTick = 0;
            hazardPressure = 0;
            heartShieldBonus = 0f;
            wavesTriggered = 0;
            heartGuardiansSpawned = false;
            nextHeartGuardianSpawnRetryTick = 0;
            scheduledHeartGuardianSpawnTick = 0;
            heartGuardianSpawnAttempts = 0;
            lastWaveLabel = null;
            lastWaveSummary = null;
            anchors.Clear();
            heartGuardians.Clear();
            linkSeverBursts.Clear();
            heartGuardianSeverBursts.Clear();
            heart = null;

            SpawnEncounterObjects(session);
            ScheduleHeartGuardianSpawn(HeartGuardianInitialSpawnDelayTicks);
            Messages.Message("ABY_DominionSliceEncounter_Breach".Translate(), new TargetInfo(session.heartCell.IsValid ? session.heartCell : map.Center, map), MessageTypeDefOf.ThreatSmall, false);
            return true;
        }

        public void RegisterAnchor(Building_ABY_DominionSliceAnchor anchor)
        {
            if (anchor == null)
            {
                return;
            }

            if (anchors == null)
            {
                anchors = new List<Building_ABY_DominionSliceAnchor>();
            }

            if (!anchors.Contains(anchor))
            {
                anchors.Add(anchor);
            }
        }

        public void RegisterHeart(Building_ABY_DominionSliceHeart value)
        {
            if (value != null)
            {
                heart = value;
            }
        }

        public void NotifyAnchorDestroyed(Building_ABY_DominionSliceAnchor anchor)
        {
            if (anchor != null)
            {
                TrySpawnAnchorLinkSeverVfx(anchor);
                if (anchors != null)
                {
                    anchors.Remove(anchor);
                }
            }

            if (phase == SlicePhase.Anchorfall)
            {
                ScheduleHeartGuardianSpawn(30);
                TargetInfo target = anchor != null ? new TargetInfo(anchor.PositionHeld, map) : new TargetInfo(map.Center, map);
                Messages.Message("ABY_DominionSliceEncounter_AnchorDestroyed".Translate(GetLiveAnchorCount()), target, MessageTypeDefOf.PositiveEvent, false);
                if (GetLiveAnchorCount() <= 0)
                {
                    BeginHeartExposed();
                }
            }
        }

        public void NotifyHeartDestroyed(Building_ABY_DominionSliceHeart destroyedHeart)
        {
            if (heart == destroyedHeart)
            {
                heart = null;
            }

            if (phase == SlicePhase.HeartExposed)
            {
                BeginCollapse(true);
            }
        }

        public void RegisterFissureVisual(Building_ABY_DominionFissure fissure)
        {
            if (fissure == null)
            {
                return;
            }

            if (!fissureVisuals.Contains(fissure))
            {
                fissureVisuals.Add(fissure);
            }
        }

        public void DeregisterFissureVisual(Building_ABY_DominionFissure fissure)
        {
            if (fissureVisuals == null || fissure == null)
            {
                return;
            }

            fissureVisuals.Remove(fissure);
        }

        private void DrawPersistentAnchorLinks()
        {
            Building_ABY_DominionSliceHeart heartBuilding = HeartBuilding;
            if (heartBuilding == null || map == null || !ShouldDrawAnchorLinks)
            {
                return;
            }

            DrawAnchorLinksFromHeart(heartBuilding);
        }

        private void DrawRegisteredFissureVisuals()
        {
            if (map == null)
            {
                return;
            }

            if (fissureVisuals.Count == 0)
            {
                RestoreFissureVisualsFromMap();
            }

            for (int i = fissureVisuals.Count - 1; i >= 0; i--)
            {
                Building_ABY_DominionFissure fissure = fissureVisuals[i];
                if (fissure == null || fissure.Destroyed || fissure.Map != map)
                {
                    fissureVisuals.RemoveAt(i);
                    continue;
                }

                fissure.DrawFissureVisualFromMapComponent();
            }
        }

        private void RestoreFissureVisualsFromMap()
        {
            if (map == null || map.listerThings == null)
            {
                return;
            }

            List<Thing> allThings = map.listerThings.AllThings;
            if (allThings == null)
            {
                return;
            }

            for (int i = 0; i < allThings.Count; i++)
            {
                Building_ABY_DominionFissure fissure = allThings[i] as Building_ABY_DominionFissure;
                if (fissure != null && !fissure.Destroyed && fissure.Map == map && !fissureVisuals.Contains(fissure))
                {
                    fissureVisuals.Add(fissure);
                }
            }
        }

        public void DrawAnchorLinkSeverBursts()
        {
            if (linkSeverBursts == null || linkSeverBursts.Count == 0 || map == null || Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            for (int i = linkSeverBursts.Count - 1; i >= 0; i--)
            {
                LinkSeverBurst burst = linkSeverBursts[i];
                int age = now - burst.startTick;
                if (age > DominionSliceVfxUtility.SeverBurstDurationTicks)
                {
                    linkSeverBursts.RemoveAt(i);
                    continue;
                }

                DominionSliceVfxUtility.DrawAnchorLinkSeverBurst(burst.anchorPos, burst.heartPos, map, burst.role, burst.seed, age);
            }
        }

        public void DrawHeartGuardianSeverBursts()
        {
            if (heartGuardianSeverBursts == null || heartGuardianSeverBursts.Count == 0 || map == null || Find.TickManager == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            for (int i = heartGuardianSeverBursts.Count - 1; i >= 0; i--)
            {
                HeartGuardianSeverBurst burst = heartGuardianSeverBursts[i];
                int age = now - burst.startTick;
                if (age > DominionSliceVfxUtility.SeverBurstDurationTicks)
                {
                    heartGuardianSeverBursts.RemoveAt(i);
                    continue;
                }

                DominionSliceVfxUtility.DrawHeartGuardianSeverBurst(burst.guardianPos, burst.heartPos, map, burst.seed, age);
            }
        }
        public void DrawAnchorLinksFromHeart(Building_ABY_DominionSliceHeart heartBuilding)
        {
            if (heartBuilding == null || map == null || !ShouldDrawAnchorLinks)
            {
                return;
            }

            CleanupReferences();
            RestoreReferencesFromMapThrottled(Find.TickManager != null ? Find.TickManager.TicksGame : 0);
            if (anchors == null || anchors.Count == 0)
            {
                return;
            }

            Vector3 heartPos = heartBuilding.DrawPos;
            for (int i = 0; i < anchors.Count; i++)
            {
                Building_ABY_DominionSliceAnchor anchor = anchors[i];
                if (anchor == null || anchor.Destroyed || anchor.Map != map)
                {
                    continue;
                }

                DominionSliceVfxUtility.DrawAnchorLink(anchor.DrawPos, heartPos, map, anchor.AnchorRole, anchor.thingIDNumber);
            }
        }


        private void TrySpawnAnchorLinkSeverVfx(Building_ABY_DominionSliceAnchor anchor)
        {
            if (anchor == null || map == null || !IsActiveEncounter || IsHeartExposed)
            {
                return;
            }

            Building_ABY_DominionSliceHeart heartBuilding = HeartBuilding;
            if (heartBuilding == null || heartBuilding.Destroyed)
            {
                return;
            }

            Vector3 anchorPos = anchor.DrawPos;
            Vector3 heartPos = heartBuilding.DrawPos;
            DominionSliceVfxUtility.SpawnAnchorLinkSever(anchorPos, heartPos, map, anchor.AnchorRole);

            linkSeverBursts.Add(new LinkSeverBurst
            {
                anchorPos = anchorPos,
                heartPos = heartPos,
                role = anchor.AnchorRole,
                startTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0,
                seed = anchor.thingIDNumber
            });
        }

        public void NotifyHeartGuardianKilled(Pawn guardian, IntVec3 lastKnownCell, string guardianKindDefName = null)
        {
            if (map == null)
            {
                return;
            }

            if (!guardianKindDefName.NullOrEmpty() && guardianKindDefName != HeartGuardianPawnKindDefName)
            {
                return;
            }

            if (heartGuardians != null && guardian != null)
            {
                heartGuardians.Remove(guardian);
            }

            IntVec3 guardianCell = lastKnownCell;
            if (!guardianCell.IsValid && guardian != null && guardian.PositionHeld.IsValid)
            {
                guardianCell = guardian.PositionHeld;
            }
            if (!guardianCell.IsValid)
            {
                guardianCell = map.Center;
            }

            Building_ABY_DominionSliceHeart heartBuilding = HeartBuilding;
            IntVec3 heartCell = heartBuilding != null && !heartBuilding.Destroyed && heartBuilding.PositionHeld.IsValid
                ? heartBuilding.PositionHeld
                : guardianCell;

            Vector3 guardianPos = guardianCell.ToVector3Shifted();
            Vector3 heartPos = heartCell.ToVector3Shifted();

            DominionSliceVfxUtility.SpawnHeartGuardianSeverance(guardianPos, heartPos, map);
            if (heartGuardianSeverBursts != null)
            {
                heartGuardianSeverBursts.Add(new HeartGuardianSeverBurst
                {
                    guardianPos = guardianPos,
                    heartPos = heartPos,
                    startTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0,
                    seed = guardian != null ? guardian.thingIDNumber : Rand.Int
                });
            }

            if (heartBuilding != null && IsActiveEncounter)
            {
                int remaining = GetLiveHeartGuardianCount(guardianKindDefName);
                Messages.Message(
                    "ABY_DominionSliceEncounter_HeartGuardianSevered".Translate(remaining),
                    new TargetInfo(guardianCell, map),
                    remaining > 0 ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.ThreatSmall,
                    false);
            }
        }

        public float GetHeartGuardianDamageFactor(string guardianKindDefName, float reductionPerGuardian, float maxReduction)
        {
            if (!IsActiveEncounter || !IsHeartExposed || reductionPerGuardian <= 0f)
            {
                return 1f;
            }

            int liveGuardians = GetLiveHeartGuardianCount(guardianKindDefName);
            if (liveGuardians <= 0)
            {
                return 1f;
            }

            float reduction = Mathf.Clamp(liveGuardians * reductionPerGuardian, 0f, Mathf.Clamp01(maxReduction));
            return Mathf.Clamp(1f - reduction, 0.05f, 1f);
        }

        public void AccelerateNextWave(int ticks)
        {
            if (ticks <= 0 || nextWaveTick <= 0)
            {
                return;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextWaveTick = System.Math.Max(now + 120, nextWaveTick - ticks);
        }

        public void AddHazardPressure(int amount)
        {
            hazardPressure = System.Math.Min(10, System.Math.Max(0, hazardPressure + amount));
        }

        public void ReinforceHeartShield(float amount)
        {
            if (amount > 0f)
            {
                heartShieldBonus = System.Math.Min(0.45f, heartShieldBonus + amount);
            }
        }

        public void EmitHeartPulse(Building_ABY_DominionSliceHeart source)
        {
            if (source == null || map == null)
            {
                return;
            }

            IReadOnlyList<Pawn> colonists = map.mapPawns != null ? map.mapPawns.FreeColonistsSpawned : null;
            if (colonists == null)
            {
                return;
            }

            int affected = 0;
            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                {
                    continue;
                }

                if (pawn.PositionHeld.DistanceTo(source.PositionHeld) > 12f)
                {
                    continue;
                }

                pawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, 4f + hazardPressure, 0f, -1f, source));
                affected++;
            }

            if (affected > 0)
            {
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", source.PositionHeld, map);
            }
        }

        public string GetCollapseEta()
        {
            if (phase != SlicePhase.Collapse || collapseAtTick <= 0 || Find.TickManager == null)
            {
                return "ABY_DominionSliceEncounter_CollapseInactive".Translate();
            }

            return (collapseAtTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();
        }

        public string GetNextWaveEtaValue()
        {
            if (!IsActiveEncounter || nextWaveTick <= 0 || Find.TickManager == null)
            {
                return "ABY_DominionWaveEta_Pending".Translate();
            }

            int ticks = System.Math.Max(0, nextWaveTick - Find.TickManager.TicksGame);
            if (ticks <= 90)
            {
                return "ABY_DominionWaveEta_Imminent".Translate();
            }

            return "ABY_DominionWaveEta_Queued".Translate(ticks.ToStringTicksToPeriod());
        }

        public string GetTelemetryObjectiveLabel()
        {
            switch (phase)
            {
                case SlicePhase.Breach:
                    return "ABY_DominionPocketTelemetry_ObjectiveBreach".Translate();
                case SlicePhase.Anchorfall:
                    return "ABY_DominionPocketTelemetry_ObjectiveAnchors".Translate(GetLiveAnchorCount());
                case SlicePhase.HeartExposed:
                    return "ABY_DominionPocketTelemetry_ObjectiveHeart".Translate(GetCollapseEta());
                case SlicePhase.Collapse:
                    return "ABY_DominionPocketTelemetry_ObjectiveExtract".Translate(GetCollapseEta());
                case SlicePhase.Failed:
                    return "ABY_DominionPocketTelemetry_ObjectiveFailed".Translate();
                default:
                    return "ABY_DominionPocketTelemetry_ObjectiveDormant".Translate();
            }
        }

        public string GetTelemetryStatusLabel()
        {
            return "ABY_DominionPocketTelemetry_Status".Translate(GetTelemetryObjectiveLabel(), GetNextWaveEtaValue(), GetRewardForecastValue());
        }

        private void TryAutoResolveSession()
        {
            ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
            if (runtime == null)
            {
                return;
            }

            ABY_DominionPocketSession session;
            if (runtime.TryGetSessionByPocketMap(map, out session))
            {
                TryInitialize(session);
            }
        }

        private bool TryResolveSession(out ABY_DominionPocketSession session)
        {
            session = null;
            ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
            return runtime != null && runtime.TryGetSessionById(sessionId, out session);
        }

        private void SpawnEncounterObjects(ABY_DominionPocketSession session)
        {
            if (session == null)
            {
                return;
            }

            SpawnAnchor(session.anchorCells.Count > 0 ? session.anchorCells[0] : map.Center, SealAnchorDefName);
            SpawnAnchor(session.anchorCells.Count > 1 ? session.anchorCells[1] : map.Center, ChoirAnchorDefName);
            SpawnAnchor(session.anchorCells.Count > 2 ? session.anchorCells[2] : map.Center, LawAnchorDefName);

            ThingDef heartDef = DefDatabase<ThingDef>.GetNamedSilentFail(HeartDefName);
            if (heartDef != null)
            {
                Thing thing = ThingMaker.MakeThing(heartDef);
                Building_ABY_DominionSliceHeart spawned = thing as Building_ABY_DominionSliceHeart;
                if (spawned != null)
                {
                    GenSpawn.Spawn(spawned, session.heartCell.IsValid ? session.heartCell : map.Center, map, Rot4.North);
                    heart = spawned;
                }
            }
        }

        private void SpawnAnchor(IntVec3 cell, string defName)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null || !cell.InBounds(map))
            {
                return;
            }

            Thing thing = ThingMaker.MakeThing(def);
            Building_ABY_DominionSliceAnchor anchor = thing as Building_ABY_DominionSliceAnchor;
            if (anchor != null)
            {
                GenSpawn.Spawn(anchor, cell, map, Rot4.North);
                RegisterAnchor(anchor);
            }
        }

        private void BeginAnchorfall()
        {
            phase = SlicePhase.Anchorfall;
            phaseStartedTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextWaveTick = phaseStartedTick + AbyssalDominionSliceWaveDirector.GetNextWaveDelayTicks(phase, wavesTriggered, hazardPressure, GetLiveAnchorCount());
            Messages.Message("ABY_DominionSliceEncounter_Anchorfall".Translate(GetLiveAnchorCount()), new TargetInfo(map.Center, map), MessageTypeDefOf.ThreatBig, false);
            ScheduleHeartGuardianSpawn(60);
        }

        private void BeginHeartExposed()
        {
            phase = SlicePhase.HeartExposed;
            phaseStartedTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextWaveTick = phaseStartedTick + AbyssalDominionSliceWaveDirector.GetNextWaveDelayTicks(phase, wavesTriggered, hazardPressure, GetLiveAnchorCount());
            Messages.Message("ABY_DominionSliceEncounter_HeartExposed".Translate(), new TargetInfo(heart != null ? heart.PositionHeld : map.Center, map), MessageTypeDefOf.ThreatBig, false);
            if (heart != null && !heart.Destroyed)
            {
                DominionSliceVfxUtility.SpawnHeartExposedBurst(heart.DrawPos, map);
            }

            ScheduleHeartGuardianSpawn(30);
        }

        private void ScheduleHeartGuardianSpawn(int delayTicks)
        {
            if (map == null || heartGuardiansSpawned || GetLiveHeartGuardianCount() >= HeartGuardianCount)
            {
                return;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int dueTick = now + Mathf.Max(1, delayTicks);
            if (scheduledHeartGuardianSpawnTick <= 0 || dueTick < scheduledHeartGuardianSpawnTick)
            {
                scheduledHeartGuardianSpawnTick = dueTick;
            }
        }

        private void TryRunScheduledHeartGuardianSpawn(int now)
        {
            if (!IsActiveEncounter || heartGuardiansSpawned || GetLiveHeartGuardianCount() >= HeartGuardianCount)
            {
                scheduledHeartGuardianSpawnTick = 0;
                return;
            }

            if (scheduledHeartGuardianSpawnTick <= 0)
            {
                ScheduleHeartGuardianSpawn(HeartGuardianInitialSpawnDelayTicks);
                return;
            }

            if (now < scheduledHeartGuardianSpawnTick)
            {
                return;
            }

            scheduledHeartGuardianSpawnTick = 0;
            TrySpawnHeartGuardians(true);
        }

        private void TrySpawnHeartGuardians(bool force = false)
        {
            if ((!force && phase != SlicePhase.Anchorfall && phase != SlicePhase.HeartExposed) || map == null)
            {
                return;
            }

            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (!force && nextHeartGuardianSpawnRetryTick > 0 && now < nextHeartGuardianSpawnRetryTick)
            {
                return;
            }

            CleanupReferences();
            RestoreReferencesFromMap(true);

            int liveCount = GetLiveHeartGuardianCount();
            if (heartGuardiansSpawned && liveCount >= HeartGuardianCount)
            {
                return;
            }

            // Safety migration for saves that were touched by the earlier invisible-anchor sprite package:
            // if the encounter says the guardians were spawned but no live guardian and no guardian corpse exists,
            // allow one recovery spawn instead of leaving the heart unguarded forever.
            if (heartGuardiansSpawned && liveCount <= 0 && !HasAnyHeartGuardianCorpseOnMap())
            {
                heartGuardiansSpawned = false;
            }

            if (heartGuardiansSpawned)
            {
                return;
            }

            Building_ABY_DominionSliceHeart heartBuilding = HeartBuilding;
            if (heartBuilding == null || heartBuilding.Destroyed)
            {
                ScheduleHeartGuardianSpawn(HeartGuardianSpawnRetryDelayTicks);
                return;
            }

            PawnKindDef guardianKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(HeartGuardianPawnKindDefName);
            Faction faction = ResolveAbyssalFaction();
            if (guardianKind == null || faction == null)
            {
                nextHeartGuardianSpawnRetryTick = now + HeartGuardianSpawnRetryDelayTicks;
                scheduledHeartGuardianSpawnTick = nextHeartGuardianSpawnRetryTick;
                if (guardianKind == null)
                {
                    ABY_LogThrottleUtility.Warning("aortic-guardian-kind-missing", "[Abyssal Protocol] Aortic Chain Harrower PawnKindDef is missing; heart guardian spawn will retry.", 900);
                }
                if (faction == null)
                {
                    ABY_LogThrottleUtility.Warning("aortic-guardian-faction-missing", "[Abyssal Protocol] Could not resolve any hostile faction for Aortic Chain Harrower spawn; heart guardian spawn will retry.", 900);
                }
                return;
            }

            if (GetLiveHeartGuardianCount() >= HeartGuardianCount)
            {
                heartGuardiansSpawned = true;
                return;
            }

            List<IntVec3> spawnFocuses = BuildHeartGuardianSpawnFocuses(heartBuilding.PositionHeld);
            if (spawnFocuses.Count == 0)
            {
                ScheduleHeartGuardianSpawn(HeartGuardianSpawnRetryDelayTicks);
                return;
            }

            heartGuardianSpawnAttempts++;
            List<Pawn> spawnedNow = new List<Pawn>();
            IntVec3 center = heartBuilding.PositionHeld;
            for (int slot = heartGuardians.Count; slot < HeartGuardianCount; slot++)
            {
                Pawn pawn;
                if (!TryGeneratePawn(guardianKind, faction, out pawn) || pawn == null)
                {
                    ABY_LogThrottleUtility.Warning("aortic-guardian-generate-failed", "[Abyssal Protocol] Failed to generate Aortic Chain Harrower for Dominion Slice heart guardian spawn.", 900);
                    continue;
                }

                IntVec3 focus = spawnFocuses[Mathf.Clamp(slot, 0, spawnFocuses.Count - 1)];
                IntVec3 spawnCell;
                if (!TryFindHeartGuardianSpawnCell(focus, center, spawnedNow, out spawnCell))
                {
                    // Do not abort the spawn only because the preferred pylon ring is blocked.
                    // ABY_SafeSpawnUtility will search for a nearby standable fallback.
                    spawnCell = focus.IsValid ? focus : center;
                }

                Pawn spawnedPawn;
                if (!ABY_SafeSpawnUtility.TrySpawnPawnSafe(
                        pawn,
                        spawnCell,
                        map,
                        out spawnedPawn,
                        Rot4.Random,
                        WipeMode.Vanish,
                        false,
                        false,
                        "dominion slice heart guardian pylon spawn"))
                {
                    SafeDestroyUnspawnedPawn(pawn, "heart guardian spawn failed");
                    ABY_LogThrottleUtility.Warning("aortic-guardian-spawn-failed", "[Abyssal Protocol] Aortic Chain Harrower spawn failed near " + spawnCell + ". The encounter will retry shortly.", 900);
                    continue;
                }

                EmitDominionEmergenceCue(spawnCell, spawnedPawn.kindDef, slot);
                TryPrepareThreatPawnSafe(spawnedPawn);
                heartGuardians.Add(spawnedPawn);
                spawnedNow.Add(spawnedPawn);
            }

            if (spawnedNow.Count > 0)
            {
                TryEnsureAssaultLordSafe(spawnedNow, faction);
                Messages.Message("ABY_DominionSliceEncounter_HeartGuardiansAwakened".Translate(spawnedNow.Count), new TargetInfo(center, map), MessageTypeDefOf.ThreatBig, false);
                nextHeartGuardianSpawnRetryTick = 0;
            }
            else
            {
                nextHeartGuardianSpawnRetryTick = now + HeartGuardianSpawnRetryDelayTicks;
                scheduledHeartGuardianSpawnTick = nextHeartGuardianSpawnRetryTick;
            }

            if (GetLiveHeartGuardianCount() >= HeartGuardianCount)
            {
                heartGuardiansSpawned = true;
                scheduledHeartGuardianSpawnTick = 0;
            }
            else if (!heartGuardiansSpawned)
            {
                nextHeartGuardianSpawnRetryTick = now + HeartGuardianSpawnRetryDelayTicks;
                scheduledHeartGuardianSpawnTick = nextHeartGuardianSpawnRetryTick;
            }
        }

        private List<IntVec3> BuildHeartGuardianSpawnFocuses(IntVec3 heartCell)
        {
            List<IntVec3> focuses = new List<IntVec3>();
            if (anchors != null)
            {
                for (int i = 0; i < anchors.Count && focuses.Count < HeartGuardianCount; i++)
                {
                    Building_ABY_DominionSliceAnchor anchor = anchors[i];
                    if (anchor == null || anchor.Destroyed || anchor.Map != map || !anchor.PositionHeld.IsValid)
                    {
                        continue;
                    }

                    focuses.Add(anchor.PositionHeld);
                }
            }

            while (focuses.Count < HeartGuardianCount && heartCell.IsValid)
            {
                focuses.Add(heartCell);
            }

            return focuses;
        }

        private bool TryFindHeartGuardianSpawnCell(IntVec3 focus, IntVec3 heartCenter, List<Pawn> reservedPawns, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            if (!focus.IsValid)
            {
                focus = heartCenter;
            }

            if (focus.IsValid && TryFindHeartGuardianSpawnCellNearFocus(focus, heartCenter, reservedPawns, 1.8f, 5.8f, out cell))
            {
                return true;
            }

            if (heartCenter.IsValid && heartCenter != focus && TryFindHeartGuardianSpawnCellNearFocus(heartCenter, heartCenter, reservedPawns, 5.0f, 9.2f, out cell))
            {
                return true;
            }

            IntVec3 fallback;
            if (focus.IsValid && CellFinder.TryFindRandomCellNear(focus, map, 12, c => ABY_SafeSpawnUtility.IsCellSpawnable(c, map), out fallback))
            {
                cell = fallback;
                return true;
            }

            if (heartCenter.IsValid && CellFinder.TryFindRandomCellNear(heartCenter, map, 14, c => ABY_SafeSpawnUtility.IsCellSpawnable(c, map), out fallback))
            {
                cell = fallback;
                return true;
            }

            return false;
        }

        private bool TryFindHeartGuardianSpawnCellNearFocus(IntVec3 focus, IntVec3 heartCenter, List<Pawn> reservedPawns, float minRadius, float maxRadius, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || !focus.IsValid)
            {
                return false;
            }

            float bestScore = float.MinValue;
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(focus, maxRadius, true))
            {
                if (!candidate.InBounds(map) || !candidate.Standable(map) || AbyssalThreatPawnUtility.CellHasOtherPawn(candidate, map, null))
                {
                    continue;
                }

                float distance = focus.DistanceTo(candidate);
                if (distance < minRadius || distance > maxRadius)
                {
                    continue;
                }

                bool reserved = false;
                if (reservedPawns != null)
                {
                    for (int i = 0; i < reservedPawns.Count; i++)
                    {
                        Pawn pawn = reservedPawns[i];
                        if (pawn != null && pawn.Spawned && pawn.PositionHeld.DistanceTo(candidate) < 3.1f)
                        {
                            reserved = true;
                            break;
                        }
                    }
                }

                if (reserved)
                {
                    continue;
                }

                float preferredDistance = Mathf.Lerp(minRadius, maxRadius, 0.46f);
                float distanceScore = maxRadius - Mathf.Abs(distance - preferredDistance);
                float heartScore = heartCenter.IsValid ? Mathf.Clamp(heartCenter.DistanceTo(candidate), 0f, 18f) * 0.04f : 0f;
                float sightScore = heartCenter.IsValid && GenSight.LineOfSight(heartCenter, candidate, map) ? 0.35f : 0f;
                float focusSightScore = GenSight.LineOfSight(focus, candidate, map) ? 0.55f : 0f;
                float score = distanceScore + heartScore + sightScore + focusSightScore + Rand.Value * 0.35f;
                if (score > bestScore)
                {
                    bestScore = score;
                    cell = candidate;
                }
            }

            return cell.IsValid;
        }

        private void BeginCollapse(bool victory)
        {
            phase = SlicePhase.Collapse;
            phaseStartedTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            collapseAtTick = phaseStartedTick + 3600;
            nextWaveTick = 0;

            ABY_DominionPocketSession session;
            TryResolveSession(out session);
            bool finalVictory = victory || (session != null && session.victoryAchieved);

            if (session != null)
            {
                if (finalVictory)
                {
                    session.victoryAchieved = true;
                    if (session.victoryAchievedTick <= 0)
                    {
                        session.victoryAchievedTick = phaseStartedTick;
                    }
                }
                session.collapseAtTick = collapseAtTick;
                if (session.rewardSummary.NullOrEmpty())
                {
                    session.rewardSummary = GetRewardForecastValue();
                }
            }

            Messages.Message(
                finalVictory ? "ABY_DominionSliceEncounter_CollapseStarted".Translate(GetCollapseEta()) : "ABY_DominionSliceEncounter_Failed".Translate(),
                new TargetInfo(map.Center, map),
                finalVictory ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.ThreatBig,
                false);

            if (finalVictory)
            {
                ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
                if (runtime != null)
                {
                    IntVec3 focusCell = session != null && session.heartCell.IsValid ? session.heartCell : map.Center;
                    runtime.TrySendDominionHeartDestroyedLoreLetterOnce(map, focusCell);
                }
            }
        }

        private void CleanupReferences()
        {
            if (anchors == null)
            {
                anchors = new List<Building_ABY_DominionSliceAnchor>();
            }
            else
            {
                for (int i = anchors.Count - 1; i >= 0; i--)
                {
                    Building_ABY_DominionSliceAnchor anchor = anchors[i];
                    if (anchor == null || anchor.Destroyed || anchor.Map != map)
                    {
                        anchors.RemoveAt(i);
                    }
                }
            }

            if (heartGuardians == null)
            {
                heartGuardians = new List<Pawn>();
            }
            else
            {
                for (int i = heartGuardians.Count - 1; i >= 0; i--)
                {
                    Pawn guardian = heartGuardians[i];
                    if (guardian == null || guardian.Destroyed || guardian.Dead || guardian.Map != map || guardian.kindDef == null || guardian.kindDef.defName != HeartGuardianPawnKindDefName)
                    {
                        heartGuardians.RemoveAt(i);
                    }
                }
            }

            if (heart != null && (heart.Destroyed || heart.Map != map))
            {
                heart = null;
            }
        }

        private void RestoreReferencesFromMapThrottled(int now, bool force = false)
        {
            if (!force && HasCompleteEncounterReferences())
            {
                return;
            }

            if (!force && now < nextReferenceRestoreTick)
            {
                return;
            }

            RestoreReferencesFromMap(force);
            nextReferenceRestoreTick = now + ReferenceRestoreFallbackIntervalTicks;
        }

        private bool HasCompleteEncounterReferences()
        {
            return anchors != null
                && anchors.Count >= 3
                && heart != null
                && (heartGuardiansSpawned || heartGuardians != null && heartGuardians.Count >= HeartGuardianCount);
        }

        private void RestoreReferencesFromMap(bool force = false)
        {
            if (map?.listerThings?.AllThings == null)
            {
                return;
            }

            anchors ??= new List<Building_ABY_DominionSliceAnchor>();
            heartGuardians ??= new List<Pawn>();
            if (!force && HasCompleteEncounterReferences())
            {
                return;
            }

            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (thing is Building_ABY_DominionSliceAnchor anchor && !anchors.Contains(anchor))
                {
                    anchors.Add(anchor);
                    continue;
                }

                if (thing is Building_ABY_DominionSliceHeart candidateHeart && heart == null)
                {
                    heart = candidateHeart;
                    continue;
                }

                if (thing is Pawn guardian && guardian.kindDef != null && guardian.kindDef.defName == HeartGuardianPawnKindDefName && !heartGuardians.Contains(guardian))
                {
                    heartGuardians.Add(guardian);
                }
            }
        }

        private bool HasAnyHeartGuardianCorpseOnMap()
        {
            if (map?.listerThings == null)
            {
                return false;
            }

            List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            if (corpses == null)
            {
                return false;
            }

            for (int i = 0; i < corpses.Count; i++)
            {
                if (corpses[i] is Corpse corpse && corpse.InnerPawn?.kindDef != null && corpse.InnerPawn.kindDef.defName == HeartGuardianPawnKindDefName)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetLiveAnchorCount()
        {
            CleanupReferences();
            return anchors != null ? anchors.Count : 0;
        }

        private int GetLiveHeartGuardianCount(string guardianKindDefName = null)
        {
            CleanupReferences();
            if (heartGuardians == null)
            {
                return 0;
            }

            string expectedDefName = !guardianKindDefName.NullOrEmpty() ? guardianKindDefName : HeartGuardianPawnKindDefName;
            int count = 0;
            for (int i = 0; i < heartGuardians.Count; i++)
            {
                Pawn guardian = heartGuardians[i];
                if (guardian != null && !guardian.Destroyed && !guardian.Dead && guardian.Spawned && guardian.Map == map && guardian.kindDef != null && guardian.kindDef.defName == expectedDefName)
                {
                    count++;
                }
            }

            return count;
        }

        private void TriggerWave()
        {
            if (!AbyssalDominionPocketUtility.HasAnyPlayerPawnsOnMap(map))
            {
                return;
            }

            Faction faction = ResolveAbyssalFaction();
            if (faction == null)
            {
                return;
            }

            ABY_DominionPocketSession session;
            TryResolveSession(out session);

            AbyssalDominionSliceWaveDirector.DominionSliceWavePlan plan =
                AbyssalDominionSliceWaveDirector.BuildPlan(
                    map,
                    phase,
                    wavesTriggered,
                    hazardPressure,
                    GetLiveAnchorCount(),
                    anchors,
                    heart,
                    session);

            if (plan == null || plan.PawnKinds.Count == 0)
            {
                return;
            }

            List<Pawn> spawned = new List<Pawn>();
            IntVec3 focus = plan.FocusCell.IsValid ? plan.FocusCell : (heart != null && !heart.Destroyed ? heart.PositionHeld : map.Center);

            for (int i = 0; i < plan.PawnKinds.Count; i++)
            {
                Pawn pawn;
                if (!TryGeneratePawn(plan.PawnKinds[i], faction, out pawn) || pawn == null)
                {
                    continue;
                }

                IntVec3 spawnCell;
                if (!TryFindWaveSpawnCell(focus, plan.MinSpawnRadius, plan.MaxSpawnRadius, out spawnCell))
                {
                    SafeDestroyUnspawnedPawn(pawn, "dominion slice wave no spawn cell");
                    continue;
                }

                Pawn spawnedPawn;
                if (!ABY_SafeSpawnUtility.TrySpawnPawnSafe(
                        pawn,
                        spawnCell,
                        map,
                        out spawnedPawn,
                        Rot4.Random,
                        WipeMode.Vanish,
                        false,
                        false,
                        "dominion slice wave pawn spawn"))
                {
                    SafeDestroyUnspawnedPawn(pawn, "dominion slice wave spawn failed");
                    continue;
                }

                EmitDominionEmergenceCue(spawnCell, spawnedPawn.kindDef, i);
                TryPrepareThreatPawnSafe(spawnedPawn);
                spawned.Add(spawnedPawn);
            }

            if (spawned.Count > 0)
            {
                TryEnsureAssaultLordSafe(spawned, faction);
                wavesTriggered++;
                lastWaveLabel = plan.GetLabel();
                lastWaveSummary = "ABY_DominionSliceEncounter_WaveSummary".Translate(lastWaveLabel, spawned.Count, wavesTriggered);
                Messages.Message(lastWaveSummary, new TargetInfo(focus, map), MessageTypeDefOf.ThreatSmall, false);
            }
            else
            {
                lastWaveLabel = plan.GetLabel();
                lastWaveSummary = null;
            }
        }

        private void EmitDominionEmergenceCue(IntVec3 cell, PawnKindDef kindDef, int index)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            Vector3 loc = cell.ToVector3Shifted();
            float glowScale = kindDef != null && kindDef.race != null && kindDef.race.race != null && kindDef.race.race.Humanlike ? 1.25f : 0.95f;
            glowScale += (index % 3) * 0.08f;

            // Dominion seam emergence: the pocket is already hell. Reinforcements should feel
            // pressure-forged out of cracked machinery, not summoned through another portal.
            FleckMaker.ThrowLightningGlow(loc, map, glowScale);
            FleckMaker.ThrowDustPuff(loc, map, Rand.Range(0.42f, 0.74f));
            TryMakeStaticDominionMote(SeamDustBurstMoteDefName, loc + new Vector3(Rand.Range(-0.10f, 0.10f), 0f, Rand.Range(-0.10f, 0.10f)), Rand.Range(0.85f, 1.25f));

            if (Rand.Chance(0.82f))
            {
                Vector3 sparkLoc = loc + new Vector3(Rand.Range(-0.22f, 0.22f), 0f, Rand.Range(-0.22f, 0.22f));
                FleckMaker.ThrowMicroSparks(sparkLoc, map);
                TryMakeStaticDominionMote(StaticPressureSparkMoteDefName, sparkLoc, Rand.Range(0.62f, 0.96f));
            }

            if (Rand.Chance(0.55f))
            {
                Vector3 dustLoc = loc + new Vector3(Rand.Range(-0.34f, 0.34f), 0f, Rand.Range(-0.34f, 0.34f));
                FleckMaker.ThrowDustPuff(dustLoc, map, Rand.Range(0.52f, 0.9f));
                TryMakeStaticDominionMote(SeamDustBurstMoteDefName, dustLoc, Rand.Range(0.72f, 1.08f));
            }

            if (index == 0)
            {
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", cell, map);
            }
        }


        private void TryMakeStaticDominionMote(string defName, Vector3 position, float scale)
        {
            if (map == null || string.IsNullOrEmpty(defName))
            {
                return;
            }

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (moteDef == null)
            {
                return;
            }

            MoteMaker.MakeStaticMote(position, map, moteDef, Mathf.Clamp(scale, 0.25f, 1.65f));
        }

        private List<PawnKindDef> BuildWaveKinds()
        {
            List<PawnKindDef> result = new List<PawnKindDef>();
            if (phase == SlicePhase.Breach)
            {
                TryAddKind(result, "ABY_RiftImp", 2);
                TryAddKind(result, "ABY_EmberHound", 1);
                TryAddKind(result, "ABY_ChainZealot", 1);
            }
            else if (phase == SlicePhase.Anchorfall)
            {
                TryAddKind(result, "ABY_GateWarden", 1);
                TryAddKind(result, "ABY_NullPriest", 1);
                TryAddKind(result, "ABY_ChainZealot", 1);
                TryAddKind(result, "ABY_EmberHound", 1);
            }
            else
            {
                TryAddKind(result, "ABY_GateWarden", 1);
                TryAddKind(result, "ABY_RiftSniper", 1);
                TryAddKind(result, "ABY_NullPriest", 1);
                TryAddKind(result, "ABY_ChainZealot", 1);
            }

            return result;
        }

        private void TryAddKind(List<PawnKindDef> list, string defName, int count)
        {
            PawnKindDef def = DefDatabase<PawnKindDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                list.Add(def);
            }
        }

        private bool TryGeneratePawn(PawnKindDef kindDef, Faction faction, out Pawn pawn)
        {
            pawn = null;
            if (kindDef == null || faction == null)
            {
                return false;
            }

            PawnGenerationRequest request = new PawnGenerationRequest(
                kindDef,
                faction,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                colonistRelationChanceFactor: 0f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: false,
                allowPregnant: false,
                allowFood: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0f,
                biocodeApparelChance: 0f,
                extraPawnForExtraRelationChance: null,
                relationWithExtraPawnChanceFactor: 0f,
                validatorPreGear: null,
                validatorPostGear: null,
                fixedBirthName: null,
                fixedLastName: null,
                fixedGender: null,
                fixedIdeo: null,
                forceNoIdeo: true,
                developmentalStages: DevelopmentalStage.Adult);

            return ABY_SafeSpawnUtility.TryGeneratePawnSafe(
                request,
                out pawn,
                out _,
                "dominion slice wave pawn generation");
        }

        private void TryPrepareThreatPawnSafe(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                return;
            }

            try
            {
                AbyssalThreatPawnUtility.PrepareThreatPawn(pawn);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Dominion slice wave threat pawn preparation failed for " + pawn.ToStringSafe() + ": " + ex.GetType().Name + ": " + ex.Message + "\n" + ex);
            }
        }

        private void TryEnsureAssaultLordSafe(List<Pawn> spawned, Faction faction)
        {
            if (spawned == null || spawned.Count == 0 || faction == null || map == null)
            {
                return;
            }

            try
            {
                AbyssalLordUtility.EnsureAssaultLord(spawned, faction, map, false);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Dominion slice wave lord creation failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex);
            }
        }

        private void SafeDestroyUnspawnedPawn(Pawn pawn, string context)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            try
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Failed to destroy unspawned dominion wave pawn (" + context + "): " + ex.GetType().Name + ": " + ex.Message + "\n" + ex);
            }
        }

        private bool TryFindWaveSpawnCell(IntVec3 focus, int minRadius, int maxRadius, out IntVec3 cell)
        {
            int resolvedMinRadius = System.Math.Max(5, minRadius);
            int resolvedMaxRadius = System.Math.Max(resolvedMinRadius + 2, maxRadius);

            for (int i = 0; i < 40; i++)
            {
                IntVec3 candidate;
                if (!CellFinder.TryFindRandomCellNear(focus, map, resolvedMaxRadius, c => ABY_SafeSpawnUtility.IsCellSpawnable(c, map), out candidate))
                {
                    continue;
                }

                float distance = candidate.DistanceTo(focus);
                if (distance >= resolvedMinRadius && distance <= resolvedMaxRadius)
                {
                    cell = candidate;
                    return true;
                }
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private void EmitAmbientPressure()
        {
            IReadOnlyList<Pawn> colonists = map.mapPawns != null ? map.mapPawns.FreeColonistsSpawned : null;
            if (colonists == null)
            {
                return;
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn pawn = colonists[i];
                if (pawn == null || pawn.Dead || !pawn.Spawned)
                {
                    continue;
                }

                if (pawn.PositionHeld.DistanceTo(map.Center) <= 10f)
                {
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Flame, 2f + hazardPressure, 0f, -1f, heart));
                }
            }
        }

        private Faction ResolveAbyssalFaction()
        {
            // ABY_AbyssalHost is hidden and has requiredCountAtGameStart=0, so many saves do
            // not contain a live faction instance until something explicitly creates it.
            // The normal summon pipeline already uses AbyssalBossSummonUtility because it can
            // generate the hidden faction on demand and then fall back to a valid hostile
            // faction. Heart guardians must use the same resolver; otherwise their spawn
            // silently retries forever with faction == null.
            return AbyssalBossSummonUtility.ResolveHostileFaction();
        }
    }
}
