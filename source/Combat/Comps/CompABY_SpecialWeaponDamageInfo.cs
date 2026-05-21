using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_SpecialWeaponDamageInfo : CompProperties
    {
        public CompProperties_ABY_SpecialWeaponDamageInfo()
        {
            compClass = typeof(CompABY_SpecialWeaponDamageInfo);
        }
    }

    public class CompABY_SpecialWeaponDamageInfo : ThingComp
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

            List<StatDrawEntry> customEntries = ABY_SpecialWeaponDamageInfoUtility.BuildStatEntries(parent?.def);
            for (int i = 0; i < customEntries.Count; i++)
            {
                if (customEntries[i] != null)
                {
                    yield return customEntries[i];
                }
            }
        }
    }
}
