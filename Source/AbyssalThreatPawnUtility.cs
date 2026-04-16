using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace AbyssalProtocol
{
    public static class AbyssalThreatPawnUtility
    {
        private const string HexgunThrallDefName = "ABY_HexgunThrall";
        private const string HexgunWeaponDefName = "ABY_Hexgun";
        private const string ChainZealotDefName = "ABY_ChainZealot";

        public static void PrepareThreatPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            EnsureLoadout(pawn);
            EnsureCombatSkills(pawn);
        }

        public static Lord GetCurrentLord(Pawn pawn)
        {
            if (pawn?.Map?.lordManager?.lords == null)
            {
                return null;
            }

            List<Lord> lords = pawn.Map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                Lord lord = lords[i];
                if (lord?.ownedPawns != null && lord.ownedPawns.Contains(pawn))
                {
                    return lord;
                }
            }

            return null;
        }

        private static void EnsureLoadout(Pawn pawn)
        {
            if (!IsHexgunThrall(pawn))
            {
                return;
            }

            if (pawn.equipment == null || pawn.equipment.Primary != null)
            {
                return;
            }

            ThingDef weaponDef = DefDatabase<ThingDef>.GetNamedSilentFail(HexgunWeaponDefName);
            if (weaponDef == null)
            {
                return;
            }

            Thing weapon = ThingMaker.MakeThing(weaponDef);
            if (weapon is ThingWithComps thingWithComps)
            {
                pawn.equipment.AddEquipment(thingWithComps);
            }
        }

        private static void EnsureCombatSkills(Pawn pawn)
        {
            if (pawn.skills == null)
            {
                return;
            }

            if (IsHexgunThrall(pawn))
            {
                SkillRecord shooting = pawn.skills.GetSkill(SkillDefOf.Shooting);
                if (shooting != null && shooting.Level < 10)
                {
                    shooting.Level = 10;
                }
            }
            else if (IsChainZealot(pawn))
            {
                SkillRecord melee = pawn.skills.GetSkill(SkillDefOf.Melee);
                if (melee != null && melee.Level < 11)
                {
                    melee.Level = 11;
                }
            }
        }

        private static bool IsHexgunThrall(Pawn pawn)
        {
            return HasDefName(pawn, HexgunThrallDefName);
        }

        private static bool IsChainZealot(Pawn pawn)
        {
            return HasDefName(pawn, ChainZealotDefName);
        }

        private static bool HasDefName(Pawn pawn, string defName)
        {
            if (pawn == null || defName.NullOrEmpty())
            {
                return false;
            }

            return pawn.def?.defName == defName || pawn.kindDef?.defName == defName;
        }
    }
}
