using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalHordeSigilUtility
    {
        public const string RitualId = "horde_gate";
        public const string EncounterPoolId = "horde_sigil_wave";
        public const int BaseContentTier = 4;

        private const float AverageUnitBudget = 235f;

        public sealed class HordePhasePlan
        {
            public string PhaseId;
            public string PhaseLabel;
            public string PhaseSummary;
            public int SequenceIndex;
            public int FrontCount;
            public float TotalBudget;
            public List<AbyssalEncounterDirectorUtility.EncounterPlan> PulsePlans = new List<AbyssalEncounterDirectorUtility.EncounterPlan>();

            public int PulseCount => PulsePlans?.Count ?? 0;

            public bool IsSurgePhase => string.Equals(PhaseId, "surge", StringComparison.OrdinalIgnoreCase);
            public bool IsCollapsePhase => string.Equals(PhaseId, "collapse", StringComparison.OrdinalIgnoreCase);

            public int TotalUnits
            {
                get
                {
                    int total = 0;
                    if (PulsePlans == null)
                    {
                        return total;
                    }

                    for (int i = 0; i < PulsePlans.Count; i++)
                    {
                        total += PulsePlans[i]?.TotalUnits ?? 0;
                    }

                    return total;
                }
            }

            public int TotalPortalRequests
            {
                get
                {
                    int total = 0;
                    if (PulsePlans == null)
                    {
                        return total;
                    }

                    for (int i = 0; i < PulsePlans.Count; i++)
                    {
                        AbyssalEncounterDirectorUtility.EncounterPlan pulsePlan = PulsePlans[i];
                        if (pulsePlan?.Entries == null)
                        {
                            continue;
                        }

                        for (int j = 0; j < pulsePlan.Entries.Count; j++)
                        {
                            AbyssalEncounterDirectorUtility.DirectedEntry entry = pulsePlan.Entries[j];
                            if (entry != null && entry.KindDef != null && entry.Count > 0)
                            {
                                total++;
                            }
                        }
                    }

                    return total;
                }
            }
        }

        public sealed class FrontDirective
        {
            public int FrontIndex;
            public string RoleId = string.Empty;
            public string RoleLabel = string.Empty;
            public int PreferredSide = -1;
            public string SideLabel = string.Empty;
            public bool IsCommandFront;
        }

        public sealed class HordePlan
        {
            public string RitualId;
            public int ColonistBand;
            public int WealthBand;
            public int Band;
            public int FrontCount;
            public int PulseCount;
            public int PhaseCount;
            public bool HasSurgePhase;
            public float TotalBudget;
            public float AveragePulseBudget;
            public string ForecastText;
            public string PrimaryTemplateDefName;
            public string PrimaryDoctrineDefName;
            public string PrimaryDoctrineLabel;
            public string PrimaryDoctrineSummary;
            public bool UsesCommandGate;
            public int CommandGateReservedBursts;
            public int CommandGateHitPoints;
            public float CommandGateCadenceFactor = 1f;
            public string CommandGateLabel;
            public string CommandGateSummary;
            public List<FrontDirective> FrontDirectives = new List<FrontDirective>();
            public List<HordePhasePlan> Phases = new List<HordePhasePlan>();
            public List<AbyssalEncounterDirectorUtility.EncounterPlan> PulsePlans = new List<AbyssalEncounterDirectorUtility.EncounterPlan>();
            public Dictionary<string, int> TotalCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public int TotalUnits
            {
                get
                {
                    int total = 0;
                    for (int i = 0; i < PulsePlans.Count; i++)
                    {
                        total += PulsePlans[i]?.TotalUnits ?? 0;
                    }

                    return total;
                }
            }

            public int TotalPortalRequests
            {
                get
                {
                    int total = 0;
                    for (int i = 0; i < PulsePlans.Count; i++)
                    {
                        AbyssalEncounterDirectorUtility.EncounterPlan pulsePlan = PulsePlans[i];
                        if (pulsePlan?.Entries == null)
                        {
                            continue;
                        }

                        for (int j = 0; j < pulsePlan.Entries.Count; j++)
                        {
                            AbyssalEncounterDirectorUtility.DirectedEntry entry = pulsePlan.Entries[j];
                            if (entry != null && entry.KindDef != null && entry.Count > 0)
                            {
                                total++;
                            }
                        }
                    }

                    return total;
                }
            }

            public int GetCount(string pawnKindDefName)
            {
                if (pawnKindDefName.NullOrEmpty())
                {
                    return 0;
                }

                return TotalCounts.TryGetValue(pawnKindDefName, out int count) ? Math.Max(0, count) : 0;
            }
        }

        private sealed class PhaseConfig
        {
            public string PhaseId;
            public int PulseCount;
            public float BudgetShare;
            public int FrontCount;
            public int ContentTier;
            public Dictionary<string, int> MinimumRoleCounts;
            public Dictionary<string, int> MaximumRoleCounts;
        }

        public static bool IsSupportedRitual(string ritualId)
        {
            return string.Equals(ritualId, RitualId, StringComparison.OrdinalIgnoreCase);
        }

        public static HordePlan GetHordePlan(Map map)
        {
            if (map == null)
            {
                return null;
            }

            int colonistBand = GetColonistBand(map);
            int wealthBand = GetWealthBand(map);
            int band = Math.Max(colonistBand, wealthBand);
            int difficultyOrder = AbyssalDifficultyUtility.GetCurrentProfileOrder();

            HordePlan plan = new HordePlan
            {
                RitualId = RitualId,
                ColonistBand = colonistBand,
                WealthBand = wealthBand,
                Band = band,
                FrontCount = ResolveFrontCount(band, difficultyOrder),
                PulseCount = ResolvePulseCount(band, difficultyOrder)
            };

            plan.TotalBudget = BuildTotalBudget(map, band);
            plan.AveragePulseBudget = Mathf.Max(900f, plan.TotalBudget / Math.Max(1, plan.PulseCount));

            List<PhaseConfig> phaseConfigs = BuildPhaseConfigs(plan, difficultyOrder);
            plan.PhaseCount = phaseConfigs.Count;
            plan.HasSurgePhase = phaseConfigs.Exists(config => string.Equals(config.PhaseId, "surge", StringComparison.OrdinalIgnoreCase));

            ResolvePrimaryDoctrine(plan, map, phaseConfigs);
            ResolveCommandGate(plan, map, difficultyOrder);
            ResolveFrontDirectives(plan, map);

            for (int phaseIndex = 0; phaseIndex < phaseConfigs.Count; phaseIndex++)
            {
                PhaseConfig config = phaseConfigs[phaseIndex];
                HordePhasePlan phasePlan = new HordePhasePlan
                {
                    PhaseId = config.PhaseId,
                    SequenceIndex = phaseIndex,
                    FrontCount = config.FrontCount,
                    TotalBudget = Mathf.Max(450f, plan.TotalBudget * Mathf.Clamp(config.BudgetShare, 0.05f, 1f))
                };

                ApplyPhasePresentation(phasePlan, config.PhaseId);

                float phaseAveragePulseBudget = Mathf.Max(320f, phasePlan.TotalBudget / Math.Max(1, config.PulseCount));
                for (int phasePulseIndex = 0; phasePulseIndex < config.PulseCount; phasePulseIndex++)
                {
                    float pulseBudget = phaseAveragePulseBudget * GetPhasePulseBudgetMultiplier(config.PhaseId, phasePulseIndex, config.PulseCount);
                    int seed = BuildPreviewSeed(map, plan, phaseIndex, phasePulseIndex);
                    AbyssalEncounterDirectorUtility.EncounterPlan pulsePlan = AbyssalEncounterDirectorUtility.BuildPlan(
                        EncounterPoolId,
                        pulseBudget,
                        config.ContentTier,
                        map,
                        seed,
                        config.MinimumRoleCounts,
                        config.MaximumRoleCounts,
                        null,
                        null,
                        plan.PrimaryTemplateDefName,
                        plan.PrimaryDoctrineDefName);

                    if (pulsePlan == null || pulsePlan.TotalUnits <= 0)
                    {
                        pulsePlan = BuildFallbackPulsePlan(map, pulseBudget, config.PhaseId);
                    }

                    if (pulsePlan == null || pulsePlan.TotalUnits <= 0)
                    {
                        continue;
                    }

                    if (plan.PrimaryDoctrineLabel.NullOrEmpty() && !pulsePlan.DoctrineDefName.NullOrEmpty())
                    {
                        plan.PrimaryDoctrineDefName = pulsePlan.DoctrineDefName;
                        plan.PrimaryTemplateDefName = pulsePlan.TemplateDefName ?? string.Empty;
                        ApplyDoctrinePresentation(plan, plan.PrimaryDoctrineDefName);
                    }

                    phasePlan.PulsePlans.Add(pulsePlan);
                    plan.PulsePlans.Add(pulsePlan);
                    MergeCounts(plan.TotalCounts, pulsePlan);
                }

                if (phasePlan.PulsePlans.Count > 0)
                {
                    plan.Phases.Add(phasePlan);
                }
            }

            if (plan.PulsePlans.Count == 0)
            {
                HordePlan fallbackPlan = new HordePlan
                {
                    RitualId = RitualId,
                    ColonistBand = colonistBand,
                    WealthBand = wealthBand,
                    Band = band,
                    FrontCount = ResolveFrontCount(band, difficultyOrder),
                    PulseCount = 1,
                    PhaseCount = 1,
                    HasSurgePhase = false,
                    TotalBudget = 0f,
                    AveragePulseBudget = 0f,
                    ForecastText = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CirclePreview_None", "no hostiles")
                };

                HordePhasePlan fallbackPhase = new HordePhasePlan
                {
                    PhaseId = "marking",
                    SequenceIndex = 0,
                    FrontCount = Mathf.Max(1, fallbackPlan.FrontCount - 1),
                    TotalBudget = 1800f
                };
                ApplyPhasePresentation(fallbackPhase, fallbackPhase.PhaseId);

                AbyssalEncounterDirectorUtility.EncounterPlan fallbackPulse = BuildFallbackPulsePlan(map, 1800f, fallbackPhase.PhaseId);
                if (fallbackPulse != null)
                {
                    fallbackPhase.PulsePlans.Add(fallbackPulse);
                    fallbackPlan.Phases.Add(fallbackPhase);
                    fallbackPlan.PulsePlans.Add(fallbackPulse);
                    MergeCounts(fallbackPlan.TotalCounts, fallbackPulse);
                    ApplyDoctrinePresentation(fallbackPlan, fallbackPlan.PrimaryDoctrineDefName);
                    fallbackPlan.ForecastText = BuildForecastText(fallbackPlan);
                }

                return fallbackPlan;
            }

            plan.ForecastText = BuildForecastText(plan);
            return plan;
        }

        public static string GetPreviewText(HordePlan plan)
        {
            return plan == null ? string.Empty : (!plan.ForecastText.NullOrEmpty() ? plan.ForecastText : BuildForecastText(plan));
        }

        public static string GetMetaText(Building_AbyssalSummoningCircle circle, AbyssalSummoningConsoleUtility.RitualDefinition ritual, int sigilCount)
        {
            HordePlan plan = GetHordePlan(circle?.Map);
            if (plan == null)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_CircleRitualMeta",
                    "Sigils on map: {0}   •   Threat budget: {1}   •   Protocol: {2}",
                    sigilCount,
                    ritual?.SpawnPoints ?? 0,
                    AbyssalDifficultyUtility.GetCurrentDifficultyLabel());
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeRitualMeta",
                "Sigils on map: {0}   •   Pattern: {1}   •   Fronts: {2}   •   Pulses: {3}   •   Command gate: {4}   •   Forecast: {5}   •   Protocol: {6}",
                sigilCount,
                GetDoctrineLabel(plan),
                plan.FrontCount,
                plan.PulseCount,
                plan.UsesCommandGate ? GetCommandGateLabel(plan) : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_AbsentShort", "no"),
                GetPreviewText(plan),
                AbyssalDifficultyUtility.GetCurrentDifficultyLabel());
        }

        private static int GetColonistBand(Map map)
        {
            int colonists = Math.Max(1, map?.mapPawns?.FreeColonistsSpawnedCount ?? 0);
            if (colonists <= 12)
            {
                return 0;
            }

            if (colonists <= 18)
            {
                return 1;
            }

            if (colonists <= 25)
            {
                return 2;
            }

            return 3;
        }

        private static int GetWealthBand(Map map)
        {
            float wealth = map?.wealthWatcher?.WealthTotal ?? 0f;
            if (wealth <= 300000f)
            {
                return 0;
            }

            if (wealth <= 500000f)
            {
                return 1;
            }

            if (wealth <= 850000f)
            {
                return 2;
            }

            return 3;
        }

        private static int ResolveFrontCount(int band, int difficultyOrder)
        {
            int fronts;
            switch (band)
            {
                case 0:
                    fronts = 2;
                    break;
                case 1:
                    fronts = 3;
                    break;
                case 2:
                    fronts = 3;
                    break;
                default:
                    fronts = 4;
                    break;
            }

            if (difficultyOrder >= 3 && band >= 2)
            {
                fronts++;
            }

            return Mathf.Clamp(fronts, 2, 5);
        }

        private static int ResolvePulseCount(int band, int difficultyOrder)
        {
            int pulses;
            switch (band)
            {
                case 0:
                    pulses = 2;
                    break;
                case 1:
                    pulses = 3;
                    break;
                case 2:
                    pulses = 3;
                    break;
                default:
                    pulses = 4;
                    break;
            }

            if (difficultyOrder >= 2)
            {
                pulses++;
            }

            return Mathf.Clamp(pulses, 2, 5);
        }

        private static float BuildTotalBudget(Map map, int band)
        {
            int colonists = Math.Max(1, map?.mapPawns?.FreeColonistsSpawnedCount ?? 0);
            float wealth = map?.wealthWatcher?.WealthTotal ?? 0f;
            float encounterMultiplier = AbyssalDifficultyUtility.GetEncounterBudgetMultiplier();
            float colonistBudget = colonists * 310f;
            float wealthBudget = Mathf.Clamp(wealth / 120f, 1000f, 7600f);
            float bandBonus = 900f + band * 650f;
            return (colonistBudget + wealthBudget + bandBonus) * encounterMultiplier;
        }

        private static List<PhaseConfig> BuildPhaseConfigs(HordePlan plan, int difficultyOrder)
        {
            List<PhaseConfig> phases = new List<PhaseConfig>();
            if (plan == null)
            {
                return phases;
            }

            bool hasTailPhase = plan.PulseCount >= 3;
            bool hasSurgePhase = hasTailPhase && (difficultyOrder >= 2 || plan.Band >= 2);

            int markingPulses = 1;
            int tailPulses = hasTailPhase ? 1 : 0;
            int latticePulses = Math.Max(1, plan.PulseCount - markingPulses - tailPulses);

            if (!hasTailPhase)
            {
                phases.Add(new PhaseConfig
                {
                    PhaseId = "marking",
                    PulseCount = 1,
                    BudgetShare = 0.28f,
                    FrontCount = Mathf.Max(1, plan.FrontCount - 1),
                    ContentTier = Math.Max(2, BaseContentTier - 1),
                    MinimumRoleCounts = MakeRoleCounts(("assault", 2)),
                    MaximumRoleCounts = MakeRoleCounts(("support", 1), ("elite", 1), ("boss", 0))
                });
                phases.Add(new PhaseConfig
                {
                    PhaseId = "lattice",
                    PulseCount = Math.Max(1, plan.PulseCount - 1),
                    BudgetShare = 0.72f,
                    FrontCount = plan.FrontCount,
                    ContentTier = BaseContentTier,
                    MinimumRoleCounts = null,
                    MaximumRoleCounts = MakeRoleCounts(("boss", 0))
                });
                return phases;
            }

            phases.Add(new PhaseConfig
            {
                PhaseId = "marking",
                PulseCount = markingPulses,
                BudgetShare = hasSurgePhase ? 0.18f : 0.20f,
                FrontCount = Mathf.Max(1, plan.FrontCount - 1),
                ContentTier = Math.Max(2, BaseContentTier - 1),
                MinimumRoleCounts = MakeRoleCounts(("assault", 2)),
                MaximumRoleCounts = MakeRoleCounts(("support", 1), ("elite", 1), ("boss", 0))
            });

            phases.Add(new PhaseConfig
            {
                PhaseId = "lattice",
                PulseCount = latticePulses,
                BudgetShare = hasSurgePhase ? 0.56f : 0.64f,
                FrontCount = plan.FrontCount,
                ContentTier = BaseContentTier,
                MinimumRoleCounts = null,
                MaximumRoleCounts = MakeRoleCounts(("boss", 0))
            });

            phases.Add(new PhaseConfig
            {
                PhaseId = hasSurgePhase ? "surge" : "collapse",
                PulseCount = tailPulses,
                BudgetShare = hasSurgePhase ? 0.26f : 0.16f,
                FrontCount = hasSurgePhase ? Mathf.Clamp(Mathf.CeilToInt(plan.FrontCount * 0.5f), 1, 2) : Mathf.Clamp(plan.FrontCount - 2, 1, 2),
                ContentTier = hasSurgePhase
                    ? BaseContentTier + ((difficultyOrder >= 3 || plan.Band >= 3) ? 1 : 0)
                    : Math.Max(2, BaseContentTier - 1),
                MinimumRoleCounts = hasSurgePhase ? MakeRoleCounts(("assault", 1), ("support", 1), ("elite", 1)) : MakeRoleCounts(("assault", 1)),
                MaximumRoleCounts = hasSurgePhase ? MakeRoleCounts(("boss", 0)) : MakeRoleCounts(("support", 1), ("elite", 1), ("boss", 0))
            });

            return phases;
        }

        private static Dictionary<string, int> MakeRoleCounts(params (string role, int count)[] pairs)
        {
            if (pairs == null || pairs.Length == 0)
            {
                return null;
            }

            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < pairs.Length; i++)
            {
                if (pairs[i].role.NullOrEmpty())
                {
                    continue;
                }

                result[pairs[i].role] = Math.Max(0, pairs[i].count);
            }

            return result.Count == 0 ? null : result;
        }

        private static void ResolvePrimaryDoctrine(HordePlan plan, Map map, List<PhaseConfig> phaseConfigs)
        {
            if (plan == null || map == null)
            {
                return;
            }

            PhaseConfig doctrinePhase = null;
            if (phaseConfigs != null)
            {
                for (int i = 0; i < phaseConfigs.Count; i++)
                {
                    if (string.Equals(phaseConfigs[i].PhaseId, "lattice", StringComparison.OrdinalIgnoreCase))
                    {
                        doctrinePhase = phaseConfigs[i];
                        break;
                    }
                }

                if (doctrinePhase == null && phaseConfigs.Count > 0)
                {
                    doctrinePhase = phaseConfigs[Math.Min(1, phaseConfigs.Count - 1)];
                }
            }

            float doctrineBudget = Mathf.Max(1200f, plan.TotalBudget * Mathf.Clamp(doctrinePhase?.BudgetShare ?? 0.55f, 0.25f, 0.75f) / Math.Max(1, doctrinePhase?.PulseCount ?? 1));
            int doctrineSeed = BuildPreviewSeed(map, plan, doctrinePhase?.PhaseId == "marking" ? 0 : 1, 0);
            AbyssalEncounterDirectorUtility.EncounterPlan doctrinePlan = AbyssalEncounterDirectorUtility.BuildPlan(
                EncounterPoolId,
                doctrineBudget,
                doctrinePhase?.ContentTier ?? BaseContentTier,
                map,
                doctrineSeed,
                doctrinePhase?.MinimumRoleCounts,
                doctrinePhase?.MaximumRoleCounts,
                null,
                null);

            if (doctrinePlan == null || doctrinePlan.TotalUnits <= 0)
            {
                doctrinePlan = BuildFallbackPulsePlan(map, doctrineBudget, doctrinePhase?.PhaseId ?? "lattice");
            }

            if (doctrinePlan == null)
            {
                return;
            }

            plan.PrimaryTemplateDefName = doctrinePlan.TemplateDefName ?? string.Empty;
            plan.PrimaryDoctrineDefName = doctrinePlan.DoctrineDefName ?? string.Empty;
            ApplyDoctrinePresentation(plan, plan.PrimaryDoctrineDefName);
        }

        private static float GetPhasePulseBudgetMultiplier(string phaseId, int pulseIndex, int totalPhasePulses)
        {
            if (totalPhasePulses <= 1)
            {
                if (string.Equals(phaseId, "marking", StringComparison.OrdinalIgnoreCase))
                {
                    return 0.98f;
                }

                if (string.Equals(phaseId, "surge", StringComparison.OrdinalIgnoreCase))
                {
                    return 1.08f;
                }

                return 1f;
            }

            float midpoint = (totalPhasePulses - 1) * 0.5f;
            float distance = Mathf.Abs(pulseIndex - midpoint);
            float normalized = midpoint <= 0.01f ? 0f : distance / midpoint;

            if (string.Equals(phaseId, "marking", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Lerp(1.02f, 0.92f, normalized);
            }

            if (string.Equals(phaseId, "surge", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Lerp(1.10f, 0.96f, normalized);
            }

            if (string.Equals(phaseId, "collapse", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Lerp(0.94f, 0.84f, normalized);
            }

            return Mathf.Lerp(1.06f, 0.94f, normalized);
        }

        private static void ResolveCommandGate(HordePlan plan, Map map, int difficultyOrder)
        {
            if (plan == null || map == null)
            {
                return;
            }

            plan.UsesCommandGate = true;

            int reservedBursts = 1;
            if (plan.Band >= 2 || difficultyOrder >= 2)
            {
                reservedBursts++;
            }

            if (plan.Band >= 3 && difficultyOrder >= 3)
            {
                reservedBursts++;
            }

            plan.CommandGateReservedBursts = Mathf.Clamp(reservedBursts, 1, 3);
            plan.CommandGateHitPoints = Mathf.Clamp(580 + plan.Band * 90 + difficultyOrder * 40, 580, 980);
            plan.CommandGateCadenceFactor = Mathf.Clamp(0.88f - (plan.Band * 0.02f) - (difficultyOrder * 0.015f), 0.72f, 0.90f);
            plan.CommandGateLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_Label", "Command gate node");
            plan.CommandGateSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeCommandGate_Summary",
                "Secondary objective expected. While intact, one perimeter command node accelerates the offensive and keeps {0} reserved reinforcement bursts routed through its front. Destroying it cancels those command-linked bursts and slows the breach cadence.",
                plan.CommandGateReservedBursts);
        }

        public static int ResolveCommandFrontIndex(HordePlan plan)
        {
            if (plan == null || plan.FrontCount <= 1)
            {
                return 0;
            }

            string doctrine = plan.PrimaryDoctrineDefName ?? string.Empty;
            if (string.Equals(doctrine, "ABY_Doctrine_HordeFireline", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Clamp(1, 0, plan.FrontCount - 1);
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeGrinder", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Clamp(plan.FrontCount - 1, 0, plan.FrontCount - 1);
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeSiege", StringComparison.OrdinalIgnoreCase))
            {
                return Mathf.Clamp(plan.FrontCount / 2, 0, plan.FrontCount - 1);
            }

            return 0;
        }

        public static FrontDirective GetFrontDirective(HordePlan plan, int frontIndex)
        {
            if (plan?.FrontDirectives == null || plan.FrontDirectives.Count == 0)
            {
                return null;
            }

            int clamped = Mathf.Clamp(frontIndex, 0, plan.FrontDirectives.Count - 1);
            return plan.FrontDirectives[clamped];
        }

        public static string ResolveEntryFrontRoleId(string pawnKindDefName, string entryRole)
        {
            string defName = pawnKindDefName ?? string.Empty;
            if (string.Equals(defName, "ABY_SiegeIdol", StringComparison.OrdinalIgnoreCase))
            {
                return "siege";
            }

            if (string.Equals(defName, "ABY_RiftSniper", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_NullPriest", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_HaloHusk", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_HexgunThrall", StringComparison.OrdinalIgnoreCase))
            {
                return "fire_support";
            }

            if (string.Equals(defName, "ABY_EmberHound", StringComparison.OrdinalIgnoreCase))
            {
                return "flank";
            }

            if (string.Equals(entryRole ?? string.Empty, "support", StringComparison.OrdinalIgnoreCase))
            {
                return "fire_support";
            }

            return "assault";
        }

        public static string GetPerimeterSummary(HordePlan plan)
        {
            if (plan == null)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePerimeter_Unknown", "Perimeter routing could not be stabilized. Expect a rough multi-front breach.");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordePerimeter_Summary",
                "Perimeter intelligence is active. Fronts are assigned to approach lanes, internal colony cells are avoided, and heavy pressure is split across distinct edges when possible.");
        }

        public static List<string> GetPerimeterLines(HordePlan plan)
        {
            List<string> lines = new List<string>();
            if (plan?.FrontDirectives == null || plan.FrontDirectives.Count == 0)
            {
                return lines;
            }

            for (int i = 0; i < plan.FrontDirectives.Count; i++)
            {
                FrontDirective front = plan.FrontDirectives[i];
                if (front == null)
                {
                    continue;
                }

                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    front.IsCommandFront ? "ABY_HordePerimeter_Line_Command" : "ABY_HordePerimeter_Line",
                    front.IsCommandFront
                        ? "Front {0}: {1} lane on the {2} perimeter. Command-linked pressure is routed through this edge."
                        : "Front {0}: {1} lane on the {2} perimeter.",
                    front.FrontIndex + 1,
                    !front.RoleLabel.NullOrEmpty() ? front.RoleLabel : GetFrontRoleLabel(front.RoleId),
                    !front.SideLabel.NullOrEmpty() ? front.SideLabel : GetSideLabel(front.PreferredSide)));
            }

            return lines;
        }

        private static void ResolveFrontDirectives(HordePlan plan, Map map)
        {
            if (plan == null)
            {
                return;
            }

            plan.FrontDirectives ??= new List<FrontDirective>();
            plan.FrontDirectives.Clear();

            int commandFrontIndex = plan.UsesCommandGate ? ResolveCommandFrontIndex(plan) : -1;
            IntVec3 strongholdCenter = GetPlayerStrongholdCenter(map);
            int mainAssaultSide = ResolveNearestPerimeterSide(map, strongholdCenter);
            int oppositeSide = GetOppositeSide(mainAssaultSide);
            int clockwiseSide = GetClockwiseSide(mainAssaultSide);
            int counterClockwiseSide = GetCounterClockwiseSide(mainAssaultSide);

            List<(string roleId, int preferredSide)> slots = BuildDoctrineFrontSlots(plan, mainAssaultSide, oppositeSide, clockwiseSide, counterClockwiseSide);
            for (int i = 0; i < Math.Max(1, plan.FrontCount); i++)
            {
                (string roleId, int preferredSide) slot = slots[Math.Min(i, slots.Count - 1)];
                FrontDirective directive = new FrontDirective
                {
                    FrontIndex = i,
                    RoleId = slot.roleId ?? "assault",
                    RoleLabel = GetFrontRoleLabel(slot.roleId),
                    PreferredSide = slot.preferredSide,
                    SideLabel = GetSideLabel(slot.preferredSide),
                    IsCommandFront = commandFrontIndex >= 0 && i == commandFrontIndex
                };

                plan.FrontDirectives.Add(directive);
            }
        }

        private static List<(string roleId, int preferredSide)> BuildDoctrineFrontSlots(HordePlan plan, int mainAssaultSide, int oppositeSide, int clockwiseSide, int counterClockwiseSide)
        {
            string doctrine = plan?.PrimaryDoctrineDefName ?? string.Empty;
            List<(string roleId, int preferredSide)> slots = new List<(string roleId, int preferredSide)>();

            if (string.Equals(doctrine, "ABY_Doctrine_HordeFireline", StringComparison.OrdinalIgnoreCase))
            {
                slots.Add(("fire_support", oppositeSide));
                slots.Add(("assault", mainAssaultSide));
                slots.Add(("flank", clockwiseSide));
                slots.Add(("flank", counterClockwiseSide));
                slots.Add(("siege", oppositeSide));
                return slots;
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeGrinder", StringComparison.OrdinalIgnoreCase))
            {
                slots.Add(("assault", mainAssaultSide));
                slots.Add(("fire_support", oppositeSide));
                slots.Add(("flank", clockwiseSide));
                slots.Add(("assault", counterClockwiseSide));
                slots.Add(("siege", oppositeSide));
                return slots;
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeSiege", StringComparison.OrdinalIgnoreCase))
            {
                slots.Add(("siege", oppositeSide));
                slots.Add(("fire_support", clockwiseSide));
                slots.Add(("assault", mainAssaultSide));
                slots.Add(("flank", counterClockwiseSide));
                slots.Add(("fire_support", oppositeSide));
                return slots;
            }

            slots.Add(("assault", mainAssaultSide));
            slots.Add(("flank", clockwiseSide));
            slots.Add(("flank", counterClockwiseSide));
            slots.Add(("fire_support", oppositeSide));
            slots.Add(("assault", oppositeSide));
            return slots;
        }

        private static string GetFrontRoleLabel(string roleId)
        {
            string safe = (roleId ?? string.Empty).ToLowerInvariant();
            switch (safe)
            {
                case "flank":
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeFrontRole_Flank", "Flank");
                case "fire_support":
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeFrontRole_FireSupport", "Fire support");
                case "siege":
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeFrontRole_Siege", "Siege");
                default:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeFrontRole_Assault", "Assault");
            }
        }

        private static string GetSideLabel(int side)
        {
            switch (side)
            {
                case 0:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeSide_West", "west");
                case 1:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeSide_East", "east");
                case 2:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeSide_South", "south");
                case 3:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeSide_North", "north");
                default:
                    return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeSide_Unknown", "unstable edge");
            }
        }

        private static IntVec3 GetPlayerStrongholdCenter(Map map)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            int totalX = 0;
            int totalZ = 0;
            int count = 0;
            List<Thing> things = map.listerThings?.AllThings;
            if (things != null)
            {
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed || thing.Faction != Faction.OfPlayer)
                    {
                        continue;
                    }

                    if (thing.def != null && thing.def.category == ThingCategory.Building)
                    {
                        totalX += thing.Position.x;
                        totalZ += thing.Position.z;
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                return new IntVec3(Mathf.RoundToInt((float)totalX / count), 0, Mathf.RoundToInt((float)totalZ / count));
            }

            List<Pawn> pawns = map.mapPawns?.FreeColonistsSpawned;
            if (pawns != null)
            {
                totalX = 0;
                totalZ = 0;
                count = 0;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn == null || !pawn.Spawned)
                    {
                        continue;
                    }

                    totalX += pawn.Position.x;
                    totalZ += pawn.Position.z;
                    count++;
                }

                if (count > 0)
                {
                    return new IntVec3(Mathf.RoundToInt((float)totalX / count), 0, Mathf.RoundToInt((float)totalZ / count));
                }
            }

            return new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);
        }

        private static int ResolveNearestPerimeterSide(Map map, IntVec3 center)
        {
            if (map == null || !center.IsValid)
            {
                return 0;
            }

            int west = center.x;
            int east = Math.Abs(map.Size.x - 1 - center.x);
            int south = center.z;
            int north = Math.Abs(map.Size.z - 1 - center.z);

            int bestSide = 0;
            int bestDistance = west;
            if (east < bestDistance)
            {
                bestDistance = east;
                bestSide = 1;
            }

            if (south < bestDistance)
            {
                bestDistance = south;
                bestSide = 2;
            }

            if (north < bestDistance)
            {
                bestSide = 3;
            }

            return bestSide;
        }

        private static int GetOppositeSide(int side)
        {
            switch (side)
            {
                case 0: return 1;
                case 1: return 0;
                case 2: return 3;
                default: return 2;
            }
        }

        private static int GetClockwiseSide(int side)
        {
            switch (side)
            {
                case 0: return 2;
                case 2: return 1;
                case 1: return 3;
                default: return 0;
            }
        }

        private static int GetCounterClockwiseSide(int side)
        {
            switch (side)
            {
                case 0: return 3;
                case 3: return 1;
                case 1: return 2;
                default: return 0;
            }
        }

        private static int BuildPreviewSeed(Map map, HordePlan plan, int phaseIndex, int pulseIndex)
        {
            int seed = 278123;
            seed = Gen.HashCombineInt(seed, map != null ? map.uniqueID : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.Band : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.ColonistBand : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.WealthBand : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.FrontCount : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.PulseCount : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.PhaseCount : 0);
            seed = Gen.HashCombineInt(seed, phaseIndex);
            seed = Gen.HashCombineInt(seed, pulseIndex);
            seed = Gen.HashCombineInt(seed, AbyssalDifficultyUtility.GetCurrentProfileOrder());
            seed = Gen.HashCombineInt(seed, AbyssalDifficultyUtility.GetProgressionStage(map));
            seed = Gen.HashCombineInt(seed, BaseContentTier);
            seed = Gen.HashCombineInt(seed, RitualId.GetHashCode());
            return seed;
        }

        private static AbyssalEncounterDirectorUtility.EncounterPlan BuildFallbackPulsePlan(Map map, float pulseBudget, string phaseId)
        {
            AbyssalEncounterDirectorUtility.EncounterPlan plan = new AbyssalEncounterDirectorUtility.EncounterPlan
            {
                PoolId = EncounterPoolId,
                Budget = pulseBudget,
                AllowedContentTier = string.Equals(phaseId, "marking", StringComparison.OrdinalIgnoreCase) || string.Equals(phaseId, "collapse", StringComparison.OrdinalIgnoreCase)
                    ? Math.Max(2, BaseContentTier - 1)
                    : BaseContentTier
            };

            int impCount = Mathf.Clamp(Mathf.RoundToInt(pulseBudget / 780f), 2, string.Equals(phaseId, "surge", StringComparison.OrdinalIgnoreCase) ? 6 : 10);
            int houndCount = Mathf.Clamp(Mathf.RoundToInt(pulseBudget / 1200f), 1, 5);
            int thrallCount = Mathf.Clamp(Mathf.RoundToInt(pulseBudget / 1650f), 1, string.Equals(phaseId, "marking", StringComparison.OrdinalIgnoreCase) ? 2 : 3);
            int zealotCount = Mathf.Clamp(Mathf.RoundToInt(pulseBudget / 1900f), 1, string.Equals(phaseId, "marking", StringComparison.OrdinalIgnoreCase) ? 2 : 3);

            AddFallbackEntry(plan, "ABY_RiftImp", impCount, "assault");
            AddFallbackEntry(plan, "ABY_EmberHound", houndCount, "assault");
            AddFallbackEntry(plan, "ABY_HexgunThrall", thrallCount, "support");
            AddFallbackEntry(plan, "ABY_ChainZealot", zealotCount, "elite");

            if (string.Equals(phaseId, "surge", StringComparison.OrdinalIgnoreCase))
            {
                AddFallbackEntry(plan, "ABY_Harvester", Mathf.Clamp(Mathf.RoundToInt(pulseBudget / 2600f), 1, 2), "elite");
            }

            return plan;
        }

        private static void AddFallbackEntry(AbyssalEncounterDirectorUtility.EncounterPlan plan, string pawnKindDefName, int count, string role)
        {
            if (plan == null || count <= 0)
            {
                return;
            }

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(pawnKindDefName);
            if (kindDef == null)
            {
                return;
            }

            plan.Entries.Add(new AbyssalEncounterDirectorUtility.DirectedEntry
            {
                KindDef = kindDef,
                Count = count,
                BudgetCost = count * AverageUnitBudget,
                Role = role
            });
        }

        private static void MergeCounts(Dictionary<string, int> totals, AbyssalEncounterDirectorUtility.EncounterPlan pulsePlan)
        {
            if (totals == null || pulsePlan?.Entries == null)
            {
                return;
            }

            for (int i = 0; i < pulsePlan.Entries.Count; i++)
            {
                AbyssalEncounterDirectorUtility.DirectedEntry entry = pulsePlan.Entries[i];
                if (entry?.KindDef == null || entry.Count <= 0)
                {
                    continue;
                }

                string key = entry.KindDef.defName ?? string.Empty;
                totals[key] = totals.TryGetValue(key, out int existing) ? existing + entry.Count : entry.Count;
            }
        }

        public static string GetDoctrineLabel(HordePlan plan)
        {
            if (plan == null || plan.PrimaryDoctrineLabel.NullOrEmpty())
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Unknown_Label", "Unshaped breach");
            }

            return plan.PrimaryDoctrineLabel;
        }

        public static string GetDoctrineSummary(HordePlan plan)
        {
            if (plan == null || plan.PrimaryDoctrineSummary.NullOrEmpty())
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Unknown_Summary", "Forecast doctrine could not be stabilized. Expect a mixed perimeter breach with no clean specialization.");
            }

            return plan.PrimaryDoctrineSummary;
        }

        public static string GetFrontsBulletin(HordePlan plan)
        {
            if (plan == null)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_Fronts_Unknown", "unstable");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeBulletin_Fronts",
                "{0} fronts",
                Math.Max(1, plan.FrontCount));
        }

        public static string GetPhasesBulletin(HordePlan plan)
        {
            if (plan == null)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_Phases_Unknown", "unstable");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeBulletin_Phases",
                "{0} phases",
                Math.Max(1, plan.PhaseCount));
        }

        public static string GetCommandBulletin(HordePlan plan)
        {
            if (plan == null)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_Command_Unknown", "unstable");
            }

            if (!plan.UsesCommandGate)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_Command_None", "No node");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeBulletin_Command_Expected",
                "Node +{0} bursts",
                Math.Max(1, plan.CommandGateReservedBursts));
        }

        public static string GetSiegeBulletin(HordePlan plan)
        {
            if (plan == null)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_Siege_Unknown", "unstable");
            }

            int siegeIdols = plan.GetCount("ABY_SiegeIdol");
            int brutes = plan.GetCount("ABY_BreachBrute");
            bool siegeDoctrine = string.Equals(plan.PrimaryDoctrineDefName, "ABY_Doctrine_HordeSiege", StringComparison.OrdinalIgnoreCase);
            if (siegeDoctrine || siegeIdols > 0)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_Siege_Likely", "Likely");
            }

            if (brutes > 0 || plan.Band >= 2 || AbyssalDifficultyUtility.GetCurrentProfileOrder() >= 3)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_Siege_Possible", "Possible");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeBulletin_Siege_Low", "Low");
        }

        public static string GetDoctrineWarning(HordePlan plan)
        {
            string doctrine = plan?.PrimaryDoctrineDefName ?? string.Empty;
            if (string.Equals(doctrine, "ABY_Doctrine_HordeFlood", StringComparison.OrdinalIgnoreCase))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrineWarning_Flood", "Warning: the opening flood wants to pin defenders with speed and body count. Do not overextend on the first breach.");
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeFireline", StringComparison.OrdinalIgnoreCase))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrineWarning_Fireline", "Warning: this pattern punishes static firing lines. Snipers and priests will try to hold open lanes behind the front.");
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeGrinder", StringComparison.OrdinalIgnoreCase))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrineWarning_Grinder", "Warning: the grinder host wants a dense frontal collapse. Do not let heavy bodies stack on a single doorway.");
            }

            if (string.Equals(doctrine, "ABY_Doctrine_HordeSiege", StringComparison.OrdinalIgnoreCase))
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrineWarning_Siege", "Warning: late heavy anchors are more likely here. Leaving open ground unchecked increases the chance of siege pressure stabilizing.");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrineWarning_Unknown", "Warning: the breach pattern is unstable. Expect mixed pressure with no clean specialization.");
        }

        public static string GetOperationBulletin(HordePlan plan)
        {
            if (plan == null)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeOperationBulletin_Unknown", "Operation bulletin could not be stabilized.");
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_HordeOperationBulletin",
                "{0} doctrine. {1}. {2}. Command status: {3}. Siege pressure: {4}.",
                GetDoctrineLabel(plan),
                GetFrontsBulletin(plan),
                GetPhasesBulletin(plan),
                GetCommandBulletin(plan),
                GetSiegeBulletin(plan));
        }

        public static string GetPhaseFlowSummary(HordePlan plan)
        {
            if (plan == null || plan.Phases == null || plan.Phases.Count == 0)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhasesPreview_Unknown", "Phase routing could not be stabilized. Expect a rough multi-front breach.");
            }

            List<string> labels = new List<string>();
            for (int i = 0; i < plan.Phases.Count; i++)
            {
                if (!plan.Phases[i].PhaseLabel.NullOrEmpty())
                {
                    labels.Add(plan.Phases[i].PhaseLabel);
                }
            }

            string flow = labels.Count > 0 ? string.Join(" → ", labels.ToArray()) : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhasesPreview_Unknown", "Unstable phase routing");
            return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhasesPreview_Summary", "{0} phases routed: {1}", Math.Max(1, plan.Phases.Count), flow);
        }

        public static List<string> GetPhaseLines(HordePlan plan)
        {
            List<string> lines = new List<string>();
            if (plan?.Phases == null)
            {
                return lines;
            }

            for (int i = 0; i < plan.Phases.Count; i++)
            {
                HordePhasePlan phase = plan.Phases[i];
                if (phase == null)
                {
                    continue;
                }

                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_HordePhasesPreview_Line",
                    "{0}: {1} Fronts {2}. Pulses {3}.",
                    phase.PhaseLabel,
                    phase.PhaseSummary,
                    phase.FrontCount,
                    Math.Max(1, phase.PulseCount)));
            }

            return lines;
        }

        public static string GetCommandGateLabel(HordePlan plan)
        {
            if (plan == null || !plan.UsesCommandGate)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_AbsentShort", "no");
            }

            return !plan.CommandGateLabel.NullOrEmpty()
                ? plan.CommandGateLabel
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_Label", "Command gate node");
        }

        public static string GetCommandGateSummary(HordePlan plan)
        {
            if (plan == null || !plan.UsesCommandGate)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_AbsentSummary", "No secondary command node is expected for this breach forecast.");
            }

            return !plan.CommandGateSummary.NullOrEmpty()
                ? plan.CommandGateSummary
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_Summary", "Secondary objective expected. Destroying the node slows the offensive and cancels reserved command bursts.");
        }

        public static List<string> GetCommandGateLines(HordePlan plan)
        {
            List<string> lines = new List<string>();
            if (plan == null || !plan.UsesCommandGate)
            {
                return lines;
            }

            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_LineBursts", "Reserved bursts: {0}", Math.Max(1, plan.CommandGateReservedBursts)));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_LineCadence", "Intact cadence factor: {0}% of normal portal delay on the command front.", Mathf.RoundToInt(plan.CommandGateCadenceFactor * 100f)));
            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeCommandGate_LineCollapse", "Destroying it immediately cancels remaining command-linked reinforcements."));
            return lines;
        }

        private static void ApplyDoctrinePresentation(HordePlan plan, string doctrineDefName)
        {
            if (plan == null)
            {
                return;
            }

            plan.PrimaryDoctrineDefName = doctrineDefName ?? string.Empty;
            string safe = (doctrineDefName ?? string.Empty).ToLowerInvariant();
            switch (safe)
            {
                case "aby_doctrine_hordeflood":
                    plan.PrimaryDoctrineLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Flood_Label", "Ravenous Breach");
                    plan.PrimaryDoctrineSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Flood_Summary", "Assault-heavy flood. Expects fast pressure, high body count, and relatively light precision support.");
                    break;
                case "aby_doctrine_hordefireline":
                    plan.PrimaryDoctrineLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Fireline_Label", "Black Procession");
                    plan.PrimaryDoctrineSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Fireline_Summary", "Fireline doctrine. Expect ranged suppression, sniper picks, and denser support pressure behind the front line.");
                    break;
                case "aby_doctrine_hordegrinder":
                    plan.PrimaryDoctrineLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Grinder_Label", "Grinder Host");
                    plan.PrimaryDoctrineSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Grinder_Summary", "Breakthrough doctrine. Predicts heavier bruisers, harvest pressure, and slower but denser frontal collapse.");
                    break;
                case "aby_doctrine_hordesiege":
                    plan.PrimaryDoctrineLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Siege_Label", "Siege Liturgy");
                    plan.PrimaryDoctrineSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Siege_Summary", "Siege doctrine. Expects a rarer late-pressure composition with elite anchors and possible Idol-backed support.");
                    break;
                default:
                    plan.PrimaryDoctrineLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Unknown_Label", "Unshaped breach");
                    plan.PrimaryDoctrineSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordeDoctrine_Unknown_Summary", "Forecast doctrine could not be stabilized. Expect a mixed perimeter breach with no clean specialization.");
                    break;
            }
        }

        private static void ApplyPhasePresentation(HordePhasePlan phase, string phaseId)
        {
            if (phase == null)
            {
                return;
            }

            string safe = (phaseId ?? string.Empty).ToLowerInvariant();
            switch (safe)
            {
                case "marking":
                    phase.PhaseLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Marking_Label", "Breach marking");
                    phase.PhaseSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Marking_Summary", "Initial probing cuts. Lighter perimeter breaches map approach lanes and expose the colony before the full offensive commits.");
                    break;
                case "lattice":
                    phase.PhaseLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Lattice_Label", "Offensive lattice");
                    phase.PhaseSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Lattice_Summary", "Main offensive phase. Multiple fronts stay active and the doctrine profile stabilizes into the bulk of the attack.");
                    break;
                case "surge":
                    phase.PhaseLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Surge_Label", "Last surge");
                    phase.PhaseSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Surge_Summary", "Concentrated final drive. Fewer but denser portals accelerate elite or support pressure into a late push.");
                    break;
                case "collapse":
                    phase.PhaseLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Collapse_Label", "Collapse tail");
                    phase.PhaseSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Collapse_Summary", "Dissipation window. Remaining rupture fronts spit out a smaller tail while the breach starts folding shut.");
                    break;
                default:
                    phase.PhaseLabel = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Unknown_Label", "Unstable phase");
                    phase.PhaseSummary = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_HordePhase_Unknown_Summary", "Phase routing could not be stabilized. Expect an uneven multi-front breach.");
                    break;
            }
        }

        private static string BuildForecastText(HordePlan plan)
        {
            if (plan == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            AddCountLabel(parts, plan.GetCount("ABY_EmberHound"), "ABY_CirclePreview_Hound_Singular", "hound", "ABY_CirclePreview_Hound_Plural", "hounds");
            AddCountLabel(parts, plan.GetCount("ABY_RiftImp"), "ABY_CirclePreview_Imp_Singular", "imp", "ABY_CirclePreview_Imp_Plural", "imps");
            AddCountLabel(parts, plan.GetCount("ABY_HexgunThrall"), "ABY_CirclePreview_Thrall_Singular", "thrall", "ABY_CirclePreview_Thrall_Plural", "thralls");
            AddCountLabel(parts, plan.GetCount("ABY_ChainZealot"), "ABY_CirclePreview_Zealot_Singular", "zealot", "ABY_CirclePreview_Zealot_Plural", "zealots");
            AddCountLabel(parts, plan.GetCount("ABY_Harvester"), "ABY_CirclePreview_Harvester_Singular", "harvester", "ABY_CirclePreview_Harvester_Plural", "harvesters");
            AddCountLabel(parts, plan.GetCount("ABY_NullPriest"), "ABY_CirclePreview_Priest_Singular", "null priest", "ABY_CirclePreview_Priest_Plural", "null priests");
            AddCountLabel(parts, plan.GetCount("ABY_RiftSniper"), "ABY_CirclePreview_Sniper_Singular", "rift sniper", "ABY_CirclePreview_Sniper_Plural", "rift snipers");
            AddCountLabel(parts, plan.GetCount("ABY_BreachBrute"), "ABY_CirclePreview_Brute_Singular", "breach brute", "ABY_CirclePreview_Brute_Plural", "breach brutes");
            AddCountLabel(parts, plan.GetCount("ABY_HaloHusk"), "ABY_CirclePreview_HaloHusk_Singular", "halo husk", "ABY_CirclePreview_HaloHusk_Plural", "halo husks");
            AddCountLabel(parts, plan.GetCount("ABY_SiegeIdol"), "ABY_CirclePreview_Idol_Singular", "siege idol", "ABY_CirclePreview_Idol_Plural", "siege idols");

            return parts.Count == 0
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CirclePreview_None", "no hostiles")
                : string.Join(" + ", parts.ToArray());
        }

        private static void AddCountLabel(List<string> parts, int count, string singularKey, string singularFallback, string pluralKey, string pluralFallback)
        {
            if (parts == null || count <= 0)
            {
                return;
            }

            if (count == 1)
            {
                parts.Add(count + " " + AbyssalSummoningConsoleUtility.TranslateOrFallback(singularKey, singularFallback));
                return;
            }

            parts.Add(count + " " + AbyssalSummoningConsoleUtility.TranslateOrFallback(pluralKey, pluralFallback));
        }
    }
}
