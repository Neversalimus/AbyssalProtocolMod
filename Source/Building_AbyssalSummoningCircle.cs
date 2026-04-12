using RimWorld;
using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Building_AbyssalSummoningCircle : Building_WorkTable
    {
        private static readonly Graphic OuterRingGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_OuterRing",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic InnerGlyphGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_InnerGlyphs",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic EnergyArcsGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_EnergyArcs",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic CoreGlowGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_CoreGlow",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Graphic IdleGlowGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_IdleGlow",
            ShaderDatabase.TransparentPostLight,
            Vector2.one,
            Color.white);

        private static readonly Vector2 OuterRingSize = new Vector2(9.45f, 9.45f);
        private static readonly Vector2 InnerGlyphSize = new Vector2(8.15f, 8.15f);
        private static readonly Vector2 EnergyArcsSize = new Vector2(5.35f, 5.35f);
        private static readonly Vector2 CoreGlowSize = new Vector2(3.10f, 3.10f);
        private static readonly Vector2 IdleGlowSize = new Vector2(9.10f, 9.10f);

        private bool Powered => GetComp<CompPowerTrader>()?.PowerOn ?? true;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            if (Map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float pulseA = 1f + Mathf.Sin(ticks * 0.055f) * 0.055f;
            float pulseB = 1f + Mathf.Sin((ticks * 0.095f) + 1.35f) * 0.080f;
            float pulseC = 1f + Mathf.Sin((ticks * 0.140f) + 2.10f) * 0.120f;

            float outerAngle = (ticks * 0.20f) % 360f;
            float innerAngle = 360f - ((ticks * 0.38f) % 360f);
            float energyAngle = (ticks * 0.72f) % 360f;

            Vector3 center = drawLoc;

            DrawLayer(IdleGlowGraphic, center, IdleGlowSize * pulseA, 0f, 0.004f);

            if (!Powered)
            {
                return;
            }

            DrawLayer(
                OuterRingGraphic,
                center,
                OuterRingSize * (1f + (pulseA - 1f) * 0.55f),
                outerAngle,
                0.010f);

            DrawLayer(
                InnerGlyphGraphic,
                center,
                InnerGlyphSize * (1f + (pulseB - 1f) * 0.30f),
                innerAngle,
                0.015f);

            DrawLayer(
                EnergyArcsGraphic,
                center,
                EnergyArcsSize * pulseC,
                energyAngle,
                0.020f);

            DrawLayer(
                CoreGlowGraphic,
                center,
                CoreGlowSize * (1f + (pulseC - 1f) * 1.25f),
                0f,
                0.025f);
        }

        private static void DrawLayer(Graphic graphic, Vector3 center, Vector2 drawSize, float angle, float yOffset)
        {
            if (graphic == null)
            {
                return;
            }

            Vector3 drawPos = center;
            drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + yOffset;

            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(
                drawPos,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(drawSize.x, 1f, drawSize.y));

            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }
    }
}
