using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_ModularTurretUtility
    {
        public const string ForgeCategory = "TurretSystems";

        private static Dictionary<ThingDef, ABY_TurretModuleDef> moduleByThingDef;

        public static bool Enabled => AbyssalProtocolMod.Settings.enableModularTurrets;

        public static string TranslateOrFallback(string key, string fallback)
        {
            try
            {
                string translated = key.Translate();
                return translated == key ? fallback : translated;
            }
            catch
            {
                return fallback;
            }
        }

        public static string TranslateOrFallback(string key, string fallbackFormat, params object[] args)
        {
            try
            {
                string translated = key.Translate();
                string template = translated == key ? fallbackFormat : translated;
                return string.Format(template, args);
            }
            catch
            {
                try
                {
                    return string.Format(fallbackFormat, args);
                }
                catch
                {
                    return fallbackFormat;
                }
            }
        }

        public static bool IsModularTurretRecipe(RecipeDef recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            DefModExtension_AbyssalForgeUnlock extension = recipe.GetModExtension<DefModExtension_AbyssalForgeUnlock>();
            if (extension != null && string.Equals(extension.category, ForgeCategory, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            ThingDef product = AbyssalForgeProgressUtility.GetPrimaryProduct(recipe);
            if (product == null)
            {
                return false;
            }

            return GetModuleForThingDef(product) != null || product.GetCompProperties<CompProperties_AbyssalModularTurret>() != null;
        }

        public static ABY_TurretModuleDef GetModuleForThingDef(ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return null;
            }

            EnsureModuleCache();
            moduleByThingDef.TryGetValue(thingDef, out ABY_TurretModuleDef moduleDef);
            return moduleDef;
        }

        public static List<ABY_TurretModuleDef> GetModulesForSlot(ABY_TurretModuleSlot slot, string chassisTag)
        {
            return DefDatabase<ABY_TurretModuleDef>.AllDefsListForReading
                .Where(module => module != null && module.slot == slot && module.CompatibleWith(chassisTag))
                .OrderBy(module => module.tier)
                .ThenBy(module => module.label)
                .ToList();
        }

        public static ABY_TurretModuleDef FindAvailableModuleOnMap(Map map, ABY_TurretModuleSlot slot, string chassisTag)
        {
            if (map?.listerThings == null)
            {
                return null;
            }

            List<ABY_TurretModuleDef> candidates = GetModulesForSlot(slot, chassisTag);
            for (int i = 0; i < candidates.Count; i++)
            {
                ABY_TurretModuleDef module = candidates[i];
                if (GetUsableLooseModuleCount(map, module) > 0)
                {
                    return module;
                }
            }

            return null;
        }

        public static int GetUsableLooseModuleCount(Map map, ABY_TurretModuleDef moduleDef)
        {
            if (map?.listerThings == null || moduleDef?.thingDef == null)
            {
                return 0;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(moduleDef.thingDef);
            if (things == null || things.Count == 0)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (IsUsableLooseThing(thing))
                {
                    total += Mathf.Max(0, thing.stackCount);
                }
            }

            return total;
        }

        public static bool TryConsumeModuleItem(Map map, ABY_TurretModuleDef moduleDef, IntVec3 priorityCell)
        {
            if (map?.listerThings == null || moduleDef?.thingDef == null)
            {
                return false;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(moduleDef.thingDef)
                .Where(IsUsableLooseThing)
                .OrderBy(thing => thing.Position.DistanceToSquared(priorityCell))
                .ToList();

            if (things.Count == 0)
            {
                return false;
            }

            Thing item = things[0];
            if (item.stackCount <= 1)
            {
                item.Destroy(DestroyMode.Vanish);
                return true;
            }

            Thing split = item.SplitOff(1);
            split.Destroy(DestroyMode.Vanish);
            return true;
        }

        public static bool TryEjectModuleItem(Thing owner, ABY_TurretModuleDef moduleDef, out string reason)
        {
            reason = null;
            if (owner?.Map == null || !owner.Spawned)
            {
                reason = TranslateOrFallback("ABY_TurretRemove_NoMap", "Cannot eject the module because the chassis is not spawned on a map.");
                return false;
            }

            if (moduleDef?.thingDef == null)
            {
                reason = TranslateOrFallback("ABY_TurretRemove_InvalidModule", "Cannot eject an invalid module item.");
                return false;
            }

            Thing item = ThingMaker.MakeThing(moduleDef.thingDef);
            item.stackCount = 1;
            if (GenPlace.TryPlaceThing(item, owner.Position, owner.Map, ThingPlaceMode.Near))
            {
                return true;
            }

            if (!item.Destroyed)
            {
                item.Destroy(DestroyMode.Vanish);
            }

            reason = TranslateOrFallback("ABY_TurretRemove_NoDropCell", "Could not find a safe nearby cell for the ejected module. The module was kept installed.");
            return false;
        }

        public static void EjectModuleItem(Thing owner, ABY_TurretModuleDef moduleDef)
        {
            TryEjectModuleItem(owner, moduleDef, out _);
        }

        public static Color SlotColor(ABY_TurretModuleSlot slot)
        {
            switch (slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return new Color(1f, 0.42f, 0.22f, 1f);
                case ABY_TurretModuleSlot.Auxiliary:
                    return new Color(0.86f, 0.64f, 1f, 1f);
                default:
                    return new Color(1f, 0.74f, 0.38f, 1f);
            }
        }

        public static string FormatTicksAsSeconds(int ticks)
        {
            return (Mathf.Max(0, ticks) / 60f).ToString("0.0") + "s";
        }


        public static string GetModuleForgeCardSummary(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return TranslateOrFallback("ABY_ForgePatternSummary_TurretSystems", "Turret system");
            }

            switch (module.slot)
            {
                case ABY_TurretModuleSlot.MainWeapon:
                    return TranslateOrFallback("ABY_TurretForgeSummary_Main", "Main weapon slot · {0}", module.RoleLabel);
                case ABY_TurretModuleSlot.Auxiliary:
                    return TranslateOrFallback("ABY_TurretForgeSummary_Aux", "Auxiliary slot · {0}", module.RoleLabel);
                default:
                    return TranslateOrFallback("ABY_TurretForgeSummary_Passive", "Passive slot · {0}", module.RoleLabel);
            }
        }

        public static string GetModuleEffectSummary(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return string.Empty;
            }

            string effect = module.EffectSummary;
            return effect.NullOrEmpty() ? GetModuleForgeCardSummary(module) : effect;
        }

        public static string GetModuleStatSummary(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return string.Empty;
            }

            if (module.slot == ABY_TurretModuleSlot.MainWeapon)
            {
                return TranslateOrFallback(
                    "ABY_TurretModuleFireStats",
                    "Range {0} · cooldown {1} · burst {2}",
                    module.range.ToString("0.0"),
                    FormatTicksAsSeconds(module.cooldownTicks),
                    Mathf.Max(1, module.burstShotCount));
            }

            if (module.slot == ABY_TurretModuleSlot.Auxiliary)
            {
                int auxCooldown = module.auxiliaryCooldownTicks > 0 ? module.auxiliaryCooldownTicks : module.cooldownTicks;
                return TranslateOrFallback(
                    "ABY_TurretModuleAuxStats",
                    "Range {0} · auxiliary cooldown {1} · burst {2}",
                    module.range.ToString("0.0"),
                    FormatTicksAsSeconds(auxCooldown),
                    Mathf.Max(1, module.burstShotCount));
            }

            return TranslateOrFallback(
                "ABY_TurretModulePassiveStats",
                "Range {0} · cooldown {1} · power +{2} W",
                FormatSignedDecimal(module.rangeOffset),
                FormatCooldownMultiplierEffect(module.cooldownMultiplier),
                module.extraPowerDraw.ToString("0"));
        }

        public static string GetModuleDetailedTooltip(ABY_TurretModuleDef module)
        {
            if (module == null)
            {
                return string.Empty;
            }

            List<string> lines = new List<string>
            {
                TranslateOrFallback("ABY_TurretModuleTooltipSlot", "Slot: {0}", module.SlotLabel),
                TranslateOrFallback("ABY_TurretModuleTooltipRole", "Role: {0}", module.RoleLabel),
                TranslateOrFallback("ABY_TurretModuleTooltipEffect", "Effect: {0}", GetModuleEffectSummary(module)),
                TranslateOrFallback("ABY_TurretModuleTooltipStats", "Stats: {0}", GetModuleStatSummary(module))
            };

            if (module.projectileDef != null)
            {
                lines.Add(TranslateOrFallback("ABY_TurretModuleProjectile", "Projectile: {0}", module.projectileDef.label));
            }

            if (module.extraPowerDraw != 0f)
            {
                lines.Add(TranslateOrFallback("ABY_TurretModulePowerDelta", "Extra module power draw: +{0} W", module.extraPowerDraw.ToString("0")));
            }

            return string.Join("\n", lines.Where(line => !line.NullOrEmpty()).ToArray());
        }

        public static string GetChassisForgeCardSummary(ThingDef chassisDef)
        {
            CompProperties_AbyssalModularTurret comp = chassisDef?.GetCompProperties<CompProperties_AbyssalModularTurret>();
            if (comp == null)
            {
                return TranslateOrFallback("ABY_TurretForgeSummary_ChassisGeneric", "Turret chassis");
            }

            return TranslateOrFallback(
                "ABY_TurretForgeSummary_ChassisSlots",
                "Chassis · {0} main / {1} aux / {2} passive slots",
                Mathf.Max(0, comp.mainWeaponSlots),
                Mathf.Max(0, comp.auxiliarySlots),
                Mathf.Max(0, comp.passiveSlots));
        }

        public static string GetChassisDetailedTooltip(ThingDef chassisDef)
        {
            CompProperties_AbyssalModularTurret comp = chassisDef?.GetCompProperties<CompProperties_AbyssalModularTurret>();
            if (comp == null)
            {
                return string.Empty;
            }

            return string.Join("\n", new[]
            {
                TranslateOrFallback("ABY_TurretChassisTooltipRole", "Role: empty modular turret body. It cannot fire until a main weapon core is installed."),
                TranslateOrFallback("ABY_TurretChassisTooltipSlots", "Slots: {0} main weapon, {1} auxiliary, {2} passive.", Mathf.Max(0, comp.mainWeaponSlots), Mathf.Max(0, comp.auxiliarySlots), Mathf.Max(0, comp.passiveSlots)),
                TranslateOrFallback("ABY_TurretChassisTooltipRuntime", "Runtime: installed modules are saved inside the building and survive save/load and feature kill-switch toggles.")
            });
        }

        public static string FormatSignedDecimal(float value)
        {
            if (Mathf.Abs(value) < 0.01f)
            {
                return "0";
            }

            return value > 0f ? "+" + value.ToString("0.0") : value.ToString("0.0");
        }

        public static string FormatCooldownMultiplierEffect(float multiplier)
        {
            if (multiplier <= 0f)
            {
                multiplier = 1f;
            }

            float delta = multiplier - 1f;
            if (Mathf.Abs(delta) < 0.005f)
            {
                return TranslateOrFallback("ABY_TurretCooldown_NoChange", "no change");
            }

            int percent = Mathf.RoundToInt(Mathf.Abs(delta) * 100f);
            if (delta < 0f)
            {
                return TranslateOrFallback("ABY_TurretCooldown_Faster", "-{0}% cooldown", percent);
            }

            return TranslateOrFallback("ABY_TurretCooldown_Slower", "+{0}% cooldown", percent);
        }

        private static void EnsureModuleCache()
        {
            if (moduleByThingDef != null)
            {
                return;
            }

            moduleByThingDef = new Dictionary<ThingDef, ABY_TurretModuleDef>();
            foreach (ABY_TurretModuleDef module in DefDatabase<ABY_TurretModuleDef>.AllDefsListForReading)
            {
                if (module?.thingDef == null)
                {
                    continue;
                }

                moduleByThingDef[module.thingDef] = module;
            }
        }

        private static bool IsUsableLooseThing(Thing thing)
        {
            return thing != null
                && !thing.Destroyed
                && thing.Spawned
                && thing.stackCount > 0
                && thing.Faction == null
                && !thing.IsForbidden(Faction.OfPlayer);
        }
    }
}
