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

        private static Texture2D overlayTex;
        private static Texture2D edgeTex;

        public AbyssalBossScreenFXGameComponent(Game game)
        {
            EnsureTextures();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref activeBoss, "activeBoss");
            Scribe_References.Look(ref effectMap, "effectMap");
            Scribe_Values.Look(ref currentStrength, "currentStrength", 0f);
            Scribe_Values.Look(ref effectStartTick, "effectStartTick", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureTextures();
            }
        }

        public void RegisterBoss(Pawn boss)
        {
            if (boss == null)
                return;

            EnsureTextures();

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

            Event ev = Event.current;
            if (ev == null || ev.type != EventType.Repaint)
                return;

            EnsureTextures();

            float time = (Find.TickManager.TicksGame - effectStartTick) * 0.045f;
            float pulseA = 0.5f + 0.5f * Mathf.Sin(time);
            float pulseB = 0.5f + 0.5f * Mathf.Sin(time * 1.7f + 1.2f);

            Rect full = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);

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

            GUI.color = darkHeat;
            GUI.DrawTexture(full, overlayTex, ScaleMode.StretchToFill, true);

            GUI.color = fireGlow;
            GUI.DrawTexture(full, overlayTex, ScaleMode.StretchToFill, true);

            GUI.color = innerHeat;
            GUI.DrawTexture(full, overlayTex, ScaleMode.StretchToFill, true);

            float edgeAlpha = currentStrength * (0.22f + pulseA * 0.10f);
            GUI.color = new Color(0.45f, 0.06f, 0.01f, edgeAlpha);
            GUI.DrawTexture(full, edgeTex, ScaleMode.StretchToFill, true);

            float edgeAlphaHot = currentStrength * (0.08f + pulseB * 0.05f);
            GUI.color = new Color(1f, 0.45f, 0.08f, edgeAlphaHot);
            GUI.DrawTexture(full, edgeTex, ScaleMode.StretchToFill, true);

            GUI.color = Color.white;
        }

        private bool BossAlive()
        {
            return activeBoss != null
                && !activeBoss.Destroyed
                && !activeBoss.Dead
                && activeBoss.Spawned
                && activeBoss.MapHeld != null;
        }

        private static void EnsureTextures()
        {
            if (overlayTex == null)
            {
                overlayTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                overlayTex.SetPixel(0, 0, Color.white);
                overlayTex.Apply();
            }

            if (edgeTex == null)
            {
                edgeTex = MakeEdgeTexture(256);
            }
        }

        private static Texture2D MakeEdgeTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            float half = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - half) / half;
                    float ny = (y - half) / half;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);

                    float alpha = Mathf.InverseLerp(0.45f, 1.0f, dist);
                    alpha = Mathf.Clamp01(alpha);
                    alpha *= alpha;

                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
