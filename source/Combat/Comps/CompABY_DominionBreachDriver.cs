using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_DominionBreachDriver : CompProperties
    {
        public int requiredHits = 3;
        public int sequenceWindowTicks = 420;
        public float sentenceDamage = 52f;
        public float sentenceArmorPenetration = 1.65f;
        public float pressureVisualScale = 0.48f;
        public float sentenceVisualScale = 1.18f;
        public float sentenceFlashScale = 0.82f;
        public string sentenceSoundDefName = "ABY_RuptureVerdict";

        public CompProperties_ABY_DominionBreachDriver()
        {
            compClass = typeof(CompABY_DominionBreachDriver);
        }
    }

    public class CompABY_DominionBreachDriver : ThingComp
    {
        private int activeTargetThingId = -1;
        private int activeMapId = -1;
        private int consecutiveHitCount;
        private int lastPressureHitTick = -1;

        public CompProperties_ABY_DominionBreachDriver Props => (CompProperties_ABY_DominionBreachDriver)props;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref activeTargetThingId, "activeTargetThingId", -1);
            Scribe_Values.Look(ref activeMapId, "activeMapId", -1);
            Scribe_Values.Look(ref consecutiveHitCount, "consecutiveHitCount", 0);
            Scribe_Values.Look(ref lastPressureHitTick, "lastPressureHitTick", -1);
        }

        public bool TryRegisterPressureHit(Pawn wielder, Pawn target, out int registeredHitCount)
        {
            registeredHitCount = 0;

            if (wielder == null
                || target == null
                || wielder.Dead
                || target.Dead
                || wielder.Destroyed
                || target.Destroyed
                || target.MapHeld == null)
            {
                ResetSequence();
                return false;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int targetMapId = target.MapHeld.uniqueID;
            int requiredHits = Mathf.Max(1, Props.requiredHits);
            int sequenceWindowTicks = Mathf.Max(1, Props.sequenceWindowTicks);

            bool sequenceExpired = lastPressureHitTick < 0
                || currentTick < lastPressureHitTick
                || currentTick - lastPressureHitTick > sequenceWindowTicks;
            bool targetChanged = activeTargetThingId != target.thingIDNumber || activeMapId != targetMapId;

            if (sequenceExpired || targetChanged)
            {
                ResetSequence();
            }

            activeTargetThingId = target.thingIDNumber;
            activeMapId = targetMapId;
            lastPressureHitTick = currentTick;
            consecutiveHitCount = Mathf.Clamp(consecutiveHitCount + 1, 1, requiredHits);
            registeredHitCount = consecutiveHitCount;

            if (consecutiveHitCount < requiredHits)
            {
                return false;
            }

            ResetSequence();
            return true;
        }

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

            StatCategoryDef category = DefDatabase<StatCategoryDef>.GetNamedSilentFail("Weapon_Melee")
                ?? DefDatabase<StatCategoryDef>.GetNamedSilentFail("Weapon")
                ?? DefDatabase<StatCategoryDef>.GetNamedSilentFail("BasicsImportant")
                ?? DefDatabase<StatCategoryDef>.GetNamedSilentFail("Basics");

            if (category == null)
            {
                yield break;
            }

            int order = 7900;
            yield return BuildEntry(
                category,
                "ABY_DominionBreachDriver_Profile",
                "Structural verdict",
                TranslateOrFallback("ABY_DominionBreachDriver_ProfileValue", "Single-target pressure sequence"),
                "ABY_DominionBreachDriver_ProfileDesc",
                "Repeated confirmed blows force a target-specific pressure sequence instead of adding general crowd damage.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_DominionBreachDriver_Sequence",
                "Sentence sequence",
                string.Format(
                    TranslateOrFallback("ABY_DominionBreachDriver_SequenceValue", "{0} confirmed hits within {1} seconds"),
                    Mathf.Max(1, Props.requiredHits),
                    FormatSeconds(Mathf.Max(1, Props.sequenceWindowTicks))),
                "ABY_DominionBreachDriver_SequenceDesc",
                "Switching targets or losing contact for too long resets the sequence.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_DominionBreachDriver_Sentence",
                "Structural sentence",
                string.Format(
                    TranslateOrFallback("ABY_DominionBreachDriver_SentenceValue", "{0} Blunt, {1}% AP"),
                    FormatNumber(Mathf.Max(0f, Props.sentenceDamage)),
                    FormatPercent(Mathf.Max(0f, Props.sentenceArmorPenetration))),
                "ABY_DominionBreachDriver_SentenceDesc",
                "The third confirmed hit adds a separate high-penetration impact against the same living pawn.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_DominionBreachDriver_Limits",
                "Target lock limits",
                TranslateOrFallback("ABY_DominionBreachDriver_LimitsValue", "Living pawns only; one active target"),
                "ABY_DominionBreachDriver_LimitsDesc",
                "The driver does not build verdict pressure on buildings, corpses, or multiple targets at once.",
                order);
        }

        private void ResetSequence()
        {
            activeTargetThingId = -1;
            activeMapId = -1;
            consecutiveHitCount = 0;
            lastPressureHitTick = -1;
        }

        private static StatDrawEntry BuildEntry(
            StatCategoryDef category,
            string labelKey,
            string fallbackLabel,
            string value,
            string descriptionKey,
            string fallbackDescription,
            int order)
        {
            return new StatDrawEntry(
                category,
                TranslateOrFallback(labelKey, fallbackLabel),
                value ?? string.Empty,
                TranslateOrFallback(descriptionKey, fallbackDescription),
                order);
        }

        private static string TranslateOrFallback(string key, string fallback)
        {
            try
            {
                string translated = key.Translate();
                return translated.NullOrEmpty() || translated == key ? fallback : translated;
            }
            catch
            {
                return fallback;
            }
        }

        private static string FormatNumber(float value)
        {
            return Mathf.Abs(value - Mathf.Round(value)) < 0.01f
                ? Mathf.Round(value).ToString("0")
                : value.ToString("0.#");
        }

        private static string FormatPercent(float fraction)
        {
            return Mathf.Round(fraction * 100f).ToString("0");
        }

        private static string FormatSeconds(int ticks)
        {
            return (ticks / 60f).ToString("0.#");
        }
    }
}
