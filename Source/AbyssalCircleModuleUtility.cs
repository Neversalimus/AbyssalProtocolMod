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

        public static AbyssalCircleModuleSlot GetSlot(List<AbyssalCircleModuleSlot> slots, AbyssalCircleModuleEdge edge)
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

        public static int CountInstalledModules(List<AbyssalCircleModuleSlot> slots, string requiredFamily = null)
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

        public static float GetMountedDrawScale(ThingDef thingDef)
        {
            DefModExtension_AbyssalCircleModule ext = GetModuleExtension(thingDef);
            if (ext == null)
            {
                return SocketDrawScale;
            }

            return Mathf.Max(0.75f, ext.mountedDrawScale);
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
    }
}
