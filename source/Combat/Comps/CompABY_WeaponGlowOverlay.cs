using UnityEngine;
using Verse;

namespace AbyssalProtocol
{
    public class CompProperties_ABY_WeaponGlowOverlay : CompProperties
    {
        public string texPath;
        public float drawSizeX = 1f;
        public float drawSizeZ = 1f;
        public float alpha = 0.58f;
        public float altitudeOffset = 0.004f;

        public CompProperties_ABY_WeaponGlowOverlay()
        {
            compClass = typeof(CompABY_WeaponGlowOverlay);
        }
    }

    public class CompABY_WeaponGlowOverlay : ThingComp
    {
        private Material cachedMaterial;

        private CompProperties_ABY_WeaponGlowOverlay Props => (CompProperties_ABY_WeaponGlowOverlay)props;

        private Material GlowMaterial
        {
            get
            {
                if (cachedMaterial == null && !Props.texPath.NullOrEmpty())
                {
                    cachedMaterial = MaterialPool.MatFrom(Props.texPath, ShaderDatabase.MoteGlow, new Color(1f, 1f, 1f, Mathf.Clamp01(Props.alpha)));
                }

                return cachedMaterial;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();

            if (parent == null || !parent.Spawned || parent.MapHeld == null || Props.texPath.NullOrEmpty())
            {
                return;
            }

            Material material = GlowMaterial;
            if (material == null)
            {
                return;
            }

            Vector3 drawPos = parent.DrawPos;
            drawPos.y += Mathf.Max(0.001f, Props.altitudeOffset);

            Vector3 scale = new Vector3(Mathf.Max(0.05f, Props.drawSizeX), 1f, Mathf.Max(0.05f, Props.drawSizeZ));
            Quaternion rotation = Quaternion.AngleAxis(parent.Rotation.AsAngle, Vector3.up);
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, rotation, scale);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }
    }
}
