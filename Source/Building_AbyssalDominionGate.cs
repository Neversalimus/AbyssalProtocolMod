using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public class Building_AbyssalDominionGate : Building
    {
        private struct ScoredCell
        {
            public IntVec3 Cell;
            public float Score;
        }

        private static readonly string[] CoreFrameTexPaths =
        {
            "Things/Building/DominionGate/ABY_DominionGate_Core_Frame0",
            "Things/Building/DominionGate/ABY_DominionGate_Core_Frame1",
            "Things/Building/DominionGate/ABY_DominionGate_Core_Frame2",
            "Things/Building/DominionGate/ABY_DominionGate_Core_Frame3"
        };

        private static readonly string[] CoreGlowFrameTexPaths =
        {
            "Things/Building/DominionGate/ABY_DominionGate_Core_Glow_Frame0",
            "Things/Building/DominionGate/ABY_DominionGate_Core_Glow_Frame1",
            "Things/Building/DominionGate/ABY_DominionGate_Core_Glow_Frame2",
            "Things/Building/DominionGate/ABY_DominionGate_Core_Glow_Frame3"
        };

        private static readonly string[] ExitFrameTexPaths =
        {
            "Things/Building/DominionGate/ABY_DominionGate_Ring_Frame0",
            "Things/Building/DominionGate/ABY_DominionGate_Ring_Frame1",
            "Things/Building/DominionGate/ABY_DominionGate_Ring_Frame2",
            "Things/Building/DominionGate/ABY_DominionGate_Ring_Frame3"
        };

        private static readonly Texture2D JumpCommandIcon = ContentFinder<Texture2D>.Get(CoreFrameTexPaths[0], true);
        private static readonly Texture2D ExtractCommandIcon = ContentFinder<Texture2D>.Get(ExitFrameTexPaths[0], true);

        private int nextSuppressionTick = -1;
        private int nextIgnitionTick = -1;
        private int nextCallTick = -1;
        private int pulseSeed;

        public int TicksUntilNextPulse
        {
            get
            {
                if (Find.TickManager == null)
                {
                    return 0;
                }

                int now = Find.TickManager.TicksGame;
                int next = GetNextPulseTick();
                if (next <= 0)
                {
                    return 0;
                }

                return Mathf.Max(0, next - now);
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ABY_LargeModpackHotfixBUtility.EnsureDominionGateFriendly(this);

            if (pulseSeed == 0)
            {
                pulseSeed = thingIDNumber >= 0 ? thingIDNumber : Rand.Range(1, 1000000);
            }

            if (!respawningAfterLoad && Find.TickManager != null)
            {
                int now = Find.TickManager.TicksGame;
                nextSuppressionTick = now + Rand.RangeInclusive(150, 240);
                nextIgnitionTick = now + Rand.RangeInclusive(210, 320);
                nextCallTick = now + Rand.RangeInclusive(480, 720);
            }

            map?.GetComponent<MapComponent_DominionCrisis>()?.RegisterGate(this);
        }

        public override AcceptanceReport ClaimableBy(Faction by)
        {
            return false;
        }

        public override AcceptanceReport DeconstructibleBy(Faction faction)
        {
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextSuppressionTick, "nextSuppressionTick", -1);
            Scribe_Values.Look(ref nextIgnitionTick, "nextIgnitionTick", -1);
            Scribe_Values.Look(ref nextCallTick, "nextCallTick", -1);
            Scribe_Values.Look(ref pulseSeed, "pulseSeed", 0);
        }

        protected override void Tick()
        {
            base.Tick();

            if (Destroyed || Map == null || Find.TickManager == null)
            {
                return;
            }

            ABY_LargeModpackHotfixBUtility.EnsureDominionGateFriendly(this);

            MapComponent_DominionCrisis crisis = Map.GetComponent<MapComponent_DominionCrisis>();
            if (crisis == null || !(crisis.IsGatePhaseActive || crisis.IsStandbyPhaseActive) || !crisis.IsRegisteredGate(this))
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (nextSuppressionTick <= 0)
            {
                nextSuppressionTick = now + 180;
            }
            if (nextIgnitionTick <= 0)
            {
                nextIgnitionTick = now + 240;
            }
            if (nextCallTick <= 0)
            {
                nextCallTick = now + 600;
            }

            if (now >= nextSuppressionTick)
            {
                nextSuppressionTick = now + GetSuppressionInterval();
                ExecuteSuppressionPulse(crisis);
            }

            if (now >= nextIgnitionTick)
            {
                nextIgnitionTick = now + GetIgnitionInterval();
                ExecuteRiftIgnition(crisis);
            }

            if (now >= nextCallTick)
            {
                nextCallTick = now + GetCallInterval();
                ExecuteCallOfDominion(crisis);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            MapComponent_DominionCrisis crisis = Map?.GetComponent<MapComponent_DominionCrisis>();
            if (crisis == null)
            {
                yield break;
            }

            if (crisis.HasActivePocketSession())
            {
                yield return new Command_Action
                {
                    defaultLabel = "ABY_DominionPocketCommand_Jump".Translate(),
                    defaultDesc = "ABY_DominionPocketCommand_JumpDesc".Translate(),
                    icon = JumpCommandIcon,
                    action = delegate
                    {
                        if (!crisis.TryJumpToPocketSlice(out string failReason) && !failReason.NullOrEmpty())
                        {
                            Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "ABY_DominionPocketCommand_Extract".Translate(),
                    defaultDesc = "ABY_DominionPocketCommand_ExtractDesc".Translate(),
                    icon = ExtractCommandIcon,
                    action = delegate
                    {
                        if (!crisis.TryReturnPocketStrikeTeam(out string failReason) && !failReason.NullOrEmpty())
                        {
                            Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                };
                yield break;
            }

            if (crisis.IsGateEntryReady())
            {
                Command_Action enterCommand = new Command_Action
                {
                    defaultLabel = "ABY_DominionPocketCommand_Enter".Translate(),
                    defaultDesc = "ABY_DominionPocketCommand_EnterDesc".Translate(),
                    icon = JumpCommandIcon,
                    action = delegate
                    {
                        if (!crisis.TryOpenPocketSliceFromPlayerFlow(out string failReason) && !failReason.NullOrEmpty())
                        {
                            Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                        }
                    }
                };

                if (AbyssalDominionPocketUtility.GetSelectedColonistsForPocketEntry(Map).Count <= 0)
                {
                    enterCommand.Disable("ABY_DominionPocketFlowFail_NoStrikeTeam".Translate());
                }

                yield return enterCommand;
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Map?.GetComponent<MapComponent_DominionCrisis>()?.NotifyGateDestroyed(this);
            base.Destroy(mode);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (!Spawned || Map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            ABY_GateAnimationUtility.DrawAnimatedPortal(CoreFrameTexPaths, CoreGlowFrameTexPaths, drawLoc, 4.8f, ticks, pulseSeed, 6, 0.055f, 0.28f, 0.82f, 0.036f);
        }

        public override string GetInspectString()
        {
            string baseText = base.GetInspectString();
            List<string> lines = new List<string>();
            if (!baseText.NullOrEmpty())
            {
                lines.Add(baseText.TrimEnd());
            }

            lines.Add("ABY_DominionGate_Inspect".Translate(GetStatusValue(), GetIntegrityValue(), GetNextPulseEtaValue()));
            MapComponent_DominionCrisis crisis = Map?.GetComponent<MapComponent_DominionCrisis>();
            if (crisis != null)
            {
                lines.Add("ABY_DominionPocketGate_Inspect".Translate(crisis.GetPocketFlowStatusValue()));
                if (crisis.TryGetActivePocketSession(out ABY_DominionPocketSession session) && session != null)
                {
                    Map pocketMap = AbyssalDominionPocketUtility.ResolveMap(session.pocketMapId);
                    lines.Add("ABY_DominionPocketGate_InspectTeam".Translate(AbyssalDominionPocketUtility.GetPocketSessionStatusValue(session, pocketMap)));
                    lines.Add("ABY_DominionPocketGate_InspectObjective".Translate(AbyssalDominionPocketUtility.GetPocketObjectiveValue(session, pocketMap)));
                    lines.Add("ABY_DominionPocketGate_InspectRewards".Translate(AbyssalDominionPocketUtility.GetPocketRewardValue(session, pocketMap)));
                }
            }
            return string.Join("\n", lines);
        }

        public string GetStatusValue()
        {
            float integrity = MaxHitPoints > 0 ? (float)HitPoints / MaxHitPoints : 1f;
            if (integrity > 0.66f)
            {
                return "ABY_DominionGate_Status_Stable".Translate();
            }

            if (integrity > 0.33f)
            {
                return "ABY_DominionGate_Status_Shaken".Translate();
            }

            return "ABY_DominionGate_Status_Critical".Translate();
        }

        public string GetIntegrityValue()
        {
            if (MaxHitPoints <= 0)
            {
                return "ABY_DominionGate_Integrity_Dormant".Translate();
            }

            int percent = Mathf.Clamp(Mathf.RoundToInt((float)HitPoints / MaxHitPoints * 100f), 0, 100);
            return "ABY_DominionGate_Integrity_Value".Translate(percent, HitPoints, MaxHitPoints);
        }

        public string GetNextPulseEtaValue()
        {
            int ticks = TicksUntilNextPulse;
            if (ticks <= 0)
            {
                return "ABY_DominionGate_PulseEta_Pending".Translate();
            }

            if (ticks < 240)
            {
                return "ABY_DominionGate_PulseEta_Imminent".Translate();
            }

            return ticks.ToStringTicksToPeriod();
        }

        public List<string> GetConsoleLines()
        {
            return new List<string>
            {
                "ABY_DominionGate_ConsoleStatus".Translate(GetStatusValue()),
                "ABY_DominionGate_ConsoleIntegrity".Translate(GetIntegrityValue()),
                "ABY_DominionGate_ConsoleNextPulse".Translate(GetNextPulseEtaValue()),
                "ABY_DominionGate_ConsoleAbilities".Translate(
                    "ABY_DominionGate_Ability_Suppression".Translate(),
                    "ABY_DominionGate_Ability_Ignition".Translate(),
                    "ABY_DominionGate_Ability_Call".Translate()),
                "ABY_DominionGate_ConsoleEntry".Translate(Map?.GetComponent<MapComponent_DominionCrisis>()?.GetPocketFlowStatusValue() ?? "ABY_DominionPocketFlowStatus_Locked".Translate())
            };
        }

        private int GetSuppressionInterval()
        {
            float integrity = MaxHitPoints > 0 ? (float)HitPoints / MaxHitPoints : 1f;
            return integrity < 0.35f ? 180 : 240;
        }

        private int GetIgnitionInterval()
        {
            float integrity = MaxHitPoints > 0 ? (float)HitPoints / MaxHitPoints : 1f;
            return integrity < 0.35f ? 240 : 320;
        }

        private int GetCallInterval()
        {
            float integrity = MaxHitPoints > 0 ? (float)HitPoints / MaxHitPoints : 1f;
            return integrity < 0.35f ? 560 : 760;
        }

        private int GetNextPulseTick()
        {
            int next = int.MaxValue;
            if (nextSuppressionTick > 0)
            {
                next = Mathf.Min(next, nextSuppressionTick);
            }
            if (nextIgnitionTick > 0)
            {
                next = Mathf.Min(next, nextIgnitionTick);
            }
            if (nextCallTick > 0)
            {
                next = Mathf.Min(next, nextCallTick);
            }

            return next == int.MaxValue ? 0 : next;
        }

        private void ExecuteSuppressionPulse(MapComponent_DominionCrisis crisis)
        {
            int affected = 0;
            foreach (Thing thing in GetNearbyTargets(18f))
            {
                if (thing is Building_Turret turret && turret.Faction == Faction.OfPlayer)
                {
                    turret.TakeDamage(new DamageInfo(DamageDefOf.EMP, 5.2f, 0f, -1f, this));
                    turret.TakeDamage(new DamageInfo(DamageDefOf.Burn, 4.0f, 0f, -1f, this));
                    affected++;
                }
                else if (thing is Building building && building.Faction == Faction.OfPlayer && (building.GetComp<CompPowerTrader>() != null || building.GetComp<CompPowerBattery>() != null))
                {
                    building.TakeDamage(new DamageInfo(DamageDefOf.EMP, 4.3f, 0f, -1f, this));
                    affected++;
                }

                if (affected >= 6)
                {
                    break;
                }
            }

            if (affected > 0)
            {
                bool lowFx = AbyssalDominionBalanceUtility.ShouldUseLowFxMode(Map, crisis);
                FleckMaker.ThrowLightningGlow(DrawPos, Map, lowFx ? 1.9f : 3.0f);
                crisis?.AddExternalContamination(0.022f);
                if (!lowFx || this.IsHashIntervalTick(180))
                {
                    ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", Position, Map);
                }
            }
        }

        private void ExecuteRiftIgnition(MapComponent_DominionCrisis crisis)
        {
            List<IntVec3> targets = FindIgnitionTargets(3);
            if (targets.Count == 0)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                IntVec3 cell = targets[i];
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                bool lowFx = AbyssalDominionBalanceUtility.ShouldUseLowFxMode(Map, crisis);
                GenExplosion.DoExplosion(cell, Map, 1.45f, DamageDefOf.Flame, this, 10, 0f);
                FireUtility.TryStartFireIn(cell, Map, 0.25f, null, null);
                FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), Map, lowFx ? 1.0f : 1.7f);
            }

            crisis?.AddExternalContamination(0.018f * targets.Count);
            if (!AbyssalDominionBalanceUtility.ShouldUseLowFxMode(Map, crisis) || this.IsHashIntervalTick(240))
            {
                ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", Position, Map);
            }
        }

        private void ExecuteCallOfDominion(MapComponent_DominionCrisis crisis)
        {
            if (crisis == null || Map == null)
            {
                return;
            }

            if (AbyssalDominionWaveUtility.TryExecuteGateSupportWave(Map, crisis, PositionHeld, out string summary, out IntVec3 focusCell))
            {
                crisis.NotifyGatePulse(summary, focusCell.IsValid ? focusCell : PositionHeld);
            }
        }

        private IEnumerable<Thing> GetNearbyTargets(float radius)
        {
            HashSet<Thing> yielded = new HashSet<Thing>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, radius, true))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(Map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed || !yielded.Add(thing))
                    {
                        continue;
                    }

                    yield return thing;
                }
            }
        }

        private List<IntVec3> FindIgnitionTargets(int maxTargets)
        {
            List<IntVec3> cells = new List<IntVec3>();
            if (Map == null)
            {
                return cells;
            }

            List<ScoredCell> scored = new List<ScoredCell>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, 22f, true))
            {
                if (!cell.InBounds(Map) || !cell.Standable(Map))
                {
                    continue;
                }

                float score = 0f;
                Pawn pawn = cell.GetFirstPawn(Map);
                if (pawn != null && pawn.Faction == Faction.OfPlayer)
                {
                    score += 3.2f;
                }

                Building edifice = cell.GetEdifice(Map);
                if (edifice != null && edifice.Faction == Faction.OfPlayer)
                {
                    score += 2.3f;
                    if (edifice is Building_Turret)
                    {
                        score += 1.4f;
                    }
                }

                if (score <= 0f)
                {
                    continue;
                }

                score -= cell.DistanceTo(Position) * 0.05f;
                scored.Add(new ScoredCell { Cell = cell, Score = score });
            }

            scored.Sort((a, b) => b.Score.CompareTo(a.Score));
            for (int i = 0; i < scored.Count && cells.Count < maxTargets; i++)
            {
                IntVec3 cell = scored[i].Cell;
                bool tooClose = false;
                for (int j = 0; j < cells.Count; j++)
                {
                    if (cells[j].DistanceTo(cell) < 4.5f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    cells.Add(cell);
                }
            }

            if (cells.Count == 0)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(Position, 10f, true))
                {
                    if (!cell.InBounds(Map) || !cell.Standable(Map))
                    {
                        continue;
                    }

                    cells.Add(cell);
                    if (cells.Count >= maxTargets)
                    {
                        break;
                    }
                }
            }

            return cells;
        }

    }
}
