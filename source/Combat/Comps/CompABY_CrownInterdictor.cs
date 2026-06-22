using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_CrownInterdictor : CompProperties
    {
        public int markWindowTicks = 180;
        public int normalLockDurationTicks = 150;
        public int bossLockDurationTicks = 90;
        public int authorityScarDurationTicks = 300;
        public int normalFlinchTicks = 15;
        public float markVisualScale = 0.26f;
        public float lockVisualScale = 0.64f;
        public float lockFlashScale = 0.48f;
        public string normalLockHediffDefName = "ABY_CrownInterdicted";
        public string bossLockHediffDefName = "ABY_CrownInterdicted_Boss";
        public string authorityScarHediffDefName = "ABY_CrownAuthorityScar";
        public string lockSoundDefName = "ABY_RuptureVerdict";

        public CompProperties_ABY_CrownInterdictor()
        {
            compClass = typeof(CompABY_CrownInterdictor);
        }
    }

    public enum CrownInterdictorHitProgress
    {
        None,
        WritMarked,
        ReadyToLock
    }

    /// <summary>
    /// Weapon-owned two-hit target lock state for Crown Interdictor. The first confirmed hit writes a
    /// short target-specific mark; the second confirmed hit on the same live hostile pawn requests an
    /// Edict Lock. State remains serialized on the equipment item and never uses a global pawn cache.
    /// </summary>
    public class CompABY_CrownInterdictor : ThingComp
    {
        private int markedTargetThingId = -1;
        private int markedAtTick = -1;
        private int lastProcessedTargetThingId = -1;
        private int lastProcessedTick = -1;

        public CompProperties_ABY_CrownInterdictor Props => (CompProperties_ABY_CrownInterdictor)props;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref markedTargetThingId, "markedTargetThingId", -1);
            Scribe_Values.Look(ref markedAtTick, "markedAtTick", -1);
            Scribe_Values.Look(ref lastProcessedTargetThingId, "lastProcessedTargetThingId", -1);
            Scribe_Values.Look(ref lastProcessedTick, "lastProcessedTick", -1);
        }

        public CrownInterdictorHitProgress RegisterConfirmedHit(Pawn wielder, Pawn target)
        {
            if (!IsValidTarget(wielder, target))
            {
                ResetWrit();
                return CrownInterdictorHitProgress.None;
            }

            int currentTick = CurrentTick;
            if (target.thingIDNumber == lastProcessedTargetThingId && currentTick == lastProcessedTick)
            {
                return CrownInterdictorHitProgress.None;
            }

            lastProcessedTargetThingId = target.thingIDNumber;
            lastProcessedTick = currentTick;

            if (!HasActiveWritFor(target, currentTick))
            {
                markedTargetThingId = target.thingIDNumber;
                markedAtTick = currentTick;
                return CrownInterdictorHitProgress.WritMarked;
            }

            ResetWrit();
            return CrownInterdictorHitProgress.ReadyToLock;
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

            int order = 7940;
            yield return BuildEntry(
                category,
                "ABY_CrownInterdictor_Profile",
                "Edict lock",
                TranslateOrFallback("ABY_CrownInterdictor_ProfileValue", "Priority-target restriction"),
                "ABY_CrownInterdictor_ProfileDesc",
                "Two precise hits on the same living hostile target impose a short movement and combat restriction.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_CrownInterdictor_Sequence",
                "Interdiction sequence",
                string.Format(
                    TranslateOrFallback("ABY_CrownInterdictor_SequenceValue", "{0} confirmed hits within {1} seconds"),
                    2,
                    FormatSeconds(Mathf.Max(1, Props.markWindowTicks))),
                "ABY_CrownInterdictor_SequenceDesc",
                "Changing targets or allowing the writ window to expire starts the sequence again.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_CrownInterdictor_NormalLock",
                "Edict lock",
                string.Format(
                    TranslateOrFallback("ABY_CrownInterdictor_NormalLockValue", "{0} seconds on normal targets"),
                    FormatSeconds(Mathf.Max(1, Props.normalLockDurationTicks))),
                "ABY_CrownInterdictor_NormalLockDesc",
                "Normal living hostile targets lose movement, aim and dodge efficiency after the second confirmed hit.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_CrownInterdictor_BossLock",
                "Protected target lock",
                string.Format(
                    TranslateOrFallback("ABY_CrownInterdictor_BossLockValue", "Reduced {0}-second effect on bosses and minibosses"),
                    FormatSeconds(Mathf.Max(1, Props.bossLockDurationTicks))),
                "ABY_CrownInterdictor_BossLockDesc",
                "Bosses and minibosses receive only the reduced restriction; the weapon does not hard-stun their combat logic.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_CrownInterdictor_Scar",
                "Authority scar",
                string.Format(
                    TranslateOrFallback("ABY_CrownInterdictor_ScarValue", "Prevents immediate re-locking for {0} seconds"),
                    FormatSeconds(Mathf.Max(1, Props.authorityScarDurationTicks))),
                "ABY_CrownInterdictor_ScarDesc",
                "A target marked by an Edict Lock cannot be immediately locked again by another Interdictor.",
                order);
        }

        private bool HasActiveWritFor(Pawn target, int currentTick)
        {
            if (target == null
                || markedTargetThingId != target.thingIDNumber
                || markedAtTick < 0
                || currentTick < markedAtTick)
            {
                return false;
            }

            int markWindowTicks = Mathf.Max(1, Props.markWindowTicks);
            return currentTick - markedAtTick <= markWindowTicks;
        }

        private void ResetWrit()
        {
            markedTargetThingId = -1;
            markedAtTick = -1;
        }

        private static bool IsValidTarget(Pawn wielder, Pawn target)
        {
            return wielder != null
                && !wielder.Dead
                && !wielder.Destroyed
                && wielder.MapHeld != null
                && target != null
                && target != wielder
                && !target.Dead
                && !target.Downed
                && !target.Destroyed
                && target.health != null
                && target.MapHeld == wielder.MapHeld
                && ABY_FactionHostilityUtility.SafeHostileTo(wielder, target);
        }

        private static int CurrentTick => Find.TickManager != null ? Find.TickManager.TicksGame : 0;

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

        private static string FormatSeconds(int ticks)
        {
            return (ticks / 60f).ToString("0.#");
        }
    }
}
