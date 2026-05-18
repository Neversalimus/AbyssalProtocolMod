using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Mote_ABY_ChoirArcAnimated : Thing
    {
        public string framePathPrefix = "Things/VFX/ChoirArc/ABY_ChoirArcImpact_";
        public int frameCount = 8;
        public int ticksPerFrame = 2;
        public int ticksLeft = 16;
        public int startingTicks = 16;
        public float drawSizeX = 1.15f;
        public float drawSizeZ = 1.15f;
        public float rotation;
        public Vector3 exactPosition;

        private Material[] cachedMaterials;

        protected override void Tick()
        {
            base.Tick();
            ticksLeft--;
            if (ticksLeft <= 0 && !Destroyed)
            {
                Destroy();
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            Material material = FrameMaterial;
            if (material == null)
            {
                return;
            }

            Vector3 drawPos = exactPosition == default(Vector3) ? drawLoc : exactPosition;
            drawPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Matrix4x4 matrix = Matrix4x4.TRS(
                drawPos,
                Quaternion.AngleAxis(rotation, Vector3.up),
                new Vector3(drawSizeX, 1f, drawSizeZ));

            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref framePathPrefix, "framePathPrefix", "Things/VFX/ChoirArc/ABY_ChoirArcImpact_");
            Scribe_Values.Look(ref frameCount, "frameCount", 8);
            Scribe_Values.Look(ref ticksPerFrame, "ticksPerFrame", 2);
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 16);
            Scribe_Values.Look(ref startingTicks, "startingTicks", 16);
            Scribe_Values.Look(ref drawSizeX, "drawSizeX", 1.15f);
            Scribe_Values.Look(ref drawSizeZ, "drawSizeZ", 1.15f);
            Scribe_Values.Look(ref rotation, "rotation", 0f);
            Scribe_Values.Look(ref exactPosition, "exactPosition");
        }

        private Material FrameMaterial
        {
            get
            {
                int count = Mathf.Max(1, frameCount);
                if (cachedMaterials == null || cachedMaterials.Length != count)
                {
                    cachedMaterials = new Material[count];
                }

                int elapsed = Mathf.Max(0, startingTicks - ticksLeft);
                int frameIndex = Mathf.Clamp(elapsed / Mathf.Max(1, ticksPerFrame), 0, count - 1);
                Material material = cachedMaterials[frameIndex];
                if (material == null)
                {
                    string path = framePathPrefix + (frameIndex + 1).ToString("00");
                    material = MaterialPool.MatFrom(path, ShaderDatabase.MoteGlow);
                    cachedMaterials[frameIndex] = material;
                }

                return material;
            }
        }
    }
}
