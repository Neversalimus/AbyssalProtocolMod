using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionPocketUtility
    {
        private const string ExitThingDefName = "ABY_DominionPocketExit";
        private const string SliceWorldObjectDefName = "ABY_DominionSliceSite";
        private const int SliceMapWidth = 120;
        private const int SliceMapHeight = 120;

        public static List<Pawn> GetSelectedColonistsForPocketEntry(Map map)
        {
            List<Pawn> result = new List<Pawn>();
            if (map == null)
            {
                return result;
            }

            if (map.mapPawns?.FreeColonistsSpawned != null)
            {
                List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
                for (int i = 0; i < colonists.Count; i++)
                {
                    Pawn pawn = colonists[i];
                    if (!CanUseEntryPawn(pawn, map))
                    {
                        continue;
                    }

                    if (pawn.drafter != null && pawn.drafter.Drafted && !result.Contains(pawn))
                    {
                        result.Add(pawn);
                    }
                }
            }

            if (result.Count > 0)
            {
                return result;
            }

            if (Find.Selector == null)
            {
                return result;
            }

            List<object> selected = Find.Selector.SelectedObjectsListForReading;
            if (selected == null)
            {
                return result;
            }

            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] is not Pawn pawn)
                {
                    continue;
                }

                if (!CanUseEntryPawn(pawn, map))
                {
                    continue;
                }

                if (!result.Contains(pawn))
                {
                    result.Add(pawn);
                }
            }

            return result;
        }

        public static bool TryOpenPocketSlice(Building_AbyssalDominionGate gate, IEnumerable<Pawn> pawns, out ABY_DominionPocketSession session, out string failReason)
        {
            session = null;
            failReason = null;
            if (gate == null || gate.Destroyed || gate.Map == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoGate".Translate();
                return false;
            }

            List<Pawn> entryPawns = pawns?.Where(p => CanUseEntryPawn(p, gate.Map)).Distinct().ToList() ?? new List<Pawn>();
            if (entryPawns.Count == 0)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoPawns".Translate();
                return false;
            }

            ABY_DominionPocketRuntimeGameComponent runtime = ABY_DominionPocketRuntimeGameComponent.Get();
            if (runtime != null && runtime.TryGetActiveSessionForSourceMap(gate.Map, out _))
            {
                failReason = "ABY_DominionPocketRuntimeFail_AlreadyOpen".Translate();
                return false;
            }

            if (!TryCreateSliceMap(gate.Map, out Map sliceMap, out int sliceTile, out failReason))
            {
                return false;
            }

            IntVec3 returnCell = ResolveReturnCell(gate);
            session = new ABY_DominionPocketSession
            {
                sessionId = Guid.NewGuid().ToString("N"),
                sourceMapId = gate.Map.uniqueID,
                pocketMapId = sliceMap.uniqueID,
                sourceGateThingId = gate.thingIDNumber,
                sourceReturnCell = returnCell,
                sliceTile = sliceTile,
                pocketEntryCell = IntVec3.Invalid,
                extractionCell = IntVec3.Invalid,
                heartCell = IntVec3.Invalid,
                createdTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0,
                victoryAchieved = false,
                rewardsGranted = false,
                collapseAtTick = 0,
                rewardSummary = null,
                active = true,
                cleanupQueued = false
            };

            if (!AbyssalDominionSliceBuilder.TryPrepareDominionSlice(sliceMap, session, out failReason))
            {
                SafeDestroyPocketMap(sliceMap, session);
                session = null;
                return false;
            }

            runtime?.RegisterSession(session);

            if (!TrySpawnPocketExit(session, sliceMap, out failReason))
            {
                runtime?.ForgetSession(session.sessionId);
                SafeDestroyPocketMap(sliceMap, session);
                session = null;
                return false;
            }

            MapComponent_DominionSliceEncounter encounter = sliceMap.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null)
            {
                encounter.TryInitialize(session);
            }

            IntVec3 entryCell = session.pocketEntryCell.IsValid ? session.pocketEntryCell : sliceMap.Center;
            for (int i = 0; i < entryPawns.Count; i++)
            {
                TransferPawnToMap(entryPawns[i], sliceMap, entryCell);
            }

            Messages.Message(
                "ABY_DominionPocketRuntimeOpened".Translate(entryPawns.Count, sliceMap.Size.x, sliceMap.Size.z),
                MessageTypeDefOf.PositiveEvent,
                false);

            Thing focusThing = ResolveExitThing(session, sliceMap);
            if (focusThing != null)
            {
                CameraJumper.TryJumpAndSelect(focusThing);
            }

            return true;
        }

        public static bool TryReturnPocketSlice(ABY_DominionPocketSession session, bool destroyPocketMap, out string failReason)
        {
            failReason = null;
            if (session == null || session.sessionId.NullOrEmpty())
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSession".Translate();
                return false;
            }

            Map sourceMap = ResolveMap(session.sourceMapId);
            if (sourceMap == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSourceMap".Translate();
                return false;
            }

            Map pocketMap = ResolveMap(session.pocketMapId);
            if (pocketMap == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoPocketMap".Translate();
                return false;
            }

            List<Pawn> pawns = GetPocketPlayerPawns(pocketMap);
            if (pawns.Count == 0)
            {
                if (destroyPocketMap)
                {
                    FailAndCollapsePocketSlice(session, pocketMap, "ABY_DominionPocketOutcome_FailureLost".Translate(), true);
                }
                failReason = "ABY_DominionPocketRuntimeFail_NoPlayerPawns".Translate();
                return false;
            }

            IntVec3 returnCell = session.sourceReturnCell.IsValid ? session.sourceReturnCell : sourceMap.Center;
            if (!returnCell.InBounds(sourceMap) || !returnCell.Standable(sourceMap))
            {
                if (!CellFinder.TryFindRandomCellNear(sourceMap.Center, sourceMap, 8, c => c.Standable(sourceMap), out returnCell))
                {
                    returnCell = sourceMap.Center;
                }
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                TransferPawnToMap(pawns[i], sourceMap, returnCell);
            }

            if (session.victoryAchieved)
            {
                ResolvePocketVictoryToSourceMap(session, pocketMap, sourceMap, returnCell);
            }
            else
            {
                ResolvePocketFailureToSourceMap(session, "ABY_DominionPocketOutcome_Retreat".Translate());
            }

            if (destroyPocketMap)
            {
                CollapsePocketSlice(session, pocketMap, true);
            }

            Messages.Message(
                session.victoryAchieved
                    ? "ABY_DominionPocketRuntimeReturnedVictory".Translate(pawns.Count)
                    : "ABY_DominionPocketRuntimeReturned".Translate(pawns.Count),
                MessageTypeDefOf.PositiveEvent,
                false);
            return true;
        }

        public static bool TryEnsurePocketExit(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null || pocketMap == null)
            {
                return false;
            }

            Thing existing = ResolveExitThing(session, pocketMap);
            if (existing != null && !existing.Destroyed)
            {
                return true;
            }

            return TrySpawnPocketExit(session, pocketMap, out _);
        }

        public static bool TryJumpToPocketSlice(ABY_DominionPocketSession session, out string failReason)
        {
            failReason = null;
            if (session == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSession".Translate();
                return false;
            }

            Map pocketMap = ResolveMap(session.pocketMapId);
            if (pocketMap == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoPocketMap".Translate();
                return false;
            }

            Thing focusThing = ResolveExitThing(session, pocketMap);
            if (focusThing != null)
            {
                CameraJumper.TryJumpAndSelect(focusThing);
                return true;
            }

            if (session.heartCell.IsValid)
            {
                CameraJumper.TryJump(session.heartCell, pocketMap);
                return true;
            }

            CameraJumper.TryJump(pocketMap.Center, pocketMap);
            return true;
        }

        public static void FailAndCollapsePocketSlice(ABY_DominionPocketSession session, Map pocketMap, string reason, bool silent)
        {
            ResolvePocketFailureToSourceMap(session, reason);
            CollapsePocketSlice(session, pocketMap, silent);
        }

        public static void CollapsePocketSlice(ABY_DominionPocketSession session, Map pocketMap, bool silent)
        {
            if (session != null)
            {
                session.active = false;
                session.cleanupQueued = false;
            }

            SafeDestroyPocketMap(pocketMap, session);
            ABY_DominionPocketRuntimeGameComponent.Get()?.ForgetSession(session?.sessionId);

            if (!silent)
            {
                Messages.Message("ABY_DominionPocketRuntimeCollapsed".Translate(), MessageTypeDefOf.CautionInput, false);
            }
        }

        private static bool TryCreateSliceMap(Map sourceMap, out Map map, out int tile, out string failReason)
        {
            map = null;
            tile = -1;
            failReason = null;

            if (sourceMap == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSourceMap".Translate();
                return false;
            }

            WorldObjectDef worldObjectDef = DefDatabase<WorldObjectDef>.GetNamedSilentFail(SliceWorldObjectDefName);
            if (worldObjectDef == null)
            {
                failReason = "Missing world object def: " + SliceWorldObjectDefName;
                return false;
            }

            if (!TryFindSliceTile(sourceMap, out tile))
            {
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

            WorldObject_ABY_DominionSliceSite worldObject = WorldObjectMaker.MakeWorldObject(worldObjectDef) as WorldObject_ABY_DominionSliceSite;
            if (worldObject == null)
            {
                failReason = "Failed to create dominion slice world object.";
                return false;
            }

            worldObject.Tile = tile;
            TrySetWorldObjectFaction(worldObject, Faction.OfPlayer);
            Find.WorldObjects.Add(worldObject);

            try
            {
                map = InvokeGetOrGenerateMap(tile, new IntVec3(SliceMapWidth, 1, SliceMapHeight), worldObject);
            }
            catch (Exception ex)
            {
                Log.Error("[Abyssal Protocol] Failed to generate dominion slice normal map: " + ex);
                TryRemoveWorldObject(tile);
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

            if (map == null)
            {
                TryRemoveWorldObject(tile);
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

            return true;
        }

        private static void ResolvePocketVictoryToSourceMap(ABY_DominionPocketSession session, Map pocketMap, Map sourceMap, IntVec3 returnCell)
        {
            if (session == null || sourceMap == null)
            {
                return;
            }

            if (!session.rewardsGranted)
            {
                AbyssalDominionSliceRewardUtility.TryAwardVictoryRewards(session, pocketMap, sourceMap, returnCell, out string summary);
                if (!summary.NullOrEmpty())
                {
                    session.rewardSummary = summary;
                }
            }

            if (TryResolveSourceCrisis(session, out MapComponent_DominionCrisis crisis))
            {
                crisis.TryResolvePocketVictory(session.rewardSummary);
            }
        }

        private static void ResolvePocketFailureToSourceMap(ABY_DominionPocketSession session, string reason)
        {
            if (session == null)
            {
                return;
            }

            if (TryResolveSourceCrisis(session, out MapComponent_DominionCrisis crisis))
            {
                crisis.TryResolvePocketFailure(reason.NullOrEmpty() ? "ABY_DominionPocketOutcome_FailureLost".Translate() : reason);
            }
        }

        private static bool TryResolveSourceCrisis(ABY_DominionPocketSession session, out MapComponent_DominionCrisis crisis)
        {
            crisis = null;
            if (session == null)
            {
                return false;
            }

            Map sourceMap = ResolveMap(session.sourceMapId);
            if (sourceMap == null)
            {
                return false;
            }

            crisis = sourceMap.GetComponent<MapComponent_DominionCrisis>();
            return crisis != null;
        }

        public static Map ResolveMap(int mapId)
        {
            if (mapId < 0 || Find.Maps == null)
            {
                return null;
            }

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map map = Find.Maps[i];
                if (map != null && map.uniqueID == mapId)
                {
                    return map;
                }
            }

            return null;
        }

        public static Thing ResolveExitThing(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null || pocketMap == null || session.pocketExitThingId < 0 || pocketMap.listerThings == null)
            {
                return null;
            }

            List<Thing> allThings = pocketMap.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing != null && !thing.Destroyed && thing.thingIDNumber == session.pocketExitThingId)
                {
                    return thing;
                }
            }

            return null;
        }

        public static bool HasAnyPlayerPawnsOnMap(Map map)
        {
            return GetPocketPlayerPawns(map).Count > 0;
        }

        public static string GetSourceMapLabel(ABY_DominionPocketSession session)
        {
            Map sourceMap = session != null ? ResolveMap(session.sourceMapId) : null;
            return sourceMap != null
                ? sourceMap.Parent?.LabelCap ?? sourceMap.ToString()
                : "ABY_DominionPocketRuntimeSource_Unknown".Translate();
        }


        public static int GetPocketPlayerCount(Map pocketMap)
        {
            return GetPocketPlayerPawns(pocketMap).Count;
        }

        public static string GetPocketSessionStatusValue(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null)
            {
                return "ABY_DominionPocketTelemetry_NoSession".Translate();
            }

            if (session.lastOutcomeReason.NullOrEmpty() == false)
            {
                return session.lastOutcomeReason;
            }

            MapComponent_DominionSliceEncounter encounter = pocketMap?.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null)
            {
                return encounter.GetTelemetryStatusLabel();
            }

            if (session.victoryAchieved)
            {
                return "ABY_DominionPocketOutcome_Victory".Translate();
            }

            if (session.active)
            {
                return "ABY_DominionPocketTelemetry_StatusAwaiting".Translate();
            }

            return "ABY_DominionPocketTelemetry_NoSession".Translate();
        }

        public static string GetPocketObjectiveValue(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null)
            {
                return "ABY_DominionPocketTelemetry_ObjectiveDormant".Translate();
            }

            MapComponent_DominionSliceEncounter encounter = pocketMap?.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null)
            {
                return encounter.GetTelemetryObjectiveLabel();
            }

            if (session.victoryAchieved)
            {
                return "ABY_DominionPocketTelemetry_ObjectiveExtract".Translate("0s");
            }

            return session.active
                ? "ABY_DominionPocketTelemetry_ObjectiveBreach".Translate()
                : "ABY_DominionPocketTelemetry_ObjectiveDormant".Translate();
        }

        public static string GetPocketRewardValue(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null)
            {
                return "ABY_DominionPocketTelemetry_RewardsUnknown".Translate();
            }

            if (session.rewardSummary.NullOrEmpty() == false)
            {
                return session.rewardSummary;
            }

            MapComponent_DominionSliceEncounter encounter = pocketMap?.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null)
            {
                return encounter.GetRewardForecastValue();
            }

            return "ABY_DominionPocketTelemetry_RewardsUnknown".Translate();
        }

        public static string GetPocketEncounterTelemetry(ABY_DominionPocketSession session, Map pocketMap)
        {
            string team = "ABY_DominionPocketTelemetry_Team".Translate(GetPocketPlayerCount(pocketMap));
            string status = "ABY_DominionPocketTelemetry_State".Translate(GetPocketSessionStatusValue(session, pocketMap));
            string objective = "ABY_DominionPocketTelemetry_Objective".Translate(GetPocketObjectiveValue(session, pocketMap));
            string rewards = "ABY_DominionPocketTelemetry_Rewards".Translate(GetPocketRewardValue(session, pocketMap));
            return string.Join(" | ", new[] { team, status, objective, rewards });
        }

        private static bool CanUseEntryPawn(Pawn pawn, Map map)
        {
            return pawn != null
                && !pawn.Destroyed
                && !pawn.Dead
                && pawn.Spawned
                && pawn.MapHeld == map
                && pawn.IsColonistPlayerControlled
                && !pawn.InMentalState;
        }

        private static bool TryFindSliceTile(Map sourceMap, out int tile)
        {
            tile = -1;
            if (sourceMap == null || Find.WorldGrid == null)
            {
                return false;
            }

            int sourceTile = sourceMap.Tile;
            List<int> candidateTiles = new List<int>();
            MethodInfo getNeighbors = Find.WorldGrid.GetType().GetMethod("GetTileNeighbors", new[] { typeof(int), typeof(List<int>) });
            if (getNeighbors != null)
            {
                try
                {
                    getNeighbors.Invoke(Find.WorldGrid, new object[] { sourceTile, candidateTiles });
                }
                catch
                {
                }
            }

            for (int i = 0; i < candidateTiles.Count; i++)
            {
                int candidate = candidateTiles[i];
                if (candidate < 0 || candidate == sourceTile || AnyMapParentAt(candidate))
                {
                    continue;
                }

                tile = candidate;
                return true;
            }

            for (int candidate = 0; candidate < Find.WorldGrid.TilesCount; candidate++)
            {
                if (candidate == sourceTile || AnyMapParentAt(candidate))
                {
                    continue;
                }

                tile = candidate;
                return true;
            }

            return false;
        }

        private static bool AnyMapParentAt(int tile)
        {
            if (tile < 0 || Find.WorldObjects == null)
            {
                return false;
            }

            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < all.Count; i++)
            {
                WorldObject worldObject = all[i];
                if (worldObject is MapParent && worldObject.Tile == tile)
                {
                    return true;
                }
            }

            return false;
        }

        private static void TrySetWorldObjectFaction(WorldObject worldObject, Faction faction)
        {
            if (worldObject == null || faction == null)
            {
                return;
            }

            MethodInfo method = worldObject.GetType().GetMethod("SetFaction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                try
                {
                    method.Invoke(worldObject, new object[] { faction });
                }
                catch
                {
                }
            }
        }

        
        private static Map InvokeGetOrGenerateMap(int tile, IntVec3 size, WorldObject_ABY_DominionSliceSite worldObject)
        {
            MethodInfo[] methods = typeof(GetOrGenerateMapUtility).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            List<Exception> failures = new List<Exception>();

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "GetOrGenerateMap")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length < 2)
                {
                    continue;
                }

                if (!TryBuildGetOrGenerateArguments(parameters, tile, size, worldObject, out object[] args))
                {
                    continue;
                }

                try
                {
                    object result = method.Invoke(null, args);
                    if (result is Map map)
                    {
                        return map;
                    }
                }
                catch (TargetInvocationException tie)
                {
                    failures.Add(tie.InnerException ?? tie);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }

            if (failures.Count > 0)
            {
                throw new AggregateException("No compatible GetOrGenerateMap overload succeeded.", failures);
            }

            throw new MissingMethodException("No compatible GetOrGenerateMap overload was found.");
        }

        private static bool TryBuildGetOrGenerateArguments(ParameterInfo[] parameters, int tile, IntVec3 size, WorldObject_ABY_DominionSliceSite worldObject, out object[] args)
        {
            args = new object[parameters.Length];
            bool assignedTile = false;
            bool assignedSize = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type type = parameter.ParameterType;

                if (!assignedTile && TryBuildTileArgument(type, tile, out object tileArg))
                {
                    args[i] = tileArg;
                    assignedTile = true;
                    continue;
                }

                if (!assignedSize && type == typeof(IntVec3))
                {
                    args[i] = size;
                    assignedSize = true;
                    continue;
                }

                if (worldObject != null && type.IsInstanceOfType(worldObject))
                {
                    args[i] = worldObject;
                    continue;
                }

                if (worldObject != null && type.IsInstanceOfType(worldObject.def))
                {
                    args[i] = worldObject.def;
                    continue;
                }

                if (type == typeof(string))
                {
                    args[i] = worldObject?.Label ?? "dominion slice";
                    continue;
                }

                if (parameter.IsOptional)
                {
                    args[i] = parameter.DefaultValue;
                    continue;
                }

                if (type == typeof(bool))
                {
                    args[i] = false;
                    continue;
                }

                if (type == typeof(int))
                {
                    args[i] = 0;
                    continue;
                }

                if (type.IsValueType)
                {
                    args[i] = Activator.CreateInstance(type);
                    continue;
                }

                args[i] = null;
            }

            return assignedTile && assignedSize;
        }

        private static bool TryBuildTileArgument(Type type, int tile, out object value)
        {
            value = null;
            if (type == typeof(int))
            {
                value = tile;
                return true;
            }

            string fullName = type.FullName;
            if (fullName == "RimWorld.Planet.PlanetTile")
            {
                try
                {
                    MethodInfo implicitOp = type.GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(int) }, null);
                    if (implicitOp != null)
                    {
                        value = implicitOp.Invoke(null, new object[] { tile });
                        return true;
                    }

                    ConstructorInfo ctor = type.GetConstructor(new[] { typeof(int) });
                    if (ctor != null)
                    {
                        value = ctor.Invoke(new object[] { tile });
                        return true;
                    }

                    value = Activator.CreateInstance(type, new object[] { tile });
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
        private static bool TryFindPocketEntryCell(Map pocketMap, out IntVec3 entryCell)
        {
            entryCell = IntVec3.Invalid;
            if (pocketMap == null)
            {
                return false;
            }

            if (CellFinder.TryFindRandomCellNear(
                pocketMap.Center,
                pocketMap,
                10,
                c => c.Standable(pocketMap) && !c.Fogged(pocketMap),
                out entryCell))
            {
                return true;
            }

            entryCell = CellFinder.RandomSpawnCellForPawnNear(pocketMap.Center, pocketMap, 8);
            return entryCell.IsValid;
        }

        private static IntVec3 ResolveReturnCell(Building_AbyssalDominionGate gate)
        {
            if (gate == null || gate.Map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 cell = gate.InteractionCell;
            if (cell.IsValid && cell.InBounds(gate.Map) && cell.Standable(gate.Map))
            {
                return cell;
            }

            if (CellFinder.TryFindRandomCellNear(gate.PositionHeld, gate.Map, 4, c => c.Standable(gate.Map), out cell))
            {
                return cell;
            }

            return gate.PositionHeld;
        }

        private static bool TrySpawnPocketExit(ABY_DominionPocketSession session, Map pocketMap, out string failReason)
        {
            failReason = null;
            if (session == null || pocketMap == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoPocketMap".Translate();
                return false;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(ExitThingDefName);
            if (def == null)
            {
                failReason = "Missing dominion pocket exit def: " + ExitThingDefName;
                return false;
            }

            IntVec3 spawnCell = session.extractionCell.IsValid ? session.extractionCell : session.pocketEntryCell;
            if (!spawnCell.IsValid || !spawnCell.InBounds(pocketMap) || !spawnCell.Standable(pocketMap))
            {
                if (!TryFindPocketEntryCell(pocketMap, out spawnCell))
                {
                    failReason = "ABY_DominionPocketRuntimeFail_NoEntryCell".Translate();
                    return false;
                }

                session.pocketEntryCell = spawnCell;
            }

            Thing existing = ResolveExitThing(session, pocketMap);
            if (existing != null && !existing.Destroyed)
            {
                return true;
            }

            Thing thing = ThingMaker.MakeThing(def);
            if (thing is not Building_ABY_DominionPocketExit exit)
            {
                failReason = "Failed to create dominion pocket exit.";
                return false;
            }

            GenSpawn.Spawn(exit, spawnCell, pocketMap, Rot4.North);
            exit.BindSession(session.sessionId);
            session.pocketExitThingId = exit.thingIDNumber;
            return true;
        }

        private static void TransferPawnToMap(Pawn pawn, Map targetMap, IntVec3 nearCell)
        {
            if (pawn == null || targetMap == null || pawn.Destroyed || pawn.Dead)
            {
                return;
            }

            if (!nearCell.IsValid || !nearCell.InBounds(targetMap) || !nearCell.Standable(targetMap))
            {
                if (!CellFinder.TryFindRandomCellNear(targetMap.Center, targetMap, 10, c => c.Standable(targetMap), out nearCell))
                {
                    nearCell = targetMap.Center;
                }
            }

            if (pawn.Spawned)
            {
                pawn.pather?.StopDead();
                pawn.stances?.CancelBusyStanceHard();
                pawn.DeSpawn();
            }

            IntVec3 spawnCell = nearCell;
            if (!spawnCell.Standable(targetMap)
                && !CellFinder.TryFindRandomCellNear(nearCell, targetMap, 6, c => c.Standable(targetMap), out spawnCell))
            {
                spawnCell = targetMap.Center;
            }

            GenSpawn.Spawn(pawn, spawnCell, targetMap, Rot4.Random);
            pawn.Notify_Teleported();
            if (pawn.drafter != null)
            {
                pawn.drafter.Drafted = false;
            }
        }

        private static List<Pawn> GetPocketPlayerPawns(Map pocketMap)
        {
            List<Pawn> result = new List<Pawn>();
            if (pocketMap?.mapPawns?.AllPawnsSpawned == null)
            {
                return result;
            }

            IReadOnlyList<Pawn> pawns = pocketMap.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Faction != Faction.OfPlayer)
                {
                    continue;
                }

                result.Add(pawn);
            }

            return result;
        }

        private static void SafeDestroyPocketMap(Map pocketMap, ABY_DominionPocketSession session)
        {
            if (pocketMap != null)
            {
                try
                {
                    MethodInfo[] methods = typeof(Game).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    MethodInfo chosen = null;
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        if (method.Name != "DeinitAndRemoveMap")
                        {
                            continue;
                        }

                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(Map))
                        {
                            chosen = method;
                            break;
                        }
                    }

                    if (chosen != null && Current.Game != null)
                    {
                        ParameterInfo[] parameters = chosen.GetParameters();
                        object[] args = new object[parameters.Length];
                        args[0] = pocketMap;
                        for (int i = 1; i < parameters.Length; i++)
                        {
                            args[i] = parameters[i].ParameterType == typeof(bool) ? (object)false : Type.Missing;
                        }
                        chosen.Invoke(Current.Game, args);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[Abyssal Protocol] Failed to destroy dominion slice normal map " + pocketMap + ": " + ex);
                }
            }

            if (session != null && session.sliceTile >= 0)
            {
                TryRemoveWorldObject(session.sliceTile);
            }
        }

        private static void TryRemoveWorldObject(int tile)
        {
            if (tile < 0 || Find.WorldObjects == null)
            {
                return;
            }

            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                WorldObject worldObject = all[i];
                if (worldObject is WorldObject_ABY_DominionSliceSite && worldObject.Tile == tile)
                {
                    Find.WorldObjects.Remove(worldObject);
                }
            }
        }
    }
}
