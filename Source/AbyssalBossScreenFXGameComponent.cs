using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class AbyssalBossScreenFXGameComponent : GameComponent
    {
        private Pawn activeBoss;
        private Map effectMap;
        private float currentStrength;
        private int effectStartTick;

        public AbyssalBossScreenFXGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref activeBoss, "activeBoss");
            Scribe_References.Look(ref effectMap, "effectMap");
            Scribe_Values.Look(ref currentStrength, "currentStrength", 0f);
            Scribe_Values.Look(ref effectStartTick, "effectStartTick", 0);
        }

        public void RegisterBoss(Pawn boss)
        {
            if (boss == null)
                return;

            activeBoss = boss;
            effectMap = boss.MapHeld;
            effectStartTick = Find.TickManager.TicksGame;
            currentStrength = Mathf.Max(currentStrength, 0.65f);
        }

        private bool BossAlive()
        {
            return activeBoss != null
                && !activeBoss.Destroyed
                && activeBoss.Spawned
                && !activeBoss.Dead
                && activeBoss.MapHeld != null;
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            bool bossAlive = BossAlive();
            float target = bossAlive ? 1f : 0f;
            float step = bossAlive ? 0.010f : 0.020f;

            currentStrength = Mathf.MoveTowards(currentStrength, target, step);

            if (!bossAlive && currentStrength <= 0.001f)
            {
                activeBoss = null;
                effectMap = null;
            }
        }

        public void DrawOverlay()
        {
            if (currentStrength <= 0.001f)
                return;

            if (Find.CurrentMap == null || effectMap == null || Find.CurrentMap != effectMap)
                return;

            if (Event.current == null || Event.current.type != EventType.Repaint)
                return;

            float t = (Find.TickManager.TicksGame - effectStartTick) * 0.05f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t);

            Rect rect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);

            Color outer = new Color(
                1f,
                0.22f,
                0.04f,
                currentStrength * (0.045f + pulse * 0.030f)
            );

            Color inner = new Color(
                1f,
                0.82f,
                0.12f,
                currentStrength * (0.018f + pulse * 0.025f)
            );

            Widgets.DrawBoxSolid(rect, outer);
            Widgets.DrawBoxSolid(rect, inner);
        }
    }
}
