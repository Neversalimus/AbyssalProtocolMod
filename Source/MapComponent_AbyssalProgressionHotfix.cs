using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Compatibility guardrails for very large progression-style modpacks.
    /// This component intentionally avoids compile-time references to the rest of Abyssal Protocol
    /// so it can be carried forward safely as a small hotfix source file.
    /// </summary>
    public class MapComponent_AbyssalProgressionHotfix : MapComponent
    {
        private const int SlowTickInterval = 60;
        private const int ExtraHordeIntervalTicks = 720;
        private const int MaxExtraHordeBurstsPerWave = 4;
        private const string AbyssalPrefix = "ABY_";
        private const string CommandGateDefName = "ABY_HordeCommandGate";
        private const string ImpPortalDefName = "ABY_ImpPortal";
        private const string SummoningCircleDefName = "ABY_SummoningCircle";

        private static bool profilesRelaxed;
        private int nextSlowTick;
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
            if (tick < nextSlowTick)
            {
                return;
            }

            nextSlowTick = tick + SlowTickInterval;
            PreventAbyssalTaming();
            MoveSigilsOffSummoningCircleFocus();
            RelocateFoggedAbyssalPortals();
            TickHordePressureBoost(tick);
            AutoCollapseOrphanedCommandGates();
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
            if (map.designationManager != null)
            {
                List<Designation> toRemove = null;
                List<Designation> all = map.designationManager.AllDesignations;
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

                if (toRemove != null)
                {
                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        map.designationManager.RemoveDesignation(toRemove[i]);
                    }
                }
            }

            Faction abyssalFaction = ResolveAbyssalFaction();
            List<Pawn> pawns = map.mapPawns != null ? map.mapPawns.AllPawnsSpawned : null;
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsAbyssalPawn(pawn) || pawn.Dead)
                {
                    continue;
                }

                if (pawn.Faction == Faction.OfPlayer || pawn.Faction == null)
                {
                    if (abyssalFaction != null)
                    {
                        pawn.SetFaction(abyssalFaction);
                    }
                }
            }
        }

        private void MoveSigilsOffSummoningCircleFocus()
        {
            List<Thing> allThings = map.listerThings?.AllThings;
            if (allThings == null)
            {
                return;
            }

            for (int i = 0; i < allThings.Count; i++)
            {
                Thing circle = allThings[i];
                if (circle == null || circle.Destroyed || !circle.Spawned || circle.def?.defName != SummoningCircleDefName)
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
            List<Thing> allThings = map.listerThings?.AllThings;
            if (allThings == null)
            {
                return;
            }

            for (int i = allThings.Count - 1; i >= 0; i--)
            {
                Thing thing = allThings[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || !IsAbyssalPortal(thing))
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

        private void TickHordePressureBoost(int tick)
        {
            bool activeHorde = HasSpawnedThingDef(CommandGateDefName);
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

            if (spawnedAny)
            {
                FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, 1.2f);
            }

            return spawnedAny;
        }

        private void AutoCollapseOrphanedCommandGates()
        {
            if (HasActiveAbyssalPortal() || HasLivingAbyssalHostiles())
            {
                return;
            }

            List<Thing> allThings = map.listerThings?.AllThings;
            if (allThings == null)
            {
                return;
            }

            for (int i = allThings.Count - 1; i >= 0; i--)
            {
                Thing thing = allThings[i];
                if (thing != null && !thing.Destroyed && thing.Spawned && thing.def?.defName == CommandGateDefName)
                {
                    thing.Destroy(DestroyMode.KillFinalize);
                }
            }
        }

        private bool HasLivingAbyssalHostiles()
        {
            List<Pawn> pawns = map.mapPawns != null ? map.mapPawns.AllPawnsSpawned : null;
            if (pawns == null)
            {
                return false;
            }

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
            List<Thing> allThings = map.listerThings?.AllThings;
            if (allThings == null)
            {
                return false;
            }

            for (int i = 0; i < allThings.Count; i++)
            {
                if (IsAbyssalPortal(allThings[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasSpawnedThingDef(string defName)
        {
            List<Thing> allThings = map.listerThings?.AllThings;
            if (allThings == null)
            {
                return false;
            }

            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing != null && !thing.Destroyed && thing.Spawned && thing.def?.defName == defName)
                {
                    return true;
                }
            }

            return false;
        }

        private Faction ResolveAbyssalFaction()
        {
            List<Faction> factions = Find.FactionManager?.AllFactionsListForReading;
            if (factions == null)
            {
                return null;
            }

            for (int i = 0; i < factions.Count; i++)
            {
                Faction faction = factions[i];
                string defName = faction?.def?.defName ?? string.Empty;
                if (defName == "ABY_AbyssalHost" || defName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase) || defName.IndexOf("Abyssal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return faction;
                }
            }

            return null;
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

            if (cell.GetEdifice(map) != null)
            {
                return false;
            }

            return true;
        }

        private bool TryFindVisiblePerimeterCell(IntVec3 origin, out IntVec3 result)
        {
            result = IntVec3.Invalid;

            for (int i = 0; i < 240; i++)
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
            if (thing == null || thing.Destroyed || !thing.Spawned || !destination.IsValid)
            {
                return;
            }

            Rot4 rotation = thing.Rotation;
            thing.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(thing, destination, map, rotation);
        }

        private static bool IsAbyssalPortal(Thing thing)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned)
            {
                return false;
            }

            string defName = thing.def?.defName ?? string.Empty;
            return defName == ImpPortalDefName || (defName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase) && defName.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0);
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
