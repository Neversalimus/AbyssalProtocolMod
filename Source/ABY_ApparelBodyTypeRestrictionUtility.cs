using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ApparelBodyTypeRestrictionUtility
    {
        private const int MessageThrottleTicks = 240;

        private static readonly HashSet<string> ForcedRestrictedBodyArmorDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ABY_InfernalCombatFrame",
            "ABY_NullAcolyteVestment",
            "ABY_AbyssalGravplatePrototype",
            "ABY_SaintAegisCarapace",
            "ABY_GatebreakerCarapace",
            "ABY_CrownedCorePlate"
        };

        private static readonly HashSet<string> DefaultAllowedBodyTypeDefNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Male",
            "Female",
            "Thin"
        };

        private static readonly Dictionary<string, int> LastMessageTicksByKey = new Dictionary<string, int>();

        public static DefModExtension_ABY_ApparelBodyTypeRestriction GetRestriction(ThingDef apparelDef)
        {
            return apparelDef?.GetModExtension<DefModExtension_ABY_ApparelBodyTypeRestriction>();
        }

        public static bool HasRestriction(ThingDef apparelDef)
        {
            return GetRestriction(apparelDef) != null || IsForcedRestrictedBodyArmor(apparelDef);
        }

        public static bool IsForcedRestrictedBodyArmor(ThingDef apparelDef)
        {
            return !string.IsNullOrEmpty(apparelDef?.defName) && ForcedRestrictedBodyArmorDefNames.Contains(apparelDef.defName);
        }

        public static bool CanWear(Pawn pawn, Apparel apparel)
        {
            return CanWear(pawn, apparel?.def);
        }

        public static bool CanWear(Pawn pawn, ThingDef apparelDef)
        {
            return CanWear(pawn, apparelDef, out _, out _);
        }

        public static bool CanWear(Pawn pawn, ThingDef apparelDef, out DefModExtension_ABY_ApparelBodyTypeRestriction restriction, out string bodyTypeDefName)
        {
            restriction = GetRestriction(apparelDef);
            bodyTypeDefName = ResolveBodyTypeDefName(pawn);

            if (apparelDef == null || string.IsNullOrEmpty(bodyTypeDefName))
            {
                return true;
            }

            // Hard fallback for the six Abyssal torso armor defs. This makes the restriction work even if
            // a patch operation is skipped, a saved def cache is stale, or another mod strips modExtensions.
            if (IsForcedRestrictedBodyArmor(apparelDef) && !DefaultAllowedBodyTypeDefNames.Contains(bodyTypeDefName))
            {
                return false;
            }

            if (restriction == null)
            {
                return true;
            }

            if (restriction.allowedBodyTypes != null && restriction.allowedBodyTypes.Count > 0 && !ContainsBodyType(restriction.allowedBodyTypes, bodyTypeDefName))
            {
                return false;
            }

            if (restriction.disallowedBodyTypes != null && restriction.disallowedBodyTypes.Count > 0 && ContainsBodyType(restriction.disallowedBodyTypes, bodyTypeDefName))
            {
                return false;
            }

            return true;
        }

        public static string BuildRejectMessage(Pawn pawn, ThingDef apparelDef, DefModExtension_ABY_ApparelBodyTypeRestriction restriction)
        {
            string apparelLabel = apparelDef != null ? apparelDef.LabelCap.ToString() : "apparel";
            string bodyTypeLabel = ResolveBodyTypeLabel(pawn);
            string key = !string.IsNullOrEmpty(restriction?.rejectMessageKey) ? restriction.rejectMessageKey : "ABY_ApparelBodyTypeRestriction_Incompatible";
            return key.Translate(apparelLabel, bodyTypeLabel).ToString();
        }

        public static string BuildRemovedMessage(Pawn pawn, ThingDef apparelDef, DefModExtension_ABY_ApparelBodyTypeRestriction restriction)
        {
            string pawnLabel = pawn != null ? pawn.LabelShortCap : "Pawn";
            string apparelLabel = apparelDef != null ? apparelDef.LabelCap.ToString() : "apparel";
            string bodyTypeLabel = ResolveBodyTypeLabel(pawn);
            string key = !string.IsNullOrEmpty(restriction?.removedMessageKey) ? restriction.removedMessageKey : "ABY_ApparelBodyTypeRestriction_Removed";
            return key.Translate(pawnLabel, apparelLabel, bodyTypeLabel).ToString();
        }

        public static void TryShowRejectMessage(Pawn pawn, ThingDef apparelDef, DefModExtension_ABY_ApparelBodyTypeRestriction restriction)
        {
            if (restriction != null && !restriction.showRejectMessage)
            {
                return;
            }

            string message = BuildRejectMessage(pawn, apparelDef, restriction);
            if (!ShouldShowMessage("reject", pawn, apparelDef))
            {
                return;
            }

            Messages.Message(message, pawn, MessageTypeDefOf.RejectInput, false);
        }

        public static void TryShowRemovedMessage(Pawn pawn, ThingDef apparelDef, DefModExtension_ABY_ApparelBodyTypeRestriction restriction)
        {
            if (restriction != null && !restriction.showRemovalMessage)
            {
                return;
            }

            if (pawn == null || pawn.Faction != Faction.OfPlayer)
            {
                return;
            }

            string message = BuildRemovedMessage(pawn, apparelDef, restriction);
            if (!ShouldShowMessage("removed", pawn, apparelDef))
            {
                return;
            }

            Messages.Message(message, pawn, MessageTypeDefOf.CautionInput, false);
        }

        public static bool TryRejectIncompatibleWear(Pawn pawn, ThingDef apparelDef)
        {
            if (CanWear(pawn, apparelDef, out DefModExtension_ABY_ApparelBodyTypeRestriction restriction, out _))
            {
                return false;
            }

            TryShowRejectMessage(pawn, apparelDef, restriction);
            return true;
        }

        public static bool TryRejectIncompatibleWear(Pawn pawn, Apparel apparel)
        {
            return TryRejectIncompatibleWear(pawn, apparel?.def);
        }

        public static bool TryRemoveIncompatibleWornApparel(Pawn pawn, bool showMessage)
        {
            if (pawn?.apparel == null || pawn.story == null)
            {
                return false;
            }

            List<Apparel> worn = pawn.apparel.WornApparel;
            if (worn == null || worn.Count == 0)
            {
                return false;
            }

            bool changed = false;
            List<Apparel> snapshot = new List<Apparel>(worn);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Apparel apparel = snapshot[i];
                if (apparel == null)
                {
                    continue;
                }

                if (CanWear(pawn, apparel.def, out DefModExtension_ABY_ApparelBodyTypeRestriction restriction, out _))
                {
                    continue;
                }

                if (!TryMoveApparelOffPawn(pawn, apparel))
                {
                    continue;
                }

                changed = true;
                if (showMessage)
                {
                    TryShowRemovedMessage(pawn, apparel.def, restriction);
                }
            }

            return changed;
        }

        private static bool TryMoveApparelOffPawn(Pawn pawn, Apparel apparel)
        {
            if (pawn?.apparel == null || apparel == null)
            {
                return false;
            }

            if (!pawn.apparel.WornApparel.Contains(apparel))
            {
                return false;
            }

            if (!pawn.Spawned && pawn.inventory == null)
            {
                return false;
            }

            pawn.apparel.Remove(apparel);

            if (pawn.Spawned && pawn.Map != null)
            {
                if (GenDrop.TryDropSpawn(apparel, pawn.Position, pawn.Map, ThingPlaceMode.Near, out _))
                {
                    return true;
                }
            }

            if (pawn.inventory?.innerContainer != null && pawn.inventory.innerContainer.TryAdd(apparel))
            {
                return true;
            }

            return true;
        }

        private static string ResolveBodyTypeDefName(Pawn pawn)
        {
            return pawn?.story?.bodyType?.defName;
        }

        private static string ResolveBodyTypeLabel(Pawn pawn)
        {
            BodyTypeDef bodyType = pawn?.story?.bodyType;
            if (bodyType == null)
            {
                return "unknown";
            }

            string label = bodyType.label;
            return !string.IsNullOrEmpty(label) ? label.CapitalizeFirst() : bodyType.defName;
        }

        private static bool ContainsBodyType(List<string> bodyTypes, string bodyTypeDefName)
        {
            if (bodyTypes == null || string.IsNullOrEmpty(bodyTypeDefName))
            {
                return false;
            }

            for (int i = 0; i < bodyTypes.Count; i++)
            {
                string entry = bodyTypes[i];
                if (!string.IsNullOrEmpty(entry) && string.Equals(entry.Trim(), bodyTypeDefName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldShowMessage(string channel, Pawn pawn, ThingDef apparelDef)
        {
            int now = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            string pawnKey = pawn != null ? pawn.thingIDNumber.ToString() : "none";
            string apparelKey = apparelDef != null ? apparelDef.defName : "none";
            string key = channel + ":" + pawnKey + ":" + apparelKey;

            if (LastMessageTicksByKey.TryGetValue(key, out int lastTick) && now - lastTick < MessageThrottleTicks)
            {
                return false;
            }

            LastMessageTicksByKey[key] = now;
            return true;
        }
    }
}
