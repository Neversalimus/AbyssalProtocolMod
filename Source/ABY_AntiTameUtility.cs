using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class ABY_AntiTameUtility
    {
        private const string AbyssalPrefix = "ABY_";
        private const string AbyssalFactionDefName = "ABY_AbyssalHost";
        private static bool normalizedRaceDefs;

        public static void NormalizeAbyssalRaceDefsOnce()
        {
            if (normalizedRaceDefs)
            {
                return;
            }

            normalizedRaceDefs = true;

            try
            {
                TrainabilityDef noneTrainability = DefDatabase<TrainabilityDef>.GetNamedSilentFail("None") ?? TrainabilityDefOf.None;

                List<ThingDef> allThingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
                if (allThingDefs != null)
                {
                    for (int i = 0; i < allThingDefs.Count; i++)
                    {
                        ThingDef def = allThingDefs[i];
                        if (!IsAbyssalRaceDef(def) || def.race == null)
                        {
                            continue;
                        }

                        ApplyNonTameRaceSettings(def.race, noneTrainability);
                    }
                }

                List<PawnKindDef> allPawnKinds = DefDatabase<PawnKindDef>.AllDefsListForReading;
                if (allPawnKinds != null)
                {
                    for (int i = 0; i < allPawnKinds.Count; i++)
                    {
                        PawnKindDef kindDef = allPawnKinds[i];
                        if (!IsAbyssalPawnKindDef(kindDef))
                        {
                            continue;
                        }

                        ApplyNonTamePawnKindSettings(kindDef);
                    }
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("anti-tame-normalize", "[Abyssal Protocol] Anti-tame race normalization failed: " + ex.Message, 5000);
            }
        }

        public static void EnforceMap(Map map)
        {
            if (map == null)
            {
                return;
            }

            NormalizeAbyssalRaceDefsOnce();
            RemoveAbyssalAnimalDesignations(map);
            CancelAbyssalAnimalWorkflowJobs(map);
            ReassertAbyssalHostility(map);
        }

        public static void RemoveAbyssalAnimalDesignations(Map map)
        {
            if (map?.designationManager == null)
            {
                return;
            }

            List<Designation> allDesignations = map.designationManager.AllDesignations;
            if (allDesignations == null || allDesignations.Count == 0)
            {
                return;
            }

            List<Designation> toRemove = null;
            for (int i = 0; i < allDesignations.Count; i++)
            {
                Designation designation = allDesignations[i];
                if (designation == null || !IsAnimalWorkflowDesignation(designation.def))
                {
                    continue;
                }

                Pawn pawn = designation.target.Thing as Pawn;
                if (!IsAbyssalPawn(pawn))
                {
                    continue;
                }

                if (toRemove == null)
                {
                    toRemove = new List<Designation>();
                }

                toRemove.Add(designation);
            }

            if (toRemove == null)
            {
                return;
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                try
                {
                    if (toRemove[i] != null)
                    {
                        map.designationManager.RemoveDesignation(toRemove[i]);
                    }
                }
                catch (Exception ex)
                {
                    ABY_LogThrottleUtility.Warning("anti-tame-designation", "[Abyssal Protocol] Could not remove abyssal animal designation: " + ex.Message, 5000);
                }
            }
        }

        public static void CancelAbyssalAnimalWorkflowJobs(Map map)
        {
            if (map?.mapPawns == null)
            {
                return;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn actor = pawns[i];
                Job curJob = actor?.CurJob;
                if (curJob == null || actor.jobs == null)
                {
                    continue;
                }

                if (!IsAnimalWorkflowJob(curJob.def))
                {
                    continue;
                }

                Pawn targetPawn = curJob.targetA.Thing as Pawn;
                if (targetPawn == null)
                {
                    targetPawn = curJob.targetB.Thing as Pawn;
                }

                if (!IsAbyssalPawn(targetPawn))
                {
                    continue;
                }

                try
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
                    Messages.Message("Abyssal entities cannot be tamed, trained, slaughtered or released.", targetPawn, MessageTypeDefOf.RejectInput, false);
                }
                catch (Exception ex)
                {
                    ABY_LogThrottleUtility.Warning("anti-tame-job", "[Abyssal Protocol] Could not cancel abyssal animal workflow job: " + ex.Message, 5000);
                }
            }
        }

        public static void ReassertAbyssalHostility(Map map)
        {
            if (map?.mapPawns == null)
            {
                return;
            }

            Faction abyssalFaction = ResolveAbyssalFaction();
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
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

                NormalizePawnAnimalState(pawn);

                if (abyssalFaction != null && ShouldForceAbyssalFaction(pawn))
                {
                    try
                    {
                        pawn.SetFaction(abyssalFaction);
                    }
                    catch (Exception ex)
                    {
                        ABY_LogThrottleUtility.Warning("anti-tame-faction-" + (pawn?.thingIDNumber.ToString() ?? "unknown"), "[Abyssal Protocol] Could not restore abyssal pawn faction for " + SafePawnLabel(pawn) + ": " + ex.Message, 5000);
                    }
                }
            }
        }

        public static bool IsAbyssalPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Faction?.def?.defName == AbyssalFactionDefName)
            {
                return true;
            }

            string kindName = pawn.kindDef?.defName ?? string.Empty;
            string raceName = pawn.def?.defName ?? string.Empty;
            return kindName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase)
                || raceName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLiveCombatCapableAbyssalPawn(Pawn pawn)
        {
            if (!IsAbyssalPawn(pawn))
            {
                return false;
            }

            if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned || pawn.Downed)
            {
                return false;
            }

            if (pawn.InMentalState || pawn.health?.capacities == null)
            {
                return false;
            }

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return false;
            }

            if (pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }

            return pawn.Faction == null || Faction.OfPlayer == null || pawn.Faction.HostileTo(Faction.OfPlayer);
        }

        public static bool IsAbyssalRaceDef(ThingDef def)
        {
            return def != null
                && def.race != null
                && !def.defName.NullOrEmpty()
                && def.defName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAbyssalPawnKindDef(PawnKindDef def)
        {
            if (def == null || def.defName.NullOrEmpty())
            {
                return false;
            }

            if (def.defName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string raceName = def.race?.defName ?? string.Empty;
            return raceName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldForceAbyssalFaction(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                return false;
            }

            if (pawn.Faction == null || pawn.Faction == Faction.OfPlayer)
            {
                return true;
            }

            return pawn.Faction?.def?.defName != AbyssalFactionDefName && !pawn.HostileTo(Faction.OfPlayer);
        }

        private static void NormalizePawnAnimalState(Pawn pawn)
        {
            try
            {
                TrainabilityDef noneTrainability = DefDatabase<TrainabilityDef>.GetNamedSilentFail("None") ?? TrainabilityDefOf.None;
                if (pawn?.def?.race != null)
                {
                    ApplyNonTameRaceSettings(pawn.def.race, noneTrainability);
                }

                if (pawn?.kindDef != null)
                {
                    ApplyNonTamePawnKindSettings(pawn.kindDef);
                }
            }
            catch
            {
            }
        }

        private static void ApplyNonTameRaceSettings(RaceProperties race, TrainabilityDef noneTrainability)
        {
            if (race == null)
            {
                return;
            }

            race.trainability = noneTrainability;
            TrySetOptionalMember(race, "wildness", 1f);
            TrySetOptionalMember(race, "petness", 0f);
            TrySetOptionalMember(race, "nuzzleMtbHours", -1f);
            TrySetOptionalMember(race, "manhunterOnTameFailChance", 0f);
            TrySetOptionalMember(race, "herdAnimal", false);
            TrySetOptionalMember(race, "packAnimal", false);
            TrySetOptionalMember(race, "canBePredatorPrey", false);
        }

        private static void ApplyNonTamePawnKindSettings(PawnKindDef kindDef)
        {
            if (kindDef == null)
            {
                return;
            }

            kindDef.ecoSystemWeight = 0f;
            kindDef.canArriveManhunter = false;
            TrySetOptionalMember(kindDef, "wildness", 1f);
            TrySetOptionalMember(kindDef, "petness", 0f);
            TrySetOptionalMember(kindDef, "allowTaming", false);
            TrySetOptionalMember(kindDef, "canBeTamed", false);
            TrySetOptionalMember(kindDef, "canBeSlaughtered", false);
            TrySetOptionalMember(kindDef, "wildGroupSize", null);
        }

        private static void TrySetOptionalMember(object target, string memberName, object value)
        {
            if (target == null || memberName.NullOrEmpty())
            {
                return;
            }

            try
            {
                Type type = target.GetType();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                FieldInfo field = type.GetField(memberName, flags);
                if (field != null && CanAssignValue(field.FieldType, value))
                {
                    field.SetValue(target, value);
                    return;
                }

                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null && property.CanWrite && CanAssignValue(property.PropertyType, value))
                {
                    property.SetValue(target, value, null);
                }
            }
            catch
            {
            }
        }

        private static bool CanAssignValue(Type targetType, object value)
        {
            if (targetType == null)
            {
                return false;
            }

            if (value == null)
            {
                return !targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null;
            }

            Type valueType = value.GetType();
            if (targetType.IsAssignableFrom(valueType))
            {
                return true;
            }

            Type underlying = Nullable.GetUnderlyingType(targetType);
            return underlying != null && underlying.IsAssignableFrom(valueType);
        }

        private static bool IsAnimalWorkflowDesignation(DesignationDef def)
        {
            string defName = def?.defName ?? string.Empty;
            return string.Equals(defName, "Tame", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "Slaughter", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ReleaseAnimalToWild", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "Train", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "Hunt", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAnimalWorkflowJob(JobDef def)
        {
            string defName = def?.defName ?? string.Empty;
            return string.Equals(defName, "Tame", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "Train", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "Slaughter", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ReleaseAnimalToWild", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "Hunt", StringComparison.OrdinalIgnoreCase)
                || defName.IndexOf("Tame", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Slaughter", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Faction ResolveAbyssalFaction()
        {
            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail(AbyssalFactionDefName);
            if (def == null || Find.FactionManager == null)
            {
                return null;
            }

            Faction faction = Find.FactionManager.FirstFactionOfDef(def);
            if (faction != null)
            {
                return faction;
            }

            List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
            if (factions == null)
            {
                return null;
            }

            for (int i = 0; i < factions.Count; i++)
            {
                Faction candidate = factions[i];
                string factionDefName = candidate?.def?.defName ?? string.Empty;
                if (factionDefName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase)
                    || factionDefName.IndexOf("Abyssal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string SafePawnLabel(Pawn pawn)
        {
            try
            {
                return pawn?.LabelShortCap ?? pawn?.def?.defName ?? "unknown pawn";
            }
            catch
            {
                return "unknown pawn";
            }
        }
    }
}
