using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public static class ABY_BossSelectionUtility
    {
        private const float FallbackWidthCells = 4.2f;
        private const float FallbackHeightCells = 3.8f;
        private const float FallbackYOffsetCells = 0f;
        private const int FallbackPriority = 40;

        private static readonly Dictionary<string, ABY_BossSelectionProfileDef> ThingProfiles = new Dictionary<string, ABY_BossSelectionProfileDef>();
        private static readonly Dictionary<string, ABY_BossSelectionProfileDef> PawnKindProfiles = new Dictionary<string, ABY_BossSelectionProfileDef>();
        private static bool profilesCached;

        public static bool TryBeginExpandedBossClick(Event currentEvent, out Pawn boss)
        {
            boss = null;
            if (currentEvent == null || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return false;
            }

            if (!currentEvent.alt || !CanUseExpandedSelection(currentEvent.mousePosition))
            {
                return false;
            }

            Map map = Find.CurrentMap;
            if (map == null)
            {
                return false;
            }

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

            if (!currentEvent.alt || !CanUseExpandedSelection(currentEvent.mousePosition))
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
            if (IsExpandedSelectableBoss(activeBoss, out ABY_BossSelectionProfileDef activeProfile))
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
                if (pawn == activeBoss || !IsExpandedSelectableBoss(pawn, out ABY_BossSelectionProfileDef profile))
                {
                    continue;
                }

                Rect rect = GetScreenSelectionRect(pawn, profile);
                if (!rect.Contains(mousePosition))
                {
                    continue;
                }

                float distance = Vector2.Distance(mousePosition, rect.center);
                int priority = profile?.priority ?? FallbackPriority;
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

            // Expanded visual-body selection is deliberately disabled while Dev Mode is on.
            // Debug tools use the same mouse events and must always win over cosmetic selection helpers.
            if (Prefs.DevMode || ABY_DevToolUtility.IsDebugToolActiveForInput())
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

        private static bool IsExpandedSelectableBoss(Pawn pawn, out ABY_BossSelectionProfileDef profile)
        {
            profile = null;
            if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.MapHeld != Find.CurrentMap)
            {
                return false;
            }

            profile = FindBestProfileFor(pawn);
            if (profile != null)
            {
                return true;
            }

            if (pawn.TryGetComp<CompABY_BossTrueDeath>() != null || pawn.TryGetComp<CompABY_BossNoDowned>() != null)
            {
                return true;
            }

            return false;
        }

        private static Rect GetScreenSelectionRect(Pawn pawn, ABY_BossSelectionProfileDef profile)
        {
            Camera camera = Find.Camera;
            float pixelsPerCell = ResolvePixelsPerCell(camera);
            float yOffsetCells = profile?.yOffsetCells ?? FallbackYOffsetCells;
            float widthCells = profile?.widthCells ?? FallbackWidthCells;
            float heightCells = profile?.heightCells ?? FallbackHeightCells;
            Vector3 worldPos = pawn.DrawPos + new Vector3(0f, 0f, yOffsetCells);
            Vector3 screen = camera.WorldToScreenPoint(worldPos);
            Vector2 guiPoint = new Vector2(screen.x, UI.screenHeight - screen.y);
            float width = Mathf.Max(26f, widthCells * pixelsPerCell);
            float height = Mathf.Max(26f, heightCells * pixelsPerCell);
            return new Rect(guiPoint.x - width * 0.5f, guiPoint.y - height * 0.5f, width, height);
        }

        private static ABY_BossSelectionProfileDef FindBestProfileFor(Pawn pawn)
        {
            EnsureProfilesCached();

            ABY_BossSelectionProfileDef best = null;
            string thingDefName = pawn.def?.defName;
            if (!thingDefName.NullOrEmpty())
            {
                ThingProfiles.TryGetValue(thingDefName, out best);
            }

            string pawnKindDefName = pawn.kindDef?.defName;
            if (!pawnKindDefName.NullOrEmpty() && PawnKindProfiles.TryGetValue(pawnKindDefName, out ABY_BossSelectionProfileDef kindProfile))
            {
                if (best == null || kindProfile.priority > best.priority)
                {
                    best = kindProfile;
                }
            }

            return best;
        }

        private static void EnsureProfilesCached()
        {
            if (profilesCached)
            {
                return;
            }

            profilesCached = true;
            ThingProfiles.Clear();
            PawnKindProfiles.Clear();

            List<ABY_BossSelectionProfileDef> allDefs = DefDatabase<ABY_BossSelectionProfileDef>.AllDefsListForReading;
            for (int i = 0; i < allDefs.Count; i++)
            {
                ABY_BossSelectionProfileDef profile = allDefs[i];
                if (profile == null)
                {
                    continue;
                }

                AddProfileNames(ThingProfiles, profile.bossThingDefNames, profile);
                AddProfileNames(PawnKindProfiles, profile.bossPawnKindDefNames, profile);
            }
        }

        private static void AddProfileNames(Dictionary<string, ABY_BossSelectionProfileDef> target, List<string> names, ABY_BossSelectionProfileDef profile)
        {
            if (target == null || names == null || names.Count == 0 || profile == null)
            {
                return;
            }

            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                if (name.NullOrEmpty())
                {
                    continue;
                }

                if (!target.TryGetValue(name, out ABY_BossSelectionProfileDef existing) || profile.priority > existing.priority)
                {
                    target[name] = profile;
                }
            }
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
