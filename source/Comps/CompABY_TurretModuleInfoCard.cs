using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_TurretModuleInfoCard : CompProperties
    {
        public CompProperties_ABY_TurretModuleInfoCard()
        {
            compClass = typeof(CompABY_TurretModuleInfoCard);
        }
    }

    public class CompABY_TurretModuleInfoCard : ThingComp
    {
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            List<StatDrawEntry> baseEntries = SafeCollectBaseEntries();
            for (int i = 0; i < baseEntries.Count; i++)
            {
                if (baseEntries[i] != null)
                {
                    yield return baseEntries[i];
                }
            }

            List<StatDrawEntry> customEntries = BuildCustomDisplayEntries();
            for (int i = 0; i < customEntries.Count; i++)
            {
                if (customEntries[i] != null)
                {
                    yield return customEntries[i];
                }
            }
        }

        private List<StatDrawEntry> SafeCollectBaseEntries()
        {
            List<StatDrawEntry> result = new List<StatDrawEntry>();
            try
            {
                IEnumerable<StatDrawEntry> enumerable = base.SpecialDisplayStats();
                if (enumerable == null)
                {
                    return result;
                }

                foreach (StatDrawEntry entry in enumerable)
                {
                    if (entry != null)
                    {
                        result.Add(entry);
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        private List<StatDrawEntry> BuildCustomDisplayEntries()
        {
            List<StatDrawEntry> result = new List<StatDrawEntry>();
            try
            {
                ABY_TurretModuleDef module = ABY_ModularTurretUtility.GetModuleForThingDef(parent?.def);
                if (module == null)
                {
                    return result;
                }

                StatCategoryDef category = ResolveStatCategory();
                if (category == null)
                {
                    return result;
                }

                int order = 7600;
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_Profile", "Turret module profile",
                    ABY_ModularTurretUtility.GetModuleForgeCardSummary(module),
                    "ABY_TurretInfo_ProfileDesc", "The occupied slot and tactical role of this modular turret component.");

                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_CompatibleChassis", "Compatible chassis",
                    FormatCompatibleChassis(module),
                    "ABY_TurretInfo_CompatibleChassisDesc", "Chassis tags that can accept this module.");

                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_Effect", "Effect",
                    ABY_ModularTurretUtility.GetModuleEffectSummary(module),
                    "ABY_TurretInfo_EffectDesc", "What this module does after installation.");

                AppendWeaponEntries(result, category, module, ref order);
                AppendPassiveEntries(result, category, module, ref order);

                if (Math.Abs(module.extraPowerDraw) > 0.01f)
                {
                    AddEntry(result, category, ref order,
                        "ABY_TurretInfo_ExtraPowerDraw", "Extra power draw",
                        ABY_ModularTurretUtility.FormatPowerDelta(module.extraPowerDraw),
                        "ABY_TurretInfo_ExtraPowerDrawDesc", "Additional power consumed by the chassis while this module is installed and modular turrets are enabled.");
                }
            }
            catch (Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("Turret module info card", ex);
            }

            return result;
        }

        private static void AppendWeaponEntries(List<StatDrawEntry> result, StatCategoryDef category, ABY_TurretModuleDef module, ref int order)
        {
            if (module == null || module.slot == ABY_TurretModuleSlot.Passive)
            {
                return;
            }

            AddEntry(result, category, ref order,
                "ABY_TurretInfo_Range", "Turret range",
                module.range.ToString("0.0"),
                "ABY_TurretInfo_RangeDesc", "Maximum firing range provided by this weapon module.");

            int cooldown = module.slot == ABY_TurretModuleSlot.Auxiliary && module.auxiliaryCooldownTicks > 0
                ? module.auxiliaryCooldownTicks
                : module.cooldownTicks;
            string cooldownLabelKey = module.slot == ABY_TurretModuleSlot.Auxiliary ? "ABY_TurretInfo_AuxCooldown" : "ABY_TurretInfo_MainCooldown";
            string cooldownFallback = module.slot == ABY_TurretModuleSlot.Auxiliary ? "Auxiliary cooldown" : "Main cooldown";
            AddEntry(result, category, ref order,
                cooldownLabelKey, cooldownFallback,
                ABY_ModularTurretUtility.FormatTicksAsSeconds(cooldown),
                "ABY_TurretInfo_CooldownDesc", "Time before this module can fire again.");

            AddEntry(result, category, ref order,
                "ABY_TurretInfo_Burst", "Burst",
                Math.Max(1, module.burstShotCount).ToString(),
                "ABY_TurretInfo_BurstDesc", "Number of separate strikes released in one attack.");

            if (module.burstShotCount > 1)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_BurstSpacing", "Burst spacing",
                    ABY_ModularTurretUtility.FormatTicksAsSeconds(module.ticksBetweenBurstShots),
                    "ABY_TurretInfo_BurstSpacingDesc", "Delay between shots inside a burst.");
            }

        }

        private static void AppendPassiveEntries(List<StatDrawEntry> result, StatCategoryDef category, ABY_TurretModuleDef module, ref int order)
        {
            if (module == null || module.slot != ABY_TurretModuleSlot.Passive)
            {
                return;
            }

            if (Math.Abs(module.rangeOffset) > 0.01f)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_RangeOffset", "Main range offset",
                    ABY_ModularTurretUtility.FormatSignedDecimal(module.rangeOffset),
                    "ABY_TurretInfo_RangeOffsetDesc", "Added to the installed main weapon range.");
            }

            if (module.cooldownMultiplier > 0f && Math.Abs(module.cooldownMultiplier - 1f) > 0.001f)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_CooldownMultiplier", "Main cooldown modifier",
                    ABY_ModularTurretUtility.FormatCooldownMultiplierEffect(module.cooldownMultiplier),
                    "ABY_TurretInfo_CooldownMultiplierDesc", "Multiplies the installed main weapon cooldown. Negative values are faster.");
            }

            if (module.cooldownOffsetTicks != 0)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_CooldownOffset", "Main cooldown offset",
                    FormatSignedTicks(module.cooldownOffsetTicks),
                    "ABY_TurretInfo_CooldownOffsetDesc", "Flat cooldown shift added after multiplier effects.");
            }

            if (Math.Abs(module.minRangeOffset) > 0.01f)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_MinRangeOffset", "Minimum range offset",
                    ABY_ModularTurretUtility.FormatSignedDecimal(module.minRangeOffset),
                    "ABY_TurretInfo_MinRangeOffsetDesc", "Added to the installed main and auxiliary weapon minimum range. Negative values let close-range-locked weapons fire nearer to the chassis.");
            }

            if (module.idleCooldownRecoveryPerTick > 0.001f)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_CooldownRecovery", "Cooldown recovery",
                    module.idleCooldownRecoveryPerTick.ToString("0.0") + " ticks/tick",
                    "ABY_TurretInfo_CooldownRecoveryDesc", "Additional cooldown recovery applied while the main weapon is cooling down.");
            }

            if (module.incomingDamageMultiplier > 0f && Math.Abs(module.incomingDamageMultiplier - 1f) > 0.001f)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_DamageMultiplier", "Incoming damage",
                    Math.Round(module.incomingDamageMultiplier * 100f).ToString("0") + "%",
                    "ABY_TurretInfo_DamageMultiplierDesc", "Multiplier applied to incoming damage before it reaches the modular turret chassis.");
            }

            if (module.turretShieldMax > 0.01f)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_ShieldCapacity", "Aegis capacity",
                    module.turretShieldMax.ToString("0"),
                    "ABY_TurretInfo_ShieldCapacityDesc", "Damage absorbed by the turret's passive aegis field before hits reach the chassis.");
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_ShieldRecharge", "Aegis recharge",
                    (module.turretShieldRechargePerTick * 60f).ToString("0.0") + "/s after " + ABY_ModularTurretUtility.FormatTicksAsSeconds(module.turretShieldRechargeDelayTicks),
                    "ABY_TurretInfo_ShieldRechargeDesc", "How quickly the passive aegis recovers after its recharge delay ends.");
            }

            if (module.targetPriorityCombatPowerScale > 0.001f || module.targetPriorityBossBonus > 0.001f || module.targetPriorityConstructBonus > 0.001f || module.targetPriorityMechanoidBonus > 0.001f || module.targetPriorityShieldedBonus > 0.001f || module.preferClusteredTargets || module.preferLineTargets)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_TargetingLogic", "Targeting logic",
                    ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInfo_TargetingLogicValue", "enhanced"),
                    "ABY_TurretInfo_TargetingLogicDesc", "Adds passive scoring hints during the turret's existing throttled target scan. This does not create a per-tick global scan.");
            }

            if (Math.Abs(module.missRadiusOffset) > 0.01f)
            {
                AddEntry(result, category, ref order,
                    "ABY_TurretInfo_MissRadiusOffset", "Miss radius offset",
                    ABY_ModularTurretUtility.FormatSignedDecimal(module.missRadiusOffset),
                    "ABY_TurretInfo_MissRadiusOffsetDesc", "Prototype targeting offset reserved for future accuracy logic.");
            }
        }

        private static void AddEntry(List<StatDrawEntry> result, StatCategoryDef category, ref int order, string labelKey, string fallbackLabel, string value, string descKey, string fallbackDesc)
        {
            if (result == null || category == null || value.NullOrEmpty())
            {
                return;
            }

            result.Add(new StatDrawEntry(
                category,
                ABY_ModularTurretUtility.TranslateOrFallback(labelKey, fallbackLabel),
                value,
                ABY_ModularTurretUtility.TranslateOrFallback(descKey, fallbackDesc),
                order++));
        }

        private static string FormatCompatibleChassis(ABY_TurretModuleDef module)
        {
            if (module?.compatibleChassisTags == null || module.compatibleChassisTags.Count == 0)
            {
                return ABY_ModularTurretUtility.TranslateOrFallback("ABY_TurretInfo_AnyChassis", "any modular chassis");
            }

            return string.Join(", ", module.compatibleChassisTags.Where(tag => !tag.NullOrEmpty()).ToArray());
        }

        private static string FormatSignedTicks(int ticks)
        {
            string seconds = ABY_ModularTurretUtility.FormatTicksAsSeconds(Math.Abs(ticks));
            return ticks > 0 ? "+" + seconds : "-" + seconds;
        }

        private static StatCategoryDef ResolveStatCategory()
        {
            return DefDatabase<StatCategoryDef>.GetNamedSilentFail("Basics")
                   ?? DefDatabase<StatCategoryDef>.AllDefsListForReading.FirstOrDefault(def => def != null);
        }
    }
}
