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
