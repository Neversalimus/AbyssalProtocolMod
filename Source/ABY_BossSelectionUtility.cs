using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_BossSelectionUtility
    {
        private sealed class SelectionProfile
        {
            public float widthCells;
            public float heightCells;
            public float yOffsetCells;
            public int priority;
        }

        private static readonly Dictionary<string, SelectionProfile> Profiles = new Dictionary<string, SelectionProfile>
        {
            { "ABY_ReactorSaint", new SelectionProfile { widthCells = 11.5f, heightCells = 8.8f, yOffsetCells = -0.2f, priority = 120 } },
            { "ABY_ArchonBeast", new SelectionProfile { widthCells = 7.6f, heightCells = 6.6f, yOffsetCells = 0.0f, priority = 100 } },
            { "ABY_ArchonOfRupture", new SelectionProfile { widthCells = 8.0f, heightCells = 7.2f, yOffsetCells = 0.0f, priority = 110 } },
            { "ABY_SiegeIdol", new SelectionProfile { widthCells = 5.8f, heightCells = 4.8f, yOffsetCells = 0.0f, priority = 65 } },
            { "ABY_ChoirEngine", new SelectionProfile { widthCells = 5.4f, heightCells = 4.8f, yOffsetCells = 0.0f, priority = 70 } },
            { "ABY_WardenOfAsh", new SelectionProfile { widthCells = 4.7f, heightCells = 4.2f, yOffsetCells = 0.0f, priority = 55 } },
            { "ABY_GateWarden", new SelectionProfile { widthCells = 4.2f, heightCells = 3.8f, yOffsetCells = 0.0f, priority = 45 } }
        };

        public static bool TrySelectBossUnderMouse(Event currentEvent)
        {
            if (currentEvent == null || currentEvent.type != EventType.MouseUp || currentEvent.button != 0)
            {
                return false;
            }

            if (!AbyssalProtocolMod.Settings.enableBossExpandedSelection || Current.ProgramState != ProgramState.Playing)
            {
                return false;
            }

            if (IsDebugToolActive() || AbyssalBossBarRenderer.MouseOverInteractiveRect(currentEvent.mousePosition))
            {
                return false;
            }

            Map map = Find.CurrentMap;
            if (map == null || Find.Selector == null || Find.Camera == null)
            {
                return false;
            }

            Pawn boss = FindSelectableBossUnderMouse(map, currentEvent.mousePosition);
            if (boss == null)
            {
                return false;
            }

            if (MouseCellContainsPreferredSelectable(map, boss))
            {
                return false;
            }

            try
            {
                if (!currentEvent.shift)
                {
                    Find.Selector.ClearSelection();
                }
                Find.Selector.Select(boss);
                currentEvent.Use();
                return true;
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("boss-expanded-selection-select", "[Abyssal Protocol] Boss expanded selection failed: " + ex.GetType().Name + ": " + ex.Message, 2000);
                return false;
            }
        }

        public static Pawn FindSelectableBossUnderMouse(Map map, Vector2 mousePosition)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return null;
            }

            Pawn best = null;
            int bestPriority = int.MinValue;
            float bestDistance = float.MaxValue;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (!IsExpandedSelectableBoss(pawn, out SelectionProfile profile))
                {
                    continue;
                }

                Rect rect = GetScreenSelectionRect(pawn, profile);
                if (!rect.Contains(mousePosition))
                {
                    continue;
                }

                float distance = Vector2.Distance(mousePosition, rect.center);
                int priority = profile.priority;
                if (priority > bestPriority || (priority == bestPriority && distance < bestDistance))
                {
                    best = pawn;
                    bestPriority = priority;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool MouseCellContainsPreferredSelectable(Map map, Pawn boss)
        {
            if (map == null || boss == null)
            {
                return false;
            }

            IntVec3 mouseCell;
            try
            {
                mouseCell = UI.MouseCell();
            }
            catch
            {
                return false;
            }

            if (!mouseCell.IsValid || !mouseCell.InBounds(map))
            {
                return false;
            }

            List<Thing> things = mouseCell.GetThingList(map);
            for (int i = things.Count - 1; i >= 0; i--)
            {
                Thing thing = things[i];
                if (thing == null || thing == boss || thing.Destroyed)
                {
                    continue;
                }

                if (IsExpandedSelectableBoss(thing as Pawn, out _))
                {
                    continue;
                }

                if (IsSelectableThing(thing))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSelectableThing(Thing thing)
        {
            if (thing == null || thing.Destroyed || !thing.Spawned)
            {
                return false;
            }

            try
            {
                if (thing.def != null && thing.def.selectable)
                {
                    return true;
                }
            }
            catch
            {
            }

            return thing is Pawn || thing is Building;
        }

        private static bool IsDebugToolActive()
        {
            if (Prefs.DevMode == false)
            {
                return false;
            }

            try
            {
                Type debugToolsType = typeof(Log).Assembly.GetType("Verse.DebugTools");
                if (debugToolsType == null)
                {
                    return false;
                }

                FieldInfo[] fields = debugToolsType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field == null || field.FieldType == null)
                    {
                        continue;
                    }

                    string name = field.Name ?? string.Empty;
                    if (name.IndexOf("tool", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    object value = field.GetValue(null);
                    if (value != null && !field.FieldType.IsArray && !typeof(System.Collections.IEnumerable).IsAssignableFrom(field.FieldType))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool IsExpandedSelectableBoss(Pawn pawn, out SelectionProfile profile)
        {
            profile = null;
            if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.MapHeld != Find.CurrentMap)
            {
                return false;
            }

            string thingDefName = pawn.def?.defName;
            string kindDefName = pawn.kindDef?.defName;
            if (!thingDefName.NullOrEmpty() && Profiles.TryGetValue(thingDefName, out profile))
            {
                return true;
            }

            if (!kindDefName.NullOrEmpty() && Profiles.TryGetValue(kindDefName, out profile))
            {
                return true;
            }

            if (pawn.TryGetComp<CompABY_BossTrueDeath>() != null || pawn.TryGetComp<CompABY_BossNoDowned>() != null)
            {
                profile = new SelectionProfile { widthCells = 4.2f, heightCells = 3.8f, yOffsetCells = 0f, priority = 40 };
                return true;
            }

            return false;
        }

        private static Rect GetScreenSelectionRect(Pawn pawn, SelectionProfile profile)
        {
            Camera camera = Find.Camera;
            float pixelsPerCell = ResolvePixelsPerCell(camera);
            Vector3 worldPos = pawn.DrawPos + new Vector3(0f, 0f, profile.yOffsetCells);
            Vector3 screen = camera.WorldToScreenPoint(worldPos);
            Vector2 guiPoint = new Vector2(screen.x, UI.screenHeight - screen.y);
            float width = Mathf.Max(26f, profile.widthCells * pixelsPerCell);
            float height = Mathf.Max(26f, profile.heightCells * pixelsPerCell);
            return new Rect(guiPoint.x - width * 0.5f, guiPoint.y - height * 0.5f, width, height);
        }

        private static float ResolvePixelsPerCell(Camera camera)
        {
            if (camera == null || camera.orthographicSize <= 0.01f)
            {
                return 24f;
            }

            return Mathf.Clamp(UI.screenHeight / (camera.orthographicSize * 2f), 8f, 90f);
        }
    }
}
