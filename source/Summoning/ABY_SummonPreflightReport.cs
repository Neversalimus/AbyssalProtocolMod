using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    /// <summary>
    /// A side-effect-free readiness snapshot shared by the Summoning Console, direct sigil use,
    /// job validation and dev reliability tools. It is deliberately descriptive: it does not
    /// reserve, consume, spawn or mutate encounter state.
    /// </summary>
    public sealed class ABY_SummonPreflightReport
    {
        public sealed class Entry
        {
            public string Id;
            public string Label;
            public string Value;
            public bool Satisfied;
            public bool Blocking;
        }

        private readonly List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;
        public string RitualId { get; private set; }
        public string SummonMode { get; private set; }
        public Building_AbyssalSummoningCircle Circle { get; private set; }
        public Pawn Operator { get; private set; }
        public Thing Sigil { get; private set; }

        public bool CanStart
        {
            get
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] != null && entries[i].Blocking && !entries[i].Satisfied)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public string PrimaryBlocker
        {
            get
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    Entry entry = entries[i];
                    if (entry != null && entry.Blocking && !entry.Satisfied && !entry.Value.NullOrEmpty())
                    {
                        return entry.Value;
                    }
                }

                return null;
            }
        }

        private ABY_SummonPreflightReport(Building_AbyssalSummoningCircle circle, CompProperties_UseEffectSummonBoss props, Pawn operatorPawn, Thing sigil)
        {
            Circle = circle;
            Operator = operatorPawn;
            Sigil = sigil;
            RitualId = props?.ritualId ?? string.Empty;
            SummonMode = props?.summonMode ?? "Boss";
        }

        public static ABY_SummonPreflightReport Create(
            Building_AbyssalSummoningCircle circle,
            CompProperties_UseEffectSummonBoss props,
            Pawn operatorPawn = null,
            Thing sigil = null,
            bool requireOperatorReachability = false,
            bool requireSpecificSigil = false)
        {
            ABY_SummonPreflightReport report = new ABY_SummonPreflightReport(circle, props, operatorPawn, sigil);
            report.Evaluate(props, requireOperatorReachability, requireSpecificSigil);
            return report;
        }

        public static ABY_SummonPreflightReport CreateForRitual(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual)
        {
            ThingDef sigilDef = AbyssalSummoningConsoleUtility.GetSigilDef(ritual);
            CompProperties_UseEffectSummonBoss props = sigilDef?.GetCompProperties<CompProperties_UseEffectSummonBoss>();
            return Create(circle, props, null, null, false, false);
        }

        private void Evaluate(CompProperties_UseEffectSummonBoss props, bool requireOperatorReachability, bool requireSpecificSigil)
        {
            if (props == null)
            {
                Add("payload", "Invocation payload", "The selected sigil has no valid summon payload.", false, true);
                return;
            }

            if (RitualId.NullOrEmpty())
            {
                Add("ritual", "Ritual", "The invocation has no ritual id.", false, true);
            }
            else if (!AbyssalSummoningConsoleUtility.IsRitualUnlocked(RitualId, out string unlockReason))
            {
                Add("unlock", "Protocol access", unlockReason ?? "This ritual is not unlocked.", false, true);
            }
            else
            {
                Add("unlock", "Protocol access", "Ritual unlock gate cleared.", true, true);
            }

            if (Circle == null || Circle.Destroyed || !Circle.Spawned || Circle.Map == null)
            {
                Add("circle", "Summoning circle", "No spawned Abyssal Summoning Circle is available.", false, true);
                Add("power", "Power", "Circle state unavailable.", false, true);
                Add("interaction", "Interaction cell", "Circle state unavailable.", false, true);
                Add("focus", "Ritual focus", "Circle state unavailable.", false, true);
                Add("encounter", "Encounter lock", "Map state unavailable.", false, true);
                Add("capacitors", "Capacitor lattice", "Circle state unavailable.", false, true);
                EvaluateSigilAndOperator(requireOperatorReachability, requireSpecificSigil);
                return;
            }

            if (Circle.RitualActive)
            {
                Add("circle", "Summoning circle", "The selected circle is already running a ritual.", false, true);
            }
            else
            {
                Add("circle", "Summoning circle", "Circle is idle and available.", true, true);
            }

            Add("power", "Power", Circle.IsPoweredForRitual ? "Power online." : "The circle is unpowered.", Circle.IsPoweredForRitual, true);

            bool interactionClear = Circle.HasValidInteractionCell(out string interactionReason);
            Add("interaction", "Interaction cell", interactionClear ? "Interaction cell clear." : interactionReason, interactionClear, true);

            bool focusClear = Circle.HasClearRitualFocus(out string focusReason);
            Add("focus", "Ritual focus", focusClear ? "Ritual focus clear." : focusReason, focusClear, true);

            if (AbyssalBossSummonUtility.TryGetActiveAbyssalEncounterBlocker(Circle.Map, out string encounterReason))
            {
                Add("encounter", "Encounter lock", encounterReason, false, true);
            }
            else
            {
                Add("encounter", "Encounter lock", "No active Abyssal encounter is blocking this map.", true, true);
            }

            if (AbyssalCircleCapacitorRitualUtility.TryAuthorizeRitualStart(
                    Circle,
                    props,
                    Circle.CapacitorOverchannelEnabled,
                    out _,
                    out _,
                    out string capacitorReason))
            {
                Add("capacitors", "Capacitor lattice", "Capacitor profile authorized.", true, true);
            }
            else
            {
                Add("capacitors", "Capacitor lattice", capacitorReason ?? "Capacitor profile is not authorized.", false, true);
            }

            // Arrival is intentionally not random-probed here. Actual route resolution is a commit-stage
            // operation so opening the console cannot disturb gameplay RNG or reserve spawn cells.
            Add("arrival", "Arrival route", "Arrival cell is resolved at commit time after all blocking checks pass.", true, false);
            EvaluateSigilAndOperator(requireOperatorReachability, requireSpecificSigil);
        }

        private void EvaluateSigilAndOperator(bool requireOperatorReachability, bool requireSpecificSigil)
        {
            if (Circle == null || Circle.Map == null)
            {
                Add("sigil", "Sigil", "Sigil availability cannot be evaluated without a map.", false, requireSpecificSigil);
                Add("operator", "Operator", "Operator availability cannot be evaluated without a map.", false, requireOperatorReachability);
                return;
            }

            if (Sigil != null && !Sigil.Destroyed)
            {
                Add("sigil", "Sigil", "Selected sigil is present.", true, requireSpecificSigil);
            }
            else
            {
                ThingDef sigilDef = DefDatabase<ThingDef>.GetNamedSilentFail(GetExpectedSigilDefName());
                int available = sigilDef != null ? Circle.Map.listerThings.ThingsOfDef(sigilDef).Count : 0;
                bool hasSigil = available > 0;
                Add("sigil", "Sigil", hasSigil
                    ? "Prepared sigils available: " + available + "."
                    : "No prepared sigil of the selected ritual is available on this map.", hasSigil, requireSpecificSigil);
            }

            if (!requireOperatorReachability)
            {
                Add("operator", "Operator", Operator != null ? "Operator check deferred to commit." : "Operator check deferred until an invocation pawn is selected.", true, false);
                return;
            }

            if (Operator == null || Operator.Destroyed || Operator.Dead)
            {
                Add("operator", "Operator", "No valid operator is available.", false, true);
                return;
            }

            bool canReach = Operator.CanReserveAndReach(Circle, PathEndMode.InteractionCell, Danger.Deadly);
            Add("operator", "Operator", canReach ? "Operator can reach and reserve the circle." : "The selected operator cannot reach or reserve the circle.", canReach, true);
        }

        private string GetExpectedSigilDefName()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                CompProperties_UseEffectSummonBoss props = def?.GetCompProperties<CompProperties_UseEffectSummonBoss>();
                if (props != null && string.Equals(props.ritualId, RitualId, StringComparison.OrdinalIgnoreCase))
                {
                    return def.defName;
                }
            }

            return null;
        }

        private void Add(string id, string label, string value, bool satisfied, bool blocking)
        {
            entries.Add(new Entry
            {
                Id = id,
                Label = label,
                Value = value ?? string.Empty,
                Satisfied = satisfied,
                Blocking = blocking
            });
        }

        public void AppendDiagnosticReport(StringBuilder sb)
        {
            if (sb == null)
            {
                return;
            }

            sb.AppendLine("Preflight: " + (CanStart ? "PASS" : "BLOCKED"));
            sb.AppendLine("Ritual: " + (RitualId.NullOrEmpty() ? "unknown" : RitualId) + " | mode: " + (SummonMode ?? "Boss"));
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                sb.Append(" - ")
                    .Append(entry.Satisfied ? "PASS" : (entry.Blocking ? "BLOCKED" : "INFO"))
                    .Append(" | ")
                    .Append(entry.Label ?? entry.Id ?? "state")
                    .Append(": ")
                    .AppendLine(entry.Value ?? string.Empty);
            }
        }
    }
}
