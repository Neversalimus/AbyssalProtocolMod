using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompABY_ApparelAegisInfo : ThingComp
    {
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            IEnumerable<StatDrawEntry> baseEntries = null;
            try
            {
                baseEntries = base.SpecialDisplayStats();
            }
            catch
            {
            }

            if (baseEntries != null)
            {
                foreach (StatDrawEntry entry in baseEntries)
                {
                    if (entry != null)
                    {
                        yield return entry;
                    }
                }
            }

            List<StatDrawEntry> customEntries = BuildCustomEntriesSafe();
            for (int i = 0; i < customEntries.Count; i++)
            {
                if (customEntries[i] != null)
                {
                    yield return customEntries[i];
                }
            }
        }



        private List<StatDrawEntry> BuildCustomEntriesSafe()
        {
            List<StatDrawEntry> result = new List<StatDrawEntry>();

            try
            {
                Apparel apparel = parent as Apparel;
                DefModExtension_ABY_ApparelAegis ext = ABY_ApparelAegisUtility.GetAegisExtension(apparel);
                if (ext == null)
                {
                    return result;
                }

                StatCategoryDef category = ABY_ApparelAegisUtility.ResolveApparelCategory();
                if (category == null)
                {
                    return result;
                }

                int order = 7800;
                result.Add(Entry(category, ref order, "ABY_ApparelAegis_InfoHeader", "Armor aegis", ABY_ApparelAegisUtility.AegisLabel(ext), "ABY_ApparelAegis_InfoHeaderDesc", "Built-in passive shield profile for this armor."));
                result.Add(Entry(category, ref order, "ABY_ApparelAegis_InfoCapacity", "Aegis capacity", ((int)ext.MaxShieldPointsSafe).ToString(), "ABY_ApparelAegis_InfoCapacityDesc", "Maximum shield points available before the armor aegis collapses."));
                result.Add(Entry(category, ref order, "ABY_ApparelAegis_InfoRechargeDelay", "Recharge delay", ABY_ApparelAegisUtility.SecondsFromTicks(ext.RechargeDelayTicksSafe), "ABY_ApparelAegis_InfoRechargeDelayDesc", "Time after a shield hit before the armor aegis can begin restoring charge."));
                result.Add(Entry(category, ref order, "ABY_ApparelAegis_InfoRechargeRate", "Recharge rate", ext.RechargePerIntervalSafe.ToString("0.#") + " / " + ABY_ApparelAegisUtility.SecondsFromTicks(ext.RechargeIntervalTicksSafe), "ABY_ApparelAegis_InfoRechargeRateDesc", "Shield points restored while the armor aegis is recharging."));
                result.Add(Entry(category, ref order, "ABY_ApparelAegis_InfoBlocks", "Blocks", ResolveBlocks(ext), "ABY_ApparelAegis_InfoBlocksDesc", "Damage classes intercepted before normal armor resolution."));
                result.Add(Entry(category, ref order, "ABY_ApparelAegis_InfoOutgoing", "Outgoing fire", ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_InfoOutgoingValue", "not blocked"), "ABY_ApparelAegis_InfoOutgoingDesc", "This custom armor aegis intercepts incoming damage only and does not prevent the wearer from shooting outward."));
                result.Add(Entry(category, ref order, "ABY_ApparelAegis_InfoExternalShield", "External shields", ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_InfoExternalShieldValue", "suppresses aegis"), "ABY_ApparelAegis_InfoExternalShieldDesc", "When a shield belt or external shield apparel is worn, this armor aegis is disabled to prevent shield stacking."));
            }
            catch (System.Exception ex)
            {
                ABY_UISafetyUtility.LogUIException("Apparel aegis info card", ex);
            }

            return result;
        }

        private static StatDrawEntry Entry(StatCategoryDef category, ref int order, string labelKey, string fallbackLabel, string value, string descKey, string fallbackDesc)
        {
            return new StatDrawEntry(
                category,
                ABY_ApparelAegisUtility.TranslateOrFallback(labelKey, fallbackLabel),
                value ?? string.Empty,
                ABY_ApparelAegisUtility.TranslateOrFallback(descKey, fallbackDesc),
                order++);
        }

        private static string ResolveBlocks(DefModExtension_ABY_ApparelAegis ext)
        {
            List<string> parts = new List<string>();
            if (ext.absorbRanged)
            {
                parts.Add(ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_BlockRanged", "ranged"));
            }

            if (ext.absorbExplosive)
            {
                parts.Add(ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_BlockExplosive", "explosions"));
            }

            if (ext.absorbMelee)
            {
                parts.Add(ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_BlockMelee", "melee"));
            }

            if (ext.empDrainsShield)
            {
                parts.Add(ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_BlockEMP", "EMP drain"));
            }

            return parts.Count == 0 ? ABY_ApparelAegisUtility.TranslateOrFallback("ABY_ApparelAegis_BlockNone", "none") : string.Join(", ", parts);
        }
    }
}
