using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    [StaticConstructorOnStartup]
    public static class AbyssalCircleModuleUtility
    {
        private const float SocketDrawScale = 1.18f;

        private static readonly AbyssalCircleModuleEdge[] OrderedEdges =
        {
            AbyssalCircleModuleEdge.North,
            AbyssalCircleModuleEdge.East,
            AbyssalCircleModuleEdge.South,
            AbyssalCircleModuleEdge.West
        };

        private static readonly Dictionary<string, Graphic> MountedGraphicCache = new Dictionary<string, Graphic>();
        private static readonly Dictionary<string, Graphic> GlowGraphicCache = new Dictionary<string, Graphic>();

        private static readonly Graphic SocketGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/Modules/ABY_CircleModuleSocket",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        public static List<AbyssalCircleModuleSlot> EnsureSlots(List<AbyssalCircleModuleSlot> slots)
        {
            List<AbyssalCircleModuleSlot> normalized = new List<AbyssalCircleModuleSlot>(OrderedEdges.Length);
            for (int i = 0; i < OrderedEdges.Length; i++)
            {
                AbyssalCircleModuleEdge edge = OrderedEdges[i];
                AbyssalCircleModuleSlot slot = GetSlot(slots, edge);
                normalized.Add(slot ?? new AbyssalCircleModuleSlot(edge));
            }

            return normalized;
        }

        public static AbyssalCircleModuleSlot GetSlot(IReadOnlyList<AbyssalCircleModuleSlot> slots, AbyssalCircleModuleEdge edge)
        {
            if (slots == null)
            {
                return null;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                AbyssalCircleModuleSlot slot = slots[i];
                if (slot != null && slot.Edge == edge)
                {
                    return slot;
                }
            }

            return null;
        }

        public static int CountInstalledModules(IReadOnlyList<AbyssalCircleModuleSlot> slots, string requiredFamily = null)
        {
            if (slots == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                AbyssalCircleModuleSlot slot = slots[i];
                if (slot == null || !slot.Occupied)
                {
                    continue;
                }

                if (!requiredFamily.NullOrEmpty())
                {
                    DefModExtension_AbyssalCircleModule ext = GetModuleExtension(slot.InstalledThingDef);
                    if (ext == null || !string.Equals(ext.moduleFamily ?? DefModExtension_AbyssalCircleModule.StabilizerFamily, requiredFamily))
                    {
                        continue;
                    }
                }

                count++;
            }

            return count;
        }

        public static DefModExtension_AbyssalCircleModule GetModuleExtension(ThingDef thingDef)
        {
            return thingDef?.GetModExtension<DefModExtension_AbyssalCircleModule>();
        }

        public static bool IsModuleThingDef(ThingDef thingDef)
        {
            return GetModuleExtension(thingDef) != null;
        }

        public static bool IsMatchingFamily(ThingDef thingDef, string requiredFamily)
        {
            DefModExtension_AbyssalCircleModule ext = GetModuleExtension(thingDef);
            if (ext == null)
            {
                return false;
            }

            string family = ext.moduleFamily ?? DefModExtension_AbyssalCircleModule.StabilizerFamily;
            return requiredFamily.NullOrEmpty() || string.Equals(family, requiredFamily);
        }

        public static Graphic GetSocketGraphic()
        {
            return SocketGraphic;
        }

        public static Graphic GetMountedGraphic(ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return null;
            }

            DefModExtension_AbyssalCircleModule ext = GetModuleExtension(thingDef);
            if (ext == null || ext.mountedTexPath.NullOrEmpty())
            {
                return null;
            }

            if (MountedGraphicCache.TryGetValue(ext.mountedTexPath, out Graphic cachedGraphic))
            {
                return cachedGraphic;
            }

            Graphic graphic = GraphicDatabase.Get<Graphic_Single>(
                ext.mountedTexPath,
                ShaderDatabase.TransparentPostLight,
                Vector2.one,
                Color.white);

            MountedGraphicCache[ext.mountedTexPath] = graphic;
            return graphic;
        }

        public static Graphic GetGlowGraphic(ThingDef thingDef)
        {
            if (thingDef == null)
            {
                return null;
            }

            DefModExtension_AbyssalCircleModule ext = GetModuleExtension(thingDef);
            if (ext == null || ext.glowTexPath.NullOrEmpty())
            {
                return null;
            }

            if (GlowGraphicCache.TryGetValue(ext.glowTexPath, out Graphic cachedGraphic))
            {
                return cachedGraphic;
            }

            Graphic graphic = GraphicDatabase.Get<Graphic_Single>(
                ext.glowTexPath,
                ShaderDatabase.TransparentPostLight,
                Vector2.one,
                Color.white);

            GlowGraphicCache[ext.glowTexPath] = graphic;
            return graphic;
        }

        public static float GetMountedDrawScale(ThingDef thingDef)
        {
            DefModExtension_AbyssalCircleModule ext = GetModuleExtension(thingDef);
            if (ext == null)
            {
                return SocketDrawScale;
            }

            return Mathf.Max(0.75f, ext.mountedDrawScale);
        }

        public static float GetGlowDrawScale(ThingDef thingDef)
        {
            DefModExtension_AbyssalCircleModule ext = GetModuleExtension(thingDef);
            if (ext == null)
            {
                return SocketDrawScale * 1.12f;
            }

            return Mathf.Max(0.90f, ext.glowDrawScale);
        }

        public static float GetSocketDrawScaleValue()
        {
            return SocketDrawScale;
        }

        public static Vector3 GetEdgeOffset(AbyssalCircleModuleEdge edge)
        {
            switch (edge)
            {
                case AbyssalCircleModuleEdge.North:
                    return new Vector3(0f, 0f, 3.47f);
                case AbyssalCircleModuleEdge.East:
                    return new Vector3(3.47f, 0f, 0f);
                case AbyssalCircleModuleEdge.South:
                    return new Vector3(0f, 0f, -3.47f);
                case AbyssalCircleModuleEdge.West:
                    return new Vector3(-3.47f, 0f, 0f);
                default:
                    return Vector3.zero;
            }
        }

        public static float GetEdgeAngle(AbyssalCircleModuleEdge edge)
        {
            switch (edge)
            {
                case AbyssalCircleModuleEdge.North:
                    return 0f;
                case AbyssalCircleModuleEdge.East:
                    return 90f;
                case AbyssalCircleModuleEdge.South:
                    return 180f;
                case AbyssalCircleModuleEdge.West:
                    return 270f;
                default:
                    return 0f;
            }
        }

        public static string GetEdgeLabel(AbyssalCircleModuleEdge edge)
        {
            switch (edge)
            {
                case AbyssalCircleModuleEdge.North:
                    return "ABY_CircleModuleEdge_North".Translate();
                case AbyssalCircleModuleEdge.East:
                    return "ABY_CircleModuleEdge_East".Translate();
                case AbyssalCircleModuleEdge.South:
                    return "ABY_CircleModuleEdge_South".Translate();
                case AbyssalCircleModuleEdge.West:
                    return "ABY_CircleModuleEdge_West".Translate();
                default:
                    return edge.ToString();
            }
        }

        public static IEnumerable<AbyssalCircleModuleEdge> GetOrderedEdges()
        {
            for (int i = 0; i < OrderedEdges.Length; i++)
            {
                yield return OrderedEdges[i];
            }
        }

        public static AbyssalCircleStabilizerBonusSummary GetStabilizerBonusSummary(IReadOnlyList<AbyssalCircleModuleSlot> slots)
        {
            AbyssalCircleStabilizerBonusSummary summary = new AbyssalCircleStabilizerBonusSummary
            {
                LowestTier = int.MaxValue,
                HeatMultiplier = 1f,
                ContaminationMultiplier = 1f,
                ContaminationPenaltyMultiplier = 1f,
                EventChanceMultiplier = 1f,
                EventSeverityMultiplier = 1f,
                PurgeEfficiencyMultiplier = 1f,
                VentEfficiencyMultiplier = 1f
            };

            if (slots == null)
            {
                summary.LowestTier = 0;
                return summary;
            }

            bool north = false;
            bool south = false;
            bool east = false;
            bool west = false;

            for (int i = 0; i < slots.Count; i++)
            {
                AbyssalCircleModuleSlot slot = slots[i];
                if (slot == null || !slot.Occupied)
                {
                    continue;
                }

                ThingDef installedDef = slot.InstalledThingDef;
                DefModExtension_AbyssalCircleModule ext = GetModuleExtension(installedDef);
                if (ext == null || !IsMatchingFamily(installedDef, DefModExtension_AbyssalCircleModule.StabilizerFamily))
                {
                    continue;
                }

                summary.InstalledCount++;
                summary.HighestTier = Mathf.Max(summary.HighestTier, ext.tier);
                summary.LowestTier = Mathf.Min(summary.LowestTier, ext.tier);
                summary.ContainmentBonus += Mathf.Max(0f, ext.containmentBonus) * 0.22f;
                summary.HeatMultiplier *= Mathf.Clamp(ext.ritualHeatMultiplier, 0.86f, 1f);
                summary.ContaminationMultiplier *= Mathf.Clamp(ext.contaminationMultiplier, 0.82f, 1f);
                summary.PurgeEfficiencyMultiplier *= 1f + Mathf.Max(0, ext.tier - 1) * 0.012f;
                summary.VentEfficiencyMultiplier *= 1f + ext.tier * 0.015f;

                switch (slot.Edge)
                {
                    case AbyssalCircleModuleEdge.North:
                        north = true;
                        break;
                    case AbyssalCircleModuleEdge.East:
                        east = true;
                        break;
                    case AbyssalCircleModuleEdge.South:
                        south = true;
                        break;
                    case AbyssalCircleModuleEdge.West:
                        west = true;
                        break;
                }
            }

            if (summary.InstalledCount <= 0)
            {
                summary.LowestTier = 0;
                summary.HighestTier = 0;
                return summary;
            }

            summary.FullRing = summary.InstalledCount >= OrderedEdges.Length;
            summary.UniformTier = summary.FullRing && summary.HighestTier == summary.LowestTier;
            summary.OpposingPairs = 0;
            if (north && south)
            {
                summary.OpposingPairs++;
            }
            if (east && west)
            {
                summary.OpposingPairs++;
            }

            if (summary.OpposingPairs > 0)
            {
                summary.ContainmentBonus += 0.020f * summary.OpposingPairs;
                summary.HeatMultiplier *= Mathf.Pow(0.965f, summary.OpposingPairs);
                summary.ContaminationMultiplier *= Mathf.Pow(0.95f, summary.OpposingPairs);
                summary.ContaminationPenaltyMultiplier *= Mathf.Pow(0.94f, summary.OpposingPairs);
                summary.EventChanceMultiplier *= Mathf.Pow(0.92f, summary.OpposingPairs);
                summary.EventSeverityMultiplier *= Mathf.Pow(0.94f, summary.OpposingPairs);
                summary.PurgeEfficiencyMultiplier *= Mathf.Pow(1.03f, summary.OpposingPairs);
                summary.VentEfficiencyMultiplier *= Mathf.Pow(1.04f, summary.OpposingPairs);
            }

            if (summary.FullRing)
            {
                summary.ContainmentBonus += 0.022f;
                summary.HeatMultiplier *= 0.96f;
                summary.ContaminationMultiplier *= 0.93f;
                summary.ContaminationPenaltyMultiplier *= 0.91f;
                summary.EventChanceMultiplier *= 0.91f;
                summary.EventSeverityMultiplier *= 0.93f;
                summary.PurgeEfficiencyMultiplier *= 1.05f;
                summary.VentEfficiencyMultiplier *= 1.06f;
            }

            if (summary.UniformTier)
            {
                float tierFactor = summary.HighestTier;
                float tierLerp = Mathf.Clamp01((tierFactor - 1f) / 2f);
                summary.ContainmentBonus += 0.008f + tierFactor * 0.005f;
                summary.HeatMultiplier *= Mathf.Lerp(0.99f, 0.945f, tierLerp);
                summary.ContaminationMultiplier *= Mathf.Lerp(0.985f, 0.90f, tierLerp);
                summary.ContaminationPenaltyMultiplier *= Mathf.Lerp(0.975f, 0.86f, tierLerp);
                summary.EventChanceMultiplier *= Mathf.Lerp(0.975f, 0.88f, tierLerp);
                summary.EventSeverityMultiplier *= Mathf.Lerp(0.98f, 0.90f, tierLerp);
                summary.PurgeEfficiencyMultiplier *= 1f + tierFactor * 0.025f;
                summary.VentEfficiencyMultiplier *= 1f + tierFactor * 0.03f;
            }

            summary.ContainmentBonus = Mathf.Clamp(summary.ContainmentBonus, 0f, 0.28f);
            summary.HeatMultiplier = Mathf.Clamp(summary.HeatMultiplier, 0.68f, 1f);
            summary.ContaminationMultiplier = Mathf.Clamp(summary.ContaminationMultiplier, 0.60f, 1f);
            summary.ContaminationPenaltyMultiplier = Mathf.Clamp(summary.ContaminationPenaltyMultiplier, 0.60f, 1f);
            summary.EventChanceMultiplier = Mathf.Clamp(summary.EventChanceMultiplier, 0.62f, 1f);
            summary.EventSeverityMultiplier = Mathf.Clamp(summary.EventSeverityMultiplier, 0.75f, 1f);
            summary.PurgeEfficiencyMultiplier = Mathf.Clamp(summary.PurgeEfficiencyMultiplier, 1f, 1.24f);
            summary.VentEfficiencyMultiplier = Mathf.Clamp(summary.VentEfficiencyMultiplier, 1f, 1.28f);
            return summary;
        }

        public static string GetPatternKey(AbyssalCircleStabilizerBonusSummary summary)
        {
            if (!summary.AnyInstalled)
            {
                return "ABY_CircleStabilizerPattern_Open";
            }

            if (summary.UniformTier)
            {
                return "ABY_CircleStabilizerPattern_Symmetry";
            }

            if (summary.FullRing)
            {
                return "ABY_CircleStabilizerPattern_FullRing";
            }

            if (summary.OpposingPairs > 0)
            {
                return "ABY_CircleStabilizerPattern_Paired";
            }

            return "ABY_CircleStabilizerPattern_Partial";
        }

        public static string GetTierLabel(ThingDef thingDef)
        {
            DefModExtension_AbyssalCircleModule ext = GetModuleExtension(thingDef);
            if (ext == null)
            {
                return "-";
            }

            return "ABY_CircleModuleTierLabel".Translate(ext.tier);
        }

        public static List<Thing> GetBestAvailableModuleCandidates(Building_AbyssalSummoningCircle circle, string requiredFamily = null)
        {
            List<Thing> results = new List<Thing>();
            if (circle?.Map == null)
            {
                return results;
            }

            Dictionary<ThingDef, Thing> bestByDef = new Dictionary<ThingDef, Thing>();
            Dictionary<ThingDef, float> bestScoreByDef = new Dictionary<ThingDef, float>();
            List<Thing> allThings = circle.Map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.MapHeld != circle.Map)
                {
                    continue;
                }

                if (!IsMatchingFamily(thing.def, requiredFamily))
                {
                    continue;
                }

                float score = thing.PositionHeld.DistanceToSquared(circle.PositionHeld);
                if (!bestByDef.TryGetValue(thing.def, out Thing currentBest) || score < bestScoreByDef[thing.def])
                {
                    bestByDef[thing.def] = thing;
                    bestScoreByDef[thing.def] = score;
                }
            }

            foreach (Thing candidate in bestByDef.Values)
            {
                results.Add(candidate);
            }

            results.SortBy(t => GetModuleExtension(t.def)?.tier ?? 0);
            return results;
        }

        public static int CountAvailableModules(Map map, ThingDef thingDef)
        {
            if (map == null || thingDef == null)
            {
                return 0;
            }

            int count = 0;
            List<Thing> allThings = map.listerThings.AllThings;
            for (int i = 0; i < allThings.Count; i++)
            {
                Thing thing = allThings[i];
                if (thing == null || thing.Destroyed || !thing.Spawned || thing.MapHeld != map || thing.def != thingDef)
                {
                    continue;
                }

                count += Mathf.Max(1, thing.stackCount);
            }

            return count;
        }
    }
}
