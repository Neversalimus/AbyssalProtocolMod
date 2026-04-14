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
            string core = GetSlotDisplay(slots.FirstOrDefault(slot => slot != null && slot.slotId == CoreBayId), TranslateOrFallback(CoreBayLabelKey, "core bay"));
            string auxiliary = GetSlotDisplay(slots.FirstOrDefault(slot => slot != null && slot.slotId == AuxiliaryBayId), TranslateOrFallback(AuxiliaryBayLabelKey, "auxiliary bay"));
            return TranslateOrFallback("ABY_CapacitorInspect_Bays", "Bays: {0} • {1}", core, auxiliary);
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

        public static string GetCompactSummary(Building_AbyssalSummoningCircle circle)
        {
            if (circle == null)
            {
                return TranslateOrFallback("ABY_CapacitorCompact", "Capacitors: no lattice");
            }

            if (circle.GetInstalledCapacitorCount() <= 0)
            {
                return TranslateOrFallback("ABY_CapacitorCompact", "Capacitors: no lattice");
            }

            return TranslateOrFallback(
                "ABY_CapacitorCompactActive",
                "Capacitors: {0} fitted • {1}",
                circle.GetInstalledCapacitorCount(),
                GetChargeReadout(circle));
        }
    }
}
