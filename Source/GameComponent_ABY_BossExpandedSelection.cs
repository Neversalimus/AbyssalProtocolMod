using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class GameComponent_ABY_BossExpandedSelection : GameComponent
    {
        private bool pendingLeftClick;
        private Vector2 pendingLeftClickStart;

        public GameComponent_ABY_BossExpandedSelection(Game game)
        {
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                pendingLeftClick = true;
                pendingLeftClickStart = currentEvent.mousePosition;
                return;
            }

            if (currentEvent.type != EventType.MouseUp || currentEvent.button != 0)
            {
                return;
            }

            if (!pendingLeftClick)
            {
                return;
            }

            bool smallClick = Vector2.Distance(pendingLeftClickStart, currentEvent.mousePosition) <= 8f;
            pendingLeftClick = false;
            if (!smallClick)
            {
                return;
            }

            ABY_BossSelectionUtility.TrySelectBossUnderMouse(currentEvent);
        }
    }
}
