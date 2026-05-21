using System;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    /// <summary>
    /// Save-compatibility shell for the first miniboss HP-bar implementation.
    /// The actual drawing is now routed through AbyssalBossScreenFXGameComponent,
    /// because existing saves do not automatically receive newly introduced GameComponents.
    /// </summary>
    [Obsolete("Miniboss health bars are now drawn by AbyssalBossScreenFXGameComponent for existing-save compatibility.")]
    public sealed class GameComponent_ABY_MiniBossHealthBars : GameComponent
    {
        public GameComponent_ABY_MiniBossHealthBars(Game game)
        {
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();

            // Avoid double-drawing on new saves. Keep a tiny fallback only for unusual saves
            // where the older boss UI GameComponent is missing entirely.
            if (Current.Game?.GetComponent<AbyssalBossScreenFXGameComponent>() != null)
            {
                return;
            }

            Event currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.Repaint)
            {
                return;
            }

            ABY_MiniBossHealthBarRenderer.DrawForCurrentMap(AbyssalProtocolMod.Settings);
        }
    }
}
