using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Defers heavy map-transfer / long-flow actions that originate from IMGUI buttons.
    ///
    /// RimWorld and several UI-heavy modpacks can leave Widgets' mouse position stack unbalanced
    /// when a gizmo or custom window immediately changes maps, destroys a temporary map, or performs
    /// a large pawn transfer from inside the same IMGUI event that is drawing scroll views.
    /// Queueing those actions by one Unity frame keeps the visible behavior the same while letting
    /// all active scroll views close normally before Dominion pocket entry/exit work begins.
    /// </summary>
    public sealed class ABY_DeferredUIActionGameComponent : GameComponent
    {
        private sealed class QueuedAction
        {
            public string Label;
            public Action Action;
            public int ExecuteAfterFrame;
        }

        private readonly List<QueuedAction> queuedActions = new List<QueuedAction>();

        public ABY_DeferredUIActionGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                queuedActions.Clear();
            }
        }

        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();
            if (queuedActions.Count == 0)
            {
                return;
            }

            int frame = Time.frameCount;
            for (int i = queuedActions.Count - 1; i >= 0; i--)
            {
                QueuedAction queued = queuedActions[i];
                if (queued == null || queued.Action == null)
                {
                    queuedActions.RemoveAt(i);
                    continue;
                }

                if (queued.ExecuteAfterFrame > frame)
                {
                    continue;
                }

                queuedActions.RemoveAt(i);
                try
                {
                    queued.Action();
                }
                catch (Exception ex)
                {
                    ABY_LogThrottleUtility.Warning(
                        "deferred-ui-action-" + (queued.Label ?? "unknown"),
                        "[Abyssal Protocol] Deferred UI action failed (" + (queued.Label ?? "unknown") + "): " + ex,
                        900);
                }
            }
        }

        public static bool TryQueue(string label, Action action, out string failReason)
        {
            failReason = null;
            if (action == null)
            {
                failReason = "No deferred action was provided.";
                return false;
            }

            Game game = Current.Game;
            if (game == null)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception ex)
                {
                    failReason = ex.Message;
                    ABY_LogThrottleUtility.Warning(
                        "deferred-ui-action-no-game-" + (label ?? "unknown"),
                        "[Abyssal Protocol] Immediate fallback for deferred UI action failed (" + (label ?? "unknown") + "): " + ex,
                        900);
                    return false;
                }
            }

            ABY_DeferredUIActionGameComponent component = null;
            try
            {
                component = game.GetComponent<ABY_DeferredUIActionGameComponent>();
            }
            catch (Exception ex)
            {
                ABY_LogThrottleUtility.Warning(
                    "deferred-ui-action-component-missing",
                    "[Abyssal Protocol] Could not resolve deferred UI action component; action will run immediately: " + ex.Message,
                    900);
            }

            if (component == null)
            {
                try
                {
                    action();
                    return true;
                }
                catch (Exception ex)
                {
                    failReason = ex.Message;
                    ABY_LogThrottleUtility.Warning(
                        "deferred-ui-action-immediate-fallback-" + (label ?? "unknown"),
                        "[Abyssal Protocol] Immediate fallback for deferred UI action failed (" + (label ?? "unknown") + "): " + ex,
                        900);
                    return false;
                }
            }

            component.Enqueue(label, action);
            return true;
        }

        private void Enqueue(string label, Action action)
        {
            queuedActions.Add(new QueuedAction
            {
                Label = label ?? "unnamed",
                Action = action,
                ExecuteAfterFrame = Time.frameCount + 1
            });
        }
    }
}
