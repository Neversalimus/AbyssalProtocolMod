using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class AbyssalCircleCapacitorUtility
    {
        public const string CoreBayId = "core";
        public const string AuxiliaryBayId = "auxiliary";
        public const string CoreBayLabelKey = "ABY_CapacitorBay_Core";
        public const string AuxiliaryBayLabelKey = "ABY_CapacitorBay_Auxiliary";

        private static readonly AbyssalCircleCapacitorBay[] OrderedBays =
        {
            AbyssalCircleCapacitorBay.Core,
            AbyssalCircleCapacitorBay.Auxiliary
        };

        private static List<ThingDef> cachedCapacitorDefs;
        private static readonly Dictionary<string, Graphic> cachedMountedGraphics = new Dictionary<string, Graphic>();

        private static string TranslateOrFallback(string key, string fallback)
        {
            string value = key.Translate();
            return value == key ? fallback : value;
        }

        private static string TranslateOrFallback(string key, string fallbackFormat, params object[] args)
        {
            string template = key.Translate();
            if (template == key)
            {
                return string.Format(fallbackFormat, args);
            }

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        public static IEnumerable<AbyssalCircleCapacitorBay> GetOrderedBays()
        {
            for (int i = 0; i < OrderedBays.Length; i++)
            {
                yield return OrderedBays[i];
            }
        }

        public static string GetBaySlotId(AbyssalCircleCapacitorBay bay)
        {
            return bay == AbyssalCircleCapacitorBay.Auxiliary ? AuxiliaryBayId : CoreBayId;
        }

        public static string GetBayLabelKey(AbyssalCircleCapacitorBay bay)
        {
            return bay == AbyssalCircleCapacitorBay.Auxiliary ? AuxiliaryBayLabelKey : CoreBayLabelKey;
        }

        public static string GetBayLabel(AbyssalCircleCapacitorBay bay)
        {
            return TranslateOrFallback(GetBayLabelKey(bay), bay == AbyssalCircleCapacitorBay.Auxiliary ? "auxiliary bay" : "core bay");
        }

        public static AbyssalCircleCapacitorBay GetBayFromSlotId(string slotId)
        {
            return string.Equals(slotId, AuxiliaryBayId, StringComparison.OrdinalIgnoreCase)
                ? AbyssalCircleCapacitorBay.Auxiliary
                : AbyssalCircleCapacitorBay.Core;
        }

        public static void EnsureSlot(AbyssalCircleCapacitorSlot slot, string slotId, string labelKey)
        {
            if (slot == null)
            {
                return;
            }

            if (slot.slotId.NullOrEmpty())
            {
                slot.slotId = slotId;
            }

            if (slot.labelKey.NullOrEmpty())
            {
                slot.labelKey = labelKey;
            }
        }

        public static AbyssalCircleCapacitorSlot GetSlot(IEnumerable<AbyssalCircleCapacitorSlot> slots, AbyssalCircleCapacitorBay bay)
        {
            if (slots == null)
            {
                return null;
            }

            string slotId = GetBaySlotId(bay);
            foreach (AbyssalCircleCapacitorSlot slot in slots)
            {
                if (slot != null && string.Equals(slot.slotId, slotId, StringComparison.OrdinalIgnoreCase))
                {
                    return slot;
                }
            }

            return null;
        }

        public static List<ThingDef> GetCapacitorDefs()
        {
            if (cachedCapacitorDefs != null)
            {
                return cachedCapacitorDefs;
            }

            cachedCapacitorDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def?.GetModExtension<DefModExtension_AbyssalCircleCapacitor>() != null)
                .OrderBy(def => def.GetModExtension<DefModExtension_AbyssalCircleCapacitor>().tier)
                .ThenBy(def => def.label)
                .ToList();
            return cachedCapacitorDefs;
        }

        public static bool IsCapacitorThing(ThingDef def)
        {
            return def?.GetModExtension<DefModExtension_AbyssalCircleCapacitor>() != null;
        }

        public static bool AllowsBay(ThingDef def, AbyssalCircleCapacitorBay bay)
        {
            DefModExtension_AbyssalCircleCapacitor ext = def?.GetModExtension<DefModExtension_AbyssalCircleCapacitor>();
            if (ext == null)
            {
                return false;
            }

            return bay == AbyssalCircleCapacitorBay.Auxiliary ? ext.allowAuxiliaryBay : ext.allowCoreBay;
        }

        public static float GetTotalCapacity(IEnumerable<AbyssalCircleCapacitorSlot> slots)
        {
            return slots?.Sum(slot => slot?.InstalledExtension?.chargeCapacity ?? 0f) ?? 0f;
        }

        public static float GetTotalThroughput(IEnumerable<AbyssalCircleCapacitorSlot> slots)
        {
            return slots?.Sum(slot => slot?.InstalledExtension?.throughput ?? 0f) ?? 0f;
        }

        public static float GetTotalChargeRate(IEnumerable<AbyssalCircleCapacitorSlot> slots)
        {
            return slots?.Sum(slot => slot?.InstalledExtension?.chargeRatePerSecond ?? 0f) ?? 0f;
        }

        public static int GetInstalledCount(IEnumerable<AbyssalCircleCapacitorSlot> slots)
        {
            return slots?.Count(slot => slot != null && !slot.IsEmpty) ?? 0;
        }

        public static string GetTierLabel(ThingDef def)
        {
            DefModExtension_AbyssalCircleCapacitor extension = def?.GetModExtension<DefModExtension_AbyssalCircleCapacitor>();
            if (extension == null)
            {
                return TranslateOrFallback("ABY_CapacitorTier_None", "none");
            }

            switch (extension.tier)
            {
                case 1:
                    return TranslateOrFallback("ABY_CapacitorTier_Ashbound", "ashbound");
                case 2:
                    return TranslateOrFallback("ABY_CapacitorTier_Rift", "rift");
                case 3:
                    return TranslateOrFallback("ABY_CapacitorTier_Crown", "crown");
                default:
                    return def.LabelCap;
            }
        }

        public static string GetPeakTierLabel(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CapacitorTier_None", "none");
            }

            ThingDef best = circle.GetCapacitorSlots()
                .Select(slot => slot?.InstalledThingDef)
                .Where(def => def != null)
                .OrderByDescending(def => def.GetModExtension<DefModExtension_AbyssalCircleCapacitor>()?.tier ?? 0)
                .FirstOrDefault();
            return best != null ? GetTierLabel(best) : TranslateOrFallback("ABY_CapacitorTier_None", "none");
        }

        public static string GetInstalledSummary(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CapacitorInspect_Slots", "Capacitors: {0} / {1} fitted", 0, 2);
            }

            int installed = circle.GetInstalledCapacitorCount();
            int total = circle.GetCapacitorSlotCount();
            if (installed <= 0)
            {
                return TranslateOrFallback("ABY_CapacitorInspect_Slots", "Capacitors: {0} / {1} fitted", installed, total);
            }

            return TranslateOrFallback("ABY_CapacitorInspect_SlotsTier", "Capacitors: {0} / {1} fitted • peak tier: {2}", installed, total, GetPeakTierLabel(circle));
        }

        public static string GetBaySummary(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CapacitorInspect_Bays", "Bays: {0} • {1}", "core bay " + TranslateOrFallback("ABY_CapacitorSlot_Empty", "empty"), "auxiliary bay " + TranslateOrFallback("ABY_CapacitorSlot_Empty", "empty"));
            }

            List<AbyssalCircleCapacitorSlot> slots = circle.GetCapacitorSlots().ToList();
            string core = GetSlotDisplay(GetSlot(slots, AbyssalCircleCapacitorBay.Core), GetBayLabel(AbyssalCircleCapacitorBay.Core));
            string auxiliary = GetSlotDisplay(GetSlot(slots, AbyssalCircleCapacitorBay.Auxiliary), GetBayLabel(AbyssalCircleCapacitorBay.Auxiliary));
            return TranslateOrFallback("ABY_CapacitorInspect_Bays", "Bays: {0} • {1}", core, auxiliary);
        }

        public static string GetSlotRowText(AbyssalCircleCapacitorSlot slot, AbyssalCircleCapacitorBay bay)
        {
            ThingDef installedDef = slot?.InstalledThingDef;
            string bayLabel = GetBayLabel(bay);
            if (installedDef == null)
            {
                return TranslateOrFallback("ABY_CapacitorSlotRow_Empty", "{0}: empty", bayLabel);
            }

            return TranslateOrFallback("ABY_CapacitorSlotRow_Installed", "{0}: {1} • {2}", bayLabel, installedDef.label.CapitalizeFirst(), GetTierLabel(installedDef));
        }

        public static string GetBayTooltip(Building_AbyssalSummoningCircle circle, AbyssalCircleCapacitorBay bay)
        {
            AbyssalCircleCapacitorSlot slot = circle?.GetCapacitorSlot(bay);
            if (slot == null || slot.IsEmpty)
            {
                return TranslateOrFallback("ABY_CapacitorBayTooltip_Empty", "{0} is empty. Install a capacitor module here to add charge capacity, throughput and recharge rate.", GetBayLabel(bay));
            }

            DefModExtension_AbyssalCircleCapacitor ext = slot.InstalledExtension;
            if (ext == null)
            {
                return GetSlotRowText(slot, bay);
            }

            return TranslateOrFallback(
                "ABY_CapacitorBayTooltip_Filled",
                "{0}\nTier: {1}\nCapacity: {2}\nThroughput: {3}\nCharge rate: {4}/s",
                GetSlotRowText(slot, bay),
                GetTierLabel(slot.InstalledThingDef),
                Mathf.RoundToInt(ext.chargeCapacity),
                Mathf.RoundToInt(ext.throughput),
                ext.chargeRatePerSecond.ToString("0.0"));
        }

        private static string GetSlotDisplay(AbyssalCircleCapacitorSlot slot, string fallbackLabel)
        {
            string label = fallbackLabel;
            if (slot != null && !slot.labelKey.NullOrEmpty())
            {
                label = TranslateOrFallback(slot.labelKey, fallbackLabel);
            }

            if (slot == null || slot.IsEmpty)
            {
                return label + " " + TranslateOrFallback("ABY_CapacitorSlot_Empty", "empty");
            }

            return label + " " + GetTierLabel(slot.InstalledThingDef);
        }

        public static string GetChargeStateLabel(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CapacitorChargeState_NoLattice", "no lattice");
            }

            float capacity = circle.GetCapacitorCapacity();
            if (capacity <= 0.01f)
            {
                return TranslateOrFallback("ABY_CapacitorChargeState_NoLattice", "no lattice");
            }

            if (circle.CapacitorRecovering && circle.StoredCapacitorCharge + 0.5f < capacity)
            {
                return TranslateOrFallback("ABY_CapacitorChargeState_Recovering", "recovering");
            }

            float fill = Mathf.Clamp01(circle.StoredCapacitorCharge / capacity);
            if (fill <= 0.01f)
            {
                return TranslateOrFallback("ABY_CapacitorChargeState_Dormant", "dormant");
            }

            if (fill < 0.35f)
            {
                return TranslateOrFallback("ABY_CapacitorChargeState_Priming", "priming");
            }

            if (fill < 0.95f)
            {
                return TranslateOrFallback("ABY_CapacitorChargeState_Charged", "charged");
            }

            return TranslateOrFallback("ABY_CapacitorChargeState_Saturated", "saturated");
        }

        public static string GetChargeReadout(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CapacitorInspect_ChargeNoLattice", "Charge: no capacitor lattice");
            }

            float capacity = circle.GetCapacitorCapacity();
            if (capacity <= 0.01f)
            {
                return TranslateOrFallback("ABY_CapacitorInspect_ChargeNoLattice", "Charge: no capacitor lattice");
            }

            return TranslateOrFallback(
                "ABY_CapacitorInspect_Charge",
                "Charge: {0} ({1} / {2})",
                GetChargeStateLabel(circle),
                Mathf.RoundToInt(circle.StoredCapacitorCharge),
                Mathf.RoundToInt(capacity));
        }

        public static string GetThroughputReadout(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CapacitorInspect_Throughput", "Throughput: 0");
            }

            return TranslateOrFallback("ABY_CapacitorInspect_Throughput", "Throughput: {0}", Mathf.RoundToInt(circle.GetCapacitorThroughput()));
        }

        public static string GetChargeRateReadout(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CapacitorInspect_ChargeRate", "Charge rate: 0/s");
            }

            return TranslateOrFallback("ABY_CapacitorInspect_ChargeRate", "Charge rate: {0}/s", circle.GetCapacitorChargeRatePerSecond().ToString("0.0"));
        }

        public static float GetMountedDrawScale(ThingDef def, float fallback = 1f)
        {
            return def?.GetModExtension<DefModExtension_AbyssalCircleCapacitor>()?.mountedDrawScale ?? fallback;
        }

        public static Graphic GetMountedGraphic(ThingDef def)
        {
            DefModExtension_AbyssalCircleCapacitor extension = def?.GetModExtension<DefModExtension_AbyssalCircleCapacitor>();
            if (extension == null || extension.mountedTexPath.NullOrEmpty())
            {
                return null;
            }

            float scale = Mathf.Max(0.10f, extension.mountedDrawScale);
            string cacheKey = extension.mountedTexPath + "|" + scale.ToString("0.###");
            if (cachedMountedGraphics.TryGetValue(cacheKey, out Graphic graphic))
            {
                return graphic;
            }

            graphic = GraphicDatabase.Get<Graphic_Single>(extension.mountedTexPath, ShaderDatabase.TransparentPostLight, new Vector2(scale, scale), Color.white);
            cachedMountedGraphics[cacheKey] = graphic;
            return graphic;
        }

        public static List<Thing> GetBestAvailableCapacitorCandidates(Building_AbyssalSummoningCircle circle, AbyssalCircleCapacitorBay bay)
        {
            List<Thing> candidates = new List<Thing>();
            if (circle?.Map == null)
            {
                return candidates;
            }

            List<ThingDef> defs = GetCapacitorDefs();
            for (int i = 0; i < defs.Count; i++)
            {
                ThingDef def = defs[i];
                if (!AllowsBay(def, bay))
                {
                    continue;
                }

                Thing bestThing = FindBestAvailableThingOfDef(circle, def);
                if (bestThing != null)
                {
                    candidates.Add(bestThing);
                }
            }

            candidates.Sort(delegate(Thing a, Thing b)
            {
                DefModExtension_AbyssalCircleCapacitor extA = a.def.GetModExtension<DefModExtension_AbyssalCircleCapacitor>();
                DefModExtension_AbyssalCircleCapacitor extB = b.def.GetModExtension<DefModExtension_AbyssalCircleCapacitor>();
                int tierCompare = (extB?.tier ?? 0).CompareTo(extA?.tier ?? 0);
                if (tierCompare != 0)
                {
                    return tierCompare;
                }

                float distA = a.PositionHeld.IsValid ? a.PositionHeld.DistanceToSquared(circle.PositionHeld) : float.MaxValue;
                float distB = b.PositionHeld.IsValid ? b.PositionHeld.DistanceToSquared(circle.PositionHeld) : float.MaxValue;
                int distCompare = distA.CompareTo(distB);
                if (distCompare != 0)
                {
                    return distCompare;
                }

                return string.Compare(a.LabelCap, b.LabelCap, StringComparison.OrdinalIgnoreCase);
            });

            return candidates;
        }

        public static int CountAvailableCapacitors(Map map, ThingDef def)
        {
            if (map == null || def == null)
            {
                return 0;
            }

            List<Thing> things = map.listerThings.ThingsOfDef(def);
            int count = 0;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.MapHeld != map)
                {
                    continue;
                }

                if (thing.IsForbidden(Faction.OfPlayerSilentFail))
                {
                    continue;
                }

                count += Mathf.Max(1, thing.stackCount);
            }

            return count;
        }

        public static string GetCompactSummary(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null || circle.GetInstalledCapacitorCount() <= 0)
            {
                return TranslateOrFallback("ABY_CapacitorCompact", "Capacitors: no lattice");
            }

            return TranslateOrFallback("ABY_CapacitorCompactActive", "Capacitors: {0} fitted • {1}", circle.GetInstalledCapacitorCount(), GetChargeReadout(circle));
        }

        public static string GetRecoverySummary(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null || !circle.CapacitorRecovering)
            {
                return TranslateOrFallback("ABY_CapacitorRecovery_None", "stable");
            }

            int seconds = Mathf.CeilToInt(circle.CapacitorRecoveryTicksRemaining / 60f);
            return TranslateOrFallback("ABY_CapacitorRecovery_Readout", "~{0}s", seconds);
        }

        private static Thing FindBestAvailableThingOfDef(Building_AbyssalSummoningCircle circle, ThingDef def)
        {
            if (circle?.Map == null || def == null)
            {
                return null;
            }

            List<Thing> things = circle.Map.listerThings.ThingsOfDef(def);
            Thing best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.MapHeld != circle.Map)
                {
                    continue;
                }

                if (thing.IsForbidden(Faction.OfPlayerSilentFail))
                {
                    continue;
                }

                float score = thing.PositionHeld.DistanceToSquared(circle.PositionHeld);
                if (score < bestScore)
                {
                    best = thing;
                    bestScore = score;
                }
            }

            return best;
        }
    }
}
