using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbyssalProtocol
{
    public static class ABY_LargeModpackHotfixBUtility
    {
        private const string AbyssalPrefix = "ABY_";
        private const string AbyssalFactionDefName = "ABY_AbyssalHost";
        private const string HordeCommandGateDefName = "ABY_HordeCommandGate";
        private const string ImpPortalDefName = "ABY_ImpPortal";
        private const string RupturePortalDefName = "ABY_RupturePortal";
        private const int HordeQueueStaleGraceTicks = 900;

        public static bool IsAbyssalPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            try
            {
                if (ABY_AntiTameUtility.IsAbyssalPawn(pawn))
                {
                    return true;
                }
            }
            catch
            {
            }

            string kindName = pawn.kindDef?.defName ?? string.Empty;
            string raceName = pawn.def?.defName ?? string.Empty;
            return kindName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase)
                || raceName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase)
                || string.Equals(pawn.Faction?.def?.defName, AbyssalFactionDefName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLiveCombatCapableAbyssalPawn(Pawn pawn)
        {
            if (!IsAbyssalPawn(pawn))
            {
                return false;
            }

            if (pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.Downed)
            {
                return false;
            }

            return true;
        }

        public static bool IsLiveHostileAbyssalEnemy(Pawn pawn)
        {
            if (!IsLiveCombatCapableAbyssalPawn(pawn))
            {
                return false;
            }

            if (pawn.Faction == Faction.OfPlayer)
            {
                return false;
            }

            if (pawn.Faction == null)
            {
                return true;
            }

            try
            {
                return pawn.HostileTo(Faction.OfPlayer);
            }
            catch
            {
                return true;
            }
        }

        public static bool IsLiveCombatCapableAbyssalEncounterPawn(Pawn pawn)
        {
            return IsLiveHostileAbyssalEnemy(pawn);
        }

        public static void EnforceAntiAnimalWorkflow(Map map, bool cancelJobs)
        {
            if (map == null)
            {
                return;
            }

            try
            {
                ABY_AntiTameUtility.NormalizeAbyssalRaceDefsOnce();
            }
            catch
            {
            }

            RemoveAbyssalAnimalDesignations(map);
            if (cancelJobs)
            {
                CancelAbyssalAnimalWorkflowJobs(map);
            }
            ReassertAbyssalHostility(map);
        }

        public static void RemoveAbyssalAnimalDesignations(Map map)
        {
            if (map?.designationManager == null)
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
                    map.designationManager.RemoveDesignation(toRemove[i]);
                }
                catch (Exception ex)
                {
                    Log.Warning("[Abyssal Protocol] Package 13 could not remove abyssal animal workflow designation: " + ex.Message);
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
                if (curJob == null || actor.jobs == null || !IsAnimalWorkflowJob(curJob.def))
                {
                    continue;
                }

                if (!JobTargetsAbyssalPawn(curJob))
                {
                    continue;
                }

                try
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
                }
                catch (Exception ex)
                {
                    Log.Warning("[Abyssal Protocol] Package 13 could not cancel abyssal animal workflow job: " + ex.Message);
                }
            }
        }

        public static void ReassertAbyssalHostility(Map map)
        {
            if (map?.mapPawns == null)
            {
                return;
            }

            Faction abyssal = ResolveAbyssalFaction();
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsAbyssalPawn(pawn) || pawn.Destroyed || pawn.Dead)
                {
                    continue;
                }

                if (abyssal != null && ShouldForceAbyssalFaction(pawn))
                {
                    TrySetFaction(pawn, abyssal);
                }
            }
        }

        public static void EnsureDominionSliceAnchorHostile(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return;
            }

            Faction abyssal = ResolveAbyssalFaction();
            if (abyssal == null)
            {
                return;
            }

            if (thing.Faction != abyssal)
            {
                TrySetFaction(thing, abyssal);
            }
        }

        public static void EnsureDominionGateFriendly(Thing thing)
        {
            if (thing == null || thing.Destroyed || Faction.OfPlayer == null)
            {
                return;
            }

            if (thing.Faction != Faction.OfPlayer)
            {
                TrySetFaction(thing, Faction.OfPlayer);
            }
        }

        public static bool TryHardStopStaleHorde(Map map, string reason)
        {
            if (map == null)
            {
                return false;
            }

            MapComponent_AbyssalPortalWave portalWave = map.GetComponent<MapComponent_AbyssalPortalWave>();
            if (portalWave == null)
            {
                return false;
            }

            bool activeHordeWave = GetField<bool>(portalWave, "activeHordeWave");
            bool waveActive = false;
            try
            {
                waveActive = portalWave.IsWaveActive;
            }
            catch
            {
            }

            if (!activeHordeWave && !waveActive)
            {
                return false;
            }

            if (HasActiveHordeCommandGate(map, portalWave))
            {
                return false;
            }

            if (HasActiveHostileAbyssalPortal(map))
            {
                return false;
            }

            if (HasLiveCombatCapableAbyssalEnemies(map))
            {
                return false;
            }

            IList queued = GetField<IList>(portalWave, "queuedPortals");
            int queuedCount = queued != null ? queued.Count : 0;
            int nextPortalOpenTick = GetField<int>(portalWave, "nextPortalOpenTick");
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;

            if (queuedCount > 0 && QueueStillLooksValid(queued, nextPortalOpenTick, now))
            {
                return false;
            }

            if (ResetPortalWave(portalWave))
            {
                Log.Message("[Abyssal Protocol] Package 13 Horde Hard Stop v2 reset a stale horde encounter lock (" + (reason ?? "watchdog") + ").");
                return true;
            }

            return false;
        }

        public static bool HasLiveCombatCapableAbyssalEnemies(Map map)
        {
            if (map?.mapPawns == null)
            {
                return false;
            }

            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return false;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                if (IsLiveHostileAbyssalEnemy(pawns[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasActiveHostileAbyssalPortal(Map map)
        {
            if (map?.listerThings?.AllThings == null)
            {
                return false;
            }

            List<Thing> things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.def == null)
                {
                    continue;
                }

                string defName = thing.def.defName ?? string.Empty;
                if (IsFriendlyDominionPortalDef(defName))
                {
                    continue;
                }

                if (string.Equals(defName, ImpPortalDefName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(defName, RupturePortalDefName, StringComparison.OrdinalIgnoreCase)
                    || (defName.IndexOf("ABY_", StringComparison.OrdinalIgnoreCase) >= 0
                        && defName.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0
                        && !IsFriendlyDominionPortalDef(defName)))
                {
                    return true;
                }
            }

            return false;
        }

        public static Faction ResolveAbyssalFaction()
        {
            try
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

                Faction generated = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
                if (generated != null)
                {
                    if (!Find.FactionManager.AllFactionsListForReading.Contains(generated))
                    {
                        Find.FactionManager.Add(generated);
                    }
                    return generated;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Package 13 could not resolve ABY_AbyssalHost faction: " + ex.Message);
            }

            return null;
        }

        private static bool HasActiveHordeCommandGate(Map map, MapComponent_AbyssalPortalWave portalWave)
        {
            Thing fieldGate = GetField<Thing>(portalWave, "activeCommandGate");
            if (fieldGate != null && fieldGate.Spawned && !fieldGate.Destroyed)
            {
                return true;
            }

            ThingDef gateDef = DefDatabase<ThingDef>.GetNamedSilentFail(HordeCommandGateDefName);
            if (gateDef == null || map?.listerThings == null)
            {
                return false;
            }

            List<Thing> gates = map.listerThings.ThingsOfDef(gateDef);
            if (gates == null)
            {
                return false;
            }

            for (int i = 0; i < gates.Count; i++)
            {
                Thing gate = gates[i];
                if (gate != null && gate.Spawned && !gate.Destroyed)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool QueueStillLooksValid(IList queued, int nextPortalOpenTick, int now)
        {
            if (queued == null || queued.Count == 0)
            {
                return false;
            }

            if (nextPortalOpenTick >= 0 && now > 0 && nextPortalOpenTick < now - HordeQueueStaleGraceTicks)
            {
                return false;
            }

            bool allRequireCommandGate = true;
            for (int i = 0; i < queued.Count; i++)
            {
                object request = queued[i];
                if (request == null)
                {
                    continue;
                }

                bool requires = GetField<bool>(request, "RequiresCommandGate");
                if (!requires)
                {
                    allRequireCommandGate = false;
                    break;
                }
            }

            return !allRequireCommandGate;
        }

        private static bool ResetPortalWave(MapComponent_AbyssalPortalWave portalWave)
        {
            if (portalWave == null)
            {
                return false;
            }

            try
            {
                MethodInfo reset = typeof(MapComponent_AbyssalPortalWave).GetMethod("ResetWave", BindingFlags.Instance | BindingFlags.NonPublic);
                if (reset != null)
                {
                    reset.Invoke(portalWave, null);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Package 13 could not invoke ResetWave: " + ex.Message);
            }

            try
            {
                IList queued = GetField<IList>(portalWave, "queuedPortals");
                queued?.Clear();
                IList used = GetField<IList>(portalWave, "usedPortalCells");
                used?.Clear();
                IList fronts = GetField<IList>(portalWave, "frontAnchorCells");
                fronts?.Clear();

                SetField(portalWave, "activeCommandGate", null);
                SetField(portalWave, "activeCommandFrontIndex", -1);
                SetField(portalWave, "commandGateCollapsed", false);
                SetField(portalWave, "waveFaction", null);
                SetField(portalWave, "nextPortalOpenTick", -1);
                SetField(portalWave, "activeWavePrefersPerimeter", false);
                SetField(portalWave, "activeHordeWave", false);
                SetField(portalWave, "closureRewardPending", false);
                SetField(portalWave, "commandRewardGranted", false);
                SetField(portalWave, "activeHordeRewardSnapshot", null);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Package 13 fallback horde reset failed: " + ex.Message);
                return false;
            }
        }

        private static bool JobTargetsAbyssalPawn(Job job)
        {
            if (job == null)
            {
                return false;
            }

            return IsAbyssalPawn(job.targetA.Thing as Pawn)
                || IsAbyssalPawn(job.targetB.Thing as Pawn)
                || IsAbyssalPawn(job.targetC.Thing as Pawn);
        }

        private static bool IsAnimalWorkflowDesignation(DesignationDef def)
        {
            string defName = def?.defName ?? string.Empty;
            return string.Equals(defName, "Tame", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "Train", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "Slaughter", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ReleaseAnimalToWild", StringComparison.OrdinalIgnoreCase)
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
                || defName.IndexOf("Train", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Slaughter", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Release", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Hunt", StringComparison.OrdinalIgnoreCase) >= 0;
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

            return !string.Equals(pawn.Faction.def?.defName, AbyssalFactionDefName, StringComparison.OrdinalIgnoreCase)
                && !pawn.HostileTo(Faction.OfPlayer);
        }

        private static bool IsFriendlyDominionPortalDef(string defName)
        {
            return string.Equals(defName, "ABY_DominionGateCore", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_DominionPocketExit", StringComparison.OrdinalIgnoreCase)
                || defName.IndexOf("DominionGate", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("DominionPocketExit", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void TrySetFaction(Thing thing, Faction faction)
        {
            if (thing == null)
            {
                return;
            }

            try
            {
                thing.SetFaction(faction);
            }
            catch (Exception ex)
            {
                Log.Warning("[Abyssal Protocol] Package 13 could not set faction on " + (thing.def?.defName ?? "thing") + ": " + ex.Message);
            }
        }

        private static T GetField<T>(object instance, string fieldName)
        {
            object value = GetField(instance, fieldName);
            if (value is T cast)
            {
                return cast;
            }
            return default(T);
        }

        private static object GetField(object instance, string fieldName)
        {
            if (instance == null || fieldName.NullOrEmpty())
            {
                return null;
            }

            try
            {
                FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return field != null ? field.GetValue(instance) : null;
            }
            catch
            {
                return null;
            }
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            if (instance == null || fieldName.NullOrEmpty())
            {
                return;
            }

            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(instance, value);
            }
        }
    }
}
