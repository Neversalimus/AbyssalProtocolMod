using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionPocketUtility
    {
        private const string ExitThingDefName = "ABY_DominionPocketExit";
        private const int PocketMapWidth = 120;
        private const int PocketMapHeight = 120;

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

            List<MapGeneratorDef> generatorDefs = ResolveGeneratorDefs();
            if (generatorDefs.Count == 0)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoGenerator".Translate();
                return false;
            }

            Map pocketMap = null;
            Exception lastException = null;
            for (int generatorIndex = 0; generatorIndex < generatorDefs.Count; generatorIndex++)
            {
                MapGeneratorDef generatorDef = generatorDefs[generatorIndex];
                if (generatorDef == null)
                {
                    continue;
                }

                try
                {
                    pocketMap = PocketMapUtility.GeneratePocketMap(
                        new IntVec3(PocketMapWidth, 1, PocketMapHeight),
                        generatorDef,
                        Enumerable.Empty<GenStepWithParams>(),
                        gate.Map);
                    if (pocketMap != null)
                    {
                        if (generatorIndex > 0)
                        {
                            Log.Warning($"[Abyssal Protocol] Dominion pocket map used fallback shell generator {generatorDef.defName}.");
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Log.Warning($"[Abyssal Protocol] Dominion pocket shell generator failed: {generatorDef.defName}. Trying next shell if available. Exception: {ex.GetType().Name}: {ex.Message}");
                }
            }

            if (pocketMap == null)
            {
                if (lastException != null)
                {
                    Log.Error($"[Abyssal Protocol] Failed to generate dominion pocket map with all neutral shell generators: {lastException}");
                }
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

            EnsurePocketMapWorldBinding(pocketMap, gate.Map);

            IntVec3 returnCell = ResolveReturnCell(gate);
            session = new ABY_DominionPocketSession
            {
                sessionId = Guid.NewGuid().ToString("N"),
                sourceMapId = gate.Map.uniqueID,
                pocketMapId = pocketMap.uniqueID,
                sourceGateThingId = gate.thingIDNumber,
                sourceReturnCell = returnCell,
                pocketEntryCell = IntVec3.Invalid,
                extractionCell = IntVec3.Invalid,
                heartCell = IntVec3.Invalid,
                createdTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0,
                initialStrikeTeamCount = entryPawns.Count,
                lastKnownPocketPawnCount = entryPawns.Count,
                victoryAchieved = false,
                rewardsGranted = false,
                collapseAtTick = 0,
                rewardSummary = null,
                active = true,
                cleanupQueued = false
            };

            if (!AbyssalDominionSliceBuilder.TryPrepareDominionSlice(pocketMap, session, out failReason))
            {
                SafeDestroyPocketMap(pocketMap);
                session = null;
                return false;
            }

            runtime?.RegisterSession(session);

            if (!TrySpawnPocketExit(session, pocketMap, out failReason))
            {
                runtime?.ForgetSession(session.sessionId);
                SafeDestroyPocketMap(pocketMap);
                session = null;
                return false;
            }

            MapComponent_DominionSliceEncounter encounter = pocketMap.GetComponent<MapComponent_DominionSliceEncounter>();
            if (encounter != null)
            {
                encounter.TryInitialize(session);
            }

            IntVec3 entryCell = session.pocketEntryCell.IsValid ? session.pocketEntryCell : pocketMap.Center;
            for (int i = 0; i < entryPawns.Count; i++)
            {
                TransferPawnToMap(entryPawns[i], pocketMap, entryCell);
            }

            Messages.Message(
                "ABY_DominionPocketRuntimeOpened".Translate(entryPawns.Count, pocketMap.Size.x, pocketMap.Size.z),
                MessageTypeDefOf.PositiveEvent,
                false);
            Thing focusThing = ResolveExitThing(session, pocketMap);
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
            session.lastKnownPocketPawnCount = pawns.Count;
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
            if (session != null && !reason.NullOrEmpty())
            {
                session.lastOutcomeReason = reason;
            }

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

            if (pocketMap != null)
            {
                SafeDestroyPocketMap(pocketMap);
            }

            ABY_DominionPocketRuntimeGameComponent.Get()?.ForgetSession(session?.sessionId);
            if (!silent)
            {
                Messages.Message("ABY_DominionPocketRuntimeCollapsed".Translate(), MessageTypeDefOf.CautionInput, false);
            }
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
                session.lastOutcomeReason = "ABY_DominionPocketOutcome_Victory".Translate();
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
                string resolvedReason = reason.NullOrEmpty() ? "ABY_DominionPocketOutcome_FailureLost".Translate() : reason;
                session.lastOutcomeReason = resolvedReason;
                crisis.TryResolvePocketFailure(resolvedReason);
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
                && !pawn.InMentalState
                && !pawn.Downed;
        }
        private static List<MapGeneratorDef> ResolveGeneratorDefs()
        {
            List<MapGeneratorDef> result = new List<MapGeneratorDef>();
            AddGeneratorDef(result, "Base_Player");
            AddGeneratorDef(result, "Base_Faction");
            return result;
        }

        private static void AddGeneratorDef(List<MapGeneratorDef> result, string defName)
        {
            if (result == null || defName.NullOrEmpty())
            {
                return;
            }

            MapGeneratorDef def = DefDatabase<MapGeneratorDef>.GetNamedSilentFail(defName);
            if (def != null && !result.Contains(def))
            {
                result.Add(def);
            }
        }

        private static void EnsurePocketMapWorldBinding(Map pocketMap, Map sourceMap)
        {
            if (pocketMap == null || sourceMap == null)
            {
                return;
            }

            int sourceTile = sourceMap.Tile;
            if (sourceTile < 0 && sourceMap.Parent != null)
            {
                sourceTile = TryGetTileValue(sourceMap.Parent);
            }

            if (sourceTile < 0)
            {
                return;
            }

            TrySetTileValue(pocketMap.Parent, sourceTile);
            TrySetTileValue(pocketMap, sourceTile);
        }

        private static int TryGetTileValue(object obj)
        {
            if (obj == null)
            {
                return -1;
            }

            Type type = obj.GetType();
            PropertyInfo property = type.GetProperty("Tile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanRead && property.PropertyType == typeof(int))
            {
                try
                {
                    return (int)property.GetValue(obj, null);
                }
                catch
                {
                }
            }

            FieldInfo field = FindIntField(type, new[] { "tileInt", "tile", "Tile" });
            if (field != null)
            {
                try
                {
                    return (int)field.GetValue(obj);
                }
                catch
                {
                }
            }

            return -1;
        }

        private static void TrySetTileValue(object obj, int tile)
        {
            if (obj == null || tile < 0)
            {
                return;
            }

            Type type = obj.GetType();
            PropertyInfo property = type.GetProperty("Tile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite && property.PropertyType == typeof(int))
            {
                try
                {
                    property.SetValue(obj, tile, null);
                    return;
                }
                catch
                {
                }
            }

            FieldInfo field = FindIntField(type, new[] { "tileInt", "tile", "Tile" });
            if (field != null)
            {
                try
                {
                    field.SetValue(obj, tile);
                }
                catch
                {
                }
            }
        }

        private static FieldInfo FindIntField(Type type, string[] candidateNames)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                for (int i = 0; i < candidateNames.Length; i++)
                {
                    FieldInfo field = current.GetField(candidateNames[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(int))
                    {
                        return field;
                    }
                }
            }

            return null;
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

        private static void SafeDestroyPocketMap(Map pocketMap)
        {
            if (pocketMap == null)
            {
                return;
            }

            try
            {
                PocketMapUtility.DestroyPocketMap(pocketMap);
            }
            catch (Exception ex)
            {
                Log.Error($"[Abyssal Protocol] Failed to destroy dominion pocket map {pocketMap}: {ex}");
            }
        }
    }
}
