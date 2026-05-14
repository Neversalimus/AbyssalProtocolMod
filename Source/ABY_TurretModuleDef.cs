using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public sealed class ABY_TurretModuleDef : Def
    {
        public ABY_TurretModuleSlot slot = ABY_TurretModuleSlot.Passive;
        public ThingDef thingDef;
        public List<string> compatibleChassisTags;
        public string role;
        public string effectSummary;
        public int tier = 1;

        public ThingDef projectileDef;
        public SoundDef soundCast;
        public float range = 24f;
        public int cooldownTicks = 180;
        public int burstShotCount = 1;
        public int ticksBetweenBurstShots = 8;
        public int auxiliaryCooldownTicks = 360;

        public float rangeOffset;
        public float cooldownMultiplier = 1f;
        public int cooldownOffsetTicks;
        public float missRadiusOffset;
        public float extraPowerDraw;

        public bool IsWeaponLike => projectileDef != null && (slot == ABY_TurretModuleSlot.MainWeapon || slot == ABY_TurretModuleSlot.Auxiliary);

        public bool CompatibleWith(string chassisTag)
        {
            if (compatibleChassisTags == null || compatibleChassisTags.Count == 0)
            {
                return true;
            }

            return !chassisTag.NullOrEmpty() && compatibleChassisTags.Contains(chassisTag);
        }

        public string SlotLabel
        {
            get
            {
                switch (slot)
                {
                    case ABY_TurretModuleSlot.MainWeapon:
                        return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlot_Main", "Main weapon");
                    case ABY_TurretModuleSlot.Auxiliary:
                        return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlot_Auxiliary", "Auxiliary");
                    default:
                        return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretSlot_Passive", "Passive");
                }
            }
        }

        public string RoleLabel => role.NullOrEmpty() ? ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretRole_Generic", "general") : role;

        public string EffectSummary
        {
            get
            {
                if (!effectSummary.NullOrEmpty())
                {
                    return effectSummary;
                }

                if (!description.NullOrEmpty())
                {
                    return description;
                }

                return RoleLabel;
            }
        }
    }
}
