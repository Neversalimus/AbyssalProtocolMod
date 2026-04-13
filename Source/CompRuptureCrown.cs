using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class RuptureCrownUtility
    {
        public const string CrownDefName = "ABY_CrownOfRupture";
        public const string AbilityDefName = "ABY_RuptureSentence";
        public const string BearerHediffDefName = "ABY_RuptureCrownBearer";
        public const string MarkHediffDefName = "ABY_RuptureSentenceMark";

        private static AbilityDef cachedAbilityDef;
        private static HediffDef cachedBearerHediffDef;

        public static AbilityDef AbilityDef
        {
            get
            {
                if (cachedAbilityDef == null)
                {
                    cachedAbilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(AbilityDefName);
                }

                return cachedAbilityDef;
            }
        }

        public static HediffDef BearerHediffDef
        {
            get
            {
                if (cachedBearerHediffDef == null)
                {
                    cachedBearerHediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(BearerHediffDefName);
                }

                return cachedBearerHediffDef;
            }
        }

        public static CompRuptureCrown GetWornCrownComp(Pawn pawn)
        {
            if (pawn?.apparel == null)
            {
                return null;
            }

            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                Apparel apparel = pawn.apparel.WornApparel[i];
                if (apparel?.def?.defName != CrownDefName)
                {
                    continue;
                }

                return apparel.TryGetComp<CompRuptureCrown>();
            }

            return null;
        }

        public static bool HasCrown(Pawn pawn)
        {
            return GetWornCrownComp(pawn) != null;
        }

        public static Ability GetGrantedAbility(Pawn pawn)
        {
            if (pawn?.abilities == null)
            {
                return null;
            }

            AbilityDef abilityDef = AbilityDef;
            if (abilityDef == null)
            {
                return null;
            }

            return pawn.abilities.GetAbility(abilityDef, false);
        }
    }

    public class CompProperties_RuptureCrown : CompProperties
    {
        public int rechargeTicks = GenDate.TicksPerDay;

        public CompProperties_RuptureCrown()
        {
            compClass = typeof(CompRuptureCrown);
        }
    }

    public class CompRuptureCrown : ThingComp
    {
        private int lastUseTick = -999999;

        public CompProperties_RuptureCrown Props => (CompProperties_RuptureCrown)props;

        public bool IsReady => TicksUntilRecharged <= 0;

        public int TicksUntilRecharged
        {
            get
            {
                if (Find.TickManager == null)
                {
                    return 0;
                }

                return Mathf.Max(0, Props.rechargeTicks - (Find.TickManager.TicksGame - lastUseTick));
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastUseTick, "lastUseTick", -999999);
        }

        public void NotifyUsed()
        {
            if (Find.TickManager == null)
            {
                lastUseTick = 0;
                return;
            }

            lastUseTick = Find.TickManager.TicksGame;
        }

        public override string CompInspectStringExtra()
        {
            if (IsReady)
            {
                return "Rupture charge: ready";
            }

            return "Rupture charge recharging: " + TicksUntilRecharged.ToStringTicksToPeriod();
        }

        public override System.Collections.Generic.IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            foreach (StatDrawEntry entry in base.SpecialDisplayStats())
            {
                yield return entry;
            }

            yield return new StatDrawEntry(
                StatCategoryDefOf.BasicsNonPawnImportant,
                "Rupture recharge",
                GenDate.ToStringTicksToDays(Props.rechargeTicks),
                "The crown condenses one new verdict charge over a full in-game day.",
                1000);
        }
    }
}
