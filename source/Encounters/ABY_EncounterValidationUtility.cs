using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public enum ABY_EncounterValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class ABY_EncounterValidationIssue
    {
        public ABY_EncounterValidationSeverity Severity;
        public string Source;
        public string Message;

        public string FormatLine()
        {
            return "[" + Severity + "] " + (Source ?? "EncounterData") + ": " + (Message ?? string.Empty);
        }
    }

    public sealed class ABY_EncounterValidationReport
    {
        public readonly List<ABY_EncounterValidationIssue> Issues = new List<ABY_EncounterValidationIssue>();

        public int WarningCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i] != null && Issues[i].Severity == ABY_EncounterValidationSeverity.Warning)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int ErrorCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i] != null && Issues[i].Severity == ABY_EncounterValidationSeverity.Error)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int InfoCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Issues.Count; i++)
                {
                    if (Issues[i] != null && Issues[i].Severity == ABY_EncounterValidationSeverity.Info)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasProblems => WarningCount > 0 || ErrorCount > 0;

        public string BuildSummaryLine()
        {
            return "Encounter data validation: " + ErrorCount + " errors, " + WarningCount + " warnings, " + InfoCount + " notes";
        }

        public List<string> BuildLines(int maxIssues = 80)
        {
            List<string> lines = new List<string>();
            lines.Add(BuildSummaryLine());
            int limit = Math.Max(0, maxIssues);
            int shown = 0;
            for (int i = 0; i < Issues.Count && shown < limit; i++)
            {
                ABY_EncounterValidationIssue issue = Issues[i];
                if (issue == null)
                {
                    continue;
                }

                lines.Add(issue.FormatLine());
                shown++;
            }

            int remaining = Math.Max(0, Issues.Count - shown);
            if (remaining > 0)
            {
                lines.Add("..." + remaining + " additional encounter validation issues omitted from this view.");
            }

            return lines;
        }

        public string BuildPlainText(int maxIssues = 120)
        {
            StringBuilder builder = new StringBuilder();
            List<string> lines = BuildLines(maxIssues);
            for (int i = 0; i < lines.Count; i++)
            {
                builder.AppendLine(lines[i]);
            }

            return builder.ToString();
        }
    }

    public static class ABY_EncounterValidationUtility
    {
        private const int StartupThrottleTicks = 60000;
        private static ABY_EncounterValidationReport cachedReport;
        private static int cachedReportTick = -999999;

        public static ABY_EncounterValidationReport GetReport(bool forceRefresh = false)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (!forceRefresh && cachedReport != null && Math.Abs(now - cachedReportTick) < 2500)
            {
                return cachedReport;
            }

            cachedReport = BuildReport();
            cachedReportTick = now;
            return cachedReport;
        }

        public static List<string> BuildStatusLines(bool forceRefresh = false, int maxIssues = 24)
        {
            ABY_EncounterValidationReport report = GetReport(forceRefresh);
            return report != null ? report.BuildLines(maxIssues) : new List<string> { "Encounter data validation: unavailable" };
        }

        public static void LogStartupValidationIfEnabled()
        {
            try
            {
                if (!(AbyssalProtocolMod.Settings?.enableEncounterDataValidation ?? true))
                {
                    return;
                }

                ABY_EncounterValidationReport report = GetReport(true);
                if (report == null)
                {
                    return;
                }

                if (report.ErrorCount > 0)
                {
                    ABY_LogThrottleUtility.Warning("encounter-validation-errors", "[Abyssal Protocol] " + report.BuildPlainText(40).Replace("\n", " | "), StartupThrottleTicks);
                }
                else if (report.WarningCount > 0)
                {
                    ABY_LogThrottleUtility.Warning("encounter-validation-warnings", "[Abyssal Protocol] " + report.BuildPlainText(40).Replace("\n", " | "), StartupThrottleTicks);
                }
                else
                {
                    ABY_StabilityDiagnosticsUtility.Verbose("encounter-validation-clean", report.BuildSummaryLine(), StartupThrottleTicks);
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("encounter-validation-failed", "[Abyssal Protocol] Encounter validation failed: " + ex.GetType().Name + ": " + ex.Message, StartupThrottleTicks);
            }
        }

        public static void LogValidationSnapshot(bool force = false)
        {
            try
            {
                ABY_EncounterValidationReport report = GetReport(true);
                if (report == null)
                {
                    return;
                }

                if (force || report.HasProblems)
                {
                    ABY_LogThrottleUtility.Message("encounter-validation-snapshot", "[Abyssal Protocol] " + report.BuildPlainText(80).Replace("\n", " | "), force ? 1 : 6000);
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("encounter-validation-snapshot-failed", "[Abyssal Protocol] Encounter validation snapshot failed: " + ex.GetType().Name + ": " + ex.Message, 6000);
            }
        }

        private static ABY_EncounterValidationReport BuildReport()
        {
            ABY_EncounterValidationReport report = new ABY_EncounterValidationReport();
            HashSet<string> knownPools = BuildKnownPoolSet();
            HashSet<string> knownRoles = BuildKnownRoleSet();

            ValidatePawnKindScaling(report, knownPools);
            ValidateTemplates(report, knownPools, knownRoles);
            ValidateDoctrines(report, knownPools, knownRoles);
            ValidateEscalationPackages(report, knownPools, knownRoles);
            ValidatePoolCoverage(report, knownPools);

            if (report.Issues.Count == 0)
            {
                Add(report, ABY_EncounterValidationSeverity.Info, "EncounterData", "No structural encounter data issues found.");
            }

            return report;
        }

        private static HashSet<string> BuildKnownPoolSet()
        {
            HashSet<string> pools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<ABY_EncounterTemplateDef> templates = DefDatabase<ABY_EncounterTemplateDef>.AllDefsListForReading;
            for (int i = 0; i < templates.Count; i++)
            {
                string poolId = templates[i]?.poolId;
                if (!poolId.NullOrEmpty())
                {
                    pools.Add(poolId);
                }
            }

            List<PawnKindDef> pawns = DefDatabase<PawnKindDef>.AllDefsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                DefModExtension_AbyssalDifficultyScaling ext = pawns[i]?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
                if (ext?.encounterPools == null)
                {
                    continue;
                }

                for (int j = 0; j < ext.encounterPools.Count; j++)
                {
                    string poolId = ext.encounterPools[j];
                    if (!poolId.NullOrEmpty())
                    {
                        pools.Add(poolId);
                    }
                }
            }

            List<ABY_ThreatDoctrineDef> doctrines = DefDatabase<ABY_ThreatDoctrineDef>.AllDefsListForReading;
            for (int i = 0; i < doctrines.Count; i++)
            {
                if (doctrines[i]?.poolIds == null)
                {
                    continue;
                }

                for (int j = 0; j < doctrines[i].poolIds.Count; j++)
                {
                    string poolId = doctrines[i].poolIds[j];
                    if (!poolId.NullOrEmpty())
                    {
                        pools.Add(poolId);
                    }
                }
            }

            return pools;
        }

        private static HashSet<string> BuildKnownRoleSet()
        {
            HashSet<string> roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<PawnKindDef> pawns = DefDatabase<PawnKindDef>.AllDefsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                DefModExtension_AbyssalDifficultyScaling ext = pawns[i]?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
                if (ext != null && !ext.role.NullOrEmpty())
                {
                    roles.Add(ext.role);
                }
            }

            roles.Add("assault");
            roles.Add("support");
            roles.Add("elite");
            roles.Add("swarm");
            roles.Add("flanker");
            roles.Add("siege");
            roles.Add("boss");
            return roles;
        }

        private static void ValidatePawnKindScaling(ABY_EncounterValidationReport report, HashSet<string> knownPools)
        {
            List<PawnKindDef> pawns = DefDatabase<PawnKindDef>.AllDefsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                PawnKindDef pawn = pawns[i];
                DefModExtension_AbyssalDifficultyScaling ext = pawn?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
                if (pawn == null || ext == null)
                {
                    continue;
                }

                string source = "PawnKindDef " + pawn.defName;
                if (ext.contentTier < 1)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "contentTier is below 1; encounter tier filtering may behave unexpectedly.");
                }

                if (ext.role.NullOrEmpty())
                {
                    Add(report, ABY_EncounterValidationSeverity.Error, source, "role is empty.");
                }

                if (ext.budgetCost <= 0f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Error, source, "budgetCost must be greater than 0.");
                }
                else if (ext.budgetCost < 10f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "budgetCost is very low; verify this is intentional.");
                }

                if (ext.selectionWeight <= 0f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "selectionWeight is <= 0; the director will clamp it but XML should be explicit.");
                }

                if (!DifficultyProfileExists(ext.difficultyFloorDefName))
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "difficultyFloorDefName references missing difficulty profile '" + ext.difficultyFloorDefName + "'.");
                }

                if (ext.encounterPools == null || ext.encounterPools.Count == 0)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "has difficulty scaling but no encounterPools; it will never be auto-selected by the directed encounter planner.");
                }
                else
                {
                    for (int p = 0; p < ext.encounterPools.Count; p++)
                    {
                        string pool = ext.encounterPools[p];
                        if (pool.NullOrEmpty())
                        {
                            Add(report, ABY_EncounterValidationSeverity.Warning, source, "contains an empty encounter pool id.");
                        }
                        else if (knownPools != null && !knownPools.Contains(pool))
                        {
                            Add(report, ABY_EncounterValidationSeverity.Info, source, "uses pool '" + pool + "' that is not referenced by current templates/doctrines. This is safe if reserved for future content.");
                        }
                    }
                }
            }
        }

        private static void ValidateTemplates(ABY_EncounterValidationReport report, HashSet<string> knownPools, HashSet<string> knownRoles)
        {
            List<ABY_EncounterTemplateDef> templates = DefDatabase<ABY_EncounterTemplateDef>.AllDefsListForReading;
            for (int i = 0; i < templates.Count; i++)
            {
                ABY_EncounterTemplateDef template = templates[i];
                if (template == null)
                {
                    continue;
                }

                string source = "EncounterTemplateDef " + template.defName;
                if (template.poolId.NullOrEmpty())
                {
                    Add(report, ABY_EncounterValidationSeverity.Error, source, "poolId is empty.");
                }

                if (template.minBaseContentTier > template.maxBaseContentTier)
                {
                    Add(report, ABY_EncounterValidationSeverity.Error, source, "minBaseContentTier is greater than maxBaseContentTier.");
                }

                if (template.selectionWeight <= 0f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "selectionWeight is <= 0; template will be clamped but should be explicit.");
                }

                if (template.budgetMultiplier <= 0f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "budgetMultiplier is <= 0; plan builder clamps it but XML should be explicit.");
                }

                if (template.maxSameKindCount <= 0)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "maxSameKindCount is <= 0; plan builder clamps it to 1.");
                }

                if (!DifficultyProfileExists(template.difficultyFloorDefName))
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "difficultyFloorDefName references missing difficulty profile '" + template.difficultyFloorDefName + "'.");
                }

                if (!template.poolId.NullOrEmpty() && !PoolHasPawnCandidates(template.poolId))
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "pool '" + template.poolId + "' has no PawnKindDef candidates with DefModExtension_AbyssalDifficultyScaling.");
                }

                ValidateRoleCounts(report, source, "minimumRoleCounts", template.minimumRoleCounts, knownRoles);
                ValidateRoleCounts(report, source, "maximumRoleCounts", template.maximumRoleCounts, knownRoles);
                ValidateRoleWeights(report, source, template.roleWeightMultipliers, knownRoles);
            }
        }

        private static void ValidateDoctrines(ABY_EncounterValidationReport report, HashSet<string> knownPools, HashSet<string> knownRoles)
        {
            List<ABY_ThreatDoctrineDef> doctrines = DefDatabase<ABY_ThreatDoctrineDef>.AllDefsListForReading;
            for (int i = 0; i < doctrines.Count; i++)
            {
                ABY_ThreatDoctrineDef doctrine = doctrines[i];
                if (doctrine == null)
                {
                    continue;
                }

                string source = "ThreatDoctrineDef " + doctrine.defName;
                if (doctrine.poolIds == null || doctrine.poolIds.Count == 0)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "poolIds is empty; doctrine can never be selected.");
                }
                else
                {
                    for (int p = 0; p < doctrine.poolIds.Count; p++)
                    {
                        string pool = doctrine.poolIds[p];
                        if (pool.NullOrEmpty())
                        {
                            Add(report, ABY_EncounterValidationSeverity.Warning, source, "contains an empty pool id.");
                        }
                        else if (knownPools != null && !knownPools.Contains(pool))
                        {
                            Add(report, ABY_EncounterValidationSeverity.Warning, source, "references unknown pool '" + pool + "'.");
                        }
                    }
                }

                if (doctrine.minProgressionStage > doctrine.maxProgressionStage)
                {
                    Add(report, ABY_EncounterValidationSeverity.Error, source, "minProgressionStage is greater than maxProgressionStage.");
                }

                if (doctrine.selectionWeight <= 0f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "selectionWeight is <= 0; doctrine will be clamped but should be explicit.");
                }

                if (doctrine.budgetMultiplier <= 0f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "budgetMultiplier is <= 0; plan builder clamps it but XML should be explicit.");
                }

                if (!DifficultyProfileExists(doctrine.difficultyFloorDefName))
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "difficultyFloorDefName references missing difficulty profile '" + doctrine.difficultyFloorDefName + "'.");
                }

                ValidateBossProfileRefs(report, source, doctrine.allowedBossProfileDefNames);
                ValidateRoleCounts(report, source, "minimumRoleCounts", doctrine.minimumRoleCounts, knownRoles);
                ValidateRoleCounts(report, source, "maximumRoleCounts", doctrine.maximumRoleCounts, knownRoles);
                ValidateRoleWeights(report, source, doctrine.roleWeightMultipliers, knownRoles);
            }
        }

        private static void ValidateEscalationPackages(ABY_EncounterValidationReport report, HashSet<string> knownPools, HashSet<string> knownRoles)
        {
            List<ABY_BossEscalationPackageDef> packages = DefDatabase<ABY_BossEscalationPackageDef>.AllDefsListForReading;
            for (int i = 0; i < packages.Count; i++)
            {
                ABY_BossEscalationPackageDef package = packages[i];
                if (package == null)
                {
                    continue;
                }

                string source = "BossEscalationPackageDef " + package.defName;
                if (package.minProgressionStage > package.maxProgressionStage)
                {
                    Add(report, ABY_EncounterValidationSeverity.Error, source, "minProgressionStage is greater than maxProgressionStage.");
                }

                if (package.selectionWeight <= 0f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "selectionWeight is <= 0; package selection will be clamped but should be explicit.");
                }

                if (package.escortBudgetMultiplier <= 0f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "escortBudgetMultiplier is <= 0; verify intended escort pressure.");
                }

                if (!DifficultyProfileExists(package.difficultyFloorDefName))
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "difficultyFloorDefName references missing difficulty profile '" + package.difficultyFloorDefName + "'.");
                }

                if (!package.difficultyCeilingDefName.NullOrEmpty() && !DifficultyProfileExists(package.difficultyCeilingDefName))
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "difficultyCeilingDefName references missing difficulty profile '" + package.difficultyCeilingDefName + "'.");
                }

                if (!package.escortPoolIdOverride.NullOrEmpty() && knownPools != null && !knownPools.Contains(package.escortPoolIdOverride))
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "escortPoolIdOverride references unknown pool '" + package.escortPoolIdOverride + "'.");
                }

                if (!package.reinforcementPoolIdOverride.NullOrEmpty() && knownPools != null && !knownPools.Contains(package.reinforcementPoolIdOverride))
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "reinforcementPoolIdOverride references unknown pool '" + package.reinforcementPoolIdOverride + "'.");
                }

                ValidateBossProfileRefs(report, source, package.allowedBossProfileDefNames);
                ValidateDoctrineRefs(report, source, "preferredDoctrineDefNames", package.preferredDoctrineDefNames);
                ValidateDoctrineRefs(report, source, "secondaryDoctrineDefNames", package.secondaryDoctrineDefNames);
                ValidateRoleCounts(report, source, "minimumRoleCounts", package.minimumRoleCounts, knownRoles);
                ValidateRoleCounts(report, source, "maximumRoleCounts", package.maximumRoleCounts, knownRoles);
            }
        }

        private static void ValidatePoolCoverage(ABY_EncounterValidationReport report, HashSet<string> knownPools)
        {
            if (knownPools == null || knownPools.Count == 0)
            {
                Add(report, ABY_EncounterValidationSeverity.Warning, "EncounterData", "No encounter pools found in templates, doctrines, or pawn scaling extensions.");
                return;
            }

            foreach (string poolId in knownPools)
            {
                if (poolId.NullOrEmpty())
                {
                    continue;
                }

                bool hasTemplate = PoolHasTemplate(poolId);
                bool hasCandidate = PoolHasPawnCandidates(poolId);
                if (hasTemplate && !hasCandidate)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, "EncounterPool " + poolId, "has templates but no pawn candidates.");
                }
                else if (!hasTemplate && hasCandidate)
                {
                    Add(report, ABY_EncounterValidationSeverity.Info, "EncounterPool " + poolId, "has pawn candidates but no template. This is safe for manually forced or future pools.");
                }
            }
        }

        private static void ValidateRoleCounts(ABY_EncounterValidationReport report, string source, string fieldName, List<ABY_EncounterTemplateRoleCount> counts, HashSet<string> knownRoles)
        {
            if (counts == null)
            {
                return;
            }

            for (int i = 0; i < counts.Count; i++)
            {
                ABY_EncounterTemplateRoleCount entry = counts[i];
                if (entry == null)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, fieldName + " contains a null entry.");
                    continue;
                }

                if (entry.role.NullOrEmpty())
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, fieldName + " contains an empty role.");
                }
                else if (knownRoles != null && !knownRoles.Contains(entry.role))
                {
                    Add(report, ABY_EncounterValidationSeverity.Info, source, fieldName + " references role '" + entry.role + "' with no current PawnKind candidates. This is safe if reserved for future content.");
                }

                if (entry.count < 0)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, fieldName + " has a negative count for role '" + entry.role + "'.");
                }
            }
        }

        private static void ValidateRoleWeights(ABY_EncounterValidationReport report, string source, List<ABY_EncounterTemplateRoleWeight> weights, HashSet<string> knownRoles)
        {
            if (weights == null)
            {
                return;
            }

            for (int i = 0; i < weights.Count; i++)
            {
                ABY_EncounterTemplateRoleWeight entry = weights[i];
                if (entry == null)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "roleWeightMultipliers contains a null entry.");
                    continue;
                }

                if (entry.role.NullOrEmpty())
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "roleWeightMultipliers contains an empty role.");
                }
                else if (knownRoles != null && !knownRoles.Contains(entry.role))
                {
                    Add(report, ABY_EncounterValidationSeverity.Info, source, "roleWeightMultipliers references role '" + entry.role + "' with no current PawnKind candidates. This is safe if reserved for future content.");
                }

                if (entry.multiplier <= 0f)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "roleWeightMultipliers has non-positive multiplier for role '" + entry.role + "'.");
                }
            }
        }

        private static void ValidateBossProfileRefs(ABY_EncounterValidationReport report, string source, List<string> profileNames)
        {
            if (profileNames == null)
            {
                return;
            }

            for (int i = 0; i < profileNames.Count; i++)
            {
                string defName = profileNames[i];
                if (defName.NullOrEmpty())
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "contains an empty boss profile reference.");
                }
                else if (DefDatabase<ABY_BossDifficultyProfileDef>.GetNamedSilentFail(defName) == null)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, "references missing boss profile '" + defName + "'.");
                }
            }
        }

        private static void ValidateDoctrineRefs(ABY_EncounterValidationReport report, string source, string fieldName, List<string> doctrineNames)
        {
            if (doctrineNames == null)
            {
                return;
            }

            for (int i = 0; i < doctrineNames.Count; i++)
            {
                string defName = doctrineNames[i];
                if (defName.NullOrEmpty())
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, fieldName + " contains an empty doctrine reference.");
                }
                else if (DefDatabase<ABY_ThreatDoctrineDef>.GetNamedSilentFail(defName) == null)
                {
                    Add(report, ABY_EncounterValidationSeverity.Warning, source, fieldName + " references missing doctrine '" + defName + "'.");
                }
            }
        }

        private static bool DifficultyProfileExists(string defName)
        {
            if (defName.NullOrEmpty())
            {
                return false;
            }

            return DefDatabase<ABY_DifficultyProfileDef>.GetNamedSilentFail(defName) != null;
        }

        private static bool PoolHasTemplate(string poolId)
        {
            if (poolId.NullOrEmpty())
            {
                return false;
            }

            List<ABY_EncounterTemplateDef> templates = DefDatabase<ABY_EncounterTemplateDef>.AllDefsListForReading;
            for (int i = 0; i < templates.Count; i++)
            {
                ABY_EncounterTemplateDef template = templates[i];
                if (template != null && string.Equals(template.poolId ?? string.Empty, poolId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PoolHasPawnCandidates(string poolId)
        {
            if (poolId.NullOrEmpty())
            {
                return false;
            }

            List<PawnKindDef> pawns = DefDatabase<PawnKindDef>.AllDefsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                DefModExtension_AbyssalDifficultyScaling ext = pawns[i]?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
                if (ext?.encounterPools == null)
                {
                    continue;
                }

                for (int p = 0; p < ext.encounterPools.Count; p++)
                {
                    if (string.Equals(ext.encounterPools[p] ?? string.Empty, poolId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void Add(ABY_EncounterValidationReport report, ABY_EncounterValidationSeverity severity, string source, string message)
        {
            if (report == null)
            {
                return;
            }

            report.Issues.Add(new ABY_EncounterValidationIssue
            {
                Severity = severity,
                Source = source ?? "EncounterData",
                Message = message ?? string.Empty
            });
        }
    }
}
