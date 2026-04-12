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

        private Map ritualPulseMap;
        private float ritualPulseStrength;

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
            Scribe_References.Look(ref ritualPulseMap, "ritualPulseMap");
            Scribe_Values.Look(ref ritualPulseStrength, "ritualPulseStrength", 0f);
        }

        public void RegisterBoss(Pawn boss)
        {
            if (boss == null)
            {
                return;
            }

            activeBoss = boss;
            effectMap = boss.MapHeld;
            effectStartTick = Find.TickManager.TicksGame;
            currentStrength = Mathf.Max(currentStrength, 0.55f);
            RegisterRitualPulse(effectMap, 0.35f);
        }

        public void RegisterRitualPulse(Map map, float strength)
        {
            if (map == null || strength <= 0f)
            {
                return;
            }

            ritualPulseMap = map;
            ritualPulseStrength = Mathf.Max(ritualPulseStrength, Mathf.Clamp01(strength));
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            bool bossAlive = BossAlive();
            float targetStrength = bossAlive ? 1f : 0f;
            float step = bossAlive ? 0.012f : 0.022f;
            currentStrength = Mathf.MoveTowards(currentStrength, targetStrength, step);

            ritualPulseStrength = Mathf.MoveTowards(ritualPulseStrength, 0f, 0.01f);
            if (ritualPulseStrength <= 0.001f)
            {
                ritualPulseMap = null;
            }

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
            {
                return;
            }

            Map currentMap = Find.CurrentMap;
            if (currentMap == null)
            {
                return;
            }

            float bossStrength = currentMap == effectMap ? currentStrength : 0f;
            float pulseStrength = currentMap == ritualPulseMap ? ritualPulseStrength : 0f;
            float totalStrength = Mathf.Clamp01(bossStrength + pulseStrength);
            if (totalStrength <= 0.001f)
            {
                return;
            }

            float time = Find.TickManager.TicksGame * 0.045f;
            float pulseA = 0.5f + 0.5f * Mathf.Sin(time);
            float pulseB = 0.5f + 0.5f * Mathf.Sin(time * 1.7f + 1.2f);
            float pulseC = 0.5f + 0.5f * Mathf.Sin(time * 2.6f + 0.4f);

            float screenW = UI.screenWidth;
            float screenH = UI.screenHeight;
            Rect full = new Rect(0f, 0f, screenW, screenH);

            Color darkHeat = new Color(0.33f, 0.04f, 0.01f, totalStrength * (0.10f + pulseA * 0.05f));
            Color fireGlow = new Color(1f, 0.28f, 0.03f, totalStrength * (0.05f + pulseB * 0.05f));
            Color innerHeat = new Color(1f, 0.78f, 0.14f, totalStrength * (0.02f + pulseC * 0.04f));

            Widgets.DrawBoxSolid(full, darkHeat);
            Widgets.DrawBoxSolid(full, fireGlow);
            Widgets.DrawBoxSolid(full, innerHeat);

            float outerX = screenW * Mathf.Lerp(0.06f, 0.10f, totalStrength);
            float outerY = screenH * Mathf.Lerp(0.06f, 0.10f, totalStrength);
            float innerX = screenW * Mathf.Lerp(0.03f, 0.05f, totalStrength);
            float innerY = screenH * Mathf.Lerp(0.03f, 0.05f, totalStrength);

            Color edgeDark = new Color(0.45f, 0.06f, 0.01f, totalStrength * (0.14f + pulseA * 0.08f));
            Color edgeHot = new Color(1f, 0.45f, 0.08f, totalStrength * (0.05f + pulseB * 0.05f));

            DrawSoftEdgeFrame(outerX, outerY, edgeDark, screenW, screenH, 6);
            DrawSoftEdgeFrame(innerX, innerY, edgeHot, screenW, screenH, 5);
        }

        private static void DrawSoftEdgeFrame(float thicknessX, float thicknessY, Color color, float screenW, float screenH, int layers)
        {
            if (thicknessX <= 0f || thicknessY <= 0f || layers <= 0)
            {
                return;
            }

            for (int i = 0; i < layers; i++)
            {
                float t = (float)(i + 1) / layers;
                float layerThicknessX = Mathf.Lerp(thicknessX, 2f, t);
                float layerThicknessY = Mathf.Lerp(thicknessY, 2f, t);
                Color layerColor = color;
                layerColor.a *= (1f - t) * 0.85f;

                if (layerColor.a <= 0.001f)
                {
                    continue;
                }

                Widgets.DrawBoxSolid(new Rect(0f, 0f, screenW, layerThicknessY), layerColor);
                Widgets.DrawBoxSolid(new Rect(0f, screenH - layerThicknessY, screenW, layerThicknessY), layerColor);
                Widgets.DrawBoxSolid(new Rect(0f, layerThicknessY, layerThicknessX, screenH - layerThicknessY * 2f), layerColor);
                Widgets.DrawBoxSolid(new Rect(screenW - layerThicknessX, layerThicknessY, layerThicknessX, screenH - layerThicknessY * 2f), layerColor);
            }
        }

        private bool BossAlive()
        {
            return activeBoss != null &&
                   !activeBoss.Destroyed &&
                   !activeBoss.Dead &&
                   activeBoss.Spawned &&
                   activeBoss.MapHeld != null;
        }
    }
}
