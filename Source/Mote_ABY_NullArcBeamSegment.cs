using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Mote_ABY_NullArcBeamSegment : Thing
    {
        public Vector3 start;
        public Vector3 end;
        public int ticksLeft = 8;
        public int startingTicks = 8;
        public int frameCount = 6;
        public int ticksPerFrame = 1;
        public float width = 0.24f;
        public string framePathPrefix = "Things/VFX/NullArcDischarger/ABY_NullArcChain_";

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
            if (start == default(Vector3) || end == default(Vector3))
            {
                return;
            }

            Material material = FrameMaterial;
            if (material == null)
            {
                return;
            }

            Vector3 a = start;
            Vector3 b = end;
            a.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            b.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            Vector3 delta = b - a;
            float length = delta.MagnitudeHorizontal();
            if (length <= 0.05f)
            {
                return;
            }

            Vector3 center = (a + b) * 0.5f;
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            // Source sheet is a horizontal left-to-right arc. Align local X with the world-space target vector.
            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg - 90f;
            float normalizedAge = startingTicks <= 0 ? 0f : Mathf.Clamp01(1f - ticksLeft / (float)startingTicks);
            float pulse = 0.86f + Mathf.Sin(normalizedAge * Mathf.PI) * 0.22f;
            Vector3 scale = new Vector3(length, 1f, Mathf.Max(0.02f, width * pulse));

            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref start, "start");
            Scribe_Values.Look(ref end, "end");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 8);
            Scribe_Values.Look(ref startingTicks, "startingTicks", 8);
            Scribe_Values.Look(ref frameCount, "frameCount", 6);
            Scribe_Values.Look(ref ticksPerFrame, "ticksPerFrame", 1);
            Scribe_Values.Look(ref width, "width", 0.24f);
            Scribe_Values.Look(ref framePathPrefix, "framePathPrefix", "Things/VFX/NullArcDischarger/ABY_NullArcChain_");
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
