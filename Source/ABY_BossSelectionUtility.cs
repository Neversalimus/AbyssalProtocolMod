using System;
using System.Collections.Generic;
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

        public static bool TryBeginExpandedBossClick(Event currentEvent, out Pawn boss)
        {
            boss = null;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return false;
            }

            if (!CanUseExpandedSelection(currentEvent.mousePosition))
            {
                return false;
            }

            Map map = Find.CurrentMap;
            boss = FindSelectableBossUnderMouse(map, currentEvent.mousePosition);
            if (boss == null)
            {
                return false;
            }

            if (MouseCellContainsPreferredSelectable(map, boss))
            {
                boss = null;
                return false;
            }

            return true;
        }

        public static bool TryCompleteExpandedBossClick(Event currentEvent, Pawn pendingBoss)
        {
            if (currentEvent == null || currentEvent.type != EventType.MouseUp || currentEvent.button != 0 || pendingBoss == null)
            {
                return false;
            }

            if (!CanUseExpandedSelection(currentEvent.mousePosition))
            {
                return false;
            }

            Map map = Find.CurrentMap;
            if (map == null || pendingBoss.Destroyed || pendingBoss.Dead || !pendingBoss.Spawned || pendingBoss.MapHeld != map)
            {
                return false;
            }

            Pawn bossUnderMouse = FindSelectableBossUnderMouse(map, currentEvent.mousePosition);
            if (bossUnderMouse != pendingBoss)
            {
                return false;
            }

            if (MouseCellContainsPreferredSelectable(map, pendingBoss))
            {
                return false;
            }

            try
            {
                if (!currentEvent.shift)
                {
                    Find.Selector.ClearSelection();
                }
                Find.Selector.Select(pendingBoss);
                return true;
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning("boss-expanded-selection-select", "[Abyssal Protocol] Boss expanded selection failed: " + ex.GetType().Name + ": " + ex.Message, 2000);
                return false;
            }
        }

        public static bool TrySelectBossUnderMouse(Event currentEvent)
        {
            if (!TryBeginExpandedBossClick(currentEvent, out Pawn boss))
            {
                return false;
            }

            return TryCompleteExpandedBossClick(currentEvent, boss);
        }

        public static Pawn FindSelectableBossUnderMouse(Map map, Vector2 mousePosition)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null || Find.Camera == null)
            {
                return null;
            }

            // Fast path: in almost every real fight the active boss is the only pawn that needs an
            // expanded hitbox. This avoids scanning every pawn on large maps and removes the visible
            // click latency reported during Reactor Saint tests.
            Pawn activeBoss = Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>()?.ActiveBoss;
            if (IsExpandedSelectableBoss(activeBoss, out SelectionProfile activeProfile))
            {
                Rect activeRect = GetScreenSelectionRect(activeBoss, activeProfile);
                if (activeRect.Contains(mousePosition))
                {
                    return activeBoss;
                }
            }

            Pawn best = null;
            int bestPriority = int.MinValue;
            float bestDistance = float.MaxValue;
            IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn == activeBoss || !IsExpandedSelectableBoss(pawn, out SelectionProfile profile))
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

        private static bool CanUseExpandedSelection(Vector2 mousePosition)
        {
            if (!AbyssalProtocolMod.Settings.enableBossExpandedSelection || Current.ProgramState != ProgramState.Playing)
            {
                return false;
            }

            if (ABY_DevToolUtility.IsDebugToolActiveForInput())
            {
                return false;
            }

            if (AbyssalBossBarRenderer.MouseOverInteractiveRect(mousePosition))
            {
                return false;
            }

            return Find.CurrentMap != null && Find.Selector != null && Find.Camera != null;
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
