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

        public sealed class HordePlan
        {
            public string RitualId;
            public int ColonistBand;
            public int WealthBand;
            public int Band;
            public int FrontCount;
            public int PulseCount;
            public float TotalBudget;
            public float AveragePulseBudget;
            public string ForecastText;
            public string PrimaryTemplateDefName;
            public string PrimaryDoctrineDefName;
            public string PrimaryDoctrineLabel;
            public string PrimaryDoctrineSummary;
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

            int primarySeed = BuildPreviewSeed(map, plan, 0);
            float primaryBudget = plan.AveragePulseBudget * GetPulseBudgetMultiplier(0, plan.PulseCount);
            AbyssalEncounterDirectorUtility.EncounterPlan primaryPulsePlan = AbyssalEncounterDirectorUtility.BuildPlan(
                EncounterPoolId,
                primaryBudget,
                BaseContentTier,
                map,
                primarySeed,
                null,
                null);

            if (primaryPulsePlan == null || primaryPulsePlan.TotalUnits <= 0)
            {
                primaryPulsePlan = BuildFallbackPulsePlan(map, primaryBudget);
            }

            if (primaryPulsePlan != null && primaryPulsePlan.TotalUnits > 0)
            {
                plan.PrimaryTemplateDefName = primaryPulsePlan.TemplateDefName ?? string.Empty;
                plan.PrimaryDoctrineDefName = primaryPulsePlan.DoctrineDefName ?? string.Empty;
                ApplyDoctrinePresentation(plan, plan.PrimaryDoctrineDefName);
                plan.PulsePlans.Add(primaryPulsePlan);
                MergeCounts(plan.TotalCounts, primaryPulsePlan);
            }

            for (int pulseIndex = plan.PulsePlans.Count; pulseIndex < plan.PulseCount; pulseIndex++)
            {
                float pulseBudget = plan.AveragePulseBudget * GetPulseBudgetMultiplier(pulseIndex, plan.PulseCount);
                int previewSeed = BuildPreviewSeed(map, plan, pulseIndex);
                AbyssalEncounterDirectorUtility.EncounterPlan pulsePlan = AbyssalEncounterDirectorUtility.BuildPlan(
                    EncounterPoolId,
                    pulseBudget,
                    BaseContentTier,
                    map,
                    previewSeed,
                    null,
                    null,
                    null,
                    null,
                    plan.PrimaryTemplateDefName,
                    plan.PrimaryDoctrineDefName);

                if (pulsePlan == null || pulsePlan.TotalUnits <= 0)
                {
                    pulsePlan = BuildFallbackPulsePlan(map, pulseBudget);
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

                plan.PulsePlans.Add(pulsePlan);
                MergeCounts(plan.TotalCounts, pulsePlan);
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
                    TotalBudget = 0f,
                    AveragePulseBudget = 0f,
                    ForecastText = AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_CirclePreview_None", "no hostiles")
                };

                fallbackPlan.PulsePlans.Add(BuildFallbackPulsePlan(map, 1800f));
                if (fallbackPlan.PulsePlans[0] != null)
                {
                    MergeCounts(fallbackPlan.TotalCounts, fallbackPlan.PulsePlans[0]);
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
                "Sigils on map: {0}   •   Pattern: {1}   •   Fronts: {2}   •   Pulses: {3}   •   Forecast: {4}   •   Protocol: {5}",
                sigilCount,
                GetDoctrineLabel(plan),
                plan.FrontCount,
                plan.PulseCount,
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

        private static float GetPulseBudgetMultiplier(int pulseIndex, int totalPulses)
        {
            if (totalPulses <= 1)
            {
                return 1f;
            }

            float midpoint = (totalPulses - 1) * 0.5f;
            float distance = Mathf.Abs(pulseIndex - midpoint);
            float normalized = midpoint <= 0.01f ? 0f : distance / midpoint;
            return Mathf.Lerp(1.06f, 0.94f, normalized);
        }

        private static int BuildPreviewSeed(Map map, HordePlan plan, int pulseIndex)
        {
            int seed = 278123;
            seed = Gen.HashCombineInt(seed, map != null ? map.uniqueID : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.Band : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.ColonistBand : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.WealthBand : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.FrontCount : 0);
            seed = Gen.HashCombineInt(seed, plan != null ? plan.PulseCount : 0);
            seed = Gen.HashCombineInt(seed, pulseIndex);
            seed = Gen.HashCombineInt(seed, AbyssalDifficultyUtility.GetCurrentProfileOrder());
            seed = Gen.HashCombineInt(seed, AbyssalDifficultyUtility.GetProgressionStage(map));
            seed = Gen.HashCombineInt(seed, BaseContentTier);
            seed = Gen.HashCombineInt(seed, RitualId.GetHashCode());
            return seed;
        }

        private static AbyssalEncounterDirectorUtility.EncounterPlan BuildFallbackPulsePlan(Map map, float pulseBudget)
        {
            AbyssalEncounterDirectorUtility.EncounterPlan plan = new AbyssalEncounterDirectorUtility.EncounterPlan
            {
                PoolId = EncounterPoolId,
                Budget = pulseBudget,
                AllowedContentTier = BaseContentTier
            };

            AddFallbackEntry(plan, "ABY_RiftImp", Mathf.Clamp(Mathf.RoundToInt(pulseBudget / 780f), 3, 10), "assault");
            AddFallbackEntry(plan, "ABY_EmberHound", Mathf.Clamp(Mathf.RoundToInt(pulseBudget / 1200f), 1, 5), "assault");
            AddFallbackEntry(plan, "ABY_HexgunThrall", Mathf.Clamp(Mathf.RoundToInt(pulseBudget / 1650f), 1, 3), "support");
            AddFallbackEntry(plan, "ABY_ChainZealot", Mathf.Clamp(Mathf.RoundToInt(pulseBudget / 1900f), 1, 3), "elite");
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
