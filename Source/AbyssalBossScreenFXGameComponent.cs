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
            currentStrength = Mathf.Max(currentStrength, 0.55f);
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            bool bossAlive = BossAlive();
            float targetStrength = bossAlive ? 1f : 0f;
            float step = bossAlive ? 0.012f : 0.022f;

            currentStrength = Mathf.MoveTowards(currentStrength, targetStrength, step);

            if (!bossAlive && currentStrength <= 0.001f)
            {
                activeBoss = null;
                effectMap = null;
            }
        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            DrawOverlay();
        }

        private void DrawOverlay()
        {
            if (Current.ProgramState != ProgramState.Playing)
                return;

            if (currentStrength <= 0.001f)
                return;

            if (Find.CurrentMap == null || effectMap == null || Find.CurrentMap != effectMap)
                return;

            float time = (Find.TickManager.TicksGame - effectStartTick) * 0.045f;
            float pulseA = 0.5f + 0.5f * Mathf.Sin(time);
            float pulseB = 0.5f + 0.5f * Mathf.Sin(time * 1.7f + 1.2f);

            float screenW = UI.screenWidth;
            float screenH = UI.screenHeight;

            Rect full = new Rect(0f, 0f, screenW, screenH);

            Color darkHeat = new Color(
                0.33f,
                0.04f,
                0.01f,
                currentStrength * (0.10f + pulseA * 0.05f)
            );

            Color fireGlow = new Color(
                1f,
                0.28f,
                0.03f,
                currentStrength * (0.05f + pulseA * 0.045f)
            );

            Color innerHeat = new Color(
                1f,
                0.78f,
                0.14f,
                currentStrength * (0.02f + pulseB * 0.035f)
            );

            Widgets.DrawBoxSolid(full, darkHeat);
            Widgets.DrawBoxSolid(full, fireGlow);
            Widgets.DrawBoxSolid(full, innerHeat);

            float outerX = screenW * 0.10f;
            float outerY = screenH * 0.10f;
            float innerX = screenW * 0.05f;
            float innerY = screenH * 0.05f;

            Color edgeDark = new Color(
                0.45f,
                0.06f,
                0.01f,
                currentStrength * (0.16f + pulseA * 0.08f)
            );

            Color edgeHot = new Color(
                1f,
                0.45f,
                0.08f,
                currentStrength * (0.05f + pulseB * 0.04f)
            );

            DrawSoftEdgeFrame(outerX, outerY, edgeDark, screenW, screenH, 6);
            DrawSoftEdgeFrame(innerX, innerY, edgeHot, screenW, screenH, 5);
        }

        private static void DrawSoftEdgeFrame(
            float thicknessX,
            float thicknessY,
            Color color,
            float screenW,
            float screenH,
            int layers)
        {
            if (thicknessX <= 0f || thicknessY <= 0f || layers <= 0)
                return;

            for (int i = 0; i < layers; i++)
            {
                float t = (float)(i + 1) / layers;

                float layerThicknessX = Mathf.Lerp(thicknessX, 2f, t);
                float layerThicknessY = Mathf.Lerp(thicknessY, 2f, t);

                Color layerColor = color;
                layerColor.a *= (1f - t) * 0.85f;

                if (layerColor.a <= 0.001f)
                    continue;

                Widgets.DrawBoxSolid(new Rect(0f, 0f, screenW, layerThicknessY), layerColor);
                Widgets.DrawBoxSolid(new Rect(0f, screenH - layerThicknessY, screenW, layerThicknessY), layerColor);
                Widgets.DrawBoxSolid(
                    new Rect(0f, layerThicknessY, layerThicknessX, screenH - layerThicknessY * 2f),
                    layerColor
                );
                Widgets.DrawBoxSolid(
                    new Rect(screenW - layerThicknessX, layerThicknessY, layerThicknessX, screenH - layerThicknessY * 2f),
                    layerColor
                );
            }
        }

        private bool BossAlive()
        {
            return activeBoss != null
                && !activeBoss.Destroyed
                && !activeBoss.Dead
                && activeBoss.Spawned
                && activeBoss.MapHeld != null;
        }
    }
}
