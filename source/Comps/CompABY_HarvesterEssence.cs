using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_HarvesterEssence : ThingComp
    {
        private const int MaxTrackedCorpseIds = 512;
        private const int MaxTrackedDeathPawnIds = 512;

        private HashSet<int> registeredCorpseIds = new HashSet<int>();
        private HashSet<int> registeredDeathPawnIds = new HashSet<int>();
        private List<int> registeredCorpseIdOrder = new List<int>();
        private List<int> registeredDeathPawnIdOrder = new List<int>();
        private int currentHarvestCorpseId = -1;
        private int harvestWarmupTicksRemaining;
        private int essenceStacks;

        private Pawn PawnParent => parent as Pawn;
        private CompProperties_ABY_HarvesterEssence Props => (CompProperties_ABY_HarvesterEssence)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentHarvestCorpseId, "currentHarvestCorpseId", -1);
            Scribe_Values.Look(ref harvestWarmupTicksRemaining, "harvestWarmupTicksRemaining", 0);
            Scribe_Values.Look(ref essenceStacks, "essenceStacks", 0);
            Scribe_Collections.Look(ref registeredCorpseIds, "registeredCorpseIds", LookMode.Value);
            Scribe_Collections.Look(ref registeredDeathPawnIds, "registeredDeathPawnIds", LookMode.Value);
            Scribe_Collections.Look(ref registeredCorpseIdOrder, "registeredCorpseIdOrder", LookMode.Value);
            Scribe_Collections.Look(ref registeredDeathPawnIdOrder, "registeredDeathPawnIdOrder", LookMode.Value);
            if (registeredCorpseIds == null)
            {
                registeredCorpseIds = new HashSet<int>();
            }

            if (registeredDeathPawnIds == null)
            {
                registeredDeathPawnIds = new HashSet<int>();
            }

            if (registeredCorpseIdOrder == null)
            {
                registeredCorpseIdOrder = new List<int>();
            }

            if (registeredDeathPawnIdOrder == null)
            {
                registeredDeathPawnIdOrder = new List<int>();
            }

            BackfillTrackedOrderIfNeeded(registeredCorpseIds, registeredCorpseIdOrder);
            BackfillTrackedOrderIfNeeded(registeredDeathPawnIds, registeredDeathPawnIdOrder);
            TrimTrackedSetsIfNeeded();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            SyncEssenceHediff(PawnParent);
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn))
            {
                ResetHarvestState();
                return;
            }

            if (parent.IsHashIntervalTick(Math.Max(15, Props.scanIntervalTicks)))
            {
                ScanNearbyCorpses(pawn);
            }

            ProgressHarvest(pawn);
        }

        public bool NotifyNearbyAbyssalDeath(Pawn deadPawn, IntVec3 focusCell, Map map, int corpseThingId = -1)
        {
            Pawn pawn = PawnParent;
            if (!ShouldOperateNow(pawn) || deadPawn == null || map == null || pawn.MapHeld != map || !focusCell.IsValid)
            {
                return false;
            }

            if (!IsEligibleDeadPawn(pawn, deadPawn))
            {
                return false;
            }

            if (pawn.PositionHeld.DistanceTo(focusCell) > Props.nearbyDeathRadius + 0.35f)
            {
                return false;
            }

            TrimTrackedSetsIfNeeded();

            int deadPawnId = deadPawn.thingIDNumber;
            if (deadPawnId >= 0 && !TrackDeathPawnId(deadPawnId))
            {
                if (corpseThingId >= 0)
                {
                    TrackCorpseId(corpseThingId);
                }

                return false;
            }

            if (corpseThingId >= 0)
            {
                TrackCorpseId(corpseThingId);
            }

            GainEssence(pawn, Math.Max(0, Props.stackGainPerDeath), focusCell, false);
            return true;
        }

        private void ScanNearbyCorpses(Pawn pawn)
        {
            List<Thing> corpses = pawn.MapHeld?.listerThings?.ThingsInGroup(ThingRequestGroup.Corpse);
            if (corpses == null || corpses.Count == 0)
            {
                return;
            }

            TrimTrackedSetsIfNeeded();

            for (int i = 0; i < corpses.Count; i++)
            {
                Corpse corpse = corpses[i] as Corpse;
                if (!IsEligibleCorpse(pawn, corpse))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceTo(corpse.PositionHeld);
                if (distance <= Props.nearbyDeathRadius
                    && corpse.Age <= Props.corpseRecognitionMaxAgeTicks)
                {
                    int innerPawnId = corpse.InnerPawn?.thingIDNumber ?? -1;
                    bool alreadyRegisteredByDeathEvent = innerPawnId >= 0 && registeredDeathPawnIds.Contains(innerPawnId);
                    if (alreadyRegisteredByDeathEvent)
                    {
                        TrackCorpseId(corpse.thingIDNumber);
                    }
                    else if (TrackCorpseId(corpse.thingIDNumber))
                    {
                        if (innerPawnId >= 0)
                        {
                            TrackDeathPawnId(innerPawnId);
                        }

                        GainEssence(pawn, Math.Max(0, Props.stackGainPerDeath), corpse.PositionHeld, false);
                    }
                }

                if (currentHarvestCorpseId >= 0)
                {
                    continue;
                }

                if (distance <= Props.harvestRadius
                    && corpse.Age <= Props.freshCorpseMaxAgeTicks
                    && !HasAdjacentHostileThreat(pawn, Props.hostileInterferenceRadius))
                {
                    StartHarvest(corpse);
                }
            }
        }

        private void StartHarvest(Corpse corpse)
        {
            if (corpse == null)
            {
                return;
            }

            currentHarvestCorpseId = corpse.thingIDNumber;
            harvestWarmupTicksRemaining = Math.Max(1, Props.harvestWarmupTicks);
        }

        private void ProgressHarvest(Pawn pawn)
        {
            if (currentHarvestCorpseId < 0)
            {
                return;
            }

            Corpse corpse = FindTrackedCorpse(pawn.MapHeld, currentHarvestCorpseId);
            if (!IsEligibleCorpse(pawn, corpse)
                || corpse.Age > Props.freshCorpseMaxAgeTicks
                || pawn.PositionHeld.DistanceTo(corpse.PositionHeld) > Props.harvestRadius + 0.35f)
            {
                ResetHarvestState();
                return;
            }

            if (HasAdjacentHostileThreat(pawn, Props.hostileInterferenceRadius))
            {
                harvestWarmupTicksRemaining = Math.Max(harvestWarmupTicksRemaining, 24);
                return;
            }

            harvestWarmupTicksRemaining--;
            if (harvestWarmupTicksRemaining > 0)
            {
                return;
            }

            ExecuteHarvest(pawn, corpse);
            ResetHarvestState();
        }

        private void ExecuteHarvest(Pawn pawn, Corpse corpse)
        {
            if (pawn == null || corpse == null || corpse.Destroyed)
            {
                return;
            }

            TrackCorpseId(corpse.thingIDNumber);
            int innerPawnId = corpse.InnerPawn?.thingIDNumber ?? -1;
            if (innerPawnId >= 0)
            {
                TrackDeathPawnId(innerPawnId);
            }

            IntVec3 corpseCell = corpse.PositionHeld;
            Map map = corpse.MapHeld;
            corpse.Destroy(DestroyMode.Vanish);

            HealWorstInjury(pawn, Props.healInjuryAmount);
            GainEssence(pawn, Math.Max(0, Props.stackGainPerHarvest), corpseCell, true);

            if (map != null && corpseCell.IsValid)
            {
                FleckMaker.ThrowLightningGlow(corpseCell.ToVector3Shifted(), map, Props.glowScale);
                FleckMaker.ThrowMicroSparks(corpseCell.ToVector3Shifted(), map);
                FleckMaker.ThrowDustPuff(corpseCell.ToVector3Shifted(), map, 1.1f);
            }
        }

        private void GainEssence(Pawn pawn, int amount, IntVec3 focusCell, bool harvested)
        {
            if (pawn == null || amount <= 0)
            {
                return;
            }

            int newStacks = Mathf.Clamp(essenceStacks + amount, 0, Math.Max(1, Props.maxEssenceStacks));
            if (newStacks == essenceStacks)
            {
                return;
            }

            essenceStacks = newStacks;
            SyncEssenceHediff(pawn);

            if (pawn.MapHeld != null && focusCell.IsValid)
            {
                float glow = harvested ? Props.glowScale + 0.15f : Props.glowScale;
                FleckMaker.ThrowLightningGlow(focusCell.ToVector3Shifted(), pawn.MapHeld, glow);
            }
        }

        private void SyncEssenceHediff(Pawn pawn)
        {
            if (pawn?.health == null || Props.essenceHediffDefName.NullOrEmpty())
            {
                return;
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(Props.essenceHediffDefName);
            if (hediffDef == null)
            {
                return;
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
            if (essenceStacks <= 0)
            {
                if (existing != null)
                {
                    pawn.health.RemoveHediff(existing);
                }

                return;
            }

            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(existing);
            }

            existing.Severity = Math.Max(0.01f, essenceStacks);
        }

        private void ResetHarvestState()
        {
            currentHarvestCorpseId = -1;
            harvestWarmupTicksRemaining = 0;
        }

        private bool TrackCorpseId(int corpseId)
        {
            return TrackId(registeredCorpseIds, registeredCorpseIdOrder, corpseId, MaxTrackedCorpseIds);
        }

        private bool TrackDeathPawnId(int pawnId)
        {
            return TrackId(registeredDeathPawnIds, registeredDeathPawnIdOrder, pawnId, MaxTrackedDeathPawnIds);
        }

        private static bool TrackId(HashSet<int> ids, List<int> order, int id, int maxTracked)
        {
            if (ids == null || order == null || id < 0)
            {
                return false;
            }

            if (!ids.Add(id))
            {
                return false;
            }

            order.Add(id);
            TrimTrackedSet(ids, order, maxTracked);
            return true;
        }

        private void TrimTrackedSetsIfNeeded()
        {
            TrimTrackedSet(registeredCorpseIds, registeredCorpseIdOrder, MaxTrackedCorpseIds);
            TrimTrackedSet(registeredDeathPawnIds, registeredDeathPawnIdOrder, MaxTrackedDeathPawnIds);
        }

        private static void BackfillTrackedOrderIfNeeded(HashSet<int> ids, List<int> order)
        {
            if (ids == null || order == null || order.Count > 0 || ids.Count == 0)
            {
                return;
            }

            foreach (int id in ids)
            {
                if (id >= 0)
                {
                    order.Add(id);
                }
            }
        }

        private static void TrimTrackedSet(HashSet<int> ids, List<int> order, int maxTracked)
        {
            if (ids == null || order == null || ids.Count <= maxTracked)
            {
                return;
            }

            if (order.Count == 0)
            {
                BackfillTrackedOrderIfNeeded(ids, order);
            }

            int targetCount = Math.Max(1, maxTracked / 2);
            int removeBudget = Math.Max(0, ids.Count - targetCount);
            int writeIndex = 0;

            for (int i = 0; i < order.Count; i++)
            {
                int id = order[i];
                if (id < 0 || !ids.Contains(id))
                {
                    continue;
                }

                if (removeBudget > 0)
                {
                    ids.Remove(id);
                    removeBudget--;
                    continue;
                }

                order[writeIndex++] = id;
            }

            if (writeIndex < order.Count)
            {
                order.RemoveRange(writeIndex, order.Count - writeIndex);
            }

            if (ids.Count <= maxTracked)
            {
                return;
            }

            // Fallback for old saves where the historical order list could not represent every set entry.
            // Remove only the surplus, not the whole cache, so recent corpse/death memory remains intact.
            int fallbackRemoveBudget = ids.Count - targetCount;
            List<int> fallbackRemove = new List<int>(fallbackRemoveBudget);
            foreach (int id in ids)
            {
                if (fallbackRemove.Count >= fallbackRemoveBudget)
                {
                    break;
                }

                fallbackRemove.Add(id);
            }

            for (int i = 0; i < fallbackRemove.Count; i++)
            {
                ids.Remove(fallbackRemove[i]);
            }

            order.Clear();
            foreach (int id in ids)
            {
                if (id >= 0)
                {
                    order.Add(id);
                }
            }
        }

        private static bool ShouldOperateNow(Pawn pawn)
        {
            return pawn != null
                && pawn.Spawned
                && pawn.MapHeld != null
                && !pawn.Dead
                && !pawn.Downed;
        }

        private static Corpse FindTrackedCorpse(Map map, int corpseId)
        {
            if (map?.listerThings == null || corpseId < 0)
            {
                return null;
            }

            List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            if (corpses == null)
            {
                return null;
            }

            for (int i = 0; i < corpses.Count; i++)
            {
                if (corpses[i] is Corpse corpse && corpse.thingIDNumber == corpseId)
                {
                    return corpse;
                }
            }

            return null;
        }

        private static bool IsEligibleCorpse(Pawn owner, Corpse corpse)
        {
            if (owner == null || corpse?.InnerPawn == null || corpse.Destroyed)
            {
                return false;
            }

            Pawn innerPawn = corpse.InnerPawn;
            if (innerPawn == owner || !innerPawn.Dead)
            {
                return false;
            }

            if (!IsEligibleDeadPawn(owner, innerPawn))
            {
                return false;
            }

            return corpse.Spawned;
        }

        private static bool IsEligibleDeadPawn(Pawn owner, Pawn deadPawn)
        {
            if (owner == null || deadPawn == null || deadPawn == owner)
            {
                return false;
            }

            if (owner.Faction == null || deadPawn.Faction == null)
            {
                return false;
            }

            if (owner.Faction != deadPawn.Faction || ABY_FactionHostilityUtility.SafeHostileTo(owner, deadPawn))
            {
                return false;
            }

            if (!IsAbyssalPawn(deadPawn) || IsProtectedBossCorpse(deadPawn))
            {
                return false;
            }

            return true;
        }

        private static bool IsAbyssalPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.TryGetComp<CompAbyssalPawnController>() != null)
            {
                return true;
            }

            string defName = pawn.def?.defName;
            return !string.IsNullOrEmpty(defName) && defName.StartsWith("ABY_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProtectedBossCorpse(Pawn pawn)
        {
            DefModExtension_AbyssalDifficultyScaling extension = pawn.kindDef?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
            if (extension != null && string.Equals(extension.role ?? string.Empty, "boss", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string defName = pawn.def?.defName ?? string.Empty;
            return string.Equals(defName, "ABY_WardenOfAsh", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_ChoirEngine", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_ArchonBeast", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_ArchonOfRupture", StringComparison.OrdinalIgnoreCase)
                || string.Equals(defName, "ABY_ReactorSaint", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasAdjacentHostileThreat(Pawn pawn, float radius)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return false;
            }

            float maxDistance = Math.Max(0.8f, radius);
            IReadOnlyList<Pawn> pawns = pawn.MapHeld.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn other = pawns[i];
                if (other == null || other == pawn || other.Dead || other.Downed || !other.Spawned)
                {
                    continue;
                }

                if (!AbyssalThreatPawnUtility.IsValidHostileTarget(pawn, other))
                {
                    continue;
                }

                if (pawn.PositionHeld.DistanceTo(other.PositionHeld) <= maxDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private static void HealWorstInjury(Pawn pawn, float amount)
        {
            if (pawn?.health?.hediffSet == null || amount <= 0f)
            {
                return;
            }

            Hediff_Injury worstInjury = null;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (!(hediffs[i] is Hediff_Injury injury) || injury.IsPermanent())
                {
                    continue;
                }

                if (worstInjury == null || injury.Severity > worstInjury.Severity)
                {
                    worstInjury = injury;
                }
            }

            worstInjury?.Heal(amount);
        }
    }
}
