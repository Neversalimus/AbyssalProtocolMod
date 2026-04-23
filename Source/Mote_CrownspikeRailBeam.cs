using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class Mote_CrownspikeRailBeam : Thing
    {
        public Vector3 start;
        public Vector3 end;
        public int ticksLeft = 7;
        public int startingTicks = 7;
        public float width = 0.32f;
        public string texturePath = "Things/VFX/CrownspikeRail/ABY_CrownspikeRail_BeamGlow";
        public bool additivePulse = true;

        private Material cachedMaterial;

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

            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            float ageFactor = startingTicks <= 0 ? 1f : Mathf.Clamp01(ticksLeft / (float)startingTicks);
            float pulse = additivePulse ? (0.84f + Mathf.Sin(ageFactor * Mathf.PI) * 0.26f) : 1f;
            Vector3 scale = new Vector3(width * pulse, 1f, length);

            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, RailMaterial, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref start, "start");
            Scribe_Values.Look(ref end, "end");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 7);
            Scribe_Values.Look(ref startingTicks, "startingTicks", 7);
            Scribe_Values.Look(ref width, "width", 0.32f);
            Scribe_Values.Look(ref texturePath, "texturePath", "Things/VFX/CrownspikeRail/ABY_CrownspikeRail_BeamGlow");
            Scribe_Values.Look(ref additivePulse, "additivePulse", true);
        }

        private Material RailMaterial
        {
            get
            {
                if (cachedMaterial == null)
                {
                    cachedMaterial = MaterialPool.MatFrom(texturePath, ShaderDatabase.MoteGlow);
                }
                return cachedMaterial;
            }
        }
    }
}
