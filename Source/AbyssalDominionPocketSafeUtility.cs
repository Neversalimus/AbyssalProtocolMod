using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Hardened dominion pocket entry/return flow for large modpacks.
    /// Goals:
    /// - avoid tile 0 pocket maps, which makes Geological Landforms / gas mods run on invalid world context;
    /// - pass the explicit ABY dominion slice genstep list into GetOrGenerateMap overloads;
    /// - cleanup external resource deposits that should never exist in the hell slice;
    /// - transfer the strike team pawn-by-pawn so one third-party SpawnSetup exception cannot abort the whole squad.
    /// </summary>
    public static class AbyssalDominionPocketSafeUtility
    {
        private const string ExitThingDefName = "ABY_DominionPocketExit";
        private const string SliceWorldObjectDefName = "ABY_DominionSliceSite";
        private const string SliceMapGeneratorDefName = "ABY_DominionSlicePocketMap";
        private const int SliceMapWidth = 120;
        private const int SliceMapHeight = 120;

        public static bool TryOpenPocketSliceFromGate(Building_AbyssalDominionGate gate, out string failReason)
        {
            failReason = null;
            if (gate == null || gate.Destroyed || gate.Map == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoGate".Translate();
                return false;
            }

            List<Pawn> pawns = AbyssalDominionPocketUtility.GetSelectedColonistsForPocketEntry(gate.Map);
            return TryOpenPocketSliceSafe(gate, pawns, out _, out failReason);
        }

        public static bool TryReturnPocketStrikeTeamFromGate(Building_AbyssalDominionGate gate, out string failReason)
        {
            failReason = null;
            if (gate == null || gate.Destroyed || gate.Map == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoGate".Translate();
                return false;
            }

            MapComponent_DominionCrisis crisis = gate.Map.GetComponent<MapComponent_DominionCrisis>();
            if (crisis == null || !crisis.TryGetActivePocketSession(out ABY_DominionPocketSession session) || session == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSession".Translate();
                return false;
            }

            return TryReturnPocketSliceSafe(session, true, out failReason);
        }

        public static bool TryOpenPocketSliceSafe(Building_AbyssalDominionGate gate, IEnumerable<Pawn> pawns, out ABY_DominionPocketSession session, out string failReason)
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

            if (!TryCreateSliceMapSafe(gate.Map, out Map sliceMap, out int sliceTile, out failReason))
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
                initialStrikeTeamCount = entryPawns.Count,
                lastKnownPocketPawnCount = 0,
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

            CleanupExternalPocketMapArtifacts(sliceMap);

            if (!TrySpawnPocketExitViaReflection(session, sliceMap, out failReason))
            {
                SafeDestroyPocketMap(sliceMap, session);
                session = null;
                return false;
            }

            runtime?.RegisterSession(session);

            MapComponent_DominionSliceEncounter encounter = sliceMap.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null)
            {
                encounter.TryInitialize(session);
            }

            CleanupExternalPocketMapArtifacts(sliceMap);

            IntVec3 entryCell = session.pocketEntryCell.IsValid ? session.pocketEntryCell : sliceMap.Center;
            int transferred = TransferStrikeTeamFaultTolerant(entryPawns, gate.Map, sliceMap, entryCell);
            session.lastKnownPocketPawnCount = transferred;

            if (transferred <= 0)
            {
                runtime?.ForgetSession(session.sessionId);
                SafeDestroyPocketMap(sliceMap, session);
                session = null;
                failReason = TranslateOrFallback("ABY_DominionPocketRuntimeFail_NoPawnsTransferred", "No strike team pawns could be transferred into the dominion slice.");
                return false;
            }

            if (transferred < entryPawns.Count)
            {
                Messages.Message(
                    TranslateOrFallback("ABY_DominionPocketRuntimePartialTransfer", "Dominion slice opened, but only {0}/{1} drafted pawns were transferred. Check third-party pawn SpawnSetup errors.", transferred, entryPawns.Count),
                    MessageTypeDefOf.CautionInput,
                    false);
            }
            else
            {
                Messages.Message(
                    "ABY_DominionPocketRuntimeOpened".Translate(transferred, sliceMap.Size.x, sliceMap.Size.z),
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }

            Thing focusThing = AbyssalDominionPocketUtility.ResolveExitThing(session, sliceMap);
            if (focusThing != null)
            {
                CameraJumper.TryJumpAndSelect(focusThing);
            }
            else
            {
                CameraJumper.TryJump(sliceMap.Center, sliceMap);
            }

            return true;
        }

        public static bool TryReturnPocketSliceSafe(ABY_DominionPocketSession session, bool destroyPocketMap, out string failReason)
        {
            failReason = null;
            if (session == null || session.sessionId.NullOrEmpty())
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSession".Translate();
                return false;
            }

            Map sourceMap = AbyssalDominionPocketUtility.ResolveMap(session.sourceMapId);
            if (sourceMap == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoSourceMap".Translate();
                return false;
            }

            Map pocketMap = AbyssalDominionPocketUtility.ResolveMap(session.pocketMapId);
            if (pocketMap == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoPocketMap".Translate();
                return false;
            }

            MapComponent_DominionSliceEncounter encounter = pocketMap.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null && encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse)
            {
                session.victoryAchieved = true;
                if (session.collapseAtTick <= 0 && Find.TickManager != null)
                {
                    session.collapseAtTick = Find.TickManager.TicksGame + 3600;
                }
            }

            List<Pawn> pawns = GetPocketPlayerPawns(pocketMap);
            if (pawns.Count == 0)
            {
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

            int transferred = TransferStrikeTeamFaultTolerant(pawns, pocketMap, sourceMap, returnCell);
            if (transferred <= 0)
            {
                failReason = TranslateOrFallback("ABY_DominionPocketRuntimeFail_NoReturnTransfer", "No strike team pawns could be returned from the dominion slice.");
                return false;
            }

            if (session.victoryAchieved)
            {
                ResolvePocketVictoryToSourceMapViaReflection(session, pocketMap, sourceMap, returnCell);
            }
            else
            {
                ResolvePocketFailureToSourceMapViaReflection(session, "ABY_DominionPocketOutcome_Retreat".Translate());
            }

            if (destroyPocketMap)
            {
                AbyssalDominionPocketUtility.CollapsePocketSlice(session, pocketMap, true);
            }

            Messages.Message(
                session.victoryAchieved
                    ? "ABY_DominionPocketRuntimeReturnedVictory".Translate(transferred)
                    : "ABY_DominionPocketRuntimeReturned".Translate(transferred),
                MessageTypeDefOf.PositiveEvent,
                false);
            return true;
        }

        private static bool TryCreateSliceMapSafe(Map sourceMap, out Map map, out int tile, out string failReason)
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

            if (!TryFindSliceTileSafe(sourceMap, out tile))
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
                Log.Error("[Abyssal Protocol] Failed to generate sterile dominion slice map: " + ex);
                TryRemoveSliceWorldObject(tile);
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

            if (map == null)
            {
                TryRemoveSliceWorldObject(tile);
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

            CleanupExternalPocketMapArtifacts(map);
            return true;
        }

        private static int TransferStrikeTeamFaultTolerant(List<Pawn> pawns, Map sourceMap, Map targetMap, IntVec3 nearCell)
        {
            if (pawns == null || targetMap == null)
            {
                return 0;
            }

            int transferred = 0;
            HashSet<IntVec3> usedCells = new HashSet<IntVec3>();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (TryTransferPawnToMapFaultTolerant(pawn, sourceMap, targetMap, nearCell, usedCells))
                {
                    transferred++;
                }
            }

            return transferred;
        }

        private static bool TryTransferPawnToMapFaultTolerant(Pawn pawn, Map sourceMap, Map targetMap, IntVec3 nearCell, HashSet<IntVec3> usedCells)
        {
            if (pawn == null || targetMap == null || pawn.Destroyed || pawn.Dead)
            {
                return false;
            }

            Map originalMap = pawn.MapHeld ?? sourceMap;
            IntVec3 originalCell = pawn.PositionHeld;
            bool wasDrafted = pawn.drafter != null && pawn.drafter.Drafted;

            try
            {
                if (!TryFindTransferSpawnCell(targetMap, nearCell, usedCells, out IntVec3 spawnCell))
                {
                    return false;
                }

                if (pawn.Spawned)
                {
                    pawn.pather?.StopDead();
                    pawn.stances?.CancelBusyStanceHard();
                    pawn.DeSpawn();
                }

                GenSpawn.Spawn(pawn, spawnCell, targetMap, Rot4.Random, WipeMode.Vanish, true, false);
                usedCells?.Add(spawnCell);
                PostTransferPawnCleanup(pawn, wasDrafted);
                return pawn.Spawned && pawn.MapHeld == targetMap;
            }
            catch (Exception ex)
            {
                if (pawn.Spawned && pawn.MapHeld == targetMap)
                {
                    PostTransferPawnCleanup(pawn, wasDrafted);
                    Log.Warning("[Abyssal Protocol] Pawn transfer into dominion slice emitted a third-party exception after spawn but recovered: " + pawn.LabelShortCap + " | " + ex.GetType().Name + ": " + ex.Message);
                    return true;
                }

                Log.Warning("[Abyssal Protocol] Pawn transfer failed and was isolated so the rest of the strike team can continue: " + (pawn.LabelShortCap ?? pawn.ToStringSafe()) + " | " + ex.GetType().Name + ": " + ex.Message);
                TryRestorePawnAfterFailedTransfer(pawn, originalMap, originalCell, wasDrafted);
                return false;
            }
        }

        private static void PostTransferPawnCleanup(Pawn pawn, bool wasDrafted)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                return;
            }

            try
            {
                pawn.Notify_Teleported();
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Third-party teleport notification failed after dominion transfer: " + ex.GetType().Name + ": " + ex.Message);
            }

            if (pawn.drafter != null)
            {
                pawn.drafter.Drafted = wasDrafted;
            }
        }

        private static void TryRestorePawnAfterFailedTransfer(Pawn pawn, Map originalMap, IntVec3 originalCell, bool wasDrafted)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || originalMap == null)
            {
                return;
            }

            if (pawn.Spawned)
            {
                return;
            }

            IntVec3 restoreCell = originalCell;
            if (!restoreCell.IsValid || !restoreCell.InBounds(originalMap) || !restoreCell.Standable(originalMap))
            {
                if (!CellFinder.TryFindRandomCellNear(originalMap.Center, originalMap, 10, c => c.Standable(originalMap), out restoreCell))
                {
                    restoreCell = originalMap.Center;
                }
            }

            try
            {
                GenSpawn.Spawn(pawn, restoreCell, originalMap, Rot4.Random, WipeMode.Vanish, true, false);
                PostTransferPawnCleanup(pawn, wasDrafted);
            }
            catch (Exception ex)
            {
                Log.Error("[Abyssal Protocol] Failed to restore pawn after dominion transfer failure: " + pawn.ToStringSafe() + " | " + ex);
            }
        }

        private static bool TryFindTransferSpawnCell(Map map, IntVec3 nearCell, HashSet<IntVec3> usedCells, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            IntVec3 origin = nearCell.IsValid && nearCell.InBounds(map) ? nearCell : map.Center;
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(origin, 9.9f, true))
            {
                if (IsValidTransferCell(candidate, map, usedCells))
                {
                    cell = candidate;
                    return true;
                }
            }

            if (CellFinder.TryFindRandomCellNear(origin, map, 16, c => IsValidTransferCell(c, map, usedCells), out cell))
            {
                return true;
            }

            foreach (IntVec3 fallback in map.AllCells)
            {
                if (IsValidTransferCell(fallback, map, usedCells))
                {
                    cell = fallback;
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidTransferCell(IntVec3 cell, Map map, HashSet<IntVec3> usedCells)
        {
            return cell.IsValid
                && cell.InBounds(map)
                && cell.Standable(map)
                && !cell.Fogged(map)
                && cell.GetFirstPawn(map) == null
                && cell.GetEdifice(map) == null
                && (usedCells == null || !usedCells.Contains(cell));
        }

        private static List<Pawn> GetPocketPlayerPawns(Map map)
        {
            List<Pawn> result = new List<Pawn>();
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return result;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn != null && !pawn.Destroyed && !pawn.Dead && pawn.Spawned && pawn.Faction == Faction.OfPlayer)
                {
                    result.Add(pawn);
                }
            }

            return result;
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

        private static bool TryFindSliceTileSafe(Map sourceMap, out int tile)
        {
            tile = -1;
            if (sourceMap == null || Find.WorldGrid == null)
            {
                return false;
            }

            int sourceTile = sourceMap.Tile;
            int tilesCount = Find.WorldGrid.TilesCount;
            if (tilesCount <= 1)
            {
                return false;
            }

            for (int radius = 1; radius < Math.Min(tilesCount, 3000); radius++)
            {
                int a = sourceTile + radius;
                int b = sourceTile - radius;
                if (TryAcceptSliceTileCandidate(a, sourceTile, tilesCount, out tile))
                {
                    return true;
                }

                if (TryAcceptSliceTileCandidate(b, sourceTile, tilesCount, out tile))
                {
                    return true;
                }
            }

            for (int candidate = 1; candidate < tilesCount; candidate++)
            {
                if (TryAcceptSliceTileCandidate(candidate, sourceTile, tilesCount, out tile))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryAcceptSliceTileCandidate(int candidate, int sourceTile, int tilesCount, out int accepted)
        {
            accepted = -1;
            if (candidate <= 0 || candidate >= tilesCount || candidate == sourceTile || AnyMapParentAt(candidate))
            {
                return false;
            }

            accepted = candidate;
            return true;
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
            List<GenStepWithParams> genSteps = ResolveDominionSliceGenSteps();

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

                if (!TryBuildGetOrGenerateArguments(parameters, tile, size, worldObject, genSteps, out object[] args))
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

        private static List<GenStepWithParams> ResolveDominionSliceGenSteps()
        {
            List<GenStepWithParams> result = new List<GenStepWithParams>();
            MapGeneratorDef mapGeneratorDef = DefDatabase<MapGeneratorDef>.GetNamedSilentFail(SliceMapGeneratorDefName);
            if (mapGeneratorDef?.genSteps == null || mapGeneratorDef.genSteps.Count <= 0)
            {
                return result;
            }

            for (int i = 0; i < mapGeneratorDef.genSteps.Count; i++)
            {
                GenStepDef genStepDef = mapGeneratorDef.genSteps[i];
                if (genStepDef == null)
                {
                    continue;
                }

                result.Add(new GenStepWithParams(genStepDef, default(GenStepParams)));
            }

            return result;
        }

        private static bool TryBuildGetOrGenerateArguments(ParameterInfo[] parameters, int tile, IntVec3 size, WorldObject_ABY_DominionSliceSite worldObject, List<GenStepWithParams> genSteps, out object[] args)
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

                if (type == typeof(WorldObjectDef) && worldObject != null)
                {
                    args[i] = worldObject.def;
                    continue;
                }

                if (typeof(IEnumerable<GenStepWithParams>).IsAssignableFrom(type))
                {
                    args[i] = genSteps;
                    continue;
                }

                if (typeof(Delegate).IsAssignableFrom(type))
                {
                    args[i] = null;
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

        private static bool TrySpawnPocketExitViaReflection(ABY_DominionPocketSession session, Map pocketMap, out string failReason)
        {
            failReason = null;
            MethodInfo method = typeof(AbyssalDominionPocketUtility).GetMethod("TrySpawnPocketExit", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                object[] args = { session, pocketMap, null };
                try
                {
                    bool result = (bool)method.Invoke(null, args);
                    failReason = args[2] as string;
                    return result;
                }
                catch (Exception ex)
                {
                    Log.Warning("[Abyssal Protocol] Reflected pocket exit spawn failed; using fallback exit spawn. " + ex.Message);
                }
            }

            return TrySpawnPocketExitFallback(session, pocketMap, out failReason);
        }

        private static bool TrySpawnPocketExitFallback(ABY_DominionPocketSession session, Map pocketMap, out string failReason)
        {
            failReason = null;
            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(ExitThingDefName);
            if (def == null)
            {
                failReason = "Missing dominion pocket exit def: " + ExitThingDefName;
                return false;
            }

            IntVec3 origin = session.pocketEntryCell.IsValid ? session.pocketEntryCell : pocketMap.Center;
            if (!TryFindTransferSpawnCell(pocketMap, origin, null, out IntVec3 cell))
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoEntryCell".Translate();
                return false;
            }

            Thing thing = ThingMaker.MakeThing(def);
            GenSpawn.Spawn(thing, cell, pocketMap, Rot4.North, WipeMode.Vanish, false, false);
            if (thing is Building_ABY_DominionPocketExit exit)
            {
                exit.BindSession(session.sessionId);
                session.pocketExitThingId = exit.thingIDNumber;
            }

            session.pocketEntryCell = cell;
            session.extractionCell = cell;
            return true;
        }

        private static void ResolvePocketVictoryToSourceMapViaReflection(ABY_DominionPocketSession session, Map pocketMap, Map sourceMap, IntVec3 returnCell)
        {
            MethodInfo method = typeof(AbyssalDominionPocketUtility).GetMethod("ResolvePocketVictoryToSourceMap", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                Log.Warning("[Abyssal Protocol] Could not find reflected victory resolver for dominion pocket return.");
                return;
            }

            try
            {
                method.Invoke(null, new object[] { session, pocketMap, sourceMap, returnCell });
            }
            catch (Exception ex)
            {
                Log.Error("[Abyssal Protocol] Reflected victory resolver failed: " + ex);
            }
        }

        private static void ResolvePocketFailureToSourceMapViaReflection(ABY_DominionPocketSession session, string reason)
        {
            MethodInfo method = typeof(AbyssalDominionPocketUtility).GetMethod("ResolvePocketFailureToSourceMap", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                Log.Warning("[Abyssal Protocol] Could not find reflected failure resolver for dominion pocket return.");
                return;
            }

            try
            {
                method.Invoke(null, new object[] { session, reason });
            }
            catch (Exception ex)
            {
                Log.Error("[Abyssal Protocol] Reflected failure resolver failed: " + ex);
            }
        }

        private static void SafeDestroyPocketMap(Map map, ABY_DominionPocketSession session)
        {
            MethodInfo method = typeof(AbyssalDominionPocketUtility).GetMethod("SafeDestroyPocketMap", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                try
                {
                    method.Invoke(null, new object[] { map, session });
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning("[Abyssal Protocol] Reflected pocket-map cleanup failed: " + ex.Message);
                }
            }

            if (map != null && Current.Game != null)
            {
                Current.Game.DeinitAndRemoveMap(map, true);
            }
        }

        private static void TryRemoveSliceWorldObject(int tile)
        {
            if (Find.WorldObjects == null)
            {
                return;
            }

            List<WorldObject> all = Find.WorldObjects.AllWorldObjects;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                WorldObject worldObject = all[i];
                if (worldObject != null && worldObject.Tile == tile && worldObject.def != null && worldObject.def.defName == SliceWorldObjectDefName)
                {
                    Find.WorldObjects.Remove(worldObject);
                }
            }
        }

        public static void CleanupExternalPocketMapArtifacts(Map map)
        {
            if (map?.listerThings?.AllThings == null)
            {
                return;
            }

            List<Thing> things = map.listerThings.AllThings.ToList();
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || thing.def == null)
                {
                    continue;
                }

                if (IsExternalGasDeposit(thing) || IsDominionCompatibilityRock(thing, map))
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private static bool IsExternalGasDeposit(Thing thing)
        {
            string defName = thing?.def?.defName ?? string.Empty;
            string label = thing?.def?.label ?? string.Empty;
            string text = (defName + " " + label).ToLowerInvariant();
            return (text.Contains("helixien") && text.Contains("gas"))
                || (text.Contains("gas") && text.Contains("deposit"))
                || text.Contains("vfe_gas")
                || text.Contains("vhelixien");
        }

        private static bool IsDominionCompatibilityRock(Thing thing, Map map)
        {
            if (thing?.def == null || map == null || !thing.def.defName.StartsWith("Mineable"))
            {
                return false;
            }

            IntVec3 c = thing.PositionHeld;
            return c.x <= 12 || c.z <= 12 || c.x >= map.Size.x - 13 || c.z >= map.Size.z - 13;
        }

        private static string TranslateOrFallback(string key, string fallback)
        {
            string translated = key.Translate();
            return translated == key ? fallback : translated;
        }

        private static string TranslateOrFallback(string key, string fallbackFormat, params object[] args)
        {
            string translated = key.Translate();
            string format = translated == key ? fallbackFormat : translated;
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return fallbackFormat;
            }
        }
    }
}
