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
    /// Package 1: conservative safe entry/return wrapper for dominion pocket slices.
    /// This file deliberately does not replace the existing map generator and does not touch Reactor Saint.
    /// It reuses the current dominion map creation path, then hardens only pawn transfer and return resolution.
    /// </summary>
    public static class AbyssalDominionPocketSafeUtility
    {
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

            if (!TryCreateSliceMapViaCurrentPipeline(gate.Map, out Map sliceMap, out int sliceTile, out failReason))
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

            if (!TrySpawnPocketExitViaCurrentPipeline(session, sliceMap, out failReason))
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

            IntVec3 entryCell = session.pocketEntryCell.IsValid ? session.pocketEntryCell : sliceMap.Center;
            int transferred = TransferStrikeTeamFaultTolerant(entryPawns, gate.Map, sliceMap, entryCell);
            session.lastKnownPocketPawnCount = transferred;

            if (transferred <= 0)
            {
                ABY_DominionPocketRuntimeGameComponent.Get()?.ForgetSession(session.sessionId);
                SafeDestroyPocketMap(sliceMap, session);
                session = null;
                failReason = "ABY_DominionPocketRuntimeFail_NoPawns".Translate();
                return false;
            }

            Messages.Message(
                "ABY_DominionPocketRuntimeOpened".Translate(transferred, sliceMap.Size.x, sliceMap.Size.z),
                MessageTypeDefOf.PositiveEvent,
                false);

            Thing focusThing = AbyssalDominionPocketUtility.ResolveExitThing(session, sliceMap);
            if (focusThing != null)
            {
                CameraJumper.TryJumpAndSelect(focusThing);
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

            DetectVictoryState(session, pocketMap);

            List<Pawn> pawns = GetPocketPlayerPawns(pocketMap);
            if (pawns.Count == 0)
            {
                if (!session.victoryAchieved && destroyPocketMap)
                {
                    AbyssalDominionPocketUtility.FailAndCollapsePocketSlice(session, pocketMap, "ABY_DominionPocketOutcome_FailureLost".Translate(), true);
                }

                failReason = "ABY_DominionPocketRuntimeFail_NoPlayerPawns".Translate();
                return false;
            }

            IntVec3 returnCell = ResolveSafeReturnCell(session, sourceMap);
            int returned = TransferStrikeTeamFaultTolerant(pawns, pocketMap, sourceMap, returnCell);
            session.lastKnownPocketPawnCount = 0;

            if (session.victoryAchieved)
            {
                ResolvePocketVictoryViaCurrentPipeline(session, pocketMap, sourceMap, returnCell);
            }
            else
            {
                ResolvePocketFailureViaCurrentPipeline(session, "ABY_DominionPocketOutcome_Retreat".Translate());
            }

            if (destroyPocketMap)
            {
                AbyssalDominionPocketUtility.CollapsePocketSlice(session, pocketMap, true);
            }

            Messages.Message(
                session.victoryAchieved
                    ? "ABY_DominionPocketRuntimeReturnedVictory".Translate(returned)
                    : "ABY_DominionPocketRuntimeReturned".Translate(returned),
                MessageTypeDefOf.PositiveEvent,
                false);

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
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    continue;
                }

                if (TryTransferPawnFaultTolerant(pawn, sourceMap, targetMap, nearCell, usedCells))
                {
                    transferred++;
                }
            }

            return transferred;
        }

        private static bool TryTransferPawnFaultTolerant(Pawn pawn, Map sourceMap, Map targetMap, IntVec3 nearCell, HashSet<IntVec3> usedCells)
        {
            if (pawn == null || targetMap == null || pawn.Destroyed || pawn.Dead)
            {
                return false;
            }

            bool wasDrafted = pawn.drafter != null && pawn.drafter.Drafted;
            IntVec3 sourceCell = pawn.PositionHeld;
            Map originalMap = pawn.MapHeld ?? sourceMap;

            if (!TryFindTransferSpawnCell(targetMap, nearCell, usedCells, out IntVec3 spawnCell))
            {
                spawnCell = targetMap.Center;
            }

            try
            {
                if (pawn.Spawned)
                {
                    pawn.pather?.StopDead();
                    pawn.stances?.CancelBusyStanceHard();
                    pawn.DeSpawn();
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Safe dominion transfer could not cleanly despawn " + pawn.LabelShortCap + ": " + ex.Message);
            }

            try
            {
                GenSpawn.Spawn(pawn, spawnCell, targetMap, Rot4.Random);
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Safe dominion transfer isolated a third-party SpawnSetup exception for " + pawn.LabelShortCap + ": " + ex.Message);
            }

            if (!pawn.Spawned || pawn.MapHeld != targetMap)
            {
                TryRecoverPawnToSource(pawn, originalMap, sourceCell);
                return false;
            }

            usedCells?.Add(pawn.PositionHeld);
            try
            {
                pawn.Notify_Teleported();
            }
            catch
            {
            }

            if (pawn.drafter != null)
            {
                try
                {
                    pawn.drafter.Drafted = wasDrafted;
                }
                catch
                {
                }
            }

            return true;
        }

        private static void TryRecoverPawnToSource(Pawn pawn, Map sourceMap, IntVec3 sourceCell)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || sourceMap == null)
            {
                return;
            }

            try
            {
                if (pawn.Spawned && pawn.MapHeld != sourceMap)
                {
                    pawn.DeSpawn();
                }

                IntVec3 recoverCell = sourceCell;
                if (!IsValidTransferCell(recoverCell, sourceMap, null))
                {
                    CellFinder.TryFindRandomCellNear(sourceMap.Center, sourceMap, 10, c => c.Standable(sourceMap), out recoverCell);
                }

                if (recoverCell.IsValid && !pawn.Spawned)
                {
                    GenSpawn.Spawn(pawn, recoverCell, sourceMap, Rot4.Random);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Safe dominion transfer recovery failed for " + pawn.LabelShortCap + ": " + ex.Message);
            }
        }

        private static bool TryFindTransferSpawnCell(Map map, IntVec3 nearCell, HashSet<IntVec3> usedCells, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            IntVec3 origin = nearCell.IsValid ? nearCell : map.Center;
            for (int radius = 0; radius <= 12; radius++)
            {
                int cells = Math.Min(GenRadial.NumCellsInRadius(radius), GenRadial.RadialPattern.Length);
                for (int i = 0; i < cells; i++)
                {
                    IntVec3 candidate = origin + GenRadial.RadialPattern[i];
                    if (IsValidTransferCell(candidate, map, usedCells))
                    {
                        cell = candidate;
                        return true;
                    }
                }
            }

            if (CellFinder.TryFindRandomCellNear(map.Center, map, 18, c => IsValidTransferCell(c, map, usedCells), out cell))
            {
                return true;
            }

            return false;
        }

        private static bool IsValidTransferCell(IntVec3 cell, Map map, HashSet<IntVec3> usedCells)
        {
            return cell.IsValid
                && map != null
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

        private static IntVec3 ResolveSafeReturnCell(ABY_DominionPocketSession session, Map sourceMap)
        {
            IntVec3 returnCell = session != null && session.sourceReturnCell.IsValid ? session.sourceReturnCell : sourceMap.Center;
            if (!IsValidReturnCell(returnCell, sourceMap))
            {
                if (!CellFinder.TryFindRandomCellNear(sourceMap.Center, sourceMap, 8, c => IsValidReturnCell(c, sourceMap), out returnCell))
                {
                    returnCell = sourceMap.Center;
                }
            }

            return returnCell;
        }

        private static bool IsValidReturnCell(IntVec3 cell, Map map)
        {
            return cell.IsValid && map != null && cell.InBounds(map) && cell.Standable(map) && cell.GetEdifice(map) == null;
        }

        private static void DetectVictoryState(ABY_DominionPocketSession session, Map pocketMap)
        {
            if (session == null || pocketMap == null || session.victoryAchieved)
            {
                return;
            }

            MapComponent_DominionSliceEncounter encounter = pocketMap.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter == null)
            {
                return;
            }

            try
            {
                if (encounter.CurrentPhase == MapComponent_DominionSliceEncounter.SlicePhase.Collapse)
                {
                    session.victoryAchieved = true;
                    session.collapseAtTick = Find.TickManager != null ? Find.TickManager.TicksGame : session.collapseAtTick;
                }
            }
            catch
            {
            }
        }

        private static bool TryCreateSliceMapViaCurrentPipeline(Map sourceMap, out Map map, out int tile, out string failReason)
        {
            map = null;
            tile = -1;
            failReason = null;

            MethodInfo method = typeof(AbyssalDominionPocketUtility).GetMethod("TryCreateSliceMap", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

            object[] args = { sourceMap, null, -1, null };
            try
            {
                bool result = (bool)method.Invoke(null, args);
                map = args[1] as Map;
                tile = args[2] is int value ? value : -1;
                failReason = args[3] as string;
                return result && map != null;
            }
            catch (TargetInvocationException tie)
            {
                Exception inner = tie.InnerException ?? tie;
                Log.Error("[Abyssal Protocol] Safe dominion entry could not create pocket map through current pipeline: " + inner);
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[Abyssal Protocol] Safe dominion entry could not create pocket map through current pipeline: " + ex);
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }
        }

        private static bool TrySpawnPocketExitViaCurrentPipeline(ABY_DominionPocketSession session, Map pocketMap, out string failReason)
        {
            failReason = null;
            MethodInfo method = typeof(AbyssalDominionPocketUtility).GetMethod("TrySpawnPocketExit", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoEntryCell".Translate();
                return false;
            }

            object[] args = { session, pocketMap, null };
            try
            {
                bool result = (bool)method.Invoke(null, args);
                failReason = args[2] as string;
                return result;
            }
            catch (TargetInvocationException tie)
            {
                Exception inner = tie.InnerException ?? tie;
                Log.Warning("[Abyssal Protocol] Safe dominion entry could not spawn pocket exit through current pipeline: " + inner.Message);
                failReason = "ABY_DominionPocketRuntimeFail_NoEntryCell".Translate();
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Safe dominion entry could not spawn pocket exit through current pipeline: " + ex.Message);
                failReason = "ABY_DominionPocketRuntimeFail_NoEntryCell".Translate();
                return false;
            }
        }

        private static void ResolvePocketVictoryViaCurrentPipeline(ABY_DominionPocketSession session, Map pocketMap, Map sourceMap, IntVec3 returnCell)
        {
            MethodInfo method = typeof(AbyssalDominionPocketUtility).GetMethod("ResolvePocketVictoryToSourceMap", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                return;
            }

            try
            {
                method.Invoke(null, new object[] { session, pocketMap, sourceMap, returnCell });
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Safe dominion victory resolve failed: " + ex.Message);
            }
        }

        private static void ResolvePocketFailureViaCurrentPipeline(ABY_DominionPocketSession session, string reason)
        {
            MethodInfo method = typeof(AbyssalDominionPocketUtility).GetMethod("ResolvePocketFailureToSourceMap", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                return;
            }

            try
            {
                method.Invoke(null, new object[] { session, reason });
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Safe dominion failure resolve failed: " + ex.Message);
            }
        }

        private static void SafeDestroyPocketMap(Map pocketMap, ABY_DominionPocketSession session)
        {
            MethodInfo method = typeof(AbyssalDominionPocketUtility).GetMethod("SafeDestroyPocketMap", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                try
                {
                    method.Invoke(null, new object[] { pocketMap, session });
                    return;
                }
                catch
                {
                }
            }

            if (pocketMap != null && Current.Game != null)
            {
                try
                {
                    MethodInfo[] methods = typeof(Game).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo candidate = methods[i];
                        if (candidate.Name != "DeinitAndRemoveMap")
                        {
                            continue;
                        }

                        ParameterInfo[] parameters = candidate.GetParameters();
                        if (parameters.Length < 1 || parameters[0].ParameterType != typeof(Map))
                        {
                            continue;
                        }

                        object[] args = new object[parameters.Length];
                        args[0] = pocketMap;
                        for (int argIndex = 1; argIndex < parameters.Length; argIndex++)
                        {
                            Type parameterType = parameters[argIndex].ParameterType;
                            args[argIndex] = parameterType == typeof(bool) ? (object)false : Type.Missing;
                        }

                        candidate.Invoke(Current.Game, args);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("[Abyssal Protocol] Fallback pocket map removal failed: " + ex.Message);
                }
            }
        }
    }
}
