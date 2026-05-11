using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class GameComponent_ABY_BossExpandedSelection : GameComponent
    {
        private Pawn pendingBossClick;
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
                pendingBossClick = null;
                pendingLeftClickStart = currentEvent.mousePosition;

                if (ABY_BossSelectionUtility.TryBeginExpandedBossClick(currentEvent, out Pawn boss))
                {
                    pendingBossClick = boss;
                    currentEvent.Use();
                }
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
            {
                if (pendingBossClick != null)
                {
                    if (Vector2.Distance(pendingLeftClickStart, currentEvent.mousePosition) > 8f)
                    {
                        pendingBossClick = null;
                    }
                    else
                    {
                        currentEvent.Use();
                    }
                }
                return;
            }

            if (currentEvent.type != EventType.MouseUp || currentEvent.button != 0)
            {
                return;
            }

            Pawn bossToSelect = pendingBossClick;
            pendingBossClick = null;
            if (bossToSelect == null)
            {
                return;
            }

            bool smallClick = Vector2.Distance(pendingLeftClickStart, currentEvent.mousePosition) <= 8f;
            if (!smallClick)
            {
                return;
            }

            if (ABY_BossSelectionUtility.TryCompleteExpandedBossClick(currentEvent, bossToSelect))
            {
                currentEvent.Use();
            }
        }
    }
}
