using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_CrownScissionArray : CompProperties
    {
        public int maxEchoCharges = 3;
        public int chargeLifetimeTicks = 240;
        public float echoDamage = 13f;
        public float echoArmorPenetration = 0.65f;
        public float echoRadius = 2.2f;
        public float chargeVisualScale = 0.34f;
        public float echoVisualScale = 0.58f;
        public float echoFlashScale = 0.46f;
        public string echoSoundDefName = "ABY_RuptureVerdict";

        public CompProperties_ABY_CrownScissionArray()
        {
            compClass = typeof(CompABY_CrownScissionArray);
        }
    }

    /// <summary>
    /// Weapon-owned Echo Severance state. Direct hostile kills store short-lived echo charges;
    /// the next confirmed melee hit can spend one charge on one nearby hostile pawn.
    /// </summary>
    public class CompABY_CrownScissionArray : ThingComp
    {
        private int echoCharges;
        private int lastChargeTick = -1;
        private int lastRecordedKillThingId = -1;
        private int lastRecordedKillTick = -1;

        public CompProperties_ABY_CrownScissionArray Props => (CompProperties_ABY_CrownScissionArray)props;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref echoCharges, "echoCharges", 0);
            Scribe_Values.Look(ref lastChargeTick, "lastChargeTick", -1);
            Scribe_Values.Look(ref lastRecordedKillThingId, "lastRecordedKillThingId", -1);
            Scribe_Values.Look(ref lastRecordedKillTick, "lastRecordedKillTick", -1);
        }

        public bool TryStoreDirectKill(Pawn wielder, Pawn target)
        {
            if (!IsValidWielder(wielder)
                || target == null
                || target.Destroyed
                || !target.Dead
                || !ABY_FactionHostilityUtility.SafeHostileTo(wielder, target))
            {
                return false;
            }

            int currentTick = CurrentTick;
            if (target.thingIDNumber == lastRecordedKillThingId && currentTick == lastRecordedKillTick)
            {
                return false;
            }

            PruneExpiredCharges(currentTick);

            lastRecordedKillThingId = target.thingIDNumber;
            lastRecordedKillTick = currentTick;
            echoCharges = Mathf.Clamp(echoCharges + 1, 1, Mathf.Max(1, Props.maxEchoCharges));
            lastChargeTick = currentTick;
            return true;
        }

        public bool TryConsumeEcho(Pawn wielder, Pawn primaryTarget, out Pawn echoTarget)
        {
            echoTarget = null;

            if (!IsValidWielder(wielder))
            {
                return false;
            }

            int currentTick = CurrentTick;
            PruneExpiredCharges(currentTick);
            if (echoCharges <= 0)
            {
                return false;
            }

            Map map = primaryTarget?.MapHeld ?? wielder.MapHeld;
            if (map == null || wielder.MapHeld != map)
            {
                return false;
            }

            IntVec3 anchorCell = primaryTarget != null && primaryTarget.MapHeld == map
                ? primaryTarget.PositionHeld
                : wielder.PositionHeld;
            if (!anchorCell.InBounds(map))
            {
                anchorCell = wielder.PositionHeld;
            }

            Pawn candidate = FindNearbyHostilePawn(wielder, primaryTarget, map, anchorCell);
            if (candidate == null)
            {
                return false;
            }

            echoCharges = Mathf.Max(0, echoCharges - 1);
            if (echoCharges <= 0)
            {
                lastChargeTick = -1;
            }

            echoTarget = candidate;
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

            int order = 7920;
            yield return BuildEntry(
                category,
                "ABY_CrownScissionArray_Profile",
                "Echo severance",
                TranslateOrFallback("ABY_CrownScissionArray_ProfileValue", "Rapid pack-reaper sequence"),
                "ABY_CrownScissionArray_ProfileDesc",
                "Direct hostile kills store short-lived cutting echoes for the next confirmed melee hit.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_CrownScissionArray_Charges",
                "Stored echoes",
                string.Format(
                    TranslateOrFallback("ABY_CrownScissionArray_ChargesValue", "Up to {0} charges"),
                    Mathf.Max(1, Props.maxEchoCharges)),
                "ABY_CrownScissionArray_ChargesDesc",
                "A direct hostile kill stores one echo. Echo damage cannot create another echo.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_CrownScissionArray_Decay",
                "Echo decay",
                string.Format(
                    TranslateOrFallback("ABY_CrownScissionArray_DecayValue", "Expires after {0} seconds without another kill"),
                    FormatSeconds(Mathf.Max(1, Props.chargeLifetimeTicks))),
                "ABY_CrownScissionArray_DecayDesc",
                "All stored echoes expire together when the array stops confirming direct hostile kills.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_CrownScissionArray_Echo",
                "Echo severance",
                string.Format(
                    TranslateOrFallback("ABY_CrownScissionArray_EchoValue", "{0} Cut, {1}% AP to one nearby hostile"),
                    FormatNumber(Mathf.Max(0f, Props.echoDamage)),
                    FormatPercent(Mathf.Max(0f, Props.echoArmorPenetration))),
                "ABY_CrownScissionArray_EchoDesc",
                "The next confirmed melee hit spends one stored echo on one other living hostile pawn within the listed local range.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_CrownScissionArray_Range",
                "Echo range",
                string.Format(
                    TranslateOrFallback("ABY_CrownScissionArray_RangeValue", "{0} tiles around the struck target"),
                    FormatNumber(Mathf.Max(0.5f, Props.echoRadius))),
                "ABY_CrownScissionArray_RangeDesc",
                "The array checks only the local radial cells around the struck target; it does not scan the map.",
                order++);

            yield return BuildEntry(
                category,
                "ABY_CrownScissionArray_Limits",
                "Echo limits",
                TranslateOrFallback("ABY_CrownScissionArray_LimitsValue", "Hostile pawns only; one echo per hit; no chaining"),
                "ABY_CrownScissionArray_LimitsDesc",
                "Buildings, corpses and the primary target are never valid echo targets. Echo damage cannot store or trigger another echo.",
                order);
        }

        private Pawn FindNearbyHostilePawn(Pawn wielder, Pawn primaryTarget, Map map, IntVec3 anchorCell)
        {
            float radius = Mathf.Max(0.5f, Props.echoRadius);
            int cellCount = GenRadial.NumCellsInRadius(radius);
            Pawn best = null;
            int bestDistanceSquared = int.MaxValue;

            for (int cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                IntVec3 cell = anchorCell + GenRadial.RadialPattern[cellIndex];
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = map.thingGrid.ThingsListAtFast(cell);
                if (things == null)
                {
                    continue;
                }

                for (int thingIndex = 0; thingIndex < things.Count; thingIndex++)
                {
                    Pawn candidate = things[thingIndex] as Pawn;
                    if (!IsValidEchoTarget(wielder, primaryTarget, candidate, map))
                    {
                        continue;
                    }

                    int distanceSquared = (candidate.PositionHeld - anchorCell).LengthHorizontalSquared;
                    if (best == null
                        || distanceSquared < bestDistanceSquared
                        || (distanceSquared == bestDistanceSquared && candidate.thingIDNumber < best.thingIDNumber))
                    {
                        best = candidate;
                        bestDistanceSquared = distanceSquared;
                    }
                }
            }

            return best;
        }

        private static bool IsValidEchoTarget(Pawn wielder, Pawn primaryTarget, Pawn candidate, Map expectedMap)
        {
            return candidate != null
                && candidate != wielder
                && candidate != primaryTarget
                && !candidate.Dead
                && !candidate.Downed
                && !candidate.Destroyed
                && candidate.health != null
                && candidate.MapHeld == expectedMap
                && ABY_FactionHostilityUtility.SafeHostileTo(wielder, candidate);
        }

        private void PruneExpiredCharges(int currentTick)
        {
            if (echoCharges <= 0)
            {
                echoCharges = 0;
                lastChargeTick = -1;
                return;
            }

            int lifetimeTicks = Mathf.Max(1, Props.chargeLifetimeTicks);
            if (lastChargeTick < 0
                || currentTick < lastChargeTick
                || currentTick - lastChargeTick > lifetimeTicks)
            {
                echoCharges = 0;
                lastChargeTick = -1;
            }
        }

        private static bool IsValidWielder(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Destroyed
                && pawn.equipment != null
                && pawn.MapHeld != null;
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
