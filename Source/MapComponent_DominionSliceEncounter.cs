using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public class MapComponent_DominionSliceEncounter : MapComponent
    {
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

        private string sessionId;
        private SlicePhase phase = SlicePhase.Dormant;
        private int phaseStartedTick;
        private int nextWaveTick;
        private int collapseAtTick;
        private int hazardPressure;
        private float heartShieldBonus;
        private int wavesTriggered;
        private string lastWaveLabel;
        private string lastWaveSummary;
        private Building_ABY_DominionSliceHeart heart;
        private List<Building_ABY_DominionSliceAnchor> anchors = new List<Building_ABY_DominionSliceAnchor>();

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

        public int HazardPressure
        {
            get { return hazardPressure; }
        }

        public int WavesTriggeredCount
        {
            get { return wavesTriggered; }
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
            Scribe_Values.Look(ref lastWaveLabel, "lastWaveLabel");
            Scribe_Values.Look(ref lastWaveSummary, "lastWaveSummary");
            Scribe_References.Look(ref heart, "heart");
            Scribe_Collections.Look(ref anchors, "anchors", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && anchors == null)
            {
                anchors = new List<Building_ABY_DominionSliceAnchor>();
            }
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

            CleanupReferences();
            int now = Find.TickManager.TicksGame;

            if (phase == SlicePhase.Breach)
            {
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
                    string reason = session.victoryAchieved
                        ? "ABY_DominionPocketOutcome_FailureNoExtraction".Translate()
                        : "ABY_DominionPocketOutcome_FailureCollapse".Translate();
                    AbyssalDominionPocketUtility.FailAndCollapsePocketSlice(session, map, reason, false);
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
            lastWaveLabel = null;
            lastWaveSummary = null;
            anchors.Clear();
            heart = null;

            SpawnEncounterObjects(session);
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
            if (anchor != null && anchors != null)
            {
                anchors.Remove(anchor);
            }

            if (phase == SlicePhase.Anchorfall)
            {
                Messages.Message("ABY_DominionSliceEncounter_AnchorDestroyed".Translate(GetLiveAnchorCount()), new TargetInfo(anchor.PositionHeld, map), MessageTypeDefOf.PositiveEvent, false);
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

            List<Pawn> colonists = map.mapPawns != null ? map.mapPawns.FreeColonistsSpawned : null;
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
        }

        private void BeginHeartExposed()
        {
            phase = SlicePhase.HeartExposed;
            phaseStartedTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            nextWaveTick = phaseStartedTick + AbyssalDominionSliceWaveDirector.GetNextWaveDelayTicks(phase, wavesTriggered, hazardPressure, GetLiveAnchorCount());
            Messages.Message("ABY_DominionSliceEncounter_HeartExposed".Translate(), new TargetInfo(heart != null ? heart.PositionHeld : map.Center, map), MessageTypeDefOf.ThreatBig, false);
        }

        private void BeginCollapse(bool victory)
        {
            phase = SlicePhase.Collapse;
            phaseStartedTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            collapseAtTick = phaseStartedTick + 3600;
            nextWaveTick = 0;

            ABY_DominionPocketSession session;
            if (TryResolveSession(out session) && session != null)
            {
                session.victoryAchieved = victory;
                session.collapseAtTick = collapseAtTick;
                session.rewardSummary = GetRewardForecastValue();
            }

            Messages.Message(
                victory ? "ABY_DominionSliceEncounter_CollapseStarted".Translate(GetCollapseEta()) : "ABY_DominionSliceEncounter_Failed".Translate(),
                new TargetInfo(map.Center, map),
                victory ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.ThreatBig,
                false);
        }

        private void CleanupReferences()
        {
            if (anchors == null)
            {
                anchors = new List<Building_ABY_DominionSliceAnchor>();
            }

            anchors.RemoveAll(anchor => anchor == null || anchor.Destroyed || anchor.Map != map);
            if (heart != null && (heart.Destroyed || heart.Map != map))
            {
                heart = null;
            }
        }

        private int GetLiveAnchorCount()
        {
            CleanupReferences();
            return anchors != null ? anchors.Count : 0;
        }

        private void TriggerWave()
        {
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
                    pawn.Destroy(DestroyMode.Vanish);
                    continue;
                }

                GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
                AbyssalThreatPawnUtility.PrepareThreatPawn(pawn);
                spawned.Add(pawn);
            }

            if (spawned.Count > 0)
            {
                AbyssalLordUtility.EnsureAssaultLord(spawned, faction, map, false);
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
                true,
                false,
                false,
                false,
                true,
                0f,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                0f,
                0f,
                null,
                0f,
                null,
                null,
                null,
                null,
                null,
                null,
                true,
                DevelopmentalStage.Adult);

            pawn = PawnGenerator.GeneratePawn(request);
            return pawn != null;
        }

        private bool TryFindWaveSpawnCell(IntVec3 focus, int minRadius, int maxRadius, out IntVec3 cell)
        {
            int resolvedMinRadius = System.Math.Max(5, minRadius);
            int resolvedMaxRadius = System.Math.Max(resolvedMinRadius + 2, maxRadius);

            for (int i = 0; i < 40; i++)
            {
                IntVec3 candidate;
                if (!CellFinder.TryFindRandomCellNear(focus, map, resolvedMaxRadius, c => c.Standable(map) && !c.Fogged(map), out candidate))
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
            List<Pawn> colonists = map.mapPawns != null ? map.mapPawns.FreeColonistsSpawned : null;
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
            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail(AbyssalFactionDefName);
            if (def == null || Find.FactionManager == null)
            {
                return null;
            }

            return Find.FactionManager.FirstFactionOfDef(def);
        }
    }
}
