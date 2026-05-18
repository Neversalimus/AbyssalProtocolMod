using System;
using Verse;

namespace AbyssalProtocol
{
    public enum ABY_AbyssalMonsterRole
    {
        Unknown,
        Melee,
        Pouncer,
        Ranged,
        Siege,
        Support,
        Escort
    }

    public struct ABY_AbyssalMonsterCombatProfile
    {
        public ABY_AbyssalMonsterRole Role;
        public bool HasRangedStance;
        public float MinRange;
        public float MaxRange;
        public float PreferredMinRange;
        public int RepositionSearchRadius;
        public float PanicMeleeRange;
        public bool HoldPositionWhenReady;
        public bool PreferFiringCell;

        public bool IsValid => Role != ABY_AbyssalMonsterRole.Unknown;
    }

    public static class ABY_AbyssalMonsterRoleUtility
    {
        public static bool ShouldUseMonsterBrain(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Map == null || !pawn.Spawned)
            {
                return false;
            }

            if (!ABY_AntiTameUtility.IsAbyssalPawn(pawn))
            {
                return false;
            }

            return !ABY_ReactorSaintAIUtility.IsReactorSaintPawn(pawn);
        }

        public static ABY_AbyssalMonsterCombatProfile ResolveProfile(Pawn pawn)
        {
            ABY_AbyssalMonsterCombatProfile profile = new ABY_AbyssalMonsterCombatProfile
            {
                Role = ABY_AbyssalMonsterRole.Unknown,
                HasRangedStance = false,
                MinRange = 0f,
                MaxRange = 2.05f,
                PreferredMinRange = 0f,
                RepositionSearchRadius = 8,
                PanicMeleeRange = 1.95f,
                HoldPositionWhenReady = false,
                PreferFiringCell = false
            };

            if (pawn == null)
            {
                return profile;
            }

            CompABY_SiegeIdolSiegeShooter siege = pawn.TryGetComp<CompABY_SiegeIdolSiegeShooter>();
            if (siege != null)
            {
                CompProperties_ABY_SiegeIdolSiegeShooter props = siege.props as CompProperties_ABY_SiegeIdolSiegeShooter;
                profile.Role = ABY_AbyssalMonsterRole.Siege;
                profile.HasRangedStance = true;
                profile.MinRange = Math.Max(0f, props?.targetMinRange ?? 8.6f);
                profile.MaxRange = Math.Max(6f, props?.range ?? 31.9f);
                profile.PreferredMinRange = Math.Max(profile.MinRange, props?.preferredMinRange ?? 10.8f);
                profile.RepositionSearchRadius = Math.Max(8, props?.retreatSearchRadius ?? 12);
                profile.PanicMeleeRange = Math.Max(1.95f, props?.panicMeleeRange ?? 4.2f);
                profile.HoldPositionWhenReady = true;
                profile.PreferFiringCell = true;
                return profile;
            }

            CompABY_RiftSapperShooter sapper = pawn.TryGetComp<CompABY_RiftSapperShooter>();
            if (sapper != null)
            {
                CompProperties_ABY_RiftSapperShooter props = sapper.props as CompProperties_ABY_RiftSapperShooter;
                profile.Role = ABY_AbyssalMonsterRole.Siege;
                profile.HasRangedStance = true;
                profile.MinRange = Math.Max(0f, props?.targetMinRange ?? 4.2f);
                profile.MaxRange = Math.Max(6f, props?.range ?? 19.9f);
                profile.PreferredMinRange = Math.Max(profile.MinRange, props?.preferredMinRange ?? 6.8f);
                profile.RepositionSearchRadius = Math.Max(6, props?.retreatSearchRadius ?? 8);
                profile.PanicMeleeRange = Math.Max(1.95f, props?.panicMeleeRange ?? 2.2f);
                profile.HoldPositionWhenReady = props?.holdPositionWhenTargeting ?? false;
                profile.PreferFiringCell = true;
                return profile;
            }

            CompHexgunThrallShooter hexgun = pawn.TryGetComp<CompHexgunThrallShooter>();
            if (hexgun != null)
            {
                CompProperties_HexgunThrallShooter props = hexgun.props as CompProperties_HexgunThrallShooter;
                profile.Role = IsSupportLike(pawn) ? ABY_AbyssalMonsterRole.Support : ABY_AbyssalMonsterRole.Ranged;
                profile.HasRangedStance = true;
                profile.MinRange = Math.Max(0f, props?.targetMinRange ?? 0f);
                profile.MaxRange = Math.Max(8f, props?.range ?? 27.9f);
                profile.PreferredMinRange = Math.Max(profile.MinRange, props?.preferredMinRange ?? DefaultPreferredMinRange(pawn));
                profile.RepositionSearchRadius = Math.Max(6, props?.retreatSearchRadius ?? 9);
                profile.PanicMeleeRange = props != null && props.panicMeleeRange > 0f ? props.panicMeleeRange : 1.95f;
                profile.HoldPositionWhenReady = props?.holdPositionWhenTargeting ?? true;
                profile.PreferFiringCell = true;
                return profile;
            }

            if (pawn.TryGetComp<CompABY_NullPriestAura>() != null || pawn.TryGetComp<CompABY_NullPriestBreach>() != null)
            {
                profile.Role = ABY_AbyssalMonsterRole.Support;
                profile.HasRangedStance = true;
                profile.MinRange = 3.8f;
                profile.MaxRange = 26f;
                profile.PreferredMinRange = 7f;
                profile.RepositionSearchRadius = 9;
                profile.PanicMeleeRange = 2.2f;
                profile.HoldPositionWhenReady = true;
                profile.PreferFiringCell = true;
                return profile;
            }

            if (pawn.TryGetComp<CompABY_GateWardenEscort>() != null || pawn.TryGetComp<CompABY_GateWardenBrace>() != null)
            {
                profile.Role = ABY_AbyssalMonsterRole.Escort;
                profile.MaxRange = 2.15f;
                profile.PanicMeleeRange = 2.15f;
                return profile;
            }

            if (pawn.TryGetComp<CompEmberPounce>() != null || pawn.TryGetComp<CompChainSnag>() != null)
            {
                profile.Role = ABY_AbyssalMonsterRole.Pouncer;
                profile.MaxRange = 2.15f;
                profile.PanicMeleeRange = 2.15f;
                return profile;
            }

            if (IsKnownAbyssalMelee(pawn))
            {
                profile.Role = ABY_AbyssalMonsterRole.Melee;
                profile.MaxRange = 2.15f;
                profile.PanicMeleeRange = 2.15f;
                return profile;
            }

            profile.Role = ABY_AbyssalMonsterRole.Melee;
            profile.MaxRange = 2.05f;
            profile.PanicMeleeRange = 2.05f;
            return profile;
        }

        public static bool HasCustomRangedController(Pawn pawn)
        {
            ABY_AbyssalMonsterCombatProfile profile = ResolveProfile(pawn);
            return profile.HasRangedStance;
        }

        private static bool IsSupportLike(Pawn pawn)
        {
            string defName = pawn?.def?.defName ?? string.Empty;
            return defName.IndexOf("NullPriest", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("HaloHusk", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsKnownAbyssalMelee(Pawn pawn)
        {
            string defName = pawn?.def?.defName ?? string.Empty;
            return defName.IndexOf("Imp", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Hound", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Zealot", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Brute", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Harvester", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Warden", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float DefaultPreferredMinRange(Pawn pawn)
        {
            string defName = pawn?.def?.defName ?? string.Empty;
            if (defName.IndexOf("Sniper", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 10.5f;
            }

            if (defName.IndexOf("NullPriest", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 7f;
            }

            return 4.5f;
        }
    }
}
