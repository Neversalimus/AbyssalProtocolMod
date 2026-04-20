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

        private HashSet<int> registeredCorpseIds = new HashSet<int>();
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
            if (registeredCorpseIds == null)
            {
                registeredCorpseIds = new HashSet<int>();
            }
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

        private void ScanNearbyCorpses(Pawn pawn)
        {
            List<Thing> corpses = pawn.MapHeld?.listerThings?.ThingsInGroup(ThingRequestGroup.Corpse);
            if (corpses == null || corpses.Count == 0)
            {
                return;
            }

            if (registeredCorpseIds.Count > MaxTrackedCorpseIds)
            {
                registeredCorpseIds.Clear();
            }

            for (int i = 0; i < corpses.Count; i++)
            {
                Corpse corpse = corpses[i] as Corpse;
                if (!IsEligibleCorpse(pawn, corpse))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceTo(corpse.PositionHeld);
                if (distance <= Props.nearbyDeathRadius
                    && corpse.Age <= Props.corpseRecognitionMaxAgeTicks
                    && registeredCorpseIds.Add(corpse.thingIDNumber))
                {
                    GainEssence(pawn, Math.Max(0, Props.stackGainPerDeath), corpse.PositionHeld, false);
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

            registeredCorpseIds.Add(corpse.thingIDNumber);
            IntVec3 corpseCell = corpse.PositionHeld;
            Map map = corpse.MapHeld;
            corpse.Destroy(DestroyMode.Vanish);

            HealWorstInjury(pawn, Props.healInjuryAmount);
            GainEssence(pawn, Math.Max(0, Props.stackGainPerHarvest), corpseCell, true);

            if (map != null && corpseCell.IsValid)
            {
                FleckMaker.ThrowLightningGlow(corpseCell.ToVector3Shifted(), map, Props.glowScale);
                FleckMaker.ThrowMicroSparks(corpseCell.ToVector3Shifted(), map);
                MoteMaker.ThrowDustPuff(corpseCell.ToVector3Shifted(), map, 1.1f);
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

            if (owner.Faction == null || innerPawn.Faction == null)
            {
                return false;
            }

            if (owner.Faction != innerPawn.Faction || owner.HostileTo(innerPawn))
            {
                return false;
            }

            if (!IsAbyssalPawn(innerPawn) || IsProtectedBossCorpse(innerPawn))
            {
                return false;
            }

            return corpse.Spawned;
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
            List<Pawn> pawns = pawn.MapHeld.mapPawns.AllPawnsSpawned;
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
