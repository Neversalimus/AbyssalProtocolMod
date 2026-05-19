using System;
using System.Collections;
using System.Reflection;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Relation-safe hostility helpers for Abyssal runtime code.
    ///
    /// RimWorld logs a red error when Faction.HostileTo/RelationWith is called for factions that do not
    /// have a relation row with PlayerColony. Hidden/generated encounter factions such as ABY_AbyssalHost
    /// can hit that state in existing saves or mid-encounter generated factions. These helpers preserve the
    /// intended Abyssal hostility without calling RelationWith when the relation entry is missing.
    /// </summary>
    public static class ABY_FactionHostilityUtility
    {
        private const string AbyssalFactionDefName = "ABY_AbyssalHost";
        private const string AbyssalPrefix = "ABY_";

        private static readonly FieldInfo RelationsField = typeof(Faction).GetField("relations", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo RelationOtherField = typeof(FactionRelation).GetField("other", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        public static bool IsAbyssalFaction(Faction faction)
        {
            return string.Equals(faction?.def?.defName, AbyssalFactionDefName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAbyssalPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (IsAbyssalFaction(pawn.Faction))
            {
                return true;
            }

            string kindName = pawn.kindDef?.defName ?? string.Empty;
            string raceName = pawn.def?.defName ?? string.Empty;
            return kindName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase)
                || raceName.StartsWith(AbyssalPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool SafeHostileToPlayer(Faction faction)
        {
            return SafeHostileTo(faction, Faction.OfPlayer);
        }

        public static bool SafeHostileTo(Faction source, Faction target)
        {
            if (source == null || target == null || source == target)
            {
                return false;
            }

            bool sourceAbyssal = IsAbyssalFaction(source);
            bool targetAbyssal = IsAbyssalFaction(target);
            if (sourceAbyssal || targetAbyssal)
            {
                return sourceAbyssal != targetAbyssal;
            }

            if (!HasRecordedRelation(source, target))
            {
                return FallbackHostility(source, target);
            }

            return source.HostileTo(target);
        }

        public static bool SafeHostileTo(Pawn source, Pawn target)
        {
            if (source == null || target == null || source == target)
            {
                return false;
            }

            bool sourceAbyssal = IsAbyssalPawn(source);
            bool targetAbyssal = IsAbyssalPawn(target);
            if (sourceAbyssal || targetAbyssal)
            {
                return sourceAbyssal != targetAbyssal;
            }

            return SafeHostileTo(source.Faction, target.Faction);
        }


        public static bool SafeHostileTo(Pawn source, Faction target)
        {
            if (source == null)
            {
                return false;
            }

            bool sourceAbyssal = IsAbyssalPawn(source);
            bool targetAbyssal = IsAbyssalFaction(target);
            if (sourceAbyssal || targetAbyssal)
            {
                return sourceAbyssal != targetAbyssal;
            }

            return SafeHostileTo(source.Faction, target);
        }

        public static bool SafeHostileTo(Thing source, Thing target)
        {
            if (source == null || target == null || source == target)
            {
                return false;
            }

            if (source is Pawn sourcePawn && target is Pawn targetPawn)
            {
                return SafeHostileTo(sourcePawn, targetPawn);
            }

            bool sourceAbyssal = source is Pawn sourcePawnOnly && IsAbyssalPawn(sourcePawnOnly);
            bool targetAbyssal = target is Pawn targetPawnOnly && IsAbyssalPawn(targetPawnOnly);
            if (sourceAbyssal || targetAbyssal)
            {
                return sourceAbyssal != targetAbyssal;
            }

            return SafeHostileTo(source.Faction, target.Faction);
        }

        public static bool SafeHostileTo(Faction source, Pawn target)
        {
            if (target == null)
            {
                return false;
            }

            bool sourceAbyssal = IsAbyssalFaction(source);
            bool targetAbyssal = IsAbyssalPawn(target);
            if (sourceAbyssal || targetAbyssal)
            {
                return sourceAbyssal != targetAbyssal;
            }

            return SafeHostileTo(source, target.Faction);
        }

        public static bool SafeHostileToPlayer(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (IsAbyssalPawn(pawn))
            {
                return true;
            }

            return SafeHostileTo(pawn.Faction, Faction.OfPlayer);
        }

        private static bool HasRecordedRelation(Faction source, Faction target)
        {
            if (source == null || target == null)
            {
                return false;
            }

            if (source == target)
            {
                return true;
            }

            // If reflection fails because RimWorld internals changed, fall back to vanilla behavior rather than
            // silently breaking ordinary faction hostility. The known 1.6 field name is "relations".
            if (RelationsField == null || RelationOtherField == null)
            {
                return true;
            }

            object relationsObject = RelationsField.GetValue(source);
            if (!(relationsObject is IEnumerable relations))
            {
                return false;
            }

            foreach (object relation in relations)
            {
                if (relation == null)
                {
                    continue;
                }

                object other = RelationOtherField.GetValue(relation);
                if (ReferenceEquals(other, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool FallbackHostility(Faction source, Faction target)
        {
            if (source == null || target == null || source == target)
            {
                return false;
            }

            bool playerInvolved = source == Faction.OfPlayer || target == Faction.OfPlayer;
            bool sourcePermanentEnemy = source.def?.permanentEnemy == true;
            bool targetPermanentEnemy = target.def?.permanentEnemy == true;

            if (playerInvolved && (sourcePermanentEnemy || targetPermanentEnemy))
            {
                return true;
            }

            // Missing relation rows outside explicit permanent-enemy/player cases are safer as non-hostile than
            // spamming red RelationWith errors every targeting tick.
            return false;
        }
    }
}
