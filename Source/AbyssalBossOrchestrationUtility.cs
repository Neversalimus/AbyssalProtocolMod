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
            public bool IsGuaranteedFallback;
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
            return BuildBestEscortContext(ritualId, null, map, fallbackBudget, seed, null, false)?.Plan;
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

            BossEscortContext context = BuildBestEscortContext(ritualId, null, map, fallbackBudget, null, forcedPackageDefName, reinforcementMode);
            if (!HasUsablePlan(context))
            {
                failReason = "No valid boss escort plan available for " + (ritualId ?? "escort") + ".";
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

            if (!spawned && !context.IsGuaranteedFallback)
            {
                BossEscortContext fallbackContext = BuildGuaranteedFallbackContext(ritualId, null, map, fallbackBudget, reinforcementMode);
                if (HasUsablePlan(fallbackContext))
                {
                    ABY_EncounterTelemetryUtility.RecordPlan(fallbackContext.Plan);
                    spawned = AbyssalHostileSummonUtility.TrySpawnHostilePack(
                        map,
                        fallbackContext.Plan.ToHostilePackEntries(),
                        faction,
                        requestedArrivalCell,
                        packLabel,
                        null,
                        null,
                        false,
                        out arrivalCell,
                        out failReason);
                    if (spawned)
                    {
                        context = fallbackContext;
                    }
                }
            }

            if (spawned)
            {
                failReason = null;
                if (allowFollowupScheduling && !reinforcementMode)
                {
                    TryScheduleDelayedReinforcement(context, map, packLabel, requestedArrivalCell);
                }
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

            BossEscortContext context = BuildBestEscortContext(ritualId, bossPawn.kindDef?.defName, map, fallbackBudget, null, forcedPackageDefName, reinforcementMode);
            if (!HasUsablePlan(context))
            {
                failReason = "No valid boss escort plan available for " + (ritualId ?? bossPawn.kindDef?.defName ?? "escort") + ".";
                return false;
            }

            ABY_EncounterTelemetryUtility.RecordPlan(context.Plan);
            IntVec3 bossAnchorCell = ResolveBossEscortAnchorCell(bossPawn, map);
            List<AbyssalHostileSummonUtility.HostilePackEntry> primaryEntries = context.Plan.ToHostilePackEntries();
            bool spawned = AbyssalHostileSummonUtility.TrySpawnHostilePackAroundAnchor(
                map,
                primaryEntries,
                faction,
                bossAnchorCell,
                packLabel,
                out failReason);

            if (spawned && CountNearbyEscortPawns(map, faction, bossPawn, context.Profile, 35f) <= 0)
            {
                spawned = false;
                failReason = "Escort plan reported success, but no escort pawns appeared near the boss.";
            }

            if (!spawned)
            {
                BossEscortContext fallbackContext = BuildGuaranteedFallbackContext(ritualId, bossPawn.kindDef?.defName, map, fallbackBudget, reinforcementMode);
                if (HasUsablePlan(fallbackContext))
                {
                    ABY_EncounterTelemetryUtility.RecordPlan(fallbackContext.Plan);
                    spawned = AbyssalHostileSummonUtility.TrySpawnHostilePackAroundAnchorWide(
                        map,
                        fallbackContext.Plan.ToHostilePackEntries(),
                        faction,
                        bossAnchorCell,
                        packLabel,
                        7,
                        20,
                        out List<Pawn> fallbackSpawnedPawns,
                        out failReason);

                    if (spawned && fallbackSpawnedPawns != null && fallbackSpawnedPawns.Count > 0)
                    {
                        context = fallbackContext;
                    }
                    else
                    {
                        spawned = false;
                    }
                }
            }

            if (spawned)
            {
                failReason = null;
                if (allowFollowupScheduling && !reinforcementMode)
                {
                    TryScheduleDelayedReinforcement(context, map, packLabel, bossAnchorCell);
                }
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

            BossEscortContext context = BuildBestEscortContext(ritualId, bossKindDefName, map, fallbackBudget, null, forcedPackageDefName, reinforcementMode);
            if (!HasUsablePlan(context))
            {
                failReason = "No valid boss escort plan available for " + (ritualId ?? bossKindDefName ?? "escort") + ".";
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

            if (!spawned && !context.IsGuaranteedFallback)
            {
                BossEscortContext fallbackContext = BuildGuaranteedFallbackContext(ritualId, bossKindDefName, map, fallbackBudget, reinforcementMode);
                if (HasUsablePlan(fallbackContext))
                {
                    ABY_EncounterTelemetryUtility.RecordPlan(fallbackContext.Plan);
                    spawned = AbyssalHostileSummonUtility.TrySpawnHostilePackThroughPortal(
                        map,
                        fallbackContext.Plan.ToHostilePackEntries(),
                        faction,
                        portalCell,
                        packLabel,
                        out failReason);
                    if (spawned)
                    {
                        context = fallbackContext;
                    }
                }
            }

            if (spawned)
            {
                failReason = null;
                if (allowFollowupScheduling && !reinforcementMode)
                {
                    TryScheduleDelayedReinforcement(context, map, packLabel, portalCell);
                }
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

        private static IntVec3 ResolveBossEscortAnchorCell(Pawn bossPawn, Map map)
        {
            if (bossPawn == null)
            {
                return IntVec3.Invalid;
            }

            CellRect rect = bossPawn.OccupiedRect();
            if (!rect.IsEmpty)
            {
                IntVec3 center = rect.CenterCell;
                if (center.IsValid && (map == null || center.InBounds(map)))
                {
                    return center;
                }
            }

            IntVec3 fallback = bossPawn.PositionHeld;
            if (fallback.IsValid && (map == null || fallback.InBounds(map)))
            {
                return fallback;
            }

            return IntVec3.Invalid;
        }

        private static int CountNearbyEscortPawns(Map map, Faction faction, Pawn bossPawn, ABY_BossDifficultyProfileDef profile, float radius)
        {
            if (map == null || faction == null || bossPawn == null || map.mapPawns == null)
            {
                return 0;
            }

            IntVec3 anchor = ResolveBossEscortAnchorCell(bossPawn, map);
            float maxDistanceSq = radius * radius;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            if (pawns == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == null || pawn == bossPawn || !pawn.Spawned || pawn.Dead || pawn.Faction != faction)
                {
                    continue;
                }

                if (profile != null && pawn.kindDef != null && profile.MatchesBossKindDefName(pawn.kindDef.defName))
                {
                    continue;
                }

                if ((pawn.Position - anchor).LengthHorizontalSquared <= maxDistanceSq)
                {
                    count++;
                }
            }

            return count;
        }

        private static BossEscortContext BuildBestEscortContext(string ritualId, string bossKindDefName, Map map, float fallbackBudget, int? seed, string forcedPackageDefName, bool reinforcementMode)
        {
            BossEscortContext context = BuildEscortPlanContext(ritualId, bossKindDefName, map, fallbackBudget, seed, forcedPackageDefName, reinforcementMode, true);
            if (HasUsablePlan(context))
            {
                return context;
            }

            if (forcedPackageDefName.NullOrEmpty())
            {
                context = BuildEscortPlanContext(ritualId, bossKindDefName, map, fallbackBudget, seed, null, reinforcementMode, false);
                if (HasUsablePlan(context))
                {
                    return context;
                }
            }

            return BuildGuaranteedFallbackContext(ritualId, bossKindDefName, map, fallbackBudget, reinforcementMode);
        }

        private static bool HasUsablePlan(BossEscortContext context)
        {
            return context != null && context.Plan != null && context.Plan.TotalUnits > 0;
        }

        private static BossEscortContext BuildGuaranteedFallbackContext(string ritualId, string bossKindDefName, Map map, float fallbackBudget, bool reinforcementMode)
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

            int baseTier = profile.escortBaseContentTier > 0 ? profile.escortBaseContentTier : GetFallbackEscortTier(ritualId);
            string poolId = profile.escortPoolId;
            if (poolId.NullOrEmpty())
            {
                return null;
            }

            AbyssalEncounterDirectorUtility.EncounterPlan plan = BuildGuaranteedFallbackPlan(ritualId, bossKindDefName, poolId, baseBudget, baseTier, profile);
            if (plan == null || plan.TotalUnits <= 0)
            {
                return null;
            }

            return new BossEscortContext
            {
                Profile = profile,
                Package = null,
                Plan = plan,
                BaseBudget = baseBudget,
                ReinforcementMode = reinforcementMode,
                IsGuaranteedFallback = true
            };
        }

        private static AbyssalEncounterDirectorUtility.EncounterPlan BuildGuaranteedFallbackPlan(string ritualId, string bossKindDefName, string poolId, float baseBudget, int baseTier, ABY_BossDifficultyProfileDef profile)
        {
            if (poolId.NullOrEmpty())
            {
                return null;
            }

            int allowedContentTier = AbyssalDifficultyUtility.GetAllowedContentTier(baseTier);
            float escortBudgetMultiplier = Mathf.Max(0.25f, profile != null && profile.escortBudgetMultiplier > 0f ? profile.escortBudgetMultiplier : 1f);
            float planBudget = Math.Max(1f, baseBudget * escortBudgetMultiplier * AbyssalDifficultyUtility.GetEncounterBudgetMultiplier());
            float remainingBudget = planBudget;

            AbyssalEncounterDirectorUtility.EncounterPlan plan = new AbyssalEncounterDirectorUtility.EncounterPlan
            {
                PoolId = poolId,
                BossProfileDefName = profile?.defName ?? string.Empty,
                AllowedContentTier = allowedContentTier,
                Budget = planBudget
            };

            int currentOrder = AbyssalDifficultyUtility.GetCurrentProfileOrder();
            string resolvedId = (ritualId ?? string.Empty).ToLowerInvariant();
            string bossDefName = bossKindDefName ?? string.Empty;

            if (resolvedId == "archon_of_rupture" || string.Equals(bossDefName, "ABY_ArchonOfRupture", StringComparison.OrdinalIgnoreCase))
            {
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_EmberHound", poolId, allowedContentTier, true);
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_ChainZealot", poolId, allowedContentTier, true);
                if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_Rupture"))
                {
                    TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_NullPriest", poolId, allowedContentTier, false);
                }
                if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_Dominion"))
                {
                    TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_RiftSniper", poolId, allowedContentTier, false);
                }
            }
            else if (resolvedId == "archon_beast" || string.Equals(bossDefName, "ABY_ArchonBeast", StringComparison.OrdinalIgnoreCase))
            {
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_EmberHound", poolId, allowedContentTier, true);
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_ChainZealot", poolId, allowedContentTier, true);
                if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_Rupture"))
                {
                    TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_NullPriest", poolId, allowedContentTier, false);
                }
                if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_Dominion"))
                {
                    TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_RiftSniper", poolId, allowedContentTier, false);
                }
            }
            else if (resolvedId == "reactor_saint" || string.Equals(bossDefName, "ABY_ReactorSaint", StringComparison.OrdinalIgnoreCase))
            {
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_HexgunThrall", poolId, allowedContentTier, true);
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_ChainZealot", poolId, allowedContentTier, true);
                if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_Rupture"))
                {
                    TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_NullPriest", poolId, allowedContentTier, false);
                }
                if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_Dominion"))
                {
                    TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_RiftSniper", poolId, allowedContentTier, false);
                }
                if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_FinalGate"))
                {
                    TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_HaloHusk", poolId, allowedContentTier, false);
                }
            }
            else
            {
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_HexgunThrall", poolId, allowedContentTier, true);
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_ChainZealot", poolId, allowedContentTier, true);
            }

            if (plan.TotalUnits <= 0)
            {
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_HexgunThrall", poolId, allowedContentTier, true);
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_EmberHound", poolId, allowedContentTier, true);
                TryAddGuaranteedEscortKind(plan, ref remainingBudget, "ABY_ChainZealot", poolId, allowedContentTier, true);
            }

            return plan.TotalUnits > 0 ? plan : null;
        }

        private static bool TryAddGuaranteedEscortKind(
            AbyssalEncounterDirectorUtility.EncounterPlan plan,
            ref float remainingBudget,
            string pawnKindDefName,
            string poolId,
            int allowedContentTier,
            bool forceAdd)
        {
            if (plan == null || pawnKindDefName.NullOrEmpty())
            {
                return false;
            }

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(pawnKindDefName);
            DefModExtension_AbyssalDifficultyScaling extension = kindDef?.GetModExtension<DefModExtension_AbyssalDifficultyScaling>();
            if (kindDef == null || extension == null)
            {
                return false;
            }

            if (!CanUseFallbackKind(extension, poolId, allowedContentTier))
            {
                return false;
            }

            int currentCount = plan.GetCount(kindDef.defName);
            if (extension.maxPlanCount > 0 && currentCount >= extension.maxPlanCount)
            {
                return false;
            }

            float budgetCost = Math.Max(1f, extension.budgetCost);
            if (!forceAdd && budgetCost > remainingBudget && plan.TotalUnits > 0)
            {
                return false;
            }

            bool incremented = false;
            for (int i = 0; i < plan.Entries.Count; i++)
            {
                AbyssalEncounterDirectorUtility.DirectedEntry existing = plan.Entries[i];
                if (existing != null && existing.KindDef == kindDef)
                {
                    existing.Count++;
                    incremented = true;
                    break;
                }
            }

            if (!incremented)
            {
                plan.Entries.Add(new AbyssalEncounterDirectorUtility.DirectedEntry
                {
                    KindDef = kindDef,
                    Count = 1,
                    BudgetCost = budgetCost,
                    Role = extension.role ?? "assault"
                });
            }

            remainingBudget = Math.Max(0f, remainingBudget - budgetCost);
            return true;
        }

        private static bool CanUseFallbackKind(DefModExtension_AbyssalDifficultyScaling extension, string poolId, int allowedContentTier)
        {
            if (extension == null)
            {
                return false;
            }

            if (extension.encounterPools == null || !ListContainsIgnoreCase(extension.encounterPools, poolId))
            {
                return false;
            }

            if (!AbyssalDifficultyUtility.CanUseByDifficulty(extension, AbyssalDifficultyUtility.GetCurrentProfile()))
            {
                return false;
            }

            return extension.contentTier <= allowedContentTier;
        }

        private static bool ListContainsIgnoreCase(List<string> values, string sought)
        {
            if (values == null || values.Count == 0 || sought.NullOrEmpty())
            {
                return false;
            }

            string safe = sought.ToLowerInvariant();
            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i];
                if (!value.NullOrEmpty() && value.ToLowerInvariant() == safe)
                {
                    return true;
                }
            }

            return false;
        }

        private static BossEscortContext BuildEscortPlanContext(string ritualId, string bossKindDefName, Map map, float fallbackBudget, int? seed, string forcedPackageDefName, bool reinforcementMode, bool allowPackageSelection)
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

            ABY_BossEscalationPackageDef package = allowPackageSelection ? ResolveEscalationPackage(profile, forcedPackageDefName) : null;
            if (!allowPackageSelection && !forcedPackageDefName.NullOrEmpty())
            {
                ABY_BossEscalationPackageDef forced = DefDatabase<ABY_BossEscalationPackageDef>.GetNamedSilentFail(forcedPackageDefName);
                if (forced != null && forced.AllowsBossProfile(profile.defName))
                {
                    package = forced;
                }
            }
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
