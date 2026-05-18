using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Conservative cleanup for confirmed legacy/orphan save states.
    /// Rules:
    /// - Never touch Reactor Saint, Horde, active Dominion combat, normal player maps, or player pawns.
    /// - Never remove a map that contains player-controlled pawns.
    /// - Never remove an active non-victory Dominion session unless its map is already missing.
    /// - Prefer deactivating impossible session state over deleting gameplay objects.
    /// </summary>
    public static class ABY_LegacyCleanupUtility
    {
        private const string DominionSliceWorldObjectDefName = "ABY_DominionSliceSite";
        private const string DominionPocketExitDefName = "ABY_DominionPocketExit";
        private const string DominionHeartDefName = "ABY_DominionSliceHeart";

        public sealed class CleanupReport
        {
            public string Reason;
            public int SessionsInspected;
            public int MapsInspected;
            public int WorldObjectsInspected;
            public readonly List<string> Actions = new List<string>();
            public readonly List<string> Skipped = new List<string>();
            public readonly List<string> Errors = new List<string>();

            public int ActionCount
            {
                get { return Actions != null ? Actions.Count : 0; }
            }

            public void AddAction(string text)
            {
                if (!text.NullOrEmpty())
                {
                    Actions.Add(text);
                }
            }

            public void AddSkipped(string text)
            {
                if (!text.NullOrEmpty())
                {
                    Skipped.Add(text);
                }
            }

            public void AddError(string text)
            {
                if (!text.NullOrEmpty())
                {
                    Errors.Add(text);
                }
            }

            public string ToLogString()
            {
                List<string> lines = new List<string>();
                lines.Add("[Abyssal Protocol] Package 11B conservative legacy cleanup. Reason: " + (Reason ?? "unknown"));
                lines.Add("  Sessions inspected: " + SessionsInspected + " | Maps inspected: " + MapsInspected + " | World objects inspected: " + WorldObjectsInspected + " | Actions: " + ActionCount + " | Errors: " + Errors.Count);

                if (Actions.Count > 0)
                {
                    lines.Add("  Applied conservative cleanup:");
                    for (int i = 0; i < Actions.Count; i++)
                    {
                        lines.Add("   - " + Actions[i]);
                    }
                }

                if (Skipped.Count > 0 && Prefs.DevMode)
                {
                    lines.Add("  Skipped:");
                    for (int i = 0; i < Skipped.Count; i++)
                    {
                        lines.Add("   - " + Skipped[i]);
                    }
                }

                if (Errors.Count > 0)
                {
                    lines.Add("  Errors:");
                    for (int i = 0; i < Errors.Count; i++)
                    {
                        lines.Add("   - " + Errors[i]);
                    }
                }

                return string.Join("\n", lines.ToArray());
            }
        }

        public static CleanupReport RunConservativeCleanup(string reason)
        {
            CleanupReport report = new CleanupReport
            {
                Reason = reason ?? "manual"
            };

            try
            {
                List<ABY_DominionPocketSession> sessions = TryGetRuntimeSessions(out string sessionFailReason);
                if (!sessionFailReason.NullOrEmpty())
                {
                    report.AddError(sessionFailReason);
                }

                CleanupSessionList(report, sessions);
                CleanupOrphanDominionPocketMaps(report, sessions);
                CleanupOrphanDominionWorldObjects(report, sessions);
                CleanupLoosePocketOnlyThingsOnNormalMaps(report, sessions);
            }
            catch (Exception ex)
            {
                report.AddError("Cleanup scanner threw an exception: " + ex.GetType().Name + " - " + ex.Message);
            }

            return report;
        }

        private static void CleanupSessionList(CleanupReport report, List<ABY_DominionPocketSession> sessions)
        {
            if (sessions == null)
            {
                report.AddSkipped("No Dominion runtime session list found. Nothing to clean in session state.");
                return;
            }

            report.SessionsInspected = sessions.Count;
            for (int i = sessions.Count - 1; i >= 0; i--)
            {
                ABY_DominionPocketSession session = sessions[i];
                if (session == null)
                {
                    sessions.RemoveAt(i);
                    report.AddAction("Removed null Dominion runtime session entry.");
                    continue;
                }

                if (session.sessionId.NullOrEmpty())
                {
                    sessions.RemoveAt(i);
                    report.AddAction("Removed Dominion runtime session with missing sessionId.");
                    continue;
                }

                Map sourceMap = AbyssalDominionPocketUtility.ResolveMap(session.sourceMapId);
                Map pocketMap = AbyssalDominionPocketUtility.ResolveMap(session.pocketMapId);
                int pocketPlayerCount = SafeGetPocketPlayerCount(pocketMap);

                if (session.active && pocketMap == null)
                {
                    session.active = false;
                    session.cleanupQueued = false;
                    session.lastOutcomeReason = "Legacy cleanup: active session had no pocket map.";
                    report.AddAction("Deactivated Dominion session " + SafeSessionLabel(session, i) + " because its pocket map no longer exists.");
                    continue;
                }

                if (session.active && sourceMap == null && pocketMap != null)
                {
                    if (pocketPlayerCount <= 0)
                    {
                        session.active = false;
                        session.cleanupQueued = false;
                        session.lastOutcomeReason = "Legacy cleanup: source map missing and pocket empty.";
                        TryCollapsePocketMap(report, session, pocketMap, "active session with missing source map and empty pocket");
                    }
                    else
                    {
                        report.AddSkipped("Dominion session " + SafeSessionLabel(session, i) + " has missing source map but pocket still contains player pawn(s); left untouched.");
                    }

                    continue;
                }

                if (!session.active && pocketMap == null)
                {
                    sessions.RemoveAt(i);
                    report.AddAction("Removed inactive Dominion session " + SafeSessionLabel(session, i) + " with no remaining pocket map.");
                    continue;
                }

                if (!session.active && pocketMap != null)
                {
                    if (pocketPlayerCount <= 0)
                    {
                        TryCollapsePocketMap(report, session, pocketMap, "inactive session with empty pocket map");
                        sessions.RemoveAt(i);
                        report.AddAction("Removed inactive Dominion session " + SafeSessionLabel(session, i) + " after empty pocket cleanup.");
                    }
                    else
                    {
                        report.AddSkipped("Inactive Dominion session " + SafeSessionLabel(session, i) + " still has player pawn(s) in pocket map; left untouched.");
                    }
                }
            }
        }

        private static void CleanupOrphanDominionPocketMaps(CleanupReport report, List<ABY_DominionPocketSession> sessions)
        {
            if (Find.Maps == null)
            {
                return;
            }

            List<Map> maps = new List<Map>(Find.Maps);
            report.MapsInspected += maps.Count;
            for (int i = 0; i < maps.Count; i++)
            {
                Map map = maps[i];
                if (map == null || !IsDominionPocketMap(map))
                {
                    continue;
                }

                if (IsPocketMapReferencedByAnyActiveSession(map, sessions))
                {
                    continue;
                }

                int playerPawnCount = SafeGetPocketPlayerCount(map);
                if (playerPawnCount > 0)
                {
                    report.AddSkipped("Orphan Dominion pocket map id " + map.uniqueID + " contains player pawn(s); left untouched.");
                    continue;
                }

                if (TryRemoveMapSafely(map, true, out string failReason))
                {
                    report.AddAction("Removed orphan empty Dominion pocket map id " + map.uniqueID + ".");
                }
                else
                {
                    report.AddError("Failed to remove orphan Dominion pocket map id " + map.uniqueID + ": " + failReason);
                }
            }
        }

        private static void CleanupOrphanDominionWorldObjects(CleanupReport report, List<ABY_DominionPocketSession> sessions)
        {
            if (Find.WorldObjects == null || Find.WorldObjects.AllWorldObjects == null)
            {
                return;
            }

            List<WorldObject> worldObjects = new List<WorldObject>(Find.WorldObjects.AllWorldObjects);
            report.WorldObjectsInspected += worldObjects.Count;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                WorldObject worldObject = worldObjects[i];
                if (!IsDominionSliceWorldObject(worldObject))
                {
                    continue;
                }

                MapParent mapParent = worldObject as MapParent;
                bool hasMap = mapParent != null && mapParent.HasMap;
                if (IsWorldObjectReferencedByAnyActiveSession(worldObject, sessions))
                {
                    continue;
                }

                if (hasMap)
                {
                    Map map = mapParent.Map;
                    if (SafeGetPocketPlayerCount(map) > 0)
                    {
                        report.AddSkipped("Orphan Dominion world object at tile " + worldObject.Tile + " has a map with player pawn(s); left untouched.");
                        continue;
                    }

                    if (!TryRemoveMapSafely(map, true, out string mapFailReason))
                    {
                        report.AddError("Failed to remove map for orphan Dominion world object at tile " + worldObject.Tile + ": " + mapFailReason);
                        continue;
                    }

                    report.AddAction("Removed empty map for orphan Dominion world object at tile " + worldObject.Tile + ".");
                    continue;
                }

                if (TryRemoveWorldObjectSafely(worldObject, out string failReason))
                {
                    report.AddAction("Removed orphan Dominion slice world object at tile " + worldObject.Tile + " with no active session and no map.");
                }
                else
                {
                    report.AddError("Failed to remove orphan Dominion world object at tile " + worldObject.Tile + ": " + failReason);
                }
            }
        }

        private static void CleanupLoosePocketOnlyThingsOnNormalMaps(CleanupReport report, List<ABY_DominionPocketSession> sessions)
        {
            if (Find.Maps == null)
            {
                return;
            }

            ThingDef exitDef = DefDatabase<ThingDef>.GetNamedSilentFail(DominionPocketExitDefName);
            ThingDef heartDef = DefDatabase<ThingDef>.GetNamedSilentFail(DominionHeartDefName);
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map == null || map.listerThings == null || IsDominionPocketMap(map))
                {
                    continue;
                }

                RemoveLooseThingsOfDef(report, map, exitDef, "Dominion pocket exit on non-pocket map");
                RemoveLooseThingsOfDef(report, map, heartDef, "Dominion Heart on non-pocket map");
            }
        }

        private static void RemoveLooseThingsOfDef(CleanupReport report, Map map, ThingDef def, string reason)
        {
            if (map == null || map.listerThings == null || def == null)
            {
                return;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(def);
            if (things == null || things.Count == 0)
            {
                return;
            }

            List<Thing> snapshot = new List<Thing>(things);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Thing thing = snapshot[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                try
                {
                    thing.Destroy(DestroyMode.Vanish);
                    report.AddAction("Removed " + reason + " (thing id " + thing.thingIDNumber + ", map id " + map.uniqueID + ").");
                }
                catch (Exception ex)
                {
                    report.AddError("Failed to remove " + reason + " on map id " + map.uniqueID + ": " + ex.GetType().Name + " - " + ex.Message);
                }
            }
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

        private static int SafeGetPocketPlayerCount(Map map)
        {
            if (map == null)
            {
                return 0;
            }

            try
            {
                return AbyssalDominionPocketUtility.GetPocketPlayerCount(map);
            }
            catch
            {
                if (map.mapPawns == null || map.mapPawns.AllPawnsSpawned == null)
                {
                    return 0;
                }

                int count = 0;
                IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn != null && !pawn.Dead && pawn.Faction == Faction.OfPlayer)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private static void TryCollapsePocketMap(CleanupReport report, ABY_DominionPocketSession session, Map pocketMap, string reason)
        {
            if (session == null || pocketMap == null)
            {
                return;
            }

            if (SafeGetPocketPlayerCount(pocketMap) > 0)
            {
                report.AddSkipped("Skipped pocket cleanup for " + reason + " because the map still contains player pawn(s).");
                return;
            }

            try
            {
                AbyssalDominionPocketUtility.CollapsePocketSlice(session, pocketMap, true);
                report.AddAction("Collapsed empty Dominion pocket map for " + reason + ".");
            }
            catch (Exception ex)
            {
                report.AddError("Failed to collapse pocket map for " + reason + ": " + ex.GetType().Name + " - " + ex.Message);
            }
        }

        private static bool TryRemoveMapSafely(Map map, bool removeWorldObject, out string failReason)
        {
            failReason = null;
            if (map == null)
            {
                failReason = "map is null";
                return false;
            }

            if (SafeGetPocketPlayerCount(map) > 0)
            {
                failReason = "map contains player pawn(s)";
                return false;
            }

            try
            {
                MethodInfo method = typeof(Game).GetMethod("DeinitAndRemoveMap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                {
                    failReason = "Game.DeinitAndRemoveMap was not found";
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                object[] args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = parameters[i].ParameterType;
                    if (parameterType == typeof(Map))
                    {
                        args[i] = map;
                    }
                    else if (parameterType == typeof(bool))
                    {
                        args[i] = removeWorldObject;
                    }
                    else if (parameters[i].IsOptional)
                    {
                        args[i] = parameters[i].DefaultValue;
                    }
                    else if (parameterType.IsValueType)
                    {
                        args[i] = Activator.CreateInstance(parameterType);
                    }
                    else
                    {
                        args[i] = null;
                    }
                }

                method.Invoke(Current.Game, args);
                return true;
            }
            catch (Exception ex)
            {
                failReason = ex.GetType().Name + " - " + ex.Message;
                return false;
            }
        }

        private static bool TryRemoveWorldObjectSafely(WorldObject worldObject, out string failReason)
        {
            failReason = null;
            if (worldObject == null)
            {
                failReason = "world object is null";
                return false;
            }

            try
            {
                MethodInfo method = Find.WorldObjects.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(WorldObject) }, null);
                if (method == null)
                {
                    failReason = "WorldObjectsHolder.Remove(WorldObject) was not found";
                    return false;
                }

                method.Invoke(Find.WorldObjects, new object[] { worldObject });
                return true;
            }
            catch (Exception ex)
            {
                failReason = ex.GetType().Name + " - " + ex.Message;
                return false;
            }
        }

        private static bool IsDominionPocketMap(Map map)
        {
            if (map == null)
            {
                return false;
            }

            return IsDominionSliceWorldObject(map.Parent);
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

        private static bool IsPocketMapReferencedByAnyActiveSession(Map map, List<ABY_DominionPocketSession> sessions)
        {
            if (map == null || sessions == null)
            {
                return false;
            }

            for (int i = 0; i < sessions.Count; i++)
            {
                ABY_DominionPocketSession session = sessions[i];
                if (session != null && session.active && session.pocketMapId == map.uniqueID)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWorldObjectReferencedByAnyActiveSession(WorldObject worldObject, List<ABY_DominionPocketSession> sessions)
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
                if (session == null || !session.active)
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

        private static string SafeSessionLabel(ABY_DominionPocketSession session, int index)
        {
            if (session == null || session.sessionId.NullOrEmpty())
            {
                return "#" + index;
            }

            string id = session.sessionId;
            return id.Length > 8 ? id.Substring(0, 8) : id;
        }
    }
}
