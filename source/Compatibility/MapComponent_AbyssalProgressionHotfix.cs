using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Compatibility guardrails for very large progression-style modpacks.
    /// Optimized to avoid one monolithic every-60-ticks scan over pawns, designations and AllThings.
    /// Each guard now runs on its own staggered cadence and uses def-specific listers / shared pawn cache where safe.
    /// </summary>
    public class MapComponent_AbyssalProgressionHotfix : MapComponent
    {
        private const int TamingGuardIntervalTicks = 300;
        private const int SigilFocusIntervalTicks = 150;
        private const int FoggedPortalIntervalTicks = 240;
        private const int HordePressureIntervalTicks = 60;
        private const int OrphanGateIntervalTicks = 300;
        private const int ExtraHordeIntervalTicks = 720;
        private const int MaxExtraHordeBurstsPerWave = 4;
        private const string AbyssalPrefix = "ABY_";
        private const string CommandGateDefName = "ABY_HordeCommandGate";
        private const string ImpPortalDefName = "ABY_ImpPortal";
        private const string RupturePortalDefName = "ABY_RupturePortal";
        private const string SummoningCircleDefName = "ABY_SummoningCircle";

        private static readonly string[] PortalDefNames = { ImpPortalDefName, RupturePortalDefName };
        private static bool profilesRelaxed;
        private int nextSlowTick;
        private int nextTamingGuardTick;
        private int nextSigilFocusTick;
        private int nextFoggedPortalTick;
        private int nextHordePressureTick;
        private int nextOrphanGateTick;
        private int nextExtraHordeTick = -1;
        private int extraHordeBurstsUsed;
        private bool hordeSeenThisActivation;

        public MapComponent_AbyssalProgressionHotfix(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextSlowTick, "abyFullProg_nextSlowTick", 0);
            Scribe_Values.Look(ref nextTamingGuardTick, "abyFullProg_nextTamingGuardTick", 0);
            Scribe_Values.Look(ref nextSigilFocusTick, "abyFullProg_nextSigilFocusTick", 0);
            Scribe_Values.Look(ref nextFoggedPortalTick, "abyFullProg_nextFoggedPortalTick", 0);
            Scribe_Values.Look(ref nextHordePressureTick, "abyFullProg_nextHordePressureTick", 0);
            Scribe_Values.Look(ref nextOrphanGateTick, "abyFullProg_nextOrphanGateTick", 0);
            Scribe_Values.Look(ref nextExtraHordeTick, "abyFullProg_nextExtraHordeTick", -1);
            Scribe_Values.Look(ref extraHordeBurstsUsed, "abyFullProg_extraHordeBurstsUsed", 0);
            Scribe_Values.Look(ref hordeSeenThisActivation, "abyFullProg_hordeSeenThisActivation", false);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            RelaxEarlyCapacitorProfilesOnce();

            if (map == null || Find.TickManager == null)
            {
                return;
            }

            int tick = Find.TickManager.TicksGame;
            BackfillSchedulesIfNeeded(tick);

            if (tick >= nextTamingGuardTick)
            {
                nextTamingGuardTick = tick + TamingGuardIntervalTicks + Math.Abs(map.uniqueID % 37);
                PreventAbyssalTaming();
            }

            if (tick >= nextSigilFocusTick)
            {
                nextSigilFocusTick = tick + SigilFocusIntervalTicks + Math.Abs(map.uniqueID % 23);
                MoveSigilsOffSummoningCircleFocus();
            }

            if (tick >= nextFoggedPortalTick)
            {
                nextFoggedPortalTick = tick + FoggedPortalIntervalTicks + Math.Abs(map.uniqueID % 41);
                RelocateFoggedAbyssalPortals();
            }

            if (tick >= nextHordePressureTick)
            {
                nextHordePressureTick = tick + HordePressureIntervalTicks;
                TickHordePressureBoost(tick);
            }

            if (tick >= nextOrphanGateTick)
            {
                nextOrphanGateTick = tick + OrphanGateIntervalTicks + Math.Abs(map.uniqueID % 53);
                AutoCollapseOrphanedCommandGates();
            }
        }

        private void BackfillSchedulesIfNeeded(int tick)
        {
            if (nextSlowTick > 0)
            {
                int baseTick = nextSlowTick;
                nextSlowTick = 0;
                if (nextTamingGuardTick <= 0) nextTamingGuardTick = baseTick;
                if (nextSigilFocusTick <= 0) nextSigilFocusTick = baseTick + 17;
                if (nextFoggedPortalTick <= 0) nextFoggedPortalTick = baseTick + 31;
                if (nextHordePressureTick <= 0) nextHordePressureTick = baseTick;
                if (nextOrphanGateTick <= 0) nextOrphanGateTick = baseTick + 47;
            }

            if (nextTamingGuardTick <= 0) nextTamingGuardTick = tick + 30;
            if (nextSigilFocusTick <= 0) nextSigilFocusTick = tick + 45;
            if (nextFoggedPortalTick <= 0) nextFoggedPortalTick = tick + 60;
            if (nextHordePressureTick <= 0) nextHordePressureTick = tick + 15;
            if (nextOrphanGateTick <= 0) nextOrphanGateTick = tick + 90;
        }

        private static void RelaxEarlyCapacitorProfilesOnce()
        {
            if (profilesRelaxed)
            {
                return;
            }

            profilesRelaxed = true;
            try
            {
                Type utilityType = GenTypes.GetTypeInAnyAssembly("AbyssalProtocol.AbyssalCircleCapacitorRitualUtility");
                if (utilityType == null)
                {
                    return;
                }

                DisableProfileMatch(utilityType, "UnstableBreachProfile");
                DisableProfileMatch(utilityType, "EmberHuntProfile");
                DisableProfileMatch(utilityType, "ArchonBeastProfile");
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Full progression hotfix could not relax early capacitor profiles: " + ex.Message);
            }
        }

        private static void DisableProfileMatch(Type utilityType, string fieldName)
        {
            FieldInfo field = utilityType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            object profile = field != null ? field.GetValue(null) : null;
            if (profile == null)
            {
                return;
            }

            FieldInfo ritualId = profile.GetType().GetField("RitualId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (ritualId != null)
            {
                ritualId.SetValue(profile, "__aby_no_required_lattice_" + fieldName);
            }
        }

        private void PreventAbyssalTaming()
        {
            RemoveAbyssalTameDesignations();
            Faction abyssalFaction = ResolveAbyssalFaction();
            if (abyssalFaction == null)
            {
                return;
            }

            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.SpawnedLivingPawnsFor(map);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsAbyssalPawn(pawn))
                {
                    continue;
                }

                if (pawn.Faction == Faction.OfPlayer || pawn.Faction == null)
                {
                    pawn.SetFaction(abyssalFaction);
                }
            }
        }

        private void RemoveAbyssalTameDesignations()
        {
            if (map.designationManager == null)
            {
                return;
            }

            List<Designation> all = map.designationManager.AllDesignations;
            if (all == null || all.Count == 0)
            {
                return;
            }

            List<Designation> toRemove = null;
            for (int i = 0; i < all.Count; i++)
            {
                Designation designation = all[i];
                if (designation != null && designation.def == DesignationDefOf.Tame && designation.target.Thing is Pawn pawn && IsAbyssalPawn(pawn))
                {
                    if (toRemove == null)
                    {
                        toRemove = new List<Designation>();
                    }

                    toRemove.Add(designation);
                }
            }

            if (toRemove == null)
            {
                return;
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                map.designationManager.RemoveDesignation(toRemove[i]);
            }
        }

        private void MoveSigilsOffSummoningCircleFocus()
        {
            IReadOnlyList<Thing> circles = ABY_RuntimeTargetCache.SpawnedThingsOfDefName(map, SummoningCircleDefName);
            for (int i = 0; i < circles.Count; i++)
            {
                Thing circle = circles[i];
                if (circle == null || circle.Destroyed || !circle.Spawned || circle.def == null)
                {
                    continue;
                }

                IntVec3 focus = GenAdj.OccupiedRect(circle.Position, circle.Rotation, circle.def.Size).CenterCell;
                if (!focus.IsValid || !focus.InBounds(map))
                {
                    continue;
                }

                List<Thing> thingsAtFocus = focus.GetThingList(map);
                for (int j = thingsAtFocus.Count - 1; j >= 0; j--)
                {
                    Thing thing = thingsAtFocus[j];
                    if (thing == null || thing.Destroyed || !IsAbyssalSigilThing(thing))
                    {
                        continue;
                    }

                    if (TryFindSafeCellNearCircle(circle, out IntVec3 destination))
                    {
                        MoveThingSafely(thing, destination);
                    }
                }
            }
        }

        private void RelocateFoggedAbyssalPortals()
        {
            for (int p = 0; p < PortalDefNames.Length; p++)
            {
                IReadOnlyList<Thing> portals = ABY_RuntimeTargetCache.SpawnedThingsOfDefName(map, PortalDefNames[p]);
                for (int i = portals.Count - 1; i >= 0; i--)
                {
                    Thing thing = portals[i];
                    if (thing == null || thing.Destroyed || !thing.Spawned)
                    {
                        continue;
                    }

                    if (!thing.PositionHeld.Fogged(map))
                    {
                        continue;
                    }

                    if (TryFindVisiblePerimeterCell(thing.PositionHeld, out IntVec3 destination))
                    {
                        MoveThingSafely(thing, destination);
                    }
                    else
                    {
                        thing.Destroy(DestroyMode.Vanish);
                    }
                }
            }
        }

        private void TickHordePressureBoost(int tick)
        {
            bool activeHorde = ABY_RuntimeTargetCache.HasSpawnedThingDef(map, CommandGateDefName);
            if (!activeHorde)
            {
                hordeSeenThisActivation = false;
                extraHordeBurstsUsed = 0;
                nextExtraHordeTick = -1;
                return;
            }

            if (!hordeSeenThisActivation)
            {
                hordeSeenThisActivation = true;
                extraHordeBurstsUsed = 0;
                nextExtraHordeTick = tick + 360;
                return;
            }

            if (extraHordeBurstsUsed >= MaxExtraHordeBurstsPerWave || tick < nextExtraHordeTick)
            {
                return;
            }

            if (TrySpawnExtraHordeBurst())
            {
                extraHordeBurstsUsed++;
                ABY_RuntimeTargetCache.NotifyLikelyStateChanged(map);
            }

            nextExtraHordeTick = tick + ExtraHordeIntervalTicks;
        }

        private bool TrySpawnExtraHordeBurst()
        {
            Faction faction = ResolveAbyssalFaction();
            if (faction == null || !TryFindVisiblePerimeterCell(IntVec3.Invalid, out IntVec3 cell))
            {
                return false;
            }

            string[] kinds = { "ABY_RiftImp", "ABY_EmberHound", "ABY_HexgunThrall", "ABY_ChainZealot" };
            int count = Rand.RangeInclusive(2, 4);
            bool spawnedAny = false;
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kinds[Rand.Range(0, kinds.Length)]);
                if (kind == null)
                {
                    continue;
                }

                Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                IntVec3 spawnCell = cell;
                CellFinder.TryFindRandomSpawnCellForPawnNear(cell, map, out spawnCell, 6);
                GenSpawn.Spawn(pawn, spawnCell, map);
                spawnedAny = true;
            }

            if (spawnedAny && ABY_VfxBudget.TrySpend(map, ABY_VfxBudgetCategory.CombatLight, 2))
            {
                FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, 1.2f);
            }

            return spawnedAny;
        }

        private void AutoCollapseOrphanedCommandGates()
        {
            if (!ABY_RuntimeTargetCache.HasSpawnedThingDef(map, CommandGateDefName))
            {
                return;
            }

            if (HasActiveAbyssalPortal() || HasLivingAbyssalHostiles())
            {
                return;
            }

            IReadOnlyList<Thing> gates = ABY_RuntimeTargetCache.SpawnedThingsOfDefName(map, CommandGateDefName);
            for (int i = gates.Count - 1; i >= 0; i--)
            {
                Thing thing = gates[i];
                if (thing != null && !thing.Destroyed && thing.Spawned)
                {
                    thing.Destroy(DestroyMode.KillFinalize);
                }
            }
        }

        private bool HasLivingAbyssalHostiles()
        {
            IReadOnlyList<Pawn> pawns = ABY_RuntimeTargetCache.SpawnedLivingPawnsFor(map);
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn.Dead || !IsAbyssalPawn(pawn))
                {
                    continue;
                }

                if (pawn.Faction != Faction.OfPlayer)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasActiveAbyssalPortal()
        {
            for (int i = 0; i < PortalDefNames.Length; i++)
            {
                if (ABY_RuntimeTargetCache.HasSpawnedThingDef(map, PortalDefNames[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private Faction ResolveAbyssalFaction()
        {
            return ABY_LargeModpackHotfixBUtility.ResolveAbyssalFaction();
        }

        private bool TryFindSafeCellNearCircle(Thing circle, out IntVec3 result)
        {
            result = IntVec3.Invalid;
            if (circle == null || circle.def == null)
            {
                return false;
            }

            IntVec3 interactionCell = circle.InteractionCell;
            if (IsValidLooseItemCell(interactionCell, circle))
            {
                result = interactionCell;
                return true;
            }

            CellRect occupied = GenAdj.OccupiedRect(circle.Position, circle.Rotation, circle.def.Size);
            for (int i = 0; i < GenRadial.NumCellsInRadius(7.9f); i++)
            {
                IntVec3 cell = circle.Position + GenRadial.RadialPattern[i];
                if (!occupied.Contains(cell) && IsValidLooseItemCell(cell, circle))
                {
                    result = cell;
                    return true;
                }
            }

            return false;
        }

        private bool IsValidLooseItemCell(IntVec3 cell, Thing circle)
        {
            if (!cell.IsValid || !cell.InBounds(map) || cell.Fogged(map) || !cell.Standable(map))
            {
                return false;
            }

            return cell.GetEdifice(map) == null;
        }

        private bool TryFindVisiblePerimeterCell(IntVec3 origin, out IntVec3 result)
        {
            result = IntVec3.Invalid;

            for (int i = 0; i < 180; i++)
            {
                IntVec3 cell = origin.IsValid
                    ? origin + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(38f))]
                    : CellFinder.RandomCell(map);

                if (IsValidPortalCell(cell))
                {
                    result = cell;
                    return true;
                }
            }

            return false;
        }

        private bool IsValidPortalCell(IntVec3 cell)
        {
            if (!cell.IsValid || !cell.InBounds(map) || cell.Fogged(map) || !cell.Standable(map))
            {
                return false;
            }

            if (cell.GetEdifice(map) != null)
            {
                return false;
            }

            if (map.areaManager != null && map.areaManager.Home != null && map.areaManager.Home[cell])
            {
                return false;
            }

            return cell.DistanceToEdge(map) >= 8;
        }

        private void MoveThingSafely(Thing thing, IntVec3 destination)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned || map == null || !destination.IsValid || !destination.InBounds(map))
            {
                return;
            }

            Map originalMap = thing.Map;
            IntVec3 originalPosition = thing.Position;
            Rot4 rotation = thing.Rotation;
            try
            {
                thing.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(thing, destination, map, rotation);
                ABY_RuntimeTargetCache.NotifyLikelyStateChanged(map);
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Failed to move abyssal runtime thing " + (thing.def?.defName ?? thing.ToStringSafe()) + " to " + destination + ": " + ex.Message);
                if (thing.Destroyed || thing.Spawned || originalMap == null || !originalPosition.IsValid || !originalPosition.InBounds(originalMap))
                {
                    return;
                }

                try
                {
                    GenSpawn.Spawn(thing, originalPosition, originalMap, rotation);
                    ABY_RuntimeTargetCache.NotifyLikelyStateChanged(originalMap);
                }
                catch (Exception rollbackEx)
                {
                    Log.Warning("[Abyssal Protocol] Failed to roll back abyssal runtime thing move for " + (thing.def?.defName ?? thing.ToStringSafe()) + ": " + rollbackEx.Message);
                }
            }
        }

        private static bool IsAbyssalSigilThing(Thing thing)
        {
            string defName = thing?.def?.defName ?? string.Empty;
            return defName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase) && defName.IndexOf("Sigil", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsAbyssalPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            string kindName = pawn.kindDef?.defName ?? string.Empty;
            string raceName = pawn.def?.defName ?? string.Empty;
            return kindName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase)
                || raceName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
