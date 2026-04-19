using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalArchonBeastPortalEncounterUtility
    {
        private const string BossPortalDefName = "ABY_RupturePortal";
        private const string CompanionPortalDefName = "ABY_ImpPortal";
        private const string EmberHoundKindDefName = "ABY_EmberHound";

        private const int BossPortalWarmupTicks = 156;
        private const int BossPortalLingerTicks = 320;

        private const int CompanionPortalWarmupTicks = 116;
        private const int CompanionPortalWarmupJitterTicks = 10;
        private const int CompanionPortalSpawnIntervalTicks = 24;
        private const int CompanionPortalLingerTicks = 220;

        private const float CompanionPortalMinRadius = 4.9f;
        private const float CompanionPortalMaxRadius = 8.8f;

        public static bool TryBeginEncounter(
            Map map,
            Faction faction,
            PawnKindDef bossKindDef,
            string bossLabel,
            IntVec3 preferredBossPortalCell,
            out IntVec3 bossPortalCell,
            out string failReason)
        {
            bossPortalCell = IntVec3.Invalid;
            failReason = null;

            if (map == null)
            {
                failReason = "No map available for archon portal encounter.";
                return false;
            }

            if (bossKindDef == null)
            {
                failReason = "Missing PawnKindDef for the archon portal encounter.";
                return false;
            }

            if (faction == null)
            {
                faction = AbyssalBossSummonUtility.ResolveHostileFaction();
            }
            if (faction == null)
            {
                failReason = "ABY_CircleFail_NoHostileFaction".Translate();
                return false;
            }

            ThingDef bossPortalDef = DefDatabase<ThingDef>.GetNamedSilentFail(BossPortalDefName);
            if (bossPortalDef == null)
            {
                failReason = "Missing ThingDef: ABY_RupturePortal";
                return false;
            }

            if (!TryResolveBossPortalCell(map, preferredBossPortalCell, out bossPortalCell))
            {
                failReason = "ABY_CircleFail_NoArrivalCell".Translate();
                return false;
            }

            Building_AbyssalRupturePortal bossPortal = ThingMaker.MakeThing(bossPortalDef) as Building_AbyssalRupturePortal;
            if (bossPortal == null)
            {
                failReason = "Failed to create rupture portal for archon encounter.";
                return false;
            }

            GenSpawn.Spawn(bossPortal, bossPortalCell, map, Rot4.Random);
            bossPortal.Initialize(
                faction,
                bossKindDef,
                BossPortalWarmupTicks,
                BossPortalLingerTicks,
                bossLabel.NullOrEmpty() ? "Archon Beast" : bossLabel);

            int companionPortalCount = Mathf.Max(1, GetCompanionPortalCount(map) + AbyssalBossOrchestrationUtility.GetCompanionPortalBonus("archon_beast"));
            TrySpawnCompanionHoundPortals(map, faction, bossPortalCell, 0, companionPortalCount);
            bool spawnedEscort = AbyssalBossOrchestrationUtility.TrySpawnEscortPackThroughPortal(map, faction, "archon_beast", "ABY_ArchonBeast", bossPortalCell, 620f, bossLabel, out string escortFailReason);
            if (!spawnedEscort && !escortFailReason.NullOrEmpty())
            {
                Log.Warning("[Abyssal Protocol] Archon escort plan warning: " + escortFailReason);
            }
            Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.20f);
            return true;
        }

        private static bool TryResolveBossPortalCell(Map map, IntVec3 preferredBossPortalCell, out IntVec3 bossPortalCell)
        {
            bossPortalCell = IntVec3.Invalid;

            if (preferredBossPortalCell.IsValid && ABY_Phase2PortalUtility.IsBossSafeStandableCell(map, preferredBossPortalCell, null, false))
            {
                bossPortalCell = preferredBossPortalCell;
                return true;
            }

            return AbyssalBossSummonUtility.TryFindBossArrivalCell(map, out bossPortalCell);
        }

        public static void TrySpawnCompanionHoundPortals(Map map, Faction faction, IntVec3 bossPortalCell, int warmupOffsetTicks = 0, int desiredPortalCount = -1, int staggerTicks = 26)
        {
            if (map == null || !bossPortalCell.IsValid || faction == null)
            {
                return;
            }

            ThingDef companionPortalDef = DefDatabase<ThingDef>.GetNamedSilentFail(CompanionPortalDefName);
            PawnKindDef emberHoundKindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(EmberHoundKindDefName);
            if (companionPortalDef == null || emberHoundKindDef == null)
            {
                return;
            }

            int resolvedPortalCount = desiredPortalCount > 0 ? desiredPortalCount : GetCompanionPortalCount(map);
            HashSet<IntVec3> usedCells = new HashSet<IntVec3> { bossPortalCell };

            for (int i = 0; i < resolvedPortalCount; i++)
            {
                if (!TryFindCompanionPortalCell(map, bossPortalCell, usedCells, out IntVec3 portalCell))
                {
                    break;
                }

                Building_AbyssalImpPortal companionPortal = ThingMaker.MakeThing(companionPortalDef) as Building_AbyssalImpPortal;
                if (companionPortal == null)
                {
                    continue;
                }

                int warmup = Mathf.Max(60, CompanionPortalWarmupTicks + warmupOffsetTicks + i * Mathf.Max(8, staggerTicks) + Rand.RangeInclusive(-CompanionPortalWarmupJitterTicks, CompanionPortalWarmupJitterTicks));
                GenSpawn.Spawn(companionPortal, portalCell, map, Rot4.Random);
                companionPortal.Initialize(
                    faction,
                    emberHoundKindDef,
                    1,
                    warmup,
                    CompanionPortalSpawnIntervalTicks,
                    CompanionPortalLingerTicks);

                ABY_SoundUtility.PlayAt("ABY_SigilChargePulse", portalCell, map);
                Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.RegisterRitualPulse(map, 0.09f + i * 0.03f);
                usedCells.Add(portalCell);
            }
        }

        public static int GetCompanionPortalCount(Map map)
        {
            int colonistCount = Mathf.Max(1, ABY_Phase2PortalUtility.CountActivePlayerColonists(map));
            if (colonistCount >= 10)
            {
                return 3;
            }

            if (colonistCount >= 5)
            {
                return 2;
            }

            return 1;
        }

        private static bool TryFindCompanionPortalCell(Map map, IntVec3 origin, HashSet<IntVec3> usedCells, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (map == null || !origin.IsValid)
            {
                return false;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;
            float preferredRadius = (CompanionPortalMinRadius + CompanionPortalMaxRadius) * 0.5f;

            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(origin, CompanionPortalMaxRadius, true))
            {
                if (!candidate.InBounds(map) || candidate.Fogged(map))
                {
                    continue;
                }

                if (usedCells != null && usedCells.Contains(candidate))
                {
                    continue;
                }

                if (HasUsedCellNearby(usedCells, candidate, 2.6f))
                {
                    continue;
                }

                float distance = candidate.DistanceTo(origin);
                if (distance < CompanionPortalMinRadius || distance > CompanionPortalMaxRadius)
                {
                    continue;
                }

                if (!ABY_Phase2PortalUtility.IsBossSafeStandableCell(map, candidate, null, false))
                {
                    continue;
                }

                if (HasBlockingPlayerBuildingNearby(map, candidate, 2.9f))
                {
                    continue;
                }

                float score = -Mathf.Abs(distance - preferredRadius);
                if (!candidate.Roofed(map))
                {
                    score += 0.35f;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = candidate;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            cell = bestCell;
            return true;
        }

        private static bool HasUsedCellNearby(HashSet<IntVec3> usedCells, IntVec3 candidate, float minDistance)
        {
            if (usedCells == null || !candidate.IsValid)
            {
                return false;
            }

            foreach (IntVec3 usedCell in usedCells)
            {
                if (!usedCell.IsValid)
                {
                    continue;
                }

                if (usedCell.DistanceTo(candidate) < minDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasBlockingPlayerBuildingNearby(Map map, IntVec3 center, float radius)
        {
            if (map == null || !center.IsValid)
            {
                return true;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                Building edifice = cell.GetEdifice(map);
                if (edifice != null && edifice.Faction == Faction.OfPlayer)
                {
                    return true;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    if (thing == null || thing.Destroyed)
                    {
                        continue;
                    }

                    if (thing.def.category == ThingCategory.Building && thing.Faction == Faction.OfPlayer)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
