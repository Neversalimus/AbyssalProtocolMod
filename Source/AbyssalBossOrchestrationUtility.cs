using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalBossOrchestrationUtility
    {
        public static ABY_BossDifficultyProfileDef ResolveProfileByRitualId(string ritualId)
        {
            if (ritualId.NullOrEmpty())
            {
                return null;
            }

            int stage = AbyssalDifficultyUtility.GetProgressionStage();
            ABY_BossDifficultyProfileDef fallback = null;
            foreach (ABY_BossDifficultyProfileDef def in DefDatabase<ABY_BossDifficultyProfileDef>.AllDefsListForReading)
            {
                if (def == null || !def.MatchesRitualId(ritualId))
                {
                    continue;
                }

                if (stage >= def.minProgressionStage)
                {
                    return def;
                }

                fallback ??= def;
            }

            return fallback;
        }

        public static ABY_BossDifficultyProfileDef ResolveProfileByBossKindDefName(string bossKindDefName)
        {
            if (bossKindDefName.NullOrEmpty())
            {
                return null;
            }

            int stage = AbyssalDifficultyUtility.GetProgressionStage();
            ABY_BossDifficultyProfileDef fallback = null;
            foreach (ABY_BossDifficultyProfileDef def in DefDatabase<ABY_BossDifficultyProfileDef>.AllDefsListForReading)
            {
                if (def == null || !def.MatchesBossKindDefName(bossKindDefName))
                {
                    continue;
                }

                if (stage >= def.minProgressionStage)
                {
                    return def;
                }

                fallback ??= def;
            }

            return fallback;
        }

        public static bool HasBossEscortProfile(string ritualId)
        {
            ABY_BossDifficultyProfileDef profile = ResolveProfileByRitualId(ritualId);
            return profile != null && !profile.escortPoolId.NullOrEmpty();
        }

        public static AbyssalEncounterDirectorUtility.EncounterPlan BuildEscortPlan(string ritualId, Map map, float fallbackBudget, int? seed = null)
        {
            ABY_BossDifficultyProfileDef profile = ResolveProfileByRitualId(ritualId);
            if (profile == null || profile.escortPoolId.NullOrEmpty())
            {
                return null;
            }

            float baseBudget = fallbackBudget > 0f ? fallbackBudget : profile.fallbackEscortBudget;
            if (baseBudget <= 0.01f)
            {
                return null;
            }

            baseBudget *= profile.escortBudgetMultiplier <= 0f ? 1f : profile.escortBudgetMultiplier;
            int baseTier = profile.escortBaseContentTier > 0 ? profile.escortBaseContentTier : GetFallbackEscortTier(ritualId);
            return AbyssalEncounterDirectorUtility.BuildPlan(
                profile.escortPoolId,
                baseBudget,
                baseTier,
                map,
                seed,
                null,
                null,
                profile.defName);
        }

        public static bool TrySpawnEscortPack(Map map, Faction faction, string ritualId, IntVec3 requestedArrivalCell, float fallbackBudget, string packLabel, out IntVec3 arrivalCell, out string failReason)
        {
            arrivalCell = IntVec3.Invalid;
            failReason = null;
            if (map == null || faction == null)
            {
                return false;
            }

            AbyssalEncounterDirectorUtility.EncounterPlan plan = BuildEscortPlan(ritualId, map, fallbackBudget);
            if (plan == null || plan.TotalUnits <= 0)
            {
                return false;
            }

            ABY_EncounterTelemetryUtility.RecordPlan(plan);
            bool spawned = AbyssalHostileSummonUtility.TrySpawnHostilePack(
                map,
                plan.ToHostilePackEntries(),
                faction,
                requestedArrivalCell,
                packLabel,
                null,
                null,
                false,
                out arrivalCell,
                out failReason);
            return spawned;
        }

        public static bool TrySpawnEscortPackNearBoss(
            Map map,
            Faction faction,
            string ritualId,
            Pawn bossPawn,
            float fallbackBudget,
            string packLabel,
            out string failReason)
        {
            failReason = null;
            if (map == null || faction == null || bossPawn == null || bossPawn.Dead || !bossPawn.Spawned)
            {
                return false;
            }

            AbyssalEncounterDirectorUtility.EncounterPlan plan = BuildEscortPlan(ritualId, map, fallbackBudget);
            if (plan == null || plan.TotalUnits <= 0)
            {
                return false;
            }

            ABY_EncounterTelemetryUtility.RecordPlan(plan);
            return AbyssalHostileSummonUtility.TrySpawnHostilePackAroundAnchor(
                map,
                plan.ToHostilePackEntries(),
                faction,
                bossPawn.PositionHeld,
                packLabel,
                out failReason);
        }

        public static int GetCompanionPortalBonus(string ritualId)
        {
            ABY_BossDifficultyProfileDef profile = ResolveProfileByRitualId(ritualId);
            if (profile == null)
            {
                return 0;
            }

            int currentOrder = AbyssalDifficultyUtility.GetCurrentProfileOrder();
            int bonus = 0;
            if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_Dominion"))
            {
                bonus += profile.bonusCompanionPortalsAtDominion;
            }

            if (currentOrder >= AbyssalDifficultyUtility.GetProfileOrder("ABY_Difficulty_FinalGate"))
            {
                bonus += profile.bonusCompanionPortalsAtFinalGate;
            }

            return bonus;
        }

        private static int GetFallbackEscortTier(string ritualId)
        {
            switch ((ritualId ?? string.Empty).ToLowerInvariant())
            {
                case "warden_of_ash":
                    return 2;
                case "archon_beast":
                    return 4;
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
