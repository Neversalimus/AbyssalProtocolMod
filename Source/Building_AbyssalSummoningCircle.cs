using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_AbyssalSummoningCircle : Building_WorkTable
    {
        public enum ConsoleRitualPhase
        {
            Idle,
            Charging,
            Surge,
            Breach,
            Cooldown
        }

        private enum RitualPhase
        {
            Idle,
            Charging,
            Surge,
            Breach,
            Cooldown
        }

        private static readonly Graphic OuterRingGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_OuterRing",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic InnerGlyphGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_InnerGlyphs",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic EnergyArcsGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_EnergyArcs",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic DataLatticeGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_DataLattice",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic CoreGlowGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_CoreGlow",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic IdleGlowGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_IdleGlow",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic CapacitorBaySocketGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/Capacitors/ABY_CapacitorBaySocket",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Vector2 OuterRingSize = new Vector2(9.38f, 9.38f);
        private static readonly Vector2 InnerGlyphSize = new Vector2(8.72f, 8.72f);
        private static readonly Vector2 EnergyArcsSize = new Vector2(8.36f, 8.36f);
        private static readonly Vector2 DataLatticeSize = new Vector2(7.52f, 7.52f);
        private static readonly Vector2 CoreGlowSize = new Vector2(2.84f, 2.84f);
        private static readonly Vector2 IdleGlowSize = new Vector2(9.20f, 9.20f);
        private static readonly Vector2 BreachSize = new Vector2(6.90f, 6.90f);
        private static readonly Vector2 CapacitorBaySocketSize = new Vector2(1.34f, 1.34f);
        private static readonly Vector2 CapacitorMountedBaseSize = new Vector2(1.42f, 1.42f);
        private static readonly Vector3 CoreCapacitorBayOffset = new Vector3(0f, 0f, 1.10f);
        private static readonly Vector3 AuxiliaryCapacitorBayOffset = new Vector3(0f, 0f, -1.10f);
        private static readonly Texture2D ConsoleCommandIcon = ContentFinder<Texture2D>.Get("UI/AbyssalSummoningCircle/ABY_SummoningSeal", false);


        private RitualPhase ritualPhase = RitualPhase.Idle;
        private int phaseTicksRemaining;
        private int phaseDuration;
        private int ritualSeed;

        private PawnKindDef pendingPawnKindDef;
        private string pendingBossLabel;
        private string pendingRitualId;
        private string pendingSummonMode;
        private string pendingCompletionLetterLabelKey;
        private string pendingCompletionLetterDescKey;
        private Faction pendingFaction;
        private IntVec3 pendingSpawnCell = IntVec3.Invalid;
        private int pendingImpCount;
        private int pendingImpPortalWarmupTicks;
        private int pendingImpSpawnIntervalTicks;
        private int pendingImpPortalLingerTicks;
        private bool reducedConsoleEffects;

        private AbyssalCircleCapacitorSlot coreCapacitorSlot = new AbyssalCircleCapacitorSlot();
        private AbyssalCircleCapacitorSlot auxiliaryCapacitorSlot = new AbyssalCircleCapacitorSlot();
        private float storedCapacitorCharge;
        private float instabilityHeat;
        private float pendingCapacitorStartupCost;
        private float pendingCapacitorSustainDrainPerSecond;
        private float pendingCapacitorReserveRequired;
        private float pendingCapacitorThroughputRequired;
        private int capacitorRecoveryTicksRemaining;
        private int capacitorRecoveryDurationTicks;
        private bool capacitorOverchannelEnabled;
        private bool capacitorEmergencyDumpEnabled = true;
        private bool pendingCapacitorForcedStart;
        private bool pendingCapacitorEmergencyDumpUsed;
        private float pendingCapacitorBacklashSeverity;
        private List<AbyssalCircleModuleSlot> moduleSlots = new List<AbyssalCircleModuleSlot>();

        public bool RitualActive => ritualPhase != RitualPhase.Idle;
        public bool IsPoweredForRitual => GetComp<CompPowerTrader>()?.PowerOn ?? true;
        public IntVec3 RitualFocusCell => GenAdj.OccupiedRect(Position, Rotation, def.Size).CenterCell;
        public bool ReducedConsoleEffects => reducedConsoleEffects;
        public float RitualProgress => RitualActive ? GetPhaseProgress() : 0f;
        public bool CapacitorOverchannelEnabled => capacitorOverchannelEnabled;
        public bool CapacitorEmergencyDumpEnabled => capacitorEmergencyDumpEnabled;
        public IReadOnlyList<AbyssalCircleModuleSlot> ModuleSlots
        {
            get
            {
                EnsureModuleSlotsInitialized();
                return moduleSlots;
            }
        }
        public int InstalledStabilizerCount
        {
            get
            {
                EnsureModuleSlotsInitialized();
                return AbyssalCircleModuleUtility.CountInstalledModules(moduleSlots, DefModExtension_AbyssalCircleModule.StabilizerFamily);
            }
        }

        public float ContainmentRating => AbyssalCircleInstabilityUtility.GetEffectiveContainment(this, AbyssalCircleInstabilityUtility.CalculateContainment(this));
        public float InstabilityHeat => instabilityHeat;
        public float ResidualContamination => Map?.GetComponent<MapComponent_AbyssalCircleInstability>()?.ResidualContamination ?? 0f;


        public ConsoleRitualPhase CurrentRitualPhase
        {
            get
            {
                switch (ritualPhase)
                {
                    case RitualPhase.Charging:
                        return ConsoleRitualPhase.Charging;
                    case RitualPhase.Surge:
                        return ConsoleRitualPhase.Surge;
                    case RitualPhase.Breach:
                        return ConsoleRitualPhase.Breach;
                    case RitualPhase.Cooldown:
                        return ConsoleRitualPhase.Cooldown;
                    default:
                        return ConsoleRitualPhase.Idle;
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ritualPhase, "ritualPhase", RitualPhase.Idle);
            Scribe_Values.Look(ref phaseTicksRemaining, "phaseTicksRemaining", 0);
            Scribe_Values.Look(ref phaseDuration, "phaseDuration", 0);
            Scribe_Values.Look(ref ritualSeed, "ritualSeed", 0);
            Scribe_Defs.Look(ref pendingPawnKindDef, "pendingPawnKindDef");
            Scribe_Values.Look(ref pendingBossLabel, "pendingBossLabel");
            Scribe_Values.Look(ref pendingRitualId, "pendingRitualId");
            Scribe_Values.Look(ref pendingSummonMode, "pendingSummonMode");
            Scribe_Values.Look(ref pendingCompletionLetterLabelKey, "pendingCompletionLetterLabelKey");
            Scribe_Values.Look(ref pendingCompletionLetterDescKey, "pendingCompletionLetterDescKey");
            Scribe_References.Look(ref pendingFaction, "pendingFaction");
            Scribe_Values.Look(ref pendingSpawnCell, "pendingSpawnCell");
            Scribe_Values.Look(ref pendingImpCount, "pendingImpCount", 0);
            Scribe_Values.Look(ref pendingImpPortalWarmupTicks, "pendingImpPortalWarmupTicks", 0);
            Scribe_Values.Look(ref pendingImpSpawnIntervalTicks, "pendingImpSpawnIntervalTicks", 0);
            Scribe_Values.Look(ref pendingImpPortalLingerTicks, "pendingImpPortalLingerTicks", 0);
            Scribe_Values.Look(ref reducedConsoleEffects, "reducedConsoleEffects", false);
            Scribe_Deep.Look(ref coreCapacitorSlot, "coreCapacitorSlot");
            Scribe_Deep.Look(ref auxiliaryCapacitorSlot, "auxiliaryCapacitorSlot");
            Scribe_Values.Look(ref storedCapacitorCharge, "storedCapacitorCharge", 0f);
            Scribe_Values.Look(ref instabilityHeat, "instabilityHeat", 0f);
            Scribe_Values.Look(ref pendingCapacitorStartupCost, "pendingCapacitorStartupCost", 0f);
            Scribe_Values.Look(ref pendingCapacitorSustainDrainPerSecond, "pendingCapacitorSustainDrainPerSecond", 0f);
            Scribe_Values.Look(ref pendingCapacitorReserveRequired, "pendingCapacitorReserveRequired", 0f);
            Scribe_Values.Look(ref pendingCapacitorThroughputRequired, "pendingCapacitorThroughputRequired", 0f);
            Scribe_Values.Look(ref capacitorRecoveryTicksRemaining, "capacitorRecoveryTicksRemaining", 0);
            Scribe_Values.Look(ref capacitorRecoveryDurationTicks, "capacitorRecoveryDurationTicks", 0);
            Scribe_Values.Look(ref capacitorOverchannelEnabled, "capacitorOverchannelEnabled", false);
            Scribe_Values.Look(ref capacitorEmergencyDumpEnabled, "capacitorEmergencyDumpEnabled", true);
            Scribe_Values.Look(ref pendingCapacitorForcedStart, "pendingCapacitorForcedStart", false);
            Scribe_Values.Look(ref pendingCapacitorEmergencyDumpUsed, "pendingCapacitorEmergencyDumpUsed", false);
            Scribe_Values.Look(ref pendingCapacitorBacklashSeverity, "pendingCapacitorBacklashSeverity", 0f);
            Scribe_Collections.Look(ref moduleSlots, "moduleSlots", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureCapacitorSlotsInitialized();
                EnsureModuleSlotsInitialized();
                ClampStoredCapacitorCharge();
            }
        }

        public void SetReducedConsoleEffects(bool value)
        {
            reducedConsoleEffects = value;
        }

        public void SetCapacitorOverchannelEnabled(bool value)
        {
            capacitorOverchannelEnabled = value;
        }

        public void SetCapacitorEmergencyDumpEnabled(bool value)
        {
            capacitorEmergencyDumpEnabled = value;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            yield return new Command_Action
            {
                defaultLabel = AbyssalSummoningConsoleUtility.GetOpenConsoleLabel(),
                defaultDesc = AbyssalSummoningConsoleUtility.GetOpenConsoleDesc(),
                icon = ConsoleCommandIcon,
                action = delegate
                {
                    Find.WindowStack.Add(new Window_AbyssalSummoningConsole(this));
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                }
            };
        }

        protected override void Tick()
        {
            base.Tick();

            EnsureCapacitorSlotsInitialized();
            TickCapacitorRecovery();
            TickCapacitorCharge();
            TickCapacitorRitualFeed();
            TickInstabilityState();

            if (!RitualActive || Map == null)
            {
                return;
            }

            if (!IsPoweredForRitual)
            {
                if (ShouldDoHashInterval(60))
                {
                    Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.05f);
                }

                return;
            }

            phaseTicksRemaining--;
            TickRitualEffects();

            if (phaseTicksRemaining > 0)
            {
                return;
            }

            AdvancePhase();
        }

        public bool TryStartSummonSequence(Pawn activator, CompProperties_UseEffectSummonBoss summonProps, out string failReason)
        {
            failReason = null;

            if (summonProps == null)
            {
                failReason = "Missing summon properties.";
                return false;
            }

            if (!IsReadyForSigil(out failReason))
            {
                return false;
            }

            if (!AbyssalCircleCapacitorRitualUtility.TryAuthorizeRitualStart(this, summonProps, capacitorOverchannelEnabled, out AbyssalCircleCapacitorRitualUtility.CapacitorReadinessReport capacitorReport, out bool forcedStart, out failReason))
            {
                return false;
            }

            pendingCapacitorForcedStart = forcedStart;
            pendingCapacitorEmergencyDumpUsed = false;
            pendingCapacitorBacklashSeverity = forcedStart ? AbyssalCircleCapacitorRitualUtility.GetOverchannelBacklashSeverity(capacitorReport) : 0f;
            pendingCapacitorStartupCost = capacitorReport.Profile != null
                ? (forcedStart ? AbyssalCircleCapacitorRitualUtility.GetOverchannelStartupCost(capacitorReport) : capacitorReport.EffectiveStartupRequired)
                : 0f;
            pendingCapacitorReserveRequired = capacitorReport.Profile != null
                ? (forcedStart ? AbyssalCircleCapacitorRitualUtility.GetOverchannelReserveCommitment(capacitorReport) : capacitorReport.EffectiveTotalRequired)
                : 0f;
            pendingCapacitorThroughputRequired = capacitorReport.Profile != null ? capacitorReport.EffectiveThroughputRequired : 0f;
            pendingCapacitorSustainDrainPerSecond = AbyssalCircleCapacitorRitualUtility.GetSustainDrainPerSecond(capacitorReport, forcedStart);

            pendingRitualId = summonProps.ritualId;
            pendingSummonMode = summonProps.summonMode ?? "Boss";
            pendingBossLabel = summonProps.bossLabel;
            pendingCompletionLetterLabelKey = summonProps.completionLetterLabelKey;
            pendingCompletionLetterDescKey = summonProps.completionLetterDescKey;
            pendingFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            if (pendingFaction == null)
            {
                failReason = "ABY_CircleFail_NoHostileFaction".Translate();
                return false;
            }

            pendingPawnKindDef = null;
            pendingSpawnCell = RitualFocusCell;
            pendingImpCount = 0;
            pendingImpPortalWarmupTicks = 0;
            pendingImpSpawnIntervalTicks = 0;
            pendingImpPortalLingerTicks = 0;

            if (IsImpPortalSummonMode(pendingSummonMode))
            {
                pendingImpCount = Mathf.Max(1, summonProps.impCount);
                pendingImpPortalWarmupTicks = Mathf.Max(30, summonProps.impPortalWarmupTicks);
                pendingImpSpawnIntervalTicks = Mathf.Max(30, summonProps.impSpawnIntervalTicks);
                pendingImpPortalLingerTicks = Mathf.Max(600, summonProps.impPortalLingerTicks);
            }
            else
            {
                pendingPawnKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(summonProps.pawnKindDefName);
                if (pendingPawnKindDef == null)
                {
                    failReason = "Missing PawnKindDef: " + summonProps.pawnKindDefName;
                    return false;
                }

                if (IsPortalWaveSummonMode(pendingSummonMode))
                {
                    pendingImpPortalWarmupTicks = Mathf.Max(90, summonProps.impPortalWarmupTicks);
                    pendingImpSpawnIntervalTicks = Mathf.Max(14, summonProps.impSpawnIntervalTicks);
                    pendingImpPortalLingerTicks = Mathf.Max(120, summonProps.impPortalLingerTicks);
                }
                else if (IsHostilePackSummonMode(pendingSummonMode))
                {
                    pendingImpCount = Mathf.Max(1, summonProps.impCount);
                    if (!AbyssalBossSummonUtility.TryFindBossArrivalCell(Map, out pendingSpawnCell))
                    {
                        failReason = "ABY_CircleFail_NoBossArrival".Translate();
                        return false;
                    }
                }
                else if (!AbyssalBossSummonUtility.TryFindBossArrivalCell(Map, out pendingSpawnCell))
                {
                    failReason = "ABY_CircleFail_NoBossArrival".Translate();
                    return false;
                }
            }

            ritualSeed = thingIDNumber * 397 ^ Find.TickManager.TicksGame;

            if (pendingCapacitorStartupCost > 0.001f)
            {
                storedCapacitorCharge = Mathf.Max(0f, storedCapacitorCharge - pendingCapacitorStartupCost);
            }

            if (pendingCapacitorForcedStart)
            {
                Messages.Message("ABY_CapacitorForcedStart_Queued".Translate(), MessageTypeDefOf.NeutralEvent, false);
            }

            StartPhase(RitualPhase.Charging, 120);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.12f);
            SpawnMinorMote("ABY_Mote_ArchonDashTrail", 0.95f);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", RitualFocusCell, Map);

            return true;
        }

        public bool TryStartBossSummonSequence(Pawn activator, CompProperties_UseEffectSummonBoss summonProps, out string failReason)
        {
            return TryStartSummonSequence(activator, summonProps, out failReason);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            if (Map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float seedPhase = (ritualSeed == 0 ? thingIDNumber : ritualSeed) * 0.0137f;
            float ritualIntensity = GetRitualIntensity();
            float powerFactor = IsPoweredForRitual ? 1f : 0.20f;
            float activity = powerFactor * (1f + ritualIntensity * 2.6f);

            float pulseA = 1f + Mathf.Sin(ticks * 0.045f + seedPhase) * (0.045f + ritualIntensity * 0.060f);
            float pulseB = 1f + Mathf.Sin(ticks * 0.080f + 1.35f + seedPhase) * (0.060f + ritualIntensity * 0.085f);
            float pulseC = 1f + Mathf.Sin(ticks * 0.135f + 2.15f + seedPhase) * (0.090f + ritualIntensity * 0.140f);

            float outerAngle = (ticks * 0.18f * activity) % 360f;
            float innerAngle = 360f - ((ticks * 0.34f * activity) % 360f);
            float energyAngle = (ticks * 0.95f * activity) % 360f;
            float latticeAngle = 360f - ((ticks * 0.56f * activity) % 360f);
            float breachAngle = (ticks * (1.65f + ritualIntensity * 4.2f)) % 360f;

            Vector3 center = drawLoc;
            if (ritualPhase == RitualPhase.Breach)
            {
                float jitter = 0.018f + 0.015f * Mathf.PingPong(ticks * 0.22f, 1f);
                center.x += Mathf.Sin(ticks * 1.25f + seedPhase) * jitter;
                center.z += Mathf.Cos(ticks * 1.40f + seedPhase) * jitter;
            }

            DrawCapacitorBayVisuals(center, ticks, ritualIntensity, powerFactor);
            DrawModuleVisuals(center, ritualIntensity, ticks, seedPhase);
            DrawLayer(IdleGlowGraphic, center, IdleGlowSize * (pulseA + ritualIntensity * 0.08f), 0f, 0.004f);

            if (!IsPoweredForRitual)
            {
                return;
            }

            DrawLayer(OuterRingGraphic, center, OuterRingSize * (1f + (pulseA - 1f) * 0.45f + ritualIntensity * 0.04f), outerAngle, 0.010f);
            DrawLayer(InnerGlyphGraphic, center, InnerGlyphSize * (1f + (pulseB - 1f) * 0.26f + ritualIntensity * 0.05f), innerAngle, 0.014f);
            DrawLayer(EnergyArcsGraphic, center, EnergyArcsSize * (1f + (pulseC - 1f) * 0.55f + ritualIntensity * 0.08f), energyAngle, 0.018f);
            DrawLayer(DataLatticeGraphic, center, DataLatticeSize * (1f + (pulseB - 1f) * 0.22f + ritualIntensity * 0.04f), latticeAngle, 0.021f);
            DrawLayer(CoreGlowGraphic, center, CoreGlowSize * (1f + (pulseC - 1f) * 1.10f + ritualIntensity * 0.65f), 0f, 0.025f);

            if (ritualIntensity > 0.01f)
            {
                DrawLayer(EnergyArcsGraphic, center, EnergyArcsSize * (1.01f + ritualIntensity * 0.14f), -energyAngle * 1.15f, 0.019f);
                DrawLayer(CoreGlowGraphic, center, CoreGlowSize * (1.18f + ritualIntensity * 0.95f), breachAngle, 0.026f);
            }

            if (ritualPhase == RitualPhase.Surge || ritualPhase == RitualPhase.Breach)
            {
                DrawLayer(OuterRingGraphic, center, OuterRingSize * (1.03f + ritualIntensity * 0.09f), -outerAngle * 0.62f, 0.011f);
                DrawLayer(DataLatticeGraphic, center, DataLatticeSize * (1.05f + ritualIntensity * 0.11f), latticeAngle * 1.45f, 0.022f);
            }

            if (ritualPhase == RitualPhase.Breach)
            {
                float breachPulse = 0.84f + 0.16f * Mathf.PingPong(ticks * 0.20f, 1f);
                DrawLayer(IdleGlowGraphic, center, BreachSize * breachPulse, breachAngle * -0.65f, 0.027f);
                DrawLayer(CoreGlowGraphic, center, BreachSize * (0.65f + ritualIntensity * 0.22f), breachAngle * 1.35f, 0.028f);
            }
        }

        private void DrawCapacitorBayVisuals(Vector3 center, int ticks, float ritualIntensity, float powerFactor)
        {
            EnsureCapacitorSlotsInitialized();
            DrawCapacitorBayVisual(center, coreCapacitorSlot, CoreCapacitorBayOffset, ticks, ritualIntensity, powerFactor, 0.007f);
            DrawCapacitorBayVisual(center, auxiliaryCapacitorSlot, AuxiliaryCapacitorBayOffset, ticks, ritualIntensity, powerFactor, 0.008f);
        }

        private void DrawCapacitorBayVisual(Vector3 center, AbyssalCircleCapacitorSlot slot, Vector3 offset, int ticks, float ritualIntensity, float powerFactor, float yOffset)
        {
            Vector3 drawCenter = center + offset;
            float socketPulse = 1f + Mathf.Sin(ticks * 0.031f + offset.z * 0.9f + thingIDNumber * 0.05f) * 0.015f * powerFactor;
            DrawLayer(CapacitorBaySocketGraphic, drawCenter, CapacitorBaySocketSize * socketPulse, 0f, yOffset);

            if (slot == null || slot.IsEmpty)
            {
                return;
            }

            Graphic mountedGraphic = AbyssalCircleCapacitorUtility.GetMountedGraphic(slot.InstalledThingDef);
            if (mountedGraphic == null)
            {
                return;
            }

            float activePulse = 1f + Mathf.Sin(ticks * 0.052f + offset.z * 1.25f + thingIDNumber * 0.08f) * (0.018f + ritualIntensity * 0.020f) * powerFactor;
            DrawLayer(mountedGraphic, drawCenter, CapacitorMountedBaseSize * activePulse, 0f, yOffset + 0.002f);
        }

        private void DrawModuleVisuals(Vector3 center, float ritualIntensity, int ticks, float seedPhase)
        {
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
            Graphic socketGraphic = AbyssalCircleModuleUtility.GetSocketGraphic();
            if (socketGraphic == null)
            {
                return;
            }

            float poweredFactor = IsPoweredForRitual ? 1f : 0f;
            float socketPulse = 1f + (poweredFactor > 0f ? ritualIntensity * 0.025f : -0.015f);
            float socketSize = AbyssalCircleModuleUtility.GetSocketDrawScaleValue() * socketPulse;
            float yBase = 0.0294f;

            foreach (AbyssalCircleModuleEdge edge in AbyssalCircleModuleUtility.GetOrderedEdges())
            {
                Vector3 moduleCenter = center + AbyssalCircleModuleUtility.GetEdgeOffset(edge);
                float angle = AbyssalCircleModuleUtility.GetEdgeAngle(edge);

                DrawLayer(socketGraphic, moduleCenter, new Vector2(socketSize, socketSize), angle, yBase);

                AbyssalCircleModuleSlot slot = AbyssalCircleModuleUtility.GetSlot(moduleSlots, edge);
                if (slot == null || !slot.Occupied)
                {
                    continue;
                }

                ThingDef installedDef = slot.InstalledThingDef;
                DefModExtension_AbyssalCircleModule ext = AbyssalCircleModuleUtility.GetModuleExtension(installedDef);
                Graphic mountedGraphic = AbyssalCircleModuleUtility.GetMountedGraphic(installedDef);
                if (mountedGraphic == null)
                {
                    continue;
                }

                float tierPulse = 0.012f + Mathf.Max(0, (ext?.tier ?? 1) - 1) * 0.005f;
                float microPulse = 1f + Mathf.Sin(ticks * 0.055f + seedPhase + (int)edge * 0.9f) * tierPulse;
                float activeScale = 1f + ritualIntensity * (0.040f + Mathf.Max(0, (ext?.tier ?? 1) - 1) * 0.010f);
                float drawScale = AbyssalCircleModuleUtility.GetMountedDrawScale(installedDef) * microPulse * activeScale;

                if (poweredFactor > 0f)
                {
                    Graphic glowGraphic = AbyssalCircleModuleUtility.GetGlowGraphic(installedDef);
                    if (glowGraphic != null)
                    {
                        float glowPulse = 1f + Mathf.Sin(ticks * 0.090f + seedPhase + (int)edge * 0.72f) * 0.020f;
                        float glowScale = AbyssalCircleModuleUtility.GetGlowDrawScale(installedDef) * (1f + ritualIntensity * 0.085f) * glowPulse;
                        DrawLayer(glowGraphic, moduleCenter, new Vector2(glowScale, glowScale), angle, yBase + 0.0008f);
                    }
                }

                DrawLayer(mountedGraphic, moduleCenter, new Vector2(drawScale, drawScale), angle, yBase + 0.0012f);
            }
        }


        public override string GetInspectString()
        {
            string failReason;
            StringBuilder sb = new StringBuilder();
            string baseText = base.GetInspectString();
            if (!baseText.NullOrEmpty())
            {
                sb.Append(baseText);
            }

            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            if (RitualActive)
            {
                sb.Append(AbyssalSummoningConsoleUtility.GetPhaseText(GetCurrentPhaseTranslated(), Mathf.RoundToInt(GetPhaseProgress() * 100f)));

                if (!IsPoweredForRitual)
                {
                    sb.AppendLine();
                    sb.Append(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CircleInspect_StalledNoPower", "Stalled: no power."));
                }
            }
            else if (IsReadyForSigil(out failReason))
            {
                sb.Append(AbyssalSummoningConsoleUtility.GetReadyText());
            }
            else
            {
                sb.Append(AbyssalSummoningConsoleUtility.GetNotReadyText(failReason));
            }

            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetInspectSigilsText(AbyssalSummoningConsoleUtility.CountAvailableSigils(this, AbyssalSummoningConsoleUtility.GetDefaultRitual())));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetInspectReadinessText(AbyssalSummoningConsoleUtility.GetShortRequirementSummary(this, AbyssalSummoningConsoleUtility.GetDefaultRitual())));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetInspectRiskText(AbyssalSummoningConsoleUtility.GetRiskLabel(AbyssalSummoningConsoleUtility.GetRiskTier(this, AbyssalSummoningConsoleUtility.GetDefaultRitual()))));
            sb.AppendLine();
            sb.Append(AbyssalCircleCapacitorUtility.GetInstalledSummary(this));
            sb.AppendLine();
            sb.Append(AbyssalCircleCapacitorUtility.GetChargeReadout(this));
            sb.AppendLine();
            sb.Append("ABY_CapacitorPanel_Lattice".Translate() + ": " + AbyssalCircleCapacitorUtility.GetLatticeProfileLabel(this));
            sb.AppendLine();
            sb.Append(AbyssalCircleCapacitorUtility.GetLeakageReadout(this));
            sb.AppendLine();
            sb.Append("ABY_CapacitorPanel_Mode".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetOperationalModeSummary(this, AbyssalSummoningConsoleUtility.GetDefaultRitual()));
            sb.AppendLine();
            sb.Append("ABY_CapacitorPanel_Dump".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetEmergencyDumpStatusLabel(this));
            sb.AppendLine();
            AbyssalCircleCapacitorRitualUtility.CapacitorReadinessReport capacitorReport = AbyssalCircleCapacitorRitualUtility.CreateReadinessReport(this, AbyssalSummoningConsoleUtility.GetDefaultRitual());
            sb.Append("ABY_CapacitorPanel_State".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetSupportStateLabel(capacitorReport));
            sb.AppendLine();
            sb.Append("ABY_CapacitorPanel_Flow".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetChargeFlowReadout(capacitorReport));
            sb.AppendLine();
            sb.Append("ABY_CapacitorPanel_Grid".Translate() + ": " + AbyssalCircleCapacitorRitualUtility.GetGridSmoothingReadout(this));
            sb.AppendLine();
            sb.Append(AbyssalCircleCapacitorUtility.GetBaySummary(this));
            sb.AppendLine();
            sb.Append("ABY_CircleInspect_Stabilizers".Translate(InstalledStabilizerCount, moduleSlots.Count));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetStabilizerMiniSummary(this));

            return sb.ToString();
        }


        public IEnumerable<AbyssalCircleCapacitorSlot> GetCapacitorSlots()
        {
            EnsureCapacitorSlotsInitialized();
            yield return coreCapacitorSlot;
            yield return auxiliaryCapacitorSlot;
        }

        public AbyssalCircleCapacitorSlot GetCapacitorSlot(AbyssalCircleCapacitorBay bay)
        {
            EnsureCapacitorSlotsInitialized();
            return bay == AbyssalCircleCapacitorBay.Auxiliary ? auxiliaryCapacitorSlot : coreCapacitorSlot;
        }

        public int GetCapacitorSlotCount()
        {
            return 2;
        }

        public int GetInstalledCapacitorCount()
        {
            return AbyssalCircleCapacitorUtility.GetInstalledCount(GetCapacitorSlots());
        }

        public float StoredCapacitorCharge => storedCapacitorCharge;
        public int CapacitorRecoveryTicksRemaining => capacitorRecoveryTicksRemaining;
        public bool CapacitorRecovering => capacitorRecoveryTicksRemaining > 0;

        public float GetCapacitorCapacity()
        {
            return AbyssalCircleCapacitorUtility.GetTotalCapacity(GetCapacitorSlots());
        }

        public float GetCapacitorThroughput()
        {
            return AbyssalCircleCapacitorUtility.GetTotalThroughput(GetCapacitorSlots());
        }

        public float GetCapacitorChargeRatePerSecond()
        {
            return AbyssalCircleCapacitorUtility.GetTotalChargeRate(GetCapacitorSlots());
        }

        public float GetCapacitorPassiveLeakagePerSecond()
        {
            return AbyssalCircleCapacitorUtility.GetTotalPassiveLeakage(GetCapacitorSlots());
        }

        public float GetCapacitorCurrentLeakagePerSecond()
        {
            float baseLeakage = GetCapacitorPassiveLeakagePerSecond();
            float capacity = GetCapacitorCapacity();
            if (baseLeakage <= 0.001f || capacity <= 0.01f || storedCapacitorCharge <= 0.001f)
            {
                return 0f;
            }

            float fill = Mathf.Clamp01(storedCapacitorCharge / capacity);
            float factor = 0.55f;
            if (fill >= 0.92f)
            {
                factor = 1.25f;
            }
            else if (fill >= 0.60f)
            {
                factor = 0.85f;
            }

            if (CapacitorRecovering)
            {
                factor *= 0.95f;
            }

            if (RitualActive)
            {
                factor *= 1.75f;
            }

            if (!IsPoweredForRitual)
            {
                factor *= 1.10f;
            }

            return baseLeakage * factor;
        }

        public float GetCapacitorNetChargeFlowPerSecond()
        {
            float leakage = GetCapacitorCurrentLeakagePerSecond();
            if (!Spawned || Destroyed || Map == null || !IsPoweredForRitual)
            {
                return leakage > 0.001f ? -leakage : 0f;
            }

            return GetCapacitorEffectiveChargeRatePerSecond() - leakage;
        }

        public float GetCapacitorEffectiveChargeRatePerSecond()
        {
            if (!Spawned || Destroyed || Map == null || !IsPoweredForRitual)
            {
                return 0f;
            }

            float baseRate = GetCapacitorChargeRatePerSecond();
            if (baseRate <= 0.001f)
            {
                return 0f;
            }

            float capacity = GetCapacitorCapacity();
            float fill = capacity <= 0.01f ? 0f : Mathf.Clamp01(storedCapacitorCharge / capacity);
            float primingBoost = fill < 0.18f ? 1.12f : (fill < 0.35f ? 1.06f : 1f);
            return baseRate * GetCapacitorRecoveryFactor() * primingBoost;
        }

        public float GetCapacitorRecoveryFactor()
        {
            if (capacitorRecoveryTicksRemaining <= 0 || capacitorRecoveryDurationTicks <= 0)
            {
                return 1f;
            }

            float progress = 1f - Mathf.Clamp01((float)capacitorRecoveryTicksRemaining / capacitorRecoveryDurationTicks);
            return Mathf.Lerp(0.42f, 1f, progress);
        }

        public float GetCapacitorGridSmoothing()
        {
            return AbyssalCircleCapacitorRitualUtility.GetGridSmoothing(this);
        }

        public float GetPendingCapacitorStartupCost()
        {
            return pendingCapacitorStartupCost;
        }

        public float GetPendingCapacitorReserveRequired()
        {
            return pendingCapacitorReserveRequired;
        }

        public float GetPendingCapacitorThroughputRequired()
        {
            return pendingCapacitorThroughputRequired;
        }

        public bool CanInstallCapacitor(ThingDef capacitorDef, AbyssalCircleCapacitorBay bay, out string failReason)
        {
            failReason = null;
            EnsureCapacitorSlotsInitialized();

            if (!Spawned || Destroyed || Map == null)
            {
                failReason = "ABY_CircleConsoleFail_NoCircle".Translate();
                return false;
            }

            if (RitualActive)
            {
                failReason = "ABY_CircleFail_Busy".Translate();
                return false;
            }

            if (!AbyssalCircleCapacitorUtility.IsCapacitorThing(capacitorDef))
            {
                failReason = "ABY_CapacitorFail_NotModule".Translate();
                return false;
            }

            if (!AbyssalCircleCapacitorUtility.AllowsBay(capacitorDef, bay))
            {
                failReason = "ABY_CapacitorFail_BayMismatch".Translate(AbyssalCircleCapacitorUtility.GetBayLabel(bay));
                return false;
            }

            AbyssalCircleCapacitorSlot slot = GetCapacitorSlot(bay);
            if (slot == null)
            {
                failReason = "ABY_CapacitorFail_NoBay".Translate();
                return false;
            }

            if (!slot.IsEmpty)
            {
                failReason = "ABY_CapacitorFail_BayOccupied".Translate(AbyssalCircleCapacitorUtility.GetBayLabel(bay));
                return false;
            }

            return true;
        }

        public bool TryInstallCapacitorDirect(ThingDef capacitorDef, AbyssalCircleCapacitorBay bay, out string failReason)
        {
            if (!CanInstallCapacitor(capacitorDef, bay, out failReason))
            {
                return false;
            }

            AbyssalCircleCapacitorSlot slot = GetCapacitorSlot(bay);
            slot.SetInstalledThingDef(capacitorDef);
            ClampStoredCapacitorCharge();
            MarkCircleVisualsDirty();
            return true;
        }

        public bool CanRemoveInstalledCapacitor(AbyssalCircleCapacitorBay bay, out string failReason)
        {
            failReason = null;
            EnsureCapacitorSlotsInitialized();

            if (!Spawned || Destroyed || Map == null)
            {
                failReason = "ABY_CircleConsoleFail_NoCircle".Translate();
                return false;
            }

            if (RitualActive)
            {
                failReason = "ABY_CircleFail_Busy".Translate();
                return false;
            }

            AbyssalCircleCapacitorSlot slot = GetCapacitorSlot(bay);
            if (slot == null || slot.IsEmpty)
            {
                failReason = "ABY_CapacitorFail_NoInstalled".Translate(AbyssalCircleCapacitorUtility.GetBayLabel(bay));
                return false;
            }

            return true;
        }

        public ThingDef RemoveInstalledCapacitor(AbyssalCircleCapacitorBay bay)
        {
            EnsureCapacitorSlotsInitialized();
            AbyssalCircleCapacitorSlot slot = GetCapacitorSlot(bay);
            ThingDef removedDef = slot?.InstalledThingDef;
            if (removedDef == null)
            {
                return null;
            }

            slot.SetInstalledThingDef(null);
            ClampStoredCapacitorCharge();
            MarkCircleVisualsDirty();
            return removedDef;
        }


        public AbyssalCircleModuleSlot GetModuleSlot(AbyssalCircleModuleEdge edge)
        {
            EnsureModuleSlotsInitialized();
            return AbyssalCircleModuleUtility.GetSlot(moduleSlots, edge);
        }

        public IReadOnlyList<AbyssalCircleModuleSlot> GetModuleSlots()
        {
            EnsureModuleSlotsInitialized();
            return moduleSlots;
        }

        public AbyssalCircleStabilizerBonusSummary GetStabilizerBonusSummary()
        {
            EnsureModuleSlotsInitialized();
            return AbyssalCircleModuleUtility.GetStabilizerBonusSummary(moduleSlots);
        }

        public bool CanInstallModule(ThingDef moduleDef, AbyssalCircleModuleEdge edge, out string failReason)
        {
            failReason = null;
            EnsureModuleSlotsInitialized();
            if (!Spawned || Destroyed || Map == null)
            {
                failReason = "ABY_CircleConsoleFail_NoCircle".Translate();
                return false;
            }
            if (RitualActive)
            {
                failReason = "ABY_CircleFail_Busy".Translate();
                return false;
            }
            if (moduleDef == null)
            {
                failReason = "ABY_CircleModuleFail_NoModuleDef".Translate();
                return false;
            }
            DefModExtension_AbyssalCircleModule ext = AbyssalCircleModuleUtility.GetModuleExtension(moduleDef);
            if (ext == null)
            {
                failReason = "ABY_CircleModuleFail_NotAModule".Translate(moduleDef.LabelCap);
                return false;
            }
            AbyssalCircleModuleSlot slot = AbyssalCircleModuleUtility.GetSlot(moduleSlots, edge);
            if (slot == null)
            {
                failReason = "ABY_CircleModuleFail_NoSlot".Translate(AbyssalCircleModuleUtility.GetEdgeLabel(edge));
                return false;
            }
            if (slot.Occupied)
            {
                failReason = "ABY_CircleModuleFail_SlotOccupied".Translate(AbyssalCircleModuleUtility.GetEdgeLabel(edge));
                return false;
            }
            return true;
        }

        public bool TryInstallModuleDirect(ThingDef moduleDef, AbyssalCircleModuleEdge edge, out string failReason)
        {
            if (!CanInstallModule(moduleDef, edge, out failReason))
            {
                return false;
            }
            AbyssalCircleModuleSlot slot = AbyssalCircleModuleUtility.GetSlot(moduleSlots, edge);
            slot.SetInstalledThingDef(moduleDef);
            MarkCircleVisualsDirty();
            return true;
        }

        public bool CanRemoveInstalledModule(AbyssalCircleModuleEdge edge, out string failReason)
        {
            failReason = null;
            EnsureModuleSlotsInitialized();
            if (!Spawned || Destroyed || Map == null)
            {
                failReason = "ABY_CircleConsoleFail_NoCircle".Translate();
                return false;
            }
            if (RitualActive)
            {
                failReason = "ABY_CircleFail_Busy".Translate();
                return false;
            }
            AbyssalCircleModuleSlot slot = AbyssalCircleModuleUtility.GetSlot(moduleSlots, edge);
            if (slot == null || !slot.Occupied)
            {
                failReason = "ABY_CircleModuleFail_SlotEmpty".Translate(AbyssalCircleModuleUtility.GetEdgeLabel(edge));
                return false;
            }
            return true;
        }

        public ThingDef RemoveInstalledModule(AbyssalCircleModuleEdge edge)
        {
            EnsureModuleSlotsInitialized();
            AbyssalCircleModuleSlot slot = AbyssalCircleModuleUtility.GetSlot(moduleSlots, edge);
            ThingDef removedDef = slot?.InstalledThingDef;
            if (removedDef == null)
            {
                return null;
            }
            slot.Clear();
            MarkCircleVisualsDirty();
            return removedDef;
        }

        private void EnsureModuleSlotsInitialized()
        {
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
        }

        private void EnsureCapacitorSlotsInitialized()
        {
            if (coreCapacitorSlot == null)
            {
                coreCapacitorSlot = new AbyssalCircleCapacitorSlot();
            }

            if (auxiliaryCapacitorSlot == null)
            {
                auxiliaryCapacitorSlot = new AbyssalCircleCapacitorSlot();
            }

            AbyssalCircleCapacitorUtility.EnsureSlot(coreCapacitorSlot, AbyssalCircleCapacitorUtility.CoreBayId, AbyssalCircleCapacitorUtility.CoreBayLabelKey);
            AbyssalCircleCapacitorUtility.EnsureSlot(auxiliaryCapacitorSlot, AbyssalCircleCapacitorUtility.AuxiliaryBayId, AbyssalCircleCapacitorUtility.AuxiliaryBayLabelKey);
        }

        private void TickCapacitorRecovery()
        {
            if (capacitorRecoveryTicksRemaining > 0)
            {
                capacitorRecoveryTicksRemaining--;
            }
        }

        private void TickCapacitorCharge()
        {
            float capacity = GetCapacitorCapacity();
            if (capacity <= 0.01f)
            {
                storedCapacitorCharge = 0f;
                capacitorRecoveryTicksRemaining = 0;
                capacitorRecoveryDurationTicks = 0;
                return;
            }

            ClampStoredCapacitorCharge();

            if (!ShouldDoHashInterval(60))
            {
                return;
            }

            float leakage = GetCapacitorCurrentLeakagePerSecond();
            if (leakage > 0.001f)
            {
                storedCapacitorCharge = Mathf.Max(0f, storedCapacitorCharge - leakage);
            }

            if (!Spawned || Destroyed || Map == null || !IsPoweredForRitual)
            {
                return;
            }

            float rate = GetCapacitorEffectiveChargeRatePerSecond();
            if (rate <= 0.001f)
            {
                return;
            }

            storedCapacitorCharge = Mathf.Clamp(storedCapacitorCharge + rate, 0f, capacity);
        }

        private void TickCapacitorRitualFeed()
        {
            if (!RitualActive || pendingCapacitorSustainDrainPerSecond <= 0.001f)
            {
                return;
            }

            if (!Spawned || Destroyed || Map == null || !IsPoweredForRitual)
            {
                return;
            }

            if (!ShouldDoHashInterval(60))
            {
                return;
            }

            float drain = pendingCapacitorSustainDrainPerSecond;
            float smoothing = GetCapacitorGridSmoothing();
            if (smoothing > 0.001f)
            {
                drain *= 1f - smoothing * 0.08f;
            }

            if (pendingCapacitorForcedStart && capacitorEmergencyDumpEnabled && !pendingCapacitorEmergencyDumpUsed && storedCapacitorCharge <= Mathf.Max(4f, drain * 1.15f))
            {
                TriggerCapacitorEmergencyDump();
                return;
            }

            storedCapacitorCharge = Mathf.Max(0f, storedCapacitorCharge - drain);
            if (pendingCapacitorForcedStart && storedCapacitorCharge <= 0.01f)
            {
                pendingCapacitorBacklashSeverity = Mathf.Clamp01(pendingCapacitorBacklashSeverity + 0.06f);
            }
        }

        private void ClampStoredCapacitorCharge()
        {
            float capacity = GetCapacitorCapacity();
            storedCapacitorCharge = Mathf.Clamp(storedCapacitorCharge, 0f, Mathf.Max(0f, capacity));
        }

        private void BeginCapacitorRecovery()
        {
            float capacity = GetCapacitorCapacity();
            float reserve = Mathf.Max(pendingCapacitorReserveRequired, pendingCapacitorStartupCost);
            if (capacity <= 0.01f || reserve <= 0.01f)
            {
                capacitorRecoveryTicksRemaining = 0;
                capacitorRecoveryDurationTicks = 0;
                return;
            }

            float smoothing = GetCapacitorGridSmoothing();
            float normalized = Mathf.Clamp01(reserve / Mathf.Max(1f, capacity));
            int duration = Mathf.RoundToInt(Mathf.Lerp(240f, 780f, normalized));
            duration = Mathf.RoundToInt(duration * (1f - smoothing * 0.28f));
            if (pendingCapacitorForcedStart)
            {
                duration = Mathf.RoundToInt(duration * AbyssalCircleCapacitorRitualUtility.GetOverchannelRecoveryMultiplier(pendingCapacitorBacklashSeverity, pendingCapacitorEmergencyDumpUsed));
            }

            duration = Mathf.Max(180, duration);
            capacitorRecoveryDurationTicks = duration;
            capacitorRecoveryTicksRemaining = duration;
        }

        private void TriggerCapacitorEmergencyDump()
        {
            if (pendingCapacitorEmergencyDumpUsed || Map == null)
            {
                return;
            }

            pendingCapacitorEmergencyDumpUsed = true;
            pendingCapacitorBacklashSeverity = Mathf.Max(0f, pendingCapacitorBacklashSeverity - AbyssalCircleCapacitorRitualUtility.GetEmergencyDumpMitigation(this));
            pendingCapacitorSustainDrainPerSecond *= 0.60f;
            storedCapacitorCharge = 0f;
            Messages.Message("ABY_CapacitorEmergencyDump_Message".Translate(), MessageTypeDefOf.NeutralEvent, false);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", RitualFocusCell, Map);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.08f);
        }

        private void ResolveCapacitorAftermath()
        {
            if (!pendingCapacitorForcedStart)
            {
                return;
            }

            if (pendingCapacitorEmergencyDumpUsed)
            {
                Messages.Message("ABY_CapacitorEmergencyDump_Aftermath".Translate(), MessageTypeDefOf.NeutralEvent, false);
            }

            float severity = Mathf.Clamp01(pendingCapacitorBacklashSeverity);
            if (severity <= 0.04f)
            {
                return;
            }

            int damage = Mathf.RoundToInt(Mathf.Lerp(4f, 20f, severity));
            if (damage > 0)
            {
                TakeDamage(new DamageInfo(DamageDefOf.Blunt, damage, 0f, -1f, null, null, null));
            }

            if (Map != null)
            {
                Messages.Message("ABY_CapacitorBacklash_Message".Translate(Mathf.RoundToInt(severity * 100f)), MessageTypeDefOf.NegativeEvent, false);
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", RitualFocusCell, Map);
            }
        }

        private void MarkCircleVisualsDirty()
        {
            if (Map == null)
            {
                return;
            }

            Notify_ColorChanged();
            foreach (IntVec3 cell in GenAdj.OccupiedRect(Position, Rotation, def.Size))
            {
                Map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Things);
            }
        }

        public bool IsReadyForSigil(out string failReason)
        {
            failReason = null;

            if (!Spawned || Destroyed || Map == null)
            {
                failReason = "ABY_CircleFail_NotPlaced".Translate();
                return false;
            }

            if (RitualActive)
            {
                failReason = "ABY_CircleFail_Busy".Translate();
                return false;
            }

            if (!IsPoweredForRitual)
            {
                failReason = "ABY_CircleFail_NoPower".Translate();
                return false;
            }

            if (!HasValidInteractionCell(out failReason))
            {
                return false;
            }

            if (!HasClearRitualFocus(out failReason))
            {
                return false;
            }

            if (AbyssalBossSummonUtility.HasActiveAbyssalEncounter(Map))
            {
                failReason = "ABY_BossSummonFail_EncounterActive".Translate();
                return false;
            }

            return true;
        }

        private bool IsAllowedFocusThing(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            CompUseEffect_SummonBoss summonComp = thing.TryGetComp<CompUseEffect_SummonBoss>();
            if (summonComp != null)
            {
                return true;
            }

            return thing.def != null && thing.def.defName == "ABY_ArchonSigil";
        }

        public bool HasValidInteractionCell(out string failReason)
        {
            failReason = null;
            IntVec3 interactionCell = InteractionCell;
            if (!interactionCell.IsValid || !interactionCell.InBounds(Map) || !interactionCell.Standable(Map))
            {
                failReason = "ABY_CircleFail_InteractionBlocked".Translate();
                return false;
            }

            return true;
        }

        public bool HasClearRitualFocus(out string failReason)
        {
            failReason = null;
            IntVec3 focusCell = RitualFocusCell;
            if (!focusCell.IsValid || !focusCell.InBounds(Map))
            {
                failReason = "ABY_CircleFail_FocusBlocked".Translate();
                return false;
            }

            foreach (Thing thing in focusCell.GetThingList(Map))
            {
                if (thing == null || thing == this || thing.Destroyed)
                {
                    continue;
                }

                if (thing is Mote || thing is Filth)
                {
                    continue;
                }

                if (IsAllowedFocusThing(thing))
                {
                    continue;
                }

                failReason = "ABY_CircleFail_FocusBlocked".Translate();
                return false;
            }

            return true;
        }

        private bool ShouldDoHashInterval(int interval)
        {
            if (interval <= 0)
            {
                return true;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int hashOffset = thingIDNumber >= 0 ? thingIDNumber : 0;
            return (ticksGame + hashOffset) % interval == 0;
        }

        private void TickRitualEffects()
        {
            float progress = GetPhaseProgress();
            AbyssalBossScreenFXGameComponent fxComp = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>();

            switch (ritualPhase)
            {
                case RitualPhase.Charging:
                    if (ShouldDoHashInterval(24))
                    {
                        SpawnMinorMote("ABY_Mote_ArchonDashTrail", 0.85f + progress * 0.35f);
                    }

                    if (ShouldDoHashInterval(18))
                    {
                        fxComp?.RegisterRitualPulse(Map, 0.06f + progress * 0.08f);
                    }

                    if (ShouldDoHashInterval(26))
                    {
                        ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", RitualFocusCell, Map);
                    }
                    break;

                case RitualPhase.Surge:
                    if (ShouldDoHashInterval(10))
                    {
                        SpawnMinorMote("ABY_Mote_ArchonDashEntry", 1.05f + progress * 0.45f);
                    }

                    if (ShouldDoHashInterval(8))
                    {
                        fxComp?.RegisterRitualPulse(Map, 0.12f + progress * 0.16f);
                    }

                    if (ShouldDoHashInterval(14))
                    {
                        ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", RitualFocusCell, Map);
                    }
                    break;

                case RitualPhase.Breach:
                    if (ShouldDoHashInterval(5))
                    {
                        SpawnMinorMote("ABY_Mote_ArchonDashExit", 1.50f + progress * 0.75f);
                    }

                    if (ShouldDoHashInterval(4))
                    {
                        fxComp?.RegisterRitualPulse(Map, 0.22f + progress * 0.28f);
                    }
                    break;

                case RitualPhase.Cooldown:
                    if (ShouldDoHashInterval(20))
                    {
                        SpawnMinorMote("ABY_Mote_ArchonDashTrail", 0.70f + (1f - progress) * 0.15f);
                    }
                    break;
            }
        }

        private void AdvancePhase()
        {
            switch (ritualPhase)
            {
                case RitualPhase.Charging:
                    StartPhase(RitualPhase.Surge, 90);
                    SpawnMinorMote("ABY_Mote_ArchonDashEntry", 1.40f);
                    Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.18f);
                    ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", RitualFocusCell, Map);
                    break;

                case RitualPhase.Surge:
                    StartPhase(RitualPhase.Breach, 30);
                    ArchonInfernalVFXUtility.DoSummonVFX(Map, RitualFocusCell);
                    Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.36f);
                    ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", RitualFocusCell, Map);
                    break;

                case RitualPhase.Breach:
                    CompleteSummon();
                    StartPhase(RitualPhase.Cooldown, 120);
                    break;

                case RitualPhase.Cooldown:
                    ResetRitual();
                    break;
            }
        }

        private void CompleteSummon()
        {
            string failReason;

            if (IsImpPortalSummonMode(pendingSummonMode))
            {
                CompleteImpPortalSummon();
                return;
            }

            if (IsPortalWaveSummonMode(pendingSummonMode))
            {
                CompletePortalWaveSummon();
                return;
            }

            if (IsHostilePackSummonMode(pendingSummonMode))
            {
                CompleteHostilePackSummon();
                return;
            }

            if (ShouldUseArchonBeastPortalEscortEncounter())
            {
                if (!AbyssalArchonBeastPortalEncounterUtility.TryBeginEncounter(
                        Map,
                        pendingFaction,
                        pendingPawnKindDef,
                        pendingBossLabel,
                        pendingSpawnCell,
                        out IntVec3 bossPortalCell,
                        out failReason))
                {
                    ResetRitual();
                    Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                    return;
                }

                ApplyRitualInstability();
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.24f);
                if (bossPortalCell.IsValid)
                {
                    ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", bossPortalCell, Map);
                }
                return;
            }

            if (!AbyssalBossSummonUtility.TryGenerateBoss(
                    Map,
                    pendingPawnKindDef,
                    pendingFaction,
                    pendingBossLabel,
                    out Pawn pawn,
                    out failReason))
            {
                ResetRitual();
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            AbyssalBossSummonUtility.FinalizeBossArrival(
                pawn,
                pendingFaction,
                Map,
                pendingSpawnCell,
                pendingBossLabel);
            ApplyRitualInstability();
        }

        private void CompleteHostilePackSummon()
        {
            string failReason;

            if (Map == null)
            {
                ResetRitual();
                return;
            }

            if (pendingFaction == null)
            {
                pendingFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            }

            if (pendingFaction == null)
            {
                ResetRitual();
                Messages.Message("ABY_CircleFail_NoHostileFaction".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (pendingPawnKindDef == null)
            {
                ResetRitual();
                Messages.Message("Missing PawnKindDef for hostile pack summon.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!AbyssalHostileSummonUtility.TrySpawnHostilePack(
                    Map,
                    pendingPawnKindDef,
                    pendingFaction,
                    pendingSpawnCell,
                    Mathf.Max(1, pendingImpCount),
                    pendingBossLabel,
                    GetCompletionLetterLabel(),
                    GetCompletionLetterDesc(),
                    out IntVec3 arrivalCell,
                    out failReason))
            {
                ResetRitual();
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            ApplyRitualInstability();
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.16f);
            if (arrivalCell.IsValid)
            {
                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", arrivalCell, Map);
            }
        }

        private void CompletePortalWaveSummon()
        {
            string failReason;

            if (Map == null)
            {
                ResetRitual();
                return;
            }

            if (pendingFaction == null)
            {
                pendingFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            }

            if (pendingFaction == null)
            {
                ResetRitual();
                Messages.Message("ABY_CircleFail_NoHostileFaction".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            MapComponent_AbyssalPortalWave portalWave = Map.GetComponent<MapComponent_AbyssalPortalWave>();
            if (portalWave == null)
            {
                ResetRitual();
                Messages.Message("ABY_CircleFail_NoPortalSpawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!portalWave.TryBeginEmberPortalWave(
                    pendingFaction,
                    pendingPawnKindDef,
                    pendingImpPortalWarmupTicks,
                    pendingImpSpawnIntervalTicks,
                    pendingImpPortalLingerTicks,
                    out IntVec3 firstPortalCell,
                    out failReason))
            {
                ResetRitual();
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            IntVec3 letterCell = firstPortalCell.IsValid ? firstPortalCell : RitualFocusCell;
            ApplyRitualInstability();
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.18f);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", letterCell, Map);
            Find.LetterStack.ReceiveLetter(
                GetCompletionLetterLabel(),
                GetCompletionLetterDesc(),
                LetterDefOf.ThreatSmall,
                new TargetInfo(letterCell, Map));
        }

        private void CompleteImpPortalSummon()
        {
            if (Map == null)
            {
                ResetRitual();
                return;
            }

            if (pendingFaction == null)
            {
                pendingFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            }

            if (pendingFaction == null)
            {
                ResetRitual();
                Messages.Message("ABY_CircleFail_NoHostileFaction".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            bool spawned = ABY_Phase2PortalUtility.TrySpawnImpPortalNear(
                Map,
                pendingFaction,
                pendingSpawnCell.IsValid ? pendingSpawnCell : RitualFocusCell,
                5.9f,
                14.9f,
                Mathf.Max(1, pendingImpCount),
                Mathf.Max(30, pendingImpPortalWarmupTicks),
                Mathf.Max(30, pendingImpSpawnIntervalTicks),
                Mathf.Max(600, pendingImpPortalLingerTicks),
                out Building_AbyssalImpPortal portal);

            if (!spawned)
            {
                spawned = ABY_Phase2PortalUtility.TrySpawnImpPortal(
                    Map,
                    pendingFaction,
                    Mathf.Max(1, pendingImpCount),
                    Mathf.Max(30, pendingImpPortalWarmupTicks),
                    Mathf.Max(30, pendingImpSpawnIntervalTicks),
                    Mathf.Max(600, pendingImpPortalLingerTicks),
                    out portal);
            }

            if (!spawned || portal == null)
            {
                ResetRitual();
                Messages.Message("ABY_CircleFail_NoPortalSpawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            ApplyRitualInstability();
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.18f);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", portal.Position, Map);
            Find.LetterStack.ReceiveLetter(
                GetCompletionLetterLabel(),
                GetCompletionLetterDesc(),
                LetterDefOf.ThreatSmall,
                new TargetInfo(portal.Position, Map));
        }

        private bool IsImpPortalSummonMode(string summonMode)
        {
            return string.Equals(summonMode, "ImpPortal", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsHostilePackSummonMode(string summonMode)
        {
            return string.Equals(summonMode, "HostilePack", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPortalWaveSummonMode(string summonMode)
        {
            return string.Equals(summonMode, "PortalWave", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldUseArchonBeastPortalEscortEncounter()
        {
            if (pendingPawnKindDef?.defName != "ABY_ArchonBeast")
            {
                return false;
            }

            return string.Equals(pendingRitualId, "archon_beast", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(pendingSummonMode, "Boss", System.StringComparison.OrdinalIgnoreCase);
        }

        private string GetCompletionLetterLabel()
        {
            if (!pendingCompletionLetterLabelKey.NullOrEmpty())
            {
                string translated = pendingCompletionLetterLabelKey.Translate();
                if (translated != pendingCompletionLetterLabelKey)
                {
                    return translated;
                }
            }

            return "ABY_BossSummonSuccessLabel".Translate();
        }

        private string GetCompletionLetterDesc()
        {
            if (!pendingCompletionLetterDescKey.NullOrEmpty())
            {
                string translated = pendingCompletionLetterDescKey.Translate(pendingImpCount);
                if (translated != pendingCompletionLetterDescKey)
                {
                    return translated;
                }
            }

            return "ABY_BossSummonSuccessDesc".Translate(pendingBossLabel ?? "breach");
        }

        private void TickInstabilityState()
        {
            if (Map == null || Destroyed)
            {
                instabilityHeat = 0f;
                return;
            }

            if (!ShouldDoHashInterval(AbyssalCircleInstabilityUtility.HeatTickInterval))
            {
                return;
            }

            float nextHeat = instabilityHeat;
            if (RitualActive)
            {
                nextHeat += AbyssalCircleInstabilityUtility.GetActivePhasePressure(CurrentRitualPhase) * 0.18f;
            }
            else
            {
                nextHeat -= AbyssalCircleInstabilityUtility.GetIdleDecayPerTick(this) * AbyssalCircleInstabilityUtility.HeatTickInterval;
            }

            instabilityHeat = Mathf.Clamp01(nextHeat);

            if (!RitualActive)
            {
                float ambientBleed = AbyssalCircleInstabilityUtility.GetAmbientBleedAmount(this);
                if (ambientBleed > 0.001f && ShouldDoHashInterval(AbyssalCircleInstabilityUtility.AmbientBleedInterval))
                {
                    Map.GetComponent<MapComponent_AbyssalCircleInstability>()?.AddContamination(ambientBleed);
                }
            }
        }

        private void ApplyRitualInstability()
        {
            AbyssalSummoningConsoleUtility.RitualDefinition ritual = GetPendingRitualDefinition();
            if (ritual == null)
            {
                return;
            }

            instabilityHeat = Mathf.Clamp01(instabilityHeat + AbyssalCircleInstabilityUtility.GetProjectedHeatGain(this, ritual));
            float contaminationGain = AbyssalCircleInstabilityUtility.GetProjectedContaminationGain(this, ritual);
            if (contaminationGain > 0.001f)
            {
                Map?.GetComponent<MapComponent_AbyssalCircleInstability>()?.AddContamination(contaminationGain);
            }
        }

        private AbyssalSummoningConsoleUtility.RitualDefinition GetPendingRitualDefinition()
        {
            if (pendingRitualId.NullOrEmpty())
            {
                return null;
            }

            foreach (AbyssalSummoningConsoleUtility.RitualDefinition ritual in AbyssalSummoningConsoleUtility.GetRituals())
            {
                if (ritual != null && ritual.Id == pendingRitualId)
                {
                    return ritual;
                }
            }

            return null;
        }

        private void StartPhase(RitualPhase phase, int duration)
        {
            ritualPhase = phase;
            phaseDuration = duration;
            phaseTicksRemaining = duration;
        }

        private void ResetRitual()
        {
            ResolveCapacitorAftermath();
            BeginCapacitorRecovery();
            ritualPhase = RitualPhase.Idle;
            phaseTicksRemaining = 0;
            phaseDuration = 0;
            pendingPawnKindDef = null;
            pendingBossLabel = null;
            pendingRitualId = null;
            pendingSummonMode = null;
            pendingCompletionLetterLabelKey = null;
            pendingCompletionLetterDescKey = null;
            pendingFaction = null;
            pendingSpawnCell = IntVec3.Invalid;
            pendingImpCount = 0;
            pendingImpPortalWarmupTicks = 0;
            pendingImpSpawnIntervalTicks = 0;
            pendingImpPortalLingerTicks = 0;
            pendingCapacitorStartupCost = 0f;
            pendingCapacitorSustainDrainPerSecond = 0f;
            pendingCapacitorReserveRequired = 0f;
            pendingCapacitorThroughputRequired = 0f;
            pendingCapacitorForcedStart = false;
            pendingCapacitorEmergencyDumpUsed = false;
            pendingCapacitorBacklashSeverity = 0f;
        }

        private float GetRitualIntensity()
        {
            float progress = GetPhaseProgress();

            switch (ritualPhase)
            {
                case RitualPhase.Charging:
                    return 0.18f + progress * 0.42f;
                case RitualPhase.Surge:
                    return 0.62f + progress * 0.22f;
                case RitualPhase.Breach:
                    return 0.92f + progress * 0.08f;
                case RitualPhase.Cooldown:
                    return 0.45f * (1f - progress);
                default:
                    return 0f;
            }
        }

        private float GetPhaseProgress()
        {
            if (phaseDuration <= 0)
            {
                return 1f;
            }

            return 1f - Mathf.Clamp01((float)phaseTicksRemaining / phaseDuration);
        }

        private string GetPhaseLabel()
        {
            switch (ritualPhase)
            {
                case RitualPhase.Charging:
                    return "ABY_CirclePhase_Charging";
                case RitualPhase.Surge:
                    return "ABY_CirclePhase_Surge";
                case RitualPhase.Breach:
                    return "ABY_CirclePhase_Breach";
                case RitualPhase.Cooldown:
                    return "ABY_CirclePhase_Cooldown";
                default:
                    return "ABY_CirclePhase_Idle";
            }
        }

        private void SpawnMinorMote(string defName, float scale)
        {
            if (Map == null || defName.NullOrEmpty())
            {
                return;
            }

            ThingDef moteDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (moteDef == null)
            {
                return;
            }

            Vector3 pos = RitualFocusCell.ToVector3Shifted();
            float tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0f;
            float angle = tick * 0.08f + ritualSeed * 0.017f;
            float radius = ritualPhase == RitualPhase.Breach ? 1.15f : 0.55f;
            pos.x += Mathf.Sin(angle) * radius;
            pos.z += Mathf.Cos(angle) * radius;
            MoteMaker.MakeStaticMote(pos, Map, moteDef, scale);
        }

        public string GetCurrentPhaseTranslated()
        {
            return GetPhaseLabel().Translate();
        }

        public string GetCurrentStatusLine()
        {
            string failReason;

            if (RitualActive)
            {
                return AbyssalSummoningConsoleUtility.GetPhaseText(GetCurrentPhaseTranslated(), Mathf.RoundToInt(GetPhaseProgress() * 100f));
            }

            if (IsReadyForSigil(out failReason))
            {
                return AbyssalSummoningConsoleUtility.GetReadyText();
            }

            return AbyssalSummoningConsoleUtility.GetNotReadyText(failReason);
        }

        private static void DrawLayer(Graphic graphic, Vector3 center, Vector2 drawSize, float angle, float yOffset)
        {
            if (graphic == null)
            {
                return;
            }

            Vector3 drawPos = center;
            drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + yOffset;

            Matrix4x4 matrix = default;
            matrix.SetTRS(
                drawPos,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(drawSize.x, 1f, drawSize.y));

            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }
    }
}
