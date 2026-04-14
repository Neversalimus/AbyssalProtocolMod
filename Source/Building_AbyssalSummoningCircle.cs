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

        private static readonly Vector2 OuterRingSize = new Vector2(9.38f, 9.38f);
        private static readonly Vector2 InnerGlyphSize = new Vector2(8.72f, 8.72f);
        private static readonly Vector2 EnergyArcsSize = new Vector2(8.36f, 8.36f);
        private static readonly Vector2 DataLatticeSize = new Vector2(7.52f, 7.52f);
        private static readonly Vector2 CoreGlowSize = new Vector2(2.84f, 2.84f);
        private static readonly Vector2 IdleGlowSize = new Vector2(9.20f, 9.20f);
        private static readonly Vector2 BreachSize = new Vector2(6.90f, 6.90f);
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
        private float instabilityHeat;
        private float cachedContainment;
        private int nextContainmentRefreshTick;
        private int lastInstabilityEventTick = -999999;
        private int purgeCooldownUntilTick;
        private int nextAmbientBleedTick;
        private List<AbyssalCircleModuleSlot> moduleSlots = new List<AbyssalCircleModuleSlot>();

        public bool RitualActive => ritualPhase != RitualPhase.Idle;
        public bool IsPoweredForRitual => GetComp<CompPowerTrader>()?.PowerOn ?? true;
        public IntVec3 RitualFocusCell => GenAdj.OccupiedRect(Position, Rotation, def.Size).CenterCell;
        public bool ReducedConsoleEffects => reducedConsoleEffects;
        public float RitualProgress => RitualActive ? GetPhaseProgress() : 0f;
        public float InstabilityHeat => instabilityHeat;
        public float RawContainmentRating => cachedContainment;
        public float ResidualContamination => GetInstabilityMapComponent()?.ResidualContamination ?? 0f;
        public int PurgeCooldownTicksRemaining
        {
            get
            {
                int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                return Mathf.Max(0, purgeCooldownUntilTick - ticksGame);
            }
        }

        public int VentCooldownTicksRemaining => GetInstabilityMapComponent()?.TicksUntilVentReady ?? 0;

        public float ContainmentRating
        {
            get
            {
                RefreshContainmentCache(false);
                return AbyssalCircleInstabilityUtility.GetEffectiveContainment(this, cachedContainment);
            }
        }

        public IReadOnlyList<AbyssalCircleModuleSlot> ModuleSlots => moduleSlots;
        public int InstalledModuleCount => AbyssalCircleModuleUtility.CountInstalledModules(moduleSlots);
        public int InstalledStabilizerCount => AbyssalCircleModuleUtility.CountInstalledModules(moduleSlots, DefModExtension_AbyssalCircleModule.StabilizerFamily);

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
            Scribe_Values.Look(ref instabilityHeat, "instabilityHeat", 0f);
            Scribe_Values.Look(ref cachedContainment, "cachedContainment", 0f);
            Scribe_Values.Look(ref nextContainmentRefreshTick, "nextContainmentRefreshTick", 0);
            Scribe_Values.Look(ref lastInstabilityEventTick, "lastInstabilityEventTick", -999999);
            Scribe_Values.Look(ref purgeCooldownUntilTick, "purgeCooldownUntilTick", 0);
            Scribe_Values.Look(ref nextAmbientBleedTick, "nextAmbientBleedTick", 0);
            Scribe_Collections.Look(ref moduleSlots, "moduleSlots", LookMode.Deep);
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
        }

        public void SetReducedConsoleEffects(bool value)
        {
            reducedConsoleEffects = value;
        }

        public override void PostMake()
        {
            base.PostMake();
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
            RefreshContainmentCache(true);
        }

        public AbyssalCircleModuleSlot GetModuleSlot(AbyssalCircleModuleEdge edge)
        {
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
            return AbyssalCircleModuleUtility.GetSlot(moduleSlots, edge);
        }

        public AbyssalCircleStabilizerBonusSummary GetStabilizerBonusSummary()
        {
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
            return AbyssalCircleModuleUtility.GetStabilizerBonusSummary(moduleSlots);
        }

        public bool CanInstallModule(ThingDef moduleDef, AbyssalCircleModuleEdge edge, out string failReason)
        {
            failReason = null;
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);

            if (Destroyed || Map == null || !Spawned)
            {
                failReason = "ABY_CircleConsoleFail_NoCircle".Translate();
                return false;
            }

            if (RitualActive)
            {
                failReason = "ABY_CircleModuleFail_Busy".Translate();
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
            NotifyModuleConfigurationChanged();
            return true;
        }

        public bool CanRemoveInstalledModule(AbyssalCircleModuleEdge edge, out string failReason)
        {
            failReason = null;
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);

            if (Destroyed || Map == null || !Spawned)
            {
                failReason = "ABY_CircleConsoleFail_NoCircle".Translate();
                return false;
            }

            if (RitualActive)
            {
                failReason = "ABY_CircleModuleFail_Busy".Translate();
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
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
            AbyssalCircleModuleSlot slot = AbyssalCircleModuleUtility.GetSlot(moduleSlots, edge);
            if (slot == null || !slot.Occupied)
            {
                return null;
            }

            ThingDef removed = slot.InstalledThingDef;
            slot.Clear();
            NotifyModuleConfigurationChanged();
            return removed;
        }

        public void NotifyModuleConfigurationChanged()
        {
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
            RefreshContainmentCache(true);
            nextAmbientBleedTick = Find.TickManager != null ? Find.TickManager.TicksGame + AbyssalCircleInstabilityUtility.AmbientBleedInterval : 0;
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

            if (Map == null)
            {
                return;
            }

            TickInstabilityPassive();

            if (!RitualActive)
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

                if (!AbyssalBossSummonUtility.TryFindBossArrivalCell(Map, out pendingSpawnCell))
                {
                    failReason = "ABY_CircleFail_NoBossArrival".Translate();
                    return false;
                }
            }

            ritualSeed = thingIDNumber * 397 ^ Find.TickManager.TicksGame;
            ApplyRitualHeat(AbyssalSummoningConsoleUtility.GetRitualById(summonProps.ritualId));

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

            DrawModuleVisuals(center, ritualIntensity, ticks, seedPhase);
        }

        private void DrawModuleVisuals(Vector3 center, float ritualIntensity, int ticks, float seedPhase)
        {
            moduleSlots = AbyssalCircleModuleUtility.EnsureSlots(moduleSlots);
            Graphic socketGraphic = AbyssalCircleModuleUtility.GetSocketGraphic();
            if (socketGraphic == null)
            {
                return;
            }

            float socketPulse = 1f + (IsPoweredForRitual ? ritualIntensity * 0.025f : -0.015f);
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
                Graphic mountedGraphic = AbyssalCircleModuleUtility.GetMountedGraphic(installedDef);
                if (mountedGraphic == null)
                {
                    continue;
                }

                float microPulse = 1f + Mathf.Sin(ticks * 0.055f + seedPhase + (int)edge * 0.9f) * 0.012f;
                float activeScale = 1f + ritualIntensity * 0.040f;
                float drawScale = AbyssalCircleModuleUtility.GetMountedDrawScale(installedDef) * microPulse * activeScale;

                DrawLayer(mountedGraphic, moduleCenter, new Vector2(drawScale, drawScale), angle, yBase + 0.0012f);
            }
        }

        public override string GetInspectString()
        {
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
            else if (IsReadyForSigil(out string failReason))
            {
                sb.Append(AbyssalSummoningConsoleUtility.GetReadyText());
            }
            else
            {
                sb.Append(AbyssalSummoningConsoleUtility.GetNotReadyText(failReason));
            }

            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetInspectSigilsText(AbyssalSummoningConsoleUtility.CountSigilsOnMap(Map, AbyssalSummoningConsoleUtility.GetDefaultRitual())));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetInspectReadinessText(AbyssalSummoningConsoleUtility.GetShortRequirementSummary(this, AbyssalSummoningConsoleUtility.GetDefaultRitual())));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetInspectRiskText(AbyssalSummoningConsoleUtility.GetRiskLabel(AbyssalSummoningConsoleUtility.GetRiskTier(this, AbyssalSummoningConsoleUtility.GetDefaultRitual()))));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetInspectHeatText(AbyssalSummoningConsoleUtility.GetHeatDisplay(this)));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetInspectContainmentText(AbyssalSummoningConsoleUtility.GetContainmentDisplay(this)));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetInspectContaminationText(AbyssalSummoningConsoleUtility.GetContaminationDisplay(this)));
            sb.AppendLine();
            sb.Append("ABY_CircleInspect_Stabilizers".Translate(InstalledStabilizerCount, moduleSlots.Count));
            sb.AppendLine();
            sb.Append(AbyssalSummoningConsoleUtility.GetStabilizerPatternSummary(this));

            return sb.ToString();
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

                if (thing.def != null && thing.def.defName == "ABY_ArchonSigil")
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
            if (IsImpPortalSummonMode(pendingSummonMode))
            {
                CompleteImpPortalSummon();
                return;
            }

            if (!AbyssalBossSummonUtility.TryGenerateBoss(
                    Map,
                    pendingPawnKindDef,
                    pendingFaction,
                    pendingBossLabel,
                    out Pawn pawn,
                    out string failReason))
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

            RegisterRitualCompletion(AbyssalSummoningConsoleUtility.GetRitualById(pendingRitualId));
            TryTriggerInstabilityEvent();
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

            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.18f);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", portal.Position, Map);
            Find.LetterStack.ReceiveLetter(
                GetCompletionLetterLabel(),
                GetCompletionLetterDesc(),
                LetterDefOf.ThreatSmall,
                new TargetInfo(portal.Position, Map));

            RegisterRitualCompletion(AbyssalSummoningConsoleUtility.GetRitualById(pendingRitualId));
            TryTriggerInstabilityEvent();
        }

        private bool IsImpPortalSummonMode(string summonMode)
        {
            return string.Equals(summonMode, "ImpPortal", System.StringComparison.OrdinalIgnoreCase);
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

        private void StartPhase(RitualPhase phase, int duration)
        {
            ritualPhase = phase;
            phaseDuration = duration;
            phaseTicksRemaining = duration;
        }

        public float GetProjectedInstabilityHeat(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            return AbyssalCircleInstabilityUtility.GetProjectedPostInvokeHeat(this, ritual);
        }

        private void TickInstabilityPassive()
        {
            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (!ShouldDoHashInterval(AbyssalCircleInstabilityUtility.HeatTickInterval))
            {
                return;
            }

            RefreshContainmentCache(false);

            if (instabilityHeat > 0f)
            {
                float decay = ritualPhase == RitualPhase.Cooldown
                    ? AbyssalCircleInstabilityUtility.GetCooldownDecayPerTick(this)
                    : RitualActive
                        ? 0f
                        : AbyssalCircleInstabilityUtility.GetIdleDecayPerTick(this);

                if (decay > 0f)
                {
                    instabilityHeat = Mathf.Max(0f, instabilityHeat - decay);
                }
            }
            else
            {
                instabilityHeat = 0f;
            }

            if (!RitualActive && ticksGame >= nextAmbientBleedTick)
            {
                float bleedAmount = AbyssalCircleInstabilityUtility.GetAmbientBleedAmount(this);
                if (bleedAmount > 0f)
                {
                    GetInstabilityMapComponent()?.AddContamination(bleedAmount);
                }

                nextAmbientBleedTick = ticksGame + AbyssalCircleInstabilityUtility.AmbientBleedInterval;
            }
        }

        private void RefreshContainmentCache(bool force)
        {
            if (Map == null)
            {
                cachedContainment = 0f;
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (!force && ticksGame < nextContainmentRefreshTick)
            {
                return;
            }

            cachedContainment = AbyssalCircleInstabilityUtility.CalculateContainment(this);
            nextContainmentRefreshTick = ticksGame + AbyssalCircleInstabilityUtility.ContainmentRefreshInterval;
        }

        private void ApplyRitualHeat(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            RefreshContainmentCache(true);
            instabilityHeat = Mathf.Clamp01(instabilityHeat + AbyssalCircleInstabilityUtility.GetProjectedHeatGain(this, ritual));
        }

        private void RegisterRitualCompletion(AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            MapComponent_AbyssalCircleInstability instability = GetInstabilityMapComponent();
            if (instability == null)
            {
                return;
            }

            instability.AddContamination(AbyssalCircleInstabilityUtility.GetProjectedContaminationGain(this, ritual));
            nextAmbientBleedTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + AbyssalCircleInstabilityUtility.AmbientBleedInterval;
        }

        public bool CanPurgeHeat(out string failReason)
        {
            failReason = null;
            if (Map == null || Destroyed)
            {
                failReason = "ABY_CirclePurgeFail_NoCircle".Translate();
                return false;
            }

            if (RitualActive)
            {
                failReason = "ABY_CirclePurgeFail_Busy".Translate();
                return false;
            }

            if (!IsPoweredForRitual)
            {
                failReason = "ABY_CircleFail_NoPower".Translate();
                return false;
            }

            if (instabilityHeat < 0.08f)
            {
                failReason = "ABY_CirclePurgeFail_LowHeat".Translate();
                return false;
            }

            if (PurgeCooldownTicksRemaining > 0)
            {
                failReason = "ABY_CirclePurgeFail_Cooldown".Translate(AbyssalSummoningConsoleUtility.FormatTicksShort(PurgeCooldownTicksRemaining));
                return false;
            }

            return true;
        }

        public bool TryPurgeHeat(out string failReason)
        {
            if (!CanPurgeHeat(out failReason))
            {
                return false;
            }

            float removed = Mathf.Min(instabilityHeat, AbyssalCircleInstabilityUtility.GetPurgeRemovedHeat(this));
            instabilityHeat = Mathf.Max(0f, instabilityHeat - removed);
            purgeCooldownUntilTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + AbyssalCircleInstabilityUtility.PurgeCooldownTicks;
            GetInstabilityMapComponent()?.AddContamination(AbyssalCircleInstabilityUtility.GetPurgeBackwash(removed));
            RefreshContainmentCache(true);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.08f);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", RitualFocusCell, Map);
            Messages.Message("ABY_CirclePurgeSuccess".Translate(Mathf.RoundToInt(removed * 100f)), this, MessageTypeDefOf.TaskCompletion, false);
            failReason = null;
            return true;
        }

        public bool CanVentContamination(out string failReason)
        {
            MapComponent_AbyssalCircleInstability instability = GetInstabilityMapComponent();
            if (instability == null)
            {
                failReason = "ABY_CircleVentFail_NoCircle".Translate();
                return false;
            }

            return instability.CanVent(this, out failReason);
        }

        public bool TryVentContamination(out string failReason)
        {
            MapComponent_AbyssalCircleInstability instability = GetInstabilityMapComponent();
            if (instability == null)
            {
                failReason = "ABY_CircleVentFail_NoCircle".Translate();
                return false;
            }

            if (!instability.TryVent(this, out float removed, out failReason))
            {
                return false;
            }

            instabilityHeat = Mathf.Clamp01(instabilityHeat + AbyssalCircleInstabilityUtility.GetVentHeatKick(this));
            RefreshContainmentCache(true);
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.11f);
            ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", RitualFocusCell, Map);
            Messages.Message("ABY_CircleVentSuccess".Translate(Mathf.RoundToInt(removed * 100f)), this, MessageTypeDefOf.TaskCompletion, false);
            return true;
        }

        private MapComponent_AbyssalCircleInstability GetInstabilityMapComponent()
        {
            return Map?.GetComponent<MapComponent_AbyssalCircleInstability>();
        }

        private void TryTriggerInstabilityEvent()
        {
            if (Map == null)
            {
                return;
            }

            int ticksGame = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (ticksGame - lastInstabilityEventTick < 2500)
            {
                return;
            }

            float contamination = ResidualContamination;
            float chance = 0f;
            if (instabilityHeat + contamination * 0.35f >= 0.82f)
            {
                chance = 0.40f + contamination * 0.10f;
            }
            else if (instabilityHeat + contamination * 0.25f >= 0.58f)
            {
                chance = 0.22f + contamination * 0.08f;
            }

            chance *= AbyssalCircleInstabilityUtility.GetInstabilityEventChanceMultiplier(this);
            if (chance <= 0f || !Rand.Chance(chance))
            {
                return;
            }

            lastInstabilityEventTick = ticksGame;
            if (instabilityHeat >= 0.82f && Rand.Chance(0.45f) && TrySpawnInstabilityImpSpill())
            {
                instabilityHeat = Mathf.Clamp01(instabilityHeat + 0.05f);
                return;
            }

            DoContainmentLash();
        }

        private void DoContainmentLash()
        {
            float damageAmount = Mathf.Lerp(8f, 18f, Mathf.InverseLerp(0.58f, 1f, instabilityHeat + ResidualContamination * 0.20f));
            damageAmount *= AbyssalCircleInstabilityUtility.GetInstabilityEventSeverityMultiplier(this);
            TakeDamage(new DamageInfo(DamageDefOf.Flame, damageAmount));
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(Map, 0.10f);
            ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", RitualFocusCell, Map);
            Messages.Message("ABY_CircleInstability_Lash".Translate(Mathf.RoundToInt(damageAmount)), this, MessageTypeDefOf.NegativeHealthEvent, false);
        }

        private bool TrySpawnInstabilityImpSpill()
        {
            PawnKindDef impKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail("ABY_RiftImp");
            Faction faction = pendingFaction ?? AbyssalBossSummonUtility.ResolveHostileFaction();
            if (impKindDef == null || faction == null)
            {
                return false;
            }

            int count = instabilityHeat >= 0.92f ? 2 : 1;
            List<Pawn> spawned = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                if (!CellFinder.TryFindRandomCellNear(RitualFocusCell, Map, 6, c => c.Standable(Map) && !c.Fogged(Map), out IntVec3 cell))
                {
                    continue;
                }

                if (!ABY_Phase2PortalUtility.TryGenerateImp(impKindDef, faction, Map, out Pawn imp))
                {
                    continue;
                }

                GenSpawn.Spawn(imp, cell, Map, Rot4.Random);
                spawned.Add(imp);
                ABY_Phase2PortalUtility.GiveAssaultLord(imp);
            }

            if (spawned.Count == 0)
            {
                return false;
            }

            ArchonInfernalVFXUtility.DoSummonVFX(Map, spawned[0].Position);
            ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", spawned[0].Position, Map);
            Messages.Message("ABY_CircleInstability_ImpSpill".Translate(spawned.Count), this, MessageTypeDefOf.ThreatSmall, false);
            return true;
        }

        private void ResetRitual()
        {
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
            if (RitualActive)
            {
                return AbyssalSummoningConsoleUtility.GetPhaseText(GetCurrentPhaseTranslated(), Mathf.RoundToInt(GetPhaseProgress() * 100f));
            }

            if (IsReadyForSigil(out string failReason))
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
