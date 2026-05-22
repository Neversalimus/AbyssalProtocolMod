using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Shared defensive spawn helpers for large-modpack hardening.
    /// Use this for Abyssal runtime systems that spawn or transfer pawns/buildings near active combat,
    /// portals, Dominion maps, power grids, or player infrastructure. The helper deliberately fails closed
    /// instead of falling back to unsafe map-center spawns.
    /// </summary>
    public static class ABY_SafeSpawnUtility
    {
        public static bool TryGeneratePawnSafe(
            PawnGenerationRequest request,
            out Pawn pawn,
            out string failReason,
            string context = null)
        {
            pawn = null;
            failReason = null;

            try
            {
                pawn = PawnGenerator.GeneratePawn(request);
                if (pawn == null)
                {
                    failReason = BuildFailure("PawnGenerator returned null.", context);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                failReason = BuildFailure("Pawn generation failed: " + ex.GetType().Name + ": " + ex.Message, context);
                Log.Warning("[Abyssal Protocol] " + failReason + "\n" + ex);
                pawn = null;
                return false;
            }
        }

        public static bool TrySpawnThingSafe(
            Thing thing,
            IntVec3 cell,
            Map map,
            out Thing spawnedThing,
            Rot4? rot = null,
            WipeMode wipeMode = WipeMode.Vanish,
            bool respawningAfterLoad = false,
            bool forbidLeavings = false,
            string context = null)
        {
            spawnedThing = null;

            if (thing == null)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Cannot spawn null thing.", context));
                return false;
            }

            if (map == null)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Cannot spawn thing on null map: " + SafeThingLabel(thing), context));
                return false;
            }

            if (thing.Destroyed)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Cannot spawn destroyed thing: " + SafeThingLabel(thing), context));
                return false;
            }

            IntVec3 spawnCell = cell;
            if (!IsCellSpawnable(spawnCell, map))
            {
                if (!TryFindStandableCellNear(cell.IsValid ? cell : map.Center, map, out spawnCell, 8))
                {
                    Log.Warning("[Abyssal Protocol] " + BuildFailure("No safe spawn cell found for " + SafeThingLabel(thing) + " near " + (cell.IsValid ? cell.ToString() : "<invalid>") + ".", context));
                    return false;
                }
            }

            try
            {
                spawnedThing = GenSpawn.Spawn(
                    thing,
                    spawnCell,
                    map,
                    rot ?? Rot4.North,
                    wipeMode,
                    respawningAfterLoad,
                    forbidLeavings);

                return spawnedThing != null && !spawnedThing.Destroyed;
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Thing spawn failed for " + SafeThingLabel(thing) + ": " + ex.GetType().Name + ": " + ex.Message, context) + "\n" + ex);
                spawnedThing = null;
                return false;
            }
        }

        public static bool TrySpawnThingDefSafe(
            ThingDef thingDef,
            IntVec3 cell,
            Map map,
            out Thing spawnedThing,
            ThingDef stuff = null,
            Rot4? rot = null,
            WipeMode wipeMode = WipeMode.Vanish,
            bool respawningAfterLoad = false,
            bool forbidLeavings = false,
            string context = null)
        {
            spawnedThing = null;
            if (thingDef == null)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Cannot make/spawn null ThingDef.", context));
                return false;
            }

            Thing thing;
            try
            {
                thing = ThingMaker.MakeThing(thingDef, stuff);
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("ThingMaker failed for " + thingDef.defName + ": " + ex.GetType().Name + ": " + ex.Message, context) + "\n" + ex);
                return false;
            }

            return TrySpawnThingSafe(
                thing,
                cell,
                map,
                out spawnedThing,
                rot,
                wipeMode,
                respawningAfterLoad,
                forbidLeavings,
                context);
        }

        public static bool TrySpawnPawnSafe(
            Pawn pawn,
            IntVec3 cell,
            Map map,
            out Pawn spawnedPawn,
            Rot4? rot = null,
            WipeMode wipeMode = WipeMode.Vanish,
            bool respawningAfterLoad = false,
            bool forbidLeavings = false,
            string context = null)
        {
            spawnedPawn = null;
            if (pawn == null)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Cannot spawn null pawn.", context));
                return false;
            }

            Thing spawnedThing;
            bool success = TrySpawnThingSafe(
                pawn,
                cell,
                map,
                out spawnedThing,
                rot ?? Rot4.Random,
                wipeMode,
                respawningAfterLoad,
                forbidLeavings,
                context);

            spawnedPawn = spawnedThing as Pawn;
            return success && spawnedPawn != null;
        }

        public static bool TryDespawnSafe(Thing thing, string context = null)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned)
            {
                return true;
            }

            try
            {
                thing.DeSpawn();
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Despawn failed for " + SafeThingLabel(thing) + ": " + ex.GetType().Name + ": " + ex.Message, context) + "\n" + ex);
                return false;
            }
        }

        public static bool TryTransferPawnSafe(
            Pawn pawn,
            Map targetMap,
            IntVec3 nearCell,
            out IntVec3 finalCell,
            bool preserveDrafted = true,
            bool notifyTeleported = true,
            string context = null)
        {
            finalCell = IntVec3.Invalid;

            if (pawn == null || pawn.Destroyed || pawn.Dead)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Cannot transfer null/destroyed/dead pawn.", context));
                return false;
            }

            if (targetMap == null)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Cannot transfer pawn to null map: " + SafeThingLabel(pawn), context));
                return false;
            }

            Map originalMap = pawn.MapHeld;
            IntVec3 originalCell = pawn.PositionHeld;
            Rot4 originalRotation = pawn.Rotation;
            bool wasSpawned = pawn.Spawned;
            bool wasDrafted = preserveDrafted && pawn.drafter != null && pawn.drafter.Drafted;

            if (!TryFindStandableCellNear(nearCell, targetMap, out IntVec3 spawnCell, 8))
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Cannot transfer pawn; no safe destination cell found near " + (nearCell.IsValid ? nearCell.ToString() : "<invalid>") + ": " + SafeThingLabel(pawn), context));
                return false;
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
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Transfer despawn step failed for " + SafeThingLabel(pawn) + ": " + ex.GetType().Name + ": " + ex.Message, context) + "\n" + ex);
                return false;
            }

            Pawn spawnedPawn;
            if (!TrySpawnPawnSafe(pawn, spawnCell, targetMap, out spawnedPawn, Rot4.Random, WipeMode.Vanish, false, false, context))
            {
                if (wasSpawned)
                {
                    TryRollbackPawnTransfer(pawn, originalMap, originalCell, originalRotation, wasDrafted, preserveDrafted, context);
                }

                return false;
            }

            finalCell = spawnedPawn.PositionHeld;

            try
            {
                if (notifyTeleported)
                {
                    spawnedPawn.Notify_Teleported();
                }

                if (preserveDrafted && spawnedPawn.drafter != null)
                {
                    spawnedPawn.drafter.Drafted = wasDrafted;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] " + BuildFailure("Transfer post-spawn notification failed for " + SafeThingLabel(spawnedPawn) + ": " + ex.GetType().Name + ": " + ex.Message, context) + "\n" + ex);
            }

            return true;
        }

        private static bool TryRollbackPawnTransfer(
            Pawn pawn,
            Map originalMap,
            IntVec3 originalCell,
            Rot4 originalRotation,
            bool wasDrafted,
            bool preserveDrafted,
            string context)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || originalMap == null || pawn.Spawned)
            {
                return false;
            }

            IntVec3 rollbackCell;
            if (!TryFindStandableCellNear(originalCell, originalMap, out rollbackCell, 8))
            {
                Log.Error("[Abyssal Protocol] " + BuildFailure("Transfer rollback failed: no safe origin cell for " + SafeThingLabel(pawn) + ".", context));
                return false;
            }

            Pawn restoredPawn;
            if (!TrySpawnPawnSafe(pawn, rollbackCell, originalMap, out restoredPawn, originalRotation, WipeMode.Vanish, false, false, context))
            {
                Log.Error("[Abyssal Protocol] " + BuildFailure("Transfer rollback failed to respawn " + SafeThingLabel(pawn) + ".", context));
                return false;
            }

            if (preserveDrafted && restoredPawn.drafter != null)
            {
                restoredPawn.drafter.Drafted = wasDrafted;
            }

            return true;
        }

        public static bool TryFindStandableCellNear(IntVec3 origin, Map map, out IntVec3 cell, int radius = 8)
        {
            cell = IntVec3.Invalid;
            if (map == null)
            {
                return false;
            }

            IntVec3 safeOrigin = origin.IsValid && origin.InBounds(map) ? origin : map.Center;
            if (IsCellSpawnable(safeOrigin, map))
            {
                cell = safeOrigin;
                return true;
            }

            try
            {
                return CellFinder.TryFindRandomCellNear(
                    safeOrigin,
                    map,
                    Math.Max(1, radius),
                    c => IsCellSpawnable(c, map),
                    out cell);
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Safe cell search failed: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex);
                cell = IntVec3.Invalid;
                return false;
            }
        }

        public static IntVec3 ResolveSpawnCell(IntVec3 preferredCell, Map map, int radius = 8)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 cell;
            if (TryFindStandableCellNear(preferredCell, map, out cell, radius))
            {
                return cell;
            }

            return IntVec3.Invalid;
        }

        public static bool IsCellSpawnable(IntVec3 cell, Map map)
        {
            if (!cell.IsValid || map == null || !cell.InBounds(map) || !cell.Standable(map) || cell.Fogged(map))
            {
                return false;
            }

            List<Thing> things = cell.GetThingList(map);
            if (things == null)
            {
                return true;
            }

            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (thing is Pawn || thing is Building || thing is Blueprint || thing is Frame)
                {
                    return false;
                }

                ThingDef def = thing.def;
                if (def != null && def.category == ThingCategory.Building)
                {
                    return false;
                }
            }

            return true;
        }

        private static string BuildFailure(string message, string context)
        {
            return context.NullOrEmpty() ? message : context + ": " + message;
        }

        private static string SafeThingLabel(Thing thing)
        {
            if (thing == null)
            {
                return "<null>";
            }

            try
            {
                return thing.def != null ? thing.def.defName : thing.ToString();
            }
            catch
            {
                return thing.ToStringSafe();
            }
        }
    }
}
