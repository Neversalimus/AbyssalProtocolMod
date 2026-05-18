using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class GameComponent_ABY_BossExpandedSelection : GameComponent
    {
        private Pawn pendingBossClick;
        private Vector2 pendingLeftClickStart;
        private bool pendingExpandedClick;

        public GameComponent_ABY_BossExpandedSelection(Game game)
        {
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            if (!AbyssalProtocolMod.Settings.enableBossExpandedSelection || Current.ProgramState != ProgramState.Playing)
            {
                pendingBossClick = null;
                pendingExpandedClick = false;
                return;
            }

            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                pendingBossClick = null;
                pendingExpandedClick = false;
                pendingLeftClickStart = currentEvent.mousePosition;

                if (ABY_BossSelectionUtility.TryBeginExpandedBossClick(currentEvent, out Pawn boss))
                {
                    pendingBossClick = boss;
                    pendingExpandedClick = true;
                    currentEvent.Use();
                }
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
            {
                if (pendingExpandedClick)
                {
                    if (Vector2.Distance(pendingLeftClickStart, currentEvent.mousePosition) > 8f)
                    {
                        pendingBossClick = null;
                        pendingExpandedClick = false;
                    }
                    currentEvent.Use();
                }
                return;
            }

            if (currentEvent.type != EventType.MouseUp || currentEvent.button != 0)
            {
                return;
            }

            Pawn bossToSelect = pendingBossClick;
            bool wasPendingExpandedClick = pendingExpandedClick;
            pendingBossClick = null;
            pendingExpandedClick = false;

            if (!wasPendingExpandedClick || bossToSelect == null)
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
