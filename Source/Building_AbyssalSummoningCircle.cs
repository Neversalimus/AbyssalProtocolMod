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

        private static readonly Graphic DataLatticeGraphic = GraphicDatabase.Get<Graphic_Single>(
            "Things/Building/ABY_SummoningCircle_DataLattice",
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

        private static readonly Vector2 OuterRingSize = new Vector2(9.38f, 9.38f);
        private static readonly Vector2 InnerGlyphSize = new Vector2(8.72f, 8.72f);
        private static readonly Vector2 EnergyArcsSize = new Vector2(8.36f, 8.36f);
        private static readonly Vector2 DataLatticeSize = new Vector2(7.52f, 7.52f);
        private static readonly Vector2 CoreGlowSize = new Vector2(2.84f, 2.84f);
        private static readonly Vector2 IdleGlowSize = new Vector2(9.20f, 9.20f);

        private bool Powered => GetComp<CompPowerTrader>()?.PowerOn ?? true;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);

            if (Map == null)
            {
                return;
            }

            int ticks = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            float phase = (thingIDNumber % 997) * 0.031f;

            float pulseA = 1f + Mathf.Sin((ticks * 0.045f) + phase) * 0.045f;
            float pulseB = 1f + Mathf.Sin((ticks * 0.080f) + 1.35f + phase) * 0.060f;
            float pulseC = 1f + Mathf.Sin((ticks * 0.135f) + 2.15f + phase) * 0.090f;

            float outerAngle = (ticks * 0.18f) % 360f;
            float innerAngle = 360f - ((ticks * 0.34f) % 360f);
            float energyAngle = (ticks * 0.95f) % 360f;
            float latticeAngle = 360f - ((ticks * 0.56f) % 360f);

            DrawLayer(IdleGlowGraphic, drawLoc, IdleGlowSize * pulseA, 0f, 0.004f);

            if (!Powered)
            {
                return;
            }

            DrawLayer(
                OuterRingGraphic,
                drawLoc,
                OuterRingSize * (1f + (pulseA - 1f) * 0.45f),
                outerAngle,
                0.010f);

            DrawLayer(
                InnerGlyphGraphic,
                drawLoc,
                InnerGlyphSize * (1f + (pulseB - 1f) * 0.26f),
                innerAngle,
                0.014f);

            DrawLayer(
                EnergyArcsGraphic,
                drawLoc,
                EnergyArcsSize * (1f + (pulseC - 1f) * 0.55f),
                energyAngle,
                0.018f);

            DrawLayer(
                DataLatticeGraphic,
                drawLoc,
                DataLatticeSize * (1f + (pulseB - 1f) * 0.22f),
                latticeAngle,
                0.021f);

            DrawLayer(
                CoreGlowGraphic,
                drawLoc,
                CoreGlowSize * (1f + (pulseC - 1f) * 1.10f),
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
