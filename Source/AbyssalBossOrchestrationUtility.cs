using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalBossOrchestrationUtility
    {
        private sealed class BossEscortContext
        {
            public ABY_BossDifficultyProfileDef Profile;
            public ABY_BossEscalationPackageDef Package;
            public AbyssalEncounterDirectorUtility.EncounterPlan Plan;
            public float BaseBudget;
            public bool ReinforcementMode;
        }

        public static ABY_BossDifficultyProfileDef ResolveProfileByRitualId(string ritualId)
        {
            if (ritualId.NullOrEmpty())
            {
                return null;
            }

            int stage = AbyssalDifficultyUtility.GetProgressionStage();
            foreach (ABY_BossDifficultyProfileDef def in DefDatabase<ABY_BossDifficultyProfileDef>.AllDefsListForReading)
            {
                if (def != null && stage >= def.minProgressionStage && def.MatchesRitualId(ritualId))
                {
                    return def;
                }
            }

            return null;
        }

        public static ABY_BossDifficultyProfileDef ResolveProfileByBossKindDefName(string bossKindDefName)
        {
            if (bossKindDefName.NullOrEmpty())
            {
                return null;
            }

            int stage = AbyssalDifficultyUtility.GetProgressionStage();
            foreach (ABY_BossDifficultyProfileDef def in DefDatabase<ABY_BossDifficultyProfileDef>.AllDefsListForReading)
            {
                if (def != null && stage >= def.minProgressionStage && def.MatchesBossKindDefName(bossKindDefName))
                {
                    return def;
                }
            }

            return null;
        }

        private static ABY_BossDifficultyProfileDef ResolveProfile(string ritualId, string bossKindDefName)
        {
            ABY_BossDifficultyProfileDef profile = ResolveProfileByRitualId(ritualId);
            if (profile == null && !bossKindDefName.NullOrEmpty())
            {
                profile = ResolveProfileByBossKindDefName(bossKindDefName);
            }

            return profile;
        }

        public static bool HasBossEscortProfile(string ritualId)
        {
            ABY_BossDifficultyProfileDef profile = ResolveProfile(ritualId, null);
            return profile != null && !profile.escortPoolId.NullOrEmpty();
        }

        public static bool ShouldSpawnEscortAtBossRelease(string ritualId, string forcedPackageDefName = null)
        {
            ABY_BossDifficultyProfileDef profile = ResolveProfile(ritualId, null);
            if (profile == null)
            {
                return false;
            }

            ABY_BossEscalationPackageDef package = ResolveEscalationPackage(profile, forcedPackageDefName);
            return package != null && package.spawnEscortNearBossRelease;
        }

        public static AbyssalEncounterDirectorUtility.EncounterPlan BuildEscortPlan(string ritualId, Map map, float fallbackBudget, int? seed = null)
        {
            return BuildEscortPlanContext(ritualId, null, map, fallbackBudget, seed, null, false)?.Plan;
        }

        public static bool TrySpawnEscortPack(
            Map map,
            Faction faction,
            string ritualId,
            IntVec3 requestedArrivalCell,
            float fallbackBudget,
            string packLabel,
            out IntVec3 arrivalCell,
            out string failReason,
            string forcedPackageDefName = null,
            bool reinforcementMode = false,
            bool allowFollowupScheduling = true)
        {
            arrivalCell = IntVec3.Invalid;
            failReason = null;
            if (map == null || faction == null)
            {
                return false;
            }

            BossEscortContext context = BuildEscortPlanContext(ritualId, null, map, fallbackBudget, null, forcedPackageDefName, reinforcementMode);
            if (context == null || context.Plan == null || context.Plan.TotalUnits <= 0)
            {
                return false;
            }

            ABY_EncounterTelemetryUtility.RecordPlan(context.Plan);
            bool spawned = AbyssalHostileSummonUtility.TrySpawnHostilePack(
                map,
                context.Plan.ToHostilePackEntries(),
                faction,
                requestedArrivalCell,
                packLabel,
                null,
                null,
                false,
                out arrivalCell,
                out failReason);

            if (spawned && allowFollowupScheduling && !reinforcementMode)
            {
                TryScheduleDelayedReinforcement(context, map, packLabel, requestedArrivalCell);
            }

            return spawned;
        }

        public static bool TrySpawnEscortPackNearBoss(
            Map map,
            Faction faction,
            string ritualId,
            Pawn bossPawn,
            float fallbackBudget,
            string packLabel,
            out string failReason,
            string forcedPackageDefName = null,
            bool reinforcementMode = false,
            bool allowFollowupScheduling = true)
        {
            failReason = null;
            if (map == null || faction == null || bossPawn == null || bossPawn.Dead || !bossPawn.Spawned)
            {
                return false;
            }

            BossEscortContext context = BuildEscortPlanContext(ritualId, bossPawn.kindDef?.defName, map, fallbackBudget, null, forcedPackageDefName, reinforcementMode);
            if (context == null || context.Plan == null || context.Plan.TotalUnits <= 0)
            {
                return false;
            }

            ABY_EncounterTelemetryUtility.RecordPlan(context.Plan);
            bool spawned = AbyssalHostileSummonUtility.TrySpawnHostilePackAroundAnchor(
                map,
                context.Plan.ToHostilePackEntries(),
                faction,
                bossPawn.PositionHeld,
                packLabel,
                out failReason);

            if (spawned && allowFollowupScheduling && !reinforcementMode)
            {
                TryScheduleDelayedReinforcement(context, map, packLabel, bossPawn.PositionHeld);
            }

            return spawned;
        }

        public static bool TrySpawnEscortPackThroughPortal(
            Map map,
            Faction faction,
            string ritualId,
            string bossKindDefName,
            IntVec3 portalCell,
            float fallbackBudget,
            string packLabel,
            out string failReason,
            string forcedPackageDefName = null,
            bool reinforcementMode = false,
            bool allowFollowupScheduling = true)
        {
            failReason = null;
            if (map == null || faction == null || !portalCell.IsValid || !portalCell.InBounds(map))
            {
                return false;
            }

            BossEscortContext context = BuildEscortPlanContext(ritualId, bossKindDefName, map, fallbackBudget, null, forcedPackageDefName, reinforcementMode);
            if (context == null || context.Plan == null || context.Plan.TotalUnits <= 0)
            {
                return false;
            }

            ABY_EncounterTelemetryUtility.RecordPlan(context.Plan);
            bool spawned = AbyssalHostileSummonUtility.TrySpawnHostilePackThroughPortal(
                map,
                context.Plan.ToHostilePackEntries(),
                faction,
                portalCell,
                packLabel,
                out failReason);

            if (spawned && allowFollowupScheduling && !reinforcementMode)
            {
                TryScheduleDelayedReinforcement(context, map, packLabel, portalCell);
            }

            return spawned;
        }

        public static int GetCompanionPortalBonus(string ritualId)
        {
            ABY_BossDifficultyProfileDef profile = ResolveProfileByRitualId(ritualId);
            if (profile == null)
            {
                return 0;
            }

            ABY_BossEscalationPackageDef package = ResolveEscalationPackage(profile, null);
            int currentOrder = AbyssalDifficultyUtility.GetCurrentProfileOrder();
            int bonus = 0;
            if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_Dominion"))
            {
                bonus += profile.bonusCompanionPortalsAtDominion;
                bonus += package != null ? package.extraCompanionPortalsAtDominion : 0;
            }

            if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_FinalGate"))
            {
                bonus += profile.bonusCompanionPortalsAtFinalGate;
                bonus += package != null ? package.extraCompanionPortalsAtFinalGate : 0;
            }

            bonus += package != null ? package.extraCompanionPortals : 0;
            return bonus;
        }

        public static IntVec3 TryResolveActiveBossAnchorCell(Map map, string ritualId, string bossKindDefName, IntVec3 fallbackCell)
        {
            if (map == null)
            {
                return fallbackCell;
            }

            ABY_BossDifficultyProfileDef profile = !ritualId.NullOrEmpty()
                ? ResolveProfileByRitualId(ritualId)
                : ResolveProfileByBossKindDefName(bossKindDefName);

            if (profile == null)
            {
                return fallbackCell;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestDistance = float.MaxValue;
            IReadOnlyList<Pawn> pawns = map.mapPawns?.AllPawnsSpawned;
            if (pawns != null)
            {
                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn == null || pawn.kindDef == null || !pawn.Spawned || pawn.Dead)
                    {
                        continue;
                    }

                    if (!profile.MatchesBossKindDefName(pawn.kindDef.defName))
                    {
                        continue;
                    }

                    float distance = fallbackCell.IsValid ? pawn.Position.DistanceToSquared(fallbackCell) : 0f;
                    if (!bestCell.IsValid || distance < bestDistance)
                    {
                        bestCell = pawn.Position;
                        bestDistance = distance;
                    }
                }
            }

            return bestCell.IsValid ? bestCell : fallbackCell;
        }

        private static BossEscortContext BuildEscortPlanContext(string ritualId, string bossKindDefName, Map map, float fallbackBudget, int? seed, string forcedPackageDefName, bool reinforcementMode)
        {
            ABY_BossDifficultyProfileDef profile = ResolveProfile(ritualId, bossKindDefName);
            if (profile == null)
            {
                return null;
            }

            float baseBudget = fallbackBudget > 0f ? fallbackBudget : profile.fallbackEscortBudget;
            if (baseBudget <= 0.01f)
            {
                return null;
            }

            ABY_BossEscalationPackageDef package = ResolveEscalationPackage(profile, forcedPackageDefName);
            float budgetMultiplier = Mathf.Max(0.25f, profile.escortBudgetMultiplier <= 0f ? 1f : profile.escortBudgetMultiplier);
            string poolId = profile.escortPoolId;
            int baseTier = profile.escortBaseContentTier > 0 ? profile.escortBaseContentTier : GetFallbackEscortTier(ritualId);

            if (package != null)
            {
                if (reinforcementMode)
                {
                    if (!package.reinforcementPoolIdOverride.NullOrEmpty())
                    {
                        poolId = package.reinforcementPoolIdOverride;
                    }

                    budgetMultiplier *= Mathf.Max(0.10f, package.reinforcementBudgetMultiplier);
                    baseTier += Mathf.Max(0, package.reinforcementExtraContentTier);
                }
                else
                {
                    if (!package.escortPoolIdOverride.NullOrEmpty())
                    {
                        poolId = package.escortPoolIdOverride;
                    }

                    budgetMultiplier *= Mathf.Max(0.10f, package.escortBudgetMultiplier);
                    baseTier += Mathf.Max(0, package.escortExtraContentTier);
                }
            }

            if (poolId.NullOrEmpty())
            {
                return null;
            }

            AbyssalEncounterDirectorUtility.EncounterPlan plan = AbyssalEncounterDirectorUtility.BuildPlan(
                poolId,
                baseBudget * budgetMultiplier,
                baseTier,
                map,
                seed,
                null,
                null,
                profile.defName,
                package?.defName);

            return new BossEscortContext
            {
                Profile = profile,
                Package = package,
                Plan = plan,
                BaseBudget = baseBudget,
                ReinforcementMode = reinforcementMode
            };
        }

        private static ABY_BossEscalationPackageDef ResolveEscalationPackage(ABY_BossDifficultyProfileDef profile, string forcedPackageDefName)
        {
            if (profile == null)
            {
                return null;
            }

            if (!forcedPackageDefName.NullOrEmpty())
            {
                ABY_BossEscalationPackageDef forced = DefDatabase<ABY_BossEscalationPackageDef>.GetNamedSilentFail(forcedPackageDefName);
                return forced != null && forced.AllowsBossProfile(profile.defName) ? forced : null;
            }

            List<ABY_BossEscalationPackageDef> defs = new List<ABY_BossEscalationPackageDef>();
            if (profile.escalationPackageDefNames != null && profile.escalationPackageDefNames.Count > 0)
            {
                for (int i = 0; i < profile.escalationPackageDefNames.Count; i++)
                {
                    ABY_BossEscalationPackageDef def = DefDatabase<ABY_BossEscalationPackageDef>.GetNamedSilentFail(profile.escalationPackageDefNames[i]);
                    if (def != null)
                    {
                        defs.Add(def);
                    }
                }
            }
            else
            {
                defs.AddRange(DefDatabase<ABY_BossEscalationPackageDef>.AllDefsListForReading);
            }

            int currentOrder = AbyssalDifficultyUtility.GetCurrentProfileOrder();
            int progressionStage = AbyssalDifficultyUtility.GetProgressionStage();
            List<ABY_BossEscalationPackageDef> candidates = new List<ABY_BossEscalationPackageDef>();
            List<float> weights = new List<float>();
            float totalWeight = 0f;

            for (int i = 0; i < defs.Count; i++)
            {
                ABY_BossEscalationPackageDef def = defs[i];
                if (def == null || !def.AllowsBossProfile(profile.defName) || !def.IsAllowedForCurrentState(currentOrder, progressionStage))
                {
                    continue;
                }

                float weight = Mathf.Max(0.01f, def.selectionWeight);
                int hits = ABY_EncounterTelemetryUtility.GetRecentPackageHits(profile.escortPoolId, def.defName, Mathf.Max(0, def.recentPackageLookback));
                for (int hit = 0; hit < hits; hit++)
                {
                    weight *= Mathf.Clamp(def.recentPackagePenalty, 0.15f, 1f);
                }

                candidates.Add(def);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            float roll = Rand.Value * Mathf.Max(0.01f, totalWeight);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f)
                {
                    return candidates[i];
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static void TryScheduleDelayedReinforcement(BossEscortContext context, Map map, string packLabel, IntVec3 fallbackCell)
        {
            if (context == null || context.Profile == null || context.Package == null || map == null)
            {
                return;
            }

            ABY_BossEscalationPackageDef package = context.Package;
            if (!package.scheduleDelayedReinforcement || package.reinforcementBudgetMultiplier <= 0.01f)
            {
                return;
            }

            int delay = Mathf.Max(60, package.reinforcementDelayTicks + Rand.RangeInclusive(-Mathf.Max(0, package.reinforcementDelayJitterTicks), Mathf.Max(0, package.reinforcementDelayJitterTicks)));
            Current.Game?.GetComponent<ABY_BossEscalationGameComponent>()?.ScheduleEscort(new ABY_BossEscalationScheduledEscort
            {
                mapUniqueId = map.uniqueID,
                triggerTick = (Find.TickManager != null ? Find.TickManager.TicksGame : 0) + delay,
                ritualId = ResolvePrimaryRitualId(context.Profile),
                bossKindDefName = ResolvePrimaryBossKindDefName(context.Profile),
                packageDefName = package.defName,
                packLabel = packLabel ?? ResolvePrimaryBossKindDefName(context.Profile),
                fallbackCell = fallbackCell,
                fallbackBudget = context.BaseBudget
            });
        }

        private static string ResolvePrimaryRitualId(ABY_BossDifficultyProfileDef profile)
        {
            if (profile == null || profile.ritualIds == null || profile.ritualIds.Count == 0)
            {
                return string.Empty;
            }

            return profile.ritualIds[0] ?? string.Empty;
        }

        private static string ResolvePrimaryBossKindDefName(ABY_BossDifficultyProfileDef profile)
        {
            if (profile == null || profile.bossPawnKindDefNames == null || profile.bossPawnKindDefNames.Count == 0)
            {
                return string.Empty;
            }

            return profile.bossPawnKindDefNames[0] ?? string.Empty;
        }

        private static int GetFallbackEscortTier(string ritualId)
        {
            switch ((ritualId ?? string.Empty).ToLowerInvariant())
            {
                case "warden_of_ash":
                    return 2;
                case "archon_beast":
                    return 4;
                case "archon_of_rupture":
                    return 5;
                case "choir_engine":
                    return 3;
                case "reactor_saint":
                    return 5;
                default:
                    return 2;
            }
        }
    }
}
