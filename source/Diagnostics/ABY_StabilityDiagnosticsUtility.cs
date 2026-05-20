using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_StabilityDiagnosticsUtility
    {
        public const string PackageTag = "StabilityDiagnostics-2026-05-11";

        public static bool DiagnosticsEnabled => AbyssalProtocolMod.Settings?.enableStabilityDiagnostics ?? false;
        public static bool VerboseDiagnostics => AbyssalProtocolMod.Settings?.verboseDiagnostics ?? false;
        public static bool ShowDebugInspectStrings => AbyssalProtocolMod.Settings?.showDebugInspectStrings ?? false;
        public static bool UIPolishEnabled => AbyssalProtocolMod.Settings?.enableUIPolish ?? true;

        public static void ReportStartupSnapshot()
        {
            try
            {
                ABY_HarmonyPatchReportUtility.LogReport(false);
            }
            catch
            {
            }
        }

        public static List<string> BuildStatusLines()
        {
            List<string> lines = new List<string>();
            lines.AddRange(ABY_HarmonyPatchReportUtility.BuildReportLines());
            lines.Add("Diagnostics enabled: " + DiagnosticsEnabled);
            lines.Add("Verbose diagnostics: " + VerboseDiagnostics);
            lines.Add("Debug inspect strings: " + ShowDebugInspectStrings);
            lines.Add("UI polish: " + UIPolishEnabled);
            lines.Add("Repeated warning throttle: " + (AbyssalProtocolMod.Settings?.suppressRepeatedWarnings ?? true));
            lines.Add("Weapon charge sounds: " + (AbyssalProtocolMod.Settings?.enableWeaponChargeSounds ?? false));
            lines.Add("Encounter data validation: " + (AbyssalProtocolMod.Settings?.enableEncounterDataValidation ?? true));
            lines.Add("Encounter shadow planning: " + (AbyssalProtocolMod.Settings?.enableEncounterShadowPlanning ?? false));
            lines.Add("Maps loaded: " + (Find.Maps != null ? Find.Maps.Count.ToString() : "0"));

            AppendEncounterValidationLines(lines);
            AppendBossLines(lines);
            AppendDashLines(lines);
            return lines;
        }

        public static string BuildPlainTextReport()
        {
            List<string> lines = BuildStatusLines();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                builder.AppendLine(lines[i]);
            }
            return builder.ToString();
        }

        public static void LogSnapshot(bool force = false)
        {
            try
            {
                if (!force && !DiagnosticsEnabled)
                {
                    return;
                }

                ABY_LogThrottleUtility.Message("diagnostics-snapshot", "[Abyssal Protocol] Diagnostics snapshot: " + BuildPlainTextReport().Replace("\n", " | "), force ? 1 : 5000);
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("diagnostics-snapshot-error", "[Abyssal Protocol] Diagnostics snapshot failed: " + ex.GetType().Name + ": " + ex.Message, 5000);
            }
        }

        public static void Verbose(string key, string message, int throttleTicks = 1800)
        {
            if (!VerboseDiagnostics)
            {
                return;
            }

            ABY_LogThrottleUtility.Message("verbose-" + key, "[Abyssal Protocol] " + message, throttleTicks);
        }

        public static string FormatPawnLabel(Pawn pawn)
        {
            try
            {
                if (pawn == null)
                {
                    return "null pawn";
                }

                return pawn.LabelShortCap + " (" + pawn.def?.defName + ")";
            }
            catch
            {
                return "pawn";
            }
        }

        private static void AppendEncounterValidationLines(List<string> lines)
        {
            try
            {
                List<string> validationLines = ABY_EncounterValidationUtility.BuildStatusLines(false, 12);
                for (int i = 0; i < validationLines.Count; i++)
                {
                    lines.Add(validationLines[i]);
                }
            }
            catch (Exception ex)
            {
                lines.Add("Encounter validation diagnostics failed: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void AppendBossLines(List<string> lines)
        {
            int bossCount = 0;
            try
            {
                if (Find.Maps == null)
                {
                    return;
                }

                for (int m = 0; m < Find.Maps.Count; m++)
                {
                    Map map = Find.Maps[m];
                    if (map?.mapPawns?.AllPawnsSpawned == null)
                    {
                        continue;
                    }

                    IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                    for (int i = 0; i < pawns.Count; i++)
                    {
                        Pawn pawn = pawns[i];
                        CompABY_BossTrueDeath comp = pawn?.TryGetComp<CompABY_BossTrueDeath>();
                        if (comp == null)
                        {
                            continue;
                        }

                        bossCount++;
                        lines.Add("Boss TrueDeath: " + FormatPawnLabel(pawn) + " hp=" + comp.CurrentBossHitPoints.ToString("0.#") + "/" + comp.MaxBossHitPoints.ToString("0.#") + " authorizedDeath=" + comp.DeathAuthorized);
                    }
                }
            }
            catch (Exception ex)
            {
                lines.Add("Boss diagnostics failed: " + ex.GetType().Name + ": " + ex.Message);
            }

            if (bossCount == 0)
            {
                lines.Add("Boss TrueDeath: no active bosses found");
            }
        }

        private static void AppendDashLines(List<string> lines)
        {
            try
            {
                int active = 0;
                if (Find.Maps != null)
                {
                    for (int i = 0; i < Find.Maps.Count; i++)
                    {
                        MapComponent_ABY_AbyssalDashRuntime component = Find.Maps[i]?.GetComponent<MapComponent_ABY_AbyssalDashRuntime>();
                        if (component != null)
                        {
                            active += component.ActiveDashCount;
                        }
                    }
                }

                lines.Add("Active abyssal dashes: " + active);
            }
            catch (Exception ex)
            {
                lines.Add("Dash diagnostics failed: " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
