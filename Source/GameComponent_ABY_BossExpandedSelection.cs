using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public sealed class GameComponent_ABY_BossExpandedSelection : GameComponent
    {
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

            ABY_BossSelectionUtility.TrySelectBossUnderMouse(currentEvent);
        }
    }
}
