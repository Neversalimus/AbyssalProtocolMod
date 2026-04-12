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

        private static readonly Vector2 OuterRingSize = new Vector2(9.65f, 9.65f);
        private static readonly Vector2 InnerGlyphSize = new Vector2(6.40f, 6.40f);
        private static readonly Vector2 CoreGlowSize = new Vector2(2.85f, 2.85f);
        private static readonly Vector2 IdleGlowSize = new Vector2(9.20f, 9.20f);

        private bool Powered => GetComp<CompPowerTrader>()?.PowerOn ?? true;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            if (Map == null)
            {
                return;
            }

            float ticks = Find.TickManager?.TicksGame ?? 0f;
            float pulse = 1f + Mathf.Sin(ticks * 0.030f) * 0.035f;
            float outerAngle = (ticks * 0.085f) % 360f;
            float innerAngle = 360f - ((ticks * 0.140f) % 360f);
            Vector3 center = drawLoc;

            DrawLayer(IdleGlowGraphic, center, IdleGlowSize, 0f, 0.004f);

            if (!Powered)
            {
                return;
            }

            DrawLayer(
                OuterRingGraphic,
                center,
                OuterRingSize * (1f + (pulse - 1f) * 0.75f),
                outerAngle,
                0.010f);

            DrawLayer(
                InnerGlyphGraphic,
                center,
                InnerGlyphSize * (1f - (pulse - 1f) * 0.35f),
                innerAngle,
                0.015f);

            DrawLayer(
                CoreGlowGraphic,
                center,
                CoreGlowSize * (1f + (pulse - 1f) * 1.80f),
                0f,
                0.020f);
        }

        private static void DrawLayer(Graphic graphic, Vector3 center, Vector2 drawSize, float angle, float yOffset)
        {
            if (graphic == null)
            {
                return;
            }

            Vector3 drawPos = center;
            drawPos.y = AltitudeLayer.BuildingOnTop.AltitudeFor() + yOffset;

            Matrix4x4 matrix = default;
            matrix.SetTRS(
                drawPos,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(drawSize.x, 1f, drawSize.y));

            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }
    }
}
