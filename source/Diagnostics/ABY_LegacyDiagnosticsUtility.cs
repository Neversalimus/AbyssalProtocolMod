using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Read-only diagnostics for old or partially broken Abyssal Protocol save state.
    /// This class must remain diagnostic-only: do not destroy, despawn, move, or mutate gameplay objects here.
    /// </summary>
    public static class ABY_LegacyDiagnosticsUtility
    {
        private const string DominionSliceWorldObjectDefName = "ABY_DominionSliceSite";
        private const string DominionGateDefName = "ABY_DominionGate";
        private const string DominionPocketExitDefName = "ABY_DominionPocketExit";
        private const string DominionHeartDefName = "ABY_DominionSliceHeart";

        public sealed class DiagnosticsReport
        {
            public string Reason;
            public int MapCount;
            public int SessionCount;
            public int ActiveSessionCount;
            public int DominionWorldObjectCount;
            public int DominionPocketMapCount;
            public readonly List<string> Issues = new List<string>();
            public readonly List<string> Notes = new List<string>();

            public int IssueCount
            {
                get { return Issues != null ? Issues.Count : 0; }
            }

            public void AddIssue(string text)
            {
                if (!text.NullOrEmpty())
                {
                    Issues.Add(text);
                }
            }

            public void AddNote(string text)
            {
                if (!text.NullOrEmpty())
                {
                    Notes.Add(text);
                }
            }

            public string ToLogString()
            {
                List<string> lines = new List<string>();
                lines.Add("[Abyssal Protocol] Package 11A legacy diagnostics (diagnostic-only, no cleanup). Reason: " + (Reason ?? "unknown"));
                lines.Add("  Maps: " + MapCount + " | Dominion pocket maps: " + DominionPocketMapCount + " | Dominion world objects: " + DominionWorldObjectCount);
                lines.Add("  Sessions: " + SessionCount + " | Active sessions: " + ActiveSessionCount + " | Issues: " + IssueCount);

                if (Issues.Count > 0)
                {
                    lines.Add("  Potential issues:");
                    for (int i = 0; i < Issues.Count; i++)
                    {
                        lines.Add("   - " + Issues[i]);
                    }
                }

                if (Notes.Count > 0 && Prefs.DevMode)
                {
                    lines.Add("  Notes:");
                    for (int i = 0; i < Notes.Count; i++)
                    {
                        lines.Add("   - " + Notes[i]);
                    }
                }

                return string.Join("\n", lines.ToArray());
            }
        }

        public static DiagnosticsReport BuildReport(string reason)
        {
            DiagnosticsReport report = new DiagnosticsReport
            {
                Reason = reason ?? "manual"
            };

            try
            {
                report.MapCount = Find.Maps != null ? Find.Maps.Count : 0;
                List<ABY_DominionPocketSession> sessions = TryGetRuntimeSessions(out string sessionReadFail);
                if (!sessionReadFail.NullOrEmpty())
                {
                    report.AddIssue(sessionReadFail);
                }

                if (sessions != null)
                {
                    report.SessionCount = sessions.Count;
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        ABY_DominionPocketSession session = sessions[i];
                        DiagnoseSession(report, session, i);
                    }
                }
                else
                {
                    report.AddNote("No Dominion runtime session list found. This is normal if no Dominion content has been used on this save.");
                }

                DiagnoseDominionWorldObjects(report, sessions);
                DiagnoseDominionPocketMaps(report, sessions);
                DiagnoseMapLevelAbyssalState(report);
            }
            catch (Exception ex)
            {
                report.AddIssue("Diagnostics scanner threw an exception: " + ex.GetType().Name + " - " + ex.Message);
            }

            return report;
        }

        private static List<ABY_DominionPocketSession> TryGetRuntimeSessions(out string failReason)
        {
            failReason = null;
            ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
            if (runtime == null)
            {
                return null;
            }

            FieldInfo sessionsField = typeof(ABY_DominionPocketRuntimeGameComponent).GetField("sessions", BindingFlags.Instance | BindingFlags.NonPublic);
            if (sessionsField == null)
            {
                failReason = "Could not inspect Dominion runtime sessions: private field 'sessions' was not found.";
                return null;
            }

            object rawValue = sessionsField.GetValue(runtime);
            List<ABY_DominionPocketSession> sessions = rawValue as List<ABY_DominionPocketSession>;
            if (rawValue != null && sessions == null)
            {
                failReason = "Could not inspect Dominion runtime sessions: field 'sessions' has unexpected type " + rawValue.GetType().FullName + ".";
                return null;
            }

            return sessions;
        }

        private static void DiagnoseSession(DiagnosticsReport report, ABY_DominionPocketSession session, int index)
        {
            if (session == null)
            {
                report.AddIssue("Dominion session #" + index + " is null.");
                return;
            }

            string prefix = "Dominion session " + SafeSessionLabel(session, index) + ": ";
            if (session.active)
            {
                report.ActiveSessionCount++;
            }

            if (session.sessionId.NullOrEmpty())
            {
                report.AddIssue(prefix + "missing sessionId.");
            }

            Map sourceMap = AbyssalDominionPocketUtility.ResolveMap(session.sourceMapId);
            Map pocketMap = AbyssalDominionPocketUtility.ResolveMap(session.pocketMapId);

            if (session.active && sourceMap == null)
            {
                report.AddIssue(prefix + "active but source map id " + session.sourceMapId + " cannot be resolved.");
            }

            if (session.active && pocketMap == null)
            {
                report.AddIssue(prefix + "active but pocket map id " + session.pocketMapId + " cannot be resolved.");
            }

            if (!session.active && pocketMap != null)
            {
                report.AddIssue(prefix + "inactive but pocket map still exists (map id " + pocketMap.uniqueID + ").");
            }

            if (sourceMap != null && session.sourceGateThingId >= 0 && FindThingById(sourceMap, session.sourceGateThingId) == null)
            {
                report.AddNote(prefix + "source gate thing id " + session.sourceGateThingId + " was not found on source map. This can be safe if the gate was destroyed intentionally.");
            }

            if (pocketMap != null)
            {
                DiagnosePocketMapForSession(report, session, pocketMap, prefix);
            }

            if (session.victoryAchieved && !session.rewardSummary.NullOrEmpty())
            {
                report.AddNote(prefix + "victory already achieved; reward summary present.");
            }
        }

        private static void DiagnosePocketMapForSession(DiagnosticsReport report, ABY_DominionPocketSession session, Map pocketMap, string prefix)
        {
            Thing exit = null;
            try
            {
                exit = AbyssalDominionPocketUtility.ResolveExitThing(session, pocketMap);
            }
            catch (Exception ex)
            {
                report.AddIssue(prefix + "failed while resolving pocket exit: " + ex.GetType().Name + " - " + ex.Message);
            }

            if (session.active && exit == null)
            {
                report.AddIssue(prefix + "active pocket map has no resolvable Dominion exit.");
            }

            int playerPawnCount = 0;
            try
            {
                playerPawnCount = AbyssalDominionPocketUtility.GetPocketPlayerCount(pocketMap);
            }
            catch (Exception ex)
            {
                report.AddIssue(prefix + "failed while counting pocket player pawns: " + ex.GetType().Name + " - " + ex.Message);
            }

            if (session.active && playerPawnCount <= 0 && !session.victoryAchieved)
            {
                report.AddIssue(prefix + "active non-victory pocket has no player-controlled pawns.");
            }

            if (session.active && session.victoryAchieved && playerPawnCount <= 0)
            {
                report.AddNote(prefix + "victory pocket has no player pawns. This may be safe if extraction already occurred and cleanup is pending.");
            }

            int heartCount = CountThingsByDefName(pocketMap, DominionHeartDefName);
            if (session.active && !session.victoryAchieved && heartCount <= 0)
            {
                report.AddIssue(prefix + "active non-victory pocket has no Dominion Heart object.");
            }

            int exitCount = CountThingsByDefName(pocketMap, DominionPocketExitDefName);
            if (session.active && exitCount <= 0)
            {
                report.AddIssue(prefix + "active pocket map has no " + DominionPocketExitDefName + " thing.");
            }
        }

        private static void DiagnoseDominionWorldObjects(DiagnosticsReport report, List<ABY_DominionPocketSession> sessions)
        {
            if (Find.WorldObjects == null || Find.WorldObjects.AllWorldObjects == null)
            {
                return;
            }

            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < all.Count; i++)
            {
                WorldObject worldObject = all[i];
                if (!IsDominionSliceWorldObject(worldObject))
                {
                    continue;
                }

                report.DominionWorldObjectCount++;
                bool referencedBySession = IsWorldObjectReferencedByAnySession(worldObject, sessions);
                MapParent mapParent = worldObject as MapParent;
                bool hasMap = mapParent != null && mapParent.HasMap;

                if (!referencedBySession && !hasMap)
                {
                    report.AddIssue("Dominion slice world object at tile " + worldObject.Tile + " is not referenced by any known session and has no map.");
                }
                else if (!referencedBySession && hasMap)
                {
                    report.AddIssue("Dominion slice world object at tile " + worldObject.Tile + " has a map but is not referenced by any known session.");
                }
            }
        }

        private static void DiagnoseDominionPocketMaps(DiagnosticsReport report, List<ABY_DominionPocketSession> sessions)
        {
            if (Find.Maps == null)
            {
                return;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map == null || !IsDominionPocketMap(map))
                {
                    continue;
                }

                report.DominionPocketMapCount++;
                if (!IsPocketMapReferencedByAnySession(map, sessions))
                {
                    report.AddIssue("Dominion pocket map id " + map.uniqueID + " exists but is not referenced by any known session.");
                }
            }
        }

        private static void DiagnoseMapLevelAbyssalState(DiagnosticsReport report)
        {
            if (Find.Maps == null)
            {
                return;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map == null || map.listerThings == null)
                {
                    continue;
                }

                int looseDominionExits = 0;
                int looseDominionHearts = 0;
                if (!IsDominionPocketMap(map))
                {
                    looseDominionExits = CountThingsByDefName(map, DominionPocketExitDefName);
                    looseDominionHearts = CountThingsByDefName(map, DominionHeartDefName);
                }

                if (looseDominionExits > 0)
                {
                    report.AddIssue("Non-pocket map id " + map.uniqueID + " contains " + looseDominionExits + " Dominion pocket exit object(s).");
                }

                if (looseDominionHearts > 0)
                {
                    report.AddIssue("Non-pocket map id " + map.uniqueID + " contains " + looseDominionHearts + " Dominion Heart object(s).");
                }

                int gateCount = CountThingsByDefName(map, DominionGateDefName);
                if (gateCount > 0)
                {
                    report.AddNote("Map id " + map.uniqueID + " contains " + gateCount + " Dominion gate object(s).");
                }
            }
        }

        private static string SafeSessionLabel(ABY_DominionPocketSession session, int index)
        {
            if (session == null || session.sessionId.NullOrEmpty())
            {
                return "#" + index;
            }

            string id = session.sessionId;
            return id.Length > 8 ? id.Substring(0, 8) : id;
        }

        private static Thing FindThingById(Map map, int thingId)
        {
            if (map == null || map.listerThings == null || thingId < 0)
            {
                return null;
            }

            List<Thing> allThings = map.listerThings.AllThings;
            if (allThings == null)
            {
                return null;
            }

            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing != null && !thing.Destroyed && thing.thingIDNumber == thingId)
                {
                    return thing;
                }
            }

            return null;
        }

        private static int CountThingsByDefName(Map map, string defName)
        {
            if (map == null || map.listerThings == null || defName.NullOrEmpty())
            {
                return 0;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return 0;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(def);
            if (things == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing != null && !thing.Destroyed)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsDominionPocketMap(Map map)
        {
            if (map == null)
            {
                return false;
            }

            MapParent parent = map.Parent;
            return IsDominionSliceWorldObject(parent);
        }

        private static bool IsDominionSliceWorldObject(WorldObject worldObject)
        {
            if (worldObject == null)
            {
                return false;
            }

            if (worldObject.def != null && string.Equals(worldObject.def.defName, DominionSliceWorldObjectDefName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string typeName = worldObject.GetType().Name;
            return typeName != null && typeName.IndexOf("DominionSlice", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsWorldObjectReferencedByAnySession(WorldObject worldObject, List<ABY_DominionPocketSession> sessions)
        {
            if (worldObject == null || sessions == null)
            {
                return false;
            }

            MapParent mapParent = worldObject as MapParent;
            int mapId = mapParent != null && mapParent.HasMap && mapParent.Map != null ? mapParent.Map.uniqueID : -1;
            for (int i = 0; i < sessions.Count; i++)
            {
                ABY_DominionPocketSession session = sessions[i];
                if (session == null)
                {
                    continue;
                }

                if (session.sliceTile == worldObject.Tile)
                {
                    return true;
                }

                if (mapId >= 0 && session.pocketMapId == mapId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPocketMapReferencedByAnySession(Map map, List<ABY_DominionPocketSession> sessions)
        {
            if (map == null || sessions == null)
            {
                return false;
            }

            for (int i = 0; i < sessions.Count; i++)
            {
                ABY_DominionPocketSession session = sessions[i];
                if (session != null && session.pocketMapId == map.uniqueID)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
