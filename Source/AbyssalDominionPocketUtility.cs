using System;
using System.Collections.Generic;
using System.Linq;
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

            MapGeneratorDef generatorDef = ResolveGeneratorDef();
            if (generatorDef == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_NoGenerator".Translate();
                return false;
            }

            Map pocketMap;
            try
            {
                pocketMap = PocketMapUtility.GeneratePocketMap(
                    new IntVec3(PocketMapWidth, 1, PocketMapHeight),
                    generatorDef,
                    Enumerable.Empty<GenStepWithParams>(),
                    gate.Map);
            }
            catch (Exception ex)
            {
                Log.Error($"[Abyssal Protocol] Failed to generate dominion pocket map: {ex}");
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

            if (pocketMap == null)
            {
                failReason = "ABY_DominionPocketRuntimeFail_MapCreate".Translate();
                return false;
            }

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
            if (pawns.Count == 0)
            {
                if (destroyPocketMap)
                {
                    CollapsePocketSlice(session, pocketMap, true);
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

            if (destroyPocketMap)
            {
                CollapsePocketSlice(session, pocketMap, true);
            }

            Messages.Message("ABY_DominionPocketRuntimeReturned".Translate(pawns.Count), MessageTypeDefOf.PositiveEvent, false);
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

        private static MapGeneratorDef ResolveGeneratorDef()
        {
            return MapGeneratorDefOf.MetalHell ?? MapGeneratorDefOf.Undercave ?? MapGeneratorDefOf.Encounter;
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
