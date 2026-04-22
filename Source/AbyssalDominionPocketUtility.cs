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

            try
            {
                map = InvokeGetOrGenerateMap(tile, new IntVec3(SliceMapWidth, 1, SliceMapHeight), worldObjectDef);
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

            WorldObject_ABY_DominionSliceSite worldObject = FindSliceSiteAtTile(tile);
            if (worldObject != null)
            {
                TrySetWorldObjectFaction(worldObject, Faction.OfPlayer);
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

        public static bool HasAnyPlayerPawnsOnMap(Map map)
        {
            return GetPocketPlayerCount(map) > 0;
        }

        public static int GetPocketPlayerCount(Map map)
        {
            return GetPocketPlayerPawns(map).Count;
        }

        public static string GetPocketCollapseEta(ABY_DominionPocketSession session)
        {
            if (session == null || !session.victoryAchieved || Find.TickManager == null || session.collapseAtTick <= 0)
            {
                return "ABY_DominionPocketFlowStatus_NoExtraction".Translate();
            }

            return System.Math.Max(0, session.collapseAtTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();
        }

        public static string GetPocketSessionStatusValue(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null)
            {
                return "ABY_DominionPocketFlowStatus_Locked".Translate();
            }

            int teamCount = pocketMap != null ? GetPocketPlayerCount(pocketMap) : session.lastKnownPocketPawnCount;
            if (teamCount <= 0)
            {
                teamCount = session.initialStrikeTeamCount;
            }

            if (session.victoryAchieved)
            {
                return "ABY_DominionPocketFlowStatus_ExtractArmed".Translate(teamCount, GetPocketCollapseEta(session));
            }

            return "ABY_DominionPocketFlowStatus_DeployedCount".Translate(teamCount);
        }

        public static string GetPocketObjectiveValue(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null)
            {
                return "ABY_DominionPocketTelemetry_ObjectiveDormant".Translate();
            }

            MapComponent_DominionSliceEncounter encounter = pocketMap != null ? pocketMap.GetComponent<MapComponent_DominionSliceEncounter>() : null;
            if (encounter != null)
            {
                return encounter.GetTelemetryObjectiveLabel();
            }

            if (session.victoryAchieved)
            {
                return "ABY_DominionPocketTelemetry_ObjectiveExtract".Translate(GetPocketCollapseEta(session));
            }

            return "ABY_DominionPocketTelemetry_ObjectiveDormant".Translate();
        }

        public static string GetPocketRewardValue(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null)
            {
                return "ABY_DominionSliceRewardForecast_None".Translate();
            }

            if (!session.rewardSummary.NullOrEmpty())
            {
                return session.rewardSummary;
            }

            MapComponent_DominionSliceEncounter encounter = pocketMap != null ? pocketMap.GetComponent<MapComponent_DominionSliceEncounter>() : null;
            if (encounter != null)
            {
                return encounter.GetRewardForecastValue();
            }

            return "ABY_DominionSliceRewardForecast_None".Translate();
        }

        public static string GetPocketEncounterTelemetry(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null)
            {
                return "ABY_DominionPocketTelemetry_Dormant".Translate();
            }

            string objective = GetPocketObjectiveValue(session, pocketMap);
            string team = GetPocketSessionStatusValue(session, pocketMap);
            string rewards = GetPocketRewardValue(session, pocketMap);
            return "ABY_DominionPocketTelemetry_Report".Translate(objective, team, rewards);
        }

        public static string GetSourceMapLabel(ABY_DominionPocketSession session)
        {
            Map sourceMap = session != null ? ResolveMap(session.sourceMapId) : null;
            return sourceMap != null
                ? sourceMap.Parent?.LabelCap ?? sourceMap.ToString()
                : "ABY_DominionPocketRuntimeSource_Unknown".Translate();
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

        private static WorldObject_ABY_DominionSliceSite FindSliceSiteAtTile(int tile)
        {
            if (tile < 0 || Find.WorldObjects == null)
            {
                return null;
            }

            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is WorldObject_ABY_DominionSliceSite sliceSite && sliceSite.Tile == tile)
                {
                    return sliceSite;
                }
            }

            return null;
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

        private static Map InvokeGetOrGenerateMap(int tile, IntVec3 size, WorldObjectDef worldObjectDef)
        {
            PlanetTile planetTile = new PlanetTile(tile);
            IEnumerable<GenStepWithParams> extraGenSteps = Enumerable.Empty<GenStepWithParams>();

            MethodInfo[] methods = typeof(GetOrGenerateMapUtility).GetMethods(BindingFlags.Static | BindingFlags.Public);
            MethodInfo sizedOverload = null;
            MethodInfo defaultOverload = null;
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "GetOrGenerateMap")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 5
                    && parameters[0].ParameterType == typeof(PlanetTile)
                    && parameters[1].ParameterType == typeof(IntVec3)
                    && parameters[2].ParameterType == typeof(WorldObjectDef))
                {
                    sizedOverload = method;
                }
                else if (parameters.Length == 3
                    && parameters[0].ParameterType == typeof(PlanetTile)
                    && parameters[1].ParameterType == typeof(WorldObjectDef))
                {
                    defaultOverload = method;
                }
            }

            if (sizedOverload != null)
            {
                try
                {
                    return sizedOverload.Invoke(null, new object[] { planetTile, size, worldObjectDef, extraGenSteps, false }) as Map;
                }
                catch (TargetInvocationException)
                {
                    if (defaultOverload == null)
                    {
                        throw;
                    }
                }
            }

            if (defaultOverload != null)
            {
                return defaultOverload.Invoke(null, new object[] { planetTile, worldObjectDef, extraGenSteps }) as Map;
            }

            throw new MissingMethodException("No compatible GetOrGenerateMapUtility.GetOrGenerateMap overload for PlanetTile was found.");
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
