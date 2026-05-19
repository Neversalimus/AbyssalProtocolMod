using System;
using System.Collections;
using System.Collections.Generic;
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
    /// intended Abyssal hostility without calling RelationWith when the relation entry is missing, and can also
    /// repair the missing relation rows so vanilla melee/damage code remains safe.
    /// </summary>
    public static class ABY_FactionHostilityUtility
    {
        private const string AbyssalFactionDefName = "ABY_AbyssalHost";
        private const string AbyssalPrefix = "ABY_";

        private static readonly FieldInfo RelationsField = typeof(Faction).GetField("relations", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo RelationOtherField = typeof(FactionRelation).GetField("other", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo RelationKindField = typeof(FactionRelation).GetField("kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo RelationGoodwillField = typeof(FactionRelation).GetField("baseGoodwill", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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
                EnsureHostileRelationIfAbyssalPair(source, target);
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
                EnsureHostileRelationIfAbyssalPair(source.Faction, target.Faction);
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
                EnsureHostileRelationIfAbyssalPair(source.Faction, target);
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
                EnsureHostileRelationIfAbyssalPair(source.Faction, target.Faction);
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
                EnsureHostileRelationIfAbyssalPair(source, target.Faction);
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
                EnsureHostileRelationIfAbyssalPair(pawn.Faction, Faction.OfPlayer);
                return true;
            }

            return SafeHostileTo(pawn.Faction, Faction.OfPlayer);
        }

        public static void RepairAllAbyssalFactionRelations()
        {
            try
            {
                if (Find.FactionManager?.AllFactionsListForReading == null || Faction.OfPlayer == null)
                {
                    return;
                }

                List<Faction> factions = Find.FactionManager.AllFactionsListForReading;
                for (int i = 0; i < factions.Count; i++)
                {
                    Faction faction = factions[i];
                    if (IsAbyssalFaction(faction))
                    {
                        EnsureHostileRelationPair(faction, Faction.OfPlayer);
                    }
                }
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("aby-faction-relation-repair-all", "[Abyssal Protocol] Abyssal faction relation repair failed: " + ex.Message, 5000);
            }
        }

        public static void EnsureHostileRelationIfAbyssalPair(Faction source, Faction target)
        {
            if (source == null || target == null || source == target)
            {
                return;
            }

            bool sourceAbyssal = IsAbyssalFaction(source);
            bool targetAbyssal = IsAbyssalFaction(target);
            if (sourceAbyssal == targetAbyssal)
            {
                return;
            }

            EnsureHostileRelationPair(source, target);
        }

        public static void EnsureHostileRelationPair(Faction a, Faction b)
        {
            if (a == null || b == null || a == b)
            {
                return;
            }

            EnsureOneWayHostileRelation(a, b);
            EnsureOneWayHostileRelation(b, a);
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

        private static void EnsureOneWayHostileRelation(Faction source, Faction target)
        {
            if (source == null || target == null || source == target || RelationsField == null || RelationOtherField == null)
            {
                return;
            }

            object relationsObject = RelationsField.GetValue(source);
            if (!(relationsObject is IList relations))
            {
                return;
            }

            for (int i = 0; i < relations.Count; i++)
            {
                object relation = relations[i];
                if (relation == null)
                {
                    continue;
                }

                object other = RelationOtherField.GetValue(relation);
                if (ReferenceEquals(other, target))
                {
                    SetRelationHostile(relation);
                    return;
                }
            }

            FactionRelation newRelation = new FactionRelation(target, FactionRelationKind.Hostile);
            SetRelationHostile(newRelation);
            relations.Add(newRelation);
        }

        private static void SetRelationHostile(object relation)
        {
            if (relation == null)
            {
                return;
            }

            RelationKindField?.SetValue(relation, FactionRelationKind.Hostile);
            RelationGoodwillField?.SetValue(relation, -100);
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
