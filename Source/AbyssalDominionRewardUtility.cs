using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalDominionRewardUtility
    {
        private const string DominionShardThingDefName = "ABY_DominionCrownShard";
        private const string ResidueThingDefName = "ABY_AbyssalResidue";
        private const string HeraldFragmentThingDefName = "ABY_HeraldCoreFragment";
        private const string SpacerComponentThingDefName = "ComponentSpacer";
        private const string EmberHoundPawnKindDefName = "ABY_EmberHound";

        private const int CancelledCooldownTicksBase = 60000;
        private const int FailedCooldownTicksBase = 180000;
        private const int CompletedCooldownTicksBase = 150000;

        private const float FailurePortalMinRadius = 7.5f;
        private const float FailurePortalMaxRadius = 15.5f;
        private const int FailurePortalWarmupTicks = 36;
        private const int FailurePortalSpawnIntervalTicks = 14;
        private const int FailurePortalLingerTicks = 240;

        private struct RewardProfile
        {
            public int DominionShards;
            public int Residue;
            public int HeraldFragments;
            public int SpacerComponents;
        }

        public static int GetCooldownTicks(MapComponent_DominionCrisis crisis, MapComponent_DominionCrisis.DominionCrisisPhase phase)
        {
            if (crisis == null)
            {
                return 0;
            }

            switch (phase)
            {
                case MapComponent_DominionCrisis.DominionCrisisPhase.Cancelled:
                    return CancelledCooldownTicksBase + Mathf.Clamp(crisis.CancelledCount * 5000, 0, 30000);
                case MapComponent_DominionCrisis.DominionCrisisPhase.Failed:
                    return FailedCooldownTicksBase + Mathf.Clamp((crisis.FailureCount - 1) * 15000, 0, 90000);
                case MapComponent_DominionCrisis.DominionCrisisPhase.Completed:
                    return CompletedCooldownTicksBase + Mathf.Clamp((crisis.CompletionCount - 1) * 10000, 0, 60000);
                default:
                    return 0;
            }
        }

        public static string GetRewardForecastText(MapComponent_DominionCrisis crisis)
        {
            if (crisis == null)
            {
                return AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionRewardForecast_Pending", "forecast pending");
            }

            RewardProfile profile = BuildRewardProfile(Mathf.Max(1, crisis.CompletionCount + 1));
            return GetRewardProfileSummary(profile);
        }

        public static List<string> GetRewardConsoleLines(MapComponent_DominionCrisis crisis)
        {
            List<string> lines = new List<string>();
            if (crisis == null)
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionRewardConsoleIdle", "No dominion breach data has been recorded on this map yet."));
                return lines;
            }

            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionRewardConsoleForecast",
                "Projected next payout: {0}",
                GetRewardForecastText(crisis)));

            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionRewardConsoleRecord",
                "Record: {0}",
                crisis.GetReplayStatusValue()));

            lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionRewardConsoleCooldown",
                "Rearm window: {0}",
                crisis.GetCooldownValue()));

            if (!crisis.LastRewardSummary.NullOrEmpty())
            {
                lines.Add(AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_DominionRewardConsoleLast",
                    "Last outcome: {0}",
                    crisis.LastRewardSummary));
            }

            return lines;
        }

        public static string ApplyOutcome(MapComponent_DominionCrisis crisis, MapComponent_DominionCrisis.DominionCrisisPhase phase, IntVec3 focusCell)
        {
            if (crisis == null || crisis.CrisisMap == null)
            {
                return null;
            }

            switch (phase)
            {
                case MapComponent_DominionCrisis.DominionCrisisPhase.Completed:
                    return ApplyCompletionOutcome(crisis, focusCell);
                case MapComponent_DominionCrisis.DominionCrisisPhase.Failed:
                    return ApplyFailureOutcome(crisis, focusCell);
                case MapComponent_DominionCrisis.DominionCrisisPhase.Cancelled:
                    return ApplyCancelledOutcome(crisis, focusCell);
                default:
                    return null;
            }
        }

        private static string ApplyCompletionOutcome(MapComponent_DominionCrisis crisis, IntVec3 focusCell)
        {
            RewardProfile profile = BuildRewardProfile(Mathf.Max(1, crisis.CompletionCount));
            IntVec3 dropCell = ResolveDropCell(crisis, focusCell);
            SpawnRewardProfile(crisis.CrisisMap, dropCell, profile);

            crisis.AddExternalContamination(0.085f);
            if (dropCell.IsValid)
            {
                FleckMaker.ThrowLightningGlow(dropCell.ToVector3Shifted(), crisis.CrisisMap, 2.2f);
                FleckMaker.ThrowMicroSparks(dropCell.ToVector3Shifted(), crisis.CrisisMap);
                ABY_SoundUtility.PlayAt("ABY_SigilSpawnImpulse", dropCell, crisis.CrisisMap);
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionOutcome_CompletedSummary",
                "Recovered {0}",
                GetRewardProfileSummary(profile));
        }

        private static string ApplyFailureOutcome(MapComponent_DominionCrisis crisis, IntVec3 focusCell)
        {
            Map map = crisis.CrisisMap;
            IntVec3 center = ResolveDropCell(crisis, focusCell);
            Faction hostileFaction = AbyssalBossSummonUtility.ResolveHostileFaction();
            int impCount = Mathf.Clamp(3 + crisis.FailureCount, 3, 6);
            bool portalSpawned = false;

            if (hostileFaction != null && center.IsValid)
            {
                portalSpawned = ABY_Phase2PortalUtility.TrySpawnImpPortalNear(
                    map,
                    hostileFaction,
                    center,
                    FailurePortalMinRadius,
                    FailurePortalMaxRadius,
                    impCount,
                    FailurePortalWarmupTicks,
                    FailurePortalSpawnIntervalTicks,
                    FailurePortalLingerTicks,
                    out _);

                if (portalSpawned)
                {
                    TrySpawnFailureHound(map, hostileFaction, center);
                }
            }

            DamageNearbyPoweredTargets(map, center, 3 + Mathf.Clamp(crisis.FailureCount, 0, 2));
            crisis.AddExternalContamination(0.22f + Mathf.Clamp(crisis.FailureCount, 0, 3) * 0.03f);

            if (center.IsValid)
            {
                FleckMaker.ThrowLightningGlow(center.ToVector3Shifted(), map, 2.6f);
                ABY_SoundUtility.PlayAt("ABY_RupturePortalOpen", center, map);
            }

            return portalSpawned
                ? AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_DominionOutcome_FailedSummary",
                    "Backlash routed a breach leak, scorched nearby infrastructure, and forced a cooldown window.")
                : AbyssalSummoningConsoleUtility.TranslateOrFallback(
                    "ABY_DominionOutcome_FailedSummarySoft",
                    "Backlash scorched nearby infrastructure, raised contamination, and forced a cooldown window.");
        }

        private static string ApplyCancelledOutcome(MapComponent_DominionCrisis crisis, IntVec3 focusCell)
        {
            IntVec3 center = ResolveDropCell(crisis, focusCell);
            crisis.AddExternalContamination(0.07f);
            if (center.IsValid)
            {
                FleckMaker.ThrowLightningGlow(center.ToVector3Shifted(), crisis.CrisisMap, 1.3f);
            }

            return AbyssalSummoningConsoleUtility.TranslateOrFallback(
                "ABY_DominionOutcome_CancelledSummary",
                "The breach was damped early, but the lattice still needs time to cool and purge contamination.");
        }

        private static RewardProfile BuildRewardProfile(int completionIndex)
        {
            RewardProfile profile = new RewardProfile
            {
                DominionShards = completionIndex >= 4 ? 2 : 1,
                Residue = 18 + Mathf.Clamp(completionIndex - 1, 0, 6) * 4,
                HeraldFragments = completionIndex >= 2 ? 2 : 1,
                SpacerComponents = completionIndex >= 5 ? 1 : 0
            };

            profile.Residue = Mathf.Max(profile.Residue, Mathf.RoundToInt(profile.Residue * AbyssalDifficultyUtility.GetResidueRewardMultiplier()));
            profile.SpacerComponents = Mathf.Max(profile.SpacerComponents, Mathf.RoundToInt(profile.SpacerComponents * AbyssalDifficultyUtility.GetBonusLootMultiplier()));
            return profile;
        }

        private static string GetRewardProfileSummary(RewardProfile profile)
        {
            List<string> parts = new List<string>();
            AddResourceLabel(parts, DominionShardThingDefName, profile.DominionShards);
            AddResourceLabel(parts, ResidueThingDefName, profile.Residue);
            AddResourceLabel(parts, HeraldFragmentThingDefName, profile.HeraldFragments);
            AddResourceLabel(parts, SpacerComponentThingDefName, profile.SpacerComponents);
            return parts.Count > 0
                ? string.Join(", ", parts)
                : AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionRewardForecast_Pending", "forecast pending");
        }

        private static void AddResourceLabel(List<string> parts, string defName, int count)
        {
            if (count <= 0)
            {
                return;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            string label = def != null ? def.label : defName;
            parts.Add(count + " " + label);
        }

        private static void SpawnRewardProfile(Map map, IntVec3 dropCell, RewardProfile profile)
        {
            if (map == null || !dropCell.IsValid)
            {
                return;
            }

            TrySpawnRewardStack(map, dropCell, DominionShardThingDefName, profile.DominionShards);
            TrySpawnRewardStack(map, dropCell, ResidueThingDefName, profile.Residue);
            TrySpawnRewardStack(map, dropCell, HeraldFragmentThingDefName, profile.HeraldFragments);
            TrySpawnRewardStack(map, dropCell, SpacerComponentThingDefName, profile.SpacerComponents);
        }

        private static void TrySpawnRewardStack(Map map, IntVec3 nearCell, string defName, int count)
        {
            if (map == null || !nearCell.IsValid || count <= 0)
            {
                return;
            }

            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return;
            }

            int remaining = count;
            while (remaining > 0)
            {
                Thing thing = ThingMaker.MakeThing(def);
                int stackCount = Mathf.Clamp(remaining, 1, def.stackLimit);
                thing.stackCount = stackCount;
                GenPlace.TryPlaceThing(thing, nearCell, map, ThingPlaceMode.Near);
                remaining -= stackCount;
            }
        }

        private static IntVec3 ResolveDropCell(MapComponent_DominionCrisis crisis, IntVec3 fallbackCell)
        {
            if (crisis == null)
            {
                return fallbackCell;
            }

            if (crisis.SourceCell.IsValid)
            {
                return crisis.SourceCell;
            }

            if (crisis.SourceCircle != null && crisis.SourceCircle.Spawned)
            {
                return crisis.SourceCircle.PositionHeld;
            }

            return fallbackCell;
        }

        private static void TrySpawnFailureHound(Map map, Faction hostileFaction, IntVec3 nearCell)
        {
            if (map == null || hostileFaction == null || !nearCell.IsValid)
            {
                return;
            }

            if (ABY_Phase2PortalUtility.CountActivePlayerColonists(map) < 5)
            {
                return;
            }

            PawnKindDef houndKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(EmberHoundPawnKindDefName);
            if (houndKind == null)
            {
                return;
            }

            AbyssalHostileSummonUtility.TrySpawnHostilePack(
                map,
                houndKind,
                hostileFaction,
                nearCell,
                1,
                AbyssalSummoningConsoleUtility.TranslateOrFallback("ABY_DominionFailure_HoundPack", "dominion failure hunter"),
                string.Empty,
                string.Empty,
                out _,
                out _);
        }

        private static void DamageNearbyPoweredTargets(Map map, IntVec3 center, int maxTargets)
        {
            if (map == null || !center.IsValid || maxTargets <= 0)
            {
                return;
            }

            int affected = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 10.9f, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (!(thing is Building building) || building.Destroyed || building.Faction != Faction.OfPlayer)
                    {
                        continue;
                    }

                    if (building.GetComp<CompPowerTrader>() == null && building.GetComp<CompPowerBattery>() == null && !(building is Building_Turret))
                    {
                        continue;
                    }

                    building.TakeDamage(new DamageInfo(DamageDefOf.EMP, 5.5f, 0f, -1f));
                    if (Rand.Chance(0.6f))
                    {
                        building.TakeDamage(new DamageInfo(DamageDefOf.Burn, 4.5f, 0f, -1f));
                    }

                    affected++;
                    if (affected >= maxTargets)
                    {
                        return;
                    }
                }
            }
        }
    }
}
